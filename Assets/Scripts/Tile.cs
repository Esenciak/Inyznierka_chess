using UnityEngine;

public class Tile : MonoBehaviour
{
	public int row;
	public int col;
	public int globalRow;
	public int globalCol;

	public BoardType boardType;
	public bool isInventory;
	public bool isOccupied;
	public Piece currentPiece;

	private SpriteRenderer sr;
	private Color originalColor;

	private void Awake()
	{
		sr = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		if (sr != null) originalColor = sr.color;
	}

	public void SetHighlight(bool active)
	{
		if (sr == null) return;

		if (active)
		{
			sr.color = Color.yellow;
		}
		else
		{
			sr.color = originalColor;
		}
	}
}