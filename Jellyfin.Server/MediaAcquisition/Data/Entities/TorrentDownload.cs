using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Server.MediaAcquisition.Data.Entities;

/// <summary>
/// Represents a torrent download tracked by the Media Acquisition module.
/// </summary>
public class TorrentDownload
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the torrent hash from qBittorrent.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string TorrentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the torrent name.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the magnet link used to add the torrent.
    /// </summary>
    [MaxLength(4096)]
    public string MagnetLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of media being downloaded.
    /// </summary>
    public MediaType MediaType { get; set; }

    /// <summary>
    /// Gets or sets the series ID (for episodes).
    /// </summary>
    public Guid? SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the series name (for episodes).
    /// </summary>
    [MaxLength(256)]
    public string? SeriesName { get; set; }

    /// <summary>
    /// Gets or sets the season ID (for episodes).
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// Gets or sets the episode ID (for episodes, if known).
    /// </summary>
    public Guid? EpisodeId { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the movie ID (for movies).
    /// </summary>
    public Guid? MovieId { get; set; }

    /// <summary>
    /// Gets or sets the movie name (for movies).
    /// </summary>
    [MaxLength(256)]
    public string? MovieName { get; set; }

    /// <summary>
    /// Gets or sets the current state of the download.
    /// </summary>
    public TorrentState State { get; set; }

    /// <summary>
    /// Gets or sets the download progress (0-100).
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Gets or sets the total size in bytes.
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the downloaded size in bytes.
    /// </summary>
    public long DownloadedSize { get; set; }

    /// <summary>
    /// Gets or sets the download speed in bytes/second.
    /// </summary>
    public long DownloadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the upload speed in bytes/second.
    /// </summary>
    public long UploadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the number of seeders.
    /// </summary>
    public int Seeders { get; set; }

    /// <summary>
    /// Gets or sets the number of leechers.
    /// </summary>
    public int Leechers { get; set; }

    /// <summary>
    /// Gets or sets the save path for the download.
    /// </summary>
    [MaxLength(1024)]
    public string SavePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content path (full path to downloaded content).
    /// </summary>
    [MaxLength(1024)]
    public string? ContentPath { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the download was added.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the download completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the download was imported.
    /// </summary>
    public DateTime? ImportedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-import this download.
    /// </summary>
    public bool AutoImport { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if the download failed.
    /// </summary>
    [MaxLength(1024)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the quality of the torrent (e.g., "1080p", "720p").
    /// </summary>
    [MaxLength(64)]
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the indexer that provided the torrent.
    /// </summary>
    [MaxLength(128)]
    public string? IndexerName { get; set; }

    /// <summary>
    /// Gets or sets the ETA in seconds.
    /// </summary>
    public long? Eta { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the download.
    /// </summary>
    public Guid InitiatedByUserId { get; set; }
}
