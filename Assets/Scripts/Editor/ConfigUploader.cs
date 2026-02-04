using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;

// Ten skrypt dodaje opcję w menu Unity, żeby wysłać config do bazy
public class ConfigUploader : Editor
{
	[MenuItem("Chess/Upload Economy Config")]
	public static void UploadConfig()
	{
		// 1. Znajdź plik EconomyConfig (musi być w Resources lub podajemy ścieżkę)
		EconomyConfig config = Resources.Load<EconomyConfig>("EconomyConfig");

		if (config == null)
		{
			Debug.LogError("Nie znaleziono pliku EconomyConfig w folderze Resources!");
			return;
		}

		// 2. Serializacja do JSON
		string json = JsonUtility.ToJson(config, true);
		Debug.Log("Serialized EconomyConfig: " + json);

		// 3. Wyślij do API
		string url = "https://game-analytics-api.onrender.com/api/config"; // Zmień na swój URL
		UploadCoroutine(url, json);
	}

	private static async void UploadCoroutine(string url, string json)
	{
		using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
		{
			byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");

			var operation = request.SendWebRequest();

			while (!operation.isDone) await System.Threading.Tasks.Task.Delay(100);

			if (request.result == UnityWebRequest.Result.Success)
			{
				Debug.Log(" Economy Config wysłany pomyślnie!");
			}
			else
			{
				Debug.LogError($"Błąd wysyłania: {request.error}\n{request.downloadHandler.text}");
				Debug.Log("Serialized EconomyConfig: " + json);
			}
		}
	}
}