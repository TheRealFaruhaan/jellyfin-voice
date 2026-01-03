using System.Collections.Generic;

namespace Jellyfin.Server.MediaAcquisition.Configuration;

/// <summary>
/// Configuration options for the Media Acquisition module.
/// </summary>
public class MediaAcquisitionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the Media Acquisition feature is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the qBittorrent Web UI URL.
    /// </summary>
    public string QBittorrentUrl { get; set; } = "http://localhost:8080";

    /// <summary>
    /// Gets or sets the qBittorrent username.
    /// </summary>
    public string QBittorrentUsername { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the qBittorrent password.
    /// </summary>
    public string QBittorrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default save path for downloads.
    /// </summary>
    public string DefaultSavePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether completed downloads should be automatically imported.
    /// </summary>
    public bool AutoImportEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the polling interval in seconds for checking download progress.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the category name to use in qBittorrent for Jellyfin downloads.
    /// </summary>
    public string TorrentCategory { get; set; } = "jellyfin";

    /// <summary>
    /// Gets or sets the list of configured torrent indexers.
    /// </summary>
    public List<TorrentIndexerConfig> Indexers { get; set; } = new();
}

/// <summary>
/// Configuration for a torrent indexer.
/// </summary>
public class TorrentIndexerConfig
{
    /// <summary>
    /// Gets or sets the name of the indexer.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the indexer (e.g., "Prowlarr", "Jackett", "Torznab").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL of the indexer.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for the indexer.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this indexer is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority of this indexer (lower is higher priority).
    /// </summary>
    public int Priority { get; set; } = 50;
}
