using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
        private const string PlayerNameKey = "name";
        private const string SessionCodeKey = "sessionCode";
        private const string AuthIdPrefsKey = "AuthId";
        private const string PlayerNamePrefsKey = "PlayerName";
        private const string SessionType = "chess-session";
        [Header("UI References")]
        [SerializeField] private InputField customIdInput;
        [SerializeField] private InputField lobbyNameInput;
        [SerializeField] private Dropdown lobbyDropdown;
        [SerializeField] private Text statusText;
        [SerializeField] private Text activePlayersText;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button quickPlayButton;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject lobbyPanel;

        [Header("Networking")]
        [SerializeField] private ConnectionMenu connectionMenu;

        private readonly List<Lobby> availableLobbies = new List<Lobby>();
        private Lobby currentLobby;
        private string customIdValue = string.Empty;
        private string lobbyNameValue = string.Empty;
        private string selectedLobbyId = string.Empty;
        private string statusMessage = string.Empty;
        private Coroutine lobbyPollCoroutine;
        private Coroutine lobbyListPollCoroutine;
        private DateTime nextLobbyRefreshAllowedAt = DateTime.MinValue;
        private const float LobbyListPollIntervalSeconds = 30f;

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
                InitializeNameInput();
                UpdatePanelVisibility(AuthenticationService.Instance.IsSignedIn);
                StartLobbyPolling();
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

                        await EnsureMultiplayerServiceInitializedAsync();

                        if (!AuthenticationService.Instance.IsSignedIn)
                        {
                                SetStatus("Wpisz username i kliknij Zaloguj.");
                                UpdatePanelVisibility(false);
                                return;
                        }

                        UpdatePanelVisibility(true);
                        StartLobbyPolling();
                        StartLobbyListPolling();
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

                string authId = GetOrCreateAuthId();
                await SignInAsync(authId);
                await EnsureMultiplayerServiceInitializedAsync();
                PlayerPrefs.SetString(PlayerNamePrefsKey, username);
                LobbyState.SetLocalPlayerName(username);
                SetStatus($"Zalogowano jako: {username}");
                UpdatePanelVisibility(true);
                StartLobbyPolling();
                StartLobbyListPolling();
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

        private async Task EnsureMultiplayerServiceInitializedAsync()
        {
                if (MultiplayerService.Instance == null)
                {
                        return;
                }

                var method = MultiplayerService.Instance.GetType().GetMethod("InitializeAsync", Type.EmptyTypes);
                if (method == null)
                {
                        return;
                }

                if (method.Invoke(MultiplayerService.Instance, null) is Task task)
                {
                        await task;
                }
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

                if (DateTime.UtcNow < nextLobbyRefreshAllowedAt)
                {
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
                        UpdateActivePlayersCount();
                        SetStatus($"Znaleziono lobby: {availableLobbies.Count}");
                        nextLobbyRefreshAllowedAt = DateTime.MinValue;
                }
                catch (Exception ex)
                {
                        if (ex.Message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
                        {
                                nextLobbyRefreshAllowedAt = DateTime.UtcNow.AddSeconds(15);
                                SetStatus("Zbyt wiele zapytań do lobby. Spróbuj ponownie za chwilę.");
                                return;
                        }

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
                        (IHostSession session, string sessionCode) = await SetupSessionHostAsync(lobbyName);
                        CreateLobbyOptions options = new CreateLobbyOptions
                        {
                                IsPrivate = false,
                                Player = BuildLocalPlayer(localName)
                        };

                        if (!string.IsNullOrWhiteSpace(sessionCode))
                        {
                                options.Data = new Dictionary<string, DataObject>
                                {
                                        {
                                                SessionCodeKey,
                                                new DataObject(DataObject.VisibilityOptions.Member, sessionCode)
                                        }
                                };
                        }

                        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
                        if (!string.IsNullOrWhiteSpace(sessionCode))
                        {
                                SetStatus($"Utworzono lobby: {currentLobby.Name} (kod: {currentLobby.LobbyCode})");
                        }
                        else
                        {
                                SetStatus($"Utworzono lobby: {currentLobby.Name} (kod: {currentLobby.LobbyCode}). Oczekiwanie na kod sesji.");
                                if (session != null)
                                {
                                        RunSafe(EnsureLobbySessionCodeAsync(currentLobby.Id, session));
                                }
                        }
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
                        Lobby joinedLobby = null;
                        string targetLobbyId = string.Empty;
                        if (lobbyDropdown != null && lobbyDropdown.value >= 0 && lobbyDropdown.value < availableLobbies.Count)
                        {
                                targetLobbyId = availableLobbies[lobbyDropdown.value].Id;
                                localName = EnsureUniqueName(localName, availableLobbies[lobbyDropdown.value]);
                                JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
                                {
                                        Player = BuildLocalPlayer(localName)
                                };
                                joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(targetLobbyId, options);
                        }
                        else if (!string.IsNullOrWhiteSpace(selectedLobbyId))
                        {
                                targetLobbyId = selectedLobbyId;
                                JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
                                {
                                        Player = BuildLocalPlayer(localName)
                                };
                                joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(targetLobbyId, options);
                        }
                        else
                        {
                                SetStatus("Wybierz lobby z listy.");
                                return;
                        }

                        currentLobby = joinedLobby;
                        if (!TryGetSessionCode(currentLobby, out string sessionCode))
                        {
                                sessionCode = await WaitForLobbySessionCodeAsync(currentLobby.Id);
                        }

                        if (string.IsNullOrWhiteSpace(sessionCode))
                        {
                                SetStatus("Lobby nie ma jeszcze kodu sesji. Spróbuj ponownie za chwilę.");
                                return;
                        }

                        bool sessionReady = await SetupSessionClientAsync(sessionCode);
                        if (!sessionReady)
                        {
                                SetStatus("Nie udało się dołączyć do sesji sieciowej.");
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
                        if (ex.Message.Contains("already a member", StringComparison.OrdinalIgnoreCase))
                        {
                                Lobby fallbackLobby = await TryGetJoinedLobbyAsync();
                                if (fallbackLobby != null)
                                {
                                        currentLobby = fallbackLobby;
                                        if (!TryGetSessionCode(currentLobby, out string sessionCode))
                                        {
                                                sessionCode = await WaitForLobbySessionCodeAsync(currentLobby.Id);
                                        }

                                        if (string.IsNullOrWhiteSpace(sessionCode))
                                        {
                                                SetStatus("Lobby nie ma jeszcze kodu sesji. Spróbuj ponownie za chwilę.");
                                                return;
                                        }

                                        bool sessionReady = await SetupSessionClientAsync(sessionCode);
                                        if (!sessionReady)
                                        {
                                                SetStatus("Nie udało się dołączyć do sesji sieciowej.");
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
                                        return;
                                }
                        }

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

        private void UpdateActivePlayersCount()
        {
                if (activePlayersText == null)
                {
                        return;
                }

                int totalPlayers = 0;
                foreach (Lobby lobby in availableLobbies)
                {
                        totalPlayers += lobby.Players.Count;
                }

                activePlayersText.text = $"Aktywni gracze: {totalPlayers}";
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

                float panelWidth = Mathf.Min(900f, Screen.width * 0.95f);
                float panelHeight = Mathf.Min(900f, Screen.height * 0.95f);
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
                GUILayout.Label("Lobby", titleStyle);
                GUILayout.Space(10);

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                        GUILayout.Label("Username:", labelStyle);
                        customIdValue = GUILayout.TextField(customIdValue, 32, textFieldStyle, GUILayout.Height(40));
                        GUILayout.Space(10);

                        if (GUILayout.Button("Zaloguj", buttonStyle, GUILayout.Height(50)))
                        {
                                RunSafe(LoginAsync());
                        }
                        GUILayout.EndArea();
                        return;
                }

                GUILayout.Label("Nazwa lobby:", labelStyle);
                lobbyNameValue = GUILayout.TextField(lobbyNameValue, 32, textFieldStyle, GUILayout.Height(40));

                GUILayout.Space(10);

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

        private void InitializeNameInput()
        {
                if (customIdInput != null)
                {
                        customIdInput.text = string.Empty;
                }
                customIdValue = string.Empty;
        }

        private void UpdatePanelVisibility(bool signedIn)
        {
                if (loginPanel != null)
                {
                        loginPanel.SetActive(!signedIn);
                }
                if (lobbyPanel != null)
                {
                        lobbyPanel.SetActive(signedIn);
                }
        }

        private void StartLobbyPolling()
        {
                if (lobbyPollCoroutine != null)
                {
                        return;
                }
                lobbyPollCoroutine = StartCoroutine(PollCurrentLobbyRoutine());
        }

        private void StartLobbyListPolling()
        {
                if (lobbyListPollCoroutine != null)
                {
                        return;
                }

                lobbyListPollCoroutine = StartCoroutine(PollLobbyListRoutine());
        }

        private System.Collections.IEnumerator PollCurrentLobbyRoutine()
        {
                while (true)
                {
                        if (AuthenticationService.Instance.IsSignedIn && currentLobby != null)
                        {
                                RunSafe(UpdateCurrentLobbyAsync());
                        }

                        yield return new WaitForSeconds(2f);
                }
        }

        private System.Collections.IEnumerator PollLobbyListRoutine()
        {
                while (true)
                {
                        if (AuthenticationService.Instance.IsSignedIn)
                        {
                                RunSafe(RefreshLobbiesAsync());
                        }

                        yield return new WaitForSeconds(LobbyListPollIntervalSeconds);
                }
        }

        private async Task UpdateCurrentLobbyAsync()
        {
                if (currentLobby == null)
                {
                        return;
                }

                Lobby updated = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                if (updated != null)
                {
                        currentLobby = updated;
                        LobbyState.UpdateFromLobby(currentLobby, AuthenticationService.Instance.PlayerId);
                }
        }

        private async Task<(IHostSession session, string sessionCode)> SetupSessionHostAsync(string lobbyName)
        {
                try
                {
                        await EnsureMultiplayerServiceInitializedAsync();
                        if (MultiplayerService.Instance == null)
                        {
                                SetStatus("Multiplayer Service nie jest dostępny.");
                                return (null, null);
                        }

                        SessionOptions options = new SessionOptions
                        {
                                MaxPlayers = 2,
                                Name = lobbyName,
                                Type = SessionType
                        };
                        options.WithPlayerName();
                        options.WithRelayNetwork();

                        IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);
                        string sessionCode = await WaitForSessionCodeAsync(session);
                        if (string.IsNullOrWhiteSpace(sessionCode))
                        {
                                SetStatus("Sesja została utworzona bez kodu.");
                        }

                        return (session, sessionCode);
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd tworzenia sesji: {ex.Message}");
                        return (null, null);
                }
        }

        private async Task<bool> SetupSessionClientAsync(string sessionCode)
        {
                try
                {
                        await EnsureMultiplayerServiceInitializedAsync();
                        if (MultiplayerService.Instance == null)
                        {
                                SetStatus("Multiplayer Service nie jest dostępny.");
                                return false;
                        }

                        JoinSessionOptions options = new JoinSessionOptions
                        {
                                Type = SessionType
                        };
                        options.WithPlayerName();

                        await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode, options);
                        return true;
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd dołączania do sesji: {ex.Message}");
                        return false;
                }
        }

        private bool TryGetSessionCode(Lobby lobby, out string sessionCode)
        {
                sessionCode = null;
                if (lobby?.Data == null)
                {
                        return false;
                }

                if (lobby.Data.TryGetValue(SessionCodeKey, out var data) && !string.IsNullOrWhiteSpace(data.Value))
                {
                        sessionCode = data.Value;
                        return true;
                }

                return false;
        }

        private static async Task<string> WaitForSessionCodeAsync(ISession session)
        {
                if (session == null)
                {
                        return null;
                }

                const int retries = 10;
                const int delayMs = 200;

                for (int attempt = 0; attempt < retries; attempt++)
                {
                        if (!string.IsNullOrWhiteSpace(session.Code))
                        {
                                return session.Code;
                        }

                        var refreshMethod = session.GetType().GetMethod("RefreshAsync", Type.EmptyTypes);
                        if (refreshMethod != null && refreshMethod.Invoke(session, null) is Task refreshTask)
                        {
                                await refreshTask;
                        }

                        await Task.Delay(delayMs);
                }

                return session.Code;
        }

        private async Task EnsureLobbySessionCodeAsync(string lobbyId, ISession session)
        {
                if (string.IsNullOrWhiteSpace(lobbyId) || session == null)
                {
                        return;
                }

                string sessionCode = await WaitForSessionCodeAsync(session);
                if (string.IsNullOrWhiteSpace(sessionCode))
                {
                        SetStatus("Nie udało się uzyskać kodu sesji.");
                        return;
                }

                try
                {
                        UpdateLobbyOptions options = new UpdateLobbyOptions
                        {
                                Data = new Dictionary<string, DataObject>
                                {
                                        {
                                                SessionCodeKey,
                                                new DataObject(DataObject.VisibilityOptions.Member, sessionCode)
                                        }
                                }
                        };

                        currentLobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
                        SetStatus("Kod sesji został zaktualizowany.");
                }
                catch (Exception ex)
                {
                        SetStatus($"Nie udało się zaktualizować kodu sesji: {ex.Message}");
                }
        }

        private async Task<string> WaitForLobbySessionCodeAsync(string lobbyId)
        {
                if (string.IsNullOrWhiteSpace(lobbyId))
                {
                        return null;
                }

                const int retries = 6;
                const int delayMs = 500;

                for (int attempt = 0; attempt < retries; attempt++)
                {
                        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                        if (TryGetSessionCode(lobby, out string sessionCode))
                        {
                                currentLobby = lobby;
                                return sessionCode;
                        }

                        await Task.Delay(delayMs);
                }

                return null;
        }

        private async Task<Lobby> TryGetJoinedLobbyAsync()
        {
                try
                {
                        var joinedLobbies = await LobbyService.Instance.GetJoinedLobbiesAsync();
                        if (joinedLobbies != null && joinedLobbies.Count > 0)
                        {
                                string lobbyId = joinedLobbies[0];
                                return await LobbyService.Instance.GetLobbyAsync(lobbyId);
                        }
                }
                catch (Exception ex)
                {
                        SetStatus($"Nie udało się pobrać istniejącego lobby: {ex.Message}");
                }

                return null;
        }
}
