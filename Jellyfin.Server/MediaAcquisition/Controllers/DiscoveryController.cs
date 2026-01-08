using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api;
using Jellyfin.Api.Extensions;
using Jellyfin.Server.MediaAcquisition.Indexers;
using Jellyfin.Server.MediaAcquisition.Models;
using Jellyfin.Server.MediaAcquisition.Services;
using Jellyfin.Server.MediaAcquisition.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.MediaAcquisition.Controllers;

/// <summary>
/// Controller for media discovery operations.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class DiscoveryController : BaseJellyfinApiController
{
    private readonly IDiscoveryService _discoveryService;
    private readonly ITorrentSearchService _searchService;
    private readonly ILibraryPathResolver _pathResolver;
    private readonly IDownloadManagerService _downloadManager;
    private readonly ILogger<DiscoveryController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryController"/> class.
    /// </summary>
    /// <param name="discoveryService">The discovery service.</param>
    /// <param name="searchService">The torrent search service.</param>
    /// <param name="pathResolver">The library path resolver.</param>
    /// <param name="downloadManager">The download manager service.</param>
    /// <param name="logger">The logger.</param>
    public DiscoveryController(
        IDiscoveryService discoveryService,
        ITorrentSearchService searchService,
        ILibraryPathResolver pathResolver,
        IDownloadManagerService downloadManager,
        ILogger<DiscoveryController> logger)
    {
        _discoveryService = discoveryService;
        _searchService = searchService;
        _pathResolver = pathResolver;
        _downloadManager = downloadManager;
        _logger = logger;
    }

    // ===== MOVIES =====

    /// <summary>
    /// Gets trending movies from TMDB.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Trending movies returned.</response>
    /// <returns>Paged list of trending movies.</returns>
    [HttpGet("Movies/Trending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoveryPagedResultDto<DiscoveryMovieDto>>> GetTrendingMovies(
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _discoveryService.GetTrendingMoviesAsync(page, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Gets popular movies from TMDB.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Popular movies returned.</response>
    /// <returns>Paged list of popular movies.</returns>
    [HttpGet("Movies/Popular")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoveryPagedResultDto<DiscoveryMovieDto>>> GetPopularMovies(
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _discoveryService.GetPopularMoviesAsync(page, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Searches for movies on TMDB.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="page">The page number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Search results returned.</response>
    /// <returns>Paged list of movies matching the query.</returns>
    [HttpGet("Movies/Search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoveryPagedResultDto<DiscoveryMovieDto>>> SearchMovies(
        [FromQuery, Required] string query,
        [FromQuery] int? year = null,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _discoveryService.SearchMoviesAsync(query, year, page, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Gets movie details from TMDB.
    /// </summary>
    /// <param name="tmdbId">The TMDB movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Movie details returned.</response>
    /// <response code="404">Movie not found.</response>
    /// <returns>The movie details.</returns>
    [HttpGet("Movies/{tmdbId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscoveryMovieDto>> GetMovieDetails(
        [FromRoute, Required] int tmdbId,
        CancellationToken cancellationToken = default)
    {
        var movie = await _discoveryService.GetMovieDetailsAsync(tmdbId, cancellationToken).ConfigureAwait(false);
        if (movie == null)
        {
            return NotFound();
        }

        return Ok(movie);
    }

    /// <summary>
    /// Searches for movie torrents using multiple patterns.
    /// </summary>
    /// <param name="tmdbId">The TMDB movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Torrent search results returned.</response>
    /// <response code="404">Movie not found.</response>
    /// <returns>List of available torrents.</returns>
    [HttpGet("Movies/{tmdbId}/Torrents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TorrentSearchResult>>> GetMovieTorrents(
        [FromRoute, Required] int tmdbId,
        CancellationToken cancellationToken = default)
    {
        var movie = await _discoveryService.GetMovieDetailsAsync(tmdbId, cancellationToken).ConfigureAwait(false);
        if (movie == null)
        {
            return NotFound();
        }

        var patterns = SearchPatternGenerator.GenerateMoviePatterns(movie.Title, movie.ReleaseYear);
        var results = await _searchService.SearchByPatternsAsync(patterns, "movie", cancellationToken).ConfigureAwait(false);

        return Ok(results);
    }

    // ===== TV SHOWS =====

    /// <summary>
    /// Gets trending TV shows from TMDB.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Trending TV shows returned.</response>
    /// <returns>Paged list of trending TV shows.</returns>
    [HttpGet("TvShows/Trending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoveryPagedResultDto<DiscoveryTvShowDto>>> GetTrendingTvShows(
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _discoveryService.GetTrendingTvShowsAsync(page, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Gets popular TV shows from TMDB.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Popular TV shows returned.</response>
    /// <returns>Paged list of popular TV shows.</returns>
    [HttpGet("TvShows/Popular")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoveryPagedResultDto<DiscoveryTvShowDto>>> GetPopularTvShows(
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _discoveryService.GetPopularTvShowsAsync(page, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Searches for TV shows on TMDB.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="page">The page number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Search results returned.</response>
    /// <returns>Paged list of TV shows matching the query.</returns>
    [HttpGet("TvShows/Search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoveryPagedResultDto<DiscoveryTvShowDto>>> SearchTvShows(
        [FromQuery, Required] string query,
        [FromQuery] int? year = null,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _discoveryService.SearchTvShowsAsync(query, year, page, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Gets TV show details from TMDB.
    /// </summary>
    /// <param name="tmdbId">The TMDB TV show ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">TV show details returned.</response>
    /// <response code="404">TV show not found.</response>
    /// <returns>The TV show details.</returns>
    [HttpGet("TvShows/{tmdbId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscoveryTvShowDto>> GetTvShowDetails(
        [FromRoute, Required] int tmdbId,
        CancellationToken cancellationToken = default)
    {
        var show = await _discoveryService.GetTvShowDetailsAsync(tmdbId, cancellationToken).ConfigureAwait(false);
        if (show == null)
        {
            return NotFound();
        }

        return Ok(show);
    }

    /// <summary>
    /// Gets season details from TMDB.
    /// </summary>
    /// <param name="tmdbId">The TMDB TV show ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Season details returned.</response>
    /// <response code="404">Season not found.</response>
    /// <returns>The season details.</returns>
    [HttpGet("TvShows/{tmdbId}/Seasons/{seasonNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscoverySeasonDto>> GetSeasonDetails(
        [FromRoute, Required] int tmdbId,
        [FromRoute, Required] int seasonNumber,
        CancellationToken cancellationToken = default)
    {
        var season = await _discoveryService.GetSeasonDetailsAsync(tmdbId, seasonNumber, cancellationToken).ConfigureAwait(false);
        if (season == null)
        {
            return NotFound();
        }

        return Ok(season);
    }

    /// <summary>
    /// Searches for season torrents using multiple patterns.
    /// </summary>
    /// <param name="tmdbId">The TMDB TV show ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Torrent search results returned.</response>
    /// <response code="404">TV show not found.</response>
    /// <returns>List of available torrents.</returns>
    [HttpGet("TvShows/{tmdbId}/Seasons/{seasonNumber}/Torrents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TorrentSearchResult>>> GetSeasonTorrents(
        [FromRoute, Required] int tmdbId,
        [FromRoute, Required] int seasonNumber,
        CancellationToken cancellationToken = default)
    {
        var show = await _discoveryService.GetTvShowDetailsAsync(tmdbId, cancellationToken).ConfigureAwait(false);
        if (show == null)
        {
            return NotFound();
        }

        var results = await _searchService.SearchSeasonByNameAsync(show.Name, seasonNumber, null, cancellationToken).ConfigureAwait(false);
        return Ok(results);
    }

    /// <summary>
    /// Searches for episode torrents using multiple patterns.
    /// </summary>
    /// <param name="tmdbId">The TMDB TV show ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Torrent search results returned.</response>
    /// <response code="404">TV show not found.</response>
    /// <returns>List of available torrents.</returns>
    [HttpGet("TvShows/{tmdbId}/Seasons/{seasonNumber}/Episodes/{episodeNumber}/Torrents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TorrentSearchResult>>> GetEpisodeTorrents(
        [FromRoute, Required] int tmdbId,
        [FromRoute, Required] int seasonNumber,
        [FromRoute, Required] int episodeNumber,
        CancellationToken cancellationToken = default)
    {
        var show = await _discoveryService.GetTvShowDetailsAsync(tmdbId, cancellationToken).ConfigureAwait(false);
        if (show == null)
        {
            return NotFound();
        }

        var results = await _searchService.SearchEpisodeByNameAsync(show.Name, seasonNumber, episodeNumber, null, cancellationToken).ConfigureAwait(false);
        return Ok(results);
    }

    // ===== CUSTOM SEARCH =====

    /// <summary>
    /// Searches for torrents with a custom query.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Search results returned.</response>
    /// <returns>List of torrent results.</returns>
    [HttpPost("Torrents/Search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TorrentSearchResult>>> SearchTorrents(
        [FromBody, Required] CustomTorrentSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchByQueryAsync(request.Query, request.Category, cancellationToken).ConfigureAwait(false);
        return Ok(results);
    }

    // ===== DISK SPACE =====

    /// <summary>
    /// Gets disk space information for the movies library.
    /// </summary>
    /// <param name="requiredBytes">Optional bytes required for the download.</param>
    /// <response code="200">Disk space info returned.</response>
    /// <response code="404">No movies library configured.</response>
    /// <returns>Disk space information.</returns>
    [HttpGet("DiskSpace/Movies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DiskSpaceDto> GetMoviesDiskSpace([FromQuery] long requiredBytes = 0)
    {
        var path = _pathResolver.GetMoviesLibraryPath();
        if (string.IsNullOrEmpty(path))
        {
            return NotFound("No movies library configured");
        }

        var diskSpace = _pathResolver.GetDiskSpace(path, requiredBytes);
        return Ok(diskSpace);
    }

    /// <summary>
    /// Gets disk space information for the TV shows library.
    /// </summary>
    /// <param name="requiredBytes">Optional bytes required for the download.</param>
    /// <response code="200">Disk space info returned.</response>
    /// <response code="404">No TV shows library configured.</response>
    /// <returns>Disk space information.</returns>
    [HttpGet("DiskSpace/TvShows")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DiskSpaceDto> GetTvShowsDiskSpace([FromQuery] long requiredBytes = 0)
    {
        var path = _pathResolver.GetTvShowsLibraryPath();
        if (string.IsNullOrEmpty(path))
        {
            return NotFound("No TV shows library configured");
        }

        var diskSpace = _pathResolver.GetDiskSpace(path, requiredBytes);
        return Ok(diskSpace);
    }

    // ===== DOWNLOADS =====

    /// <summary>
    /// Starts a movie download from discovery.
    /// </summary>
    /// <param name="request">The download request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Download started.</response>
    /// <response code="400">Invalid request.</response>
    /// <returns>The created download.</returns>
    [HttpPost("Movies/Download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DownloadDto>> StartMovieDownload(
        [FromBody, Required] DiscoveryDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.GetUserId();

            // Get raw values - they may be swapped (Prowlarr sends magnet in downloadUrl)
            var rawMagnet = request.MagnetLink ?? string.Empty;
            var rawDownload = request.DownloadUrl ?? string.Empty;

            // Detect and fix swapped fields by checking content
            string magnetLink;
            string? downloadUrl;
            if (rawDownload.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                magnetLink = rawDownload;
                downloadUrl = string.IsNullOrEmpty(rawMagnet) ? null : rawMagnet;
            }
            else if (rawMagnet.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                magnetLink = rawMagnet;
                downloadUrl = string.IsNullOrEmpty(rawDownload) ? null : rawDownload;
            }
            else
            {
                // Neither is a magnet link - use downloadUrl if available
                magnetLink = !string.IsNullOrEmpty(rawDownload) ? rawDownload : rawMagnet;
                downloadUrl = null;
            }

            var torrent = new TorrentSearchResult
            {
                Title = request.Title,
                MagnetLink = magnetLink,
                DownloadUrl = downloadUrl,
                Size = request.Size,
                Seeders = request.Seeders,
                Leechers = request.Leechers,
                Quality = request.Quality,
                IndexerName = request.IndexerName ?? "Discovery"
            };

            _logger.LogInformation(
                "Starting movie download: Title={Title}, MagnetLink={Magnet}, DownloadUrl={Download}",
                request.Title, magnetLink.Length > 50 ? magnetLink[..50] + "..." : magnetLink, downloadUrl);

            // Use the discovery download method which handles library path resolution
            var download = await _downloadManager.StartDiscoveryMovieDownloadAsync(
                torrent,
                request.TmdbId,
                request.MovieTitle,
                request.Year,
                userId,
                cancellationToken).ConfigureAwait(false);

            return Ok(DownloadDto.FromEntity(download));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start movie download for {Title}", request.Title);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Starts a TV show episode download from discovery.
    /// </summary>
    /// <param name="request">The download request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Download started.</response>
    /// <response code="400">Invalid request.</response>
    /// <returns>The created download.</returns>
    [HttpPost("TvShows/Download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DownloadDto>> StartTvShowDownload(
        [FromBody, Required] DiscoveryTvDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.GetUserId();

            // Get raw values - they may be swapped (Prowlarr sends magnet in downloadUrl)
            var rawMagnet = request.MagnetLink ?? string.Empty;
            var rawDownload = request.DownloadUrl ?? string.Empty;

            // Detect and fix swapped fields by checking content
            string magnetLink;
            string? downloadUrl;
            if (rawDownload.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                magnetLink = rawDownload;
                downloadUrl = string.IsNullOrEmpty(rawMagnet) ? null : rawMagnet;
            }
            else if (rawMagnet.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                magnetLink = rawMagnet;
                downloadUrl = string.IsNullOrEmpty(rawDownload) ? null : rawDownload;
            }
            else
            {
                // Neither is a magnet link - use downloadUrl if available
                magnetLink = !string.IsNullOrEmpty(rawDownload) ? rawDownload : rawMagnet;
                downloadUrl = null;
            }

            var torrent = new TorrentSearchResult
            {
                Title = request.Title,
                MagnetLink = magnetLink,
                DownloadUrl = downloadUrl,
                Size = request.Size,
                Seeders = request.Seeders,
                Leechers = request.Leechers,
                Quality = request.Quality,
                IndexerName = request.IndexerName ?? "Discovery"
            };

            _logger.LogInformation(
                "Starting TV download: Title={Title}, ShowName={Show}, S{Season}E{Episode}, MagnetLink={Magnet}",
                request.Title, request.ShowName, request.SeasonNumber, request.EpisodeNumber,
                magnetLink.Length > 50 ? magnetLink[..50] + "..." : magnetLink);

            // Use the discovery download method which handles library path resolution
            var download = await _downloadManager.StartDiscoveryEpisodeDownloadAsync(
                torrent,
                request.TmdbId,
                request.ShowName,
                request.SeasonNumber,
                request.EpisodeNumber,
                userId,
                cancellationToken).ConfigureAwait(false);

            return Ok(DownloadDto.FromEntity(download));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TV show download for {Title}", request.Title);
            return BadRequest(ex.Message);
        }
    }
}
