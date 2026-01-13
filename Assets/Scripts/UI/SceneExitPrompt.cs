using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneExitPrompt : MonoBehaviour
{
        [Header("Tryb")]
        [SerializeField] private bool isBattleScene = true;

        [Header("Teksty")]
        [SerializeField] private string battlePromptText = "Czy chcesz się poddać?";
        [SerializeField] private string shopPromptText = "Czy chcesz wyjść?";
        [SerializeField] private string confirmLabel = "Tak";
        [SerializeField] private string cancelLabel = "Nie";
        [SerializeField] private string resignButtonLabel = "Poddaj się";

        private GameObject promptRoot;
        private Canvas promptCanvas;
        private TextMeshProUGUI promptLabel;
        private Button confirmButton;
        private Button cancelButton;
        private Button resignButton;

        private void Start()
        {
                EnsureSingleEventSystem();
                BuildPromptUI();
                if (isBattleScene)
                {
                        BuildResignButton();
                }
                SetPromptVisible(false);
        }

        private void Update()
        {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                        TogglePrompt();
                }
        }

        private void TogglePrompt()
        {
                if (promptRoot == null)
                {
                        return;
                }

                bool nextState = !promptRoot.activeSelf;
                SetPromptVisible(nextState);
        }

        private void SetPromptVisible(bool isVisible)
        {
                if (promptRoot != null)
                {
                        promptRoot.SetActive(isVisible);
                }
        }

        private void BuildPromptUI()
        {
                GameObject canvasObject = new GameObject("ExitPromptCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                promptCanvas = canvasObject.GetComponent<Canvas>();
                promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                promptCanvas.overrideSorting = true;
                promptCanvas.sortingOrder = 1000;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                promptRoot = new GameObject("ExitPromptPanel", typeof(Image));
                promptRoot.transform.SetParent(promptCanvas.transform, false);

                RectTransform panelRect = promptRoot.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(620f, 260f);

                Image panelImage = promptRoot.GetComponent<Image>();
                panelImage.color = new Color(0f, 0f, 0f, 0.8f);

                GameObject labelObject = new GameObject("PromptLabel", typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(promptRoot.transform, false);
                promptLabel = labelObject.GetComponent<TextMeshProUGUI>();
                promptLabel.text = isBattleScene ? battlePromptText : shopPromptText;
                promptLabel.alignment = TextAlignmentOptions.Center;
                promptLabel.fontSize = 36;
                promptLabel.color = Color.white;

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.1f, 0.55f);
                labelRect.anchorMax = new Vector2(0.9f, 0.9f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                confirmButton = CreateButton(promptRoot.transform, "ConfirmButton", confirmLabel, new Vector2(0.15f, 0.15f), new Vector2(0.45f, 0.4f));
                cancelButton = CreateButton(promptRoot.transform, "CancelButton", cancelLabel, new Vector2(0.55f, 0.15f), new Vector2(0.85f, 0.4f));

                confirmButton.onClick.AddListener(OnConfirm);
                cancelButton.onClick.AddListener(() => SetPromptVisible(false));
        }

        private void BuildResignButton()
        {
                if (promptRoot == null)
                {
                        return;
                }

                if (promptCanvas == null)
                {
                        return;
                }

                resignButton = CreateButton(promptCanvas.transform, "ResignButton", resignButtonLabel, new Vector2(0.78f, 0.9f), new Vector2(0.97f, 0.98f));
                resignButton.onClick.AddListener(() => SetPromptVisible(true));
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
                GameObject buttonObject = new GameObject(name, typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);

                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                GameObject textObject = new GameObject("Label", typeof(TextMeshProUGUI));
                textObject.transform.SetParent(buttonObject.transform, false);
                TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
                text.text = label;
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 28;
                text.color = Color.white;

                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                return buttonObject.GetComponent<Button>();
        }

        private void OnConfirm()
        {
                SetPromptVisible(false);

                if (isBattleScene)
                {
                        HandleBattleResign();
                }
                else
                {
                        _ = HandleShopExitAsync();
                }
        }

        private void HandleBattleResign()
        {
                if (GameManager.Instance == null)
                {
                        return;
                }

                if (GameManager.Instance.isMultiplayer && BattleMoveSync.Instance != null && BattleMoveSync.Instance.IsSpawned)
                {
                        BattleMoveSync.Instance.RequestResign();
                        return;
                }

                GameManager.Instance.GameOver(false);
        }

        private async Task HandleShopExitAsync()
        {
                await LobbyState.LeaveLobbyAsync();

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                        NetworkManager.Singleton.Shutdown();
                }

                SceneFader.LoadSceneWithFade("MainMenu");
        }

        private void EnsureSingleEventSystem()
        {
                EventSystem[] systems = FindObjectsOfType<EventSystem>();
                if (systems.Length == 0)
                {
                        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                        DontDestroyOnLoad(eventSystemObject);
                        return;
                }

                for (int i = 1; i < systems.Length; i++)
                {
                        Destroy(systems[i].gameObject);
                }
        }
}
