using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Server.MediaAcquisition.QBittorrent.Models;

/// <summary>
/// Represents the main data response from qBittorrent sync endpoint.
/// </summary>
public class QBittorrentMainData
{
    /// <summary>
    /// Gets or sets the response ID.
    /// </summary>
    [JsonPropertyName("rid")]
    public int Rid { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether full update is needed.
    /// </summary>
    [JsonPropertyName("full_update")]
    public bool FullUpdate { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of torrents.
    /// </summary>
    [JsonPropertyName("torrents")]
    public Dictionary<string, QBittorrentTorrent>? Torrents { get; set; }

    /// <summary>
    /// Gets or sets the list of removed torrent hashes.
    /// </summary>
    [JsonPropertyName("torrents_removed")]
    public List<string>? TorrentsRemoved { get; set; }

    /// <summary>
    /// Gets or sets the server state.
    /// </summary>
    [JsonPropertyName("server_state")]
    public QBittorrentServerState? ServerState { get; set; }
}

/// <summary>
/// Represents the server state from qBittorrent.
/// </summary>
public class QBittorrentServerState
{
    /// <summary>
    /// Gets or sets the global download speed.
    /// </summary>
    [JsonPropertyName("dl_info_speed")]
    public long DownloadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the global upload speed.
    /// </summary>
    [JsonPropertyName("up_info_speed")]
    public long UploadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the total data downloaded.
    /// </summary>
    [JsonPropertyName("dl_info_data")]
    public long DownloadedData { get; set; }

    /// <summary>
    /// Gets or sets the total data uploaded.
    /// </summary>
    [JsonPropertyName("up_info_data")]
    public long UploadedData { get; set; }

    /// <summary>
    /// Gets or sets the free space on disk.
    /// </summary>
    [JsonPropertyName("free_space_on_disk")]
    public long FreeSpaceOnDisk { get; set; }
}
