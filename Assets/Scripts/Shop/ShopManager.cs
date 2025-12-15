using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ShopManager : MonoBehaviour
{
	[Header("Ustawienia Sklepu")]
	public int shopRows = 2;
	public int shopCols = 3;
	public Vector2 shopOffset = new Vector2(-5, 0); // Gdzie ma staæ sklep (np. po lewej)

	[Header("Prefabrykaty")]
	public GameObject tilePrefab; // Kafelek pó³ki sklepowej
	public GameObject[] piecePrefabs; // Przypisz tu prefaby: Pawn, Knight, Bishop, Rook, Queen (w tej kolejnoœci!)

	[Header("UI")]
	public TextMeshProUGUI coinsText;

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
		// Generujemy "pó³kê" (siatkê kafelków)
		for (int r = 0; r < shopRows; r++)
		{
			for (int c = 0; c < shopCols; c++)
			{
				Vector3 pos = new Vector3(shopOffset.x + c, shopOffset.y + r, 0);
				GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
				tile.transform.parent = transform;

				// Ustawiamy kolor "sklepowy" np. z³oty/¿ó³ty
				tile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.5f, 0.2f);
				shopTiles.Add(tile);
			}
		}
	}

	public void RefillShop()
	{
		// Czyœcimy stare oferty (jeœli s¹)
		foreach (var tileGO in shopTiles)
		{
			Tile tile = tileGO.GetComponent<Tile>();
			if (tile.isOccupied && tile.currentPiece != null)
			{
				Destroy(tile.currentPiece.gameObject);
				tile.isOccupied = false;
				tile.currentPiece = null;
			}

			// Losujemy now¹ figurê na ten kafelek
			SpawnRandomShopItem(tileGO);
		}
	}

	void SpawnRandomShopItem(GameObject tileGO)
	{
		// Losowanie typu (prosta waga: wiêcej pionków ni¿ hetmanów)
		PieceType type = GetRandomPieceType();
		GameObject prefab = GetPrefabByType(type);

		if (prefab == null) return;

		// Tworzymy wizualizacjê figury
		Vector3 pos = tileGO.transform.position;
		pos.z = -1; // Na wierzchu
		GameObject itemGO = Instantiate(prefab, pos, Quaternion.identity);

		// WA¯NE: Usuwamy komponent Piece (¿eby nie da³o siê ni¹ graæ) 
		// a dodajemy ShopItem (¿eby da³o siê kupiæ)
		Destroy(itemGO.GetComponent<Piece>());

		ShopItem shopItem = itemGO.AddComponent<ShopItem>();
		// Opcjonalnie: Dodaj TextMeshPro do wyœwietlania ceny dynamicznie

		shopItem.Setup(type, prices[type], this);

		// Oznaczamy kafelek (technicznie)
		Tile tile = tileGO.GetComponent<Tile>();
		tile.isOccupied = true;
		// tile.currentPiece = ... nie ustawiamy Piece, bo to ShopItem
	}

	public void TryBuyPiece(ShopItem item)
	{
		if (GameProgress.Instance.SpendCoins(item.price))
		{
			Debug.Log($"Kupiono {item.type}!");

			// 1. Dodaj do Ekwipunku gracza
			InventoryManager.Instance.AddPieceToInventory(item.type, GetPrefabByType(item.type));

			// 2. Usuñ ze sklepu
			Destroy(item.gameObject);
			UpdateUI();
		}
		else
		{
			Debug.Log("Za ma³o kasy!");
		}
	}

	public void StartGame()
	{
		// £aduje scenê walki
		GameProgress.Instance.LoadScene("Battle");
	}

	void UpdateUI()
	{
		if (coinsText != null)
			coinsText.text = "Coins: " + GameProgress.Instance.coins;
	}

	// --- Helpery ---

	PieceType GetRandomPieceType()
	{
		int rand = Random.Range(0, 100);
		if (rand < 40) return PieceType.Pawn;   // 40%
		if (rand < 65) return PieceType.Knight; // 25%
		if (rand < 90) return PieceType.Bishop; // 25%
		if (rand < 98) return PieceType.Rook;   // 8%
		return PieceType.queen;                 // 2%
	}

	GameObject GetPrefabByType(PieceType type)
	{
		// Zak³adam kolejnoœæ w tablicy inspektora: 0:Pawn, 1:Knight, 2:Bishop, 3:Rook, 4:Queen
		// Dostosuj indeksy do tego jak przeci¹gniesz w Unity!
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
}