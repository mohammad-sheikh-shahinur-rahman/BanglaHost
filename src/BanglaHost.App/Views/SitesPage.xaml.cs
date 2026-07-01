using System;
using System.Threading.Tasks;
using BanglaHost.App.Services;
using BanglaHost.App.ViewModels;
using BanglaHost.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BanglaHost.App.Views;

public sealed partial class SitesPage : Page
{
    public SitesViewModel ViewModel => SitesViewModel.Instance;

    public SitesPage()
    {
        InitializeComponent();

        ViewModel.BrowseFolderRequested = () => Picker.FolderAsync();

        ViewModel.ShowHostsHelpRequested = async domain => await ShowHostsHelpAsync(domain);
        
        ViewModel.EditRequested = async item =>
        {
            var phpBox = new ComboBox { Header = "PHP Version", Width = 200, Margin = new Thickness(0,0,0,12) };
            foreach (var v in ViewModel.PhpVersions) phpBox.Items.Add(v);
            phpBox.SelectedItem = item.PhpVersion;

            var cmsBox = new ComboBox { Header = "CMS", Width = 200, Margin = new Thickness(0,0,0,12) };
            foreach (var v in ViewModel.CmsOptions) cmsBox.Items.Add(v);
            cmsBox.SelectedItem = item.Cms;

            var srvBox = new ComboBox { Header = "Web Server", Width = 200, Margin = new Thickness(0,0,0,12) };
            foreach (var v in ViewModel.WebServers) srvBox.Items.Add(v);
            srvBox.SelectedItem = item.WebServer;

            var sslBox = new CheckBox { Content = "HTTPS", IsChecked = item.Https };

            var panel = new StackPanel();
            panel.Children.Add(phpBox);
            panel.Children.Add(cmsBox);
            panel.Children.Add(srvBox);
            panel.Children.Add(sslBox);

            var dlg = new ContentDialog
            {
                Title = $"Edit {item.SiteName}",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dlg.ShowAsync() == ContentDialogResult.Primary)
            {
                var php = phpBox.SelectedItem?.ToString() ?? "";
                var cms = cmsBox.SelectedItem?.ToString() ?? "";
                var server = srvBox.SelectedItem?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(php))
                {
                    await ShowFieldError("Choose a PHP version.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(cms))
                {
                    await ShowFieldError("Choose a CMS / site type.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(server))
                {
                    await ShowFieldError("Choose a web server.");
                    return;
                }

                await ViewModel.UpdateSiteAsync(item, php, cms, server, sslBox.IsChecked == true);
            }
        };

        ViewModel.ConfirmDeleteRequested = async item =>
        {
            var dlg = new ContentDialog
            {
                Title = "Delete Site",
                Content = $"Are you sure you want to delete '{item.SiteName}'?\nThis will remove the site mapping.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            return await dlg.ShowAsync() == ContentDialogResult.Primary;
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        await ViewModel.LoadAsync();
    }

    public static Visibility BoolToVis(bool b) => b ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility BoolToVisInverse(bool b) => b ? Visibility.Collapsed : Visibility.Visible;

    private Task ShowFieldError(string message) =>
        new ContentDialog
        {
            Title = "Missing required field",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        }.ShowAsync().AsTask();

    private async Task ShowHostsHelpAsync(string domain)
    {
        var line = Hosts.ManualLine(domain);
        var hostsPath = Paths.HostsFile;
        var panel = new StackPanel { Spacing = 12, MinWidth = 420 };
        panel.Children.Add(new TextBlock
        {
            Text = $"Windows needs a hosts-file entry so {domain} resolves to your PC. BanglaHost tried to add it automatically but couldn't (UAC declined, antivirus, or missing elevate helper).",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
        });
        panel.Children.Add(new TextBlock { Text = "Add this line to the hosts file as Administrator:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var lineBox = new TextBox { Text = line, IsReadOnly = true, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas") };
        panel.Children.Add(lineBox);
        panel.Children.Add(new TextBlock { Text = hostsPath, Opacity = 0.65, TextWrapping = TextWrapping.Wrap, FontSize = 12 });

        var dlg = new ContentDialog
        {
            Title = $"Fix DNS for {domain}",
            Content = panel,
            PrimaryButtonText = "Copy line",
            SecondaryButtonText = "Open hosts folder",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dp.SetText(line);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
            }
            catch { }
        }
        else if (result == ContentDialogResult.Secondary)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(hostsPath)!;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            catch { }
        }
    }
}
