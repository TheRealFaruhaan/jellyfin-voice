using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Models;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for discovering movies and TV shows from TMDB.
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Gets trending movies from TMDB.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of movies.</returns>
    Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> GetTrendingMoviesAsync(int page = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets popular movies from TMDB.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of movies.</returns>
    Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> GetPopularMoviesAsync(int page = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for movies on TMDB.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of movies.</returns>
    Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> SearchMoviesAsync(string query, int? year = null, int page = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets movie details from TMDB.
    /// </summary>
    /// <param name="tmdbId">The TMDB movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The movie details.</returns>
    Task<DiscoveryMovieDto?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trending TV shows from TMDB.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of TV shows.</returns>
    Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> GetTrendingTvShowsAsync(int page = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets popular TV shows from TMDB.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of TV shows.</returns>
    Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> GetPopularTvShowsAsync(int page = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for TV shows on TMDB.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of TV shows.</returns>
    Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> SearchTvShowsAsync(string query, int? year = null, int page = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets TV show details from TMDB.
    /// </summary>
    /// <param name="tmdbId">The TMDB TV show ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The TV show details.</returns>
    Task<DiscoveryTvShowDto?> GetTvShowDetailsAsync(int tmdbId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets season details from TMDB.
    /// </summary>
    /// <param name="tmdbId">The TMDB TV show ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The season details.</returns>
    Task<DiscoverySeasonDto?> GetSeasonDetailsAsync(int tmdbId, int seasonNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets episode details from TMDB.
    /// </summary>
    /// <param name="tmdbId">The TMDB TV show ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The episode details.</returns>
    Task<DiscoveryEpisodeDto?> GetEpisodeDetailsAsync(int tmdbId, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full URL for a TMDB image path.
    /// </summary>
    /// <param name="path">The image path from TMDB.</param>
    /// <param name="size">The image size (e.g., "w500", "original").</param>
    /// <returns>The full image URL.</returns>
    string? GetImageUrl(string? path, string size = "w500");
}
