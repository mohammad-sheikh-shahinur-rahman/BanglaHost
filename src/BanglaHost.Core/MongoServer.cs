using System.Diagnostics;
using System.Net.Sockets;

namespace BanglaHost.Core;

public static class MongoServer
{
    public const int Port = 27017;
    private static string DataDir => Path.Combine(Paths.Home, "mongodata");
    private static string LogFile => Path.Combine(Paths.Logs, "mongodb.log");

    public static bool Running()
    {
        try { using var c = new TcpClient(); return c.ConnectAsync("127.0.0.1", Port).Wait(600) && c.Connected; }
        catch { return false; }
    }

    public static (bool ok, string msg) Start()
    {
        if (Running()) return (true, "MongoDB already running");
        var exe = Tools.MongoExe();
        if (exe is null) return (false, "mongod.exe not found — install MongoDB");
        
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(Paths.Logs);

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"--dbpath \"{DataDir}\" --logpath \"{LogFile}\" --bind_ip 127.0.0.1 --port {Port}",
            UseShellExecute = false, CreateNoWindow = true
        };
        Process.Start(psi);
        
        for (var i = 0; i < 20 && !Running(); i++) System.Threading.Thread.Sleep(400);
        return Running() ? (true, $"MongoDB started on :{Port}") : (false, "MongoDB failed to start (check mongodb.log)");
    }

    public static void Stop()
    {
        var exe = Tools.MongoExe();
        if (exe is not null && Running())
        {
            var p = Process.GetProcessesByName("mongod").FirstOrDefault(p => { try { return p.MainModule?.FileName == exe; } catch { return false; } });
            if (p != null && !p.HasExited)
            {
                p.Kill();
            }
        }
    }
}
