using BanglaHost.App.Services;
using BanglaHost.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BanglaHost.App.Views;

public sealed partial class PhpPage : Page
{
    public ObservableCollection<PhpToolRow> Versions { get; } = new();

    public PhpPage() => InitializeComponent();

    private void Page_Loaded(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh()
    {
        Versions.Clear();
        var statusList = Php.Status();
        foreach (var s in statusList.Where(x => x.Installed))
        {
            var iniPath = Php.IniPath(s.Version);
            var iniContent = File.Exists(iniPath) ? File.ReadAllText(iniPath) : "";
            bool isXdebugOn = iniContent.Contains("zend_extension=xdebug");

            Versions.Add(new PhpToolRow
            {
                Version = s.Version,
                IsXdebugOn = isXdebugOn,
                StatusText = s.Running ? "Running" : "Stopped",
                StatusColor = new SolidColorBrush(s.Running ? Colors.SeaGreen : Colors.Gray)
            });
        }

        VersList.ItemsSource = Versions;
        EmptyVers.Visibility = Versions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Xdebug_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ts.Tag is string version)
        {
            var row = Versions.FirstOrDefault(v => v.Version == version);
            if (row == null || row.IsXdebugOn == ts.IsOn) return;

            try
            {
                EngineHost.Instance.Engine.PhpXdebug(ts.IsOn ? "on" : "off", version);
                row.IsXdebugOn = ts.IsOn;
            }
            catch
            {
                ts.IsOn = !ts.IsOn; // revert
            }
        }
    }

    private void EditIni_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string version)
        {
            var ini = Php.IniPath(version);
            if (File.Exists(ini))
                Process.Start(new ProcessStartInfo { FileName = ini, UseShellExecute = true });
        }
    }

    private void ErrorLog_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string version)
        {
            var logPath = Path.Combine(Paths.Logs, $"php{version}-error.log");
            if (!File.Exists(logPath)) File.WriteAllText(logPath, ""); // Create if missing
            Process.Start(new ProcessStartInfo { FileName = logPath, UseShellExecute = true });
        }
    }

    private async void UpdateComposer_Click(object sender, RoutedEventArgs e)
    {
        ComposerBusy.IsActive = true;
        ComposerStatus.Text = "Updating Composer...";
        try
        {
            await System.Threading.Tasks.Task.Run(() => EngineHost.Instance.Engine.Composer(new[] { "self-update" }));
            ComposerStatus.Text = "Composer updated successfully.";
        }
        catch (Exception ex)
        {
            ComposerStatus.Text = $"Error: {ex.Message}";
        }
        finally
        {
            ComposerBusy.IsActive = false;
        }
    }
}

public class PhpToolRow : System.ComponentModel.INotifyPropertyChanged
{
    public string Version { get; set; } = "";
    
    private bool _isXdebugOn;
    public bool IsXdebugOn 
    { 
        get => _isXdebugOn; 
        set { _isXdebugOn = value; PropertyChanged?.Invoke(this, new(nameof(IsXdebugOn))); } 
    }

    public string StatusText { get; set; } = "";
    public SolidColorBrush? StatusColor { get; set; }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
