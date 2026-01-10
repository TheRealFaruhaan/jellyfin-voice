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
    public Task<TorrentDownload> StartDiscoveryMovieDownloadAsync(
        TorrentSearchResult torrent,
        int tmdbId,
        string movieTitle,
        int? year,
        Guid userId,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(DisabledMessage);

    /// <inheritdoc />
    public Task<TorrentDownload> StartDiscoveryEpisodeDownloadAsync(
        TorrentSearchResult torrent,
        int tmdbId,
        string showName,
        int seasonNumber,
        int episodeNumber,
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

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentSearchResult>> SearchSeasonByNameAsync(
        string seriesName,
        int seasonNumber,
        IDictionary<string, string>? providerIds = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyResults);

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentSearchResult>> SearchEpisodeByNameAsync(
        string seriesName,
        int seasonNumber,
        int episodeNumber,
        IDictionary<string, string>? providerIds = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyResults);

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentSearchResult>> SearchByQueryAsync(
        string query,
        string category = "movie",
        CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyResults);

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentSearchResult>> SearchByPatternsAsync(
        IEnumerable<string> patterns,
        string category = "movie",
        CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyResults);
}

/// <summary>
/// Disabled implementation of <see cref="IDiscoveryService"/> when Media Acquisition is disabled.
/// </summary>
public class DisabledDiscoveryService : IDiscoveryService
{
    private static readonly DiscoveryPagedResultDto<DiscoveryMovieDto> EmptyMovies = new();
    private static readonly DiscoveryPagedResultDto<DiscoveryTvShowDto> EmptyTvShows = new();

    /// <inheritdoc />
    public Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> GetTrendingMoviesAsync(int page = 1, Guid userId = default, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyMovies);

    /// <inheritdoc />
    public Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> GetPopularMoviesAsync(int page = 1, Guid userId = default, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyMovies);

    /// <inheritdoc />
    public Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> SearchMoviesAsync(string query, int? year = null, int page = 1, Guid userId = default, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyMovies);

    /// <inheritdoc />
    public Task<DiscoveryMovieDto?> GetMovieDetailsAsync(int tmdbId, Guid userId = default, CancellationToken cancellationToken = default)
        => Task.FromResult<DiscoveryMovieDto?>(null);

    /// <inheritdoc />
    public Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> GetTrendingTvShowsAsync(int page = 1, Guid userId = default, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyTvShows);

    /// <inheritdoc />
    public Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> GetPopularTvShowsAsync(int page = 1, Guid userId = default, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyTvShows);

    /// <inheritdoc />
    public Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> SearchTvShowsAsync(string query, int? year = null, int page = 1, Guid userId = default, CancellationToken cancellationToken = default)
        => Task.FromResult(EmptyTvShows);

    /// <inheritdoc />
    public Task<DiscoveryTvShowDto?> GetTvShowDetailsAsync(int tmdbId, Guid userId = default, CancellationToken cancellationToken = default)
        => Task.FromResult<DiscoveryTvShowDto?>(null);

    /// <inheritdoc />
    public Task<DiscoverySeasonDto?> GetSeasonDetailsAsync(int tmdbId, int seasonNumber, CancellationToken cancellationToken = default)
        => Task.FromResult<DiscoverySeasonDto?>(null);

    /// <inheritdoc />
    public Task<DiscoveryEpisodeDto?> GetEpisodeDetailsAsync(int tmdbId, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default)
        => Task.FromResult<DiscoveryEpisodeDto?>(null);

    /// <inheritdoc />
    public string? GetImageUrl(string? path, string size = "w500")
        => null;
}

/// <summary>
/// Disabled implementation of <see cref="ILibraryPathResolver"/> when Media Acquisition is disabled.
/// </summary>
public class DisabledLibraryPathResolver : ILibraryPathResolver
{
    /// <inheritdoc />
    public string? GetMovieDownloadPath(string movieTitle, int? year = null)
        => null;

    /// <inheritdoc />
    public string? GetTvShowDownloadPath(string showName, int seasonNumber)
        => null;

    /// <inheritdoc />
    public string? GetMoviesLibraryPath()
        => null;

    /// <inheritdoc />
    public string? GetTvShowsLibraryPath()
        => null;

    /// <inheritdoc />
    public DiskSpaceDto GetDiskSpace(string path, long requiredBytes = 0)
        => new DiskSpaceDto { FreeSpaceBytes = -1, FormattedFreeSpace = "Unknown", HasEnoughSpace = false };

    /// <inheritdoc />
    public bool HasEnoughDiskSpace(string path, long requiredBytes)
        => false;
}
