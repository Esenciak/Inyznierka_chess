using UnityEngine;

public class Tile : MonoBehaviour
{
	public int row;         // lokalny rz¹d w obrêbie swojej planszy
	public int col;         // lokalna kolumna

	public int globalRow;   // rz¹d w jednej wspólnej "wie¿y" (player+center+enemy)
	public int globalCol;   // na razie = col

	public BoardType boardType;

	public bool isOccupied;
	public Piece currentPiece;
}
