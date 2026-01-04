using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Jellyfin.Server.MediaAcquisition.Utils;

/// <summary>
/// Utility class for generating torrent search patterns.
/// </summary>
public static partial class SearchPatternGenerator
{
    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();

    /// <summary>
    /// Generates search patterns for a movie.
    /// </summary>
    /// <param name="title">The movie title.</param>
    /// <param name="year">The release year (optional).</param>
    /// <returns>A list of search patterns.</returns>
    public static IEnumerable<string> GenerateMoviePatterns(string title, int? year = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            yield break;
        }

        var cleanTitle = CleanTitle(title);
        var dottedTitle = cleanTitle.Replace(" ", ".", StringComparison.Ordinal);

        // Exact title
        yield return cleanTitle;

        // Dotted format
        yield return dottedTitle;

        // With year
        if (year.HasValue)
        {
            yield return $"{cleanTitle} {year}";
            yield return $"{dottedTitle}.{year}";
        }
    }

    /// <summary>
    /// Generates search patterns for a TV show season.
    /// </summary>
    /// <param name="seriesName">The series name.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <returns>A list of search patterns.</returns>
    public static IEnumerable<string> GenerateSeasonPatterns(string seriesName, int seasonNumber)
    {
        if (string.IsNullOrWhiteSpace(seriesName))
        {
            yield break;
        }

        var cleanName = CleanTitle(seriesName);
        var dottedName = cleanName.Replace(" ", ".", StringComparison.Ordinal);

        // "Show Name Season 10"
        yield return $"{cleanName} Season {seasonNumber}";

        // "Show.Name.S10"
        yield return $"{dottedName}.S{seasonNumber:D2}";

        // "Show.Name.Season.10"
        yield return $"{dottedName}.Season.{seasonNumber}";

        // "Show Name S10"
        yield return $"{cleanName} S{seasonNumber:D2}";

        // "Show Name Complete Season 10"
        yield return $"{cleanName} Complete Season {seasonNumber}";
    }

    /// <summary>
    /// Generates search patterns for a TV show episode.
    /// </summary>
    /// <param name="seriesName">The series name.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <returns>A list of search patterns.</returns>
    public static IEnumerable<string> GenerateEpisodePatterns(string seriesName, int seasonNumber, int episodeNumber)
    {
        if (string.IsNullOrWhiteSpace(seriesName))
        {
            yield break;
        }

        var cleanName = CleanTitle(seriesName);
        var dottedName = cleanName.Replace(" ", ".", StringComparison.Ordinal);
        var seasonCode = $"S{seasonNumber:D2}";
        var episodeCode = $"E{episodeNumber:D2}";

        // "Show Name S01E05"
        yield return $"{cleanName} {seasonCode}{episodeCode}";

        // "Show.Name.S01E05"
        yield return $"{dottedName}.{seasonCode}{episodeCode}";

        // "Show Name Season 1 Episode 5"
        yield return $"{cleanName} Season {seasonNumber} Episode {episodeNumber}";

        // "Show.Name.Season.1.Episode.5"
        yield return $"{dottedName}.Season.{seasonNumber}.Episode.{episodeNumber}";

        // "Show Name 1x05"
        yield return $"{cleanName} {seasonNumber}x{episodeNumber:D2}";
    }

    /// <summary>
    /// Cleans a title for use in search patterns.
    /// Removes special characters and normalizes whitespace.
    /// </summary>
    /// <param name="title">The title to clean.</param>
    /// <returns>The cleaned title.</returns>
    public static string CleanTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        // Remove special characters except alphanumeric and spaces
        var cleaned = NonAlphanumericRegex().Replace(title, " ");

        // Normalize multiple spaces to single space
        cleaned = MultipleSpacesRegex().Replace(cleaned, " ");

        return cleaned.Trim();
    }

    /// <summary>
    /// Creates a safe folder name from a title.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="year">Optional year to append.</param>
    /// <returns>A safe folder name.</returns>
    public static string CreateSafeFolderName(string title, int? year = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Unknown";
        }

        // Remove characters that are invalid in folder names
        var invalid = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        var safeName = title;

        foreach (var c in invalid)
        {
            safeName = safeName.Replace(c, ' ');
        }

        // Normalize whitespace
        safeName = MultipleSpacesRegex().Replace(safeName, " ").Trim();

        // Append year if provided
        if (year.HasValue)
        {
            safeName = $"{safeName} ({year})";
        }

        return safeName;
    }

    /// <summary>
    /// Creates a safe season folder name.
    /// </summary>
    /// <param name="seasonNumber">The season number.</param>
    /// <returns>A season folder name (e.g., "Season 01").</returns>
    public static string CreateSeasonFolderName(int seasonNumber)
    {
        return $"Season {seasonNumber:D2}";
    }
}
