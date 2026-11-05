namespace GreenDragon.Core.Sonos;

/// <summary>Maps independent repeat and shuffle settings to the upstream protocol.</summary>
/// <param name="Repeat">The repeat setting.</param>
/// <param name="Shuffle">Whether queue order is randomized.</param>
public sealed record SonosPlayMode(SonosRepeatMode Repeat, bool Shuffle)
{
    private const string _normal = "NORMAL";
    private const string _repeatAll = "REPEAT_ALL";
    private const string _repeatOne = "REPEAT_ONE";
    private const string _shuffleOff = "SHUFFLE_NOREPEAT";
    private const string _shuffleAll = "SHUFFLE";
    private const string _shuffleOne = "SHUFFLE_REPEAT_ONE";

    /// <summary>Parses known protocol modes without guessing unknown values.</summary>
    /// <param name="value">The reported mode.</param>
    /// <returns>The independent settings, or null for unsupported values.</returns>
    public static SonosPlayMode? Parse(string? value)
    {
        return value switch
        {
            _normal => new(SonosRepeatMode.Off, false),
            _repeatAll => new(SonosRepeatMode.All, false),
            _repeatOne => new(SonosRepeatMode.One, false),
            _shuffleOff => new(SonosRepeatMode.Off, true),
            _shuffleAll => new(SonosRepeatMode.All, true),
            _shuffleOne => new(SonosRepeatMode.One, true),
            _ => null
        };
    }

    /// <summary>Applies one mode change, preserving the other setting.</summary>
    /// <param name="kind">A repeat-cycle or shuffle-toggle command.</param>
    /// <returns>The updated mode.</returns>
    /// <exception cref="BusinessLogicException">The command is not a mode change.</exception>
    public SonosPlayMode Change(SonosCommandKind kind)
    {
        return kind switch
        {
            SonosCommandKind.ToggleShuffle => this with { Shuffle = !Shuffle },
            SonosCommandKind.CycleRepeat => this with
            {
                Repeat = Repeat switch
                {
                    SonosRepeatMode.Off => SonosRepeatMode.All,
                    SonosRepeatMode.All => SonosRepeatMode.One,
                    _ => SonosRepeatMode.Off
                }
            },
            _ => throw new BusinessLogicException(SonosMessages.InvalidCommand)
        };
    }

    /// <summary>Converts the independent settings to a supported upstream mode.</summary>
    /// <returns>The protocol mode identifier.</returns>
    public string ToProtocol()
    {
        return (Repeat, Shuffle) switch
        {
            (SonosRepeatMode.Off, false) => _normal,
            (SonosRepeatMode.All, false) => _repeatAll,
            (SonosRepeatMode.One, false) => _repeatOne,
            (SonosRepeatMode.Off, true) => _shuffleOff,
            (SonosRepeatMode.All, true) => _shuffleAll,
            _ => _shuffleOne
        };
    }
}
