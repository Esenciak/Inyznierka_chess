using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI; // Zostaw to dla Buttonów
using TMPro; // <--- DODAJ TO KONIECZNIE!

// Pakiety Unity Services
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;



public class LobbyMenu : MonoBehaviour
{
	private const string PlayerNameKey = "name";
	private const string SessionCodeKey = "sessionCode";
	private const string AuthIdPrefsKey = "AuthId";
	private const string PlayerNamePrefsKey = "PlayerName";
	private const string RelayConnectionType = "dtls";

	[Header("UI References")]
	// ZMIANA: InputField -> TMP_InputField
	[SerializeField] private TMP_InputField customIdInput;
	[SerializeField] private TMP_InputField lobbyNameInput;

	// ZMIANA: Dropdown -> TMP_Dropdown
	[SerializeField] private TMP_Dropdown lobbyDropdown;

	// ZMIANA: Text -> TMP_Text
	[SerializeField] private TMP_Text statusText;
	[SerializeField] private TMP_Text activePlayersText;
	[SerializeField] private Transform lobbyListParent;

	// Buttony zostają bez zmian (Unity używa tych samych buttonów dla obu systemów)
	[SerializeField] private Button loginButton;
	[SerializeField] private Button createLobbyButton;
	[SerializeField] private Button joinLobbyButton;
	[SerializeField] private Button refreshButton;
	[SerializeField] private Button quickPlayButton; // Jeśli masz

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
        private readonly List<GameObject> lobbyListEntries = new List<GameObject>();

        public void SetConnectionMenu(ConnectionMenu menu)
        {
                connectionMenu = menu;
        }

        private async void Awake()
        {
                BuildLobbyUiIfMissing();
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

        private void BuildLobbyUiIfMissing()
        {
                if (customIdInput != null || lobbyPanel != null || loginPanel != null)
                {
                        return;
                }

                GameObject canvasObject = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                loginPanel = CreatePanel(canvas.transform, "LoginPanel", new Vector2(0.5f, 0.5f), new Vector2(600f, 280f));
                TMP_Text loginTitle = CreateLabel(loginPanel.transform, "LoginTitle", "Relay Lobby", 42, TextAlignmentOptions.Center);
                SetAnchors(loginTitle.rectTransform, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.95f));
                customIdInput = CreateInputField(loginPanel.transform, "NicknameInput", "Wpisz nick");
                SetAnchors(customIdInput.GetComponent<RectTransform>(), new Vector2(0.15f, 0.45f), new Vector2(0.85f, 0.65f));

                loginButton = CreateButton(loginPanel.transform, "LoginButton", "Zaloguj");
                SetAnchors(loginButton.GetComponent<RectTransform>(), new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.35f));

                lobbyPanel = CreatePanel(canvas.transform, "LobbyPanel", new Vector2(0.5f, 0.5f), new Vector2(860f, 720f));
                TMP_Text lobbyTitle = CreateLabel(lobbyPanel.transform, "LobbyTitle", "Lobby", 42, TextAlignmentOptions.Center);
                SetAnchors(lobbyTitle.rectTransform, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.98f));

                TMP_Text lobbyNameLabel = CreateLabel(lobbyPanel.transform, "LobbyNameLabel", "Nazwa lobby:", 26, TextAlignmentOptions.Left);
                SetAnchors(lobbyNameLabel.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.4f, 0.86f));

                lobbyNameInput = CreateInputField(lobbyPanel.transform, "LobbyNameInput", "Np. Lobby-1234");
                SetAnchors(lobbyNameInput.GetComponent<RectTransform>(), new Vector2(0.08f, 0.7f), new Vector2(0.55f, 0.78f));

                createLobbyButton = CreateButton(lobbyPanel.transform, "CreateLobbyButton", "Hostuj");
                SetAnchors(createLobbyButton.GetComponent<RectTransform>(), new Vector2(0.6f, 0.7f), new Vector2(0.92f, 0.78f));

                refreshButton = CreateButton(lobbyPanel.transform, "RefreshButton", "Odśwież");
                SetAnchors(refreshButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.6f), new Vector2(0.35f, 0.68f));

                quickPlayButton = CreateButton(lobbyPanel.transform, "QuickPlayButton", "Quick Play");
                SetAnchors(quickPlayButton.GetComponent<RectTransform>(), new Vector2(0.38f, 0.6f), new Vector2(0.65f, 0.68f));

                joinLobbyButton = CreateButton(lobbyPanel.transform, "JoinLobbyButton", "Dołącz");
                SetAnchors(joinLobbyButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.6f), new Vector2(0.92f, 0.68f));

                TMP_Text listLabel = CreateLabel(lobbyPanel.transform, "LobbyListLabel", "Dostępne lobby:", 24, TextAlignmentOptions.Left);
                SetAnchors(listLabel.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.6f, 0.58f));

                GameObject listObject = new GameObject("LobbyList", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                lobbyListParent = listObject.transform;
                listObject.transform.SetParent(lobbyPanel.transform, false);
                RectTransform listRect = listObject.GetComponent<RectTransform>();
                SetAnchors(listRect, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.52f));
                Image listImage = listObject.GetComponent<Image>();
                listImage.color = new Color(0f, 0f, 0f, 0.35f);
                VerticalLayoutGroup layout = listObject.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                ContentSizeFitter fitter = listObject.GetComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                statusText = CreateLabel(lobbyPanel.transform, "LobbyStatus", string.Empty, 22, TextAlignmentOptions.Left);
                SetAnchors(statusText.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.15f));

                activePlayersText = CreateLabel(lobbyPanel.transform, "ActivePlayers", string.Empty, 20, TextAlignmentOptions.Left);
                SetAnchors(activePlayersText.rectTransform, new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.2f));
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
                GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(parent, false);
                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
                Image image = panel.GetComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.6f);
                return panel;
        }

        private TMP_Text CreateLabel(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
                GameObject labelObject = new GameObject(name, typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(parent, false);
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                label.text = text;
                label.fontSize = fontSize;
                label.alignment = alignment;
                label.color = Color.white;
                return label;
        }

        private TMP_InputField CreateInputField(Transform parent, string name, string placeholderText)
        {
                GameObject inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
                inputObject.transform.SetParent(parent, false);
                Image image = inputObject.GetComponent<Image>();
                image.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

                GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                viewport.transform.SetParent(inputObject.transform, false);
                RectTransform viewportRect = viewport.GetComponent<RectTransform>();
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = new Vector2(10f, 6f);
                viewportRect.offsetMax = new Vector2(-10f, -6f);
                Image viewportImage = viewport.GetComponent<Image>();
                viewportImage.color = new Color(0f, 0f, 0f, 0f);
                Mask viewportMask = viewport.GetComponent<Mask>();
                viewportMask.showMaskGraphic = false;

                GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI));
                textObject.transform.SetParent(viewport.transform, false);
                TMP_FontAsset fallbackFont = TMP_Settings.defaultFontAsset;
                if (fallbackFont == null)
                {
                        fallbackFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }

                TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
                text.fontSize = 24;
                text.alignment = TextAlignmentOptions.Left;
                text.color = Color.white;
                if (fallbackFont != null)
                {
                        text.font = fallbackFont;
                }
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                GameObject placeholderObject = new GameObject("Placeholder", typeof(TextMeshProUGUI));
                placeholderObject.transform.SetParent(viewport.transform, false);
                TextMeshProUGUI placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();
                placeholder.text = placeholderText;
                placeholder.fontSize = 24;
                placeholder.alignment = TextAlignmentOptions.Left;
                placeholder.color = new Color(1f, 1f, 1f, 0.5f);
                if (fallbackFont != null)
                {
                        placeholder.font = fallbackFont;
                }
                RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.offsetMin = Vector2.zero;
                placeholderRect.offsetMax = Vector2.zero;

                TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
                input.textViewport = viewportRect;
                input.textComponent = text;
                input.placeholder = placeholder;
                input.pointSize = 24;
                input.lineType = TMP_InputField.LineType.SingleLine;
                input.characterLimit = 32;
                return input;
        }

        private Button CreateButton(Transform parent, string name, string label)
        {
                GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);
                Image image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                GameObject labelObject = new GameObject("Label", typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(buttonObject.transform, false);
                TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
                labelText.text = label;
                labelText.fontSize = 26;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = Color.white;
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                return buttonObject.GetComponent<Button>();
        }

        private void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
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
                        string sessionCode = await SetupSessionHostAsync(lobbyName);
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
                                SetStatus($"Utworzono lobby: {currentLobby.Name} (kod: {currentLobby.LobbyCode}). Brak kodu relaya.");
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
                {
                        UpdateLobbyListButtons();
                        return;
                }

                lobbyDropdown.ClearOptions();
                List<string> options = new List<string>();
                foreach (Lobby lobby in availableLobbies)
                {
                        options.Add($"{lobby.Name} ({lobby.Players.Count}/2)");
                }
                lobbyDropdown.AddOptions(options);
        }

        private void UpdateLobbyListButtons()
        {
                if (lobbyListParent == null)
                {
                        return;
                }

                foreach (GameObject entry in lobbyListEntries)
                {
                        if (entry != null)
                        {
                                Destroy(entry);
                        }
                }
                lobbyListEntries.Clear();

                foreach (Lobby lobby in availableLobbies)
                {
                        GameObject entry = new GameObject($"LobbyEntry_{lobby.Name}", typeof(RectTransform), typeof(Image), typeof(Button));
                        entry.transform.SetParent(lobbyListParent, false);
                        Image background = entry.GetComponent<Image>();
                        bool isSelected = lobby.Id == selectedLobbyId;
                        background.color = isSelected ? new Color(0.35f, 0.35f, 0.35f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);

                        Button button = entry.GetComponent<Button>();
                        button.onClick.AddListener(() =>
                        {
                                selectedLobbyId = lobby.Id;
                                SetStatus($"Wybrano lobby: {lobby.Name}");
                                UpdateLobbyListButtons();
                        });

                        GameObject labelObject = new GameObject("Label", typeof(TextMeshProUGUI));
                        labelObject.transform.SetParent(entry.transform, false);
                        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                        label.text = $"{lobby.Name} ({lobby.Players.Count}/2)";
                        label.fontSize = 22;
                        label.alignment = TextAlignmentOptions.Center;
                        label.color = Color.white;
                        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                        labelRect.anchorMin = Vector2.zero;
                        labelRect.anchorMax = Vector2.one;
                        labelRect.offsetMin = Vector2.zero;
                        labelRect.offsetMax = Vector2.zero;

                        RectTransform entryRect = entry.GetComponent<RectTransform>();
                        entryRect.sizeDelta = new Vector2(0f, 46f);

                        lobbyListEntries.Add(entry);
                }
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

        private async Task<string> SetupSessionHostAsync(string lobbyName)
        {
                try
                {
                        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
                        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                        ConfigureRelayTransport(new RelayServerData(allocation, RelayConnectionType));
                        return joinCode;
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd tworzenia relaya: {ex.Message}");
                        return null;
                }
        }

        private async Task<bool> SetupSessionClientAsync(string sessionCode)
        {
                try
                {
                        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(sessionCode);
                        ConfigureRelayTransport(new RelayServerData(joinAllocation, RelayConnectionType));
                        return true;
                }
                catch (Exception ex)
                {
                        SetStatus($"Błąd dołączania do relaya: {ex.Message}");
                        return false;
                }
        }

        private void ConfigureRelayTransport(RelayServerData relayServerData)
        {
                if (NetworkManager.Singleton == null)
                {
                        SetStatus("Brak NetworkManager w scenie.");
                        return;
                }

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                        SetStatus("Brak UnityTransport w NetworkManager.");
                        return;
                }

                transport.SetRelayServerData(relayServerData);
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
