using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Data.Entities;

namespace Jellyfin.Server.MediaAcquisition.Data;

/// <summary>
/// Interface for torrent download repository operations.
/// </summary>
public interface ITorrentDownloadRepository
{
    /// <summary>
    /// Gets all downloads.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of all downloads.</returns>
    Task<IReadOnlyList<TorrentDownload>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a download by ID.
    /// </summary>
    /// <param name="id">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The download, or null if not found.</returns>
    Task<TorrentDownload?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a download by torrent hash.
    /// </summary>
    /// <param name="torrentHash">The torrent hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The download, or null if not found.</returns>
    Task<TorrentDownload?> GetByHashAsync(string torrentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets downloads by state.
    /// </summary>
    /// <param name="state">The state to filter by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of downloads in the specified state.</returns>
    Task<IReadOnlyList<TorrentDownload>> GetByStateAsync(TorrentState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active downloads (queued, downloading, paused, seeding).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of active downloads.</returns>
    Task<IReadOnlyList<TorrentDownload>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets downloads for a specific series.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of downloads for the series.</returns>
    Task<IReadOnlyList<TorrentDownload>> GetBySeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets downloads for a specific movie.
    /// </summary>
    /// <param name="movieId">The movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of downloads for the movie.</returns>
    Task<IReadOnlyList<TorrentDownload>> GetByMovieAsync(Guid movieId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets completed downloads that haven't been imported yet.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of completed downloads pending import.</returns>
    Task<IReadOnlyList<TorrentDownload>> GetCompletedPendingImportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new download.
    /// </summary>
    /// <param name="download">The download to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The added download.</returns>
    Task<TorrentDownload> AddAsync(TorrentDownload download, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing download.
    /// </summary>
    /// <param name="download">The download to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated download.</returns>
    Task<TorrentDownload> UpdateAsync(TorrentDownload download, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a download.
    /// </summary>
    /// <param name="id">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a download exists for a specific episode.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if a download exists.</returns>
    Task<bool> ExistsForEpisodeAsync(Guid seriesId, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a download exists for a specific movie.
    /// </summary>
    /// <param name="movieId">The movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if a download exists.</returns>
    Task<bool> ExistsForMovieAsync(Guid movieId, CancellationToken cancellationToken = default);
}
