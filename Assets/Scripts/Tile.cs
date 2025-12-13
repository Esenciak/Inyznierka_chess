using UnityEngine;

public class Tile : MonoBehaviour
{
	public int row;         
	public int col;         

	public int globalRow;   // rz¹d w jednej wspólnej planszy (player+center+enemy)
	public int globalCol;   // na razie = col

	public BoardType boardType;

	public bool isOccupied;
	public Piece currentPiece;
}
