using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TelemetryHttpClient
{
	private readonly MonoBehaviour host;

	public TelemetryHttpClient(MonoBehaviour host)
	{
		this.host = host;
	}

	public void SendJsonWithRetry(string url, string json, int timeoutSeconds, int maxRetries, Action<bool> onComplete)
	{
		host.StartCoroutine(SendJsonWithRetryCoroutine(url, json, timeoutSeconds, maxRetries, onComplete));
	}

	private IEnumerator SendJsonWithRetryCoroutine(string url, string json, int timeoutSeconds, int maxRetries, Action<bool> onComplete)
	{
		bool success = false;
		int attempts = Mathf.Max(1, maxRetries);
		int[] backoffSeconds = { 1, 3, 7 };

		for (int attempt = 0; attempt < attempts; attempt++)
		{
			using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
			{
				byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
				request.uploadHandler = new UploadHandlerRaw(bodyRaw);
				request.downloadHandler = new DownloadHandlerBuffer();
				request.SetRequestHeader("Content-Type", "application/json");
				request.timeout = timeoutSeconds;

				Debug.Log($"[TelemetryHttpClient] POST attempt {attempt + 1}/{attempts} -> {url}");
				yield return request.SendWebRequest();

				Debug.Log($"[TelemetryHttpClient] result={request.result} code={request.responseCode} err={request.error}");

				if (request.result == UnityWebRequest.Result.Success &&
					request.responseCode >= 200 && request.responseCode < 300)
				{
					success = true;
					break;
				}
			}

			if (attempt < attempts - 1)
			{
				int delay = attempt < backoffSeconds.Length ? backoffSeconds[attempt] : backoffSeconds[backoffSeconds.Length - 1];
				yield return new WaitForSeconds(delay);
			}
		}

		onComplete?.Invoke(success);
	}
}
