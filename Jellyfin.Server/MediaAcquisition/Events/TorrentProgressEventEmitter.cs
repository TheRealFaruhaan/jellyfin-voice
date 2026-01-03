using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Data.Entities;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.MediaAcquisition.Events;

/// <summary>
/// Emits torrent progress events via WebSocket.
/// </summary>
public class TorrentProgressEventEmitter : ITorrentProgressEventEmitter
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<TorrentProgressEventEmitter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TorrentProgressEventEmitter"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="logger">The logger.</param>
    public TorrentProgressEventEmitter(
        ISessionManager sessionManager,
        ILogger<TorrentProgressEventEmitter> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EmitProgressUpdateAsync(TorrentDownload download, CancellationToken cancellationToken = default)
    {
        try
        {
            var progressData = new TorrentProgressUpdate
            {
                Id = download.Id,
                TorrentHash = download.TorrentHash,
                Name = download.Name,
                MediaType = download.MediaType.ToString(),
                SeriesId = download.SeriesId,
                SeriesName = download.SeriesName,
                SeasonNumber = download.SeasonNumber,
                EpisodeNumber = download.EpisodeNumber,
                MovieId = download.MovieId,
                MovieName = download.MovieName,
                State = download.State.ToString(),
                Progress = download.Progress,
                TotalSize = download.TotalSize,
                DownloadedSize = download.DownloadedSize,
                DownloadSpeed = download.DownloadSpeed,
                UploadSpeed = download.UploadSpeed,
                Seeders = download.Seeders,
                Leechers = download.Leechers,
                Eta = download.Eta,
                Quality = download.Quality,
                ErrorMessage = download.ErrorMessage
            };

            // Send to all admin sessions
            await _sessionManager.SendMessageToAdminSessions(
                SessionMessageType.RefreshProgress,
                progressData,
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Emitted progress update for {Name}: {Progress:F1}%", download.Name, download.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit progress update for {Name}", download.Name);
        }
    }
}

/// <summary>
/// Data structure for torrent progress WebSocket updates.
/// </summary>
public class TorrentProgressUpdate
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
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
