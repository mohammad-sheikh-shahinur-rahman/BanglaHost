using System.Diagnostics;

namespace BanglaHost.Core;

/// <summary>nginx process lifecycle on Windows (start/stop/reload/test) — analog of the
/// mac engine's <c>nginx_start</c>/<c>nginx_stop</c>/<c>nginx_reload</c>.</summary>
public static class Nginx
{
    private static string NginxDir => Path.Combine(Paths.Home, "nginx");
    private static string ConfPath => Path.Combine(NginxDir, "nginx.conf");
    private static string PidFile  => Path.Combine(Paths.Run, "nginx.pid");

    public static bool Running()
    {
        // pid file is authoritative when valid, but it goes stale across restarts — so
        // fall back to "is any nginx.exe process alive?" (avoids a stale pid skipping reloads).
        try
        {
            if (File.Exists(PidFile) && int.TryParse(File.ReadAllText(PidFile).Trim(), out var pid))
            {
                using var p = Process.GetProcessById(pid);
                if (!p.HasExited) return true;
            }
        }
        catch { }
        try { return Process.GetProcessesByName("nginx").Length > 0; } catch { return false; }
    }

    private static (int code, string output) Run(string exe, string args, bool wait = true)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(exe)!,
        };
        var proc = Process.Start(psi)!;
        if (!wait)
            // Detached daemon: DON'T read the streams — ReadToEnd() would block until the
            // child exits (i.e. forever for nginx). Redirecting (above) is enough to keep
            // the daemon from inheriting the caller's console handle.
            return (0, "");
        var outp = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, outp);
    }

    /// <summary>nginx -t: returns (ok, message). Treats "syntax is ok" as success even if the bind probe fails.</summary>
    public static (bool ok, string msg) Test(string exe)
    {
        var (_, outp) = Run(exe, $"-t -p \"{NginxConfig.Fwd(NginxDir)}\" -c \"{NginxConfig.Fwd(ConfPath)}\"");
        return (outp.Contains("syntax is ok"), outp);
    }

    public static (bool ok, string msg) Start(Config cfg)
    {
        NginxConfig.RenderMain(cfg);
        var exe = Tools.NginxExe();
        if (exe is null) return (false, "nginx not installed — run: banglahost install nginx");
        if (Running()) return (true, "nginx already running");

        var (ok, msg) = Test(exe);
        if (!ok) return (false, "nginx config test failed:\n" + msg);

        // Launch detached. nginx daemonizes on Windows and writes its own pid file.
        Run(exe, $"-p \"{NginxConfig.Fwd(NginxDir)}\" -c \"{NginxConfig.Fwd(ConfPath)}\"", wait: false);
        // Poll for up to ~3s instead of a single fixed wait: a cold start (first launch, or AV
        // scanning nginx.exe) routinely takes longer than 400ms to spawn + write the pid file.
        // Declaring "failed" too early made reload callers roll back (delete) a perfectly good site.
        for (var i = 0; i < 30; i++) { if (Running()) return (true, "nginx started"); System.Threading.Thread.Sleep(100); }
        return (false, "nginx failed to start (see logs/nginx-error.log)");
    }

    /// <summary>Block until no nginx process remains (or the timeout elapses). Returns true once stopped.</summary>
    private static bool WaitUntilStopped(int timeoutMs)
    {
        for (var waited = 0; waited < timeoutMs && Running(); waited += 100)
            System.Threading.Thread.Sleep(100);
        return !Running();
    }

    public static void Stop()
    {
        var exe = Tools.NginxExe();
        if (exe is not null && Running())
            Run(exe, $"-s stop -p \"{NginxConfig.Fwd(NginxDir)}\" -c \"{NginxConfig.Fwd(ConfPath)}\"");
        // Fallback: kill by pid if -s stop didn't clear it.
        try
        {
            if (File.Exists(PidFile) && int.TryParse(File.ReadAllText(PidFile).Trim(), out var pid))
                Process.GetProcessById(pid).Kill(true);
        }
        catch { }
    }

    public static void Reload(Config cfg)
    {
        NginxConfig.RenderMain(cfg);
        var exe = Tools.NginxExe();
        if (exe is null || !Running()) return;

        var (ok, msg) = Test(exe);
        if (!ok) throw new BhException("nginx config test failed:\n" + msg);

        // On Windows, `nginx -s reload` is buggy and can crash the master process if it fails to bind
        // to a port (e.g. 443) or just due to socket reuse issues. A hard restart is safer and
        // guarantees the new config is applied cleanly for local dev servers.
        //
        // The old master/workers MUST be fully gone before we Start() again: if a straggler is still
        // alive, Start() short-circuits on "already running" and we'd silently keep serving the OLD
        // config (the just-added site never loads) — or end up with nothing running when that straggler
        // exits a moment later. So confirm a clean stop, and bail loudly if it won't die.
        Stop();
        if (!WaitUntilStopped(3000))
            throw new BhException("Nginx crashed during reload! A worker process is stuck and wouldn't shut down — close anything using it (or reboot) and try again.");

        var (sOk, _) = Start(cfg);
        if (!sOk)
            throw new BhException("Nginx crashed during reload! This usually happens if port 80 or 443 is being used by another program (like IIS, Skype, or VMWare). Check your ports and try again.");
    }
}
