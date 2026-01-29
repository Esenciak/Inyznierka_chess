using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public static class TelemetryJson
{
	private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
	{
		ContractResolver = new CamelCasePropertyNamesContractResolver(),
		NullValueHandling = NullValueHandling.Ignore
	};

	public static string SerializeBatch(TelemetryRoundBatchDto batch)
	{
		return JsonConvert.SerializeObject(batch, Settings);
	}

	public static TelemetryRoundBatchDto DeserializeBatch(string json)
	{
		return JsonConvert.DeserializeObject<TelemetryRoundBatchDto>(json, Settings);
	}
}
