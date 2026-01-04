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

	// Pamiêæ koloru 
	public Color originalColor {  get; set; }
	private SpriteRenderer sr;

	public bool isInventory = false;

	private void Awake()
	{
		sr = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		// Zapamiêtaj kolor ustawiony przy generowaniu planszy
		if (sr != null) originalColor = sr.color;
	}

	public void SetHighlight(bool active)
	{
		if (sr == null) return;

		if (active)
		{
			sr.color = Color.green; // Kolor podœwietlenia
		}
		else
		{
			sr.color = originalColor; // Wróæ do zapamiêtanego orygina³u
		}
	}
}