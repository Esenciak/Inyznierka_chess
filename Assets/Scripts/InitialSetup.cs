using System.Collections;
using UnityEngine;

public class InitialSetup : MonoBehaviour
{
        [Header("Prefaby figur")]
        public GameObject kingPrefab;
        public GameObject pawnPrefab;

        private IEnumerator Start()
        {
                yield return new WaitUntil(() =>
                        BoardManager.Instance != null && BoardManager.Instance.IsReady);

                // awaryjnie podmieniamy brakujcy prefab pionka na ten sam, ktry
                // jest uytwany dla krla, eby unikn sytuacji braku pionw na planszy
                if (pawnPrefab == null)
                {
                        Debug.LogWarning("Brak przypisanego prefab'u pionka - uycie kingPrefab jako zamiennika.");
                        pawnPrefab = kingPrefab;
                }

                int rows = BoardManager.Instance.PlayerRows;
                int cols = BoardManager.Instance.PlayerCols;

                int playerFrontRow = rows - 1; // rząd przy centrum dla gracza
                int enemyFrontRow = 0;          // rząd przy centrum dla przeciwnika

                // wszystkie pionki w pierwszym rzędzie przy centrum
                for (int c = 0; c < cols; c++)
                {
                        SpawnPiece(pawnPrefab, BoardType.Player, playerFrontRow, c, PieceOwner.Player, PieceType.Pawn);
                        SpawnPiece(pawnPrefab, BoardType.Enemy, enemyFrontRow, c, PieceOwner.Enemy, PieceType.Pawn);
                }

                // króle w rogach plansz gracza/przeciwnika
                int playerKingRow = 0;
                int playerKingCol = Mathf.Max(0, cols - 1);
                SpawnPiece(kingPrefab, BoardType.Player, playerKingRow, playerKingCol, PieceOwner.Player, PieceType.King);

                int enemyKingRow = Mathf.Max(0, rows - 1);
                int enemyKingCol = Mathf.Max(0, cols - 1);
                SpawnPiece(kingPrefab, BoardType.Enemy, enemyKingRow, enemyKingCol, PieceOwner.Enemy, PieceType.King);
        }

        private void SpawnPiece(GameObject prefab, BoardType boardType, int row, int col, PieceOwner owner, PieceType type)
        {
                Tile tile = BoardManager.Instance.GetTile(boardType, row, col);
                if (tile == null)
                {
                        Debug.LogWarning($"Brak tile dla {boardType} ({row},{col})");
                        return;
                }

                GameObject pieceGO = Instantiate(prefab, tile.transform.position, Quaternion.identity);
                Piece piece = pieceGO.GetComponent<Piece>();

                if (piece == null)
                {
                        Debug.LogError("Prefab nie ma komponentu Piece!");
                        return;
                }

                piece.owner = owner;
                piece.pieceType = type;
                piece.currentTile = tile;

                tile.isOccupied = true;
                tile.currentPiece = piece;
        }
}
