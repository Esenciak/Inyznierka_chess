using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TelemetryHttpClient
{
	private TelemetryConfig _config;

	public TelemetryHttpClient(TelemetryConfig config)
	{
		_config = config;
	}

	public IEnumerator PostBatchAsync(TelemetryRoundBatchDto batchData)
	{
		// 1. Serializacja do JSON
		string json = JsonUtility.ToJson(batchData); // Lub Newtonsoft.Json jeúli masz zainstalowany

		if (_config.EnableDebugLogs)
			Debug.Log($"[Telemetry] Wysy≥anie batcha: {json}");

		// 2. Przygotowanie requestu
		using (UnityWebRequest request = new UnityWebRequest(_config.ApiUrl, "POST"))
		{
			byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");
			request.timeout = (int)_config.SendTimeout;

			// 3. Wys≥anie i czekanie
			yield return request.SendWebRequest();

			// 4. Obs≥uga odpowiedzi
			if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.LogError($"[Telemetry] B≥πd wysy≥ania: {request.error}\nOdpowiedü: {request.downloadHandler.text}");
			}
			else
			{
				if (_config.EnableDebugLogs)
					Debug.Log($"[Telemetry] Sukces! Odpowiedü serwera: {request.downloadHandler.text}");
			}
		}
	}
}