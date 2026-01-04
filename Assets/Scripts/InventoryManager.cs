using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Potrzebne do Coroutine
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
	public static InventoryManager Instance { get; private set; }

	[Header("Ustawienia Ekwipunku")]
	public int rows = 5;
	public int cols = 2;
	public GameObject tilePrefab;
	public GameObject kingPrefab;
	public Vector2 inventoryOffset = new Vector2(0, 5);

	public Color inventoryColor1 = new Color(0.3f, 0.3f, 0.3f);
	public Color inventoryColor2 = new Color(0.4f, 0.4f, 0.4f);

	private List<GameObject> inventoryTiles = new List<GameObject>();

	private void Awake() => Instance = this;

	private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
	private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == "Shop")
		{
			// Zmieniamy na Coroutine, ¿eby poczekaæ na BoardManagera
			StartCoroutine(InitializeInventoryRoutine());
		}
		else if (scene.name == "MainMenu")
		{
			ClearInventory();
		}
	}

	private void Start()
	{
		// Fallback dla testowania samej sceny Shop
		if (SceneManager.GetActiveScene().name == "Shop")
		{
			StartCoroutine(InitializeInventoryRoutine());
		}
	}

	// --- POPRAWKA: Czekamy na BoardManagera ---
	IEnumerator InitializeInventoryRoutine()
	{
		// Czekamy, a¿ BoardManager powstanie (jeœli jest null)
		while (BoardManager.Instance == null)
		{
			yield return null;
		}

		// Czekamy jeszcze jedn¹ klatkê dla pewnoœci, ¿e BoardManager obliczy³ offsety
		yield return new WaitForEndOfFrame();

		ClearInventory();
		GenerateInventory();

		// Króla te¿ spawnujemy z ma³ym opóŸnieniem
		SpawnKingOnBoard();
	}
	// ------------------------------------------

	void ClearInventory()
	{
		foreach (var go in inventoryTiles)
		{
			if (go != null) Destroy(go);
		}
		inventoryTiles.Clear();
	}

	void GenerateInventory()
	{
		if (BoardManager.Instance == null)
		{
			Debug.LogError("BoardManager nadal jest null! Nie mogê stworzyæ Inventory.");
			return;
		}

		float startX = BoardManager.Instance.playerOffset.x + BoardManager.Instance.PlayerCols + inventoryOffset.x;
		float startY = BoardManager.Instance.playerOffset.y + inventoryOffset.y;

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector3 pos = new Vector3(startX + c, startY + r, 0);
				GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity);
				go.name = $"Inv_Tile_{r}_{c}";
				go.transform.parent = transform;

				Tile tile = go.GetComponent<Tile>();
				tile.isInventory = true;
				tile.boardType = BoardType.Player;

				SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
				if (sr != null) sr.color = (r + c) % 2 == 0 ? inventoryColor1 : inventoryColor2;

				inventoryTiles.Add(go);
			}
		}
		Debug.Log($"Inventory wygenerowane: {inventoryTiles.Count} slotów.");
	}

	void SpawnKingOnBoard()
	{
		if (BoardManager.Instance == null) return;

		Tile centerTile = BoardManager.Instance.GetPlayerCenterTile();
		if (centerTile != null && kingPrefab != null && !centerTile.isOccupied)
		{
			SpawnPiece(kingPrefab, centerTile, PieceType.King);
		}
	}

	public bool AddPieceToInventory(PieceType type, GameObject prefab)
	{
		// Zabezpieczenie przed pust¹ list¹
		if (inventoryTiles == null || inventoryTiles.Count == 0)
		{
			Debug.LogError("B³¹d: Próba dodania do Inventory, ale lista jest pusta! Czy scena Shop za³adowa³a siê poprawnie?");
			// Próba ratunkowa: spróbuj wygenerowaæ teraz
			if (BoardManager.Instance != null) GenerateInventory();
			if (inventoryTiles.Count == 0) return false;
		}

		foreach (var tileGO in inventoryTiles)
		{
			if (tileGO == null) continue;
			Tile tile = tileGO.GetComponent<Tile>();

			if (!tile.isOccupied)
			{
				SpawnPiece(prefab, tile, type);
				return true;
			}
		}

		Debug.Log("Ekwipunek pe³ny!");
		return false;
	}

	void SpawnPiece(GameObject prefab, Tile tile, PieceType type)
	{
		GameObject pieceGO = Instantiate(prefab, tile.transform.position, Quaternion.identity);

		// Fix dla NetworkObject (DestroyImmediate jest wymagane, ¿eby unikn¹æ b³êdów parentowania)
		if (pieceGO.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
		{
			DestroyImmediate(netObj);
		}

		pieceGO.transform.parent = tile.transform;

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