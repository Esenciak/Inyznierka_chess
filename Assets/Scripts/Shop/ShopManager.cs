using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
        [Header("Ustawienia Sklepu")]
        public int shopRows = 2;
        public int shopCols = 3;
        public Vector2 shopOffset = new Vector2(-5, 0);

        [Header("Prefabrykaty")]
        public GameObject tilePrefab;
        public GameObject priceTextPrefab; // Pusty obiekt z TextMeshPro (nie UI!)

        // 0:Pawn, 1:King, 2:Queen, 3:Rook, 4:Bishop, 5:Knight
        public GameObject[] piecePrefabs;
        public GameObject[] whitePiecePrefabs;
        public GameObject[] blackPiecePrefabs;

        [Header("UI - Teksty")]
        public TextMeshProUGUI coinsText;
        public TextMeshProUGUI centerBoardSizeText;
        public TextMeshProUGUI roundText;
        public Button startButton;

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

        // --- ZARZĄDZANIE SCENAMI ---
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
                // Jeśli weszliśmy do Sklepu -> Generuj
                if (scene.name == "Shop")
                {
                        FindUIReferences();
                        InitializeShop();
                }
                else
                {
                        // W Menu i Bitwie czyścimy sklep
                        CleanupShop();
                }
        }

        private void Start()
        {
                // Fallback: jeśli startujemy prosto ze sceny Shop
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
                // Szukamy obiektów w scenie po nazwie.
                // UPEWNIJ SIĘ, ŻE NAZWAŁEŚ JE TAK SAMO W CANVASIE!
                GameObject coinObj = GameObject.Find("UI_Coins");
                if (coinObj) coinsText = coinObj.GetComponent<TextMeshProUGUI>();

                GameObject roundObj = GameObject.Find("UI_Round");
                if (roundObj) roundText = roundObj.GetComponent<TextMeshProUGUI>();

                GameObject sizeObj = GameObject.Find("UI_BoardSize");
                if (sizeObj) centerBoardSizeText = sizeObj.GetComponent<TextMeshProUGUI>();

                GameObject btnObj = GameObject.Find("StartButton");
                if (btnObj != null)
                {
                        startButton = btnObj.GetComponent<Button>();
                        // Czyścimy stare kliknięcia (żeby nie klikało się 2 razy)
                        startButton.onClick.RemoveAllListeners();
                        // Dodajemy funkcję StartGame
                        startButton.onClick.AddListener(StartGame);
                }

                ToggleUI(true);
        }

        void InitializeShop()
        {
                ApplyPiecePrefabsForLocalPlayer();
                CleanupShop();
                GenerateShopGrid();
                RefillShop();
                ToggleUI(true);
                UpdateUI();
                StartCoroutine(RestoreLayoutRoutine());
        }

        void CleanupShop()
        {
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
        }
        // ----------------------------

        void GenerateShopGrid()
        {
                for (int r = 0; r < shopRows; r++)
                {
                        for (int c = 0; c < shopCols; c++)
                        {
                                Vector3 pos = new Vector3(shopOffset.x + c, shopOffset.y + r, 0);
                                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
                                tile.transform.parent = transform; // Porządek w hierarchii

                                tile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.5f, 0.2f);
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
                        if (tileGO == null) continue;

                        Tile tile = tileGO.GetComponent<Tile>();

                        // Czyścimy stare (szukamy w dzieciach lub przez Raycast)
                        ShopItem existingItem = tileGO.GetComponentInChildren<ShopItem>();
                        if (existingItem == null)
                        {
                                RaycastHit2D hit = Physics2D.Raycast(tileGO.transform.position, Vector2.zero);
                                if (hit.collider != null) existingItem = hit.collider.GetComponent<ShopItem>();
                        }

                        if (existingItem != null) Destroy(existingItem.gameObject);

                        tile.isOccupied = false;
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

                // 1. Tworzymy obiekt
                GameObject itemGO = Instantiate(prefab, pos, Quaternion.identity);

                // 2. KLUCZOWA POPRAWKA: Najpierw usuwamy NetworkObject!
                // Musimy użyć DestroyImmediate, żeby zniknął w tej milisekundzie, zanim kod pójdzie dalej
                if (itemGO.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
                {
                        DestroyImmediate(netObj);
                }

                // 3. Dopiero teraz bezpiecznie ustawiamy rodzica
                itemGO.transform.parent = tileGO.transform;

                // 4. Usuwamy resztę niepotrzebnych skryptów (logikę gry)
                Destroy(itemGO.GetComponent<Piece>());
                Destroy(itemGO.GetComponent<PieceMovement>());

                // 5. Dodajemy logikę sklepową
                ShopItem shopItem = itemGO.AddComponent<ShopItem>();
                Tile tile = tileGO.GetComponent<Tile>();

                Vector3 textOffset = (tile.row == 0) ? new Vector3(0, -1.2f, 0) : new Vector3(0, 1.2f, 0);

                shopItem.Setup(type, GetPrice(type), this, tile, priceTextPrefab, textOffset);

                tile.isOccupied = true;
        }

        public void TryBuyPiece(ShopItem item)
        {
                // 1. Czy stać nas?
                if (GameProgress.Instance.coins >= item.price)
                {
                        // 2. Czy jest miejsce w ekwipunku? (AddPieceToInventory zwraca teraz bool)
                        bool success = InventoryManager.Instance.AddPieceToInventory(item.type, GetPrefabByType(item.type));

                        if (success)
                        {
                                // Dopiero teraz zabieramy kasę i niszczymy przedmiot
                                GameProgress.Instance.SpendCoins(item.price);
                                Destroy(item.gameObject);
                                UpdateUI();
                        }
                        else
                        {
                                Debug.Log("Nie kupiono: Brak miejsca w ekwipunku!");
                                // Tutaj można dodać jakiś efekt dźwiękowy błędu lub tekst "FULL"
                        }
                }
                else
                {
                        Debug.Log("Nie stać Cię!");
                }
        }

        // --- START GRY (MULTIPLAYER + SINGLEPLAYER) ---
        public void StartGame()
        {
                // 1. Zapisz obecny stan planszy do GameProgress
                SaveBoardLayout();
                SaveInventoryLayout();

                // 2. Sprawdź tryb gry
                if (GameManager.Instance.isMultiplayer)
                {
                        StartCoroutine(WaitForBattleSessionAndReady());
                }
                else
                {
                        // Singleplayer: ładuj od razu
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
                        return economyConfig.GetPrice(type);
                }

                return prices[type];
        }
}
