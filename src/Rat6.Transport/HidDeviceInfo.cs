namespace Rat6.Transport;

public sealed record HidDeviceInfo(
    string Path,
    ushort VendorId,
    ushort ProductId,
    string Name,
    string Phys);
