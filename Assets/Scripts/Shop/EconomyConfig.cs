using UnityEngine;

[CreateAssetMenu(menuName = "Chess/Economy Config", fileName = "EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [Header("Coins")]
    public int startingCoins = 100;
    public int winReward = 20;
    public int loseReward = 10;

    [Header("Prices")]
    public int pawnPrice = 10;
    public int kingPrice = 0;
    public int queenPrice = 100;
    public int rookPrice = 50;
    public int bishopPrice = 30;
    public int knightPrice = 30;

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
}
