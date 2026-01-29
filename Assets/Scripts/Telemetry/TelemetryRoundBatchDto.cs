using System.Collections.Generic;

public class TelemetryRoundBatchDto
{
	public int SchemaVersion { get; set; } = 1;
	public string MatchId { get; set; }
    public string PlayerId { get; set; }
    public int RoundNumber { get; set; }
    public string BalanceVersion { get; set; }
    public int BoardSize { get; set; }
    public int CoinsBeforeShop { get; set; }
    public int CoinsAfterShop { get; set; }
    public List<TelemetryEventBase> Events { get; set; }
}
