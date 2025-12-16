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

	// WA¯NE: Ustaw w Unity dok³adnie w tej kolejnoœci (wg Twojego Enuma):
	// 0:Pawn, 1:King, 2:Queen, 3:Rook, 4:Bishop, 5:Knight
	public GameObject[] piecePrefabs;

	[Header("UI - Teksty")]
	public TextMeshProUGUI coinsText;
	public TextMeshProUGUI centerBoardSizeText; // PRZYWRÓCONE
	public TextMeshProUGUI roundText;           // PRZYWRÓCONE

	private List<GameObject> shopTiles = new List<GameObject>();

	// Cennik (Zaktualizowany do Enuma)
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

		Destroy(itemGO.GetComponent<Piece>());
		ShopItem shopItem = itemGO.AddComponent<ShopItem>();
		shopItem.Setup(type, prices[type], this);

		Tile tile = tileGO.GetComponent<Tile>();
		tile.isOccupied = true;
	}

	public void TryBuyPiece(ShopItem item)
	{
		if (GameProgress.Instance.SpendCoins(item.price))
		{
			InventoryManager.Instance.AddPieceToInventory(item.type, GetPrefabByType(item.type));
			Destroy(item.gameObject);
			UpdateUI();
		}
	}

	public void StartGame()
	{
		SaveBoardLayout(); // Zapisz armiê
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
					data.x = c;
					data.y = r;
					GameProgress.Instance.myArmy.Add(data);
				}
			}
		}
		Debug.Log($"Zapisano {GameProgress.Instance.myArmy.Count} figur.");
	}

	void UpdateUI()
	{
		// 1. Monety
		if (coinsText != null)
			coinsText.text = "Coins: " + GameProgress.Instance.coins;

		// 2. Rozmiar Planszy (PRZYWRÓCONE)
		if (centerBoardSizeText != null)
		{
			int size = GameProgress.Instance.centerBoardSize;
			centerBoardSizeText.text = $"Board Size: {size}x{size}";
		}

		// 3. Runda (PRZYWRÓCONE)
		if (roundText != null)
		{
			int currentRound = GameProgress.Instance.gamesPlayed + 1;
			roundText.text = "Round: " + currentRound;
		}
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
		// *** KOLEJNOŒÆ WG TWOJEGO ENUMA ***
		// 0:Pawn, 1:King, 2:Queen, 3:Rook, 4:Bishop, 5:Knight
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