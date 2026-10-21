namespace Rat6.Transport;

public interface IHidTransport : IDisposable
{
    byte[] GetFeature(byte reportId, int length);

    void SetFeature(ReadOnlySpan<byte> report);

    byte[]? ReadReport(int timeoutMs);
}
