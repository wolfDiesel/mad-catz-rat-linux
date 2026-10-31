using Rat6.Protocol;
using Xunit;

namespace Rat6.Tests;

public sealed class ProtocolTests
{
    [Fact]
    public void Command_IsBuiltAsTenBytes()
    {
        var command = CommandReport.Build(CommandCodes.SelectSlot, 2);

        Assert.Equal(10, command.Length);
        Assert.Equal([0x0C, 0xC4, 0x07, 0x02, 0, 0, 0, 0, 0, 0], command);
    }

    [Fact]
    public void FirmwareReply_IsParsed()
    {
        Assert.True(CommandReport.TryParseFirmware([0xB0, 0x55, 0xAA, 0x06], out var version));
        Assert.Equal(6, version);
    }

    [Fact]
    public void SelectedSlotReply_IsParsed()
    {
        Assert.True(CommandReport.TryParseSelectedSlot([0xB0, 0xC6, 0x02, 0x04], out var slot, out var count));
        Assert.Equal(2, slot);
        Assert.Equal(4, count);
    }

    [Fact]
    public void WriteAck_RequiresMatchingSlotAndEqualTail()
    {
        Assert.True(CommandReport.IsWriteAck([0xB0, 0x05, 0x01, 0x0A, 0x0A], 1));
        Assert.False(CommandReport.IsWriteAck([0xB0, 0x05, 0x01, 0x0A, 0x0B], 1));
        Assert.False(CommandReport.IsWriteAck([0xB0, 0x05, 0x02, 0x0A, 0x0A], 1));
    }

    [Fact]
    public void StatusReport_DecodesDpiEvent()
    {
        Assert.True(StatusReport.TryParse([0x03, 0xA3, 0x02, 0x20, 0x20, 0x00, 0x00], out var status));
        Assert.True(status.IsDpiEvent);
        Assert.False(status.IsProfileEvent);
        Assert.Equal(2, status.DpiStageIndex);
        Assert.Equal(3200, status.DpiX);
        Assert.Equal(3200, status.DpiY);
    }

    [Fact]
    public void StatusReport_DecodesProfileEvent()
    {
        Assert.True(StatusReport.TryParse([0x03, 0xA2, 0x03, 0x10, 0x10, 0x00, 0x00], out var status));
        Assert.True(status.IsProfileEvent);
        Assert.Equal(3, status.ProfileSlot);
        Assert.Equal(1600, status.DpiX);
    }

    [Fact]
    public void Checksum_RejectsCorruptedPayload()
    {
        var payload = Enumerable.Range(0, ProfileLayout.PayloadSize).Select(value => (byte)value).ToArray();
        var checksum = ProfileChecksum.Compute(payload);

        Assert.True(ProfileChecksum.Verify(payload, checksum));

        payload[10] ^= 0x01;
        Assert.False(ProfileChecksum.Verify(payload, checksum));
    }

    [Fact]
    public void RgbColor_ParsesHex()
    {
        Assert.Equal(new RgbColor(0x11, 0x22, 0x33), RgbColor.FromHex("#112233"));
        Assert.Equal("112233", new RgbColor(0x11, 0x22, 0x33).ToHex());
    }
}
