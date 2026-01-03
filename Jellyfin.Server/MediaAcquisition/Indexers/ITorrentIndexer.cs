using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.MediaAcquisition.Indexers;

/// <summary>
/// Interface for torrent indexer implementations.
/// </summary>
public interface ITorrentIndexer
{
    /// <summary>
    /// Gets the name of the indexer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the indexer is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the priority of the indexer (lower is higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Searches for TV episode torrents.
    /// </summary>
    /// <param name="seriesName">The series name.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="providerIds">Optional provider IDs (TVDB, TMDB, IMDB).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of search results.</returns>
    Task<IEnumerable<TorrentSearchResult>> SearchEpisodeAsync(
        string seriesName,
        int seasonNumber,
        int episodeNumber,
        IDictionary<string, string>? providerIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for movie torrents.
    /// </summary>
    /// <param name="movieName">The movie name.</param>
    /// <param name="year">The release year.</param>
    /// <param name="providerIds">Optional provider IDs (TMDB, IMDB).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of search results.</returns>
    Task<IEnumerable<TorrentSearchResult>> SearchMovieAsync(
        string movieName,
        int? year = null,
        IDictionary<string, string>? providerIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the indexer.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the connection is successful.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
