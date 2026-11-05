namespace GreenDragon.Core.Sonos;

/// <summary>Supported user commands; none is automatically retried.</summary>
public enum SonosCommandKind
{
    /// <summary>Rejects missing or unspecified commands.</summary>
    None,
    /// <summary>Moves to the previous track in the group.</summary>
    Previous,
    /// <summary>Changes between playing and paused using current group state.</summary>
    TogglePlayback,
    /// <summary>Moves to the next track in the group.</summary>
    Next,
    /// <summary>Cycles repeat through off, all and one.</summary>
    CycleRepeat,
    /// <summary>Changes shuffle while preserving repeat.</summary>
    ToggleShuffle,
    /// <summary>Sets the selected room's volume percentage.</summary>
    SetVolume,
    /// <summary>Decreases the selected room's volume by one point.</summary>
    VolumeDown,
    /// <summary>Increases the selected room's volume by one point.</summary>
    VolumeUp
}
