using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnIndicator : MonoBehaviour
{
        public static TurnIndicator Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private string yourTurnText = "Twoja tura";
        [SerializeField] private string enemyTurnText = "Tura przeciwnika";

        private void Awake()
        {
                if (Instance != null && Instance != this)
                {
                        Destroy(gameObject);
                        return;
                }
                Instance = this;

                if (turnText == null)
                {
                        turnText = CreateDefaultText();
                }
        }

        private void OnDestroy()
        {
                if (Instance == this)
                {
                        Instance = null;
                }
        }

        private void Start()
        {
                UpdateTurnText();
        }

        public void UpdateTurnText()
        {
                if (turnText == null)
                {
                        return;
                }

                if (GameManager.Instance != null && GameManager.Instance.isMultiplayer && BattleMoveSync.Instance != null && BattleMoveSync.Instance.IsSpawned)
                {
                        bool isLocalTurn = BattleMoveSync.Instance.IsLocalPlayersTurn();
                        string localName = LobbyState.LocalPlayerName;
                        string opponentName = LobbyState.OpponentPlayerName;
                        turnText.text = isLocalTurn ? $"Tura: {localName}" : $"Tura: {opponentName}";
                        return;
                }

                if (GameManager.Instance != null)
                {
                        turnText.text = GameManager.Instance.currentTurn == PieceOwner.Player ? yourTurnText : enemyTurnText;
                }
        }

        private TextMeshProUGUI CreateDefaultText()
        {
                GameObject canvasObject = new GameObject("TurnCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                GameObject textObject = new GameObject("TurnText", typeof(TextMeshProUGUI));
                textObject.transform.SetParent(canvasObject.transform, false);

                TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 48;
                text.color = Color.white;

                RectTransform rect = text.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -20f);
                rect.sizeDelta = new Vector2(600f, 120f);

                return text;
        }
}
