namespace GreenDragon.Core.Sonos;

/// <summary>Separates player-catalog idle retention from other metadata expiration.</summary>
public enum SonosCachePolicy
{
    /// <summary>Expires metadata absolutely after the configured duration.</summary>
    MetadataAbsolute,
    /// <summary>Retains player IDs and their catalog for 24 hours after the last access.</summary>
    PlayerCatalog
}
