using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
        [Header("UI References")]
        [SerializeField] private InputField customIdInput;
        [SerializeField] private InputField lobbyNameInput;
        [SerializeField] private InputField lobbyCodeInput;
        [SerializeField] private Dropdown lobbyDropdown;
        [SerializeField] private Text statusText;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button refreshButton;

        [Header("Networking")]
        [SerializeField] private ConnectionMenu connectionMenu;

        private readonly List<Lobby> availableLobbies = new List<Lobby>();
        private Lobby currentLobby;
        private string customIdValue = string.Empty;
        private string lobbyNameValue = string.Empty;
        private string lobbyCodeValue = string.Empty;
        private string selectedLobbyId = string.Empty;
        private string statusMessage = string.Empty;

        public void SetConnectionMenu(ConnectionMenu menu)
        {
                connectionMenu = menu;
        }

        private async void Start()
        {
                if (createLobbyButton != null)
                        createLobbyButton.onClick.AddListener(() => RunSafe(CreateLobbyAsync()));
                if (joinLobbyButton != null)
                        joinLobbyButton.onClick.AddListener(() => RunSafe(JoinLobbyAsync()));
                if (refreshButton != null)
                        refreshButton.onClick.AddListener(() => RunSafe(RefreshLobbiesAsync()));

                await InitializeServicesAsync();
        }

        private async Task InitializeServicesAsync()
        {
                try
                {
                        if (UnityServices.State != ServicesInitializationState.Initialized)
                        {
                                await UnityServices.InitializeAsync();
                        }

                        if (!AuthenticationService.Instance.IsSignedIn)
                        {
                                string customId = GetOrCreateCustomId();
                                await SignInAsync(customId);
                                SetStatus($"Zalogowano jako: {customId}");
                        }

                        await RefreshLobbiesAsync();
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd inicjalizacji usług: {ex.Message}");
                }
        }

        private async Task SignInAsync(string customId)
        {
                if (AuthenticationService.Instance.IsSignedIn)
                {
                        return;
                }

                var service = AuthenticationService.Instance;
                var method = service.GetType().GetMethod("SignInWithCustomIdAsync", new[] { typeof(string) });
                if (method != null)
                {
                        if (method.Invoke(service, new object[] { customId }) is Task task)
                        {
                                await task;
                                return;
                        }
                }

                await service.SignInAnonymouslyAsync();
        }

        private string GetOrCreateCustomId()
        {
                if (customIdInput != null && !string.IsNullOrWhiteSpace(customIdInput.text))
                {
                        PlayerPrefs.SetString("CustomId", customIdInput.text.Trim());
                        return customIdInput.text.Trim();
                }

                if (customIdInput == null && !string.IsNullOrWhiteSpace(customIdValue))
                {
                        PlayerPrefs.SetString("CustomId", customIdValue.Trim());
                        return customIdValue.Trim();
                }

                string saved = PlayerPrefs.GetString("CustomId", string.Empty);
                if (string.IsNullOrWhiteSpace(saved))
                {
                        saved = Guid.NewGuid().ToString("N");
                        PlayerPrefs.SetString("CustomId", saved);
                }

                if (customIdInput != null)
                {
                        customIdInput.text = saved;
                }
                else
                {
                        customIdValue = saved;
                }

                return saved;
        }

        private async Task RefreshLobbiesAsync()
        {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                        SetStatus("Najpierw zaloguj się.");
                        return;
                }

                try
                {
                        QueryLobbiesOptions options = new QueryLobbiesOptions
                        {
                                Count = 10
                        };
                        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
                        availableLobbies.Clear();
                        availableLobbies.AddRange(response.Results);
                        UpdateLobbyDropdown();
                        SetStatus($"Znaleziono lobby: {availableLobbies.Count}");
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd pobierania lobby: {ex.Message}");
                }
        }

        private async Task CreateLobbyAsync()
        {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                        SetStatus("Najpierw zaloguj się.");
                        return;
                }

                try
                {
                        string lobbyName = lobbyNameInput != null && !string.IsNullOrWhiteSpace(lobbyNameInput.text)
                                ? lobbyNameInput.text.Trim()
                                : !string.IsNullOrWhiteSpace(lobbyNameValue)
                                        ? lobbyNameValue.Trim()
                                : $"Lobby-{UnityEngine.Random.Range(1000, 9999)}";

                        CreateLobbyOptions options = new CreateLobbyOptions
                        {
                                IsPrivate = false
                        };

                        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
                        SetStatus($"Utworzono lobby: {currentLobby.Name} (kod: {currentLobby.LobbyCode})");

                        if (connectionMenu != null)
                        {
                                connectionMenu.StartHost();
                        }
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd tworzenia lobby: {ex.Message}");
                }
        }

        private async Task JoinLobbyAsync()
        {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                        SetStatus("Najpierw zaloguj się.");
                        return;
                }

                try
                {
                        if (lobbyCodeInput != null && !string.IsNullOrWhiteSpace(lobbyCodeInput.text))
                        {
                                currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCodeInput.text.Trim());
                        }
                        else if (lobbyCodeInput == null && !string.IsNullOrWhiteSpace(lobbyCodeValue))
                        {
                                currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCodeValue.Trim());
                        }
                        else if (lobbyDropdown != null && lobbyDropdown.value >= 0 && lobbyDropdown.value < availableLobbies.Count)
                        {
                                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(availableLobbies[lobbyDropdown.value].Id);
                        }
                        else if (!string.IsNullOrWhiteSpace(selectedLobbyId))
                        {
                                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(selectedLobbyId);
                        }
                        else
                        {
                                SetStatus("Wybierz lobby lub wpisz kod.");
                                return;
                        }

                        SetStatus($"Dołączono do lobby: {currentLobby.Name}");

                        if (connectionMenu != null)
                        {
                                connectionMenu.StartClient();
                        }
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd dołączania do lobby: {ex.Message}");
                }
        }

        private void UpdateLobbyDropdown()
        {
                if (lobbyDropdown == null)
                        return;

                lobbyDropdown.ClearOptions();
                List<string> options = new List<string>();
                foreach (Lobby lobby in availableLobbies)
                {
                        options.Add($"{lobby.Name} ({lobby.Players.Count}/2)");
                }
                lobbyDropdown.AddOptions(options);
        }

        private void SetStatus(string message)
        {
                statusMessage = message;
                if (statusText != null)
                {
                        statusText.text = message;
                }
                Debug.Log(message);
        }

        private async void RunSafe(Task task)
        {
                try
                {
                        await task;
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd: {ex.Message}");
                }
        }

        private void OnGUI()
        {
                if (customIdInput != null || lobbyNameInput != null || lobbyCodeInput != null || lobbyDropdown != null)
                {
                        return;
                }

                GUILayout.BeginArea(new Rect(20, 20, 320, 420), GUI.skin.box);
                GUILayout.Label("Lobby (UGS)");

                GUILayout.Label("Custom ID:");
                customIdValue = GUILayout.TextField(customIdValue, 32);

                GUILayout.Label("Nazwa lobby:");
                lobbyNameValue = GUILayout.TextField(lobbyNameValue, 32);

                GUILayout.Label("Kod lobby (opcjonalnie):");
                lobbyCodeValue = GUILayout.TextField(lobbyCodeValue, 16);

                if (GUILayout.Button("Odśwież listę"))
                {
                        RunSafe(RefreshLobbiesAsync());
                }

                if (availableLobbies.Count > 0)
                {
                        GUILayout.Label("Dostępne lobby:");
                        foreach (Lobby lobby in availableLobbies)
                        {
                                if (GUILayout.Button($"{lobby.Name} ({lobby.Players.Count}/2)"))
                                {
                                        lobbyCodeValue = string.Empty;
                                        selectedLobbyId = lobby.Id;
                                        RunSafe(JoinLobbyAsync());
                                        break;
                                }
                        }
                }
                else
                {
                        GUILayout.Label("Brak lobby na liście.");
                }

                if (GUILayout.Button("Utwórz lobby (host)"))
                {
                        RunSafe(CreateLobbyAsync());
                }

                if (GUILayout.Button("Dołącz (kod lub lista)"))
                {
                        RunSafe(JoinLobbyAsync());
                }

                if (!string.IsNullOrWhiteSpace(statusMessage))
                {
                        GUILayout.Label(statusMessage);
                }
                GUILayout.EndArea();
        }
}
