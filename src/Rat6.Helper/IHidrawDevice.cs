namespace Rat6.Helper;

public interface IHidrawDevice : IDisposable
{
    void Open(string path);

    byte[] GetFeature(byte reportId, int length);

    void SetFeature(ReadOnlySpan<byte> report);
}
