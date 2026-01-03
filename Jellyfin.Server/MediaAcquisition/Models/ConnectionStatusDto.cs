using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Server.MediaAcquisition.Models;

/// <summary>
/// Data transfer object for connection status.
/// </summary>
public class ConnectionStatusDto
{
    /// <summary>
    /// Gets or sets a value indicating whether qBittorrent is connected.
    /// </summary>
    [JsonPropertyName("qBittorrentConnected")]
    public bool QBittorrentConnected { get; set; }

    /// <summary>
    /// Gets or sets the indexer status dictionary.
    /// </summary>
    [JsonPropertyName("indexers")]
    public IDictionary<string, bool> Indexers { get; set; } = new Dictionary<string, bool>();
}
