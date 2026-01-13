using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Jeśli używasz starego UI, lub TMPro jeśli nowego
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

                LobbyMenu lobbyMenu = GetComponent<LobbyMenu>();
                if (lobbyMenu == null)
                {
                        lobbyMenu = gameObject.AddComponent<LobbyMenu>();
                }
                lobbyMenu.SetConnectionMenu(this);
        }

        public void StartHost()
        {
                Debug.Log("Startuj jako HOST...");

                // 1. To musi być aktywne, żeby gra wiedziała, że to multiplayer
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
                        Debug.LogError("GameManager jest null! Upewnij się, że obiekt Manager jest włączony w scenie.");
                        return;
                }

                // 2. Start Hosta
                NetworkManager.Singleton.StartHost();

                // 3. Ładowanie sceny
                NetworkManager.Singleton.SceneManager.LoadScene("Shop", LoadSceneMode.Single);
        }
        public void StartClient()
        {
                Debug.Log("Dołączam jako KLIENT...");
                GameManager.Instance.isMultiplayer = true;
                if (GameProgress.Instance != null)
                {
                        GameProgress.Instance.isHostPlayer = false;
                }

                // Klient tylko się łączy. To Host przeniesie go do sklepu automatycznie.
                NetworkManager.Singleton.StartClient();
        }

        public void StartSingleplayer()
        {
                Debug.Log("Tryb Singleplayer");
                GameManager.Instance.isMultiplayer = false;
                if (GameProgress.Instance != null)
                {
                        GameProgress.Instance.isHostPlayer = true;
                }
                SceneManager.LoadScene("Shop");
        }
}
