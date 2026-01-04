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
		// 1. Ustawiamy flagê w GameManagerze (zaraz j¹ dodamy)
		GameManager.Instance.isMultiplayer = true; 

		// 2. Odpalamy Hosta w NetworkManagerze
		NetworkManager.Singleton.StartHost();

		// 3. Host ³aduje scenê Sklepu. 
		// W NGO, Host decyduje o zmianie sceny dla wszystkich!
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