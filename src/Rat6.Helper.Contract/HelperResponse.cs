namespace Rat6.Helper.Contract;

public sealed record HelperResponse(
    int Id,
    bool Ok,
    string? Error = null,
    string? Data = null)
{
    public static HelperResponse Success(int id, string? data = null) => new(id, true, null, data);

    public static HelperResponse Failure(int id, string error) => new(id, false, error);
}
