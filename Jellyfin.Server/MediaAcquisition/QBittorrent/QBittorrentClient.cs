using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Configuration;
using Jellyfin.Server.MediaAcquisition.QBittorrent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.MediaAcquisition.QBittorrent;

/// <summary>
/// Client for interacting with qBittorrent Web API.
/// </summary>
public class QBittorrentClient : IQBittorrentClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QBittorrentClient> _logger;
    private readonly MediaAcquisitionOptions _options;
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    private bool _isAuthenticated;
    private DateTime _lastLoginAttempt = DateTime.MinValue;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="QBittorrentClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The configuration options.</param>
    public QBittorrentClient(
        HttpClient httpClient,
        ILogger<QBittorrentClient> logger,
        IOptions<MediaAcquisitionOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.QBittorrentUrl.TrimEnd('/') + "/api/v2/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc />
    public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
    {
        await _loginLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Rate limit login attempts
            if (DateTime.UtcNow - _lastLoginAttempt < TimeSpan.FromSeconds(5))
            {
                return _isAuthenticated;
            }

            _lastLoginAttempt = DateTime.UtcNow;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = _options.QBittorrentUsername,
                ["password"] = _options.QBittorrentPassword
            });

            var response = await _httpClient.PostAsync("auth/login", content, cancellationToken).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            _isAuthenticated = response.IsSuccessStatusCode && responseText.Contains("Ok", StringComparison.OrdinalIgnoreCase);

            if (_isAuthenticated)
            {
                _logger.LogInformation("Successfully authenticated with qBittorrent");
            }
            else
            {
                _logger.LogWarning("Failed to authenticate with qBittorrent: {Response}", responseText);
            }

            return _isAuthenticated;
        }
        finally
        {
            _loginLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var response = await _httpClient.GetAsync("app/version", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QBittorrentTorrent>> GetTorrentsAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var url = "torrents/info";
        if (!string.IsNullOrEmpty(category))
        {
            url += $"?category={Uri.EscapeDataString(category)}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var torrents = JsonSerializer.Deserialize<List<QBittorrentTorrent>>(json);

        return torrents ?? new List<QBittorrentTorrent>();
    }

    /// <inheritdoc />
    public async Task<QBittorrentTorrent?> GetTorrentAsync(string hash, CancellationToken cancellationToken = default)
    {
        var torrents = await GetTorrentsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return torrents.FirstOrDefault(t => t.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QBittorrentFile>> GetTorrentFilesAsync(string hash, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var response = await _httpClient.GetAsync($"torrents/files?hash={hash}", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var files = JsonSerializer.Deserialize<List<QBittorrentFile>>(json);

        return files ?? new List<QBittorrentFile>();
    }

    /// <inheritdoc />
    public async Task<bool> AddTorrentAsync(string magnetLink, string? savePath = null, string? category = null, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var formData = new Dictionary<string, string>
        {
            ["urls"] = magnetLink
        };

        if (!string.IsNullOrEmpty(savePath))
        {
            formData["savepath"] = savePath;
        }

        if (!string.IsNullOrEmpty(category))
        {
            formData["category"] = category;
        }

        var content = new FormUrlEncodedContent(formData);

        var response = await _httpClient.PostAsync("torrents/add", content, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully added torrent: {MagnetLink}", magnetLink.Substring(0, Math.Min(100, magnetLink.Length)));
            return true;
        }

        var errorText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogWarning("Failed to add torrent: {Error}", errorText);
        return false;
    }

    /// <inheritdoc />
    public async Task PauseTorrentAsync(string hash, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["hashes"] = hash
        });

        var response = await _httpClient.PostAsync("torrents/pause", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        _logger.LogDebug("Paused torrent: {Hash}", hash);
    }

    /// <inheritdoc />
    public async Task ResumeTorrentAsync(string hash, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["hashes"] = hash
        });

        var response = await _httpClient.PostAsync("torrents/resume", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        _logger.LogDebug("Resumed torrent: {Hash}", hash);
    }

    /// <inheritdoc />
    public async Task DeleteTorrentAsync(string hash, bool deleteFiles = false, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["hashes"] = hash,
            ["deleteFiles"] = deleteFiles.ToString().ToLowerInvariant()
        });

        var response = await _httpClient.PostAsync("torrents/delete", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Deleted torrent: {Hash}, deleteFiles: {DeleteFiles}", hash, deleteFiles);
    }

    /// <inheritdoc />
    public async Task SetCategoryAsync(string hash, string category, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["hashes"] = hash,
            ["category"] = category
        });

        var response = await _httpClient.PostAsync("torrents/setCategory", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task CreateCategoryAsync(string category, string? savePath = null, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        var formData = new Dictionary<string, string>
        {
            ["category"] = category
        };

        if (!string.IsNullOrEmpty(savePath))
        {
            formData["savePath"] = savePath;
        }

        var content = new FormUrlEncodedContent(formData);

        // This endpoint returns 409 if category already exists, which is fine
        var response = await _httpClient.PostAsync("torrents/createCategory", content, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Conflict)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Force a fresh login attempt to verify connection is still alive
            var loginSuccess = await LoginAsync(cancellationToken).ConfigureAwait(false);
            if (!loginSuccess)
            {
                _logger.LogWarning("qBittorrent connection check failed: login unsuccessful");
                return false;
            }

            var response = await _httpClient.GetAsync("app/version", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("qBittorrent version check failed with status {StatusCode}", response.StatusCode);
                _isAuthenticated = false;
                return false;
            }

            var version = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("qBittorrent connection check successful, version: {Version}", version);
            return !string.IsNullOrEmpty(version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "qBittorrent connection check failed with exception");
            _isAuthenticated = false;
            return false;
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!_isAuthenticated)
        {
            var success = await LoginAsync(cancellationToken).ConfigureAwait(false);
            if (!success)
            {
                throw new InvalidOperationException("Failed to authenticate with qBittorrent");
            }
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
            _loginLock.Dispose();
        }

        _disposed = true;
    }
}
