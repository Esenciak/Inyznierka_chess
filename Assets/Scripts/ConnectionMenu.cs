using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectionMenu : MonoBehaviour
{
	[Header("UI References")]
	public Button hostBtn;          // Przycisk "Host 
	public Button clientBtn;        // Przycisk "Client 
	public Button singleplayerBtn;  // Przycisk Singleplayer jeżeli zdążę 

	[Header("Dependencies")]
	public LobbyMenu lobbyMenu;     

	void Start()
	{
		// Obsługa przycisków bezpośrednich (tylko do testów LAN lub Single)
		if (hostBtn != null)
			hostBtn.onClick.AddListener(StartHost);

		if (clientBtn != null)
			clientBtn.onClick.AddListener(StartClient);

		if (singleplayerBtn != null)
			singleplayerBtn.onClick.AddListener(StartSingleplayer);

		if (lobbyMenu != null)
		{
			lobbyMenu.SetConnectionMenu(this);
		}
		else
		{
			lobbyMenu = GetComponent<LobbyMenu>();
			if (lobbyMenu != null)
			{
				lobbyMenu.SetConnectionMenu(this);
			}
			else
			{
				Debug.LogWarning("Brak LobbyMenu na obiekcie! Multiplayer przez internet nie zadziała.");
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

		NetworkManager.Singleton.StartHost();

		if (SceneFader.Instance != null) // Zabezpieczenie
		{
			SceneFader.FadeOutThen(() =>
			{
				NetworkManager.Singleton.SceneManager.LoadScene("Shop", LoadSceneMode.Single);
			});
		}
		else
		{
	
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

		NetworkManager.Singleton.StartClient();

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