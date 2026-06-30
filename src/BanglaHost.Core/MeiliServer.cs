using System.Diagnostics;
using System.Net.Sockets;

namespace BanglaHost.Core;

public static class MeiliServer
{
    public const int Port = 7700;
    private static string DataDir => Path.Combine(Paths.Home, "meilidata");
    private static string LogFile => Path.Combine(Paths.Logs, "meilisearch.log");

    public static bool Running()
    {
        try { using var c = new TcpClient(); return c.ConnectAsync("127.0.0.1", Port).Wait(600) && c.Connected; }
        catch { return false; }
    }

    public static (bool ok, string msg) Start()
    {
        if (Running()) return (true, "Meilisearch already running");
        var exe = Tools.MeiliExe();
        if (exe is null) return (false, "meilisearch.exe not found — install Meilisearch");
        
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(Paths.Logs);

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"--db-path \"{DataDir}\" --http-addr 127.0.0.1:{Port}",
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        // Meilisearch logs to stdout/stderr
        var p = Process.Start(psi);
        if (p != null)
        {
            // Background read to log file to prevent blocking
            Task.Run(async () =>
            {
                using var fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var sw = new StreamWriter(fs);
                while (!p.StandardOutput.EndOfStream)
                {
                    var line = await p.StandardOutput.ReadLineAsync();
                    if (line != null) await sw.WriteLineAsync(line);
                }
            });
        }
        
        for (var i = 0; i < 20 && !Running(); i++) System.Threading.Thread.Sleep(400);
        return Running() ? (true, $"Meilisearch started on :{Port}") : (false, "Meilisearch failed to start");
    }

    public static void Stop()
    {
        var exe = Tools.MeiliExe();
        if (exe is not null && Running())
        {
            var p = Process.GetProcessesByName("meilisearch").FirstOrDefault(p => { try { return p.MainModule?.FileName == exe; } catch { return false; } });
            if (p != null && !p.HasExited)
            {
                p.Kill();
            }
        }
    }
}
