namespace Rat6.Protocol;

public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public static RgbColor FromHex(string value)
    {
        var text = value.Trim().TrimStart('#');
        if (text.Length != 6 || !byte.TryParse(text[..2], System.Globalization.NumberStyles.HexNumber, null, out var r)
            || !byte.TryParse(text[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g)
            || !byte.TryParse(text[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            throw new FormatException("Color must be in RRGGBB format.");
        }

        return new RgbColor(r, g, b);
    }

    public string ToHex() => $"{R:X2}{G:X2}{B:X2}";
}
