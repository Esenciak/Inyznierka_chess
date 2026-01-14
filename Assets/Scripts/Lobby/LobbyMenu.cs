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
        private const string AuthIdPrefsKey = "AuthId";
        private const string PlayerNamePrefsKey = "PlayerName";
        private static readonly (string Label, Color Color)[] TileColorOptions =
        {
                ("Biały", Color.white),
                ("Jasny Niebieski", new Color(0.4f, 0.7f, 1f)),
                ("Niebieski", new Color(0.25f, 0.55f, 0.95f)),
                ("Zielony", new Color(0.2f, 0.75f, 0.4f)),
                ("Żółty", new Color(0.95f, 0.85f, 0.25f)),
                ("Pomarańczowy", new Color(0.95f, 0.55f, 0.2f)),
                ("Czerwony", new Color(0.9f, 0.3f, 0.3f)),
                ("Fioletowy", new Color(0.6f, 0.35f, 0.85f))
        };

        [Header("UI References")]
        [SerializeField] private InputField customIdInput;
        [SerializeField] private InputField lobbyNameInput;
        [SerializeField] private Dropdown tileColor0Dropdown;
        [SerializeField] private Dropdown tileColor1Dropdown;
        [SerializeField] private Dropdown lobbyDropdown;
        [SerializeField] private Text statusText;
        [SerializeField] private Text activePlayersText;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button quickPlayButton;
        [SerializeField] private Button changeColorButton;
        [SerializeField] private GameObject colorPanel;
        [SerializeField] private Transform whiteColorContainer;
        [SerializeField] private Transform blackColorContainer;
        [SerializeField] private GameObject colorButtonPrefab;
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
        private int tileColor0Index;
        private int tileColor1Index = 1;
        private Coroutine lobbyPollCoroutine;
        private Coroutine lobbyListPollCoroutine;

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
                InitializeColorDropdowns();
                InitializeColorPanel();
                ApplyLocalTileColorSelection();
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
                if (changeColorButton != null)
                        changeColorButton.onClick.AddListener(ToggleColorPanel);
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

                ApplyLocalTileColorSelection();
                string authId = GetOrCreateAuthId();
                await SignInAsync(authId);
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
                string color0Hex = $"#{ColorUtility.ToHtmlStringRGBA(GetSelectedTileColor(tileColor0Dropdown, tileColor0Index))}";
                string color1Hex = $"#{ColorUtility.ToHtmlStringRGBA(GetSelectedTileColor(tileColor1Dropdown, tileColor1Index))}";
                return new Player
                {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                                { PlayerNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                                { LobbyState.PlayerColor0Key, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, color0Hex) },
                                { LobbyState.PlayerColor1Key, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, color1Hex) }
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
                        UpdateActivePlayersCount();
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
                GUILayout.Label("Lobby (UGS)", titleStyle);
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

                GUILayout.Label("Kolor kafelków (0):", labelStyle);
                tileColor0Index = GUILayout.SelectionGrid(tileColor0Index, GetTileColorLabels(), 2, buttonStyle, GUILayout.Height(120));

                GUILayout.Label("Kolor kafelków (1):", labelStyle);
                tileColor1Index = GUILayout.SelectionGrid(tileColor1Index, GetTileColorLabels(), 2, buttonStyle, GUILayout.Height(120));
                ApplyLocalTileColorSelection();

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

        private void ApplyLocalTileColorSelection()
        {
                Color color0 = GetSelectedTileColor(tileColor0Dropdown, tileColor0Index);
                Color color1 = GetSelectedTileColor(tileColor1Dropdown, tileColor1Index);
                LobbyState.SetLocalTileColors(color0, color1);
        }

        private void InitializeNameInput()
        {
                if (customIdInput != null)
                {
                        customIdInput.text = string.Empty;
                }
                customIdValue = string.Empty;
        }

        private void InitializeColorDropdowns()
        {
                List<string> labels = new List<string>(GetTileColorLabels());
                if (tileColor0Dropdown != null)
                {
                        tileColor0Dropdown.ClearOptions();
                        tileColor0Dropdown.AddOptions(labels);
                        tileColor0Dropdown.onValueChanged.RemoveAllListeners();
                        tileColor0Dropdown.onValueChanged.AddListener(index =>
                        {
                                tileColor0Index = index;
                                ApplyLocalTileColorSelection();
                        });
                        tileColor0Dropdown.value = Mathf.Clamp(tileColor0Index, 0, TileColorOptions.Length - 1);
                }

                if (tileColor1Dropdown != null)
                {
                        tileColor1Dropdown.ClearOptions();
                        tileColor1Dropdown.AddOptions(labels);
                        tileColor1Dropdown.onValueChanged.RemoveAllListeners();
                        tileColor1Dropdown.onValueChanged.AddListener(index =>
                        {
                                tileColor1Index = index;
                                ApplyLocalTileColorSelection();
                        });
                        tileColor1Dropdown.value = Mathf.Clamp(tileColor1Index, 0, TileColorOptions.Length - 1);
                }
        }

        private void InitializeColorPanel()
        {
                if (colorPanel != null)
                {
                        colorPanel.SetActive(false);
                }

                if (whiteColorContainer != null)
                {
                        BuildColorButtons(whiteColorContainer, SetWhiteColorIndex);
                }

                if (blackColorContainer != null)
                {
                        BuildColorButtons(blackColorContainer, SetBlackColorIndex);
                }
        }

        private void BuildColorButtons(Transform container, Action<int> onSelect)
        {
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                        Destroy(container.GetChild(i).gameObject);
                }

                for (int i = 0; i < TileColorOptions.Length; i++)
                {
                        GameObject buttonObject = colorButtonPrefab != null
                                ? Instantiate(colorButtonPrefab, container)
                                : CreateColorButtonObject(container);

                        Image image = buttonObject.GetComponent<Image>();
                        if (image != null)
                        {
                                image.color = TileColorOptions[i].Color;
                        }

                        Button button = buttonObject.GetComponent<Button>();
                        if (button != null)
                        {
                                int index = i;
                                button.onClick.RemoveAllListeners();
                                button.onClick.AddListener(() =>
                                {
                                        onSelect(index);
                                        ApplyLocalTileColorSelection();
                                });
                        }
                }
        }

        private GameObject CreateColorButtonObject(Transform parent)
        {
                GameObject go = new GameObject("ColorButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(48f, 48f);
                return go;
        }

        private void SetWhiteColorIndex(int index)
        {
                tileColor0Index = index;
                if (tileColor0Dropdown != null)
                {
                        tileColor0Dropdown.value = index;
                }
        }

        private void SetBlackColorIndex(int index)
        {
                tileColor1Index = index;
                if (tileColor1Dropdown != null)
                {
                        tileColor1Dropdown.value = index;
                }
        }

        private void ToggleColorPanel()
        {
                if (colorPanel == null)
                {
                        return;
                }

                colorPanel.SetActive(!colorPanel.activeSelf);
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
                if (changeColorButton != null)
                {
                        changeColorButton.gameObject.SetActive(!signedIn);
                }
                if (colorPanel != null && signedIn)
                {
                        colorPanel.SetActive(false);
                }
                if (tileColor0Dropdown != null)
                {
                        tileColor0Dropdown.gameObject.SetActive(!signedIn);
                }
                if (tileColor1Dropdown != null)
                {
                        tileColor1Dropdown.gameObject.SetActive(!signedIn);
                }
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

        private Color GetSelectedTileColor(Dropdown dropdown, int fallbackIndex)
        {
                int index = fallbackIndex;
                if (dropdown != null)
                {
                        index = dropdown.value;
                }

                index = Mathf.Clamp(index, 0, TileColorOptions.Length - 1);
                return TileColorOptions[index].Color;
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

                        yield return new WaitForSeconds(10f);
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
}
