using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BanglaHost.App.Views;

public sealed partial class RuntimesPage : Page
{
    public RuntimesPage() => InitializeComponent();

    private void Page_Loaded(object sender, RoutedEventArgs e) => _ = CheckVersions();

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = CheckVersions();

    private async Task CheckVersions()
    {
        Busy.IsActive = true;
        
        GoVersionText.Text = await GetVersion("go", "version");
        RustVersionText.Text = await GetVersion("cargo", "--version");
        DockerVersionText.Text = await GetVersion("docker", "--version");

        Busy.IsActive = false;
    }

    private async Task<string> GetVersion(string exe, string args)
    {
        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                using var p = Process.Start(psi);
                if (p == null) return "Not installed";
                
                string output = p.StandardOutput.ReadLine()?.Trim() ?? "";
                p.WaitForExit();
                
                return string.IsNullOrEmpty(output) ? "Not installed" : output;
            }
            catch
            {
                return "Not installed";
            }
        });
    }

    private void DownloadGo_Click(object sender, RoutedEventArgs e) => OpenUrl("https://go.dev/dl/");
    private void DownloadRust_Click(object sender, RoutedEventArgs e) => OpenUrl("https://rustup.rs/");
    private void DownloadDocker_Click(object sender, RoutedEventArgs e) => OpenUrl("https://docs.docker.com/desktop/install/windows-install/");

    private void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
}
