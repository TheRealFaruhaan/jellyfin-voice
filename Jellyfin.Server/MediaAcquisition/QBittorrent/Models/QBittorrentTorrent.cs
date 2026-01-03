using System.Text.Json.Serialization;

namespace Jellyfin.Server.MediaAcquisition.QBittorrent.Models;

/// <summary>
/// Represents a torrent in qBittorrent.
/// </summary>
public class QBittorrentTorrent
{
    /// <summary>
    /// Gets or sets the torrent hash.
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the torrent name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the torrent size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the download progress (0-1).
    /// </summary>
    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    /// <summary>
    /// Gets or sets the download speed in bytes/second.
    /// </summary>
    [JsonPropertyName("dlspeed")]
    public long DownloadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the upload speed in bytes/second.
    /// </summary>
    [JsonPropertyName("upspeed")]
    public long UploadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the number of seeds connected.
    /// </summary>
    [JsonPropertyName("num_seeds")]
    public int Seeds { get; set; }

    /// <summary>
    /// Gets or sets the number of leechers connected.
    /// </summary>
    [JsonPropertyName("num_leechs")]
    public int Leechers { get; set; }

    /// <summary>
    /// Gets or sets the torrent state.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the save path.
    /// </summary>
    [JsonPropertyName("save_path")]
    public string SavePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content path (full path to downloaded content).
    /// </summary>
    [JsonPropertyName("content_path")]
    public string ContentPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount downloaded in bytes.
    /// </summary>
    [JsonPropertyName("downloaded")]
    public long Downloaded { get; set; }

    /// <summary>
    /// Gets or sets the ETA in seconds.
    /// </summary>
    [JsonPropertyName("eta")]
    public long Eta { get; set; }

    /// <summary>
    /// Gets or sets the magnet URI.
    /// </summary>
    [JsonPropertyName("magnet_uri")]
    public string MagnetUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time when the torrent was added (Unix timestamp).
    /// </summary>
    [JsonPropertyName("added_on")]
    public long AddedOn { get; set; }

    /// <summary>
    /// Gets or sets the completion time (Unix timestamp).
    /// </summary>
    [JsonPropertyName("completion_on")]
    public long CompletionOn { get; set; }
}
