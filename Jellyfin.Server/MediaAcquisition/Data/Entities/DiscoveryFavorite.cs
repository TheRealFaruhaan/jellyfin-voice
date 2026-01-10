using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Server.MediaAcquisition.Data.Entities;

/// <summary>
/// Represents a user's favorite discovery item (movie or TV show).
/// </summary>
[Table("DiscoveryFavorites")]
public class DiscoveryFavorite
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the user ID who favorited the item.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the TMDB ID of the item.
    /// </summary>
    [Required]
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the media type (Movie or TvShow).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when this was favorited.
    /// </summary>
    [Required]
    public DateTime FavoritedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the title/name for quick reference.
    /// </summary>
    [MaxLength(500)]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the poster path for quick reference.
    /// </summary>
    [MaxLength(500)]
    public string? PosterPath { get; set; }

    /// <summary>
    /// Gets or sets the release year for quick reference.
    /// </summary>
    public int? Year { get; set; }
}
