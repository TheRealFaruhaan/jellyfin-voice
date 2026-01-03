using System;

namespace Jellyfin.Server.MediaAcquisition.Indexers;

/// <summary>
/// Represents a search result from a torrent indexer.
/// </summary>
public class TorrentSearchResult
{
    /// <summary>
    /// Gets or sets the title of the torrent.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the magnet link.
    /// </summary>
    public string MagnetLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the info hash.
    /// </summary>
    public string? InfoHash { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the number of seeders.
    /// </summary>
    public int Seeders { get; set; }

    /// <summary>
    /// Gets or sets the number of leechers.
    /// </summary>
    public int Leechers { get; set; }

    /// <summary>
    /// Gets or sets the quality (e.g., "1080p", "720p", "2160p").
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the source (e.g., "BluRay", "WEB-DL", "HDTV").
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the codec (e.g., "x264", "x265", "HEVC").
    /// </summary>
    public string? Codec { get; set; }

    /// <summary>
    /// Gets or sets the name of the indexer that provided this result.
    /// </summary>
    public string IndexerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the publish date of the torrent.
    /// </summary>
    public DateTime? PublishDate { get; set; }

    /// <summary>
    /// Gets or sets the download URL (for .torrent file).
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Gets or sets the details/info page URL.
    /// </summary>
    public string? DetailsUrl { get; set; }

    /// <summary>
    /// Gets or sets the category of the torrent.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets the formatted size string.
    /// </summary>
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
