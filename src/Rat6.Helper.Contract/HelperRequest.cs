namespace Rat6.Helper.Contract;

public sealed record HelperRequest(
    int Id,
    string Op,
    string? Path = null,
    byte? ReportId = null,
    int? Length = null,
    string? Data = null);
