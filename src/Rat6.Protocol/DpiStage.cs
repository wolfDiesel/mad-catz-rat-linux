namespace Rat6.Protocol;

public readonly record struct DpiStage(int X, int Y)
{
    public static DpiStage FromRaw(byte x, byte y) => new(x * 100, y * 100);
}
