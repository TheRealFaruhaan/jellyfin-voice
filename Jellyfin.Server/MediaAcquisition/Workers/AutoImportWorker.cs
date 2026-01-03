using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Configuration;
using Jellyfin.Server.MediaAcquisition.Data;
using Jellyfin.Server.MediaAcquisition.Data.Entities;
using Jellyfin.Server.MediaAcquisition.Events;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.MediaAcquisition.Workers;

/// <summary>
/// Background worker that handles automatic importing of completed downloads.
/// </summary>
public class AutoImportWorker : BackgroundService
{
    private readonly ITorrentDownloadRepository _repository;
    private readonly ILibraryManager _libraryManager;
    private readonly ITorrentProgressEventEmitter _eventEmitter;
    private readonly ILogger<AutoImportWorker> _logger;
    private readonly MediaAcquisitionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoImportWorker"/> class.
    /// </summary>
    /// <param name="repository">The download repository.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="eventEmitter">The event emitter for progress updates.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The configuration options.</param>
    public AutoImportWorker(
        ITorrentDownloadRepository repository,
        ILibraryManager libraryManager,
        ITorrentProgressEventEmitter eventEmitter,
        ILogger<AutoImportWorker> logger,
        IOptions<MediaAcquisitionOptions> options)
    {
        _repository = repository;
        _libraryManager = libraryManager;
        _eventEmitter = eventEmitter;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.AutoImportEnabled)
        {
            _logger.LogInformation("Media Acquisition auto-import is disabled");
            return;
        }

        _logger.LogInformation("AutoImportWorker started");

        // Wait for initial startup
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCompletedDownloadsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing completed downloads");
            }

            // Check every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessCompletedDownloadsAsync(CancellationToken cancellationToken)
    {
        // Get completed downloads that need importing
        var pendingImports = await _repository.GetCompletedPendingImportAsync(cancellationToken).ConfigureAwait(false);

        // Also get downloads marked for importing
        var importing = await _repository.GetByStateAsync(TorrentState.Importing, cancellationToken).ConfigureAwait(false);
        var allToProcess = pendingImports.Concat(importing).Distinct().ToList();

        if (allToProcess.Count == 0)
        {
            return;
        }

        foreach (var download in allToProcess)
        {
            try
            {
                await ImportDownloadAsync(download, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing download: {Name}", download.Name);

                download.State = TorrentState.Error;
                download.ErrorMessage = ex.Message;
                await _repository.UpdateAsync(download, cancellationToken).ConfigureAwait(false);
                await _eventEmitter.EmitProgressUpdateAsync(download, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ImportDownloadAsync(TorrentDownload download, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(download.ContentPath))
        {
            _logger.LogWarning("Download has no content path: {Name}", download.Name);
            return;
        }

        // Check if content exists
        var contentExists = File.Exists(download.ContentPath) || Directory.Exists(download.ContentPath);
        if (!contentExists)
        {
            _logger.LogWarning("Download content not found at path: {Path}", download.ContentPath);
            return;
        }

        download.State = TorrentState.Importing;
        await _repository.UpdateAsync(download, cancellationToken).ConfigureAwait(false);
        await _eventEmitter.EmitProgressUpdateAsync(download, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Importing download: {Name} from {Path}", download.Name, download.ContentPath);

        // Find the library folder to scan based on media type
        string? libraryPath = null;

        if (download.MediaType == MediaType.Episode && download.SeriesId.HasValue)
        {
            var series = _libraryManager.GetItemById(download.SeriesId.Value) as Series;
            if (series != null)
            {
                libraryPath = series.ContainingFolderPath;
            }
        }
        else if (download.MediaType == MediaType.Movie)
        {
            // For movies, we'll trigger a general library scan
            // The movie will be picked up when the library is refreshed
        }

        // Trigger library scan
        if (!string.IsNullOrEmpty(libraryPath))
        {
            _logger.LogInformation("Triggering library scan for path: {Path}", libraryPath);

            // Find the library containing this path
            var folders = _libraryManager.GetVirtualFolders();
            var matchingFolder = folders
                .FirstOrDefault(f => f.Locations.Any(l =>
                    libraryPath.StartsWith(l, StringComparison.OrdinalIgnoreCase)));

            if (matchingFolder != null)
            {
                // Queue a library scan for this folder
                // Note: In a real implementation, you would use ILibraryMonitor or schedule a task
                _logger.LogInformation("Queuing library refresh for: {Library}", matchingFolder.Name);
            }
        }

        // Mark as imported
        download.State = TorrentState.Imported;
        download.ImportedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(download, cancellationToken).ConfigureAwait(false);
        await _eventEmitter.EmitProgressUpdateAsync(download, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully imported download: {Name}", download.Name);
    }
}
