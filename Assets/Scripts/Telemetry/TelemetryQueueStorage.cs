using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TelemetryQueueStorage
{
    private readonly string queueDirectory;

    public TelemetryQueueStorage()
    {
        queueDirectory = Path.Combine(Application.persistentDataPath, "telemetry_queue");
		Debug.Log($"[TelemetryQueueStorage] queueDirectory = {queueDirectory}");
	}

    public void EnsureQueueDirectory()
    {
        if (!Directory.Exists(queueDirectory))
        {
            Directory.CreateDirectory(queueDirectory);
        }
    }

    public void SaveBatch(string json)
    {
		Debug.Log($"[TelemetryQueueStorage] Saving batch to {queueDirectory}");

		try
		{
            EnsureQueueDirectory();
            string fileName = $"batch_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.json";
            string finalPath = Path.Combine(queueDirectory, fileName);
            string tempPath = finalPath + ".tmp";

            File.WriteAllText(tempPath, json);
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }
            File.Move(tempPath, finalPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TelemetryQueueStorage] Failed to save batch: {ex.Message}");
        }
    }

    public List<string> LoadQueuedBatchFiles()
    {
        List<string> files = new List<string>();
        try
        {
            if (!Directory.Exists(queueDirectory))
            {
                return files;
            }

            files.AddRange(Directory.GetFiles(queueDirectory, "*.json"));
            files.Sort();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TelemetryQueueStorage] Failed to load queue: {ex.Message}");
        }

        return files;
    }

    public string ReadBatch(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TelemetryQueueStorage] Failed to read batch: {ex.Message}");
            return null;
        }
    }

    public void DeleteBatch(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TelemetryQueueStorage] Failed to delete batch: {ex.Message}");
        }
    }



}
