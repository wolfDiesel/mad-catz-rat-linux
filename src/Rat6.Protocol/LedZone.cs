namespace Rat6.Protocol;

public sealed record LedZone(
    int Index,
    byte Mode,
    byte Brightness,
    RgbColor Color,
    ushort Period,
    ushort Speed,
    byte ColorCount)
{
    public const byte StaticMode = 1;
    public const byte BreathingMode = 2;
    public const byte RainbowMode = 3;

    public static LedZone Decode(ReadOnlySpan<byte> block, int index)
    {
        if (block.Length < ProfileLayout.LedZoneSize)
        {
            throw new ArgumentException("LED block is shorter than 191 bytes.", nameof(block));
        }

        var mode = block[0];
        var period = (ushort)(block[5] | (block[6] << 8));
        var speed = (ushort)(block[52] | (block[53] << 8));
        var color = new RgbColor(block[2], block[3], block[4]);

        return new LedZone(index, mode, block[1], color, period, speed, block[11]);
    }

    public static void WriteStatic(Span<byte> block, RgbColor color, byte brightness) =>
        Write(block, StaticMode, color, brightness);

    public static void Write(Span<byte> block, byte mode, RgbColor color, byte brightness, ushort? period = null, ushort? speed = null)
    {
        if (block.Length < ProfileLayout.LedZoneSize)
        {
            throw new ArgumentException("LED block is shorter than 191 bytes.", nameof(block));
        }

        if (mode is < StaticMode or > RainbowMode)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "LED mode must be 1..3.");
        }

        block[0] = mode;
        block[1] = brightness;
        block[2] = color.R;
        block[3] = color.G;
        block[4] = color.B;

        if (mode == BreathingMode)
        {
            block[12] = brightness;
            block[13] = color.R;
            block[14] = color.G;
            block[15] = color.B;
        }

        if (period is { } breathingPeriod)
        {
            block[5] = (byte)(breathingPeriod & 0xFF);
            block[6] = (byte)(breathingPeriod >> 8);
        }

        if (speed is { } rainbowSpeed)
        {
            block[52] = (byte)(rainbowSpeed & 0xFF);
            block[53] = (byte)(rainbowSpeed >> 8);
        }
    }
}
