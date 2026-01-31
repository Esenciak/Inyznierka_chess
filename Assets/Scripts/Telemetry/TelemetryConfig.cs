using UnityEngine;
using WebSocketSharp.Server;

[CreateAssetMenu(menuName = "Chess/Telemetry Config", fileName = "TelemetryConfig")]
public class TelemetryConfig : ScriptableObject
{
    [Header("Endpoint")]
    public string baseUrl = "https://127.0.0.1:1";
    public string roundBatchEndpointPath = "/telemetry/round";

    [Header("Transport")]
    public int requestTimeoutSeconds = 10;
    public int maxRetries = 3;
    public int flushIntervalSeconds = 15;

    [Header("Behavior")]
    public bool enableTelemetry = true;
    public bool logToUnityConsole = true;

    [Header("Debug")]
    public bool showBattleCoordsDebug = false;

    [Header("save to file")]
	public bool writeBatchesToDisk = true;
	public bool writeEventsToDisk = true; // opcjonalnie

}
