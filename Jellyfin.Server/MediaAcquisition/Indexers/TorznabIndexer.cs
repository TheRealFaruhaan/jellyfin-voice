using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jellyfin.Server.MediaAcquisition.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.MediaAcquisition.Indexers;

/// <summary>
/// Torznab-compatible indexer (Prowlarr, Jackett, etc.).
/// </summary>
public class TorznabIndexer : ITorrentIndexer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TorznabIndexer> _logger;
    private readonly TorrentIndexerConfig _config;

    // Torznab category IDs
    private const int CategoryTvHd = 5040;
    private const int CategoryTvSd = 5030;
    private const int CategoryMoviesHd = 2040;
    private const int CategoryMoviesSd = 2030;
    private const int CategoryMovies4K = 2045;
    private const int CategoryTv4K = 5045;

    /// <summary>
    /// Initializes a new instance of the <see cref="TorznabIndexer"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="config">The indexer configuration.</param>
    public TorznabIndexer(
        HttpClient httpClient,
        ILogger<TorznabIndexer> logger,
        TorrentIndexerConfig config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    /// <inheritdoc />
    public string Name => _config.Name;

    /// <inheritdoc />
    public bool IsEnabled => _config.Enabled;

    /// <inheritdoc />
    public int Priority => _config.Priority;

    /// <inheritdoc />
    public async Task<IEnumerable<TorrentSearchResult>> SearchEpisodeAsync(
        string seriesName,
        int seasonNumber,
        int episodeNumber,
        IDictionary<string, string>? providerIds = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TorrentSearchResult>();

        try
        {
            // Build search URL
            var url = BuildSearchUrl("tvsearch", new Dictionary<string, string>
            {
                ["q"] = seriesName,
                ["season"] = seasonNumber.ToString(CultureInfo.InvariantCulture),
                ["ep"] = episodeNumber.ToString(CultureInfo.InvariantCulture),
                ["cat"] = $"{CategoryTvHd},{CategoryTvSd},{CategoryTv4K}"
            }, providerIds);

            var response = await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            results.AddRange(ParseResults(response));

            _logger.LogDebug(
                "Torznab search for {Series} S{Season:D2}E{Episode:D2} returned {Count} results from {Indexer}",
                seriesName, seasonNumber, episodeNumber, results.Count, Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search {Indexer} for episode", Name);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TorrentSearchResult>> SearchMovieAsync(
        string movieName,
        int? year = null,
        IDictionary<string, string>? providerIds = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TorrentSearchResult>();

        try
        {
            var searchQuery = year.HasValue ? $"{movieName} {year}" : movieName;

            var url = BuildSearchUrl("movie", new Dictionary<string, string>
            {
                ["q"] = searchQuery,
                ["cat"] = $"{CategoryMoviesHd},{CategoryMoviesSd},{CategoryMovies4K}"
            }, providerIds);

            var response = await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            results.AddRange(ParseResults(response));

            _logger.LogDebug(
                "Torznab search for movie '{Movie}' ({Year}) returned {Count} results from {Indexer}",
                movieName, year, results.Count, Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search {Indexer} for movie", Name);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_config.BaseUrl.TrimEnd('/')}/api?apikey={_config.ApiKey}&t=caps";
            var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to test connection to {Indexer}", Name);
            return false;
        }
    }

    private string BuildSearchUrl(string searchType, Dictionary<string, string> parameters, IDictionary<string, string>? providerIds)
    {
        var url = $"{_config.BaseUrl.TrimEnd('/')}/api?apikey={_config.ApiKey}&t={searchType}";

        foreach (var param in parameters)
        {
            url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
        }

        // Add provider IDs if available
        if (providerIds != null)
        {
            if (providerIds.TryGetValue("Tvdb", out var tvdbId))
            {
                url += $"&tvdbid={tvdbId}";
            }

            if (providerIds.TryGetValue("Tmdb", out var tmdbId))
            {
                url += $"&tmdbid={tmdbId}";
            }

            if (providerIds.TryGetValue("Imdb", out var imdbId))
            {
                url += $"&imdbid={imdbId}";
            }
        }

        return url;
    }

    private IEnumerable<TorrentSearchResult> ParseResults(string xmlResponse)
    {
        var results = new List<TorrentSearchResult>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            var torznabNs = XNamespace.Get("http://torznab.com/schemas/2015/feed");

            var items = doc.Descendants(ns + "item");

            foreach (var item in items)
            {
                try
                {
                    var result = new TorrentSearchResult
                    {
                        Title = item.Element(ns + "title")?.Value ?? string.Empty,
                        IndexerName = Name
                    };

                    // Get enclosure for magnet/download URL
                    var enclosure = item.Element(ns + "enclosure");
                    if (enclosure != null)
                    {
                        var enclosureUrl = enclosure.Attribute("url")?.Value;
                        if (!string.IsNullOrEmpty(enclosureUrl))
                        {
                            if (enclosureUrl.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
                            {
                                result.MagnetLink = enclosureUrl;
                            }
                            else
                            {
                                result.DownloadUrl = enclosureUrl;
                            }
                        }

                        if (long.TryParse(enclosure.Attribute("length")?.Value, out var length))
                        {
                            result.Size = length;
                        }
                    }

                    // Parse torznab attributes
                    foreach (var attr in item.Elements(torznabNs + "attr"))
                    {
                        var name = attr.Attribute("name")?.Value;
                        var value = attr.Attribute("value")?.Value;

                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
                        {
                            continue;
                        }

                        switch (name.ToLowerInvariant())
                        {
                            case "seeders":
                                if (int.TryParse(value, out var seeders))
                                {
                                    result.Seeders = seeders;
                                }

                                break;
                            case "peers":
                            case "leechers":
                                if (int.TryParse(value, out var leechers))
                                {
                                    result.Leechers = leechers;
                                }

                                break;
                            case "size":
                                if (long.TryParse(value, out var size))
                                {
                                    result.Size = size;
                                }

                                break;
                            case "magneturl":
                                result.MagnetLink = value;
                                break;
                            case "infohash":
                                result.InfoHash = value;
                                if (string.IsNullOrEmpty(result.MagnetLink))
                                {
                                    result.MagnetLink = $"magnet:?xt=urn:btih:{value}";
                                }

                                break;
                        }
                    }

                    // Parse quality from title
                    result.Quality = ParseQuality(result.Title);
                    result.Source = ParseSource(result.Title);
                    result.Codec = ParseCodec(result.Title);

                    // Get link/details URL
                    result.DetailsUrl = item.Element(ns + "link")?.Value;

                    // Get publish date
                    var pubDate = item.Element(ns + "pubDate")?.Value;
                    if (!string.IsNullOrEmpty(pubDate) && DateTime.TryParse(pubDate, out var date))
                    {
                        result.PublishDate = date;
                    }

                    // Only add if we have a magnet link or download URL
                    if (!string.IsNullOrEmpty(result.MagnetLink) || !string.IsNullOrEmpty(result.DownloadUrl))
                    {
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse torrent result item");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Torznab XML response");
        }

        return results;
    }

    private static string? ParseQuality(string title)
    {
        var qualityPatterns = new[] { "2160p", "4K", "1080p", "720p", "480p", "576p" };
        foreach (var pattern in qualityPatterns)
        {
            if (title.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return pattern.ToUpperInvariant();
            }
        }

        return null;
    }

    private static string? ParseSource(string title)
    {
        var sourcePatterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BluRay"] = "BluRay",
            ["Blu-Ray"] = "BluRay",
            ["BDRip"] = "BluRay",
            ["BRRip"] = "BluRay",
            ["WEB-DL"] = "WEB-DL",
            ["WEBDL"] = "WEB-DL",
            ["WEBRip"] = "WEBRip",
            ["HDTV"] = "HDTV",
            ["DVDRip"] = "DVD",
            ["DVDR"] = "DVD"
        };

        foreach (var pattern in sourcePatterns)
        {
            if (title.Contains(pattern.Key, StringComparison.OrdinalIgnoreCase))
            {
                return pattern.Value;
            }
        }

        return null;
    }

    private static string? ParseCodec(string title)
    {
        var codecPatterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["x265"] = "x265",
            ["HEVC"] = "HEVC",
            ["H.265"] = "HEVC",
            ["x264"] = "x264",
            ["H.264"] = "x264",
            ["AVC"] = "x264",
            ["XviD"] = "XviD"
        };

        foreach (var pattern in codecPatterns)
        {
            if (title.Contains(pattern.Key, StringComparison.OrdinalIgnoreCase))
            {
                return pattern.Value;
            }
        }

        return null;
    }
}
