using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Data.Entities;

namespace Jellyfin.Server.MediaAcquisition.Data;

/// <summary>
/// Repository interface for managing discovery favorites.
/// </summary>
public interface IDiscoveryFavoriteRepository
{
    /// <summary>
    /// Gets all favorites for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of user's favorites.</returns>
    Task<IReadOnlyList<DiscoveryFavorite>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a favorite by user ID and TMDB ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tmdbId">The TMDB ID.</param>
    /// <param name="mediaType">The media type (Movie or TvShow).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The favorite if found.</returns>
    Task<DiscoveryFavorite?> GetByUserAndTmdbIdAsync(Guid userId, int tmdbId, string mediaType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an item is favorited by a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tmdbId">The TMDB ID.</param>
    /// <param name="mediaType">The media type (Movie or TvShow).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if favorited.</returns>
    Task<bool> IsFavoritedAsync(Guid userId, int tmdbId, string mediaType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets favorite status for multiple TMDB IDs.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tmdbIds">The TMDB IDs to check.</param>
    /// <param name="mediaType">The media type (Movie or TvShow).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Dictionary mapping TMDB ID to favorite status.</returns>
    Task<Dictionary<int, bool>> GetFavoriteStatusAsync(Guid userId, IEnumerable<int> tmdbIds, string mediaType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a favorite.
    /// </summary>
    /// <param name="favorite">The favorite to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(DiscoveryFavorite favorite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a favorite.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tmdbId">The TMDB ID.</param>
    /// <param name="mediaType">The media type (Movie or TvShow).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if removed.</returns>
    Task<bool> RemoveAsync(Guid userId, int tmdbId, string mediaType, CancellationToken cancellationToken = default);
}
