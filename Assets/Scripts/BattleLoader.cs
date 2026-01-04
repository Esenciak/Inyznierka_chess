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
                                && session.HostArmy.Count == 0
                                && session.ClientArmy.Count == 0
                                && !(session.IsHostReady.Value && session.IsClientReady.Value)
                                && waitTime < 3f)
                        {
                                waitTime += Time.deltaTime;
                                yield return null;
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
                        SpawnPiece(data.type, data.x, data.y, BoardType.Player, PieceOwner.Player);
                        // 2. Wróg (Lustrzane odbicie)
                        SpawnPiece(data.type, data.x, data.y, BoardType.Enemy, PieceOwner.Enemy);
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

                if (myArmy.Count == 0 && enemyArmy.Count == 0)
                {
                        Debug.LogWarning("Brak danych armii w trybie multiplayer. Generuję armię testową.");
                        GenerateDebugArmy();
                        return;
                }

                foreach (NetworkArmyPiece data in myArmy)
                {
                        SpawnPiece(data.type, data.x, data.y, BoardType.Player, PieceOwner.Player);
                }

                foreach (NetworkArmyPiece data in enemyArmy)
                {
                        SpawnPiece(data.type, data.x, data.y, BoardType.Enemy, PieceOwner.Enemy);
                }
        }

        void GenerateDebugArmy()
        {
                // Generuje Króla i kilka pionków dla testu
                SpawnPiece(PieceType.King, 1, 1, BoardType.Player, PieceOwner.Player);
                SpawnPiece(PieceType.King, 1, 1, BoardType.Enemy, PieceOwner.Enemy);

                SpawnPiece(PieceType.Pawn, 0, 0, BoardType.Player, PieceOwner.Player);
                SpawnPiece(PieceType.Pawn, 0, 0, BoardType.Enemy, PieceOwner.Enemy);
        }

        void SpawnPiece(PieceType type, int x, int y, BoardType board, PieceOwner owner)
        {
                Tile tile = BoardManager.Instance.GetTile(board, y, x);

                if (tile != null)
                {
                        GameObject prefab = GetPrefabByType(type);
                        if (prefab != null)
                        {
                                GameObject go = Instantiate(prefab, tile.transform.position, Quaternion.identity);
                                go.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);

                                Piece piece = go.GetComponent<Piece>();
                                piece.owner = owner;
                                piece.pieceType = type;
                                piece.currentTile = tile;

                                tile.isOccupied = true;
                                tile.currentPiece = piece;

                                // Wróg na czerwono i bez ruchu myszką
                                if (owner == PieceOwner.Enemy)
                                {
                                        go.GetComponent<SpriteRenderer>().color = new Color(1f, 0.6f, 0.6f);
                                        if (go.GetComponent<PieceMovement>())
                                                Destroy(go.GetComponent<PieceMovement>());
                                }
                        }
                }
        }

        GameObject GetPrefabByType(PieceType type)
        {
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
