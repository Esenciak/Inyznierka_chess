using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Jeœli u¿ywasz starego UI, lub TMPro jeœli nowego
using TMPro;
public class ConnectionMenu : MonoBehaviour
{
	[Header("UI References")]
	public Button hostBtn;
	public Button clientBtn;
	public Button singleplayerBtn;

	void Start()
	{
		// Przypisanie funkcji do przycisków
		hostBtn.onClick.AddListener(StartHost);
		clientBtn.onClick.AddListener(StartClient);

		if (singleplayerBtn != null)
			singleplayerBtn.onClick.AddListener(StartSingleplayer);
	}

	void StartHost()
	{
		Debug.Log("Startujê jako HOST...");

		// 1. To musi byæ aktywne, ¿eby gra wiedzia³a, ¿e to multiplayer
		if (GameManager.Instance != null)
		{
			GameManager.Instance.isMultiplayer = true;
		}
		else
		{
			Debug.LogError("GameManager jest null! Upewnij siê, ¿e obiekt Manager jest w³¹czony w scenie.");
			return;
		}

		// 2. Start Hosta
		NetworkManager.Singleton.StartHost();

		// 3. £adowanie sceny
		NetworkManager.Singleton.SceneManager.LoadScene("Shop", LoadSceneMode.Single);
	}
	void StartClient()
	{
		Debug.Log("Do³¹czam jako KLIENT...");
		GameManager.Instance.isMultiplayer = true;

		// Klient tylko siê ³¹czy. To Host przeniesie go do sklepu automatycznie.
		NetworkManager.Singleton.StartClient();
	}

	void StartSingleplayer()
	{
		Debug.Log("Tryb Singleplayer");
		GameManager.Instance.isMultiplayer = false;
		SceneManager.LoadScene("Shop");
	}
}