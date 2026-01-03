using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Configuration;
using Jellyfin.Server.MediaAcquisition.Data;
using Jellyfin.Server.MediaAcquisition.Data.Entities;
using Jellyfin.Server.MediaAcquisition.Events;
using Jellyfin.Server.MediaAcquisition.QBittorrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.MediaAcquisition.Workers;

/// <summary>
/// Background worker that polls qBittorrent for download progress updates.
/// </summary>
public class TorrentProgressWorker : BackgroundService
{
    private readonly IQBittorrentClient _qbClient;
    private readonly ITorrentDownloadRepository _repository;
    private readonly ITorrentProgressEventEmitter _eventEmitter;
    private readonly ILogger<TorrentProgressWorker> _logger;
    private readonly MediaAcquisitionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TorrentProgressWorker"/> class.
    /// </summary>
    /// <param name="qbClient">The qBittorrent client.</param>
    /// <param name="repository">The download repository.</param>
    /// <param name="eventEmitter">The event emitter for progress updates.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The configuration options.</param>
    public TorrentProgressWorker(
        IQBittorrentClient qbClient,
        ITorrentDownloadRepository repository,
        ITorrentProgressEventEmitter eventEmitter,
        ILogger<TorrentProgressWorker> logger,
        IOptions<MediaAcquisitionOptions> options)
    {
        _qbClient = qbClient;
        _repository = repository;
        _eventEmitter = eventEmitter;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Media Acquisition is disabled, TorrentProgressWorker will not run");
            return;
        }

        _logger.LogInformation("TorrentProgressWorker started, polling every {Interval} seconds", _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollProgressAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling torrent progress");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task PollProgressAsync(CancellationToken cancellationToken)
    {
        // Get active downloads from our repository
        var activeDownloads = await _repository.GetActiveDownloadsAsync(cancellationToken).ConfigureAwait(false);
        if (!activeDownloads.Any())
        {
            return;
        }

        // Get torrents from qBittorrent
        var torrents = await _qbClient.GetTorrentsAsync(_options.TorrentCategory, cancellationToken).ConfigureAwait(false);
        var torrentLookup = torrents.ToDictionary(t => t.Hash.ToLowerInvariant());

        foreach (var download in activeDownloads)
        {
            try
            {
                if (!torrentLookup.TryGetValue(download.TorrentHash.ToLowerInvariant(), out var torrent))
                {
                    // Torrent not found in qBittorrent - might have been deleted externally
                    _logger.LogWarning("Torrent not found in qBittorrent: {Hash}", download.TorrentHash);
                    continue;
                }

                var previousState = download.State;
                var previousProgress = download.Progress;

                // Update download with current values
                download.Progress = torrent.Progress * 100;
                download.DownloadedSize = torrent.Downloaded;
                download.TotalSize = torrent.Size > 0 ? torrent.Size : download.TotalSize;
                download.DownloadSpeed = torrent.DownloadSpeed;
                download.UploadSpeed = torrent.UploadSpeed;
                download.Seeders = torrent.Seeds;
                download.Leechers = torrent.Leechers;
                download.SavePath = torrent.SavePath;
                download.ContentPath = torrent.ContentPath;
                download.Eta = torrent.Eta > 0 ? torrent.Eta : null;

                // Map qBittorrent state to our state
                download.State = MapTorrentState(torrent.State, download.State);

                // Check for completion
                if (download.State == TorrentState.Completed || download.State == TorrentState.Seeding)
                {
                    if (download.CompletedAt == null)
                    {
                        download.CompletedAt = DateTime.UtcNow;
                        _logger.LogInformation("Download completed: {Name}", download.Name);
                    }
                }

                // Save updated download
                await _repository.UpdateAsync(download, cancellationToken).ConfigureAwait(false);

                // Emit progress event if state or progress changed significantly
                var progressChanged = Math.Abs(download.Progress - previousProgress) > 0.5;
                var stateChanged = download.State != previousState;

                if (progressChanged || stateChanged)
                {
                    await _eventEmitter.EmitProgressUpdateAsync(download, cancellationToken).ConfigureAwait(false);
                }

                if (stateChanged)
                {
                    _logger.LogDebug(
                        "Download state changed: {Name} {PreviousState} -> {NewState}",
                        download.Name, previousState, download.State);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating download: {Name}", download.Name);
            }
        }
    }

    private static TorrentState MapTorrentState(string qbState, TorrentState currentState)
    {
        // Don't override Importing or Imported states
        if (currentState == TorrentState.Importing || currentState == TorrentState.Imported)
        {
            return currentState;
        }

        return qbState.ToLowerInvariant() switch
        {
            "error" => TorrentState.Error,
            "pauseddl" or "pausedup" => TorrentState.Paused,
            "queueddl" or "queuedup" or "stalledDL" or "metadl" or "checkingdl" => TorrentState.Queued,
            "downloading" or "forceup" or "forcedl" => TorrentState.Downloading,
            "uploading" or "stalledup" => TorrentState.Seeding,
            "checkingup" or "checkingresumedata" => currentState,
            "allocating" => TorrentState.Queued,
            "moving" => currentState,
            _ => currentState
        };
    }
}
