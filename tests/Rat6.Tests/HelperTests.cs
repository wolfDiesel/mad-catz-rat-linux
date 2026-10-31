using Rat6.Helper;
using Rat6.Helper.Contract;
using Rat6.Transport.Linux;
using Xunit;

namespace Rat6.Tests;

public sealed class HelperTests
{
    [Fact]
    public void Protocol_RoundTripsRequest()
    {
        var request = new HelperRequest(7, "get", ReportId: 0x05, Length: 738);
        var parsed = HelperProtocol.DeserializeRequest(HelperProtocol.Serialize(request));

        Assert.Equal(7, parsed.Id);
        Assert.Equal("get", parsed.Op);
        Assert.Equal((byte)0x05, parsed.ReportId);
        Assert.Equal(738, parsed.Length);
    }

    [Fact]
    public void Protocol_RoundTripsResponse()
    {
        var response = HelperResponse.Success(3, "AQID");
        var parsed = HelperProtocol.DeserializeResponse(HelperProtocol.Serialize(response));

        Assert.True(parsed.Ok);
        Assert.Equal("AQID", parsed.Data);
    }

    [Fact]
    public void Session_OpenGetSetClose()
    {
        var device = new FakeHidrawDevice();
        using var session = new HelperSession(device);

        Assert.True(session.Handle(new HelperRequest(1, "open", Path: "/dev/hidraw3")).Ok);

        var get = session.Handle(new HelperRequest(2, "get", ReportId: 0x05, Length: 8));
        Assert.True(get.Ok);
        var data = Convert.FromBase64String(get.Data!);
        Assert.Equal(8, data.Length);
        Assert.Equal(0x05, data[0]);

        Assert.True(session.Handle(new HelperRequest(3, "set", Data: Convert.ToBase64String([1, 2, 3]))).Ok);
        Assert.Equal([1, 2, 3], device.LastReport);

        Assert.True(session.Handle(new HelperRequest(4, "close")).Ok);
    }

    [Fact]
    public void Session_RejectsUnknownOperation()
    {
        using var session = new HelperSession(new FakeHidrawDevice());

        var response = session.Handle(new HelperRequest(1, "nope"));

        Assert.False(response.Ok);
        Assert.Contains("unknown", response.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Session_RequiresOpenBeforeGet()
    {
        using var session = new HelperSession(new FakeHidrawDevice());

        var response = session.Handle(new HelperRequest(1, "get", ReportId: 0x05, Length: 8));

        Assert.False(response.Ok);
    }

    [Fact]
    public void Transport_TalksToFakeHelperProcess()
    {
        var helperDll = Path.Combine(AppContext.BaseDirectory, "Rat6.Helper.dll");
        Assert.True(File.Exists(helperDll), $"missing {helperDll}");

        using var transport = new PolkitHelperTransport(
            () => new HelperLaunch("dotnet", [helperDll, "--fake"]));
        transport.OpenDevice("/dev/hidraw3");

        var data = transport.GetFeature(0x05, 8);
        Assert.Equal(8, data.Length);
        Assert.Equal(0x05, data[0]);

        transport.SetFeature([1, 2, 3]);
    }
}
