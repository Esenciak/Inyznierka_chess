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
	public GameObject[] piecePrefabs; // 0:Pawn, 1:Knight, 2:Bishop, 3:Rook, 4:Queen

	[Header("UI - Teksty")]
	public TextMeshProUGUI coinsText;
	public TextMeshProUGUI centerBoardSizeText; // NOWE: Wyœwietlanie rozmiaru planszy
	public TextMeshProUGUI roundText;           // NOWE: Wyœwietlanie rundy

	private List<GameObject> shopTiles = new List<GameObject>();

	// Cennik
	private Dictionary<PieceType, int> prices = new Dictionary<PieceType, int>()
	{
		{ PieceType.Pawn, 10 },
		{ PieceType.Knight, 30 },
		{ PieceType.Bishop, 30 },
		{ PieceType.Rook, 50 },
		{ PieceType.queen, 100 }
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

				// Kolor pó³ki sklepowej
				tile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.5f, 0.2f);
				shopTiles.Add(tile);
			}
		}
	}

	public void RefillShop()
	{
		foreach (var tileGO in shopTiles)
		{
			Tile tile = tileGO.GetComponent<Tile>();
			if (tile.isOccupied && tile.currentPiece != null)
			{
				Destroy(tile.currentPiece.gameObject);
				tile.isOccupied = false;
				tile.currentPiece = null;
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

		// Usuwamy logikê gry, dodajemy logikê sklepu
		Destroy(itemGO.GetComponent<Piece>());
		ShopItem shopItem = itemGO.AddComponent<ShopItem>();

		// Przekazujemy referencjê do Managera, ¿eby ShopItem wiedzia³ kogo zawo³aæ przy klikniêciu
		shopItem.Setup(type, prices[type], this);

		Tile tile = tileGO.GetComponent<Tile>();
		tile.isOccupied = true;
	}

	// Ta metoda jest wywo³ywana przez klikniêcie w ShopItem (figurê)
	public void TryBuyPiece(ShopItem item)
	{
		if (GameProgress.Instance.SpendCoins(item.price))
		{
			Debug.Log($"Kupiono {item.type}!");
			InventoryManager.Instance.AddPieceToInventory(item.type, GetPrefabByType(item.type));
			Destroy(item.gameObject);
			UpdateUI(); // Odœwie¿amy kasê po zakupie
		}
		else
		{
			Debug.Log("Za ma³o kasy!");
		}
	}

	public void StartGame()
	{
		SaveArmyConfig(); // Zapisz armiê
		GameProgress.Instance.LoadScene("Battle"); // Za³aduj bitwê
	}

	void UpdateUI()
	{
		// 1. Aktualizacja Monet
		if (coinsText != null)
			coinsText.text = "Coins: " + GameProgress.Instance.coins;

		// 2. Aktualizacja Rozmiaru Planszy
		if (centerBoardSizeText != null)
		{
			int size = GameProgress.Instance.centerBoardSize;
			centerBoardSizeText.text = $"Board Size: {size}x{size}";
		}

		// 3. Aktualizacja Rundy (GamesPlayed + 1, ¿eby zaczynaæ od Rundy 1)
		if (roundText != null)
		{
			int currentRound = GameProgress.Instance.gamesPlayed + 1;
			roundText.text = "Round: " + currentRound;
		}
	}

	// --- Helpery ---
	PieceType GetRandomPieceType()
	{
		int rand = Random.Range(0, 100);
		if (rand < 40) return PieceType.Pawn;
		if (rand < 65) return PieceType.Knight;
		if (rand < 90) return PieceType.Bishop;
		if (rand < 98) return PieceType.Rook;
		return PieceType.queen;
	}

	GameObject GetPrefabByType(PieceType type)
	{
		switch (type)
		{
			case PieceType.Pawn: return piecePrefabs[0];
			case PieceType.Knight: return piecePrefabs[1];
			case PieceType.Bishop: return piecePrefabs[2];
			case PieceType.Rook: return piecePrefabs[3];
			case PieceType.queen: return piecePrefabs[4];
		}
		return piecePrefabs[0];
	}

	void SaveArmyConfig()
	{
		// 1. Wyczyœæ star¹ pamiêæ
		GameProgress.Instance.savedArmy.Clear();

		// 2. Pobierz rozmiar planszy gracza
		int rows = BoardManager.Instance.PlayerRows;
		int cols = BoardManager.Instance.PlayerCols;

		// 3. Przeszukaj ka¿dy kafelek na planszy gracza
		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Tile tile = BoardManager.Instance.GetTile(BoardType.Player, r, c);

				// Jeœli na kafelku jest figura
				if (tile != null && tile.isOccupied && tile.currentPiece != null)
				{
					SavedPiece data = new SavedPiece();
					data.type = tile.currentPiece.pieceType;
					data.row = r;
					data.col = c;

					GameProgress.Instance.savedArmy.Add(data);
				}
			}
		}
		Debug.Log($"Zapisano {GameProgress.Instance.savedArmy.Count} figur do bitwy.");
	}
}