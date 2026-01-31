using System;
using System.Collections.Generic;

public static class TelemetryEventTypes
{
    public const string MatchStart = "MatchStart";
    public const string RoundStart = "RoundStart";
    public const string ShopOfferGenerated = "ShopOfferGenerated";
    public const string Purchase = "Purchase";
    public const string Reroll = "Reroll";
    public const string Sell = "Sell";
    public const string PiecePlaced = "PiecePlaced";
    public const string BattleStart = "BattleStart";
    public const string PieceMoved = "PieceMoved";
    public const string PieceCaptured = "PieceCaptured";
    public const string RoundEnd = "RoundEnd";
    public const string ResignRound = "ResignRound";
    public const string MatchEnd = "MatchEnd";
}

public class TelemetryEventBase
{
    public string EventId { get; set; }
    public string MatchId { get; set; }
    public string PlayerId { get; set; }
    public int RoundNumber { get; set; }
    public string EventType { get; set; }
    public string TimestampUtc { get; set; }
    public long ClientTimeMsFromMatchStart { get; set; }
    public int ClientEventSeq { get; set; }
    public int? TurnIndexInRound { get; set; }

    public string[] OfferedPieces { get; set; }
    public int? ShopSlots { get; set; }
    public int? RerollCost { get; set; }

    public string PieceType { get; set; }
    public int? Price { get; set; }
    public int? ShopSlotIndex { get; set; }
    public int? CoinsBefore { get; set; }
    public int? CoinsAfter { get; set; }
    public int? Cost { get; set; }
    public int? Refund { get; set; }

    public int? FromX { get; set; }
    public int? FromY { get; set; }
    public int? ToX { get; set; }
    public int? ToY { get; set; }
    public string Source { get; set; }
    public string BoardContext { get; set; }

    public int? BoardSize { get; set; }
    public string CapturedPieceType { get; set; }
    public string CapturedOnRegion { get; set; }

    public bool? PlayerWon { get; set; }
    public int? CoinsEnd { get; set; }
    public int? PiecesRemaining { get; set; }

    public string WinnerColor { get; set; }
    public string Reason { get; set; }
    public int? TotalRounds { get; set; }
}
