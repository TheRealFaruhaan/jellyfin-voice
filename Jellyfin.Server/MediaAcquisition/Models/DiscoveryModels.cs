using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Server.MediaAcquisition.Models;

/// <summary>
/// Represents a movie from TMDB discovery.
/// </summary>
public class DiscoveryMovieDto
{
    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original title.
    /// </summary>
    [JsonPropertyName("originalTitle")]
    public string OriginalTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the movie overview.
    /// </summary>
    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the poster path.
    /// </summary>
    [JsonPropertyName("posterPath")]
    public string? PosterPath { get; set; }

    /// <summary>
    /// Gets or sets the backdrop path.
    /// </summary>
    [JsonPropertyName("backdropPath")]
    public string? BackdropPath { get; set; }

    /// <summary>
    /// Gets or sets the release date.
    /// </summary>
    [JsonPropertyName("releaseDate")]
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    [JsonPropertyName("releaseYear")]
    public int? ReleaseYear { get; set; }

    /// <summary>
    /// Gets or sets the vote average.
    /// </summary>
    [JsonPropertyName("voteAverage")]
    public double VoteAverage { get; set; }

    /// <summary>
    /// Gets or sets the vote count.
    /// </summary>
    [JsonPropertyName("voteCount")]
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    /// <summary>
    /// Gets or sets the runtime in minutes.
    /// </summary>
    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    /// <summary>
    /// Gets or sets the IMDB ID.
    /// </summary>
    [JsonPropertyName("imdbId")]
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the tagline.
    /// </summary>
    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    /// <summary>
    /// Gets or sets the status (Released, In Production, etc.).
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Represents a TV show from TMDB discovery.
/// </summary>
public class DiscoveryTvShowDto
{
    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the show name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original name.
    /// </summary>
    [JsonPropertyName("originalName")]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the show overview.
    /// </summary>
    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the poster path.
    /// </summary>
    [JsonPropertyName("posterPath")]
    public string? PosterPath { get; set; }

    /// <summary>
    /// Gets or sets the backdrop path.
    /// </summary>
    [JsonPropertyName("backdropPath")]
    public string? BackdropPath { get; set; }

    /// <summary>
    /// Gets or sets the first air date.
    /// </summary>
    [JsonPropertyName("firstAirDate")]
    public DateTime? FirstAirDate { get; set; }

    /// <summary>
    /// Gets or sets the first air year.
    /// </summary>
    [JsonPropertyName("firstAirYear")]
    public int? FirstAirYear { get; set; }

    /// <summary>
    /// Gets or sets the vote average.
    /// </summary>
    [JsonPropertyName("voteAverage")]
    public double VoteAverage { get; set; }

    /// <summary>
    /// Gets or sets the vote count.
    /// </summary>
    [JsonPropertyName("voteCount")]
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of seasons.
    /// </summary>
    [JsonPropertyName("numberOfSeasons")]
    public int NumberOfSeasons { get; set; }

    /// <summary>
    /// Gets or sets the number of episodes.
    /// </summary>
    [JsonPropertyName("numberOfEpisodes")]
    public int NumberOfEpisodes { get; set; }

    /// <summary>
    /// Gets or sets the status (Returning Series, Ended, etc.).
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the seasons.
    /// </summary>
    [JsonPropertyName("seasons")]
    public List<DiscoverySeasonDto> Seasons { get; set; } = new();

    /// <summary>
    /// Gets or sets the external IDs.
    /// </summary>
    [JsonPropertyName("externalIds")]
    public DiscoveryExternalIdsDto? ExternalIds { get; set; }
}

/// <summary>
/// Represents a season from TMDB discovery.
/// </summary>
public class DiscoverySeasonDto
{
    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the season name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season overview.
    /// </summary>
    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the poster path.
    /// </summary>
    [JsonPropertyName("posterPath")]
    public string? PosterPath { get; set; }

    /// <summary>
    /// Gets or sets the air date.
    /// </summary>
    [JsonPropertyName("airDate")]
    public DateTime? AirDate { get; set; }

    /// <summary>
    /// Gets or sets the episode count.
    /// </summary>
    [JsonPropertyName("episodeCount")]
    public int EpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the episodes.
    /// </summary>
    [JsonPropertyName("episodes")]
    public List<DiscoveryEpisodeDto> Episodes { get; set; } = new();
}

/// <summary>
/// Represents an episode from TMDB discovery.
/// </summary>
public class DiscoveryEpisodeDto
{
    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the episode overview.
    /// </summary>
    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the still path (episode image).
    /// </summary>
    [JsonPropertyName("stillPath")]
    public string? StillPath { get; set; }

    /// <summary>
    /// Gets or sets the air date.
    /// </summary>
    [JsonPropertyName("airDate")]
    public DateTime? AirDate { get; set; }

    /// <summary>
    /// Gets or sets the vote average.
    /// </summary>
    [JsonPropertyName("voteAverage")]
    public double VoteAverage { get; set; }

    /// <summary>
    /// Gets or sets the runtime in minutes.
    /// </summary>
    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    /// <summary>
    /// Gets or sets the episode code (e.g., "S01E05").
    /// </summary>
    [JsonPropertyName("episodeCode")]
    public string EpisodeCode => $"S{SeasonNumber:D2}E{EpisodeNumber:D2}";
}

/// <summary>
/// Represents external IDs for a TV show.
/// </summary>
public class DiscoveryExternalIdsDto
{
    /// <summary>
    /// Gets or sets the IMDB ID.
    /// </summary>
    [JsonPropertyName("imdbId")]
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the TVDB ID.
    /// </summary>
    [JsonPropertyName("tvdbId")]
    public int? TvdbId { get; set; }
}

/// <summary>
/// Represents a paginated result from TMDB.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class DiscoveryPagedResultDto<T>
{
    /// <summary>
    /// Gets or sets the current page.
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the total pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the total results.
    /// </summary>
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    /// <summary>
    /// Gets or sets the results.
    /// </summary>
    [JsonPropertyName("results")]
    public List<T> Results { get; set; } = new();
}

/// <summary>
/// Represents disk space information.
/// </summary>
public class DiskSpaceDto
{
    /// <summary>
    /// Gets or sets the free space in bytes.
    /// </summary>
    [JsonPropertyName("freeSpaceBytes")]
    public long FreeSpaceBytes { get; set; }

    /// <summary>
    /// Gets or sets the formatted free space string.
    /// </summary>
    [JsonPropertyName("formattedFreeSpace")]
    public string FormattedFreeSpace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether there is enough space for the download.
    /// </summary>
    [JsonPropertyName("hasEnoughSpace")]
    public bool HasEnoughSpace { get; set; }

    /// <summary>
    /// Gets or sets the minimum required space in bytes.
    /// </summary>
    [JsonPropertyName("minimumRequiredBytes")]
    public long MinimumRequiredBytes { get; set; }

    /// <summary>
    /// Gets or sets the formatted minimum required space string.
    /// </summary>
    [JsonPropertyName("formattedMinimumRequired")]
    public string FormattedMinimumRequired { get; set; } = string.Empty;
}

/// <summary>
/// Request to start a discovery movie download.
/// </summary>
public class StartDiscoveryMovieDownloadRequest
{
    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the magnet link.
    /// </summary>
    public string MagnetLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the torrent title.
    /// </summary>
    public string TorrentTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the quality.
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the indexer name.
    /// </summary>
    public string? IndexerName { get; set; }
}

/// <summary>
/// Request to start a discovery season download.
/// </summary>
public class StartDiscoverySeasonDownloadRequest
{
    /// <summary>
    /// Gets or sets the TMDB ID of the TV show.
    /// </summary>
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the show name.
    /// </summary>
    public string ShowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the magnet link.
    /// </summary>
    public string MagnetLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the torrent title.
    /// </summary>
    public string TorrentTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the quality.
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the indexer name.
    /// </summary>
    public string? IndexerName { get; set; }
}

/// <summary>
/// Request to start a discovery episode download.
/// </summary>
public class StartDiscoveryEpisodeDownloadRequest
{
    /// <summary>
    /// Gets or sets the TMDB ID of the TV show.
    /// </summary>
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the show name.
    /// </summary>
    public string ShowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the magnet link.
    /// </summary>
    public string MagnetLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the torrent title.
    /// </summary>
    public string TorrentTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the quality.
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the indexer name.
    /// </summary>
    public string? IndexerName { get; set; }
}

/// <summary>
/// Request to search torrents with a custom query.
/// </summary>
public class CustomTorrentSearchRequest
{
    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category (movie or tv).
    /// </summary>
    public string Category { get; set; } = "movie";
}
