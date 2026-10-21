namespace Rat6.Protocol;

public static class ProfileLayout
{
    public const int PayloadSize = 733;
    public const int FrameSize = 737;
    public const int FeatureReportLength = 738;

    public const int PollRateOffset = 0;
    public const int AngleSnapOffset = 2;
    public const int DpiHeaderOffset = 17;
    public const int DpiOffset = 18;
    public const int DpiStages = 4;
    public const int DpiMin = 100;
    public const int DpiMax = 12000;

    public const int ButtonIdsOffset = 64;
    public const int ButtonCount = 13;
    public const int ButtonActionsOffset = 80;
    public const int ButtonRecordSize = 5;

    public const int LedOffset = 160;
    public const int LedZoneSize = 191;
    public const int LedZones = 3;
}
