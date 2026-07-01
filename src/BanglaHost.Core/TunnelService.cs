using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BanglaHost.Core;

public static class TunnelService
{
    private static readonly Dictionary<string, Process> RunningTunnels = new();
    private static readonly string ConfigPath = Path.Combine(Paths.Config, "tunnel.json");

    private static (string exePath, string tunnelName, string credPath) LoadConfig()
    {
        // Default values if config missing
        var exe = "C:/Program Files/Cloudflare/cloudflared.exe";
        var name = "tuneltest";
        var cred = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cloudflared", $"{name}.json");
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var obj = System.Text.Json.JsonSerializer.Deserialize<TunnelConfig>(json);
                if (obj != null)
                {
                    exe = string.IsNullOrWhiteSpace(obj.CloudflaredPath) ? exe : obj.CloudflaredPath;
                    name = string.IsNullOrWhiteSpace(obj.DefaultTunnel) ? name : obj.DefaultTunnel;
                    cred = string.IsNullOrWhiteSpace(obj.CredentialFile) ? cred : obj.CredentialFile;
                }
            }
        }
        catch { /* ignore config errors, fall back to defaults */ }
        return (exe, name, cred);
    }

    public static (bool ok, string msg) Start(string tunnelName = null)
    {
        var (exe, defName, _) = LoadConfig();
        var name = tunnelName ?? defName;
        if (!File.Exists(exe))
            return (false, $"cloudflared executable not found at {exe}");

        if (RunningTunnels.ContainsKey(name))
            return (true, $"Tunnel {name} already running");

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"tunnel run {name}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? Paths.Home,
        };
        try
        {
            var proc = Process.Start(psi);
            if (proc == null)
                return (false, "Failed to start cloudflared process");
            RunningTunnels[name] = proc;
            _ = Task.Run(() =>
            {
                var outLog = Path.Combine(Paths.Logs, $"{name}-tunnel.log");
                using var sw = new StreamWriter(outLog, true);
                while (!proc.HasExited)
                {
                    var line = proc.StandardOutput.ReadLine();
                    if (line != null) sw.WriteLine(line);
                }
                var err = proc.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(err)) sw.WriteLine(err);
            });
            return (true, $"Tunnel {name} started");
        }
        catch (Exception ex)
        {
            return (false, $"Exception starting tunnel: {ex.Message}");
        }
    }

    public static (bool ok, string msg) Stop(string tunnelName = null)
    {
        var (_, defName, _) = LoadConfig();
        var name = tunnelName ?? defName;
        if (!RunningTunnels.TryGetValue(name, out var proc))
            return (false, $"Tunnel {name} not running");
        try
        {
            if (!proc.HasExited)
                proc.Kill(true);
            proc.WaitForExit(2000);
            RunningTunnels.Remove(name);
            return (true, $"Tunnel {name} stopped");
        }
        catch (Exception ex)
        {
            return (false, $"Exception stopping tunnel: {ex.Message}");
        }
    }

    private class TunnelConfig
    {
        public string CloudflaredPath { get; set; }
        public string DefaultTunnel { get; set; }
        public string CredentialFile { get; set; }
    }
}
