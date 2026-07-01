namespace BanglaHost.Core;

/// <summary>
/// A persisted site row — the SQLite <c>Sites</c> table record. This is the durable
/// catalog the Sites page binds to; the engine (nginx vhost + php + hosts) provisions
/// the actual running site, and this table is the source of truth for what exists.
/// </summary>
public sealed class SiteRecord
{
    public long Id { get; set; }
    public string SiteName { get; set; } = "";
    public string PhpVersion { get; set; } = "";
    public string Cms { get; set; } = "";
    public string WebServer { get; set; } = "";
    public bool Https { get; set; }
    /// <summary>ISO-8601 round-trip timestamp (UTC-agnostic local), written on insert.</summary>
    public string CreatedAt { get; set; } = "";
}
