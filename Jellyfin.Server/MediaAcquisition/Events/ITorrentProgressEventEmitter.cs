using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.MediaAcquisition.Data.Entities;

namespace Jellyfin.Server.MediaAcquisition.Events;

/// <summary>
/// Interface for emitting torrent progress events.
/// </summary>
public interface ITorrentProgressEventEmitter
{
    /// <summary>
    /// Emits a progress update event for a download.
    /// </summary>
    /// <param name="download">The download.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task EmitProgressUpdateAsync(TorrentDownload download, CancellationToken cancellationToken = default);
}
