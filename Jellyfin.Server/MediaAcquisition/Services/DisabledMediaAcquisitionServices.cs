using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Data.Entities;
using Jellyfin.Server.MediaAcquisition.Indexers;
using Jellyfin.Server.MediaAcquisition.Models;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Disabled implementation of <see cref="IMissingMediaService"/> when Media Acquisition is disabled.
/// </summary>
public class DisabledMissingMediaService : IMissingMediaService
{
    private static readonly IReadOnlyList<MissingEpisodeInfo> EmptyMissingEpisodes = Array.Empty<MissingEpisodeInfo>();

    /// <inheritdoc />
    public Task<IReadOnlyList<MissingEpisodeInfo>> GetMissingEpisodesAsync(Guid seriesId, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyMissingEpisodes);

    /// <inheritdoc />
    public Task<IReadOnlyList<MissingEpisodeInfo>> GetAllMissingEpisodesAsync(int limit = 100, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyMissingEpisodes);

    /// <inheritdoc />
    public Task<IReadOnlyList<MissingEpisodeInfo>> GetMissingEpisodesForSeasonAsync(Guid seriesId, int seasonNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyMissingEpisodes);

    /// <inheritdoc />
    public Task<bool> IsEpisodeMissingAsync(Guid seriesId, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}

/// <summary>
/// Disabled implementation of <see cref="IDownloadManagerService"/> when Media Acquisition is disabled.
/// </summary>
public class DisabledDownloadManagerService : IDownloadManagerService
{
    private const string DisabledMessage = "Media Acquisition feature is not enabled. Please configure it in appsettings.json.";
    private static readonly IReadOnlyList<TorrentDownload> EmptyDownloads = Array.Empty<TorrentDownload>();

    /// <inheritdoc />
    public Task<TorrentDownload> StartEpisodeDownloadAsync(
        TorrentSearchResult torrent,
        Guid seriesId,
        int seasonNumber,
        int episodeNumber,
        Guid userId,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(DisabledMessage);

    /// <inheritdoc />
    public Task<TorrentDownload> StartMovieDownloadAsync(
        TorrentSearchResult torrent,
        Guid movieId,
        Guid userId,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(DisabledMessage);

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentDownload>> GetAllDownloadsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyDownloads);

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentDownload>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyDownloads);

    /// <inheritdoc />
    public Task<TorrentDownload?> GetDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default)
        => Task.FromResult<TorrentDownload?>(null);

    /// <inheritdoc />
    public Task PauseDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(DisabledMessage);

    /// <inheritdoc />
    public Task ResumeDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(DisabledMessage);

    /// <inheritdoc />
    public Task CancelDownloadAsync(Guid downloadId, bool deleteFiles = false, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(DisabledMessage);

    /// <inheritdoc />
    public Task ImportDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(DisabledMessage);

    /// <inheritdoc />
    public Task<bool> GetConnectionStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}

/// <summary>
/// Disabled implementation of <see cref="ITorrentSearchService"/> when Media Acquisition is disabled.
/// </summary>
public class DisabledTorrentSearchService : ITorrentSearchService
{
    private static readonly IReadOnlyList<TorrentSearchResult> EmptyResults = Array.Empty<TorrentSearchResult>();
    private static readonly IDictionary<string, bool> EmptyIndexers = new Dictionary<string, bool>();

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentSearchResult>> SearchEpisodeAsync(
        Guid seriesId,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyResults);

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentSearchResult>> SearchMovieAsync(
        Guid movieId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyResults);

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentSearchResult>> SearchMovieByNameAsync(
        string movieName,
        int? year = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyResults);

    /// <inheritdoc />
    public Task<IDictionary<string, bool>> TestIndexersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyIndexers);
}
