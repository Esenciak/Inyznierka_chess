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
        private const string LobbyPasswordKey = "password";
        private const string LobbyHasPasswordKey = "hasPassword";
        private const string AuthIdPrefsKey = "AuthId";
        private const string PlayerNamePrefsKey = "PlayerName";
        private static readonly (string Label, Color Color)[] TileColorOptions =
        {
                ("Niebieski", new Color(0.25f, 0.55f, 0.95f)),
                ("Zielony", new Color(0.2f, 0.75f, 0.4f)),
                ("Czerwony", new Color(0.9f, 0.3f, 0.3f)),
                ("Fioletowy", new Color(0.6f, 0.35f, 0.85f))
        };

        [Header("UI References")]
        [SerializeField] private InputField customIdInput;
        [SerializeField] private InputField lobbyNameInput;
        [SerializeField] private InputField lobbyPasswordInput;
        [SerializeField] private InputField joinPasswordInput;
        [SerializeField] private Dropdown lobbyDropdown;
        [SerializeField] private Dropdown tileColorDropdown;
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
        private string lobbyPasswordValue = string.Empty;
        private string joinPasswordValue = string.Empty;
        private string selectedLobbyId = string.Empty;
        private string statusMessage = string.Empty;
        private int selectedTileColorIndex;

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
                InitializeColorDropdown();
                ApplyLocalTileColorSelection();
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

                ApplyLocalTileColorSelection();
                string authId = GetOrCreateAuthId();
                await SignInAsync(authId);
                PlayerPrefs.SetString(PlayerNamePrefsKey, username);
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

        private string GetOrCreateAuthId()
        {
                string saved = PlayerPrefs.GetString(AuthIdPrefsKey, string.Empty);
                if (string.IsNullOrWhiteSpace(saved))
                {
                        saved = Guid.NewGuid().ToString("N");
                        PlayerPrefs.SetString(AuthIdPrefsKey, saved);
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

                string saved = PlayerPrefs.GetString(PlayerNamePrefsKey, string.Empty);
                if (string.IsNullOrWhiteSpace(saved))
                {
                        saved = PlayerPrefs.GetString("CustomId", string.Empty);
                }
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

                ApplyLocalTileColorSelection();
                string colorHex = $"#{ColorUtility.ToHtmlStringRGBA(GetSelectedTileColor())}";
                return new Player
                {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                                { PlayerNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                                { LobbyState.PlayerColorKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, colorHex) }
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
                        string lobbyPassword = GetLobbyPasswordInput();
                        if (!string.IsNullOrWhiteSpace(lobbyPassword))
                        {
                                options.Data = new Dictionary<string, DataObject>
                                {
                                        { LobbyPasswordKey, new DataObject(DataObject.VisibilityOptions.Public, lobbyPassword) },
                                        { LobbyHasPasswordKey, new DataObject(DataObject.VisibilityOptions.Public, "1") }
                                };
                        }

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
                        Lobby selectedLobby = null;
                        if (lobbyDropdown != null && lobbyDropdown.value >= 0 && lobbyDropdown.value < availableLobbies.Count)
                        {
                                selectedLobby = availableLobbies[lobbyDropdown.value];
                                if (!ValidateLobbyPassword(selectedLobby))
                                {
                                        return;
                                }
                                localName = EnsureUniqueName(localName, availableLobbies[lobbyDropdown.value]);
                                JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
                                {
                                        Player = BuildLocalPlayer(localName)
                                };
                                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(availableLobbies[lobbyDropdown.value].Id, options);
                        }
                        else if (!string.IsNullOrWhiteSpace(selectedLobbyId))
                        {
                                selectedLobby = FindLobbyById(selectedLobbyId);
                                if (!ValidateLobbyPassword(selectedLobby))
                                {
                                        return;
                                }
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
                        string label = $"{lobby.Name} ({lobby.Players.Count}/2)";
                        if (HasLobbyPassword(lobby))
                        {
                                label += " [Prywatne]";
                        }
                        options.Add(label);
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

                GUILayout.Label("Hasło lobby (opcjonalne):", labelStyle);
                lobbyPasswordValue = GUILayout.PasswordField(lobbyPasswordValue, '*', 32, textFieldStyle, GUILayout.Height(40));

                GUILayout.Label("Kolor kafelków (kolor 1):", labelStyle);
                selectedTileColorIndex = GUILayout.SelectionGrid(selectedTileColorIndex, GetTileColorLabels(), 2, buttonStyle, GUILayout.Height(80));
                ApplyLocalTileColorSelection();

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
                                string label = $"{lobby.Name} ({lobby.Players.Count}/2)";
                                if (HasLobbyPassword(lobby))
                                {
                                        label += " [Prywatne]";
                                }
                                if (GUILayout.Button(label, buttonStyle, GUILayout.Height(45)))
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
                GUILayout.Label("Hasło dołączania:", labelStyle);
                joinPasswordValue = GUILayout.PasswordField(joinPasswordValue, '*', 32, textFieldStyle, GUILayout.Height(40));

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
                        if (lobby.Players.Count < lobby.MaxPlayers && !HasLobbyPassword(lobby))
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

        private void InitializeColorDropdown()
        {
                if (tileColorDropdown == null)
                {
                        return;
                }

                tileColorDropdown.ClearOptions();
                tileColorDropdown.AddOptions(new List<string>(GetTileColorLabels()));
                tileColorDropdown.onValueChanged.RemoveAllListeners();
                tileColorDropdown.onValueChanged.AddListener(index =>
                {
                        selectedTileColorIndex = index;
                        ApplyLocalTileColorSelection();
                });
                tileColorDropdown.value = Mathf.Clamp(selectedTileColorIndex, 0, TileColorOptions.Length - 1);
        }

        private void ApplyLocalTileColorSelection()
        {
                LobbyState.SetLocalTileColor1(GetSelectedTileColor());
        }

        private Color GetSelectedTileColor()
        {
                if (TileColorOptions.Length == 0)
                {
                        return Color.white;
                }

                int index = Mathf.Clamp(selectedTileColorIndex, 0, TileColorOptions.Length - 1);
                return TileColorOptions[index].Color;
        }

        private string[] GetTileColorLabels()
        {
                string[] labels = new string[TileColorOptions.Length];
                for (int i = 0; i < TileColorOptions.Length; i++)
                {
                        labels[i] = TileColorOptions[i].Label;
                }
                return labels;
        }

        private string GetLobbyPasswordInput()
        {
                if (lobbyPasswordInput != null && !string.IsNullOrWhiteSpace(lobbyPasswordInput.text))
                {
                        return lobbyPasswordInput.text.Trim();
                }

                if (!string.IsNullOrWhiteSpace(lobbyPasswordValue))
                {
                        return lobbyPasswordValue.Trim();
                }

                return string.Empty;
        }

        private string GetJoinPasswordInput()
        {
                if (joinPasswordInput != null && !string.IsNullOrWhiteSpace(joinPasswordInput.text))
                {
                        return joinPasswordInput.text.Trim();
                }

                if (!string.IsNullOrWhiteSpace(joinPasswordValue))
                {
                        return joinPasswordValue.Trim();
                }

                if (lobbyPasswordInput != null && !string.IsNullOrWhiteSpace(lobbyPasswordInput.text))
                {
                        return lobbyPasswordInput.text.Trim();
                }

                if (!string.IsNullOrWhiteSpace(lobbyPasswordValue))
                {
                        return lobbyPasswordValue.Trim();
                }

                return string.Empty;
        }

        private bool HasLobbyPassword(Lobby lobby)
        {
                if (lobby?.Data == null)
                {
                        return false;
                }

                if (lobby.Data.TryGetValue(LobbyPasswordKey, out var data) && !string.IsNullOrWhiteSpace(data.Value))
                {
                        return true;
                }

                if (lobby.Data.TryGetValue(LobbyHasPasswordKey, out var hasData) && hasData.Value == "1")
                {
                        return true;
                }

                return false;
        }

        private bool ValidateLobbyPassword(Lobby lobby)
        {
                if (lobby == null || !HasLobbyPassword(lobby))
                {
                        return true;
                }

                string password = GetJoinPasswordInput();
                if (string.IsNullOrWhiteSpace(password))
                {
                        SetStatus("Podaj hasło do lobby.");
                        return false;
                }

                if (lobby.Data != null && lobby.Data.TryGetValue(LobbyPasswordKey, out var data)
                        && string.Equals(data.Value, password, StringComparison.Ordinal))
                {
                        return true;
                }

                SetStatus("Podaj poprawne hasło do lobby.");
                return false;
        }

        private Lobby FindLobbyById(string lobbyId)
        {
                if (string.IsNullOrWhiteSpace(lobbyId))
                {
                        return null;
                }

                foreach (var lobby in availableLobbies)
                {
                        if (lobby.Id == lobbyId)
                        {
                                return lobby;
                        }
                }

                return null;
        }
}
