using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class BattleLoader : MonoBehaviour
{
        [Header("Prefaby (Kolejno wg Enuma!)")]
        // 0:Pawn, 1:King, 2:Queen, 3:Rook, 4:Bishop, 5:Knight
        public GameObject[] piecePrefabs;
        public GameObject[] whitePrefabs;
        public GameObject[] blackPrefabs;

        private void Start()
        {
                if (SceneManager.GetActiveScene().name == "Battle")
                {
                        StartCoroutine(LoadBattleRoutine());
                }
        }

        IEnumerator LoadBattleRoutine()
        {
                yield return new WaitUntil(() => BoardManager.Instance != null && BoardManager.Instance.IsReady);

                if (GameManager.Instance != null && GameManager.Instance.isMultiplayer)
                {
                        yield return new WaitUntil(() => BattleSession.Instance != null);

                        var session = BattleSession.Instance;
                        float waitTime = 0f;
                        while (session != null
                                && waitTime < 10f
                                && (!session.IsHostReady.Value || !session.IsClientReady.Value
                                        || (session.HostArmy.Count == 0 && session.ClientArmy.Count == 0)))
                        {
                                waitTime += Time.deltaTime;
                                yield return null;
                        }

                        if (session == null
                                || !session.IsHostReady.Value
                                || !session.IsClientReady.Value
                                || (session.HostArmy.Count == 0 && session.ClientArmy.Count == 0))
                        {
                                Debug.LogWarning("Brak danych armii w trybie multiplayer - przerywam ładowanie bitwy.");
                                yield break;
                        }
                }

                LoadBattle();
        }

        void LoadBattle()
        {
                bool isMultiplayerActive = GameManager.Instance != null
                        && GameManager.Instance.isMultiplayer
                        && BattleSession.Instance != null
                        && NetworkManager.Singleton != null
                        && NetworkManager.Singleton.IsListening;

                if (isMultiplayerActive)
                {
                        LoadMultiplayerBattle();
                        return;
                }

                LoadSingleplayerBattle();
        }

        void LoadSingleplayerBattle()
        {
                List<SavedPieceData> army = GameProgress.Instance.myArmy;

                // --- TRYB AWARYJNY (Jeśli odpalasz Battle bezpośrednio) ---
                if (army.Count == 0)
                {
                        Debug.LogWarning("Brak armii ze sklepu! Generuję armię testową.");
                        GenerateDebugArmy(); // Generuje domyślne pionki
                        return;
                }

                // --- TRYB NORMALNY (Ze sklepu) ---
                foreach (SavedPieceData data in army)
                {
                        // 1. Gracz
                        SpawnPiece(data.type, data.x, data.y, BoardType.Player, PieceOwner.Player, false);
                        // 2. Wróg (Lustrzane odbicie)
                        SpawnPiece(data.type, data.x, data.y, BoardType.Enemy, PieceOwner.Enemy, true);
                }
        }

        void LoadMultiplayerBattle()
        {
                var session = BattleSession.Instance;
                if (session == null)
                {
                        Debug.LogWarning("Brak BattleSession w trybie multiplayer. Ładuję tryb domyślny.");
                        LoadSingleplayerBattle();
                        return;
                }

                bool isHostView = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

                var myArmy = isHostView ? session.HostArmy : session.ClientArmy;
                var enemyArmy = isHostView ? session.ClientArmy : session.HostArmy;
                PieceOwner localOwner = isHostView ? PieceOwner.Player : PieceOwner.Enemy;
                PieceOwner enemyOwner = isHostView ? PieceOwner.Enemy : PieceOwner.Player;
                BoardType localBoard = BoardType.Player;
                BoardType enemyBoard = BoardType.Enemy;
                bool localMirror = false;
                bool enemyMirror = true;

                if (myArmy.Count == 0 && enemyArmy.Count == 0)
                {
                        Debug.LogWarning("Brak danych armii w trybie multiplayer. Przerywam ładowanie.");
                        return;
                }

                foreach (NetworkArmyPiece data in myArmy)
                {
                        SpawnPiece(data.type, data.x, data.y, localBoard, localOwner, localMirror);
                }

                foreach (NetworkArmyPiece data in enemyArmy)
                {
                        SpawnPiece(data.type, data.x, data.y, enemyBoard, enemyOwner, enemyMirror);
                }
        }

        void GenerateDebugArmy()
        {
                // Generuje Króla i kilka pionków dla testu
                SpawnPiece(PieceType.King, 1, 1, BoardType.Player, PieceOwner.Player, false);
                SpawnPiece(PieceType.King, 1, 1, BoardType.Enemy, PieceOwner.Enemy, true);

                SpawnPiece(PieceType.Pawn, 0, 0, BoardType.Player, PieceOwner.Player, false);
                SpawnPiece(PieceType.Pawn, 0, 0, BoardType.Enemy, PieceOwner.Enemy, true);
        }

        void SpawnPiece(PieceType type, int x, int y, BoardType board, PieceOwner owner, bool mirrorCoordinates)
        {
                Vector2Int coords = mirrorCoordinates ? MirrorCoordinates(x, y) : new Vector2Int(x, y);
                Tile tile = BoardManager.Instance.GetTile(board, coords.y, coords.x);

                if (tile != null)
                {
                        GameObject prefab = GetPrefabByType(type, owner);
                        if (prefab != null)
                        {
                                GameObject go = Instantiate(prefab, tile.transform.position, Quaternion.identity);
                                if (go.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
                                {
                                        DestroyImmediate(netObj);
                                }
                                go.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);

                                Piece piece = go.GetComponent<Piece>();
                                piece.owner = owner;
                                piece.pieceType = type;
                                piece.currentTile = tile;

                                tile.isOccupied = true;
                                tile.currentPiece = piece;
                        }
                }
        }

        Vector2Int MirrorCoordinates(int x, int y)
        {
                int mirroredX = BoardManager.Instance.PlayerCols - 1 - x;
                int mirroredY = BoardManager.Instance.PlayerRows - 1 - y;
                return new Vector2Int(mirroredX, mirroredY);
        }

        GameObject GetPrefabByType(PieceType type, PieceOwner owner)
        {
                GameObject[] set = GetPrefabSet(owner);
                if (set == null || set.Length < 6)
                {
                        set = piecePrefabs;
                }

                switch (type)
                {
                        case PieceType.Pawn: return set[0];
                        case PieceType.King: return set[1];
                        case PieceType.queen: return set[2];
                        case PieceType.Rook: return set[3];
                        case PieceType.Bishop: return set[4];
                        case PieceType.Knight: return set[5];
                }
                return set[0];
        }

        GameObject[] GetPrefabSet(PieceOwner owner)
        {
                bool localWhite = GameProgress.Instance == null || GameProgress.Instance.IsLocalPlayerWhite();
                bool isLocalPiece = owner == PieceOwner.Player;

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                        bool localIsHost = NetworkManager.Singleton.IsHost;
                        isLocalPiece = localIsHost ? owner == PieceOwner.Player : owner == PieceOwner.Enemy;
                }

                if (localWhite)
                {
                        return isLocalPiece ? whitePrefabs : blackPrefabs;
                }

                return isLocalPiece ? blackPrefabs : whitePrefabs;
        }
}
