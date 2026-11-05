using System.ComponentModel.DataAnnotations;
using GreenDragon.Core;
using GreenDragon.Core.Sonos;

namespace GreenDragon.Web.Models;

/// <summary>Server-validated form input for one discovered speaker.</summary>
public sealed class SpeakerCommandAIM
{
    /// <summary>The stable ID supplied by the speaker overview.</summary>
    [Required, StringLength(BusinessRules.Sonos.MaximumIdLength)]
    public string SpeakerId { get; set; } = string.Empty;

    /// <summary>The operation selected by the form submitter.</summary>
    [EnumDataType(typeof(SonosCommandKind))]
    public SonosCommandKind Kind { get; set; }

    /// <summary>The optional target volume percentage.</summary>
    [Range(BusinessRules.Sonos.MinimumVolume, BusinessRules.Sonos.MaximumVolume)]
    public int? Volume { get; set; }
}
