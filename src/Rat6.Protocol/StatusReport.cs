namespace Rat6.Protocol;

public readonly record struct StatusReport(byte Flags, byte Value, int DpiX, int DpiY)
{
    public const byte ProfileFlags = 0xA2;
    public const byte DpiFlags = 0xA3;
    public const byte ReportId = 0x03;

    public bool IsProfileEvent => Flags == ProfileFlags;

    public bool IsDpiEvent => Flags == DpiFlags;

    public int? ProfileSlot => IsProfileEvent ? Value : null;

    public int? DpiStageIndex => IsDpiEvent ? Value : null;

    public static bool TryParse(ReadOnlySpan<byte> report, out StatusReport status)
    {
        status = default;
        if (report.Length < 5 || report[0] != ReportId)
        {
            return false;
        }

        status = new StatusReport(report[1], report[2], report[3] * 100, report[4] * 100);
        return true;
    }
}
