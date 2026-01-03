using System.Text.Json.Serialization;

namespace Jellyfin.Server.MediaAcquisition.QBittorrent.Models;

/// <summary>
/// Represents a file within a torrent in qBittorrent.
/// </summary>
public class QBittorrentFile
{
    /// <summary>
    /// Gets or sets the file index.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the file name (relative path within torrent).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the download progress (0-1).
    /// </summary>
    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    /// <summary>
    /// Gets or sets the file priority (0=skip, 1=normal, 6=high, 7=max).
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file is a seed.
    /// </summary>
    [JsonPropertyName("is_seed")]
    public bool IsSeed { get; set; }

    /// <summary>
    /// Gets or sets the availability of the file.
    /// </summary>
    [JsonPropertyName("availability")]
    public double Availability { get; set; }
}
