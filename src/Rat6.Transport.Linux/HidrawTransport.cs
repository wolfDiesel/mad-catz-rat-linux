using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Rat6.Transport;

namespace Rat6.Transport.Linux;

public sealed class HidrawTransport : IHidTransport
{
    private readonly SafeFileHandle _handle;
    private readonly int _fd;

    private HidrawTransport(SafeFileHandle handle)
    {
        _handle = handle;
        _fd = (int)handle.DangerousGetHandle();
    }

    public static HidrawTransport Open(string path)
    {
        var fd = HidrawNative.open(path, HidrawNative.ORdwr | HidrawNative.ONonblock);
        if (fd < 0)
        {
            var error = Marshal.GetLastPInvokeError();
            if (error == 13)
            {
                throw new UnauthorizedAccessException(
                    $"No access to {path} (errno 13). Run with sudo.");
            }

            throw new IOException($"open({path}) failed, errno {error}.");
        }

        return new HidrawTransport(new SafeFileHandle((IntPtr)fd, ownsHandle: true));
    }

    public byte[] GetFeature(byte reportId, int length)
    {
        var buffer = new byte[length];
        buffer[0] = reportId;
        var result = HidrawNative.GetFeature(_fd, buffer);
        if (result < 0)
        {
            throw new IOException($"GET_FEATURE 0x{reportId:X2} failed, errno {Marshal.GetLastPInvokeError()}.");
        }

        var actual = Math.Min(result, length);
        return buffer[..actual];
    }

    public void SetFeature(ReadOnlySpan<byte> report)
    {
        var buffer = report.ToArray();
        var result = HidrawNative.SetFeature(_fd, buffer);
        if (result < 0)
        {
            throw new IOException($"SET_FEATURE failed, errno {Marshal.GetLastPInvokeError()}.");
        }
    }

    public byte[]? ReadReport(int timeoutMs)
    {
        var pollFd = new HidrawNative.PollFd { Fd = _fd, Events = HidrawNative.PollIn };
        var ready = HidrawNative.poll(ref pollFd, 1, timeoutMs);
        if (ready < 0)
        {
            throw new IOException($"poll failed, errno {Marshal.GetLastPInvokeError()}.");
        }

        if (ready == 0)
        {
            return null;
        }

        var buffer = new byte[256];
        var count = HidrawNative.read(_fd, buffer, (nuint)buffer.Length);
        if (count < 0)
        {
            var error = Marshal.GetLastPInvokeError();
            if (error == HidrawNative.Eagain)
            {
                return null;
            }

            throw new IOException($"read failed, errno {error}.");
        }

        if (count == 0)
        {
            return null;
        }

        return buffer[..(int)count];
    }

    public void Dispose() => _handle.Dispose();
}
