using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Models;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for detecting missing media in the library.
/// </summary>
public interface IMissingMediaService
{
    /// <summary>
    /// Gets missing episodes for a specific series.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of missing episodes.</returns>
    Task<IReadOnlyList<MissingEpisodeInfo>> GetMissingEpisodesAsync(Guid seriesId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all missing episodes across all series.
    /// </summary>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of missing episodes.</returns>
    Task<IReadOnlyList<MissingEpisodeInfo>> GetAllMissingEpisodesAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets missing episodes for a specific season.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of missing episodes.</returns>
    Task<IReadOnlyList<MissingEpisodeInfo>> GetMissingEpisodesForSeasonAsync(Guid seriesId, int seasonNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific episode is missing.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the episode is missing.</returns>
    Task<bool> IsEpisodeMissingAsync(Guid seriesId, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default);
}
