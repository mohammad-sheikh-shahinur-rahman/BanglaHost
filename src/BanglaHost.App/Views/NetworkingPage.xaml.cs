using BanglaHost.App.Services;
using BanglaHost.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace BanglaHost.App.Views;

public sealed partial class NetworkingPage : Page
{
    public ObservableCollection<TunnelRow> Tunnels { get; } = new();

    public NetworkingPage() => InitializeComponent();

    private void Page_Loaded(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private async void Refresh()
    {
        // Update sites combobox
        try
        {
            var cfg = Config.Load();
            LanToggle.IsOn = cfg.LanSharing;
            QuicToggle.IsOn = cfg.QuicEnabled;
            TldBox.Text = cfg.Tld;
            PortBox.Text = cfg.PortForwarding;
            
            var snap = await EngineHost.Instance.Snapshot();
            var sites = snap.Sites.Select(s => s.Name).OrderBy(n => n).ToList();
            SiteBox.ItemsSource = sites;
            if (SiteBox.SelectedItem == null && sites.Count > 0)
                SiteBox.SelectedIndex = 0;
        }
        catch { }

        // Update active tunnels list
        Tunnels.Clear();
        var tunnels = Tunnel.List().ToList();
        foreach (var t in tunnels)
        {
            Tunnels.Add(new TunnelRow
            {
                Name = t.name,
                Url = string.IsNullOrEmpty(t.url) ? "Starting (URL pending...)" : t.url,
                HasUrl = !string.IsNullOrEmpty(t.url)
            });
        }

        EmptyTunnels.Visibility = Tunnels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TunnelsList.ItemsSource = Tunnels;
    }

    private async void StartTunnel_Click(object sender, RoutedEventArgs e)
    {
        if (SiteBox.SelectedItem is not string site || string.IsNullOrWhiteSpace(site))
        {
            StartStatus.Text = "Please select a site first.";
            return;
        }

        StartBusy.IsActive = true;
        StartStatus.Text = $"Starting tunnel for {site}... (this may take a few seconds if installing cloudflared)";
        
        try
        {
            var (ok, output) = await EngineHost.Instance.RunCaptured(
                () => EngineHost.Instance.Engine.Tunnel("start", site));
            
            StartStatus.Text = ok ? $"Tunnel started for {site}!" : $"Error: {output}";
        }
        catch (Exception ex)
        {
            StartStatus.Text = $"Error: {ex.Message}";
        }
        finally
        {
            StartBusy.IsActive = false;
            Refresh();
        }
    }

    private void StopTunnel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string site)
        {
            try
            {
                Tunnel.Stop(site);
                Refresh();
            }
            catch { }
        }
    }

    private void OpenTunnel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string url && url.StartsWith("http"))
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
    
    private async void ApplyRouting_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (btn != null) btn.IsEnabled = false;

        // Snapshot the controls on the UI thread, then apply on the engine thread.
        var lan  = LanToggle.IsOn;
        var quic = QuicToggle.IsOn;
        var tld  = TldBox.Text;
        var port = PortBox.Text;

        try
        {
            var (ok, output) = await EngineHost.Instance.RunCaptured(
                () => EngineHost.Instance.Engine.ApplyNetworking(lan, quic, tld, port));

            var dlg = new ContentDialog
            {
                Title = ok ? "Routing updated" : "Couldn't apply routing",
                Content = string.IsNullOrWhiteSpace(output)
                    ? (ok ? "Your routing settings have been applied and nginx reloaded." : "Nothing was changed.")
                    : output.Trim(),
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dlg.ShowAsync();
        }
        finally
        {
            if (btn != null) btn.IsEnabled = true;
            Refresh();
        }
    }
}

public class TunnelRow
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public bool HasUrl { get; set; }
}
