using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Data.Entities;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.MediaAcquisition.Data;

/// <summary>
/// File-based repository for torrent downloads.
/// Uses JSON file storage to avoid requiring EF Core schema changes.
/// </summary>
public class TorrentDownloadRepository : ITorrentDownloadRepository, IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly ILogger<TorrentDownloadRepository> _logger;
    private readonly string _dataFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ConcurrentDictionary<Guid, TorrentDownload> _downloads = new();
    private bool _isLoaded;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TorrentDownloadRepository"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public TorrentDownloadRepository(
        IServerApplicationPaths applicationPaths,
        ILogger<TorrentDownloadRepository> logger)
    {
        _logger = logger;
        _dataFilePath = Path.Combine(applicationPaths.DataPath, "media-acquisition", "downloads.json");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentDownload>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.Values.OrderByDescending(d => d.AddedAt).ToList();
    }

    /// <inheritdoc />
    public async Task<TorrentDownload?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.TryGetValue(id, out var download) ? download : null;
    }

    /// <inheritdoc />
    public async Task<TorrentDownload?> GetByHashAsync(string torrentHash, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.Values.FirstOrDefault(d => d.TorrentHash.Equals(torrentHash, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentDownload>> GetByStateAsync(TorrentState state, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.Values.Where(d => d.State == state).OrderByDescending(d => d.AddedAt).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentDownload>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        var activeStates = new[] { TorrentState.Queued, TorrentState.Downloading, TorrentState.Paused, TorrentState.Seeding };
        return _downloads.Values.Where(d => activeStates.Contains(d.State)).OrderByDescending(d => d.AddedAt).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentDownload>> GetBySeriesAsync(Guid seriesId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.Values.Where(d => d.SeriesId == seriesId).OrderByDescending(d => d.AddedAt).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentDownload>> GetByMovieAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.Values.Where(d => d.MovieId == movieId).OrderByDescending(d => d.AddedAt).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TorrentDownload>> GetCompletedPendingImportAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.Values
            .Where(d => d.State == TorrentState.Completed && d.AutoImport && d.ImportedAt == null)
            .OrderBy(d => d.CompletedAt)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<TorrentDownload> AddAsync(TorrentDownload download, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (download.Id == Guid.Empty)
        {
            download.Id = Guid.NewGuid();
        }

        download.AddedAt = DateTime.UtcNow;

        _downloads[download.Id] = download;
        await SaveAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added download: {Name} ({Id})", download.Name, download.Id);
        return download;
    }

    /// <inheritdoc />
    public async Task<TorrentDownload> UpdateAsync(TorrentDownload download, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        _downloads[download.Id] = download;
        await SaveAsync(cancellationToken).ConfigureAwait(false);

        return download;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (_downloads.TryRemove(id, out var removed))
        {
            await SaveAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Deleted download: {Name} ({Id})", removed.Name, id);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForEpisodeAsync(Guid seriesId, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.Values.Any(d =>
            d.MediaType == MediaType.Episode &&
            d.SeriesId == seriesId &&
            d.SeasonNumber == seasonNumber &&
            d.EpisodeNumber == episodeNumber &&
            d.State != TorrentState.Error);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForMovieAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _downloads.Values.Any(d =>
            d.MediaType == MediaType.Movie &&
            d.MovieId == movieId &&
            d.State != TorrentState.Error);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_isLoaded)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isLoaded)
            {
                return;
            }

            if (File.Exists(_dataFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_dataFilePath, cancellationToken).ConfigureAwait(false);
                    var downloads = JsonSerializer.Deserialize<List<TorrentDownload>>(json);
                    if (downloads != null)
                    {
                        _downloads = new ConcurrentDictionary<Guid, TorrentDownload>(
                            downloads.ToDictionary(d => d.Id));
                    }

                    _logger.LogInformation("Loaded {Count} downloads from storage", _downloads.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load downloads from storage");
                }
            }

            _isLoaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(_downloads.Values.ToList(), _jsonOptions);
            await File.WriteAllTextAsync(_dataFilePath, json, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save downloads to storage");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _lock.Dispose();
        }

        _disposed = true;
    }
}
