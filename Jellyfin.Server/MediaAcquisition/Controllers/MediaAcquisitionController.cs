using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api;
using Jellyfin.Api.Extensions;
using Jellyfin.Server.MediaAcquisition.Indexers;
using Jellyfin.Server.MediaAcquisition.Models;
using Jellyfin.Server.MediaAcquisition.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.MediaAcquisition.Controllers;

/// <summary>
/// Controller for media acquisition operations.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "RequiresElevation")]
public class MediaAcquisitionController : BaseJellyfinApiController
{
    private readonly IMissingMediaService _missingMediaService;
    private readonly ITorrentSearchService _searchService;
    private readonly IDownloadManagerService _downloadManager;
    private readonly ILogger<MediaAcquisitionController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaAcquisitionController"/> class.
    /// </summary>
    /// <param name="missingMediaService">The missing media service.</param>
    /// <param name="searchService">The torrent search service.</param>
    /// <param name="downloadManager">The download manager service.</param>
    /// <param name="logger">The logger.</param>
    public MediaAcquisitionController(
        IMissingMediaService missingMediaService,
        ITorrentSearchService searchService,
        IDownloadManagerService downloadManager,
        ILogger<MediaAcquisitionController> logger)
    {
        _missingMediaService = missingMediaService;
        _searchService = searchService;
        _downloadManager = downloadManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets qBittorrent connection status.
    /// </summary>
    /// <response code="200">Connection status returned.</response>
    /// <returns>Connection status.</returns>
    [HttpGet("Status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectionStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var isConnected = await _downloadManager.GetConnectionStatusAsync(cancellationToken).ConfigureAwait(false);
        var indexerStatus = await _searchService.TestIndexersAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Status check - qBittorrent connected: {IsConnected}, indexers: {IndexerCount}", isConnected, indexerStatus.Count);

        return Ok(new ConnectionStatusDto
        {
            QBittorrentConnected = isConnected,
            Indexers = indexerStatus
        });
    }

    /// <summary>
    /// Gets missing episodes for a series.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Missing episodes returned.</response>
    /// <response code="404">Series not found.</response>
    /// <returns>List of missing episodes.</returns>
    [HttpGet("Missing/Episodes/{seriesId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MissingEpisodeInfo>>> GetMissingEpisodes(
        [FromRoute, Required] Guid seriesId,
        CancellationToken cancellationToken)
    {
        var missing = await _missingMediaService.GetMissingEpisodesAsync(seriesId, cancellationToken).ConfigureAwait(false);
        return Ok(missing);
    }

    /// <summary>
    /// Gets missing episodes for a specific season.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Missing episodes returned.</response>
    /// <returns>List of missing episodes.</returns>
    [HttpGet("Missing/Episodes/{seriesId}/Season/{seasonNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MissingEpisodeInfo>>> GetMissingEpisodesForSeason(
        [FromRoute, Required] Guid seriesId,
        [FromRoute, Required] int seasonNumber,
        CancellationToken cancellationToken)
    {
        var missing = await _missingMediaService.GetMissingEpisodesForSeasonAsync(seriesId, seasonNumber, cancellationToken).ConfigureAwait(false);
        return Ok(missing);
    }

    /// <summary>
    /// Gets all missing episodes.
    /// </summary>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Missing episodes returned.</response>
    /// <returns>List of missing episodes.</returns>
    [HttpGet("Missing/Episodes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MissingEpisodeInfo>>> GetAllMissingEpisodes(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var missing = await _missingMediaService.GetAllMissingEpisodesAsync(limit, cancellationToken).ConfigureAwait(false);
        return Ok(missing);
    }

    /// <summary>
    /// Searches for episode torrents.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Search results returned.</response>
    /// <returns>List of torrent search results.</returns>
    [HttpPost("Search/Episode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TorrentSearchResult>>> SearchEpisode(
        [FromBody, Required] SearchEpisodeRequest request,
        CancellationToken cancellationToken)
    {
        var results = await _searchService.SearchEpisodeAsync(
            request.SeriesId,
            request.SeasonNumber,
            request.EpisodeNumber,
            cancellationToken).ConfigureAwait(false);

        return Ok(results);
    }

    /// <summary>
    /// Searches for movie torrents.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Search results returned.</response>
    /// <returns>List of torrent search results.</returns>
    [HttpPost("Search/Movie")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TorrentSearchResult>>> SearchMovie(
        [FromBody, Required] SearchMovieRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TorrentSearchResult> results;

        if (request.MovieId.HasValue)
        {
            results = await _searchService.SearchMovieAsync(request.MovieId.Value, cancellationToken).ConfigureAwait(false);
        }
        else if (!string.IsNullOrEmpty(request.MovieName))
        {
            results = await _searchService.SearchMovieByNameAsync(request.MovieName, request.Year, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return BadRequest("Either MovieId or MovieName must be provided");
        }

        return Ok(results);
    }

    /// <summary>
    /// Gets all downloads.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Downloads returned.</response>
    /// <returns>List of downloads.</returns>
    [HttpGet("Downloads")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DownloadDto>>> GetDownloads(CancellationToken cancellationToken)
    {
        var downloads = await _downloadManager.GetAllDownloadsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(downloads.Select(DownloadDto.FromEntity));
    }

    /// <summary>
    /// Gets active downloads.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Active downloads returned.</response>
    /// <returns>List of active downloads.</returns>
    [HttpGet("Downloads/Active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DownloadDto>>> GetActiveDownloads(CancellationToken cancellationToken)
    {
        var downloads = await _downloadManager.GetActiveDownloadsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(downloads.Select(DownloadDto.FromEntity));
    }

    /// <summary>
    /// Gets a download by ID.
    /// </summary>
    /// <param name="id">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Download returned.</response>
    /// <response code="404">Download not found.</response>
    /// <returns>The download.</returns>
    [HttpGet("Downloads/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DownloadDto>> GetDownload(
        [FromRoute, Required] Guid id,
        CancellationToken cancellationToken)
    {
        var download = await _downloadManager.GetDownloadAsync(id, cancellationToken).ConfigureAwait(false);
        if (download == null)
        {
            return NotFound();
        }

        return Ok(DownloadDto.FromEntity(download));
    }

    /// <summary>
    /// Starts an episode download.
    /// </summary>
    /// <param name="request">The download request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Download started.</response>
    /// <response code="400">Invalid request.</response>
    /// <returns>The created download.</returns>
    [HttpPost("Downloads/Episode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DownloadDto>> StartEpisodeDownload(
        [FromBody, Required] StartEpisodeDownloadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();

            var torrent = new TorrentSearchResult
            {
                Title = request.Title,
                MagnetLink = request.MagnetLink,
                Size = request.Size,
                Seeders = request.Seeders,
                Leechers = request.Leechers,
                Quality = request.Quality,
                IndexerName = request.IndexerName ?? "Manual"
            };

            var download = await _downloadManager.StartEpisodeDownloadAsync(
                torrent,
                request.SeriesId,
                request.SeasonNumber,
                request.EpisodeNumber,
                userId,
                cancellationToken).ConfigureAwait(false);

            return Ok(DownloadDto.FromEntity(download));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start episode download");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Starts a movie download.
    /// </summary>
    /// <param name="request">The download request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Download started.</response>
    /// <response code="400">Invalid request.</response>
    /// <returns>The created download.</returns>
    [HttpPost("Downloads/Movie")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DownloadDto>> StartMovieDownload(
        [FromBody, Required] StartMovieDownloadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();

            var torrent = new TorrentSearchResult
            {
                Title = request.Title,
                MagnetLink = request.MagnetLink,
                Size = request.Size,
                Seeders = request.Seeders,
                Leechers = request.Leechers,
                Quality = request.Quality,
                IndexerName = request.IndexerName ?? "Manual"
            };

            var download = await _downloadManager.StartMovieDownloadAsync(
                torrent,
                request.MovieId,
                userId,
                cancellationToken).ConfigureAwait(false);

            return Ok(DownloadDto.FromEntity(download));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start movie download");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Pauses a download.
    /// </summary>
    /// <param name="id">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="204">Download paused.</response>
    /// <response code="404">Download not found.</response>
    /// <returns>No content.</returns>
    [HttpPost("Downloads/{id}/Pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PauseDownload(
        [FromRoute, Required] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _downloadManager.PauseDownloadAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Resumes a download.
    /// </summary>
    /// <param name="id">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="204">Download resumed.</response>
    /// <response code="404">Download not found.</response>
    /// <returns>No content.</returns>
    [HttpPost("Downloads/{id}/Resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ResumeDownload(
        [FromRoute, Required] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _downloadManager.ResumeDownloadAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Deletes a download.
    /// </summary>
    /// <param name="id">The download ID.</param>
    /// <param name="deleteFiles">Whether to delete downloaded files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="204">Download deleted.</response>
    /// <response code="404">Download not found.</response>
    /// <returns>No content.</returns>
    [HttpDelete("Downloads/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDownload(
        [FromRoute, Required] Guid id,
        [FromQuery] bool deleteFiles = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _downloadManager.CancelDownloadAsync(id, deleteFiles, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Triggers import for a completed download.
    /// </summary>
    /// <param name="id">The download ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="204">Import triggered.</response>
    /// <response code="400">Download not ready for import.</response>
    /// <response code="404">Download not found.</response>
    /// <returns>No content.</returns>
    [HttpPost("Downloads/{id}/Import")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ImportDownload(
        [FromRoute, Required] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _downloadManager.ImportDownloadAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
