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
        private const string PlayerNameKey = "name";

        [Header("UI References")]
        [SerializeField] private InputField customIdInput;
        [SerializeField] private InputField lobbyNameInput;
        [SerializeField] private Dropdown lobbyDropdown;
        [SerializeField] private Text statusText;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button quickPlayButton;

        [Header("Networking")]
        [SerializeField] private ConnectionMenu connectionMenu;

        private readonly List<Lobby> availableLobbies = new List<Lobby>();
        private Lobby currentLobby;
        private string customIdValue = string.Empty;
        private string lobbyNameValue = string.Empty;
        private string selectedLobbyId = string.Empty;
        private string statusMessage = string.Empty;

        public void SetConnectionMenu(ConnectionMenu menu)
        {
                connectionMenu = menu;
        }

        private async void Awake()
        {
                await InitializeServicesAsync();
        }

        private void Start()
        {
                if (createLobbyButton != null)
                        createLobbyButton.onClick.AddListener(() => RunSafe(CreateLobbyAsync()));
                if (loginButton != null)
                        loginButton.onClick.AddListener(() => RunSafe(LoginAsync()));
                if (joinLobbyButton != null)
                        joinLobbyButton.onClick.AddListener(() => RunSafe(JoinLobbyAsync()));
                if (refreshButton != null)
                        refreshButton.onClick.AddListener(() => RunSafe(RefreshLobbiesAsync()));
                if (quickPlayButton != null)
                        quickPlayButton.onClick.AddListener(() => RunSafe(QuickPlayAsync()));
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
                                SetStatus("Wpisz username i kliknij Zaloguj.");
                                return;
                        }

                        await RefreshLobbiesAsync();
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd inicjalizacji usług: {ex.Message}");
                }
        }

        private async Task LoginAsync()
        {
                if (AuthenticationService.Instance.IsSignedIn)
                {
                        SetStatus("Już zalogowano.");
                        await RefreshLobbiesAsync();
                        return;
                }

                string username = GetUsernameInput();
                if (string.IsNullOrWhiteSpace(username))
                {
                        SetStatus("Podaj username przed logowaniem.");
                        return;
                }

                await SignInAsync(username);
                PlayerPrefs.SetString("CustomId", username);
                LobbyState.SetLocalPlayerName(username);
                SetStatus($"Zalogowano jako: {username}");
                await RefreshLobbiesAsync();
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

        private string GetUsernameInput()
        {
                if (customIdInput != null && !string.IsNullOrWhiteSpace(customIdInput.text))
                {
                        return customIdInput.text.Trim();
                }

                if (!string.IsNullOrWhiteSpace(customIdValue))
                {
                        return customIdValue.Trim();
                }

                string saved = PlayerPrefs.GetString("CustomId", string.Empty);
                if (!string.IsNullOrWhiteSpace(saved))
                {
                        return saved;
                }

                return string.Empty;
        }

        private string ResolveLocalPlayerName()
        {
                string username = GetUsernameInput();
                if (string.IsNullOrWhiteSpace(username))
                {
                        username = PlayerPrefs.GetString("CustomId", string.Empty);
                }

                if (string.IsNullOrWhiteSpace(username))
                {
                        username = "Player";
                }

                return username.Trim();
        }

        private Player BuildLocalPlayer(string playerName)
        {
                if (string.IsNullOrWhiteSpace(playerName))
                {
                        playerName = "Player";
                }

                return new Player
                {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                                { PlayerNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
                        }
                };
        }

        private string EnsureUniqueName(string desiredName, Lobby lobby)
        {
                if (lobby == null || lobby.Players == null || lobby.Players.Count == 0)
                {
                        return desiredName;
                }

                string baseName = string.IsNullOrWhiteSpace(desiredName) ? "Player" : desiredName.Trim();
                string candidate = baseName;
                int suffix = 1;

                while (IsNameTaken(candidate, lobby))
                {
                        candidate = $"{baseName}{suffix}";
                        suffix++;
                }

                return candidate;
        }

        private bool IsNameTaken(string name, Lobby lobby)
        {
                if (lobby == null || lobby.Players == null)
                {
                        return false;
                }

                foreach (var player in lobby.Players)
                {
                        if (player.Data != null
                                && player.Data.TryGetValue(PlayerNameKey, out var data)
                                && string.Equals(data.Value, name, StringComparison.OrdinalIgnoreCase))
                        {
                                return true;
                        }
                }

                return false;
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

                        string localName = ResolveLocalPlayerName();
                        CreateLobbyOptions options = new CreateLobbyOptions
                        {
                                IsPrivate = false,
                                Player = BuildLocalPlayer(localName)
                        };

                        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
                        SetStatus($"Utworzono lobby: {currentLobby.Name} (kod: {currentLobby.LobbyCode})");
                        LobbyState.RegisterLobby(currentLobby.Id, true);
                        LobbyState.UpdateFromLobby(currentLobby, AuthenticationService.Instance.PlayerId);
                        ResetProgressForNewLobby();

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
                        string localName = ResolveLocalPlayerName();
                        if (lobbyDropdown != null && lobbyDropdown.value >= 0 && lobbyDropdown.value < availableLobbies.Count)
                        {
                                localName = EnsureUniqueName(localName, availableLobbies[lobbyDropdown.value]);
                                JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
                                {
                                        Player = BuildLocalPlayer(localName)
                                };
                                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(availableLobbies[lobbyDropdown.value].Id, options);
                        }
                        else if (!string.IsNullOrWhiteSpace(selectedLobbyId))
                        {
                                JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
                                {
                                        Player = BuildLocalPlayer(localName)
                                };
                                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(selectedLobbyId, options);
                        }
                        else
                        {
                                SetStatus("Wybierz lobby z listy.");
                                return;
                        }

                        SetStatus($"Dołączono do lobby: {currentLobby.Name}");
                        LobbyState.RegisterLobby(currentLobby.Id, false);
                        LobbyState.UpdateFromLobby(currentLobby, AuthenticationService.Instance.PlayerId);
                        ResetProgressForNewLobby();

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
                if (customIdInput != null || lobbyNameInput != null || lobbyDropdown != null)
                {
                        return;
                }

                float panelWidth = Mathf.Min(720f, Screen.width * 0.9f);
                float panelHeight = Mathf.Min(800f, Screen.height * 0.9f);
                float x = (Screen.width - panelWidth) * 0.5f;
                float y = (Screen.height - panelHeight) * 0.5f;

                GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
                {
                        fontSize = 32,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                };
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
                GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 20 };

                GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight), GUI.skin.box);
                GUILayout.Label("Lobby (UGS)", titleStyle);
                GUILayout.Space(10);

                GUILayout.Label("Username:", labelStyle);
                customIdValue = GUILayout.TextField(customIdValue, 32, textFieldStyle, GUILayout.Height(40));

                GUILayout.Label("Nazwa lobby:", labelStyle);
                lobbyNameValue = GUILayout.TextField(lobbyNameValue, 32, textFieldStyle, GUILayout.Height(40));

                GUILayout.Space(10);

                if (GUILayout.Button("Zaloguj", buttonStyle, GUILayout.Height(50)))
                {
                        RunSafe(LoginAsync());
                }

                if (GUILayout.Button("Odśwież listę", buttonStyle, GUILayout.Height(50)))
                {
                        RunSafe(RefreshLobbiesAsync());
                }

                if (availableLobbies.Count > 0)
                {
                        GUILayout.Space(10);
                        GUILayout.Label("Dostępne lobby:", labelStyle);
                        foreach (Lobby lobby in availableLobbies)
                        {
                                if (GUILayout.Button($"{lobby.Name} ({lobby.Players.Count}/2)", buttonStyle, GUILayout.Height(45)))
                                {
                                        selectedLobbyId = lobby.Id;
                                        RunSafe(JoinLobbyAsync());
                                        break;
                                }
                        }
                }
                else
                {
                        GUILayout.Space(10);
                        GUILayout.Label("Brak lobby na liście.", labelStyle);
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Utwórz lobby (host)", buttonStyle, GUILayout.Height(55)))
                {
                        RunSafe(CreateLobbyAsync());
                }

                if (GUILayout.Button("Quick Play (dołącz lub utwórz)", buttonStyle, GUILayout.Height(55)))
                {
                        RunSafe(QuickPlayAsync());
                }

                if (GUILayout.Button("Dołącz (z listy)", buttonStyle, GUILayout.Height(55)))
                {
                        RunSafe(JoinLobbyAsync());
                }

                if (!string.IsNullOrWhiteSpace(statusMessage))
                {
                        GUILayout.Space(10);
                        GUILayout.Label(statusMessage, labelStyle);
                }
                GUILayout.EndArea();
        }

        private async Task QuickPlayAsync()
        {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                        SetStatus("Najpierw zaloguj się.");
                        return;
                }

                await RefreshLobbiesAsync();
                Lobby openLobby = null;
                foreach (Lobby lobby in availableLobbies)
                {
                        if (lobby.Players.Count < lobby.MaxPlayers)
                        {
                                openLobby = lobby;
                                break;
                        }
                }

                if (openLobby != null)
                {
                        selectedLobbyId = openLobby.Id;
                        await JoinLobbyAsync();
                        return;
                }

                await CreateLobbyAsync();
        }

        private void ResetProgressForNewLobby()
        {
                if (GameProgress.Instance != null)
                {
                        GameProgress.Instance.ResetProgressForNewLobby();
                }
        }
}
