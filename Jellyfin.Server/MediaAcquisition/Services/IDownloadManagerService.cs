using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Data.Entities;
using Jellyfin.Server.MediaAcquisition.Indexers;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for managing torrent downloads.
/// </summary>
public interface IDownloadManagerService
{
    /// <summary>
    /// Starts a download for a TV episode.
    /// </summary>
    /// <param name="torrent">The torrent search result.</param>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="userId">The user who initiated the download.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created download.</returns>
    Task<TorrentDownload> StartEpisodeDownloadAsync(
        TorrentSearchResult torrent,
        Guid seriesId,
        int seasonNumber,
        int episodeNumber,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a download for a movie.
    /// </summary>
    /// <param name="torrent">The torrent search result.</param>
    /// <param name="movieId">The movie ID.</param>
    /// <param name="userId">The user who initiated the download.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created download.</returns>
    Task<TorrentDownload> StartMovieDownloadAsync(
        TorrentSearchResult torrent,
        Guid movieId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all downloads.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of all downloads.</returns>
    Task<IReadOnlyList<TorrentDownload>> GetAllDownloadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active downloads.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of active downloads.</returns>
    Task<IReadOnlyList<TorrentDownload>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a download by ID.
    /// </summary>
    /// <param name="downloadId">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The download, or null if not found.</returns>
    Task<TorrentDownload?> GetDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a download.
    /// </summary>
    /// <param name="downloadId">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PauseDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a download.
    /// </summary>
    /// <param name="downloadId">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ResumeDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels and deletes a download.
    /// </summary>
    /// <param name="downloadId">The download ID.</param>
    /// <param name="deleteFiles">Whether to delete downloaded files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CancelDownloadAsync(Guid downloadId, bool deleteFiles = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually triggers import for a completed download.
    /// </summary>
    /// <param name="downloadId">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ImportDownloadAsync(Guid downloadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the qBittorrent connection status.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if connected.</returns>
    Task<bool> GetConnectionStatusAsync(CancellationToken cancellationToken = default);
}
