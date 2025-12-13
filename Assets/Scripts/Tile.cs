using UnityEngine;

public class Tile : MonoBehaviour
{
	public int row;
	public int col;

	public int globalRow;
	public int globalCol;

	public BoardType boardType;

	public bool isOccupied;
	public Piece currentPiece;
}
