using GreenDragon.Core.Configuration;
using GreenDragon.Core.Sonos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace GreenDragon.Core.UnitTests;

public sealed class SonosControlsTests
{
    [Theory]
    [InlineData("NORMAL", SonosRepeatMode.Off, false)]
    [InlineData("REPEAT_ALL", SonosRepeatMode.All, false)]
    [InlineData("REPEAT_ONE", SonosRepeatMode.One, false)]
    [InlineData("SHUFFLE_NOREPEAT", SonosRepeatMode.Off, true)]
    [InlineData("SHUFFLE", SonosRepeatMode.All, true)]
    [InlineData("SHUFFLE_REPEAT_ONE", SonosRepeatMode.One, true)]
    public void ModeChangesPreserveTheOtherSetting(string protocol, SonosRepeatMode repeat, bool shuffle)
    {
        var mode = SonosPlayMode.Parse(protocol).ShouldNotBeNull();
        mode.Repeat.ShouldBe(repeat);
        mode.Shuffle.ShouldBe(shuffle);
        mode.ToProtocol().ShouldBe(protocol);
        mode.Change(SonosCommandKind.ToggleShuffle).Repeat.ShouldBe(repeat);
        mode.Change(SonosCommandKind.ToggleShuffle).Shuffle.ShouldBe(!shuffle);
        var next = mode.Change(SonosCommandKind.CycleRepeat);
        next.Shuffle.ShouldBe(shuffle);
        next.Repeat.ShouldBe(repeat switch { SonosRepeatMode.Off => SonosRepeatMode.All, SonosRepeatMode.All => SonosRepeatMode.One, _ => SonosRepeatMode.Off });
        next.Change(SonosCommandKind.CycleRepeat).Change(SonosCommandKind.CycleRepeat).ShouldBe(mode);
        SonosPlayMode.Parse("UNKNOWN").ShouldBeNull();
    }

    [Fact]
    public void CommandsValidateIdsKindsAndVolume()
    {
        var invalid = new[]
        {
            new SonosCommand("", SonosCommandKind.Next),
            new SonosCommand(new string('x', BusinessRules.Sonos.MaximumIdLength + 1), SonosCommandKind.Next),
            new SonosCommand("known", SonosCommandKind.None),
            new SonosCommand("known", (SonosCommandKind)999),
            new SonosCommand("known", SonosCommandKind.SetVolume),
            new SonosCommand("known", SonosCommandKind.SetVolume, -1),
            new SonosCommand("known", SonosCommandKind.SetVolume, 101),
            new SonosCommand("known", SonosCommandKind.Next, 10)
        };
        foreach (var command in invalid)
        {
            Should.Throw<BusinessLogicException>(command.Validate);
        }

        new SonosCommand("known", SonosCommandKind.SetVolume, 0).Validate();
        new SonosCommand("known", SonosCommandKind.SetVolume, 100).Validate();
    }

    [Fact]
    public void SharedCacheRetainsMetadataForAnHourWithoutSlidingExpiration()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var clock = new ControlledClock();
        var settings = Options.Create(new SonosSettings());
        var firstRequest = new SonosMetadataCache(memory, settings, clock);
        var nextRequest = new SonosMetadataCache(memory, settings, clock);
        var metadata = new SpeakerSnapshot("player", "Room", "Model");
        firstRequest.Set("players", "household-A", metadata);
        clock.Advance(TimeSpan.FromMinutes(59));
        nextRequest.TryGet<SpeakerSnapshot>("players", "household-A", out var found).ShouldBeTrue();
        found.ShouldBeSameAs(metadata);
        nextRequest.TryGet<SpeakerSnapshot>("players", "household-B", out _).ShouldBeFalse();
        nextRequest.TryGet<SpeakerSnapshot>("favorites", "household-A", out _).ShouldBeFalse();
        clock.Advance(TimeSpan.FromMinutes(1));
        nextRequest.TryGet<SpeakerSnapshot>("players", "household-A", out _).ShouldBeFalse();
        nextRequest.Set("players", "household-A", metadata);
        nextRequest.Remove<SpeakerSnapshot>("players", "household-A");
        firstRequest.TryGet<SpeakerSnapshot>("players", "household-A", out _).ShouldBeFalse();
    }

    [Fact]
    public void PlayerCatalogUsesTwentyFourHoursOfSlidingRetentionAcrossRequests()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var clock = new ControlledClock();
        var settings = Options.Create(new SonosSettings());
        var firstRequest = new SonosMetadataCache(memory, settings, clock);
        var nextRequest = new SonosMetadataCache(memory, settings, clock);
        var player = new SpeakerSnapshot("player", "Room", "Model");
        firstRequest.Set("catalog", "active", player, SonosCachePolicy.PlayerCatalog);
        firstRequest.Set("catalog", "idle", player, SonosCachePolicy.PlayerCatalog);
        clock.Advance(TimeSpan.FromHours(23));
        nextRequest.TryGet<SpeakerSnapshot>("catalog", "active", out var found).ShouldBeTrue();
        found.ShouldBeSameAs(player);
        clock.Advance(TimeSpan.FromHours(2));
        nextRequest.TryGet<SpeakerSnapshot>("catalog", "idle", out _).ShouldBeFalse();
        firstRequest.TryGet<SpeakerSnapshot>("catalog", "active", out _).ShouldBeTrue();
        clock.Advance(TimeSpan.FromHours(23));
        nextRequest.TryGet<SpeakerSnapshot>("catalog", "active", out _).ShouldBeTrue();
        clock.Advance(TimeSpan.FromHours(24));
        firstRequest.TryGet<SpeakerSnapshot>("catalog", "active", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(59, false)]
    [InlineData(60, true)]
    [InlineData(120, true)]
    public void CacheRetentionConfigurationHasOneHourMinimum(int minutes, bool valid)
    {
        var services = new ServiceCollection();

        services.AddSonos(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Sonos:MetadataCacheDurationMinutes"] = minutes.ToString(System.Globalization.CultureInfo.InvariantCulture)
        }).Build());
        services.RemoveAll<ISonosGateway>();
        services.AddSingleton(Substitute.For<ISonosGateway>());
        var validators = services.Where(descriptor => descriptor.ServiceType == typeof(IValidateOptions<SonosSettings>))
            .Select(descriptor => descriptor.ImplementationInstance).OfType<IValidateOptions<SonosSettings>>().ToArray();
        validators.ShouldNotBeEmpty();
        validators.All(validator => validator.Validate(Options.DefaultName,
            new SonosSettings { MetadataCacheDurationMinutes = minutes }).Succeeded).ShouldBe(valid);
    }
    private sealed class ControlledClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public void Advance(TimeSpan duration)
        {
            _now += duration;
        }
    }
}
