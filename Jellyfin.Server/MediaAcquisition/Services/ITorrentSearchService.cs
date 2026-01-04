using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Indexers;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for searching torrents across configured indexers.
/// </summary>
public interface ITorrentSearchService
{
    /// <summary>
    /// Searches for episode torrents.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of search results from all indexers.</returns>
    Task<IReadOnlyList<TorrentSearchResult>> SearchEpisodeAsync(
        Guid seriesId,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for movie torrents.
    /// </summary>
    /// <param name="movieId">The movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of search results from all indexers.</returns>
    Task<IReadOnlyList<TorrentSearchResult>> SearchMovieAsync(
        Guid movieId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for movie torrents by name.
    /// </summary>
    /// <param name="movieName">The movie name.</param>
    /// <param name="year">The release year.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of search results from all indexers.</returns>
    Task<IReadOnlyList<TorrentSearchResult>> SearchMovieByNameAsync(
        string movieName,
        int? year = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests all configured indexers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Dictionary of indexer names and their connection status.</returns>
    Task<IDictionary<string, bool>> TestIndexersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for season torrents by name.
    /// </summary>
    /// <param name="seriesName">The series name.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="providerIds">Optional provider IDs for better matching.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of search results from all indexers.</returns>
    Task<IReadOnlyList<TorrentSearchResult>> SearchSeasonByNameAsync(
        string seriesName,
        int seasonNumber,
        IDictionary<string, string>? providerIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for episode torrents by name.
    /// </summary>
    /// <param name="seriesName">The series name.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="providerIds">Optional provider IDs for better matching.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of search results from all indexers.</returns>
    Task<IReadOnlyList<TorrentSearchResult>> SearchEpisodeByNameAsync(
        string seriesName,
        int seasonNumber,
        int episodeNumber,
        IDictionary<string, string>? providerIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for torrents with a custom query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="category">The category ("movie" or "tv").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of search results from all indexers.</returns>
    Task<IReadOnlyList<TorrentSearchResult>> SearchByQueryAsync(
        string query,
        string category = "movie",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for torrents using multiple patterns and aggregates results.
    /// </summary>
    /// <param name="patterns">The search patterns to try.</param>
    /// <param name="category">The category ("movie" or "tv").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of unique search results from all patterns and indexers.</returns>
    Task<IReadOnlyList<TorrentSearchResult>> SearchByPatternsAsync(
        IEnumerable<string> patterns,
        string category = "movie",
        CancellationToken cancellationToken = default);
}
