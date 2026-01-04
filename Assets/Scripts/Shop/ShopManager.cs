using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ShopManager : MonoBehaviour
{
	[Header("Ustawienia Sklepu")]
	public int shopRows = 2;
	public int shopCols = 3;
	public Vector2 shopOffset = new Vector2(-5, 0);

	[Header("Prefabrykaty")]
	public GameObject tilePrefab;

	// WA¯NE: Dodaj tu pusty obiekt z komponentem TextMeshPro (nie UI!)
	public GameObject priceTextPrefab;

	// 0:Pawn, 1:King, 2:Queen, 3:Rook, 4:Bishop, 5:Knight
	public GameObject[] piecePrefabs;

	[Header("UI - Teksty")]
	public TextMeshProUGUI coinsText;
	public TextMeshProUGUI centerBoardSizeText;
	public TextMeshProUGUI roundText;

	private List<GameObject> shopTiles = new List<GameObject>();

	private Dictionary<PieceType, int> prices = new Dictionary<PieceType, int>()
	{
		{ PieceType.Pawn, 10 },
		{ PieceType.King, 0 },
		{ PieceType.queen, 100 },
		{ PieceType.Rook, 50 },
		{ PieceType.Bishop, 30 },
		{ PieceType.Knight, 30 }
	};

	private void Start()
	{
		GenerateShopGrid();
		RefillShop();
		UpdateUI();
	}

	void GenerateShopGrid()
	{
		for (int r = 0; r < shopRows; r++)
		{
			for (int c = 0; c < shopCols; c++)
			{
				Vector3 pos = new Vector3(shopOffset.x + c, shopOffset.y + r, 0);
				GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
				tile.transform.parent = null;

				// Ustawiamy kolor "pó³ki" (br¹zowy/drewniany)
				tile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.5f, 0.2f);

				// Zapisujemy informacjê o wierszu w kafelku (przydatne do logiki cen)
				Tile t = tile.GetComponent<Tile>();
				t.row = r;

				shopTiles.Add(tile);
			}
		}
	}

	public void RefillShop()
	{
		foreach (var tileGO in shopTiles)
		{
			Tile tile = tileGO.GetComponent<Tile>();
			if (tile.isOccupied && tile.currentPiece != null) // Tutaj currentPiece nie bêdzie u¿ywane przez ShopItem, ale czyœcimy
			{
				// Czyœcimy stare obiekty (ShopItem nie jest Piece, wiêc szukamy dzieci lub komponentów)
				// W tym systemie ShopItem jest na pozycji kafelka. U¿yjmy prostej metody:
				// Szukamy obiektów w promieniu kafelka, które maj¹ ShopItem
				ShopItem existingItem = tileGO.GetComponentInChildren<ShopItem>();
				if (existingItem == null)
				{
					// Alternatywnie: Raycast w miejscu kafelka
					RaycastHit2D hit = Physics2D.Raycast(tileGO.transform.position, Vector2.zero);
					if (hit.collider != null) existingItem = hit.collider.GetComponent<ShopItem>();
				}

				if (existingItem != null) Destroy(existingItem.gameObject);

				tile.isOccupied = false;
			}
			SpawnRandomShopItem(tileGO);
		}
	}

	void SpawnRandomShopItem(GameObject tileGO)
	{
		PieceType type = GetRandomPieceType();
		GameObject prefab = GetPrefabByType(type);

		if (prefab == null) return;

		Vector3 pos = tileGO.transform.position;
		pos.z = -1;
		GameObject itemGO = Instantiate(prefab, pos, Quaternion.identity);

		// 1. USUWANIE LOGIKI GRY I RUCHU (NAPRAWA BUGU Z PRZECI¥GANIEM)
		Destroy(itemGO.GetComponent<Piece>());
		Destroy(itemGO.GetComponent<PieceMovement>()); // <--- TO ZABLOKUJE PRZECI¥GANIE

		// 2. DODANIE LOGIKI SKLEPU
		ShopItem shopItem = itemGO.AddComponent<ShopItem>();

		Tile tile = tileGO.GetComponent<Tile>();

		// Obliczamy pozycjê ceny: Jeœli rz¹d 0 (dó³) -> cena pod (-1.2), Jeœli rz¹d 1 (góra) -> cena nad (+1.2)
		Vector3 textOffset = (tile.row == 0) ? new Vector3(0, -1.2f, 0) : new Vector3(0, 1.2f, 0);

		shopItem.Setup(type, prices[type], this, tile, priceTextPrefab, textOffset);

		tile.isOccupied = true;
	}

	public void TryBuyPiece(ShopItem item)
	{
		if (GameProgress.Instance.SpendCoins(item.price))
		{
			InventoryManager.Instance.AddPieceToInventory(item.type, GetPrefabByType(item.type));
			Destroy(item.gameObject); // To przywróci kolor kafelka (OnDestroy w ShopItem)
			UpdateUI();
		}
	}

	// --- Reszta metod (StartGame, SaveBoardLayout, UpdateUI...) bez zmian ---
	// (Wklej tu metody z poprzedniej wersji ShopManager.cs)

	public void StartGame()
	{
		SaveBoardLayout();
		GameProgress.Instance.LoadScene("Battle");
	}

	void SaveBoardLayout()
	{
		GameProgress.Instance.myArmy.Clear();
		int rows = BoardManager.Instance.PlayerRows;
		int cols = BoardManager.Instance.PlayerCols;
		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Tile tile = BoardManager.Instance.GetTile(BoardType.Player, r, c);
				if (tile != null && tile.isOccupied && tile.currentPiece != null)
				{
					SavedPieceData data = new SavedPieceData();
					data.type = tile.currentPiece.pieceType;
					data.x = c; data.y = r;
					GameProgress.Instance.myArmy.Add(data);
				}
			}
		}
	}

	void UpdateUI()
	{
		if (coinsText != null) coinsText.text = "Coins: " + GameProgress.Instance.coins;
		if (centerBoardSizeText != null) centerBoardSizeText.text = $"Board Size: {GameProgress.Instance.centerBoardSize}x{GameProgress.Instance.centerBoardSize}";
		if (roundText != null) roundText.text = "Round: " + (GameProgress.Instance.gamesPlayed + 1);
	}

	PieceType GetRandomPieceType()
	{
		int rand = Random.Range(0, 100);
		if (rand < 40) return PieceType.Pawn;
		if (rand < 60) return PieceType.Knight;
		if (rand < 80) return PieceType.Bishop;
		if (rand < 95) return PieceType.Rook;
		return PieceType.queen;
	}

	GameObject GetPrefabByType(PieceType type)
	{
		switch (type)
		{
			case PieceType.Pawn: return piecePrefabs[0];
			case PieceType.King: return piecePrefabs[1];
			case PieceType.queen: return piecePrefabs[2];
			case PieceType.Rook: return piecePrefabs[3];
			case PieceType.Bishop: return piecePrefabs[4];
			case PieceType.Knight: return piecePrefabs[5];
		}
		return piecePrefabs[0];
	}
}