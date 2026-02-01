using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TelemetryHttpClient
{
    private readonly TelemetryConfig config;

    public TelemetryHttpClient(TelemetryConfig config)
    {
        this.config = config;
    }

    public IEnumerator PostBatchAsync(TelemetryRoundBatchDto batchData)
    {
        if (config == null)
        {
            yield break;
        }

        string json = TelemetryJson.SerializeBatch(batchData);
        string url = BuildBatchUrl(config);
        if (string.IsNullOrEmpty(url))
        {
            yield break;
        }

        bool success = false;
        yield return SendJsonWithRetry(url, json, config.requestTimeoutSeconds, config.maxRetries, result => success = result);
        if (!success && config.logToUnityConsole)
        {
            Debug.LogWarning("[Telemetry] Batch send failed in PostBatchAsync.");
        }
    }

    public IEnumerator SendJsonWithRetry(string url, string json, int timeoutSeconds, int maxRetries, Action<bool> onComplete)
    {
        int attempts = Mathf.Max(1, maxRetries);
        int[] backoffSeconds = { 1, 3, 7 };
        bool success = false;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            yield return SendJsonOnce(url, json, timeoutSeconds, result => success = result);
            if (success)
            {
                break;
            }

            if (attempt < attempts - 1)
            {
                int delay = backoffSeconds[Mathf.Min(attempt, backoffSeconds.Length - 1)];
                yield return new WaitForSeconds(delay);
            }
        }

        onComplete?.Invoke(success);
    }

    private IEnumerator SendJsonOnce(string url, string json, int timeoutSeconds, Action<bool> onComplete)
    {
        if (config != null && config.logToUnityConsole)
        {
            Debug.Log($"[Telemetry] Sending batch to {url}: {json}");
        }

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.Max(1, timeoutSeconds);

            yield return request.SendWebRequest();

            bool isConflict = request.responseCode == 409;
            bool success = request.result == UnityWebRequest.Result.Success || isConflict;
            if (!success)
            {
                Debug.LogError($"[Telemetry] Send failed: {request.error}\nResponse: {request.downloadHandler.text}");
            }
            else if (config != null && config.logToUnityConsole)
            {
                if (isConflict)
                {
                    Debug.LogWarning($"[Telemetry] Batch already exists on server (409). Response: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.Log($"[Telemetry] Send success. Response: {request.downloadHandler.text}");
                }
            }

            onComplete?.Invoke(success);
        }
    }

    private static string BuildBatchUrl(TelemetryConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.baseUrl))
        {
            return null;
        }

        string baseUrlTrimmed = config.baseUrl.TrimEnd('/');
        string path = string.IsNullOrEmpty(config.roundBatchEndpointPath) ? string.Empty : config.roundBatchEndpointPath.TrimStart('/');
        return string.IsNullOrEmpty(path) ? baseUrlTrimmed : $"{baseUrlTrimmed}/{path}";
    }
}
