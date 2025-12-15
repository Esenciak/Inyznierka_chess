using UnityEngine;

public class InventoryManager : MonoBehaviour
{
	public static InventoryManager Instance { get; private set; }

	[Header("Ustawienia Ekwipunku")]
	public int rows = 5; // Wysokoœæ (5)
	public int cols = 2; // Szerokoœæ (2)
	public Vector2 offsetFromBoard = new Vector2(2.0f, 0.0f); // Odstêp od planszy gracza

	public GameObject tilePrefab;
	public Color inventoryColor1 = new Color(0.3f, 0.3f, 0.3f);
	public Color inventoryColor2 = new Color(0.4f, 0.4f, 0.4f);
	private GameObject[,] inventoryTiles;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		GenerateInventory();
	}

	void GenerateInventory()
	{
		// Pobieramy szerokoœæ planszy gracza, ¿eby wiedzieæ gdzie zacz¹æ rysowaæ ekwipunek
		float startX = BoardManager.Instance.PlayerCols + offsetFromBoard.x;
		float startY = BoardManager.Instance.playerOffset.y; // Zaczynamy na poziomie gracza

		inventoryTiles = new GameObject[rows, cols];

		

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				// Pozycja w œwiecie
				Vector3 pos = new Vector3(startX + c, startY + r, 0);

				GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity);
				go.name = $"Inventory_Tile_{r}_{c}";
				go.transform.parent = null; // Porz¹dek w hierarchii

				Tile tile = go.GetComponent<Tile>();
				tile.boardType = BoardType.Player; // Traktujemy to jako strefê gracza (dozwolon¹ do stawiania)
				tile.isInventory = true; // WA¯NE: Dodaj bool isInventory w Tile.cs (instrukcja ni¿ej)

				// Kolorowanie na "szachownicê"
				SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
				if (sr != null)
				{
					sr.color = (r + c) % 2 == 0 ? inventoryColor1 : inventoryColor2;
				}
				inventoryTiles[r, c] = go;
			}
		}
	}

	
	public void AddPieceToInventory(PieceType type, GameObject prefab)
    {
        // Szukamy wolnego miejsca w inventoryTiles[,]
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // Musimy pobraæ komponent Tile z GameObjectu w tablicy
                GameObject tileGO = inventoryTiles[r, c];
                Tile tile = tileGO.GetComponent<Tile>();

                if (!tile.isOccupied)
                {
                    // Tworzymy now¹, grywaln¹ figurê
                    Vector3 pos = tile.transform.position;
                    pos.z = -1;
                    GameObject newPieceGO = Instantiate(prefab, pos, Quaternion.identity);
                    Piece piece = newPieceGO.GetComponent<Piece>();

                    // Konfiguracja
                    piece.owner = PieceOwner.Player;
                    piece.pieceType = type;
                    piece.currentTile = tile;
                    
                    tile.isOccupied = true;
                    tile.currentPiece = piece;
                    
                    return; // Zrobione, wychodzimy
                }
            }
        }
        Debug.Log("Ekwipunek pe³ny! Sprzedaj coœ albo zwolnij miejsce.");
        // Opcjonalnie: Zwróæ kasê jeœli nie ma miejsca
    }
}