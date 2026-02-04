using System.Collections;
using System.Collections.Generic;
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
                SceneFader.EnsureInstance();
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
                        if (isMultiplayer && BattleMoveSync.Instance != null && BattleMoveSync.Instance.IsSpawned)
                        {
                                currentTurn = BattleMoveSync.Instance.CurrentTurn.Value;
                        }
                        else if (isMultiplayer && BattleSession.Instance != null)
                        {
                                currentTurn = BattleSession.Instance.ActiveTeam.Value == 0
                                        ? PieceOwner.Player
                                        : PieceOwner.Enemy;
                        }
                        else
                        {
                                currentTurn = PieceOwner.Player;
                        }
                        gameEnded = false;
                        UpdateBattleHeaderTexts();
                        if (TelemetryService.Instance != null && BoardManager.Instance != null)
                        {
                                TelemetryService.Instance.LogBattleStart(BoardManager.Instance.CenterRows);
                        }
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

        private void UpdateBattleHeaderTexts()
        {
                if (GameProgress.Instance == null)
                {
                        return;
                }

                string localName = LobbyState.LocalPlayerName;
                string opponentName = LobbyState.OpponentPlayerName;
                int wins = GameProgress.Instance.wins;
                int losses = GameProgress.Instance.losses;

                GameObject playerObj = GameObject.Find("Player_name");
                if (playerObj != null)
                {
                        TextMeshProUGUI playerText = playerObj.GetComponent<TextMeshProUGUI>();
                        if (playerText != null)
                        {
                                playerText.text = $"{localName}: {wins}";
                        }
                }

                GameObject enemyObj = GameObject.Find("Enemy_name");
                if (enemyObj != null)
                {
                        TextMeshProUGUI enemyText = enemyObj.GetComponent<TextMeshProUGUI>();
                        if (enemyText != null)
                        {
                                enemyText.text = $"{opponentName}: {losses}";
                        }
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
                canvas.overrideSorting =true;
		        canvas.sortingOrder = -10;
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
		bool networkActive = isMultiplayer
			|| (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
			|| (BattleMoveSync.Instance != null && BattleMoveSync.Instance.IsSpawned);

		if (networkActive && BattleMoveSync.Instance != null && BattleMoveSync.Instance.IsSpawned)
		{
			return BattleMoveSync.Instance.IsLocalPlayersTurn();
		}

		if (!networkActive)
		{
			return currentTurn == PieceOwner.Player;
		}

		if (BattleSession.Instance == null || NetworkManager.Singleton == null)
		{
			PieceOwner localSide = PieceOwner.Player;
			if (NetworkManager.Singleton != null)
			{
				localSide = NetworkManager.Singleton.IsHost ? PieceOwner.Player : PieceOwner.Enemy;
			}
			else if (GameProgress.Instance != null)
			{
				localSide = GameProgress.Instance.isHostPlayer ? PieceOwner.Player : PieceOwner.Enemy;
			}

			return currentTurn == localSide;
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

	public void GameOver(bool localWon, string reason = "KingCaptured")
	{
		if (gameEnded) return;
		gameEnded = true;

		Debug.Log(localWon ? "WYGRANA!" : "PRZEGRANA!");

		if (GameProgress.Instance == null)
		{
			Debug.LogWarning("GameProgress jest null - nie mogę zapisać nagrody.");
			currentPhase = GamePhase.Placement;
			currentTurn = PieceOwner.Player;
			return;
		}

		// Runda liczona PRZED CompleteRound (bo CompleteRound zwiększa gamesPlayed)
		int roundNumber = GameProgress.Instance.gamesPlayed + 1;

		// policz pozostałe figury lokalnego gracza (Twoja istniejąca metoda)
		int piecesRemaining = GetLocalPiecesRemaining();

		// aktualizacja armii po walce (Twoja istniejąca metoda)
		UpdateArmyAfterBattle();

		int winValue = economyConfig != null ? economyConfig.GetWinReward(roundNumber) : winReward;
		int loseValue = economyConfig != null ? economyConfig.GetLoseReward(roundNumber) : loseReward;

		GameProgress.Instance.CompleteRound(localWon, winValue, loseValue);

		// Telemetria końca rundy (każdy klient loguje "swój" wynik)
		if (TelemetryService.Instance != null && BoardManager.Instance != null)
		{
			int turnIndexInRound = ResolveTurnIndexInRoundForTelemetry();
			string roundWinnerColor = ResolveWinnerColor(localWon);
			if (reason == "ResignRound")
			{
				TelemetryService.Instance.LogResignRound(
					localWon,
					GameProgress.Instance.coins,
					piecesRemaining,
					BoardManager.Instance.CenterRows,
					turnIndexInRound,
					roundWinnerColor
				);
			}

			if (GameProgress.Instance != null && GameProgress.Instance.gamesPlayed >= 9)
			{
				TelemetryService.Instance.LogMatchEnd(roundWinnerColor, reason, GameProgress.Instance.gamesPlayed);
			}

			TelemetryService.Instance.LogRoundEnd(
				localWon,
				GameProgress.Instance.coins,
				piecesRemaining,
				BoardManager.Instance.CenterRows,
				turnIndexInRound,
				roundWinnerColor
			);
		}

		bool isNetwork = isMultiplayer
			&& Unity.Netcode.NetworkManager.Singleton != null
			&& Unity.Netcode.NetworkManager.Singleton.IsListening;

		// KONIEC MECZU po 9 rundach (tylko wtedy idziemy do MainMenu)
		if (GameProgress.Instance.gamesPlayed >= 9)
		{
			GameProgress.Instance.lastWinnerMessage = localWon ? "Winner: You" : "Winner: Enemy";

			if (isNetwork)
			{
				// Server ładuje scenę, client tylko robi fade i czeka na sync sceny
				if (Unity.Netcode.NetworkManager.Singleton.IsServer)
				{
					SceneFader.FadeOutThen(() =>
					{
						Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
					});
				}
				else
				{
					SceneFader.FadeOutThen(() => { });
				}
			}
			else
			{
				SceneFader.LoadSceneWithFade("MainMenu");
			}

			currentPhase = GamePhase.Placement;
			currentTurn = PieceOwner.Player;
			return;
		}

		// NORMALNY KONIEC RUNDY (w tym "Resign") => wracamy do Shop
		if (isNetwork)
		{
			if (Unity.Netcode.NetworkManager.Singleton.IsServer)
			{
				if (BattleSession.Instance != null)
				{
					BattleSession.Instance.SharedGamesPlayed.Value = GameProgress.Instance.gamesPlayed;
					BattleSession.Instance.SharedPlayerBoardSize.Value = GameProgress.Instance.playerBoardSize;
					BattleSession.Instance.ResetSessionState();
				}

				SceneFader.FadeOutThen(() =>
				{
					Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("Shop", UnityEngine.SceneManagement.LoadSceneMode.Single);
				});
			}
			else
			{
				// client: fade, scena przyjdzie z serwera
				SceneFader.FadeOutThen(() => { });
			}
		}
		else
		{
			SceneFader.LoadSceneWithFade("Shop");
		}

		currentPhase = GamePhase.Placement;
		currentTurn = PieceOwner.Player;
	}

	private int ResolveTurnIndexInRoundForTelemetry()
	{
		if (BattleMoveSync.Instance != null && BattleMoveSync.Instance.IsSpawned)
		{
			return BattleMoveSync.Instance.GetLastTurnIndexInRound();
		}

		if (TelemetryService.Instance != null)
		{
			return TelemetryService.Instance.GetLastTurnIndexInRound();
		}

		return 0;
	}




	private int ResolveCenterBoardSizeSafe()
	{
		if (GameProgress.Instance != null && GameProgress.Instance.centerBoardSize > 0)
			return GameProgress.Instance.centerBoardSize;

		if (BoardManager.Instance != null && BoardManager.Instance.CenterRows > 0)
			return BoardManager.Instance.CenterRows;

		return 3;
	}


	private void UpdateArmyAfterBattle()
        {
                if (GameProgress.Instance == null || BoardManager.Instance == null)
                {
                        return;
                }

                Dictionary<PieceType, int> aliveCounts = CountLocalAlivePieces();
                EnsureKingRespawn(aliveCounts);

                List<SavedPieceData> updatedArmy = new List<SavedPieceData>();
                foreach (SavedPieceData data in GameProgress.Instance.myArmy)
                {
                        if (aliveCounts.TryGetValue(data.type, out int remaining) && remaining > 0)
                        {
                                updatedArmy.Add(data);
                                aliveCounts[data.type] = remaining - 1;
                        }
                }

                EnsureKingInArmyList(updatedArmy);
                GameProgress.Instance.myArmy = updatedArmy;
        }

        private void EnsureKingRespawn(Dictionary<PieceType, int> aliveCounts)
        {
                if (aliveCounts == null)
                {
                        return;
                }

                if (!aliveCounts.TryGetValue(PieceType.King, out int count) || count <= 0)
                {
                        aliveCounts[PieceType.King] = 1;
                        if (GameProgress.Instance != null)
                        {
                                int col = BoardManager.Instance.PlayerCols / 2;
                                int row = BoardManager.Instance.PlayerRows / 2;
                                GameProgress.Instance.myArmy.Add(new SavedPieceData
                                {
                                        type = PieceType.King,
                                        x = col,
                                        y = row
                                });
                        }
                }
        }

        private void EnsureKingInArmyList(List<SavedPieceData> army)
        {
                if (army == null)
                {
                        return;
                }

                foreach (SavedPieceData data in army)
                {
                        if (data.type == PieceType.King)
                        {
                                return;
                        }
                }

                Vector2Int coords = FindFallbackKingCoords();
                army.Add(new SavedPieceData
                {
                        type = PieceType.King,
                        x = coords.x,
                        y = coords.y
                });
        }

        private Vector2Int FindFallbackKingCoords()
        {
                if (BoardManager.Instance == null)
                {
                        return Vector2Int.zero;
                }

                int rows = BoardManager.Instance.PlayerRows;
                int cols = BoardManager.Instance.PlayerCols;
                int centerRow = rows / 2;
                int centerCol = cols / 2;
                Tile centerTile = BoardManager.Instance.GetTile(BoardType.Player, centerRow, centerCol);
                if (centerTile != null && !centerTile.isOccupied)
                {
                        return new Vector2Int(centerCol, centerRow);
                }

                for (int r = 0; r < rows; r++)
                {
                        for (int c = 0; c < cols; c++)
                        {
                                Tile tile = BoardManager.Instance.GetTile(BoardType.Player, r, c);
                                if (tile != null && !tile.isOccupied)
                                {
                                        return new Vector2Int(c, r);
                                }
                        }
                }

                return new Vector2Int(centerCol, centerRow);
        }

        private Dictionary<PieceType, int> CountLocalAlivePieces()
        {
                Dictionary<PieceType, int> counts = new Dictionary<PieceType, int>();
                CountPiecesOnBoard(BoardType.Player, BoardManager.Instance.PlayerRows, BoardManager.Instance.PlayerCols, counts);
                CountPiecesOnBoard(BoardType.Center, BoardManager.Instance.CenterRows, BoardManager.Instance.CenterCols, counts);
                CountPiecesOnBoard(BoardType.Enemy, BoardManager.Instance.PlayerRows, BoardManager.Instance.PlayerCols, counts);
                return counts;
        }

        private void CountPiecesOnBoard(BoardType boardType, int rows, int cols, Dictionary<PieceType, int> counts)
        {
                for (int r = 0; r < rows; r++)
                {
                        for (int c = 0; c < cols; c++)
                        {
                                Tile tile = BoardManager.Instance.GetTile(boardType, r, c);
                                if (tile == null || tile.currentPiece == null)
                                {
                                        continue;
                                }

                                Piece piece = tile.currentPiece;
                                if (!IsLocalPieceOwner(piece.owner))
                                {
                                        continue;
                                }

                                if (counts.TryGetValue(piece.pieceType, out int value))
                                {
                                        counts[piece.pieceType] = value + 1;
                                }
                                else
                                {
                                        counts[piece.pieceType] = 1;
                                }
                        }
                }
        }

        private bool IsLocalPieceOwner(PieceOwner owner)
        {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                        bool localIsHost = NetworkManager.Singleton.IsHost;
                        return localIsHost ? owner == PieceOwner.Player : owner == PieceOwner.Enemy;
                }

                if (GameProgress.Instance != null && isMultiplayer)
                {
                        return GameProgress.Instance.isHostPlayer ? owner == PieceOwner.Player : owner == PieceOwner.Enemy;
                }

                return owner == PieceOwner.Player;
        }

        private int GetLocalPiecesRemaining()
        {
                if (BoardManager.Instance == null)
                {
                        return 0;
                }

                Dictionary<PieceType, int> counts = CountLocalAlivePieces();
                int total = 0;
                foreach (var entry in counts)
                {
                        total += entry.Value;
                }
                return total;
        }

        private string ResolveWinnerColor(bool playerWon)
        {
                if (GameProgress.Instance == null)
                {
                        return playerWon ? "White" : "Black";
                }

                bool localIsWhite = GameProgress.Instance.IsLocalPlayerWhite();
                if (playerWon)
                {
                        return localIsWhite ? "White" : "Black";
                }

                return localIsWhite ? "Black" : "White";
        }
}
