namespace Rat6.Protocol;

public sealed record ButtonBinding(byte Id, byte Action, byte[] Parameters)
{
    public static ButtonBinding Decode(byte id, ReadOnlySpan<byte> record)
    {
        if (record.Length < ProfileLayout.ButtonRecordSize)
        {
            throw new ArgumentException("Button record is shorter than 5 bytes.", nameof(record));
        }

        return new ButtonBinding(id, record[0], record[1..ProfileLayout.ButtonRecordSize].ToArray());
    }
}
