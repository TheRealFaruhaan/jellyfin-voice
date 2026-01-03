using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Server.MediaAcquisition.Models;

/// <summary>
/// Request to start an episode download.
/// </summary>
public class StartEpisodeDownloadRequest
{
    /// <summary>
    /// Gets or sets the series ID.
    /// </summary>
    [Required]
    public Guid SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [Required]
    [Range(0, 100)]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the magnet link.
    /// </summary>
    [Required]
    public string MagnetLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the torrent title.
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

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
    /// Gets or sets the quality.
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the indexer name.
    /// </summary>
    public string? IndexerName { get; set; }
}

/// <summary>
/// Request to start a movie download.
/// </summary>
public class StartMovieDownloadRequest
{
    /// <summary>
    /// Gets or sets the movie ID.
    /// </summary>
    [Required]
    public Guid MovieId { get; set; }

    /// <summary>
    /// Gets or sets the magnet link.
    /// </summary>
    [Required]
    public string MagnetLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the torrent title.
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

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
    /// Gets or sets the quality.
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the indexer name.
    /// </summary>
    public string? IndexerName { get; set; }
}

/// <summary>
/// Request to search for episode torrents.
/// </summary>
public class SearchEpisodeRequest
{
    /// <summary>
    /// Gets or sets the series ID.
    /// </summary>
    [Required]
    public Guid SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [Required]
    [Range(0, 100)]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public int EpisodeNumber { get; set; }
}

/// <summary>
/// Request to search for movie torrents.
/// </summary>
public class SearchMovieRequest
{
    /// <summary>
    /// Gets or sets the movie ID.
    /// </summary>
    public Guid? MovieId { get; set; }

    /// <summary>
    /// Gets or sets the movie name (for manual search).
    /// </summary>
    public string? MovieName { get; set; }

    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    public int? Year { get; set; }
}
