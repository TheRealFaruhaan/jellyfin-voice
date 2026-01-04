using Jellyfin.Server.MediaAcquisition.Models;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for resolving library paths for downloads.
/// </summary>
public interface ILibraryPathResolver
{
    /// <summary>
    /// Gets the download path for a movie.
    /// Creates the folder structure if it doesn't exist.
    /// </summary>
    /// <param name="movieTitle">The movie title.</param>
    /// <param name="year">The release year.</param>
    /// <returns>The full path where the movie should be downloaded, or null if no movies library is configured.</returns>
    string? GetMovieDownloadPath(string movieTitle, int? year = null);

    /// <summary>
    /// Gets the download path for a TV show season or episode.
    /// Creates the folder structure if it doesn't exist.
    /// </summary>
    /// <param name="showName">The TV show name.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <returns>The full path where the content should be downloaded, or null if no TV library is configured.</returns>
    string? GetTvShowDownloadPath(string showName, int seasonNumber);

    /// <summary>
    /// Gets the root path for the movies library.
    /// </summary>
    /// <returns>The movies library path, or null if not configured.</returns>
    string? GetMoviesLibraryPath();

    /// <summary>
    /// Gets the root path for the TV shows library.
    /// </summary>
    /// <returns>The TV shows library path, or null if not configured.</returns>
    string? GetTvShowsLibraryPath();

    /// <summary>
    /// Gets disk space information for a given path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="requiredBytes">The required bytes for the download.</param>
    /// <returns>Disk space information.</returns>
    DiskSpaceDto GetDiskSpace(string path, long requiredBytes = 0);

    /// <summary>
    /// Checks if there is enough disk space for a download.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="requiredBytes">The required bytes for the download.</param>
    /// <returns>True if there is enough space.</returns>
    bool HasEnoughDiskSpace(string path, long requiredBytes);
}
