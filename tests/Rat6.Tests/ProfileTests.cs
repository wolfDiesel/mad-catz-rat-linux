using Rat6.Protocol;
using Xunit;

namespace Rat6.Tests;

public sealed class ProfileTests
{
    private static Profile LoadSlot(int slot)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", $"slot{slot}.bin");
        return new Profile(File.ReadAllBytes(path));
    }

    [Fact]
    public void Slot0_HasExpectedDpiStages()
    {
        var profile = LoadSlot(0);

        Assert.Equal(
            [800, 1600, 3200, 12000],
            profile.DpiStages.Select(stage => stage.X).ToArray());
    }

    [Fact]
    public void Slot1_And_Slot2_AreIdentical()
    {
        Assert.Equal(LoadSlot(1).ToArray(), LoadSlot(2).ToArray());
    }

    [Fact]
    public void Checksum_IsStable()
    {
        var profile = LoadSlot(0);

        Assert.Equal(0x1F, profile.Checksum);
        Assert.True(profile.VerifyChecksum());
    }

    [Fact]
    public void Buttons_MatchKnownActions()
    {
        var buttons = LoadSlot(0).Buttons;

        Assert.Equal(13, buttons.Count);
        Assert.Equal((byte)0x01, buttons[0].Action);
        Assert.Equal([1, 0, 0, 0], buttons[0].Parameters);
        Assert.Equal((byte)0x10, buttons[6].Action);
        Assert.Equal([4, 4, 0, 0], buttons[6].Parameters);
        Assert.Equal((byte)0x09, buttons[7].Action);
        Assert.Equal((byte)0x0A, buttons[8].Action);
        Assert.Equal((byte)0x07, buttons[9].Action);
        Assert.Equal((byte)0x08, buttons[10].Action);
    }

    [Fact]
    public void LedZone0_IsBreathingRed()
    {
        var zone = LoadSlot(0).GetLedZone(0);

        Assert.Equal(LedZone.BreathingMode, zone.Mode);
        Assert.Equal(new RgbColor(255, 0, 0), zone.Color);
        Assert.Equal(255, zone.Brightness);
        Assert.Equal(4, zone.Period);
        Assert.Equal(1, zone.ColorCount);
    }

    [Fact]
    public void SetDpiStage_ChangesRawBytes()
    {
        var profile = LoadSlot(0);
        profile.SetDpiStage(3, new DpiStage(10000, 10000));

        Assert.Equal(100, profile.Payload[24]);
        Assert.Equal(100, profile.Payload[25]);
        Assert.NotEqual(0x1F, profile.Checksum);
        Assert.True(profile.VerifyChecksum());
    }

    [Fact]
    public void SetLedStatic_WritesRgbAndMode()
    {
        var profile = LoadSlot(0);
        var color = new RgbColor(0x11, 0x22, 0x33);
        profile.SetLedStatic(1, color, 128);

        var offset = ProfileLayout.LedOffset + ProfileLayout.LedZoneSize;
        Assert.Equal(LedZone.StaticMode, profile.Payload[offset]);
        Assert.Equal(128, profile.Payload[offset + 1]);
        Assert.Equal(0x11, profile.Payload[offset + 2]);
        Assert.Equal(0x22, profile.Payload[offset + 3]);
        Assert.Equal(0x33, profile.Payload[offset + 4]);

        var zone = profile.GetLedZone(1);
        Assert.Equal(color, zone.Color);
        Assert.Equal(128, zone.Brightness);
    }

    [Fact]
    public void SetLedBreathing_ChangesModeAndKeepsPeriod()
    {
        var profile = LoadSlot(0);
        profile.SetLed(0, LedZone.BreathingMode, new RgbColor(0, 0, 255), 64, period: 7);

        var zone = profile.GetLedZone(0);
        Assert.Equal(LedZone.BreathingMode, zone.Mode);
        Assert.Equal(new RgbColor(0, 0, 255), zone.Color);
        Assert.Equal(64, zone.Brightness);
        Assert.Equal(7, zone.Period);

        var offset = ProfileLayout.LedOffset;
        Assert.Equal(7, profile.Payload[offset + 5]);
        Assert.Equal(0, profile.Payload[offset + 6]);
        Assert.Equal(64, profile.Payload[offset + 12]);
        Assert.Equal(0, profile.Payload[offset + 13]);
        Assert.Equal(0, profile.Payload[offset + 14]);
        Assert.Equal(255, profile.Payload[offset + 15]);
    }

    [Fact]
    public void SetLedBreathing_WithoutPeriodKeepsExistingPeriod()
    {
        var profile = LoadSlot(0);
        profile.SetLed(0, LedZone.BreathingMode, new RgbColor(0, 0, 255), 64);

        Assert.Equal(4, profile.GetLedZone(0).Period);
    }

    [Fact]
    public void SetLedRainbow_SetsMode()
    {
        var profile = LoadSlot(0);
        profile.SetLed(2, LedZone.RainbowMode, new RgbColor(0, 0, 0), 200);

        var zone = profile.GetLedZone(2);
        Assert.Equal(LedZone.RainbowMode, zone.Mode);
        Assert.Equal(200, zone.Brightness);
    }

    [Fact]
    public void WriteFrame_ContainsChecksumAndPayload()
    {
        var profile = LoadSlot(0);
        var frame = ProfileFrame.BuildWriteFrame(2, profile.Payload);

        Assert.Equal(ProfileFrame.Length, frame.Length);
        Assert.Equal(ProfileFrame.ReportId, frame[0]);
        Assert.Equal(0x00, frame[1]);
        Assert.Equal(2, frame[2]);
        Assert.Equal(profile.Checksum, frame[3]);
        Assert.Equal(profile.ToArray(), frame[4..(4 + ProfileLayout.PayloadSize)]);
        Assert.Equal(0, frame[^1]);
    }

    [Fact]
    public void ReadFrame_ParsesPayload()
    {
        var profile = LoadSlot(0);
        var frame = new byte[ProfileFrame.Length];
        frame[0] = ProfileFrame.ReportId;
        frame[1] = 0xB0;
        frame[2] = 2;
        frame[3] = profile.Checksum;
        profile.Payload.CopyTo(frame.AsSpan(4));

        Assert.True(ProfileFrame.TryParse(frame, out var slot, out var checksum, out var payload));
        Assert.Equal(2, slot);
        Assert.Equal(profile.Checksum, checksum);
        Assert.Equal(profile.ToArray(), payload);
    }
}
