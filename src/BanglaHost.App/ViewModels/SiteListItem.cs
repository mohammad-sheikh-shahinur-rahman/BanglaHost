using System;
using BanglaHost.App.Mvvm;
using BanglaHost.Core;

namespace BanglaHost.App.ViewModels;

/// <summary>One bindable row in the Sites list. Mutable + observable so an in-place edit
/// (PHP/CMS/server/HTTPS) updates the UI instantly without rebuilding the collection.</summary>
public sealed class SiteListItem : ObservableObject
{
    public long Id { get; set; }

    private string _siteName = "";
    public string SiteName { get => _siteName; set { if (SetProperty(ref _siteName, value)) OnPropertyChanged(nameof(Url)); } }

    private string _domain = "";
    public string Domain { get => _domain; set { if (SetProperty(ref _domain, value)) OnPropertyChanged(nameof(Url)); } }

    private string _phpVersion = "";
    public string PhpVersion { get => _phpVersion; set { if (SetProperty(ref _phpVersion, value)) OnPropertyChanged(nameof(Badge)); } }

    private string _cms = "";
    public string Cms { get => _cms; set { if (SetProperty(ref _cms, value)) OnPropertyChanged(nameof(Badge)); } }

    private string _webServer = "";
    public string WebServer { get => _webServer; set { if (SetProperty(ref _webServer, value)) OnPropertyChanged(nameof(Badge)); } }

    private bool _https;
    public bool Https
    {
        get => _https;
        set { if (SetProperty(ref _https, value)) { OnPropertyChanged(nameof(HttpsText)); OnPropertyChanged(nameof(Url)); } }
    }

    public string CreatedAt { get; set; } = "";

    // ── display helpers (bound in the row template) ──────────────────────────────
    public string CreatedDisplay => DateTime.TryParse(CreatedAt, out var d) ? d.ToString("yyyy-MM-dd HH:mm") : CreatedAt;
    public string HttpsText => Https ? "HTTPS" : "HTTP";
    public string Url => (Https ? "https://" : "http://") + Domain;
    public string Badge => $"php {PhpVersion} · {Cms} · {WebServer}";

    public static SiteListItem From(SiteRecord r, string tld) => new()
    {
        Id = r.Id,
        SiteName = r.SiteName,
        Domain = $"{r.SiteName}.{tld}",
        PhpVersion = r.PhpVersion,
        Cms = r.Cms,
        WebServer = r.WebServer,
        Https = r.Https,
        CreatedAt = r.CreatedAt,
    };
}
