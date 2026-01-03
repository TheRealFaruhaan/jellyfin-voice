using System;
using System.Collections.Generic;

namespace Jellyfin.Server.MediaAcquisition.Models;

/// <summary>
/// Information about a missing movie (from watchlist or wanted list).
/// </summary>
public class MissingMovieInfo
{
    /// <summary>
    /// Gets or sets the movie ID (if virtual item exists).
    /// </summary>
    public Guid? MovieId { get; set; }

    /// <summary>
    /// Gets or sets the movie name.
    /// </summary>
    public string MovieName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the overview.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets the provider IDs.
    /// </summary>
    public Dictionary<string, string> ProviderIds { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this movie has an active download.
    /// </summary>
    public bool HasActiveDownload { get; set; }

    /// <summary>
    /// Gets or sets the active download ID if any.
    /// </summary>
    public Guid? ActiveDownloadId { get; set; }

    /// <summary>
    /// Gets or sets the poster image URL.
    /// </summary>
    public string? PosterUrl { get; set; }

    /// <summary>
    /// Gets the display string for the movie.
    /// </summary>
    public string DisplayName => Year.HasValue ? $"{MovieName} ({Year})" : MovieName;
}
