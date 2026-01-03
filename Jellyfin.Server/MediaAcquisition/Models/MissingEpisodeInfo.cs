using System;
using System.Collections.Generic;

namespace Jellyfin.Server.MediaAcquisition.Models;

/// <summary>
/// Information about a missing TV episode.
/// </summary>
public class MissingEpisodeInfo
{
    /// <summary>
    /// Gets or sets the series ID.
    /// </summary>
    public Guid SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the series name.
    /// </summary>
    public string SeriesName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season ID.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode ID (if virtual episode exists).
    /// </summary>
    public Guid? EpisodeId { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode name.
    /// </summary>
    public string? EpisodeName { get; set; }

    /// <summary>
    /// Gets or sets the air date.
    /// </summary>
    public DateTime? AirDate { get; set; }

    /// <summary>
    /// Gets or sets the overview.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets the provider IDs for the series.
    /// </summary>
    public Dictionary<string, string> SeriesProviderIds { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this episode has an active download.
    /// </summary>
    public bool HasActiveDownload { get; set; }

    /// <summary>
    /// Gets or sets the active download ID if any.
    /// </summary>
    public Guid? ActiveDownloadId { get; set; }

    /// <summary>
    /// Gets the display string for the episode (e.g., "S01E05").
    /// </summary>
    public string EpisodeCode => $"S{SeasonNumber:D2}E{EpisodeNumber:D2}";
}
