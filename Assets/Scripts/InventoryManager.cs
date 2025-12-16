using UnityEngine;

public class InventoryManager : MonoBehaviour
{
	public static InventoryManager Instance { get; private set; }

	[Header("Ustawienia Ekwipunku")]
	public int rows = 5;
	public int cols = 2;
	public GameObject tilePrefab;
	public GameObject kingPrefab; // PRZYPISZ TU KRÓLA!
	public Vector2 inventoryOffset = new Vector2(0, 5);

	public Color inventoryColor1 = new Color(0.3f, 0.3f, 0.3f);
	public Color inventoryColor2 = new Color(0.4f, 0.4f, 0.4f);

	private GameObject[,] inventoryTiles;

	private void Awake() => Instance = this;

	private void Start()
	{
		GenerateInventory();
		Invoke("SpawnKingOnBoard", 0.1f);
	}

	void GenerateInventory()
	{
		float startX = BoardManager.Instance.playerOffset.x + BoardManager.Instance.PlayerCols + 1.0f;
		float startY = BoardManager.Instance.playerOffset.y + inventoryOffset.y;

		inventoryTiles = new GameObject[rows, cols];

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector3 pos = new Vector3(startX + c, startY + r, 0);
				GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity);
				go.name = $"Inv_Tile_{r}_{c}";
				go.transform.parent = null;

				Tile tile = go.GetComponent<Tile>();
				tile.isInventory = true;
				tile.boardType = BoardType.Player;

				SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
				if (sr != null) sr.color = (r + c) % 2 == 0 ? inventoryColor1 : inventoryColor2;

				inventoryTiles[r, c] = go;
			}
		}
	}

	void SpawnKingOnBoard()
	{
		Tile centerTile = BoardManager.Instance.GetPlayerCenterTile();
		if (centerTile != null && kingPrefab != null)
		{
			SpawnPiece(kingPrefab, centerTile, PieceType.King);
		}
	}

	public void AddPieceToInventory(PieceType type, GameObject prefab)
	{
		foreach (var tileGO in inventoryTiles)
		{
			Tile tile = tileGO.GetComponent<Tile>();
			if (!tile.isOccupied)
			{
				SpawnPiece(prefab, tile, type);
				return;
			}
		}
		Debug.Log("Ekwipunek pe³ny!");
	}

	void SpawnPiece(GameObject prefab, Tile tile, PieceType type)
	{
		GameObject pieceGO = Instantiate(prefab, tile.transform.position, Quaternion.identity);

		// Dodajemy skrypt ruchu automatycznie
		if (pieceGO.GetComponent<PieceMovement>() == null)
			pieceGO.AddComponent<PieceMovement>();

		Piece piece = pieceGO.GetComponent<Piece>();
		piece.owner = PieceOwner.Player;
		piece.pieceType = type;
		piece.currentTile = tile;

		tile.isOccupied = true;
		tile.currentPiece = piece;

		Vector3 pos = pieceGO.transform.position;
		pos.z = -1;
		pieceGO.transform.position = pos;
	}
}