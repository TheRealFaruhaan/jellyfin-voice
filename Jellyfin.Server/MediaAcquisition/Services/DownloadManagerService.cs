using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Configuration;
using Jellyfin.Server.MediaAcquisition.Data;
using Jellyfin.Server.MediaAcquisition.Data.Entities;
using Jellyfin.Server.MediaAcquisition.Indexers;
using Jellyfin.Server.MediaAcquisition.QBittorrent;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for managing torrent downloads.
/// </summary>
public class DownloadManagerService : IDownloadManagerService
{
    private readonly IQBittorrentClient _qbClient;
    private readonly ITorrentDownloadRepository _repository;
    private readonly ILibraryManager _libraryManager;
    private readonly ILibraryPathResolver _pathResolver;
    private readonly ILogger<DownloadManagerService> _logger;
    private readonly MediaAcquisitionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadManagerService"/> class.
    /// </summary>
    /// <param name="qbClient">The qBittorrent client.</param>
    /// <param name="repository">The download repository.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="pathResolver">The library path resolver.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The configuration options.</param>
    public DownloadManagerService(
        IQBittorrentClient qbClient,
        ITorrentDownloadRepository repository,
        ILibraryManager libraryManager,
        ILibraryPathResolver pathResolver,
        ILogger<DownloadManagerService> logger,
        IOptions<MediaAcquisitionOptions> options)
    {
        _qbClient = qbClient;
        _repository = repository;
        _libraryManager = libraryManager;
        _pathResolver = pathResolver;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<TorrentDownload> StartEpisodeDownloadAsync(
        TorrentSearchResult torrent,
        Guid seriesId,
        int seasonNumber,
        int episodeNumber,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var series = _libraryManager.GetItemById(seriesId) as Series;
        if (series == null)
        {
            throw new ArgumentException($"Series not found: {seriesId}");
        }

        // Check if download already exists
        var exists = await _repository.ExistsForEpisodeAsync(seriesId, seasonNumber, episodeNumber, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException($"Download already exists for {series.Name} S{seasonNumber:D2}E{episodeNumber:D2}");
        }

        // Ensure category exists in qBittorrent
        await _qbClient.CreateCategoryAsync(_options.TorrentCategory, _options.DefaultSavePath, cancellationToken).ConfigureAwait(false);

        // Add torrent to qBittorrent
        var added = await _qbClient.AddTorrentAsync(
            torrent.MagnetLink,
            _options.DefaultSavePath,
            _options.TorrentCategory,
            cancellationToken).ConfigureAwait(false);

        if (!added)
        {
            throw new InvalidOperationException("Failed to add torrent to qBittorrent");
        }

        // Extract hash from magnet link
        var hash = ExtractHashFromMagnet(torrent.MagnetLink);

        // Create download record
        var download = new TorrentDownload
        {
            TorrentHash = hash,
            Name = torrent.Title,
            MagnetLink = torrent.MagnetLink,
            MediaType = MediaType.Episode,
            SeriesId = seriesId,
            SeriesName = series.Name,
            SeasonNumber = seasonNumber,
            EpisodeNumber = episodeNumber,
            State = TorrentState.Queued,
            TotalSize = torrent.Size,
            Seeders = torrent.Seeders,
            Leechers = torrent.Leechers,
            Quality = torrent.Quality,
            IndexerName = torrent.IndexerName,
            AutoImport = _options.AutoImportEnabled,
            InitiatedByUserId = userId
        };

        await _repository.AddAsync(download, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Started episode download: {Series} S{Season:D2}E{Episode:D2} - {Torrent}",
            series.Name, seasonNumber, episodeNumber, torrent.Title);

        return download;
    }

    /// <inheritdoc />
    public async Task<TorrentDownload> StartMovieDownloadAsync(
        TorrentSearchResult torrent,
        Guid movieId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var movie = _libraryManager.GetItemById(movieId) as Movie;
        if (movie == null)
        {
            throw new ArgumentException($"Movie not found: {movieId}");
        }

        // Check if download already exists
        var exists = await _repository.ExistsForMovieAsync(movieId, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException($"Download already exists for {movie.Name}");
        }

        // Ensure category exists in qBittorrent
        await _qbClient.CreateCategoryAsync(_options.TorrentCategory, _options.DefaultSavePath, cancellationToken).ConfigureAwait(false);

        // Add torrent to qBittorrent
        var added = await _qbClient.AddTorrentAsync(
            torrent.MagnetLink,
            _options.DefaultSavePath,
            _options.TorrentCategory,
            cancellationToken).ConfigureAwait(false);

        if (!added)
        {
            throw new InvalidOperationException("Failed to add torrent to qBittorrent");
        }

        // Extract hash from magnet link
        var hash = ExtractHashFromMagnet(torrent.MagnetLink);

        // Create download record
        var download = new TorrentDownload
        {
            TorrentHash = hash,
            Name = torrent.Title,
            MagnetLink = torrent.MagnetLink,
            MediaType = MediaType.Movie,
            MovieId = movieId,
            MovieName = movie.Name,
            State = TorrentState.Queued,
            TotalSize = torrent.Size,
            Seeders = torrent.Seeders,
            Leechers = torrent.Leechers,
            Quality = torrent.Quality,
            IndexerName = torrent.IndexerName,
            AutoImport = _options.AutoImportEnabled,
            InitiatedByUserId = userId
        };

        await _repository.AddAsync(download, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Started movie download: {Movie} - {Torrent}", movie.Name, torrent.Title);

        return download;
    }

    /// <inheritdoc />
    public async Task<TorrentDownload> StartDiscoveryMovieDownloadAsync(
        TorrentSearchResult torrent,
        int tmdbId,
        string movieTitle,
        int? year,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get the download path using the library path resolver
        var downloadPath = _pathResolver.GetMovieDownloadPath(movieTitle, year);
        if (string.IsNullOrEmpty(downloadPath))
        {
            throw new InvalidOperationException("No movies library configured. Please configure a movies library in Jellyfin settings.");
        }

        // Note: Don't create the directory here - qBittorrent will create it with proper permissions

        // Check disk space (use parent directory for check since target folder may not exist yet)
        if (!_pathResolver.HasEnoughDiskSpace(downloadPath, torrent.Size))
        {
            throw new InvalidOperationException("Insufficient disk space for download");
        }

        // Ensure category exists in qBittorrent
        await _qbClient.CreateCategoryAsync(_options.TorrentCategory, downloadPath, cancellationToken).ConfigureAwait(false);

        // Add torrent to qBittorrent with the library path
        var added = await _qbClient.AddTorrentAsync(
            torrent.MagnetLink,
            downloadPath,
            _options.TorrentCategory,
            cancellationToken).ConfigureAwait(false);

        if (!added)
        {
            throw new InvalidOperationException("Failed to add torrent to qBittorrent");
        }

        // Extract hash from magnet link
        var hash = ExtractHashFromMagnet(torrent.MagnetLink);

        // Create a deterministic GUID from TMDB ID
        var movieGuid = CreateGuidFromTmdbId("movie", tmdbId);

        // Create download record
        var download = new TorrentDownload
        {
            TorrentHash = hash,
            Name = torrent.Title,
            MagnetLink = torrent.MagnetLink,
            MediaType = MediaType.Movie,
            MovieId = movieGuid,
            MovieName = movieTitle,
            State = TorrentState.Queued,
            TotalSize = torrent.Size,
            Seeders = torrent.Seeders,
            Leechers = torrent.Leechers,
            Quality = torrent.Quality,
            IndexerName = torrent.IndexerName,
            AutoImport = _options.AutoImportEnabled,
            InitiatedByUserId = userId,
            SavePath = downloadPath
        };

        await _repository.AddAsync(download, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Started discovery movie download: {Movie} ({Year}) - {Torrent} -> {Path}",
            movieTitle, year, torrent.Title, downloadPath);

        return download;
    }

    /// <inheritdoc />
    public async Task<TorrentDownload> StartDiscoveryEpisodeDownloadAsync(
        TorrentSearchResult torrent,
        int tmdbId,
        string showName,
        int seasonNumber,
        int episodeNumber,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get the download path using the library path resolver
        // This returns: {TV Library}/{Show Name}/Season {XX}/
        var downloadPath = _pathResolver.GetTvShowDownloadPath(showName, seasonNumber);
        if (string.IsNullOrEmpty(downloadPath))
        {
            throw new InvalidOperationException("No TV shows library configured. Please configure a TV shows library in Jellyfin settings.");
        }

        // Note: Don't create the directory here - qBittorrent will create it with proper permissions

        // Check disk space (use parent directory for check since target folder may not exist yet)
        if (!_pathResolver.HasEnoughDiskSpace(downloadPath, torrent.Size))
        {
            throw new InvalidOperationException("Insufficient disk space for download");
        }

        // Ensure category exists in qBittorrent
        await _qbClient.CreateCategoryAsync(_options.TorrentCategory, downloadPath, cancellationToken).ConfigureAwait(false);

        // Add torrent to qBittorrent with the library path
        var added = await _qbClient.AddTorrentAsync(
            torrent.MagnetLink,
            downloadPath,
            _options.TorrentCategory,
            cancellationToken).ConfigureAwait(false);

        if (!added)
        {
            throw new InvalidOperationException("Failed to add torrent to qBittorrent");
        }

        // Extract hash from magnet link
        var hash = ExtractHashFromMagnet(torrent.MagnetLink);

        // Create a deterministic GUID from TMDB ID
        var seriesGuid = CreateGuidFromTmdbId("tv", tmdbId);

        // Create download record
        var download = new TorrentDownload
        {
            TorrentHash = hash,
            Name = torrent.Title,
            MagnetLink = torrent.MagnetLink,
            MediaType = MediaType.Episode,
            SeriesId = seriesGuid,
            SeriesName = showName,
            SeasonNumber = seasonNumber,
            EpisodeNumber = episodeNumber,
            State = TorrentState.Queued,
            TotalSize = torrent.Size,
            Seeders = torrent.Seeders,
            Leechers = torrent.Leechers,
            Quality = torrent.Quality,
            IndexerName = torrent.IndexerName,
            AutoImport = _options.AutoImportEnabled,
            InitiatedByUserId = userId,
            SavePath = downloadPath
        };

        await _repository.AddAsync(download, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Started discovery episode download: {Show} S{Season:D2}E{Episode:D2} - {Torrent} -> {Path}",
            showName, seasonNumber, episodeNumber, torrent.Title, downloadPath);

        return download;
    }

    /// <summary>
    /// Creates a deterministic GUID from a TMDB ID.
    /// </summary>
    private static Guid CreateGuidFromTmdbId(string type, int tmdbId)
    {
        var input = $"tmdb:{type}:{tmdbId}";
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentDownload>> GetAllDownloadsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TorrentDownload>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetActiveDownloadsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<TorrentDownload?> GetDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(downloadId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PauseDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var download = await _repository.GetByIdAsync(downloadId, cancellationToken).ConfigureAwait(false);
        if (download == null)
        {
            throw new ArgumentException($"Download not found: {downloadId}");
        }

        await _qbClient.PauseTorrentAsync(download.TorrentHash, cancellationToken).ConfigureAwait(false);

        download.State = TorrentState.Paused;
        await _repository.UpdateAsync(download, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Paused download: {Name}", download.Name);
    }

    /// <inheritdoc />
    public async Task ResumeDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var download = await _repository.GetByIdAsync(downloadId, cancellationToken).ConfigureAwait(false);
        if (download == null)
        {
            throw new ArgumentException($"Download not found: {downloadId}");
        }

        await _qbClient.ResumeTorrentAsync(download.TorrentHash, cancellationToken).ConfigureAwait(false);

        download.State = TorrentState.Downloading;
        await _repository.UpdateAsync(download, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Resumed download: {Name}", download.Name);
    }

    /// <inheritdoc />
    public async Task CancelDownloadAsync(Guid downloadId, bool deleteFiles = false, CancellationToken cancellationToken = default)
    {
        var download = await _repository.GetByIdAsync(downloadId, cancellationToken).ConfigureAwait(false);
        if (download == null)
        {
            throw new ArgumentException($"Download not found: {downloadId}");
        }

        await _qbClient.DeleteTorrentAsync(download.TorrentHash, deleteFiles, cancellationToken).ConfigureAwait(false);
        await _repository.DeleteAsync(downloadId, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cancelled download: {Name}, deleteFiles: {DeleteFiles}", download.Name, deleteFiles);
    }

    /// <inheritdoc />
    public async Task ImportDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var download = await _repository.GetByIdAsync(downloadId, cancellationToken).ConfigureAwait(false);
        if (download == null)
        {
            throw new ArgumentException($"Download not found: {downloadId}");
        }

        if (download.State != TorrentState.Completed && download.State != TorrentState.Seeding)
        {
            throw new InvalidOperationException("Download is not completed");
        }

        download.State = TorrentState.Importing;
        await _repository.UpdateAsync(download, cancellationToken).ConfigureAwait(false);

        // The actual import is handled by the AutoImportWorker
        _logger.LogInformation("Marked download for import: {Name}", download.Name);
    }

    /// <inheritdoc />
    public Task<bool> GetConnectionStatusAsync(CancellationToken cancellationToken = default)
    {
        return _qbClient.IsConnectedAsync(cancellationToken);
    }

    private static string ExtractHashFromMagnet(string magnetLink)
    {
        const string btihPrefix = "urn:btih:";
        var startIndex = magnetLink.IndexOf(btihPrefix, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1)
        {
            throw new ArgumentException("Invalid magnet link: no btih found");
        }

        startIndex += btihPrefix.Length;
        var endIndex = magnetLink.IndexOf('&', startIndex);
        if (endIndex == -1)
        {
            endIndex = magnetLink.Length;
        }

        return magnetLink.Substring(startIndex, endIndex - startIndex).ToLowerInvariant();
    }
}
