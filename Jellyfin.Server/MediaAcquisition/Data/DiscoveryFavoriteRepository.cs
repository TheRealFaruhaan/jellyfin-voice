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
/// File-based repository for discovery favorites.
/// Uses JSON file storage to avoid requiring EF Core schema changes.
/// </summary>
public class DiscoveryFavoriteRepository : IDiscoveryFavoriteRepository, IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly ILogger<DiscoveryFavoriteRepository> _logger;
    private readonly string _dataFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ConcurrentDictionary<Guid, DiscoveryFavorite> _favorites = new();
    private bool _isLoaded;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryFavoriteRepository"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public DiscoveryFavoriteRepository(
        IServerApplicationPaths applicationPaths,
        ILogger<DiscoveryFavoriteRepository> logger)
    {
        _logger = logger;
        _dataFilePath = Path.Combine(applicationPaths.DataPath, "media-acquisition", "discovery-favorites.json");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DiscoveryFavorite>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _favorites.Values
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.FavoritedAt)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<DiscoveryFavorite?> GetByUserAndTmdbIdAsync(Guid userId, int tmdbId, string mediaType, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _favorites.Values.FirstOrDefault(f =>
            f.UserId == userId &&
            f.TmdbId == tmdbId &&
            f.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<bool> IsFavoritedAsync(Guid userId, int tmdbId, string mediaType, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _favorites.Values.Any(f =>
            f.UserId == userId &&
            f.TmdbId == tmdbId &&
            f.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, bool>> GetFavoriteStatusAsync(Guid userId, IEnumerable<int> tmdbIds, string mediaType, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        var idsSet = new HashSet<int>(tmdbIds);
        var favoritedIds = _favorites.Values
            .Where(f => f.UserId == userId &&
                        f.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase) &&
                        idsSet.Contains(f.TmdbId))
            .Select(f => f.TmdbId)
            .ToHashSet();

        return idsSet.ToDictionary(id => id, id => favoritedIds.Contains(id));
    }

    /// <inheritdoc />
    public async Task AddAsync(DiscoveryFavorite favorite, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check if already exists
            var existing = _favorites.Values.FirstOrDefault(f =>
                f.UserId == favorite.UserId &&
                f.TmdbId == favorite.TmdbId &&
                f.MediaType.Equals(favorite.MediaType, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _logger.LogDebug("Favorite already exists for user {UserId}, TMDB ID {TmdbId}, type {Type}",
                    favorite.UserId, favorite.TmdbId, favorite.MediaType);
                return;
            }

            _favorites.TryAdd(favorite.Id, favorite);
            await SaveAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Added favorite for user {UserId}: {Title} (TMDB: {TmdbId}, Type: {Type})",
                favorite.UserId, favorite.Title, favorite.TmdbId, favorite.MediaType);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid userId, int tmdbId, string mediaType, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var toRemove = _favorites.Values.FirstOrDefault(f =>
                f.UserId == userId &&
                f.TmdbId == tmdbId &&
                f.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase));

            if (toRemove == null)
            {
                return false;
            }

            if (_favorites.TryRemove(toRemove.Id, out _))
            {
                await SaveAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Removed favorite for user {UserId}: {Title} (TMDB: {TmdbId}, Type: {Type})",
                    userId, toRemove.Title, tmdbId, mediaType);
                return true;
            }

            return false;
        }
        finally
        {
            _lock.Release();
        }
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

            if (!File.Exists(_dataFilePath))
            {
                _logger.LogInformation("Favorites file does not exist, starting with empty collection");
                _isLoaded = true;
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_dataFilePath, cancellationToken).ConfigureAwait(false);
                var favorites = JsonSerializer.Deserialize<List<DiscoveryFavorite>>(json, _jsonOptions);

                if (favorites != null)
                {
                    _favorites = new ConcurrentDictionary<Guid, DiscoveryFavorite>(
                        favorites.ToDictionary(f => f.Id));
                    _logger.LogInformation("Loaded {Count} favorites from {Path}", _favorites.Count, _dataFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load favorites from {Path}, starting with empty collection", _dataFilePath);
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
        try
        {
            var json = JsonSerializer.Serialize(_favorites.Values.ToList(), _jsonOptions);
            await File.WriteAllTextAsync(_dataFilePath, json, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Saved {Count} favorites to {Path}", _favorites.Count, _dataFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save favorites to {Path}", _dataFilePath);
            throw;
        }
    }

    /// <summary>
    /// Disposes the repository.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Dispose managed resources
            _lock.Dispose();
        }

        _disposed = true;
    }
}
