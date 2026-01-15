using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectionMenu : MonoBehaviour
{
	[Header("UI References")]
	public Button hostBtn;          // Przycisk "Host (LAN/Direct)" - Opcjonalny
	public Button clientBtn;        // Przycisk "Client (LAN/Direct)" - Opcjonalny
	public Button singleplayerBtn;  // Przycisk Singleplayer

	[Header("Dependencies")]
	public LobbyMenu lobbyMenu;     // Przypisz tu skrypt LobbyMenu w Inspektorze!

	void Start()
	{
		// Obsługa przycisków bezpośrednich (tylko do testów LAN lub Single)
		if (hostBtn != null)
			hostBtn.onClick.AddListener(StartHost);

		if (clientBtn != null)
			clientBtn.onClick.AddListener(StartClient);

		if (singleplayerBtn != null)
			singleplayerBtn.onClick.AddListener(StartSingleplayer);

		// Łączymy się z LobbyMenu, jeśli zostało przypisane
		if (lobbyMenu != null)
		{
			lobbyMenu.SetConnectionMenu(this);
		}
		else
		{
			// Próba znalezienia, jeśli zapomniałeś przypisać
			lobbyMenu = GetComponent<LobbyMenu>();
			if (lobbyMenu != null)
			{
				lobbyMenu.SetConnectionMenu(this);
			}
			else
			{
				Debug.LogWarning("Brak LobbyMenu na obiekcie! Multiplayer przez internet nie zadziała z tego poziomu.");
			}
		}
	}

	public void StartHost()
	{
		Debug.Log("[ConnectionMenu] Startuj jako HOST...");

		if (GameManager.Instance != null)
		{
			GameManager.Instance.isMultiplayer = true;
			if (GameProgress.Instance != null)
			{
				GameProgress.Instance.isHostPlayer = true;
			}
		}
		else
		{
			Debug.LogError("GameManager jest null!");
			return;
		}

		// WAŻNE: Jeśli startujemy przez Lobby, Relay został już skonfigurowany w LobbyMenu.
		// Jeśli kliknąłeś zwykły przycisk "Host", Relay NIE jest skonfigurowany i gra ruszy na LAN (127.0.0.1).
		NetworkManager.Singleton.StartHost();

		// Przenosimy graczy do sklepu
		// SceneFader to twój system przejść
		if (SceneFader.Instance != null) // Zabezpieczenie
		{
			SceneFader.FadeOutThen(() =>
			{
				NetworkManager.Singleton.SceneManager.LoadScene("Shop", LoadSceneMode.Single);
			});
		}
		else
		{
			// Fallback jeśli nie ma fadera
			NetworkManager.Singleton.SceneManager.LoadScene("Shop", LoadSceneMode.Single);
		}
	}

	public void StartClient()
	{
		Debug.Log("[ConnectionMenu] Dołączam jako KLIENT...");

		if (GameManager.Instance != null)
		{
			GameManager.Instance.isMultiplayer = true;
			if (GameProgress.Instance != null)
			{
				GameProgress.Instance.isHostPlayer = false;
			}
		}

		// Tutaj UnityTransport musi mieć już dane z Relay (ustawione przez LobbyMenu)
		NetworkManager.Singleton.StartClient();

		// Klient NIE ładuje sceny sam. Czeka aż Host go pociągnie za sobą.
	}

	public void StartSingleplayer()
	{
		Debug.Log("[ConnectionMenu] Tryb Singleplayer");

		if (GameManager.Instance != null)
			GameManager.Instance.isMultiplayer = false;

		if (GameProgress.Instance != null)
			GameProgress.Instance.isHostPlayer = true;

		// W singlu ładujemy scenę lokalnie
		if (SceneFader.Instance != null)
			SceneFader.LoadSceneWithFade("Shop");
		else
			SceneManager.LoadScene("Shop");
	}
}