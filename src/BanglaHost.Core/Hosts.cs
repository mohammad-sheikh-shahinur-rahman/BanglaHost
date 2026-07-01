using System.Security.Principal;
using System.Text.RegularExpressions;

namespace BanglaHost.Core;

/// <summary>
/// Windows hosts-file management (the analog of mac dnsmasq/resolver, since the
/// Windows hosts file can't do wildcards). Each managed line is tagged so we can
/// add/remove cleanly. Writing requires Administrator — callers check
/// <see cref="IsElevated"/> and surface an elevation hint when false.
/// </summary>
public static class Hosts
{
    private const string Tag = "# BanglaHost";
    private static readonly string[] LocalIps = { "127.0.0.1", "::1", "0.0.0.0" };

    /// <summary>A strict hostname: lowercase labels of [a-z0-9-] joined by dots, no leading/trailing
    /// hyphen, max 253 chars. Critically rejects whitespace/newlines so the value can never inject
    /// extra lines into the hosts file (this code runs elevated).</summary>
    public static bool IsValidDomain(string domain) =>
        !string.IsNullOrEmpty(domain) && domain.Length <= 253 &&
        Regex.IsMatch(domain, @"^(?=.{1,253}$)([a-z0-9](-?[a-z0-9])*)(\.[a-z0-9](-?[a-z0-9])*)+$");

    public static bool IsElevated()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    /// <summary>True if the hosts file maps <paramref name="domain"/> to localhost (any tool, tagged or not).</summary>
    public static bool IsMapped(string domain)
    {
        if (!IsValidDomain(domain)) return false;
        try
        {
            if (!File.Exists(Paths.HostsFile)) return false;
            foreach (var line in File.ReadLines(Paths.HostsFile))
            {
                var active = line.Split('#')[0].Trim();
                if (active.Length == 0) continue;
                var parts = active.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                if (!LocalIps.Contains(parts[0])) continue;
                for (var i = 1; i < parts.Length; i++)
                    if (string.Equals(parts[i], domain, StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            return false;
        }
        catch { return false; }
    }

    public static bool Has(string domain) => IsMapped(domain);

    /// <summary>Append "127.0.0.1 domain # BanglaHost" if absent. Returns false (no-throw) when not elevated.</summary>
    public static bool Add(string domain)
    {
        if (!IsValidDomain(domain)) return false;   // never write unvalidated input to the hosts file
        if (IsMapped(domain)) return true;
        if (!IsElevated()) return false;
        try
        {
            File.AppendAllText(Paths.HostsFile, $"127.0.0.1 {domain} {Tag}{Environment.NewLine}");
            return IsMapped(domain);
        }
        catch { return false; }
    }

    /// <summary>Remove our tagged line for a domain. Returns false when not elevated.</summary>
    public static bool Remove(string domain)
    {
        if (!IsValidDomain(domain)) return false;
        if (!IsMapped(domain)) return true;
        if (!IsElevated()) return false;
        try
        {
            var kept = File.ReadAllLines(Paths.HostsFile)
                           .Where(l => !(l.Contains(Tag) && l.Contains(domain, StringComparison.OrdinalIgnoreCase)));
            File.WriteAllLines(Paths.HostsFile, kept);
            return true;
        }
        catch { return false; }
    }

    /// <summary>The line users can paste into the hosts file when automatic elevation fails.</summary>
    public static string ManualLine(string domain) => $"127.0.0.1 {domain} {Tag}";
}
