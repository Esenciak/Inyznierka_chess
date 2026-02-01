using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TelemetryService : MonoBehaviour
{
    public static TelemetryService Instance { get; private set; }

    [SerializeField] private TelemetryConfig config;
	private bool matchStartLogged;

	private TelemetryClock clock;
    private TelemetryHttpClient httpClient;
    private TelemetryQueueStorage queueStorage;

	private TelemetryFileLogger fileLogger;


	private readonly List<TelemetryEventBase> currentEvents = new List<TelemetryEventBase>();

    private string matchId;
    private string playerId;
    private int currentRoundNumber;
    private int coinsBeforeShop;
    private int coinsAfterShop;
    private bool matchStarted;
    private Coroutine flushRoutine;
    private int clientEventSeq;
    private int localTurnIndexInRound;
    private int lastTurnIndexInRound = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("TelemetryService");
        DontDestroyOnLoad(go);
        go.AddComponent<TelemetryService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        config = config != null ? config : Resources.Load<TelemetryConfig>("Telemetry/TelemetryConfig");
        clock = new TelemetryClock();
        httpClient = new TelemetryHttpClient();
        queueStorage = new TelemetryQueueStorage();
        RefreshPlayerId();

        if (config != null && config.enableTelemetry)
        {
            clock.Reset();
            flushRoutine = StartCoroutine(FlushLoop());
            StartCoroutine(FlushOfflineQueue());
        }

		fileLogger = new TelemetryFileLogger();
		Debug.Log($"[Telemetry] persistentDataPath = {Application.persistentDataPath}");

	}

	public void StartMatchIfNeeded(int roundNumber)
	{
		if (!IsTelemetryEnabled())
			return;

		RefreshPlayerId();

		// zawsze gwarantuj matchId (offline fallback)
		if (string.IsNullOrEmpty(matchId))
			matchId = TelemetryIds.CreateMatchId();

		// inicjalizacja stanu meczu tylko raz
		if (!matchStarted)
		{
			matchStarted = true;
			clientEventSeq = 0;
			clock.Reset();
			matchStartLogged = false;
		}

		// MatchStart logujemy dokładnie raz na mecz
		if (!matchStartLogged)
		{
			LogEvent(CreateBaseEvent(TelemetryEventTypes.MatchStart, roundNumber));
			matchStartLogged = true;
		}
	}


	public void SetMatchContext(string newMatchId)
	{
		if (!IsTelemetryEnabled())
			return;

		if (string.IsNullOrEmpty(newMatchId))
			return;

		if (!string.IsNullOrEmpty(matchId) && matchId == newMatchId)
			return;

		RefreshPlayerId();

		matchId = newMatchId;

		// reset "meczu" na nowy kontekst lobby
		matchStarted = false;
		matchStartLogged = false;
		clientEventSeq = 0;
		clock.Reset();

		// bezpiecznie: nie mieszaj eventów między lobby
		currentEvents.Clear();

		ResetTurnIndexInRound();
	}



	public void StartRound(int roundNumber, int coinsAtStart)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        StartMatchIfNeeded(roundNumber);
        currentRoundNumber = roundNumber;
        coinsBeforeShop = coinsAtStart;
        coinsAfterShop = coinsAtStart;

        LogEvent(CreateBaseEvent(TelemetryEventTypes.RoundStart, roundNumber));
    }

    public void ResetTurnIndexInRound()
    {
        localTurnIndexInRound = 0;
        lastTurnIndexInRound = -1;
    }

    public int GetNextLocalTurnIndexInRound()
    {
        int index = localTurnIndexInRound;
        localTurnIndexInRound++;
        lastTurnIndexInRound = index;
        return index;
    }

    public int GetLastTurnIndexInRound()
    {
        return Mathf.Max(lastTurnIndexInRound, 0);
    }

    public void RecordCoinsAfterShop(int coins)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        coinsAfterShop = coins;
    }

    public void LogShopOfferGenerated(List<string> offeredPieces, int shopSlots, int rerollCost)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.ShopOfferGenerated, currentRoundNumber);
        evt.OfferedPieces = offeredPieces?.ToArray();
        evt.ShopSlots = shopSlots;
        evt.RerollCost = rerollCost;
        LogEvent(evt);
    }

    public void LogPurchase(string pieceType, int price, int shopSlotIndex, int coinsBefore, int coinsAfter)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.Purchase, currentRoundNumber);
        evt.PieceType = pieceType;
        evt.Price = price;
        evt.ShopSlotIndex = shopSlotIndex;
        evt.CoinsBefore = coinsBefore;
        evt.CoinsAfter = coinsAfter;
        LogEvent(evt);
    }

    public void LogReroll(int cost, int coinsBefore, int coinsAfter)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.Reroll, currentRoundNumber);
        evt.Cost = cost;
        evt.CoinsBefore = coinsBefore;
        evt.CoinsAfter = coinsAfter;
        LogEvent(evt);
    }

    public void LogSell(string pieceType, int refund, int coinsBefore, int coinsAfter)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.Sell, currentRoundNumber);
        evt.PieceType = pieceType;
        evt.Refund = refund;
        evt.CoinsBefore = coinsBefore;
        evt.CoinsAfter = coinsAfter;
        LogEvent(evt);
    }

    public void LogPiecePlaced(string pieceType, int toX, int toY, string source)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.PiecePlaced, currentRoundNumber);
        evt.PieceType = pieceType;
        evt.ToX = toX;
        evt.ToY = toY;
        evt.Source = source;
        evt.BoardContext = "Setup";
        LogEvent(evt);
    }

    public void LogBattleStart(int boardSize)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        ResetTurnIndexInRound();
        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.BattleStart, currentRoundNumber);
        evt.BoardSize = boardSize;
        LogEvent(evt);
    }

    public void LogPieceMoved(string pieceType, int fromX, int fromY, int toX, int toY, int turnIndexInRound)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.PieceMoved, currentRoundNumber);
        evt.PieceType = pieceType;
        evt.FromX = fromX;
        evt.FromY = fromY;
        evt.ToX = toX;
        evt.ToY = toY;
        evt.BoardContext = "Battle";
        evt.TurnIndexInRound = turnIndexInRound;
        LogEvent(evt);
    }

    public void LogPieceCaptured(
        string pieceType,
        int fromX,
        int fromY,
        int toX,
        int toY,
        string capturedPieceType,
        int? boardSize = null,
        string capturedOnRegion = null,
        int? turnIndexInRound = null)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.PieceCaptured, currentRoundNumber);
        evt.PieceType = pieceType;
        evt.FromX = fromX;
        evt.FromY = fromY;
        evt.ToX = toX;
        evt.ToY = toY;
        evt.CapturedPieceType = capturedPieceType;
        evt.BoardContext = "Battle";
        evt.BoardSize = boardSize;
        evt.CapturedOnRegion = capturedOnRegion;
        evt.TurnIndexInRound = turnIndexInRound;
        LogEvent(evt);
    }

    public void LogRoundEnd(bool playerWon, int coinsEnd, int piecesRemaining, int boardSize, int turnIndexInRound)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.RoundEnd, currentRoundNumber);
        evt.PlayerWon = playerWon;
        evt.CoinsEnd = coinsEnd;
        evt.PiecesRemaining = piecesRemaining;
        evt.BoardSize = boardSize;
        evt.TurnIndexInRound = turnIndexInRound;
        LogEvent(evt);

        SendRoundBatch(boardSize);
        currentEvents.Clear();
    }

    public void LogResignRound(bool playerWon, int coinsEnd, int piecesRemaining, int boardSize, int turnIndexInRound)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.ResignRound, currentRoundNumber);
        evt.PlayerWon = playerWon;
        evt.CoinsEnd = coinsEnd;
        evt.PiecesRemaining = piecesRemaining;
        evt.BoardSize = boardSize;
        evt.TurnIndexInRound = turnIndexInRound;
        LogEvent(evt);
    }

    public void LogMatchEnd(string winnerColor, string reason, int totalRounds)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryEventBase evt = CreateBaseEvent(TelemetryEventTypes.MatchEnd, currentRoundNumber);
        evt.WinnerColor = winnerColor;
        evt.Reason = reason;
        evt.TotalRounds = totalRounds;
        LogEvent(evt);
    }

    public bool IsTelemetryEnabled()
    {
        return config != null && config.enableTelemetry;
    }

    public bool IsLocalOwner(PieceOwner owner)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            bool localIsHost = Unity.Netcode.NetworkManager.Singleton.IsHost;
            return localIsHost ? owner == PieceOwner.Player : owner == PieceOwner.Enemy;
        }

        if (GameManager.Instance != null && GameManager.Instance.isMultiplayer && GameProgress.Instance != null)
        {
            return GameProgress.Instance.isHostPlayer ? owner == PieceOwner.Player : owner == PieceOwner.Enemy;
        }

        return owner == PieceOwner.Player;
    }

    public static string ToTelemetryPieceType(PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return "Pawn";
            case PieceType.King:
                return "King";
            case PieceType.queen:
                return "Queen";
            case PieceType.Rook:
                return "Rook";
            case PieceType.Bishop:
                return "Bishop";
            case PieceType.Knight:
                return "Knight";
            default:
                return type.ToString();
        }
    }

    private TelemetryEventBase CreateBaseEvent(string eventType, int roundNumber)
    {
        return new TelemetryEventBase
        {
            EventId = Guid.NewGuid().ToString(),
            MatchId = matchId,
            PlayerId = playerId,
            RoundNumber = roundNumber,
            EventType = eventType,
            TimestampUtc = clock.GetTimestampUtc(),
            ClientTimeMsFromMatchStart = clock.GetElapsedMs(),
            ClientEventSeq = clientEventSeq++
        };
    }

    private void RefreshPlayerId()
    {
        playerId = TelemetryIds.GetOrCreatePlayerId();
    }

    private void LogEvent(TelemetryEventBase evt)
    {
        try
        {
            if (evt == null)
            {
                return;
            }

            currentEvents.Add(evt);
            if (config != null && config.logToUnityConsole)
            {
                Debug.Log($"[Telemetry] Logged {evt.EventType} for round {evt.RoundNumber}.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Telemetry] Failed to log event: {ex.Message}");
        }
    }

    private void SendRoundBatch(int boardSize)
    {
        if (!IsTelemetryEnabled())
        {
            return;
        }

        TelemetryRoundBatchDto batch = new TelemetryRoundBatchDto
        {
            MatchId = matchId,
            PlayerId = playerId,
            PlayerName = LobbyState.LocalPlayerName,
            RoundNumber = currentRoundNumber,
            BalanceVersion = GetBalanceVersion(),
            BoardSize = boardSize,
            CoinsBeforeShop = coinsBeforeShop,
            CoinsAfterShop = coinsAfterShop,
            Events = new List<TelemetryEventBase>(currentEvents)
        };

		string json = TelemetryJson.SerializeBatch(batch);

		// DEBUG: zawsze zapisuj batch lokalnie, ¿eby sprawdziæ czy dzia³a
		if (config != null && config.writeBatchesToDisk)
		{
			fileLogger.WriteBatch(json, currentRoundNumber);
		}

		StartCoroutine(SendOrQueue(json));


	}

	private string GetBalanceVersion()
    {
        if (GameProgress.Instance != null && GameProgress.Instance.economyConfig != null)
        {
            return GameProgress.Instance.economyConfig.configVersion;
        }

        if (GameManager.Instance != null && GameManager.Instance.economyConfig != null)
        {
            return GameManager.Instance.economyConfig.configVersion;
        }

        return "unknown";
    }

	private IEnumerator SendOrQueue(string json)
	{
		if (!IsTelemetryEnabled())
			yield break;

		string url = BuildRoundBatchUrl();
		if (string.IsNullOrEmpty(url))
		{
			queueStorage.SaveBatch(json);
			yield break;
		}

		bool success = false;

		int timeout = (config != null) ? config.requestTimeoutSeconds : 10;
		int retries = (config != null) ? config.maxRetries : 3;

		// jawnie uruchamiamy coroutine (najbardziej kompatybilne)
		yield return StartCoroutine(
	        httpClient.SendJsonWithRetry(url, json, timeout, retries, result => success = result)
                                    );

		if (!success)
			queueStorage.SaveBatch(json);
	}

	private IEnumerator FlushLoop()
	{
		while (true)
		{
			if (IsTelemetryEnabled())
			{
				yield return StartCoroutine(FlushOfflineQueue());
			}

			int wait = config != null ? Mathf.Max(1, config.flushIntervalSeconds) : 15;
			yield return new WaitForSeconds(wait);
		}
	}

	private IEnumerator FlushOfflineQueue()
    {
        if (!IsTelemetryEnabled())
        {
            yield break;
        }

        string url = BuildRoundBatchUrl();
        if (string.IsNullOrEmpty(url))
        {
            yield break;
        }

        List<string> files = queueStorage.LoadQueuedBatchFiles();
        foreach (string file in files)
        {
            string json = queueStorage.ReadBatch(file);
            if (string.IsNullOrEmpty(json))
            {
                queueStorage.DeleteBatch(file);
                continue;
            }

            bool success = false;
            yield return httpClient.SendJsonWithRetry(url, json, config.requestTimeoutSeconds, config.maxRetries, result => success = result);

            if (success)
            {
                queueStorage.DeleteBatch(file);
            }
            else
            {
                break;
            }
        }
    }

    private string BuildRoundBatchUrl()
    {
        if (config == null || string.IsNullOrEmpty(config.baseUrl))
        {
            return null;
        }

        string baseUrlTrimmed = config.baseUrl.TrimEnd('/');
        string path = string.IsNullOrEmpty(config.roundBatchEndpointPath) ? string.Empty : config.roundBatchEndpointPath.TrimStart('/');
        return string.IsNullOrEmpty(path) ? baseUrlTrimmed : $"{baseUrlTrimmed}/{path}";
    }

	public void LogResignAndFlush(int boardSize, int coinsEnd, int piecesRemaining, string winnerColor, int totalRounds)
	{
		if (!IsTelemetryEnabled()) return;

		int ti = GetLastTurnIndexInRound();

		// 1) ResignRound MUSI być przed RoundEnd
		LogResignRound(false, coinsEnd, piecesRemaining, boardSize, ti);

		// 2) MatchEnd chcemy mieć w tym samym batchu, więc przed RoundEnd
		LogMatchEnd(winnerColor, "Resign", totalRounds);

		// 3) RoundEnd wysyła batch i czyści eventy
		LogRoundEnd(false, coinsEnd, piecesRemaining, boardSize, ti);
	}

	public void ResetMatchState()
	{
		matchId = null;
		matchStarted = false;
		matchStartLogged = false;

		currentRoundNumber = 0;
		coinsBeforeShop = 0;
		coinsAfterShop = 0;

		clientEventSeq = 0;
		currentEvents.Clear();
		ResetTurnIndexInRound();

		// Nie resetuję clock tutaj — zresetuje się przy StartMatchIfNeeded
	}


}
