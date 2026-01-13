using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;



public class GameManager : MonoBehaviour
{
        public static GameManager Instance { get; private set; }

        [Header("Tryb Gry")]
        public bool isMultiplayer = false;

        [Header("Stan Gry")]
        public GamePhase currentPhase = GamePhase.Placement;

        // ZMIANA: Używamy PieceOwner zamiast "Turn", żeby pasowało do kodu Piece.cs
        public PieceOwner currentTurn = PieceOwner.Player;

        [Header("Nagrody")]
        public int winReward = 20;
        public int loseReward = 10;
        public EconomyConfig economyConfig;

        private bool gameEnded = false;

        private void Awake()
        {
                // Singleton - zapewnia, że jest tylko jeden GameManager
                if (Instance != null && Instance != this)
                {
                        Destroy(gameObject);
                        return;
                }
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SyncMultiplayerState();
        }

        private void OnEnable()
        {
                SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
                SyncMultiplayerState();
                if (scene.name == "Battle")
                {
                        currentPhase = GamePhase.Battle;
                        currentTurn = PieceOwner.Player;
                        gameEnded = false;
                        return;
                }

                if (scene.name == "Shop")
                {
                        currentPhase = GamePhase.Placement;
                        currentTurn = PieceOwner.Player;
                        return;
                }

                if (scene.name == "MainMenu")
                {
                        ShowWinnerBanner();
                }
        }

        private void SyncMultiplayerState()
        {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                        isMultiplayer = true;
                        if (GameProgress.Instance != null)
                        {
                                GameProgress.Instance.isHostPlayer = NetworkManager.Singleton.IsHost;
                        }
                }
        }

        private void ShowWinnerBanner()
        {
                if (GameProgress.Instance == null)
                {
                        return;
                }

                string message = GameProgress.Instance.lastWinnerMessage;
                if (string.IsNullOrEmpty(message))
                {
                        return;
                }

                GameObject canvasObject = new GameObject("WinnerBannerCanvas");
                var canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();

                GameObject textObject = new GameObject("WinnerBannerText");
                textObject.transform.SetParent(canvasObject.transform, false);
                var text = textObject.AddComponent<TextMeshProUGUI>();
                text.text = message;
                text.fontSize = 48;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                if (TMP_Settings.defaultFontAsset != null)
                {
                        text.font = TMP_Settings.defaultFontAsset;
                }

                RectTransform rectTransform = text.rectTransform;
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                GameProgress.Instance.lastWinnerMessage = string.Empty;
        }

	public bool IsMyTurn()
	{
		if (!isMultiplayer)
		{
			return currentTurn == PieceOwner.Player;
		}

		if (BattleMoveSync.Instance != null)
		{
			return BattleMoveSync.Instance.IsLocalPlayersTurn();
		}

		if (BattleSession.Instance == null || NetworkManager.Singleton == null)
		{
			return false;
		}

		int activeTeam = BattleSession.Instance.ActiveTeam.Value;

		if (NetworkManager.Singleton.IsHost)
		{
			return activeTeam == 0;
		}

		return activeTeam == 1;
	}

	public void EndTurn()
	{
		// Zlecenie zmiany tury musi iść do serwera, bo NetworkVariable jest zapisywalne tylko przez serwer
		if (NetworkManager.Singleton.IsServer)
		{
			BattleSession.Instance.SwapTurn();
		}
		else
		{
			// Jeśli jesteśmy klientem, musimy poprosić serwer o zmianę tury (zrobimy to przez RPC w BattleMoveSync)
			// Tutaj lokalnie nic nie zmieniamy "na siłę", czekamy na synchronizację.
		}
	}

	public int GetCurrentTurnTeam()
	{
		return BattleSession.Instance != null ? BattleSession.Instance.ActiveTeam.Value : 0;
	}

	public bool CanPieceMove(Piece piece)
        {
                // 1. Faza Placement (Sklep):
                // Pozwalamy ruszać tylko naszymi figurami (np. przestawiać je na planszy)
                if (currentPhase == GamePhase.Placement)
                {
                        return piece.owner == PieceOwner.Player;
                }

                // 2. Faza Battle (Walka):
                // Sprawdzamy czyja jest tura i czy ruszamy właściwą figurą
                if (currentPhase == GamePhase.Battle)
                {
                        // Jeśli tura Gracza -> ruszamy tylko Player
                        if (currentTurn == PieceOwner.Player && piece.owner == PieceOwner.Player)
                                return true;

                        // Jeśli tura Wroga -> ruszamy tylko Enemy (dla AI)
                        if (currentTurn == PieceOwner.Enemy && piece.owner == PieceOwner.Enemy)
                                return true;
                }

                return false;
        }

        // Metoda wywoływana przez przycisk "Start" (przejście ze Sklepu do Bitwy)
        public void StartBattle()
        {
                currentPhase = GamePhase.Battle;
                currentTurn = PieceOwner.Player; // Zawsze zaczyna gracz
                gameEnded = false;
                Debug.Log("Faza bitwy rozpoczęta!");
        }

        // --- System Tur ---

        public void SwitchTurn()
        {
                if (currentTurn == PieceOwner.Player)
                {
                        EndPlayerMove();
                }
                else
                {
                        EndEnemyMove();
                }
        }

        public void EndPlayerMove()
        {
                Debug.Log("Koniec tury gracza. Tura Enemy...");
                currentTurn = PieceOwner.Enemy;

                // Uruchamiamy AI z opóźnieniem
                StartCoroutine(EnemyMoveRoutine());
        }

        private IEnumerator EnemyMoveRoutine()
        {
                yield return new WaitForSeconds(1.0f); // "Myślenie" AI

                if (EnemyAI.Instance != null)
                {
                        EnemyAI.Instance.MakeMove();
                }
                else
                {
                        // Zabezpieczenie: jeśli nie ma AI, oddaj turę
                        Debug.LogError("Brak skryptu EnemyAI na scenie!");
                        SwitchTurn();
                }
        }

        public void EndEnemyMove()
        {
                Debug.Log("Koniec tury Enemy. Tura Gracza.");
                currentTurn = PieceOwner.Player;
        }

        public void GameOver(bool playerWon)
        {
                if (gameEnded) return;

                gameEnded = true;
                Debug.Log(playerWon ? "WYGRANA!" : "PRZEGRANA!");

                if (GameProgress.Instance != null)
                {
                        int winValue = economyConfig != null ? economyConfig.winReward : winReward;
                        int loseValue = economyConfig != null ? economyConfig.loseReward : loseReward;
                        GameProgress.Instance.CompleteRound(playerWon, winValue, loseValue);
                        if (GameProgress.Instance.gamesPlayed >= 9)
                        {
                                GameProgress.Instance.lastWinnerMessage = playerWon ? "Winner: You" : "Winner: Enemy";
                                if (isMultiplayer && Unity.Netcode.NetworkManager.Singleton != null)
                                {
                                        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
                                        {
                                                Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
                                        }
                                }
                                else
                                {
                                        SceneManager.LoadScene("MainMenu");
                                }
                                return;
                        }
                        if (isMultiplayer && Unity.Netcode.NetworkManager.Singleton != null)
                        {
                                if (Unity.Netcode.NetworkManager.Singleton.IsServer)
                                {
                                        if (BattleSession.Instance != null)
                                        {
                                                BattleSession.Instance.SharedGamesPlayed.Value = GameProgress.Instance.gamesPlayed;
                                                BattleSession.Instance.SharedPlayerBoardSize.Value = GameProgress.Instance.playerBoardSize;
                                                BattleSession.Instance.ResetSessionState();
                                        }
                                        Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("Shop", UnityEngine.SceneManagement.LoadSceneMode.Single);
                                }
                        }
                        else
                        {
                                GameProgress.Instance.LoadScene("Shop");
                        }
                }
                else
                {
                        Debug.LogWarning("GameProgress jest null - nie mogę zapisać nagrody.");
                }

                currentPhase = GamePhase.Placement;
                currentTurn = PieceOwner.Player;
        }
}
