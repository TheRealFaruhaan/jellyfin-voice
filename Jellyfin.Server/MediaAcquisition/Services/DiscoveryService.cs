using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Configuration;
using Jellyfin.Server.MediaAcquisition.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Trending;
using TMDbLib.Objects.TvShows;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for discovering movies and TV shows from TMDB.
/// </summary>
public class DiscoveryService : IDiscoveryService, IDisposable
{
    private const string TmdbImageBaseUrl = "https://image.tmdb.org/t/p/";
    private const string DefaultApiKey = "4219e299c89411838049ab0dab19ebd5";
    private const int CacheDurationMinutes = 30;

    private readonly IMemoryCache _cache;
    private readonly ILogger<DiscoveryService> _logger;
    private readonly MediaAcquisitionOptions _options;
    private readonly TMDbClient _tmdbClient;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryService"/> class.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The configuration options.</param>
    public DiscoveryService(
        IMemoryCache cache,
        ILogger<DiscoveryService> logger,
        IOptions<MediaAcquisitionOptions> options)
    {
        _cache = cache;
        _logger = logger;
        _options = options.Value;

        var apiKey = string.IsNullOrEmpty(_options.TmdbApiKey) ? DefaultApiKey : _options.TmdbApiKey;
        _tmdbClient = new TMDbClient(apiKey);
        _tmdbClient.ThrowApiExceptions = false;
    }

    /// <inheritdoc />
    public async Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> GetTrendingMoviesAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"trending-movies-{page}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryPagedResultDto<DiscoveryMovieDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var result = await _tmdbClient.GetTrendingMoviesAsync(TimeWindow.Week, page, _options.TmdbLanguage, cancellationToken).ConfigureAwait(false);

            var dto = new DiscoveryPagedResultDto<DiscoveryMovieDto>
            {
                Page = result.Page,
                TotalPages = result.TotalPages,
                TotalResults = result.TotalResults,
                Results = result.Results.Select(MapToMovieDto).ToList()
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trending movies from TMDB");
            return new DiscoveryPagedResultDto<DiscoveryMovieDto>();
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> GetPopularMoviesAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"popular-movies-{page}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryPagedResultDto<DiscoveryMovieDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var result = await _tmdbClient.GetMoviePopularListAsync(_options.TmdbLanguage, page, null, cancellationToken).ConfigureAwait(false);

            var dto = new DiscoveryPagedResultDto<DiscoveryMovieDto>
            {
                Page = result.Page,
                TotalPages = result.TotalPages,
                TotalResults = result.TotalResults,
                Results = result.Results.Select(MapToMovieDto).ToList()
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular movies from TMDB");
            return new DiscoveryPagedResultDto<DiscoveryMovieDto>();
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryPagedResultDto<DiscoveryMovieDto>> SearchMoviesAsync(string query, int? year = null, int page = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new DiscoveryPagedResultDto<DiscoveryMovieDto>();
        }

        var cacheKey = $"search-movies-{query}-{year}-{page}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryPagedResultDto<DiscoveryMovieDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var result = await _tmdbClient.SearchMovieAsync(query, page, false, year ?? 0, null, 0, cancellationToken).ConfigureAwait(false);

            var dto = new DiscoveryPagedResultDto<DiscoveryMovieDto>
            {
                Page = result.Page,
                TotalPages = result.TotalPages,
                TotalResults = result.TotalResults,
                Results = result.Results.Select(MapToMovieDto).ToList()
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching movies on TMDB");
            return new DiscoveryPagedResultDto<DiscoveryMovieDto>();
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryMovieDto?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"movie-details-{tmdbId}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryMovieDto? cached))
        {
            return cached;
        }

        try
        {
            var movie = await _tmdbClient.GetMovieAsync(tmdbId, TMDbLib.Objects.Movies.MovieMethods.Undefined, cancellationToken).ConfigureAwait(false);

            if (movie == null)
            {
                return null;
            }

            var dto = new DiscoveryMovieDto
            {
                Id = movie.Id,
                Title = movie.Title,
                OriginalTitle = movie.OriginalTitle,
                Overview = movie.Overview ?? string.Empty,
                PosterPath = GetImageUrl(movie.PosterPath),
                BackdropPath = GetImageUrl(movie.BackdropPath, "w1280"),
                ReleaseDate = movie.ReleaseDate,
                ReleaseYear = movie.ReleaseDate?.Year,
                VoteAverage = movie.VoteAverage,
                VoteCount = movie.VoteCount,
                Genres = movie.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                Runtime = movie.Runtime,
                ImdbId = movie.ImdbId,
                Tagline = movie.Tagline,
                Status = movie.Status
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movie details from TMDB for ID {TmdbId}", tmdbId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> GetTrendingTvShowsAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"trending-tvshows-{page}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryPagedResultDto<DiscoveryTvShowDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var result = await _tmdbClient.GetTrendingTvAsync(TimeWindow.Week, page, _options.TmdbLanguage, cancellationToken).ConfigureAwait(false);

            var dto = new DiscoveryPagedResultDto<DiscoveryTvShowDto>
            {
                Page = result.Page,
                TotalPages = result.TotalPages,
                TotalResults = result.TotalResults,
                Results = result.Results.Select(MapToTvShowDto).ToList()
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trending TV shows from TMDB");
            return new DiscoveryPagedResultDto<DiscoveryTvShowDto>();
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> GetPopularTvShowsAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"popular-tvshows-{page}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryPagedResultDto<DiscoveryTvShowDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var result = await _tmdbClient.GetTvShowPopularAsync(page, _options.TmdbLanguage, cancellationToken).ConfigureAwait(false);

            var dto = new DiscoveryPagedResultDto<DiscoveryTvShowDto>
            {
                Page = result.Page,
                TotalPages = result.TotalPages,
                TotalResults = result.TotalResults,
                Results = result.Results.Select(MapToTvShowDto).ToList()
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular TV shows from TMDB");
            return new DiscoveryPagedResultDto<DiscoveryTvShowDto>();
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryPagedResultDto<DiscoveryTvShowDto>> SearchTvShowsAsync(string query, int? year = null, int page = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new DiscoveryPagedResultDto<DiscoveryTvShowDto>();
        }

        var cacheKey = $"search-tvshows-{query}-{year}-{page}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryPagedResultDto<DiscoveryTvShowDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var result = await _tmdbClient.SearchTvShowAsync(query, _options.TmdbLanguage, page, false, year ?? 0, cancellationToken).ConfigureAwait(false);

            var dto = new DiscoveryPagedResultDto<DiscoveryTvShowDto>
            {
                Page = result.Page,
                TotalPages = result.TotalPages,
                TotalResults = result.TotalResults,
                Results = result.Results.Select(MapToTvShowDto).ToList()
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TV shows on TMDB");
            return new DiscoveryPagedResultDto<DiscoveryTvShowDto>();
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryTvShowDto?> GetTvShowDetailsAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tvshow-details-{tmdbId}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryTvShowDto? cached))
        {
            return cached;
        }

        try
        {
            var show = await _tmdbClient.GetTvShowAsync(tmdbId, TvShowMethods.ExternalIds, _options.TmdbLanguage, null, cancellationToken).ConfigureAwait(false);

            if (show == null)
            {
                return null;
            }

            var dto = new DiscoveryTvShowDto
            {
                Id = show.Id,
                Name = show.Name,
                OriginalName = show.OriginalName,
                Overview = show.Overview ?? string.Empty,
                PosterPath = GetImageUrl(show.PosterPath),
                BackdropPath = GetImageUrl(show.BackdropPath, "w1280"),
                FirstAirDate = show.FirstAirDate,
                FirstAirYear = show.FirstAirDate?.Year,
                VoteAverage = show.VoteAverage,
                VoteCount = show.VoteCount,
                Genres = show.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                NumberOfSeasons = show.NumberOfSeasons,
                NumberOfEpisodes = show.NumberOfEpisodes,
                Status = show.Status,
                Seasons = show.Seasons?.Select(s => new DiscoverySeasonDto
                {
                    Id = s.Id,
                    SeasonNumber = s.SeasonNumber,
                    Name = s.Name ?? $"Season {s.SeasonNumber}",
                    Overview = s.Overview ?? string.Empty,
                    PosterPath = GetImageUrl(s.PosterPath),
                    AirDate = s.AirDate,
                    EpisodeCount = s.EpisodeCount
                }).ToList() ?? new List<DiscoverySeasonDto>(),
                ExternalIds = show.ExternalIds != null ? new DiscoveryExternalIdsDto
                {
                    ImdbId = show.ExternalIds.ImdbId,
                    TvdbId = int.TryParse(show.ExternalIds.TvdbId, out var tvdbId) ? tvdbId : null
                } : null
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching TV show details from TMDB for ID {TmdbId}", tmdbId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DiscoverySeasonDto?> GetSeasonDetailsAsync(int tmdbId, int seasonNumber, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"season-details-{tmdbId}-{seasonNumber}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoverySeasonDto? cached))
        {
            return cached;
        }

        try
        {
            var season = await _tmdbClient.GetTvSeasonAsync(tmdbId, seasonNumber, TvSeasonMethods.Undefined, _options.TmdbLanguage, null, cancellationToken).ConfigureAwait(false);

            if (season == null)
            {
                return null;
            }

            var dto = new DiscoverySeasonDto
            {
                Id = season.Id ?? 0,
                SeasonNumber = season.SeasonNumber,
                Name = season.Name ?? $"Season {season.SeasonNumber}",
                Overview = season.Overview ?? string.Empty,
                PosterPath = GetImageUrl(season.PosterPath),
                AirDate = season.AirDate,
                EpisodeCount = season.Episodes?.Count ?? 0,
                Episodes = season.Episodes?.Select(e => new DiscoveryEpisodeDto
                {
                    Id = e.Id,
                    EpisodeNumber = e.EpisodeNumber,
                    SeasonNumber = e.SeasonNumber,
                    Name = e.Name ?? $"Episode {e.EpisodeNumber}",
                    Overview = e.Overview ?? string.Empty,
                    StillPath = GetImageUrl(e.StillPath),
                    AirDate = e.AirDate,
                    VoteAverage = e.VoteAverage,
                    Runtime = e.Runtime
                }).ToList() ?? new List<DiscoveryEpisodeDto>()
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching season details from TMDB for show {TmdbId} season {SeasonNumber}", tmdbId, seasonNumber);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryEpisodeDto?> GetEpisodeDetailsAsync(int tmdbId, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"episode-details-{tmdbId}-{seasonNumber}-{episodeNumber}-{_options.TmdbLanguage}";

        if (_cache.TryGetValue(cacheKey, out DiscoveryEpisodeDto? cached))
        {
            return cached;
        }

        try
        {
            var episode = await _tmdbClient.GetTvEpisodeAsync(tmdbId, seasonNumber, episodeNumber, TvEpisodeMethods.Undefined, _options.TmdbLanguage, null, cancellationToken).ConfigureAwait(false);

            if (episode == null)
            {
                return null;
            }

            var dto = new DiscoveryEpisodeDto
            {
                Id = episode.Id ?? 0,
                EpisodeNumber = episode.EpisodeNumber,
                SeasonNumber = episode.SeasonNumber,
                Name = episode.Name ?? $"Episode {episode.EpisodeNumber}",
                Overview = episode.Overview ?? string.Empty,
                StillPath = GetImageUrl(episode.StillPath),
                AirDate = episode.AirDate,
                VoteAverage = episode.VoteAverage,
                Runtime = episode.Runtime
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CacheDurationMinutes));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching episode details from TMDB for show {TmdbId} S{SeasonNumber}E{EpisodeNumber}", tmdbId, seasonNumber, episodeNumber);
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetImageUrl(string? path, string size = "w500")
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        return $"{TmdbImageBaseUrl}{size}{path}";
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
            _tmdbClient.Dispose();
        }

        _disposed = true;
    }

    private DiscoveryMovieDto MapToMovieDto(SearchMovie movie)
    {
        return new DiscoveryMovieDto
        {
            Id = movie.Id,
            Title = movie.Title,
            OriginalTitle = movie.OriginalTitle,
            Overview = movie.Overview ?? string.Empty,
            PosterPath = GetImageUrl(movie.PosterPath),
            BackdropPath = GetImageUrl(movie.BackdropPath, "w1280"),
            ReleaseDate = movie.ReleaseDate,
            ReleaseYear = movie.ReleaseDate?.Year,
            VoteAverage = movie.VoteAverage,
            VoteCount = movie.VoteCount
        };
    }

    private DiscoveryTvShowDto MapToTvShowDto(SearchTv show)
    {
        return new DiscoveryTvShowDto
        {
            Id = show.Id,
            Name = show.Name,
            OriginalName = show.OriginalName,
            Overview = show.Overview ?? string.Empty,
            PosterPath = GetImageUrl(show.PosterPath),
            BackdropPath = GetImageUrl(show.BackdropPath, "w1280"),
            FirstAirDate = show.FirstAirDate,
            FirstAirYear = show.FirstAirDate?.Year,
            VoteAverage = show.VoteAverage,
            VoteCount = show.VoteCount
        };
    }
}
