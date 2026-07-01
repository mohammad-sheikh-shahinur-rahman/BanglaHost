using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace BanglaHost.Core;

/// <summary>
/// SQLite-backed persistence for the Sites catalog. Owns the <c>Sites</c> table
/// (auto-created on first use) and all CRUD. Every operation is wrapped in try/catch,
/// logged via <see cref="Log"/>, and surfaces a friendly <see cref="BhException"/> on
/// failure so the UI can show a meaningful message instead of crashing.
/// </summary>
public sealed class SitesRepository
{
    /// <summary>Process-wide default instance pointing at <c>%LOCALAPPDATA%\BanglaHost\banglahost.db</c>.</summary>
    public static SitesRepository Instance { get; } = new();

    private readonly string _connectionString;

    public string DatabasePath { get; }

    public SitesRepository() : this(Path.Combine(Paths.Home, "banglahost.db")) { }

    public SitesRepository(string databasePath)
    {
        DatabasePath = databasePath;
        try { Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!); } catch { }
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        EnsureCreated();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>Create the Sites table if it doesn't exist (idempotent). Safe to call at startup.</summary>
    public void EnsureCreated()
    {
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Sites (
                    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                    SiteName   TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                    PhpVersion TEXT    NOT NULL,
                    Cms        TEXT    NOT NULL,
                    WebServer  TEXT    NOT NULL,
                    Https      INTEGER NOT NULL,
                    CreatedAt  TEXT    NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Log.Error("DB: failed to create/verify Sites table", ex);
            throw new BhException("Could not initialize the sites database: " + ex.Message);
        }
    }

    /// <summary>All sites, ordered by name. Returns an empty list (never null) on any error.</summary>
    public List<SiteRecord> GetAll()
    {
        var list = new List<SiteRecord>();
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, SiteName, PhpVersion, Cms, WebServer, Https, CreatedAt FROM Sites ORDER BY SiteName COLLATE NOCASE;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }
        catch (Exception ex)
        {
            Log.Error("DB: failed to load sites", ex);
            throw new BhException("Could not load sites from the database: " + ex.Message);
        }
    }

    public SiteRecord? GetByName(string name)
    {
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, SiteName, PhpVersion, Cms, WebServer, Https, CreatedAt FROM Sites WHERE SiteName = $n COLLATE NOCASE LIMIT 1;";
            cmd.Parameters.AddWithValue("$n", name);
            using var r = cmd.ExecuteReader();
            return r.Read() ? Map(r) : null;
        }
        catch (Exception ex)
        {
            Log.Error($"DB: failed to look up site '{name}'", ex);
            throw new BhException("Could not query the database: " + ex.Message);
        }
    }

    /// <summary>True if a site with this name already exists (case-insensitive).</summary>
    public bool Exists(string name)
    {
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM Sites WHERE SiteName = $n COLLATE NOCASE;";
            cmd.Parameters.AddWithValue("$n", name);
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }
        catch (Exception ex)
        {
            Log.Error($"DB: failed to check existence of '{name}'", ex);
            throw new BhException("Could not query the database: " + ex.Message);
        }
    }

    /// <summary>Insert a new site. Validates the name and rejects duplicates. Returns the new Id.</summary>
    public long Add(SiteRecord s)
    {
        if (!IsValidSiteName(s.SiteName))
            throw new BhException($"Invalid site name '{s.SiteName}'. Use lowercase letters, digits, hyphens or dots.");
        if (string.IsNullOrWhiteSpace(s.CreatedAt))
            s.CreatedAt = DateTime.Now.ToString("o");

        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Sites (SiteName, PhpVersion, Cms, WebServer, Https, CreatedAt)
                VALUES ($name, $php, $cms, $web, $https, $created);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$name", s.SiteName);
            cmd.Parameters.AddWithValue("$php", s.PhpVersion ?? "");
            cmd.Parameters.AddWithValue("$cms", s.Cms ?? "");
            cmd.Parameters.AddWithValue("$web", s.WebServer ?? "");
            cmd.Parameters.AddWithValue("$https", s.Https ? 1 : 0);
            cmd.Parameters.AddWithValue("$created", s.CreatedAt);
            s.Id = Convert.ToInt64(cmd.ExecuteScalar());
            Log.Info($"DB: inserted site '{s.SiteName}' (id={s.Id})");
            return s.Id;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT (UNIQUE)
        {
            Log.Error($"DB: duplicate site name '{s.SiteName}'", ex);
            throw new BhException($"A site named '{s.SiteName}' already exists.");
        }
        catch (BhException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"DB: failed to insert site '{s.SiteName}'", ex);
            throw new BhException("Could not save the site to the database: " + ex.Message);
        }
    }

    /// <summary>Update an existing site's editable fields (matched by Id, falling back to SiteName).</summary>
    public void Update(SiteRecord s)
    {
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE Sites
                   SET PhpVersion = $php, Cms = $cms, WebServer = $web, Https = $https
                 WHERE Id = $id OR ($id = 0 AND SiteName = $name COLLATE NOCASE);
                """;
            cmd.Parameters.AddWithValue("$php", s.PhpVersion ?? "");
            cmd.Parameters.AddWithValue("$cms", s.Cms ?? "");
            cmd.Parameters.AddWithValue("$web", s.WebServer ?? "");
            cmd.Parameters.AddWithValue("$https", s.Https ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", s.Id);
            cmd.Parameters.AddWithValue("$name", s.SiteName);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) throw new BhException($"No site '{s.SiteName}' found to update.");
            Log.Info($"DB: updated site '{s.SiteName}' (id={s.Id})");
        }
        catch (BhException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"DB: failed to update site '{s.SiteName}'", ex);
            throw new BhException("Could not update the site in the database: " + ex.Message);
        }
    }

    /// <summary>Delete a site by name. No-op (logged) if it isn't present.</summary>
    public void Delete(string name)
    {
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Sites WHERE SiteName = $n COLLATE NOCASE;";
            cmd.Parameters.AddWithValue("$n", name);
            var rows = cmd.ExecuteNonQuery();
            Log.Info($"DB: deleted site '{name}' (rows={rows})");
        }
        catch (Exception ex)
        {
            Log.Error($"DB: failed to delete site '{name}'", ex);
            throw new BhException("Could not delete the site from the database: " + ex.Message);
        }
    }

    private static SiteRecord Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        SiteName = r.GetString(1),
        PhpVersion = r.GetString(2),
        Cms = r.GetString(3),
        WebServer = r.GetString(4),
        Https = r.GetInt64(5) != 0,
        CreatedAt = r.GetString(6),
    };

    /// <summary>Same rule the engine enforces for vhost names: lowercase alnum, with hyphens/dots
    /// allowed in the middle, starting and ending alphanumeric.</summary>
    public static bool IsValidSiteName(string name) =>
        !string.IsNullOrWhiteSpace(name) &&
        Regex.IsMatch(name, "^[a-z0-9]([a-z0-9.-]*[a-z0-9])?$");
}
