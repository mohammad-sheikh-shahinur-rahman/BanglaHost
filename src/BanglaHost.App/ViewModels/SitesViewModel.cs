using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BanglaHost.App.Mvvm;
using BanglaHost.App.Services;
using BanglaHost.Core;
using Microsoft.UI.Xaml.Controls;

namespace BanglaHost.App.ViewModels;

/// <summary>
/// View-model for the Sites page. Owns the bound site collection, the add-form state,
/// and the Add/Edit/Delete/Refresh commands. The SQLite <see cref="SitesRepository"/> is
/// the durable source of truth; the <see cref="Engine"/> provisions the real running site
/// (nginx vhost + PHP + hosts entry) so a saved row is an actually-serving site.
/// </summary>
public sealed class SitesViewModel : ObservableObject
{
    /// <summary>Process-wide singleton so sites load once at startup and stay in sync across navigation.</summary>
    public static SitesViewModel Instance { get; } = new();

    private readonly SitesRepository _repo = SitesRepository.Instance;

    public ObservableCollection<SiteListItem> Sites { get; } = new();

    // ── option lists (combo box sources) ─────────────────────────────────────────
    public string[] PhpVersions { get; } = BanglaHost.Core.Services.PhpVersions;
    public string[] CmsOptions { get; } =
    {
        "Custom", "WordPress", "Laravel", "PHP", "Static", "React", "Vue", "Next.js",
    };
    public string[] WebServers { get; } = { "nginx", "apache" };

    private SitesViewModel()
    {
        var cfg = Config.Load();
        _selectedPhp = PhpVersions.Contains(cfg.DefaultPhp) ? cfg.DefaultPhp : PhpVersions.FirstOrDefault() ?? "8.4";
        _selectedWebServer = WebServers.Contains(cfg.DefaultWeb) ? cfg.DefaultWeb : "nginx";

        AddSiteCommand = new AsyncRelayCommand(_ => AddSiteAsync());
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
        EditSiteCommand = new RelayCommand(p => { if (p is SiteListItem i && EditRequested is not null) _ = EditRequested(i); });
        DeleteSiteCommand = new AsyncRelayCommand(p => p is SiteListItem i ? DeleteSiteAsync(i) : Task.CompletedTask);
        FixHostsCommand = new AsyncRelayCommand(p => p is SiteListItem i ? FixHostsAsync(i) : Task.CompletedTask);
        BrowseFolderCommand = new AsyncRelayCommand(_ => BrowseFolderAsync());
    }

    // ── form fields ──────────────────────────────────────────────────────────────
    private string _newSiteName = "";
    public string NewSiteName { get => _newSiteName; set => SetProperty(ref _newSiteName, value); }

    private string _selectedPhp;
    public string SelectedPhp { get => _selectedPhp; set => SetProperty(ref _selectedPhp, value); }

    private string _selectedCms = "Custom";
    public string SelectedCms
    {
        get => _selectedCms;
        set { if (SetProperty(ref _selectedCms, value)) OnPropertyChanged(nameof(IsCustomMode)); }
    }

    /// <summary>True when the user is adding a custom site with their own project folder.</summary>
    public bool IsCustomMode => SelectedCms == "Custom";

    private string _customRootPath = "";
    /// <summary>Document root for a Custom site (existing project folder on disk).</summary>
    public string CustomRootPath { get => _customRootPath; set => SetProperty(ref _customRootPath, value); }

    private string _selectedWebServer;
    public string SelectedWebServer { get => _selectedWebServer; set => SetProperty(ref _selectedWebServer, value); }

    private bool _enableHttps = true;
    public bool EnableHttps { get => _enableHttps; set => SetProperty(ref _enableHttps, value); }

    // ── status / busy ────────────────────────────────────────────────────────────
    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    private bool _isEmpty = true;
    public bool IsEmpty { get => _isEmpty; set => SetProperty(ref _isEmpty, value); }

    private bool _statusOpen;
    public bool StatusOpen { get => _statusOpen; set => SetProperty(ref _statusOpen, value); }

    private string _statusMessage = "";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;
    public InfoBarSeverity StatusSeverity { get => _statusSeverity; set => SetProperty(ref _statusSeverity, value); }

    // ── commands ─────────────────────────────────────────────────────────────────
    public ICommand AddSiteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand EditSiteCommand { get; }
    public ICommand DeleteSiteCommand { get; }
    public ICommand FixHostsCommand { get; }
    public ICommand BrowseFolderCommand { get; }

    /// <summary>Set by the view to show manual hosts instructions when UAC/elevation fails.</summary>
    public Func<string, Task>? ShowHostsHelpRequested;
    /// <summary>Set by the view to open the folder picker (Custom site type).</summary>
    public Func<Task<string?>>? BrowseFolderRequested;

    /// <summary>Set by the view so the VM can ask it to show the edit dialog (the dialog itself
    /// is a view concern; the VM keeps the data logic).</summary>
    public Func<SiteListItem, Task>? EditRequested;
    /// <summary>Set by the view to confirm a destructive delete. Returns true to proceed.</summary>
    public Func<SiteListItem, Task<bool>>? ConfirmDeleteRequested;

    private static string Tld => Config.Load().Tld;

    // ── load on startup / navigate ───────────────────────────────────────────────
    /// <summary>Load every saved site from SQLite. First reconciles any real engine sites that
    /// aren't in the catalog yet (e.g. created via the CLI or an older build) so nothing is lost.</summary>
    public async Task LoadAsync()
    {
        try
        {
            _repo.EnsureCreated();
            await Task.Run(() =>
            {
                ReconcileFromEngine();
                EnsureAllHostsMapped();
            });

            var records = await Task.Run(() => _repo.GetAll());
            var tld = Tld;
            var items = records.Select(r => SiteListItem.From(r, tld)).ToList();
            await RunOnUiAsync(() =>
            {
                Sites.Clear();
                foreach (var item in items) Sites.Add(item);
                UpdateEmpty();
            });
        }
        catch (Exception ex)
        {
            Log.Error("Sites: load failed", ex);
            await RunOnUiAsync(() => SetStatus("Could not load sites: " + ex.Message, InfoBarSeverity.Error));
        }
    }

    /// <summary>Import engine vhost sites that aren't yet in the SQLite catalog (best-effort).</summary>
    private void ReconcileFromEngine()
    {
        try
        {
            var snap = EngineHost.Instance.Engine.Api();
            foreach (var s in snap.Sites)
            {
                if (Engine.IsTool(s.Name)) continue;
                if (_repo.Exists(s.Name)) continue;
                _repo.Add(new SiteRecord
                {
                    SiteName = s.Name,
                    PhpVersion = NormalizePhp(s.Php),
                    Cms = "Imported",
                    WebServer = string.IsNullOrWhiteSpace(s.Server) ? "nginx" : s.Server,
                    Https = s.Secure,
                    CreatedAt = DateTime.Now.ToString("o"),
                });
                Log.Info($"Sites: imported existing engine site '{s.Name}' into the catalog");
            }
        }
        catch (Exception ex)
        {
            // Non-fatal: the catalog still loads what it has.
            Log.Warn("Sites: engine reconcile skipped — " + ex.Message);
        }
    }

    /// <summary>Best-effort: map every catalog site to 127.0.0.1 in the Windows hosts file.</summary>
    private void EnsureAllHostsMapped()
    {
        try
        {
            var tld = Tld;
            foreach (var r in _repo.GetAll())
            {
                var domain = $"{r.SiteName}.{tld}";
                if (Hosts.IsMapped(domain)) continue;
                EngineHost.Instance.Engine.EnsureHosts(domain);
                if (Hosts.IsMapped(domain))
                    Log.Info($"Sites: mapped hosts {domain} → 127.0.0.1");
                else
                    Log.Warn($"Sites: hosts not mapped for {domain} — UAC may have been declined");
            }
        }
        catch (Exception ex)
        {
            Log.Warn("Sites: hosts reconcile skipped — " + ex.Message);
        }
    }

    // ── create ───────────────────────────────────────────────────────────────────
    public async Task AddSiteAsync()
    {
        var name = (NewSiteName ?? "").Trim().ToLowerInvariant();

        // 1) validate required fields
        if (string.IsNullOrWhiteSpace(name)) { SetStatus("Enter a site name first.", InfoBarSeverity.Error); return; }
        if (!SitesRepository.IsValidSiteName(name)) { SetStatus("Invalid site name. Use lowercase letters, digits, hyphens or dots (e.g. my-app).", InfoBarSeverity.Error); return; }
        if (IsNodeCms(SelectedCms) && !IsValidNodeSiteName(name))
        {
            SetStatus("React, Vue and Next.js site names must be lowercase letters, digits or hyphens only (no dots).", InfoBarSeverity.Error);
            return;
        }
        if (string.IsNullOrWhiteSpace(SelectedPhp) && !IsNodeCms(SelectedCms)) { SetStatus("Choose a PHP version.", InfoBarSeverity.Error); return; }
        if (string.IsNullOrWhiteSpace(SelectedCms)) { SetStatus("Choose a CMS / site type.", InfoBarSeverity.Error); return; }
        if (string.IsNullOrWhiteSpace(SelectedWebServer)) { SetStatus("Choose a web server.", InfoBarSeverity.Error); return; }

        string? customRoot = null;
        if (IsCustomCms(SelectedCms))
        {
            customRoot = (CustomRootPath ?? "").Trim();
            if (string.IsNullOrEmpty(customRoot) && BrowseFolderRequested is not null)
                customRoot = (await BrowseFolderRequested())?.Trim();
            if (string.IsNullOrEmpty(customRoot))
            {
                SetStatus("Custom sites need a project folder — click Browse or pick one when prompted.", InfoBarSeverity.Error);
                return;
            }
            if (!System.IO.Directory.Exists(customRoot))
            {
                SetStatus($"Folder not found: {customRoot}", InfoBarSeverity.Error);
                return;
            }
            CustomRootPath = customRoot;
        }

        // 2) reject duplicate names
        try { if (_repo.Exists(name)) { SetStatus($"A site named '{name}' already exists.", InfoBarSeverity.Error); return; } }
        catch (Exception ex) { Log.Error("Create site: duplicate check failed", ex); SetStatus(ex.Message, InfoBarSeverity.Error); return; }

        var php = SelectedPhp ?? Config.Load().DefaultPhp;
        var server = SelectedWebServer;
        var cms = SelectedCms;
        var https = EnableHttps;
        var type = CmsToType(cms);
        var domain = $"{name}.{Tld}";

        IsBusy = true;
        SetStatus($"Creating '{name}'… installing/starting required services if needed.", InfoBarSeverity.Informational);
        try
        {
            // 3) provision the real site via the engine (installs + starts nginx/php/db as needed).
            //    Runs off the UI thread. Throws on hard failure — in which case we do NOT persist,
            //    so the catalog never gets an orphan row for a site that didn't actually come up.
            var error = await Task.Run(() =>
            {
                try
                {
                    ProvisionSite(name, type, php, server, domain, https, customRoot);
                    return (string?)null;
                }
                catch (Exception ex) { return ex.Message; }
            });

            if (error is not null)
            {
                Log.Error($"Create site '{name}' failed during provisioning: {error}");
                SetStatus($"Couldn't create '{name}': {error}", InfoBarSeverity.Error);
                return;
            }

            var hostsOk = Hosts.IsMapped(domain);
            if (!hostsOk)
            {
                hostsOk = await Task.Run(() => EngineHost.Instance.Engine.EnsureHosts(domain));
            }

            // 4) persist to SQLite
            var rec = new SiteRecord
            {
                SiteName = name,
                PhpVersion = php,
                Cms = cms,
                WebServer = server,
                Https = https,
                CreatedAt = DateTime.Now.ToString("o"),
            };
            _repo.Add(rec);
            Log.Info($"Create site '{name}' (php={php}, cms={cms}, server={server}, https={https})");

            // 5) refresh the list instantly — no restart, no full reload
            Sites.Insert(0, SiteListItem.From(rec, Tld));
            UpdateEmpty();
            if (hostsOk)
                SetStatus($"Site '{name}' created successfully — http{(https ? "s" : "")}://{domain}", InfoBarSeverity.Success);
            else
            {
                SetStatus($"Site '{name}' was created but {domain} isn't in your hosts file yet. Click the globe icon on the row (or allow the UAC prompt) to fix DNS.", InfoBarSeverity.Warning);
                if (ShowHostsHelpRequested is not null) await ShowHostsHelpRequested(domain);
            }

            // reset the form for the next add
            NewSiteName = "";
            CustomRootPath = "";
            EnableHttps = true;
        }
        catch (BhException ex) { Log.Error($"Create site '{name}' database error", ex); SetStatus(ex.Message, InfoBarSeverity.Error); }
        catch (Exception ex) { Log.Error($"Create site '{name}' unexpected error", ex); SetStatus("Unexpected error: " + ex.Message, InfoBarSeverity.Error); }
        finally { IsBusy = false; }
    }

    /// <summary>Map a site's domain to 127.0.0.1 in the Windows hosts file (needs UAC once).</summary>
    public async Task FixHostsAsync(SiteListItem item)
    {
        if (item is null) return;
        var domain = item.Domain;
        IsBusy = true;
        SetStatus($"Mapping {domain} → 127.0.0.1… allow the UAC prompt if Windows asks.", InfoBarSeverity.Informational);
        try
        {
            var ok = await Task.Run(() => EngineHost.Instance.Engine.EnsureHosts(domain));
            if (ok)
                SetStatus($"{domain} now resolves to 127.0.0.1 — try opening the site again.", InfoBarSeverity.Success);
            else
            {
                SetStatus($"Couldn't update the hosts file for {domain}.", InfoBarSeverity.Error);
                if (ShowHostsHelpRequested is not null) await ShowHostsHelpRequested(domain);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Fix hosts for '{domain}' failed", ex);
            SetStatus("Couldn't update hosts: " + ex.Message, InfoBarSeverity.Error);
        }
        finally { IsBusy = false; }
    }

    // ── update ───────────────────────────────────────────────────────────────────
    /// <summary>Apply edited fields to a site: re-point PHP / web server / HTTPS via the engine,
    /// then persist to SQLite and refresh the row. SiteName is immutable (renaming a live site
    /// would mean moving its vhost, files, DB and hosts entry — out of scope).</summary>
    public async Task UpdateSiteAsync(SiteListItem item, string php, string cms, string server, bool https)
    {
        if (item is null) return;
        var name = item.SiteName;
        var domain = item.Domain;

        if (string.IsNullOrWhiteSpace(php)) { SetStatus("Choose a PHP version.", InfoBarSeverity.Error); return; }
        if (string.IsNullOrWhiteSpace(cms)) { SetStatus("Choose a CMS / site type.", InfoBarSeverity.Error); return; }
        if (string.IsNullOrWhiteSpace(server)) { SetStatus("Choose a web server.", InfoBarSeverity.Error); return; }

        IsBusy = true;
        SetStatus($"Updating '{name}'…", InfoBarSeverity.Informational);
        try
        {
            var wasHttps = item.Https;
            var phpChanged = !string.Equals(php, item.PhpVersion, StringComparison.OrdinalIgnoreCase);
            var serverChanged = !string.Equals(server, item.WebServer, StringComparison.OrdinalIgnoreCase);
            var httpsTurnedOn = https && !wasHttps;

            var error = await Task.Run(() =>
            {
                try
                {
                    if (phpChanged) EngineHost.Instance.Engine.SitePhp(name, php);
                    if (serverChanged) EngineHost.Instance.Engine.SiteServer(name, server);
                    if (httpsTurnedOn) EngineHost.Instance.Engine.Secure(domain);
                    return (string?)null;
                }
                catch (Exception ex) { return ex.Message; }
            });

            if (error is not null)
            {
                Log.Error($"Update site '{name}' failed during apply: {error}");
                SetStatus($"Couldn't update '{name}': {error}", InfoBarSeverity.Error);
                return;
            }

            // Note: turning HTTPS off keeps the existing certificate on disk (the engine has no
            // "unsecure" step); the catalog flag is updated so the URL reflects the user's choice.
            item.PhpVersion = php;
            item.Cms = cms;
            item.WebServer = server;
            item.Https = https;

            _repo.Update(new SiteRecord { Id = item.Id, SiteName = name, PhpVersion = php, Cms = cms, WebServer = server, Https = https });
            Log.Info($"Update site '{name}' (php={php}, cms={cms}, server={server}, https={https})");

            // Honest about a no-op direction: the engine can add HTTPS but not remove a cert.
            var note = (wasHttps && !https) ? " (existing certificate left in place)" : "";
            SetStatus($"Site '{name}' updated.{note}", InfoBarSeverity.Success);
        }
        catch (BhException ex) { Log.Error($"Update site '{name}' database error", ex); SetStatus(ex.Message, InfoBarSeverity.Error); }
        catch (Exception ex) { Log.Error($"Update site '{name}' unexpected error", ex); SetStatus("Unexpected error: " + ex.Message, InfoBarSeverity.Error); }
        finally { IsBusy = false; }
    }

    // ── delete ───────────────────────────────────────────────────────────────────
    public async Task DeleteSiteAsync(SiteListItem item)
    {
        if (item is null) return;
        if (ConfirmDeleteRequested is not null && !await ConfirmDeleteRequested(item)) return;

        var name = item.SiteName;
        IsBusy = true;
        SetStatus($"Removing '{name}'…", InfoBarSeverity.Informational);
        try
        {
            // Remove the live site mapping but keep files + database by default (non-destructive).
            var error = await Task.Run(() =>
            {
                try { EngineHost.Instance.Engine.SiteRemove(name, purgeFiles: false, dropDb: false); return (string?)null; }
                catch (Exception ex) { return ex.Message; }
            });
            if (error is not null) Log.Warn($"Delete site '{name}': engine remove reported: {error}");

            _repo.Delete(name);
            Log.Info($"Delete site '{name}'");

            Sites.Remove(item);
            UpdateEmpty();
            SetStatus($"Site '{name}' deleted.", InfoBarSeverity.Success);
        }
        catch (BhException ex) { Log.Error($"Delete site '{name}' database error", ex); SetStatus(ex.Message, InfoBarSeverity.Error); }
        catch (Exception ex) { Log.Error($"Delete site '{name}' unexpected error", ex); SetStatus("Unexpected error: " + ex.Message, InfoBarSeverity.Error); }
        finally { IsBusy = false; }
    }

    // ── helpers ──────────────────────────────────────────────────────────────────
    /// <summary>Remove a site from the bound list after a delete elsewhere (e.g. Dashboard).</summary>
    public void RemoveSite(string name)
    {
        var item = Sites.FirstOrDefault(s => string.Equals(s.SiteName, name, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;
        Sites.Remove(item);
        UpdateEmpty();
    }

    /// <summary>Refresh or insert a catalog row in the bound list (e.g. after a Dashboard edit).</summary>
    public void ApplyCatalogRecord(SiteRecord rec)
    {
        var tld = Tld;
        var item = Sites.FirstOrDefault(s => s.Id == rec.Id && rec.Id > 0
            || string.Equals(s.SiteName, rec.SiteName, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            Sites.Insert(0, SiteListItem.From(rec, tld));
            UpdateEmpty();
            return;
        }
        item.PhpVersion = rec.PhpVersion;
        item.Cms = rec.Cms;
        item.WebServer = rec.WebServer;
        item.Https = rec.Https;
    }

    private void SetStatus(string message, InfoBarSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        StatusOpen = true;
    }

    private void UpdateEmpty() => IsEmpty = Sites.Count == 0;

    private static async Task RunOnUiAsync(Action action)
    {
        var dq = App.Window?.DispatcherQueue;
        if (dq is null || dq.HasThreadAccess)
        {
            action();
            return;
        }
        var tcs = new TaskCompletionSource();
        dq.TryEnqueue(() =>
        {
            try { action(); tcs.SetResult(); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        await tcs.Task;
    }

    public static string CmsToType(string cms) => cms switch
    {
        "Custom" => "custom",
        "WordPress" => "wordpress",
        "Laravel" => "laravel",
        "PHP" => "php",
        "Static" => "others",
        "React" => "react",
        "Vue" => "vue",
        "Next.js" => "nextjs",
        _ => "others",
    };

    public static bool IsNodeCms(string cms) =>
        cms is "React" or "Vue" or "Next.js";

    public static bool IsCustomCms(string cms) => cms == "Custom";

    private async Task BrowseFolderAsync()
    {
        if (BrowseFolderRequested is null) return;
        var path = await BrowseFolderRequested();
        if (!string.IsNullOrWhiteSpace(path)) CustomRootPath = path;
    }

    private static bool IsValidNodeSiteName(string name) =>
        !string.IsNullOrWhiteSpace(name) &&
        System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z0-9]([a-z0-9-]*[a-z0-9])?$");

    private static void ProvisionSite(string name, string type, string php, string server, string domain, bool https, string? customRoot = null)
    {
        var engine = EngineHost.Instance.Engine;
        engine.EnsureSiteServices(type == "custom" ? "others" : type, php, server);
        switch (type)
        {
            case "react":
                engine.ReactSiteAdd(name);
                break;
            case "vue":
                engine.VueSiteAdd(name);
                break;
            case "nextjs":
                engine.NextJsSiteAdd(name);
                break;
            case "custom":
                engine.SiteAdd(name, php: php, root: customRoot, server: server, type: "others");
                break;
            default:
                engine.SiteAdd(name, php: php, root: null, server: server, type: type);
                break;
        }
        if (https) engine.Secure(domain);
        if (!Hosts.IsMapped(domain)) engine.EnsureHosts(domain);
    }

    /// <summary>Turn an engine php token ("php", "php@8.4", "8.4") into a bare version ("8.4").</summary>
    private static string NormalizePhp(string php)
    {
        if (string.IsNullOrWhiteSpace(php) || php == "php") return Config.Load().DefaultPhp;
        var at = php.IndexOf('@');
        return at >= 0 ? php[(at + 1)..] : php;
    }
}
