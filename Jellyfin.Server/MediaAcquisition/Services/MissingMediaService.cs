using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Server.MediaAcquisition.Data;
using Jellyfin.Server.MediaAcquisition.Data.Entities;
using Jellyfin.Server.MediaAcquisition.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for detecting missing media in the library.
/// </summary>
public class MissingMediaService : IMissingMediaService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ITorrentDownloadRepository _downloadRepository;
    private readonly ILogger<MissingMediaService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingMediaService"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="downloadRepository">The download repository.</param>
    /// <param name="logger">The logger.</param>
    public MissingMediaService(
        ILibraryManager libraryManager,
        ITorrentDownloadRepository downloadRepository,
        ILogger<MissingMediaService> logger)
    {
        _libraryManager = libraryManager;
        _downloadRepository = downloadRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MissingEpisodeInfo>> GetMissingEpisodesAsync(Guid seriesId, CancellationToken cancellationToken = default)
    {
        var series = _libraryManager.GetItemById(seriesId) as Series;
        if (series == null)
        {
            _logger.LogWarning("Series not found: {SeriesId}", seriesId);
            return Array.Empty<MissingEpisodeInfo>();
        }

        var missingEpisodes = new List<MissingEpisodeInfo>();

        // Get all episodes for this series
        var episodes = series.GetRecursiveChildren()
            .OfType<Episode>()
            .ToList();

        // Find virtual (missing) episodes
        var virtualEpisodes = episodes
            .Where(e => e.LocationType == LocationType.Virtual)
            .Where(e => e.IndexNumber.HasValue && e.ParentIndexNumber.HasValue)
            .OrderBy(e => e.ParentIndexNumber)
            .ThenBy(e => e.IndexNumber)
            .ToList();

        // Get active downloads for this series
        var activeDownloads = await _downloadRepository.GetBySeriesAsync(seriesId, cancellationToken).ConfigureAwait(false);
        var downloadLookup = activeDownloads
            .Where(d => d.State != TorrentState.Error && d.State != TorrentState.Imported)
            .ToDictionary(d => (d.SeasonNumber ?? 0, d.EpisodeNumber ?? 0));

        foreach (var episode in virtualEpisodes)
        {
            var seasonNumber = episode.ParentIndexNumber!.Value;
            var episodeNumber = episode.IndexNumber!.Value;

            var info = new MissingEpisodeInfo
            {
                SeriesId = seriesId,
                SeriesName = series.Name,
                SeasonId = episode.SeasonId,
                SeasonNumber = seasonNumber,
                EpisodeId = episode.Id,
                EpisodeNumber = episodeNumber,
                EpisodeName = episode.Name,
                AirDate = episode.PremiereDate,
                Overview = episode.Overview,
                SeriesProviderIds = series.ProviderIds.ToDictionary(x => x.Key, x => x.Value)
            };

            // Check for active download
            if (downloadLookup.TryGetValue((seasonNumber, episodeNumber), out var download))
            {
                info.HasActiveDownload = true;
                info.ActiveDownloadId = download.Id;
            }

            missingEpisodes.Add(info);
        }

        _logger.LogDebug("Found {Count} missing episodes for series {SeriesName}", missingEpisodes.Count, series.Name);
        return missingEpisodes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MissingEpisodeInfo>> GetAllMissingEpisodesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var allMissing = new List<MissingEpisodeInfo>();

        // Get all series
        var seriesItems = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series },
            IsVirtualItem = false,
            Recursive = true
        });

        foreach (var seriesItem in seriesItems.OfType<Series>())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var missing = await GetMissingEpisodesAsync(seriesItem.Id, cancellationToken).ConfigureAwait(false);
            allMissing.AddRange(missing);

            if (allMissing.Count >= limit)
            {
                break;
            }
        }

        return allMissing
            .OrderByDescending(e => e.AirDate ?? DateTime.MinValue)
            .Take(limit)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MissingEpisodeInfo>> GetMissingEpisodesForSeasonAsync(Guid seriesId, int seasonNumber, CancellationToken cancellationToken = default)
    {
        var allMissing = await GetMissingEpisodesAsync(seriesId, cancellationToken).ConfigureAwait(false);
        return allMissing.Where(e => e.SeasonNumber == seasonNumber).ToList();
    }

    /// <inheritdoc />
    public Task<bool> IsEpisodeMissingAsync(Guid seriesId, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default)
    {
        var series = _libraryManager.GetItemById(seriesId) as Series;
        if (series == null)
        {
            return Task.FromResult(false);
        }

        var episodes = series.GetRecursiveChildren()
            .OfType<Episode>()
            .Where(e => e.ParentIndexNumber == seasonNumber && e.IndexNumber == episodeNumber)
            .ToList();

        // If no episodes found or all are virtual, it's missing
        return Task.FromResult(episodes.Count == 0 || episodes.All(e => e.LocationType == LocationType.Virtual));
    }
}
