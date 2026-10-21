namespace Rat6.Protocol;

public static class CommandCodes
{
    public const byte Reload = 0x00;
    public const byte SelectSlot = 0x07;
    public const byte Firmware = 0x08;
    public const byte ReadProfile = 0x0C;
    public const byte SelectedSlot = 0x16;
    public const byte ResetSlot = 0x17;
}

public static class CommandReport
{
    public const byte ReportId = 0x0C;
    public const int Length = 10;
    private const byte Ack = 0xB0;

    public static byte[] Build(byte command, params byte[] args)
    {
        var buffer = new byte[Length];
        buffer[0] = ReportId;
        buffer[1] = 0xC4;
        buffer[2] = command;
        args.CopyTo(buffer, 3);
        return buffer;
    }

    public static bool IsReply(ReadOnlySpan<byte> reply) => reply.Length > 0 && reply[0] == Ack;

    public static bool TryParseFirmware(ReadOnlySpan<byte> reply, out int version)
    {
        version = 0;
        if (reply.Length < 4 || reply[0] != Ack || reply[1] != 0x55 || reply[2] != 0xAA)
        {
            return false;
        }

        version = reply[3];
        return true;
    }

    public static bool TryParseSelectedSlot(ReadOnlySpan<byte> reply, out int slot, out int count)
    {
        slot = 0;
        count = 0;
        if (reply.Length < 4 || reply[0] != Ack || reply[1] != 0xC6)
        {
            return false;
        }

        slot = reply[2];
        count = reply[3];
        return true;
    }

    public static bool IsWriteAck(ReadOnlySpan<byte> reply, int slot)
    {
        return reply.Length >= 5
            && reply[0] == Ack
            && reply[1] == ProfileFrame.ReportId
            && reply[2] == slot
            && reply[3] == reply[4];
    }
}

public static class ProfileFrame
{
    public const byte ReportId = 0x05;
    public const int Length = 738;

    public static byte[] BuildWriteFrame(int slot, ReadOnlySpan<byte> payload)
    {
        if (payload.Length != ProfileLayout.PayloadSize)
        {
            throw new ArgumentException("Invalid profile size.", nameof(payload));
        }

        var frame = new byte[Length];
        frame[0] = ReportId;
        frame[1] = 0x00;
        frame[2] = (byte)slot;
        frame[3] = ProfileChecksum.Compute(payload);
        payload.CopyTo(frame.AsSpan(4));
        return frame;
    }

    public static bool TryParse(ReadOnlySpan<byte> frame, out int slot, out byte checksum, out byte[] payload)
    {
        slot = 0;
        checksum = 0;
        payload = [];
        if (frame.Length < 4 + ProfileLayout.PayloadSize || frame[0] != ReportId || frame[1] != 0xB0)
        {
            return false;
        }

        slot = frame[2];
        checksum = frame[3];
        var body = frame.Slice(4, ProfileLayout.PayloadSize);
        if (!ProfileChecksum.Verify(body, checksum))
        {
            return false;
        }

        payload = body.ToArray();
        return true;
    }
}
