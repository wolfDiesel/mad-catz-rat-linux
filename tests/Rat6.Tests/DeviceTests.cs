using Rat6.Device;
using Rat6.Protocol;
using Rat6.Transport;
using Xunit;

namespace Rat6.Tests;

public sealed class DeviceTests
{
    private static Profile LoadSlot(int slot)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", $"slot{slot}.bin");
        return new Profile(File.ReadAllBytes(path));
    }

    [Fact]
    public void GetFirmware_ParsesReply()
    {
        var transport = new FakeTransport();
        transport.Replies.Enqueue([0xB0, 0x55, 0xAA, 0x06]);
        using var device = new Rat6Device(transport);

        Assert.Equal(6, device.GetFirmware());
        Assert.Equal([0x0C, 0xC4, 0x08, 0, 0, 0, 0, 0, 0, 0], transport.Written[0]);
    }

    [Fact]
    public void GetSelectedSlot_ParsesSlotAndCount()
    {
        var transport = new FakeTransport();
        transport.Replies.Enqueue([0xB0, 0xC6, 0x02, 0x04, 0, 0, 0, 0]);
        using var device = new Rat6Device(transport);

        var selected = device.GetSelectedSlot();

        Assert.Equal(2, selected.Slot);
        Assert.Equal(4, selected.Count);
    }

    [Fact]
    public void ReadProfile_ReturnsParsedPayload()
    {
        var profile = LoadSlot(0);
        var transport = new FakeTransport();
        transport.Replies.Enqueue(BuildReadFrame(0, profile));
        using var device = new Rat6Device(transport, profileAttempts: 1);

        var read = device.ReadProfile(0);

        Assert.Equal(profile.ToArray(), read.ToArray());
        Assert.Equal([0x0C, 0xC4, 0x0C, 0, 0, 0, 0, 0, 0, 0], transport.Written[0]);
    }

    [Fact]
    public void WriteProfile_SendsFrameAndAcceptsAck()
    {
        var profile = LoadSlot(0);
        var transport = new FakeTransport();
        transport.Replies.Enqueue([0xB0, 0x05, 0x01, 0x11, 0x11]);
        using var device = new Rat6Device(transport);

        device.WriteProfile(1, profile);

        var frame = transport.Written[0];
        Assert.Equal(ProfileFrame.Length, frame.Length);
        Assert.Equal(0x05, frame[0]);
        Assert.Equal(0x00, frame[1]);
        Assert.Equal(1, frame[2]);
        Assert.Equal(profile.Checksum, frame[3]);
        Assert.Equal(profile.ToArray(), frame[4..(4 + ProfileLayout.PayloadSize)]);
    }

    [Fact]
    public void Profile_RejectsWrongSize()
    {
        Assert.Throws<ArgumentException>(() => new Profile(new byte[100]));
    }

    [Fact]
    public void SelectSlot_SendsCommandAndVerifiesSelectedSlot()
    {
        var transport = new FakeTransport();
        transport.Replies.Enqueue([0xB0, 0xC6, 0x03, 0x04, 0, 0, 0, 0]);
        using var device = new Rat6Device(transport);

        device.SelectSlot(3);

        Assert.Equal([0x0C, 0xC4, 0x07, 0x03, 0, 0, 0, 0, 0, 0], transport.Written[0]);
        Assert.Equal([0x0C, 0xC4, 0x16, 0, 0, 0, 0, 0, 0, 0], transport.Written[1]);
    }

    private static byte[] BuildReadFrame(int slot, Profile profile)
    {
        var frame = new byte[ProfileFrame.Length];
        frame[0] = ProfileFrame.ReportId;
        frame[1] = 0xB0;
        frame[2] = (byte)slot;
        frame[3] = profile.Checksum;
        profile.Payload.CopyTo(frame.AsSpan(4));
        return frame;
    }

    private sealed class FakeTransport : IHidTransport
    {
        public List<byte[]> Written { get; } = [];

        public Queue<byte[]> Replies { get; } = new();

        public byte[] GetFeature(byte reportId, int length)
        {
            if (Replies.Count == 0)
            {
                throw new IOException("no prepared reply");
            }

            return Replies.Dequeue();
        }

        public void SetFeature(ReadOnlySpan<byte> report) => Written.Add(report.ToArray());

        public byte[]? ReadReport(int timeoutMs) => null;

        public void Dispose()
        {
        }
    }
}
