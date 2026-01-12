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
		// Zapamiêtujemy kolor startowy (nadany przez BoardManager)
		if (sr != null) originalColor = sr.color;
	}

	// Tê funkcjê wywo³a Piece.cs
	public void SetHighlight(bool active)
	{
		if (sr == null) return;

		if (active)
		{
			// Kolor podœwietlenia (np. pó³przezroczysty ¿ó³ty lub jaskrawy)
			sr.color = Color.yellow;
		}
		else
		{
			// Przywracamy orygina³
			sr.color = originalColor;
		}
	}
}