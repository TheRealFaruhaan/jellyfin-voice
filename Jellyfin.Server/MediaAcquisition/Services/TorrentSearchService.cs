using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Indexers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for searching torrents across configured indexers.
/// </summary>
public class TorrentSearchService : ITorrentSearchService
{
    private readonly IEnumerable<ITorrentIndexer> _indexers;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<TorrentSearchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TorrentSearchService"/> class.
    /// </summary>
    /// <param name="indexers">The configured indexers.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public TorrentSearchService(
        IEnumerable<ITorrentIndexer> indexers,
        ILibraryManager libraryManager,
        ILogger<TorrentSearchService> logger)
    {
        _indexers = indexers;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentSearchResult>> SearchEpisodeAsync(
        Guid seriesId,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken = default)
    {
        var series = _libraryManager.GetItemById(seriesId) as Series;
        if (series == null)
        {
            _logger.LogWarning("Series not found: {SeriesId}", seriesId);
            return Array.Empty<TorrentSearchResult>();
        }

        var seriesName = series.Name;
        var providerIds = series.ProviderIds.ToDictionary(x => x.Key, x => x.Value);

        _logger.LogInformation(
            "Searching for episode: {Series} S{Season:D2}E{Episode:D2}",
            seriesName, seasonNumber, episodeNumber);

        var allResults = new List<TorrentSearchResult>();
        var enabledIndexers = _indexers.Where(i => i.IsEnabled).OrderBy(i => i.Priority);

        var searchTasks = enabledIndexers.Select(async indexer =>
        {
            try
            {
                var results = await indexer.SearchEpisodeAsync(
                    seriesName,
                    seasonNumber,
                    episodeNumber,
                    providerIds,
                    cancellationToken).ConfigureAwait(false);

                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search indexer {Indexer}", indexer.Name);
                return new List<TorrentSearchResult>();
            }
        });

        var results = await Task.WhenAll(searchTasks).ConfigureAwait(false);

        foreach (var resultSet in results)
        {
            allResults.AddRange(resultSet);
        }

        // Sort by seeders (descending) and remove duplicates by info hash
        var sortedResults = allResults
            .GroupBy(r => r.InfoHash ?? r.MagnetLink)
            .Select(g => g.OrderByDescending(r => r.Seeders).First())
            .OrderByDescending(r => r.Seeders)
            .ToList();

        _logger.LogInformation(
            "Found {Count} unique results for {Series} S{Season:D2}E{Episode:D2}",
            sortedResults.Count, seriesName, seasonNumber, episodeNumber);

        return sortedResults;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentSearchResult>> SearchMovieAsync(
        Guid movieId,
        CancellationToken cancellationToken = default)
    {
        var movie = _libraryManager.GetItemById(movieId) as Movie;
        if (movie == null)
        {
            _logger.LogWarning("Movie not found: {MovieId}", movieId);
            return Array.Empty<TorrentSearchResult>();
        }

        var movieName = movie.Name;
        var year = movie.ProductionYear;
        var providerIds = movie.ProviderIds.ToDictionary(x => x.Key, x => x.Value);

        _logger.LogInformation("Searching for movie: {Movie} ({Year})", movieName, year);

        var allResults = new List<TorrentSearchResult>();
        var enabledIndexers = _indexers.Where(i => i.IsEnabled).OrderBy(i => i.Priority);

        var searchTasks = enabledIndexers.Select(async indexer =>
        {
            try
            {
                var results = await indexer.SearchMovieAsync(
                    movieName,
                    year,
                    providerIds,
                    cancellationToken).ConfigureAwait(false);

                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search indexer {Indexer}", indexer.Name);
                return new List<TorrentSearchResult>();
            }
        });

        var results = await Task.WhenAll(searchTasks).ConfigureAwait(false);

        foreach (var resultSet in results)
        {
            allResults.AddRange(resultSet);
        }

        // Sort by seeders (descending) and remove duplicates
        var sortedResults = allResults
            .GroupBy(r => r.InfoHash ?? r.MagnetLink)
            .Select(g => g.OrderByDescending(r => r.Seeders).First())
            .OrderByDescending(r => r.Seeders)
            .ToList();

        _logger.LogInformation("Found {Count} unique results for {Movie}", sortedResults.Count, movieName);

        return sortedResults;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentSearchResult>> SearchMovieByNameAsync(
        string movieName,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching for movie by name: {Movie} ({Year})", movieName, year);

        var allResults = new List<TorrentSearchResult>();
        var enabledIndexers = _indexers.Where(i => i.IsEnabled).OrderBy(i => i.Priority);

        var searchTasks = enabledIndexers.Select(async indexer =>
        {
            try
            {
                var results = await indexer.SearchMovieAsync(
                    movieName,
                    year,
                    null,
                    cancellationToken).ConfigureAwait(false);

                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search indexer {Indexer}", indexer.Name);
                return new List<TorrentSearchResult>();
            }
        });

        var results = await Task.WhenAll(searchTasks).ConfigureAwait(false);

        foreach (var resultSet in results)
        {
            allResults.AddRange(resultSet);
        }

        return allResults
            .GroupBy(r => r.InfoHash ?? r.MagnetLink)
            .Select(g => g.OrderByDescending(r => r.Seeders).First())
            .OrderByDescending(r => r.Seeders)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, bool>> TestIndexersAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();

        foreach (var indexer in _indexers)
        {
            try
            {
                var isConnected = await indexer.TestConnectionAsync(cancellationToken).ConfigureAwait(false);
                results[indexer.Name] = isConnected;
            }
            catch
            {
                results[indexer.Name] = false;
            }
        }

        return results;
    }
}
