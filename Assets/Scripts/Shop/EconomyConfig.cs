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

    [Header("Coins")]
    public int startingCoins = 100;
    public int winReward = 20;
    public int loseReward = 10;
    public int rerollCost = 5;

    [Header("Prices")]
    public int pawnPrice = 10;
    public int kingPrice = 0;
    public int queenPrice = 100;
    public int rookPrice = 50;
    public int bishopPrice = 30;
    public int knightPrice = 30;

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
        return true;
    }
}
