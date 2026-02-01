using System;
using System.Collections.Generic;

[Serializable]
public class TelemetryRoundBatchDto
{
	public int schemaVersion = 1;
	public string matchId;
	public string playerId;
	public string playerName;
	public int roundNumber;
	public string balanceVersion;
	public int boardSize;
	public int coinsBeforeShop;
	public int coinsAfterShop;
	public List<TelemetryEventDto> events = new List<TelemetryEventDto>();
}

[Serializable]
public class TelemetryEventDto
{
	public string eventId;
	public string matchId;
	public string playerId;
	public int roundNumber;
	public string eventType;
	public string timestampUtc;
	public long clientTimeMsFromMatchStart;
	public int clientEventSeq;

	// Pola opcjonalne (zale¿ne od typu zdarzenia)
	public string pieceType;
	public int price;
	public int shopSlotIndex;
	public int coinsBefore;
	public int coinsAfter;
	public int toX;
	public int toY;
	public string source;
	public string boardContext;
}