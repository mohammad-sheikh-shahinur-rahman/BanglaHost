using System;
using System.IO;

namespace BanglaHost.Core;

/// <summary>
/// Tiny append-only logger for app-level events (site create/update/delete, database
/// errors). Writes timestamped lines to <c>logs\banglahost.log</c>. Never throws — a
/// logging failure must not take down the operation it was recording.
/// </summary>
public static class Log
{
    private static readonly object Gate = new();

    public static string LogFile => Path.Combine(Paths.Logs, "banglahost.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message} :: {ex.GetType().Name}: {ex.Message}");

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(Paths.Logs);
            lock (Gate)
                File.AppendAllText(LogFile,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}");
        }
        catch { /* logging must never throw */ }
    }
}
