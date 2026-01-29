using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Chess/Economy Config", fileName = "EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [System.Serializable]
    public struct PieceSpawnWeights
    {
        public int roundNumber;
        public int pawnWeight;
        public int kingWeight;
        public int queenWeight;
        public int rookWeight;
        public int bishopWeight;
        public int knightWeight;
    }

	[Header("Unlock rounds (1 = from start)")]
	public int pawnUnlockRound = 1;
	public int knightUnlockRound = 1;
	public int bishopUnlockRound = 1;
	public int rookUnlockRound = 3;  
	public int queenUnlockRound = 5;

	[System.Serializable]
    public struct PiecePricesByRound
    {
        public int roundNumber;
        public int pawnPrice;
        public int kingPrice;
        public int queenPrice;
        public int rookPrice;
        public int bishopPrice;
        public int knightPrice;
    }

    [System.Serializable]
    public struct CashRewardsByRound
    {
        public int roundNumber;
        public int winReward;
        public int loseReward;
    }

    [Header("Coins")]
    public int startingCoins = 100;
    public int winReward = 20;
    public int loseReward = 10;
    public int rerollCost = 5;
    public string configVersion = "v1";

    [Header("Prices")]
    public int pawnPrice = 10;
    public int kingPrice = 0;
    public int queenPrice = 100;
    public int rookPrice = 50;
    public int bishopPrice = 30;
    public int knightPrice = 30;

    [Header("Prices (per round)")]
    public PiecePricesByRound[] pricesByRound;

    [Header("Cash Rewards (per round)")]
    public CashRewardsByRound[] cashRewardsByRound;

    [Header("Shop Spawn Weights (per round)")]
    public PieceSpawnWeights[] spawnWeightsByRound;

	public int GetPrice(PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return pawnPrice;
            case PieceType.King:
                return kingPrice;
            case PieceType.queen:
                return queenPrice;
            case PieceType.Rook:
                return rookPrice;
            case PieceType.Bishop:
                return bishopPrice;
            case PieceType.Knight:
                return knightPrice;
            default:
                return pawnPrice;
        }
    }

    public int GetPrice(PieceType type, int roundNumber)
    {
        if (TryGetPricesForRound(roundNumber, out PiecePricesByRound prices))
        {
            return GetPriceFromRound(prices, type);
        }

        return GetPrice(type);
    }

    public int GetWinReward(int roundNumber)
    {
        if (TryGetCashRewards(roundNumber, out CashRewardsByRound rewards))
        {
            return rewards.winReward;
        }

        return winReward;
    }

    public int GetLoseReward(int roundNumber)
    {
        if (TryGetCashRewards(roundNumber, out CashRewardsByRound rewards))
        {
            return rewards.loseReward;
        }

        return loseReward;
    }

    private int GetPriceFromRound(PiecePricesByRound prices, PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return prices.pawnPrice;
            case PieceType.King:
                return prices.kingPrice;
            case PieceType.queen:
                return prices.queenPrice;
            case PieceType.Rook:
                return prices.rookPrice;
            case PieceType.Bishop:
                return prices.bishopPrice;
            case PieceType.Knight:
                return prices.knightPrice;
            default:
                return prices.pawnPrice;
        }
    }

    public bool TryGetSpawnWeights(int roundNumber, out PieceSpawnWeights weights)
    {
        weights = default;

        if (spawnWeightsByRound == null || spawnWeightsByRound.Length == 0)
        {
            return false;
        }

        int bestIndex = -1;
        for (int i = 0; i < spawnWeightsByRound.Length; i++)
        {
            if (spawnWeightsByRound[i].roundNumber == roundNumber)
            {
                bestIndex = i;
                break;
            }

            if (spawnWeightsByRound[i].roundNumber <= roundNumber)
            {
                if (bestIndex == -1 || spawnWeightsByRound[i].roundNumber > spawnWeightsByRound[bestIndex].roundNumber)
                {
                    bestIndex = i;
                }
            }
        }

        if (bestIndex < 0)
        {
            return false;
        }

        weights = spawnWeightsByRound[bestIndex];
		ApplyUnlocks(roundNumber, ref weights);
		return true;


    }

    public bool TryGetPricesForRound(int roundNumber, out PiecePricesByRound prices)
    {
        prices = default;

        if (pricesByRound == null || pricesByRound.Length == 0)
        {
            return false;
        }

        int bestIndex = -1;
        for (int i = 0; i < pricesByRound.Length; i++)
        {
            if (pricesByRound[i].roundNumber == roundNumber)
            {
                bestIndex = i;
                break;
            }

            if (pricesByRound[i].roundNumber <= roundNumber)
            {
                if (bestIndex == -1 || pricesByRound[i].roundNumber > pricesByRound[bestIndex].roundNumber)
                {
                    bestIndex = i;
                }
            }
        }

        if (bestIndex < 0)
        {
            return false;
        }

        prices = pricesByRound[bestIndex];
        return true;
    }

    public bool TryGetCashRewards(int roundNumber, out CashRewardsByRound rewards)
    {
        rewards = default;

        if (cashRewardsByRound == null || cashRewardsByRound.Length == 0)
        {
            return false;
        }

        int bestIndex = -1;
        for (int i = 0; i < cashRewardsByRound.Length; i++)
        {
            if (cashRewardsByRound[i].roundNumber == roundNumber)
            {
                bestIndex = i;
                break;
            }

            if (cashRewardsByRound[i].roundNumber <= roundNumber)
            {
                if (bestIndex == -1 || cashRewardsByRound[i].roundNumber > cashRewardsByRound[bestIndex].roundNumber)
                {
                    bestIndex = i;
                }
            }
        }

        if (bestIndex < 0)
        {
            return false;
        }

        rewards = cashRewardsByRound[bestIndex];
        return true;
    }


	private void ApplyUnlocks(int roundNumber, ref PieceSpawnWeights w)
	{
		// King nigdy w sklepie
		w.kingWeight = 0;

		if (roundNumber < pawnUnlockRound) w.pawnWeight = 0;
		if (roundNumber < knightUnlockRound) w.knightWeight = 0;
		if (roundNumber < bishopUnlockRound) w.bishopWeight = 0;
		if (roundNumber < rookUnlockRound) w.rookWeight = 0;
		if (roundNumber < queenUnlockRound) w.queenWeight = 0;
	}
}
