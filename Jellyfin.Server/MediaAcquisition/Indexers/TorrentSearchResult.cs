using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Server.MediaAcquisition.Indexers;

/// <summary>
/// Represents a search result from a torrent indexer.
/// </summary>
public class TorrentSearchResult
{
    /// <summary>
    /// Gets or sets the title of the torrent.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the magnet link.
    /// </summary>
    [JsonPropertyName("magnetLink")]
    public string MagnetLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the info hash.
    /// </summary>
    [JsonPropertyName("infoHash")]
    public string? InfoHash { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the number of seeders.
    /// </summary>
    [JsonPropertyName("seeders")]
    public int Seeders { get; set; }

    /// <summary>
    /// Gets or sets the number of leechers.
    /// </summary>
    [JsonPropertyName("leechers")]
    public int Leechers { get; set; }

    /// <summary>
    /// Gets or sets the quality (e.g., "1080p", "720p", "2160p").
    /// </summary>
    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the source (e.g., "BluRay", "WEB-DL", "HDTV").
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the codec (e.g., "x264", "x265", "HEVC").
    /// </summary>
    [JsonPropertyName("codec")]
    public string? Codec { get; set; }

    /// <summary>
    /// Gets or sets the name of the indexer that provided this result.
    /// </summary>
    [JsonPropertyName("indexerName")]
    public string IndexerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the publish date of the torrent.
    /// </summary>
    [JsonPropertyName("publishDate")]
    public DateTime? PublishDate { get; set; }

    /// <summary>
    /// Gets or sets the download URL (for .torrent file).
    /// </summary>
    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Gets or sets the details/info page URL.
    /// </summary>
    [JsonPropertyName("detailsUrl")]
    public string? DetailsUrl { get; set; }

    /// <summary>
    /// Gets or sets the category of the torrent.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Gets the formatted size string.
    /// </summary>
    [JsonPropertyName("formattedSize")]
    public string FormattedSize => FormatSize(Size);

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
