namespace Jellyfin.Server.MediaAcquisition.Data.Entities;

/// <summary>
/// Represents the state of a torrent download.
/// </summary>
public enum TorrentState
{
    /// <summary>
    /// The torrent is queued for download.
    /// </summary>
    Queued = 0,

    /// <summary>
    /// The torrent is currently downloading.
    /// </summary>
    Downloading = 1,

    /// <summary>
    /// The torrent download is paused.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// The torrent download is completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The torrent is being imported into the library.
    /// </summary>
    Importing = 4,

    /// <summary>
    /// The torrent has been imported into the library.
    /// </summary>
    Imported = 5,

    /// <summary>
    /// The torrent download encountered an error.
    /// </summary>
    Error = 6,

    /// <summary>
    /// The torrent is seeding.
    /// </summary>
    Seeding = 7
}
