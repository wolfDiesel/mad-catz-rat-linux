using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rat6.Helper.Contract;

public static class HelperProtocol
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(HelperRequest request) =>
        JsonSerializer.Serialize(request, Options);

    public static HelperRequest DeserializeRequest(string json) =>
        JsonSerializer.Deserialize<HelperRequest>(json, Options)
        ?? throw new FormatException("empty helper request.");

    public static string Serialize(HelperResponse response) =>
        JsonSerializer.Serialize(response, Options);

    public static HelperResponse DeserializeResponse(string json) =>
        JsonSerializer.Deserialize<HelperResponse>(json, Options)
        ?? throw new FormatException("empty helper response.");
}
