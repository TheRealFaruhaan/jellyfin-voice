using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.QBittorrent.Models;

namespace Jellyfin.Server.MediaAcquisition.QBittorrent;

/// <summary>
/// Interface for qBittorrent Web API client.
/// </summary>
public interface IQBittorrentClient
{
    /// <summary>
    /// Authenticates with qBittorrent.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if authentication was successful.</returns>
    Task<bool> LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the qBittorrent application version.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The version string.</returns>
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all torrents, optionally filtered by category.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of torrents.</returns>
    Task<IReadOnlyList<QBittorrentTorrent>> GetTorrentsAsync(string? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific torrent by hash.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The torrent, or null if not found.</returns>
    Task<QBittorrentTorrent?> GetTorrentAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files within a torrent.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of files.</returns>
    Task<IReadOnlyList<QBittorrentFile>> GetTorrentFilesAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a torrent by magnet link.
    /// </summary>
    /// <param name="magnetLink">The magnet link.</param>
    /// <param name="savePath">Optional save path.</param>
    /// <param name="category">Optional category.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the torrent was added successfully.</returns>
    Task<bool> AddTorrentAsync(string magnetLink, string? savePath = null, string? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a torrent.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PauseTorrentAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a torrent.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ResumeTorrentAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a torrent.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="deleteFiles">Whether to delete downloaded files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteTorrentAsync(string hash, bool deleteFiles = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the category for a torrent.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="category">The category name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SetCategoryAsync(string hash, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a category if it doesn't exist.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="savePath">Optional save path for the category.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateCategoryAsync(string category, string? savePath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if qBittorrent is reachable and authenticated.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if connected and authenticated.</returns>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
}
