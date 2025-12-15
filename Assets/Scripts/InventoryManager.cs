using UnityEngine;

public class InventoryManager : MonoBehaviour
{
	public static InventoryManager Instance { get; private set; }

	[Header("Ustawienia Ekwipunku")]
	public int rows = 5;
	public int cols = 2;
	public GameObject tilePrefab;
	public GameObject kingPrefab; // PRZYPISZ PREFAB KRÓLA!

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
		// OpóŸnienie spawnowania króla o klatkê, ¿eby BoardManager zd¹¿y³ wygenerowaæ planszê
		Invoke("SpawnKingOnBoard", 0.1f);
	}

	void GenerateInventory()
	{
		float startX = BoardManager.Instance.playerOffset.x + BoardManager.Instance.PlayerCols + 1.0f;
		float startY = BoardManager.Instance.playerOffset.y;

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
				sr.color = (r + c) % 2 == 0 ? inventoryColor1 : inventoryColor2;

				inventoryTiles[r, c] = go;
			}
		}
	}

	void SpawnKingOnBoard()
	{
		Tile centerTile = BoardManager.Instance.GetPlayerCenterTile();
		if (centerTile != null && kingPrefab != null)
		{
			// Spawbujemy Króla od razu na planszy
			SpawnPiece(kingPrefab, centerTile, PieceType.King);
		}
		else
		{
			Debug.LogWarning("Nie uda³o siê znaleŸæ œrodka planszy dla Króla!");
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

		// Dodajemy skrypt ruchu, jeœli prefab go nie ma
		if (pieceGO.GetComponent<PieceMovement>() == null)
			pieceGO.AddComponent<PieceMovement>();

		Piece piece = pieceGO.GetComponent<Piece>();
		piece.owner = PieceOwner.Player;
		piece.pieceType = type;
		piece.currentTile = tile;

		tile.isOccupied = true;
		tile.currentPiece = piece;

		// Ustawienie Z na -1, ¿eby figura by³a nad kafelkiem
		Vector3 pos = pieceGO.transform.position;
		pos.z = -1;
		pieceGO.transform.position = pos;
	}
}