using Rat6.Transport;

namespace Rat6.Transport.Linux;

public static class Rat6TransportFactory
{
    public static IHidTransport Open(string path)
    {
        try
        {
            return HidrawTransport.Open(path);
        }
        catch (UnauthorizedAccessException direct)
        {
            try
            {
                return PolkitHelperTransport.Open(path);
            }
            catch (Exception helper) when (helper is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                throw new IOException($"{direct.Message} {helper.Message}", helper);
            }
        }
    }
}
