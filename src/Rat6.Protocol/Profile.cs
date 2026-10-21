namespace Rat6.Protocol;

public sealed class Profile
{
    private readonly byte[] _payload;

    public Profile(ReadOnlySpan<byte> payload, byte? expectedChecksum = null)
    {
        if (payload.Length != ProfileLayout.PayloadSize)
        {
            throw new ArgumentException(
                $"Profile must be {ProfileLayout.PayloadSize} bytes, got {payload.Length}.",
                nameof(payload));
        }

        _payload = payload.ToArray();
        ExpectedChecksum = expectedChecksum;
    }

    public byte? ExpectedChecksum { get; }

    public ReadOnlySpan<byte> Payload => _payload;

    public byte Checksum => ProfileChecksum.Compute(_payload);

    public bool VerifyChecksum() =>
        ExpectedChecksum is null || ProfileChecksum.Verify(_payload, ExpectedChecksum.Value);

    public int PollRateNibble => _payload[ProfileLayout.PollRateOffset] & 0x0F;

    public bool AngleSnap => _payload[ProfileLayout.AngleSnapOffset] != 0;

    public byte DpiHeader => _payload[ProfileLayout.DpiHeaderOffset];

    public IReadOnlyList<DpiStage> DpiStages
    {
        get
        {
            var stages = new DpiStage[ProfileLayout.DpiStages];
            for (var index = 0; index < stages.Length; index++)
            {
                var offset = ProfileLayout.DpiOffset + index * 2;
                stages[index] = DpiStage.FromRaw(_payload[offset], _payload[offset + 1]);
            }

            return stages;
        }
    }

    public DpiStage GetDpiStage(int index)
    {
        ValidateStage(index);
        var offset = ProfileLayout.DpiOffset + index * 2;
        return DpiStage.FromRaw(_payload[offset], _payload[offset + 1]);
    }

    public void SetDpiStage(int index, DpiStage stage)
    {
        ValidateStage(index);
        ValidateDpi(stage.X);
        ValidateDpi(stage.Y);

        var offset = ProfileLayout.DpiOffset + index * 2;
        _payload[offset] = (byte)(stage.X / 100);
        _payload[offset + 1] = (byte)(stage.Y / 100);
    }

    public IReadOnlyList<ButtonBinding> Buttons
    {
        get
        {
            var buttons = new ButtonBinding[ProfileLayout.ButtonCount];
            for (var index = 0; index < buttons.Length; index++)
            {
                var id = _payload[ProfileLayout.ButtonIdsOffset + index];
                var recordOffset = ProfileLayout.ButtonActionsOffset + index * ProfileLayout.ButtonRecordSize;
                buttons[index] = ButtonBinding.Decode(id, _payload.AsSpan(recordOffset, ProfileLayout.ButtonRecordSize));
            }

            return buttons;
        }
    }

    public LedZone GetLedZone(int index)
    {
        ValidateZone(index);
        var offset = ProfileLayout.LedOffset + index * ProfileLayout.LedZoneSize;
        return LedZone.Decode(_payload.AsSpan(offset, ProfileLayout.LedZoneSize), index);
    }

    public void SetLedStatic(int index, RgbColor color, byte brightness) =>
        SetLed(index, LedZone.StaticMode, color, brightness);

    public void SetLed(int index, byte mode, RgbColor color, byte brightness, ushort? period = null, ushort? speed = null)
    {
        ValidateZone(index);
        var offset = ProfileLayout.LedOffset + index * ProfileLayout.LedZoneSize;
        LedZone.Write(_payload.AsSpan(offset, ProfileLayout.LedZoneSize), mode, color, brightness, period, speed);
    }

    public void SetLedStaticAll(RgbColor color, byte brightness)
    {
        for (var index = 0; index < ProfileLayout.LedZones; index++)
        {
            SetLedStatic(index, color, brightness);
        }
    }

    public Profile Clone() => new(_payload, ExpectedChecksum);

    public byte[] ToArray() => (byte[])_payload.Clone();

    private static void ValidateStage(int index)
    {
        if (index is < 0 or >= ProfileLayout.DpiStages)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "DPI stage must be 0..3.");
        }
    }

    private static void ValidateZone(int index)
    {
        if (index is < 0 or >= ProfileLayout.LedZones)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Zone must be 0..2.");
        }
    }

    private static void ValidateDpi(int dpi)
    {
        if (dpi < ProfileLayout.DpiMin || dpi > ProfileLayout.DpiMax || dpi % 100 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dpi),
                dpi,
                $"DPI must be a multiple of 100 and within {ProfileLayout.DpiMin}..{ProfileLayout.DpiMax}.");
        }
    }
}
