namespace Rat6.Helper;

public sealed class FakeHidrawDevice : IHidrawDevice
{
    private const int MaxReportLength = 4096;

    public string? OpenedPath { get; private set; }

    public byte[]? LastReport { get; private set; }

    public void Open(string path)
    {
        if (!path.StartsWith("/dev/hidraw", StringComparison.Ordinal))
        {
            throw new IOException($"fake: unsupported path {path}.");
        }

        OpenedPath = path;
    }

    public byte[] GetFeature(byte reportId, int length)
    {
        if (length is < 1 or > MaxReportLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Report length must be 1..4096.");
        }

        var data = new byte[length];
        data[0] = reportId;
        return data;
    }

    public void SetFeature(ReadOnlySpan<byte> report)
    {
        if (report.Length is < 1 or > MaxReportLength)
        {
            throw new ArgumentOutOfRangeException(nameof(report), report.Length, "Report length must be 1..4096.");
        }

        LastReport = report.ToArray();
    }

    public void Dispose()
    {
        OpenedPath = null;
    }
}
