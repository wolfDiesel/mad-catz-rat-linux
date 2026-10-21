namespace Rat6.Protocol;

public static class ProfileChecksum
{
    public static byte Compute(ReadOnlySpan<byte> payload)
    {
        var sum = 0;
        foreach (var value in payload)
        {
            sum += value;
        }

        return (byte)(-sum & 0xFF);
    }

    public static bool Verify(ReadOnlySpan<byte> payload, byte checksum)
    {
        var sum = checksum;
        foreach (var value in payload)
        {
            sum += value;
        }

        return (sum & 0xFF) == 0;
    }
}
