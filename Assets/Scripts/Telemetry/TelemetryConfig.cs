using UnityEngine;

[CreateAssetMenu(menuName = "Telemetry/Telemetry Config", fileName = "TelemetryConfig")]
public class TelemetryConfig : ScriptableObject
{
	[Header("Main")]
	public bool enableTelemetry = true;
	public bool logToUnityConsole = true;
	public bool writeBatchesToDisk = true;
    public bool showBattleCoordsDebug = false;

	[Header("Networking")]
	public int requestTimeoutSeconds = 10;
	public int maxRetries = 3;
	public int flushIntervalSeconds = 15;

	[Header("API")]
	[SerializeField] private string _baseUrl = "https://game-analytics-api.onrender.com";
	[SerializeField] private string _roundBatchEndpointPath = "/api/logs/batch";

	public string baseUrl => _baseUrl;
	public string roundBatchEndpointPath => _roundBatchEndpointPath;
}
