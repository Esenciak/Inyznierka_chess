using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
        public static InventoryManager Instance { get; private set; }
        public bool IsReady { get; private set; } = false;
        private bool isInitializing = false;

        [Header("Ustawienia Ekwipunku")]
        public int rows = 5;
        public int cols = 2;
        public GameObject tilePrefab;
        public GameObject kingPrefab;
        public GameObject whiteKingPrefab;
        public GameObject blackKingPrefab;
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
                        IsReady = false;
                        StartCoroutine(InitializeInventoryRoutine());
                }
                else
                {
                        ClearInventory();
                }
        }

        private void Start()
        {
                if (SceneManager.GetActiveScene().name == "Shop")
                {
                        StartCoroutine(InitializeInventoryRoutine());
                }
        }

        IEnumerator InitializeInventoryRoutine()
        {
                if (isInitializing)
                {
                        yield break;
                }

                isInitializing = true;

                while (BoardManager.Instance == null)
                {
                        yield return null;
                }

                SelectKingPrefab();

                yield return new WaitForEndOfFrame();

                ClearInventory();
                GenerateInventory();

                EnsureKingInArmy();
                if (GameProgress.Instance == null || !HasKingInArmy())
                {
                        SpawnKingOnBoard();
                }

                IsReady = true;
                isInitializing = false;
        }

        public void EnsureInitialized()
        {
                if (SceneManager.GetActiveScene().name != "Shop")
                {
                        return;
                }

                if (!IsReady && !isInitializing)
                {
                        StartCoroutine(InitializeInventoryRoutine());
                }
        }

        void ClearInventory()
        {
                foreach (var go in inventoryTiles)
                {
                        if (go != null) Destroy(go);
                }
                inventoryTiles.Clear();
                IsReady = false;
        }

        void GenerateInventory()
        {
                if (BoardManager.Instance == null)
                {
                        Debug.LogError("BoardManager nadal jest null! Nie mogę stworzyć Inventory.");
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
                                tile.row = r;
                                tile.col = c;

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
                if (inventoryTiles == null || inventoryTiles.Count == 0)
                {
                        Debug.LogError("Błąd: Próba dodania do Inventory, ale lista jest pusta! Czy scena Shop załadowała się poprawnie?");
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

                Debug.Log("Ekwipunek pełny!");
                return false;
        }

        public void SaveInventoryLayout(List<SavedInventoryData> target)
        {
                if (target == null) return;
                target.Clear();

                foreach (var tileGO in inventoryTiles)
                {
                        if (tileGO == null) continue;
                        Tile tile = tileGO.GetComponent<Tile>();
                        if (tile == null || !tile.isOccupied || tile.currentPiece == null) continue;

                        SavedInventoryData data = new SavedInventoryData
                        {
                                type = tile.currentPiece.pieceType,
                                row = tile.row,
                                col = tile.col
                        };
                        target.Add(data);
                }
        }

        public bool TryPlaceInventoryPiece(PieceType type, GameObject prefab, int row, int col)
        {
                Tile tile = GetInventoryTile(row, col);
                if (tile == null || tile.isOccupied) return false;

                SpawnPiece(prefab, tile, type);
                return true;
        }

        void SpawnPiece(GameObject prefab, Tile tile, PieceType type)
        {
                GameObject pieceGO = Instantiate(prefab, tile.transform.position, Quaternion.identity);

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

        Tile GetInventoryTile(int row, int col)
        {
                foreach (var tileGO in inventoryTiles)
                {
                        if (tileGO == null) continue;
                        Tile tile = tileGO.GetComponent<Tile>();
                        if (tile != null && tile.row == row && tile.col == col)
                        {
                                return tile;
                        }
                }
                return null;
        }

        bool HasKingInArmy()
        {
                if (GameProgress.Instance == null)
                {
                        return false;
                }

                foreach (SavedPieceData piece in GameProgress.Instance.myArmy)
                {
                        if (piece.type == PieceType.King)
                        {
                                return true;
                        }
                }

                return false;
        }

        void EnsureKingInArmy()
        {
                if (GameProgress.Instance == null || HasKingInArmy())
                {
                        return;
                }

                if (BoardManager.Instance == null)
                {
                        return;
                }

                int col = BoardManager.Instance.PlayerCols / 2;
                int row = BoardManager.Instance.PlayerRows / 2;
                GameProgress.Instance.myArmy.Add(new SavedPieceData
                {
                        type = PieceType.King,
                        x = col,
                        y = row
                });
        }

        void SelectKingPrefab()
        {
                bool useWhite = GameProgress.Instance == null || GameProgress.Instance.IsLocalPlayerWhite();
                if (useWhite && whiteKingPrefab != null)
                {
                        kingPrefab = whiteKingPrefab;
                }
                else if (!useWhite && blackKingPrefab != null)
                {
                        kingPrefab = blackKingPrefab;
                }
        }
}
