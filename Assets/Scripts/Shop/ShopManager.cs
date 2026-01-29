using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
	    private bool shopInitialized = false;
    
	    [Header("Ustawienia Sklepu")]
        public int shopRows = 2;
        public int shopCols = 3;
        public Vector2 shopOffset = new Vector2(-5, 0);

        [Header("Prefabrykaty")]
        public GameObject tilePrefab;
        public GameObject priceTextPrefab;


        public GameObject[] piecePrefabs;
        public GameObject[] whitePiecePrefabs;
        public GameObject[] blackPiecePrefabs;

        [Header("UI - Teksty")]
        public TextMeshProUGUI coinsText;
        public TextMeshProUGUI centerBoardSizeText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI enemyNameText;
        public Button startButton;
        public Button rerollButton;

        [Header("Ekonomia")]
        public EconomyConfig economyConfig;

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

	    private List<string> currentOfferPieces = new List<string>();
	    private int offerIndexThisRound = 0;
	    private void OnEnable()
        {
                SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

                if (scene.name == "Shop")
                {
                        FindUIReferences();
                        InitializeShop();
                }
                else
                {

                        CleanupShop();
                }
        }

        private void Start()
        {

                if (SceneManager.GetActiveScene().name == "Shop")
                {
                        FindUIReferences();
                        InitializeShop();
                }
                else
                {
                        ToggleUI(false);
                }
        }

        void FindUIReferences()
        {


                GameObject coinObj = GameObject.Find("UI_Coins");
                if (coinObj) coinsText = coinObj.GetComponent<TextMeshProUGUI>();

                GameObject roundObj = GameObject.Find("UI_Round");
                if (roundObj) roundText = roundObj.GetComponent<TextMeshProUGUI>();

                GameObject playerNameObj = GameObject.Find("UI_Player_Name");
                if (playerNameObj) playerNameText = playerNameObj.GetComponent<TextMeshProUGUI>();

                GameObject enemyNameObj = GameObject.Find("UI_enemy_name");
                if (enemyNameObj) enemyNameText = enemyNameObj.GetComponent<TextMeshProUGUI>();

                GameObject sizeObj = GameObject.Find("UI_BoardSize");
                if (sizeObj) centerBoardSizeText = sizeObj.GetComponent<TextMeshProUGUI>();

                GameObject btnObj = GameObject.Find("StartButton");
                if (btnObj != null)
                {
                        startButton = btnObj.GetComponent<Button>();

                        startButton.onClick.RemoveAllListeners();

                        startButton.onClick.AddListener(StartGame);
                }

                GameObject rerollObj = GameObject.Find("RerollButton");
                if (rerollObj != null)
                {
                        rerollButton = rerollObj.GetComponent<Button>();
                }
                else
                {
                        rerollButton = CreateRerollButton();
                }

                if (rerollButton != null)
                {
                        rerollButton.onClick.RemoveAllListeners();
                        rerollButton.onClick.AddListener(TryRerollShop);
                        PositionRerollButton(rerollButton);
                }

                ToggleUI(true);
        }

        void InitializeShop()
        {
		    if (TelemetryService.Instance != null && !string.IsNullOrWhiteSpace(LobbyState.CurrentLobbyId))
	        {
			    TelemetryService.Instance.SetMatchContext(LobbyState.CurrentLobbyId);
	    	}


		        if (shopInitialized) return;
                shopInitialized = true;

        		ApplyPiecePrefabsForLocalPlayer();
                CleanupShop();
                GenerateShopGrid();
                if (GameProgress.Instance != null && TelemetryService.Instance != null)
                {
                        int roundNumber = GameProgress.Instance.gamesPlayed + 1;
                        TelemetryService.Instance.StartRound(roundNumber, GameProgress.Instance.coins);
                }
                RefillShop();
                ToggleUI(true);
                UpdateUI();
                if (InventoryManager.Instance != null)
                {
                        InventoryManager.Instance.EnsureInitialized();
                }
                StartCoroutine(RestoreLayoutRoutine());
        }

        void CleanupShop()
        {
		        shopInitialized = false;
		        foreach (var tile in shopTiles)
                {
                        if (tile != null) Destroy(tile);
                }
                shopTiles.Clear();
                ToggleUI(false);
        }

        void ToggleUI(bool state)
        {
                if (coinsText) coinsText.gameObject.SetActive(state);
                if (centerBoardSizeText) centerBoardSizeText.gameObject.SetActive(state);
                if (roundText) roundText.gameObject.SetActive(state);
                if (playerNameText) playerNameText.gameObject.SetActive(state);
                if (enemyNameText) enemyNameText.gameObject.SetActive(state);
                if (rerollButton) rerollButton.gameObject.SetActive(state);
        }


        void GenerateShopGrid()
        {
                for (int r = 0; r < shopRows; r++)
                {
                        for (int c = 0; c < shopCols; c++)
                        {
                                Vector3 pos = new Vector3(shopOffset.x + c, shopOffset.y + r, 0);
                                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
                                tile.transform.parent = transform;

                                tile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.5f, 0.2f);
                                Tile t = tile.GetComponent<Tile>();
                                t.row = r;
                                t.boardType = BoardType.Center;
                                t.isInventory = false;

                                shopTiles.Add(tile);
                        }
                }
        }


	    public void RefillShop()
	    {
		    currentOfferPieces.Clear();

		    for (int i = 0; i < shopTiles.Count; i++)
		    {
			    var tileGO = shopTiles[i];
			    if (tileGO == null)
			    {
				    currentOfferPieces.Add("Empty");
				    continue;
			    }

			    // usuń stary item
			    ShopItem existingItem = tileGO.GetComponentInChildren<ShopItem>();
			    if (existingItem != null) Destroy(existingItem.gameObject);

			    Tile tile = tileGO.GetComponent<Tile>();
			    tile.isOccupied = false;

			    // spawn i zapisz do oferty w tej samej kolejności co sloty
			    PieceType spawned = SpawnRandomShopItem(tileGO);
			    string typeStr = (spawned == PieceType.King) ? "Empty" : TelemetryService.ToTelemetryPieceType(spawned);
			    currentOfferPieces.Add(typeStr);
		    }

		    // 1 log na ofertę
		    LogShopOfferFromCurrent();
	    }


	    PieceType SpawnRandomShopItem(GameObject tileGO)
        {
                PieceType type = GetRandomPieceType();
                GameObject prefab = GetPrefabByType(type);

                if (prefab == null) return type;

                Vector3 pos = tileGO.transform.position;
                pos.z = -1;


                GameObject itemGO = Instantiate(prefab, pos, Quaternion.identity);



                if (itemGO.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
                {
                        DestroyImmediate(netObj);
                }


                itemGO.transform.parent = tileGO.transform;


                Destroy(itemGO.GetComponent<Piece>());
                Destroy(itemGO.GetComponent<PieceMovement>());


                ShopItem shopItem = itemGO.AddComponent<ShopItem>();
                Tile tile = tileGO.GetComponent<Tile>();

                Vector3 textOffset = (tile.row == 0) ? new Vector3(0, -1.2f, 0) : new Vector3(0, 1.2f, 0);

                shopItem.Setup(type, GetPrice(type), this, tile, priceTextPrefab, textOffset);
                EnsureShopItemCollider(itemGO);

                tile.isOccupied = true;
        		return type;

	    }
	private void LogShopOfferFromCurrent()
	{
		if (TelemetryService.Instance == null || economyConfig == null)
			return;

		int slots = shopRows * shopCols;

		// gwarancja długości
		while (currentOfferPieces.Count < slots) currentOfferPieces.Add("Empty");
		if (currentOfferPieces.Count > slots) currentOfferPieces.RemoveRange(slots, currentOfferPieces.Count - slots);

		TelemetryService.Instance.LogShopOfferGenerated(currentOfferPieces, slots, economyConfig.rerollCost);
	}


	private void EnsureShopItemCollider(GameObject itemGO)
        {
                if (itemGO == null || itemGO.GetComponent<Collider2D>() != null)
                {
                        return;
                }

                BoxCollider2D collider = itemGO.AddComponent<BoxCollider2D>();
                SpriteRenderer renderer = itemGO.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                        collider.size = renderer.sprite.bounds.size;
                        collider.offset = renderer.sprite.bounds.center;
                }
        }

	public void TryBuyPiece(ShopItem item)
	{
		InventoryManager inventory = InventoryManager.Instance ?? FindObjectOfType<InventoryManager>();
		if (inventory == null)
		{
			Debug.Log("Nie kupiono: brak InventoryManager.");
			return;
		}

		if (!inventory.IsReady)
		{
			inventory.EnsureInitialized();
			Debug.Log("Nie kupiono: ekwipunek jeszcze się ładuje.");
			return;
		}

		if (GameProgress.Instance.coins < item.price)
		{
			Debug.Log("Nie stać Cię!");
			return;
		}

		int coinsBefore = GameProgress.Instance.coins;
		bool success = inventory.AddPieceToInventory(item.type, GetPrefabByType(item.type));

		if (!success)
		{
			Debug.Log("Nie kupiono: Brak miejsca w ekwipunku!");
			return;
		}

		// kasa
		GameProgress.Instance.SpendCoins(item.price);
		int coinsAfter = GameProgress.Instance.coins;

		// telemetry (NIE BLOKUJE logiki sklepu)
		if (TelemetryService.Instance != null && item.type != PieceType.King)
		{
			int slotIndex = GetShopSlotIndex(item.CurrentTile);

			string bought = TelemetryService.ToTelemetryPieceType(item.type);
			if (slotIndex >= 0 && slotIndex < currentOfferPieces.Count)
			{
				string offered = currentOfferPieces[slotIndex];
				if (offered != bought)
					Debug.LogWarning($"[Shop] Telemetry mismatch slot {slotIndex}: offer={offered}, buy={bought}");
			}
			else
			{
				Debug.LogWarning($"[Shop] Invalid slotIndex={slotIndex}, offerCount={currentOfferPieces.Count}");
			}

			TelemetryService.Instance.LogPurchase(bought, item.price, slotIndex, coinsBefore, coinsAfter);
		}

		// usuń z tile + zniszcz
		if (item.CurrentTile != null)
		{
			item.CurrentTile.isOccupied = false;
			item.CurrentTile.currentPiece = null;
		}

		Destroy(item.gameObject);
		UpdateUI();
	}



	public void StartGame()
        {
                SaveBoardLayout();
                SaveInventoryLayout();
                if (TelemetryService.Instance != null && GameProgress.Instance != null)
                {
                        TelemetryService.Instance.RecordCoinsAfterShop(GameProgress.Instance.coins);
                }

                if (GameManager.Instance.isMultiplayer)
                {
                        StartCoroutine(WaitForBattleSessionAndReady());
                }
                else
                {
                        Debug.Log("Singleplayer: Start bitwy!");
                        GameProgress.Instance.LoadScene("Battle");
                }
        }

        private System.Collections.IEnumerator WaitForBattleSessionAndReady()
        {
                Debug.Log("Multiplayer: Zgłaszam gotowość do serwera...");
                yield return new WaitUntil(() => BattleSession.Instance != null);
                BattleSession.Instance.PlayerReady(GameProgress.Instance.myArmy);
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

                EnsureKingInArmy();
        }

        void SaveInventoryLayout()
        {
                if (GameProgress.Instance == null)
                {
                        return;
                }

                if (InventoryManager.Instance != null)
                {
                        InventoryManager.Instance.SaveInventoryLayout(GameProgress.Instance.inventoryPieces);
                }
                else
                {
                        GameProgress.Instance.inventoryPieces.Clear();
                }
        }

        System.Collections.IEnumerator RestoreLayoutRoutine()
        {
                while (BoardManager.Instance == null || !BoardManager.Instance.IsReady)
                {
                        yield return null;
                }

                while (InventoryManager.Instance == null || !InventoryManager.Instance.IsReady)
                {
                        yield return null;
                }

                RestoreBoardLayout();
                RestoreInventoryLayout();
        }

        void RestoreBoardLayout()
        {
                if (GameProgress.Instance == null || GameProgress.Instance.myArmy.Count == 0)
                {
                        return;
                }

                ClearPlayerBoardPieces();

                foreach (SavedPieceData data in GameProgress.Instance.myArmy)
                {
                        Tile tile = BoardManager.Instance.GetTile(BoardType.Player, data.y, data.x);
                        if (tile == null || tile.isOccupied) continue;

                        SpawnPieceOnTile(GetPrefabByType(data.type), tile, data.type);
                }
        }

        void EnsureKingInArmy()
        {
                if (GameProgress.Instance == null)
                {
                        return;
                }

                foreach (SavedPieceData data in GameProgress.Instance.myArmy)
                {
                        if (data.type == PieceType.King)
                        {
                                return;
                        }
                }

                if (BoardManager.Instance == null)
                {
                        return;
                }

                Vector2Int coords = FindFallbackKingCoords();
                GameProgress.Instance.myArmy.Add(new SavedPieceData
                {
                        type = PieceType.King,
                        x = coords.x,
                        y = coords.y
                });
        }

        Vector2Int FindFallbackKingCoords()
        {
                int rows = BoardManager.Instance.PlayerRows;
                int cols = BoardManager.Instance.PlayerCols;
                int centerRow = rows / 2;
                int centerCol = cols / 2;
                Tile centerTile = BoardManager.Instance.GetTile(BoardType.Player, centerRow, centerCol);
                if (centerTile != null && !centerTile.isOccupied)
                {
                        return new Vector2Int(centerCol, centerRow);
                }

                for (int r = 0; r < rows; r++)
                {
                        for (int c = 0; c < cols; c++)
                        {
                                Tile tile = BoardManager.Instance.GetTile(BoardType.Player, r, c);
                                if (tile != null && !tile.isOccupied)
                                {
                                        return new Vector2Int(c, r);
                                }
                        }
                }

                return new Vector2Int(centerCol, centerRow);
        }

        void RestoreInventoryLayout()
        {
                if (GameProgress.Instance == null || GameProgress.Instance.inventoryPieces.Count == 0)
                {
                        return;
                }

                if (InventoryManager.Instance == null)
                {
                        return;
                }

                foreach (SavedInventoryData data in GameProgress.Instance.inventoryPieces)
                {
                        GameObject prefab = GetPrefabByType(data.type);
                        if (prefab == null) continue;
                        InventoryManager.Instance.TryPlaceInventoryPiece(data.type, prefab, data.row, data.col);
                }
        }

        void ClearPlayerBoardPieces()
        {
                if (BoardManager.Instance == null) return;

                int rows = BoardManager.Instance.PlayerRows;
                int cols = BoardManager.Instance.PlayerCols;
                for (int r = 0; r < rows; r++)
                {
                        for (int c = 0; c < cols; c++)
                        {
                                Tile tile = BoardManager.Instance.GetTile(BoardType.Player, r, c);
                                if (tile == null) continue;
                                if (tile.currentPiece != null)
                                {
                                        Destroy(tile.currentPiece.gameObject);
                                }
                                tile.isOccupied = false;
                                tile.currentPiece = null;
                        }
                }
        }

        void SpawnPieceOnTile(GameObject prefab, Tile tile, PieceType type)
        {
                if (prefab == null || tile == null) return;

                GameObject pieceGO = Instantiate(prefab, tile.transform.position, Quaternion.identity);
                if (pieceGO.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
                {
                        DestroyImmediate(netObj);
                }

                pieceGO.transform.parent = tile.transform;

                if (pieceGO.GetComponent<PieceMovement>() == null)
                {
                        pieceGO.AddComponent<PieceMovement>();
                }

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

        void UpdateUI()
        {
                if (coinsText != null) coinsText.text = "Coins: " + GameProgress.Instance.coins;
                if (centerBoardSizeText != null) centerBoardSizeText.text = $"Board: {GameProgress.Instance.centerBoardSize}x{GameProgress.Instance.centerBoardSize}";
                if (roundText != null)
                {
                        roundText.text = $"Runda {GameProgress.Instance.gamesPlayed + 1}";
                }
                if (playerNameText != null || enemyNameText != null)
                {
                        string opponentName = LobbyState.OpponentPlayerName;
                        string localName = LobbyState.LocalPlayerName;
                        int wins = GameProgress.Instance.wins;
                        int losses = GameProgress.Instance.losses;
                        if (playerNameText != null)
                        {
                                playerNameText.text = $"{localName}: {wins}";
                        }
                        if (enemyNameText != null)
                        {
                                enemyNameText.text = $"{opponentName}: {losses}";
                        }
                }
                if (rerollButton != null)
                {
                        int cost = economyConfig != null ? economyConfig.rerollCost : 0;
                        TextMeshProUGUI label = rerollButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (label != null)
                        {
                                label.text = $"Losuj za: {cost} C";
                        }
                }
        }

	PieceType GetRandomPieceType()
	{
		int round = GameProgress.Instance != null ? GameProgress.Instance.gamesPlayed + 1 : 1;

		if (economyConfig != null && GameProgress.Instance != null
			&& economyConfig.TryGetSpawnWeights(round, out var weights))
		{
			int pawnW = Mathf.Max(0, weights.pawnWeight);
			int knightW = Mathf.Max(0, weights.knightWeight);
			int bishopW = Mathf.Max(0, weights.bishopWeight);

			// KING zawsze wyłączony
			int kingW = 0;

			// unlock
			int rookW = (economyConfig != null && round >= economyConfig.rookUnlockRound) ? Mathf.Max(0, weights.rookWeight) : 0;
			int queenW = (economyConfig != null && round >= economyConfig.queenUnlockRound) ? Mathf.Max(0, weights.queenWeight) : 0;

			int total = pawnW + knightW + bishopW + rookW + queenW;
			if (total > 0)
			{
				int roll = Random.Range(0, total);
				int current = pawnW;
				if (roll < current) return PieceType.Pawn;
				current += knightW;
				if (roll < current) return PieceType.Knight;
				current += bishopW;
				if (roll < current) return PieceType.Bishop;
				current += rookW;
				if (roll < current) return PieceType.Rook;
				return PieceType.queen;
			}
		}

		// fallback (też z unlock)
		int r = Random.Range(0, 100);
		if (r < 45) return PieceType.Pawn;
		if (r < 70) return PieceType.Knight;
		if (r < 90) return PieceType.Bishop;
		if (economyConfig == null || round >= economyConfig.rookUnlockRound) return PieceType.Rook;
		if (economyConfig == null || round >= economyConfig.queenUnlockRound) return PieceType.queen;
		return PieceType.Pawn;
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

        void ApplyPiecePrefabsForLocalPlayer()
        {
                bool useWhite = GameProgress.Instance == null || GameProgress.Instance.IsLocalPlayerWhite();
                if (useWhite && whitePiecePrefabs != null && whitePiecePrefabs.Length == 6)
                {
                        piecePrefabs = whitePiecePrefabs;
                }
                else if (!useWhite && blackPiecePrefabs != null && blackPiecePrefabs.Length == 6)
                {
                        piecePrefabs = blackPiecePrefabs;
                }
        }

        int GetPrice(PieceType type)
        {
                if (economyConfig != null)
                {
                        int roundNumber = GameProgress.Instance != null ? GameProgress.Instance.gamesPlayed + 1 : 1;
                        return economyConfig.GetPrice(type, roundNumber);
                }

                return prices[type];
        }

	public void TryRerollShop()
	{
		if (GameProgress.Instance == null) return;

		int cost = economyConfig != null ? economyConfig.rerollCost : 0;
		if (cost > 0 && GameProgress.Instance.coins < cost)
		{
			Debug.Log("Nie stać Cię na przelosowanie sklepu!");
			return;
		}

		int coinsBefore = GameProgress.Instance.coins;

		if (cost > 0)
			GameProgress.Instance.SpendCoins(cost);

		RefillShop();
		UpdateUI();

		int coinsAfter = GameProgress.Instance.coins;

		if (TelemetryService.Instance != null)
			TelemetryService.Instance.LogReroll(cost, coinsBefore, coinsAfter);
	}





	private Button CreateRerollButton()
        {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                        return null;
                }

                GameObject buttonObject = new GameObject("RerollButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(canvas.transform, false);

                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(220f, 60f);
                rect.anchoredPosition = new Vector2(-30f, 30f);

                Image image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                GameObject labelObject = new GameObject("Label", typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(buttonObject.transform, false);
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 24;
                label.color = Color.white;
                label.text = "Losuj za: 0 C";

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                return buttonObject.GetComponent<Button>();
        }

        private void PositionRerollButton(Button button)
        {
                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect == null)
                {
                        return;
                }

                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(220f, 60f);
                rect.anchoredPosition = new Vector2(-30f, 30f);
        }

	//private void LogShopOffer()
	//{
	//	if (TelemetryService.Instance == null || economyConfig == null)
	//		return;

	//	List<string> offeredPieces = new List<string>();

	//	foreach (GameObject tileGO in shopTiles)
	//	{
	//		if (tileGO == null)
	//		{
	//			offeredPieces.Add("Empty");
	//			continue;
	//		}

	//		ShopItem item = tileGO.GetComponentInChildren<ShopItem>();
	//		if (item == null || item.type == PieceType.King)
	//		{
	//			offeredPieces.Add("Empty");
	//			continue;
	//		}

	//		offeredPieces.Add(TelemetryService.ToTelemetryPieceType(item.type));
	//	}

	//	// cache oferty pod sanity-check
	//	currentOfferPieces = new List<string>(offeredPieces);

	//	int slots = shopRows * shopCols;
	//	TelemetryService.Instance.LogShopOfferGenerated(offeredPieces, slots, economyConfig.rerollCost);
	//}


	private int GetShopSlotIndex(Tile tile)
        {
                if (tile == null)
                {
                        return -1;
                }

                int index = shopTiles.FindIndex(t => t != null && t.GetComponent<Tile>() == tile);
                if (index >= 0)
                {
                        return index;
                }

                return (tile.row * shopCols) + tile.col;
        }
}
