using System;
using Jellyfin.Server.MediaAcquisition.Data.Entities;

namespace Jellyfin.Server.MediaAcquisition.Models;

/// <summary>
/// Data transfer object for torrent downloads.
/// </summary>
public class DownloadDto
{
    /// <summary>
    /// Gets or sets the download ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the torrent hash.
    /// </summary>
    public string TorrentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the series ID.
    /// </summary>
    public Guid? SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the series name.
    /// </summary>
    public string? SeriesName { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the movie ID.
    /// </summary>
    public Guid? MovieId { get; set; }

    /// <summary>
    /// Gets or sets the movie name.
    /// </summary>
    public string? MovieName { get; set; }

    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the progress (0-100).
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Gets or sets the total size in bytes.
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the downloaded size in bytes.
    /// </summary>
    public long DownloadedSize { get; set; }

    /// <summary>
    /// Gets or sets the download speed in bytes/second.
    /// </summary>
    public long DownloadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the upload speed in bytes/second.
    /// </summary>
    public long UploadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the number of seeders.
    /// </summary>
    public int Seeders { get; set; }

    /// <summary>
    /// Gets or sets the number of leechers.
    /// </summary>
    public int Leechers { get; set; }

    /// <summary>
    /// Gets or sets the ETA in seconds.
    /// </summary>
    public long? Eta { get; set; }

    /// <summary>
    /// Gets or sets the quality.
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the indexer name.
    /// </summary>
    public string? IndexerName { get; set; }

    /// <summary>
    /// Gets or sets the date added.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Gets or sets the date completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the date imported.
    /// </summary>
    public DateTime? ImportedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets the formatted size string.
    /// </summary>
    public string FormattedSize => FormatSize(TotalSize);

    /// <summary>
    /// Gets the formatted download speed string.
    /// </summary>
    public string FormattedSpeed => FormatSize(DownloadSpeed) + "/s";

    /// <summary>
    /// Gets the episode code (e.g., "S01E05").
    /// </summary>
    public string? EpisodeCode => SeasonNumber.HasValue && EpisodeNumber.HasValue
        ? $"S{SeasonNumber:D2}E{EpisodeNumber:D2}"
        : null;

    /// <summary>
    /// Creates a DTO from an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The DTO.</returns>
    public static DownloadDto FromEntity(TorrentDownload entity)
    {
        return new DownloadDto
        {
            Id = entity.Id,
            TorrentHash = entity.TorrentHash,
            Name = entity.Name,
            MediaType = entity.MediaType.ToString(),
            SeriesId = entity.SeriesId,
            SeriesName = entity.SeriesName,
            SeasonNumber = entity.SeasonNumber,
            EpisodeNumber = entity.EpisodeNumber,
            MovieId = entity.MovieId,
            MovieName = entity.MovieName,
            State = entity.State.ToString(),
            Progress = entity.Progress,
            TotalSize = entity.TotalSize,
            DownloadedSize = entity.DownloadedSize,
            DownloadSpeed = entity.DownloadSpeed,
            UploadSpeed = entity.UploadSpeed,
            Seeders = entity.Seeders,
            Leechers = entity.Leechers,
            Eta = entity.Eta,
            Quality = entity.Quality,
            IndexerName = entity.IndexerName,
            AddedAt = entity.AddedAt,
            CompletedAt = entity.CompletedAt,
            ImportedAt = entity.ImportedAt,
            ErrorMessage = entity.ErrorMessage
        };
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
