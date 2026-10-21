using System.Runtime.InteropServices;

namespace Rat6.Transport.Linux;

internal static partial class HidrawNative
{
    internal const int ORdwr = 2;
    internal const int ONonblock = 0x800;
    internal const int Eagain = 11;
    internal const short PollIn = 0x001;
    internal const int GetFeatureNumber = 0x07;
    internal const int SetFeatureNumber = 0x06;

    private const uint IocReadWrite = 3u << 30;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PollFd
    {
        public int Fd;
        public short Events;
        public short Revents;
    }

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int open(string pathname, int flags);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial nint read(int fd, [Out] byte[] buffer, nuint count);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int poll(ref PollFd fds, nuint nfds, int timeout);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int ioctl(int fd, nuint request, nint argp);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int close(int fd);

    internal static int GetFeature(int fd, byte[] buffer) =>
        InvokeIoctl(fd, GetFeatureNumber, buffer);

    internal static int SetFeature(int fd, byte[] buffer) =>
        InvokeIoctl(fd, SetFeatureNumber, buffer);

    private static int InvokeIoctl(int fd, int number, byte[] buffer)
    {
        var pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return ioctl(fd, Request(number, buffer.Length), pinned.AddrOfPinnedObject());
        }
        finally
        {
            pinned.Free();
        }
    }

    internal static nuint Request(int number, int length)
    {
        return IocReadWrite
            | ((nuint)(uint)length << 16)
            | ((nuint)'H' << 8)
            | (nuint)number;
    }
}
