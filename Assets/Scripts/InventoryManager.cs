using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
	public static InventoryManager Instance { get; private set; }

	[Header("Ustawienia Ekwipunku")]
	public int rows = 5;
	public int cols = 2;
	public GameObject tilePrefab;
	public GameObject kingPrefab; // Król (przypisz w Inspectorze!)
	public Vector2 inventoryOffset = new Vector2(0, 5);

	public Color inventoryColor1 = new Color(0.3f, 0.3f, 0.3f);
	public Color inventoryColor2 = new Color(0.4f, 0.4f, 0.4f);

	// Lista kafelków (zamiast tablicy, ³atwiej czyœciæ)
	private List<GameObject> inventoryTiles = new List<GameObject>();

	private void Awake() => Instance = this;

	private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
	private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// Generujemy inventory TYLKO w sklepie
		if (scene.name == "Shop")
		{
			InitializeInventory();
		}
		else
		{
			// W Menu i Bitwie czyœcimy (lub zostawiamy w bitwie, zale¿nie od preferencji)
			// Tutaj: czyœcimy w Menu
			if (scene.name == "MainMenu") ClearInventory();
		}
	}

	private void Start()
	{
		if (SceneManager.GetActiveScene().name == "Shop")
		{
			InitializeInventory();
		}
	}

	void InitializeInventory()
	{
		ClearInventory();
		GenerateInventory();
		// OpóŸnienie, ¿eby BoardManager zd¹¿y³ ustawiæ planszê zanim postawimy Króla
		Invoke("SpawnKingOnBoard", 0.1f);
	}

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
		// Pozycja inventory wzglêdem planszy gracza
		float startX = BoardManager.Instance.playerOffset.x + BoardManager.Instance.PlayerCols + inventoryOffset.x;
		float startY = BoardManager.Instance.playerOffset.y + inventoryOffset.y;

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector3 pos = new Vector3(startX + c, startY + r, 0);
				GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity);
				go.name = $"Inv_Tile_{r}_{c}";
				go.transform.parent = transform; // Przypisz do Managera

				Tile tile = go.GetComponent<Tile>();
				tile.isInventory = true;
				tile.boardType = BoardType.Player;

				SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
				if (sr != null) sr.color = (r + c) % 2 == 0 ? inventoryColor1 : inventoryColor2;

				inventoryTiles.Add(go);
			}
		}
	}

	void SpawnKingOnBoard()
	{
		// Stawiamy Króla na œrodku planszy gracza
		Tile centerTile = BoardManager.Instance.GetPlayerCenterTile();
		if (centerTile != null && kingPrefab != null)
		{
			if (!centerTile.isOccupied)
			{
				SpawnPiece(kingPrefab, centerTile, PieceType.King);
			}
		}
	}

	public void AddPieceToInventory(PieceType type, GameObject prefab)
	{
		foreach (var tileGO in inventoryTiles)
		{
			if (tileGO == null) continue;
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
		// 1. Tworzymy obiekt
		GameObject pieceGO = Instantiate(prefab, tile.transform.position, Quaternion.identity);

		// 2. KLUCZOWA POPRAWKA: Usuwamy NetworkObject PRZED ustawieniem rodzica
		if (pieceGO.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
		{
			DestroyImmediate(netObj);
		}

		// 3. Ustawiamy rodzica
		pieceGO.transform.parent = tile.transform;

		// 4. Reszta logiki
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