using System;
using System.IO;
using System.Linq;
using Jellyfin.Server.MediaAcquisition.Configuration;
using Jellyfin.Server.MediaAcquisition.Models;
using Jellyfin.Server.MediaAcquisition.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.MediaAcquisition.Services;

/// <summary>
/// Service for resolving library paths for downloads.
/// </summary>
public class LibraryPathResolver : ILibraryPathResolver
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryPathResolver> _logger;
    private readonly MediaAcquisitionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryPathResolver"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The configuration options.</param>
    public LibraryPathResolver(
        ILibraryManager libraryManager,
        ILogger<LibraryPathResolver> logger,
        IOptions<MediaAcquisitionOptions> options)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public string? GetMovieDownloadPath(string movieTitle, int? year = null)
    {
        var libraryPath = GetMoviesLibraryPath();
        if (string.IsNullOrEmpty(libraryPath))
        {
            _logger.LogWarning("No movies library path configured");
            return null;
        }

        var folderName = SearchPatternGenerator.CreateSafeFolderName(movieTitle, year);
        var fullPath = Path.Combine(libraryPath, folderName);

        try
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                _logger.LogInformation("Created movie download folder: {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create movie download folder: {Path}", fullPath);
            return null;
        }

        return fullPath;
    }

    /// <inheritdoc />
    public string? GetTvShowDownloadPath(string showName, int seasonNumber)
    {
        var libraryPath = GetTvShowsLibraryPath();
        if (string.IsNullOrEmpty(libraryPath))
        {
            _logger.LogWarning("No TV shows library path configured");
            return null;
        }

        var showFolderName = SearchPatternGenerator.CreateSafeFolderName(showName);
        var seasonFolderName = SearchPatternGenerator.CreateSeasonFolderName(seasonNumber);
        var fullPath = Path.Combine(libraryPath, showFolderName, seasonFolderName);

        try
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                _logger.LogInformation("Created TV show download folder: {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create TV show download folder: {Path}", fullPath);
            return null;
        }

        return fullPath;
    }

    /// <inheritdoc />
    public string? GetMoviesLibraryPath()
    {
        var folders = _libraryManager.GetVirtualFolders();
        var moviesFolder = folders.FirstOrDefault(f =>
            f.CollectionType == CollectionTypeOptions.movies);

        if (moviesFolder?.Locations?.Length > 0)
        {
            return moviesFolder.Locations[0];
        }

        // Fallback to default save path if configured
        if (!string.IsNullOrEmpty(_options.DefaultSavePath))
        {
            var moviesPath = Path.Combine(_options.DefaultSavePath, "Movies");
            if (!Directory.Exists(moviesPath))
            {
                try
                {
                    Directory.CreateDirectory(moviesPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create default movies folder: {Path}", moviesPath);
                    return null;
                }
            }

            return moviesPath;
        }

        return null;
    }

    /// <inheritdoc />
    public string? GetTvShowsLibraryPath()
    {
        var folders = _libraryManager.GetVirtualFolders();
        var tvFolder = folders.FirstOrDefault(f =>
            f.CollectionType == CollectionTypeOptions.tvshows);

        if (tvFolder?.Locations?.Length > 0)
        {
            return tvFolder.Locations[0];
        }

        // Fallback to default save path if configured
        if (!string.IsNullOrEmpty(_options.DefaultSavePath))
        {
            var tvPath = Path.Combine(_options.DefaultSavePath, "TV Shows");
            if (!Directory.Exists(tvPath))
            {
                try
                {
                    Directory.CreateDirectory(tvPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create default TV shows folder: {Path}", tvPath);
                    return null;
                }
            }

            return tvPath;
        }

        return null;
    }

    /// <inheritdoc />
    public DiskSpaceDto GetDiskSpace(string path, long requiredBytes = 0)
    {
        var dto = new DiskSpaceDto
        {
            MinimumRequiredBytes = _options.MinimumFreeSpaceBytes + requiredBytes,
            FormattedMinimumRequired = FormatSize(_options.MinimumFreeSpaceBytes + requiredBytes)
        };

        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? path);
            dto.FreeSpaceBytes = driveInfo.AvailableFreeSpace;
            dto.FormattedFreeSpace = FormatSize(driveInfo.AvailableFreeSpace);
            dto.HasEnoughSpace = driveInfo.AvailableFreeSpace >= dto.MinimumRequiredBytes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get disk space for path: {Path}", path);
            dto.FreeSpaceBytes = -1;
            dto.FormattedFreeSpace = "Unknown";
            dto.HasEnoughSpace = false;
        }

        return dto;
    }

    /// <inheritdoc />
    public bool HasEnoughDiskSpace(string path, long requiredBytes)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? path);
            var requiredTotal = _options.MinimumFreeSpaceBytes + requiredBytes;
            return driveInfo.AvailableFreeSpace >= requiredTotal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check disk space for path: {Path}", path);
            return false;
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
