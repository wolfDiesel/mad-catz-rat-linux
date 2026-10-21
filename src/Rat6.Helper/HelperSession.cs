using Rat6.Helper.Contract;

namespace Rat6.Helper;

public sealed class HelperSession : IDisposable
{
    private const int MaxReportLength = 4096;
    private readonly IHidrawDevice _device;
    private bool _opened;

    public HelperSession(IHidrawDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public HelperResponse Handle(HelperRequest request)
    {
        try
        {
            return request.Op switch
            {
                "open" => Open(request),
                "get" => Get(request),
                "set" => Set(request),
                "close" => Close(request),
                "ping" => HelperResponse.Success(request.Id),
                _ => HelperResponse.Failure(request.Id, $"unknown operation: {request.Op}"),
            };
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or FormatException
            or ArgumentException
            or InvalidOperationException)
        {
            return HelperResponse.Failure(request.Id, exception.Message);
        }
    }

    private HelperResponse Open(HelperRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return HelperResponse.Failure(request.Id, "open requires path.");
        }

        if (_opened)
        {
            return HelperResponse.Failure(request.Id, "device is already open.");
        }

        _device.Open(request.Path);
        _opened = true;
        return HelperResponse.Success(request.Id);
    }

    private HelperResponse Get(HelperRequest request)
    {
        EnsureOpen();
        var reportId = request.ReportId
            ?? throw new FormatException("get requires reportId.");
        var length = request.Length
            ?? throw new FormatException("get requires length.");
        if (length is < 1 or > MaxReportLength)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Length), length, "Report length must be 1..4096.");
        }

        var data = _device.GetFeature(reportId, length);
        return HelperResponse.Success(request.Id, Convert.ToBase64String(data));
    }

    private HelperResponse Set(HelperRequest request)
    {
        EnsureOpen();
        if (string.IsNullOrEmpty(request.Data))
        {
            throw new FormatException("set requires data.");
        }

        var data = Convert.FromBase64String(request.Data);
        if (data.Length is < 1 or > MaxReportLength)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Data), data.Length, "Report length must be 1..4096.");
        }

        _device.SetFeature(data);
        return HelperResponse.Success(request.Id);
    }

    private HelperResponse Close(HelperRequest request)
    {
        _device.Dispose();
        _opened = false;
        return HelperResponse.Success(request.Id);
    }

    private void EnsureOpen()
    {
        if (!_opened)
        {
            throw new InvalidOperationException("device is not open.");
        }
    }

    public void Dispose() => _device.Dispose();
}
