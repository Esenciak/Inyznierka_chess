using System.Text.Json;
using System.Text.Json.Serialization;

public static class TelemetryJson
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string SerializeBatch(TelemetryRoundBatchDto batch)
    {
        return JsonSerializer.Serialize(batch, Options);
    }

    public static TelemetryRoundBatchDto DeserializeBatch(string json)
    {
        return JsonSerializer.Deserialize<TelemetryRoundBatchDto>(json, Options);
    }
}
