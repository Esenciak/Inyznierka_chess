using System;
using System.IO;
using UnityEngine;

public class TelemetryFileLogger
{
	private readonly string dir;

	public TelemetryFileLogger()
	{
		dir = Path.Combine(Application.persistentDataPath, "telemetry_debug");
		Directory.CreateDirectory(dir);
		Debug.Log($"[TelemetryFileLogger] dir = {dir}");
	}

	public void WriteBatch(string json, int roundNumber)
	{
		try
		{
			string fileName = $"round_{roundNumber}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.json";
			string path = Path.Combine(dir, fileName);
			File.WriteAllText(path, json);
			Debug.Log($"[TelemetryFileLogger] Saved batch: {path}");
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"[TelemetryFileLogger] Failed to write batch: {ex.Message}");
		}
	}
}
