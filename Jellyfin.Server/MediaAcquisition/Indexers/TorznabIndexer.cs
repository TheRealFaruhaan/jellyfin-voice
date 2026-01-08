using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
            // Build search query with common episode naming patterns
            var searchQuery = $"{seriesName} S{seasonNumber:D2}E{episodeNumber:D2}";

            // Build search URL - use 'search' type for maximum compatibility
            var url = BuildSearchUrl("search", new Dictionary<string, string>
            {
                ["q"] = searchQuery,
                ["cat"] = $"{CategoryTvHd},{CategoryTvSd},{CategoryTv4K}"
            }, providerIds);

            var response = await SendSearchRequestAsync(url, cancellationToken).ConfigureAwait(false);
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

            // Use 'search' type for maximum compatibility with all indexers
            var url = BuildSearchUrl("search", new Dictionary<string, string>
            {
                ["q"] = searchQuery,
                ["cat"] = $"{CategoryMoviesHd},{CategoryMoviesSd},{CategoryMovies4K}"
            }, providerIds);

            var response = await SendSearchRequestAsync(url, cancellationToken).ConfigureAwait(false);
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
            var baseUrl = _config.BaseUrl.TrimEnd('/');

            if (IsProwlarr)
            {
                // Use Prowlarr native API status endpoint with header auth
                var url = $"{baseUrl}/api/v1/indexer";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Api-Key", _config.ApiKey);

                var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            else
            {
                // Use standard Torznab caps endpoint
                var url = $"{baseUrl}/api?apikey={_config.ApiKey}&t=caps";
                var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to test connection to {Indexer}", Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TorrentSearchResult>> SearchByQueryAsync(
        string query,
        string category = "movie",
        CancellationToken cancellationToken = default)
    {
        var results = new List<TorrentSearchResult>();

        try
        {
            // Use 'search' type for maximum compatibility with all indexers
            var categories = category.Equals("tv", StringComparison.OrdinalIgnoreCase)
                ? $"{CategoryTvHd},{CategoryTvSd},{CategoryTv4K}"
                : $"{CategoryMoviesHd},{CategoryMoviesSd},{CategoryMovies4K}";

            var url = BuildSearchUrl("search", new Dictionary<string, string>
            {
                ["q"] = query,
                ["cat"] = categories
            }, null);

            var response = await SendSearchRequestAsync(url, cancellationToken).ConfigureAwait(false);
            results.AddRange(ParseResults(response));

            _logger.LogDebug(
                "Torznab query search for '{Query}' (category: {Category}) returned {Count} results from {Indexer}",
                query, category, results.Count, Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search {Indexer} with query '{Query}'", Name, query);
        }

        return results;
    }

    private bool IsProwlarr => _config.Name.Contains("Prowlarr", StringComparison.OrdinalIgnoreCase);

    private async Task<string> SendSearchRequestAsync(string url, CancellationToken cancellationToken)
    {
        if (IsProwlarr)
        {
            // Prowlarr requires API key as header
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _config.ApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Standard Torznab uses API key in URL
            return await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        }
    }

    private string BuildSearchUrl(string searchType, Dictionary<string, string> parameters, IDictionary<string, string>? providerIds)
    {
        var baseUrl = _config.BaseUrl.TrimEnd('/');

        string url;
        if (IsProwlarr)
        {
            // Use Prowlarr native API format (API key sent via header)
            var query = parameters.TryGetValue("q", out var q) ? q : string.Empty;
            url = $"{baseUrl}/api/v1/search?query={Uri.EscapeDataString(query)}";
        }
        else
        {
            // Use standard Torznab API format
            url = $"{baseUrl}/api?apikey={_config.ApiKey}&t={searchType}";

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
        }

        // Log URL for debugging
        _logger.LogInformation("Built search URL for {Indexer}: {Url}", Name, url);

        return url;
    }

    private IEnumerable<TorrentSearchResult> ParseResults(string response)
    {
        var results = new List<TorrentSearchResult>();

        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogWarning("Empty response received from indexer {Indexer}", Name);
            return results;
        }

        // Log first 500 chars for debugging (at Info level for troubleshooting)
        var preview = response.Length > 500 ? response[..500] : response;
        _logger.LogInformation("Response from {Indexer} (length: {Length}): {Preview}", Name, response.Length, preview);

        // Detect response type and parse accordingly
        var trimmedResponse = response.TrimStart();

        // Check for HTML error pages
        if (trimmedResponse.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            trimmedResponse.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Received HTML error page from {Indexer} - check indexer URL and API key configuration", Name);
            return results;
        }

        if (trimmedResponse.StartsWith('<'))
        {
            // XML response (Torznab)
            return ParseXmlResults(response);
        }
        else if (trimmedResponse.StartsWith('{') || trimmedResponse.StartsWith('['))
        {
            // JSON response (Prowlarr API format)
            return ParseJsonResults(response);
        }
        else
        {
            _logger.LogWarning("Unknown response format from {Indexer}. First 200 chars: {Preview}", Name, response.Length > 200 ? response[..200] : response);
            return results;
        }
    }

    private IEnumerable<TorrentSearchResult> ParseXmlResults(string xmlResponse)
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
            _logger.LogWarning(ex, "Failed to parse Torznab XML response from {Indexer}", Name);
        }

        return results;
    }

    private IEnumerable<TorrentSearchResult> ParseJsonResults(string jsonResponse)
    {
        var results = new List<TorrentSearchResult>();

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);

            // Handle array response (direct results)
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var result = ParseJsonItem(item);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
            }
            // Handle object response (wrapped results)
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                // Check for error response
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                {
                    _logger.LogWarning("Indexer {Indexer} returned error: {Error}", Name, errorProp.GetString());
                    return results;
                }

                // Try common result wrapper properties
                JsonElement? resultsArray = null;
                if (doc.RootElement.TryGetProperty("results", out var resultsProp))
                {
                    resultsArray = resultsProp;
                }
                else if (doc.RootElement.TryGetProperty("items", out var itemsProp))
                {
                    resultsArray = itemsProp;
                }
                else if (doc.RootElement.TryGetProperty("data", out var dataProp))
                {
                    resultsArray = dataProp;
                }

                if (resultsArray?.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in resultsArray.Value.EnumerateArray())
                    {
                        var result = ParseJsonItem(item);
                        if (result != null)
                        {
                            results.Add(result);
                        }
                    }
                }
            }

            _logger.LogDebug("Parsed {Count} results from JSON response from {Indexer}", results.Count, Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from {Indexer}", Name);
        }

        return results;
    }

    private TorrentSearchResult? ParseJsonItem(JsonElement item)
    {
        try
        {
            var result = new TorrentSearchResult
            {
                IndexerName = Name
            };

            // Title (multiple possible property names)
            result.Title = GetJsonString(item, "title", "name", "Title", "Name") ?? string.Empty;

            // Get raw values from JSON - Prowlarr may have them swapped
            var rawMagnetUrl = GetJsonString(item, "magnetUrl", "magnetLink", "magnet", "MagnetUrl", "MagnetLink");
            var rawDownloadUrl = GetJsonString(item, "downloadUrl", "link", "guid", "DownloadUrl", "Link");

            // Prowlarr returns downloadUrl with actual magnet link, and magnetUrl with HTTP download link
            // Detect and fix this by checking content
            if (!string.IsNullOrEmpty(rawDownloadUrl) && rawDownloadUrl.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                result.MagnetLink = rawDownloadUrl;
                result.DownloadUrl = rawMagnetUrl; // This is the HTTP .torrent download URL
            }
            else if (!string.IsNullOrEmpty(rawMagnetUrl) && rawMagnetUrl.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                result.MagnetLink = rawMagnetUrl;
                result.DownloadUrl = rawDownloadUrl;
            }
            else
            {
                // Neither is a magnet link, use as-is
                result.MagnetLink = rawMagnetUrl ?? string.Empty;
                result.DownloadUrl = rawDownloadUrl;
            }

            // Info hash
            result.InfoHash = GetJsonString(item, "infoHash", "hash", "InfoHash", "Hash");
            if (!string.IsNullOrEmpty(result.InfoHash) && string.IsNullOrEmpty(result.MagnetLink))
            {
                result.MagnetLink = $"magnet:?xt=urn:btih:{result.InfoHash}";
            }

            // Size
            result.Size = GetJsonLong(item, "size", "Size") ?? 0;

            // Seeders
            result.Seeders = GetJsonInt(item, "seeders", "Seeders", "seed") ?? 0;

            // Leechers
            result.Leechers = GetJsonInt(item, "leechers", "Leechers", "leech", "peers", "Peers") ?? 0;

            // Details URL
            result.DetailsUrl = GetJsonString(item, "infoUrl", "detailsUrl", "details", "guid", "InfoUrl", "DetailsUrl");

            // Publish date
            var dateStr = GetJsonString(item, "publishDate", "pubDate", "date", "PublishDate", "PubDate");
            if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
            {
                result.PublishDate = date;
            }

            // Parse quality from title
            result.Quality = ParseQuality(result.Title);
            result.Source = ParseSource(result.Title);
            result.Codec = ParseCodec(result.Title);

            // Only return if we have a valid download method
            if (!string.IsNullOrEmpty(result.MagnetLink) || !string.IsNullOrEmpty(result.DownloadUrl))
            {
                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse JSON torrent item");
            return null;
        }
    }

    private static string? GetJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
        }

        return null;
    }

    private static int? GetJsonInt(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var intVal))
                {
                    return intVal;
                }

                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsedVal))
                {
                    return parsedVal;
                }
            }
        }

        return null;
    }

    private static long? GetJsonLong(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var longVal))
                {
                    return longVal;
                }

                if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var parsedVal))
                {
                    return parsedVal;
                }
            }
        }

        return null;
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
