using System.Text.Json;
using Rat6.Helper;
using Rat6.Helper.Contract;

if (args.Contains("--version"))
{
    Console.WriteLine("rat6-helper 1.0");
    return 0;
}

if (args.Length >= 2 && args[0] == "--check")
{
    try
    {
        var supported = HidrawDevice.IsSupportedPath(args[1]);
        Console.WriteLine(supported ? "supported" : "not supported");
        return supported ? 0 : 2;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
    {
        Console.Error.WriteLine(exception.Message);
        return 2;
    }
}

var fake = args.Contains("--fake");
if (!fake && !Environment.IsPrivilegedProcess)
{
    Console.Error.WriteLine("rat6-helper: root is required (run via pkexec).");
    return 1;
}

IHidrawDevice device = fake ? new FakeHidrawDevice() : new HidrawDevice();
using var session = new HelperSession(device);

string? line;
while ((line = Console.In.ReadLine()) is not null)
{
    HelperRequest request;
    try
    {
        request = HelperProtocol.DeserializeRequest(line);
    }
    catch (Exception exception) when (exception is JsonException or FormatException)
    {
        Console.Out.WriteLine(
            HelperProtocol.Serialize(HelperResponse.Failure(0, $"invalid request: {exception.Message}")));
        Console.Out.Flush();
        continue;
    }

    var response = session.Handle(request);
    Console.Out.WriteLine(HelperProtocol.Serialize(response));
    Console.Out.Flush();

    if (request.Op == "close")
    {
        break;
    }
}

return 0;
