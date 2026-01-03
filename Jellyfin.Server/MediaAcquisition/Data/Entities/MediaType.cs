namespace Jellyfin.Server.MediaAcquisition.Data.Entities;

/// <summary>
/// Represents the type of media being downloaded.
/// </summary>
public enum MediaType
{
    /// <summary>
    /// A TV episode.
    /// </summary>
    Episode = 0,

    /// <summary>
    /// A movie.
    /// </summary>
    Movie = 1
}
