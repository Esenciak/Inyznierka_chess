using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections.Generic;

public class PieceMovement : MonoBehaviour
{
        private Vector3 startPosition;
        private bool isDragging = false;
        private Piece pieceComponent;
        private SpriteRenderer sr;
        private int originalOrder;
        [SerializeField] private bool logInputBlocks = false;

        void Start()
        {
                pieceComponent = GetComponent<Piece>();
                sr = GetComponent<SpriteRenderer>();
        }

        void OnMouseDown()
        {
                bool isBattle = SceneManager.GetActiveScene().name == "Battle";
                bool isNetworked = isBattle
                        && ((GameManager.Instance != null && GameManager.Instance.isMultiplayer)
                                || (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
                                || (BattleMoveSync.Instance != null && BattleMoveSync.Instance.IsSpawned));

                if (isBattle)
                {
                        if (isNetworked)
                        {
                                if (!IsLocalPlayersPiece())
                                {
                                        if (logInputBlocks)
                                        {
                                                Debug.Log($"[PieceMovement] Blocked: not local piece. Owner={pieceComponent.owner}");
                                        }
                                        return;
                                }
                                if (BattleMoveSync.Instance != null && !GameManager.Instance.IsMyTurn())
                                {
                                        if (logInputBlocks)
                                        {
                                                Debug.Log($"[PieceMovement] Blocked: not your turn. Owner={pieceComponent.owner}");
                                        }
                                        Debug.Log("To nie twoja tura!");
                                        return;
                                }
                        }
                        else if (GameManager.Instance != null && !GameManager.Instance.IsMyTurn())
                        {
                                if (logInputBlocks)
                                {
                                        Debug.Log($"[PieceMovement] Blocked: not your turn (offline). Owner={pieceComponent.owner}");
                                }
                                Debug.Log("To nie twoja tura!");
                                return;
                        }
                        else if (pieceComponent.owner != PieceOwner.Player)
                        {
                                if (logInputBlocks)
                                {
                                        Debug.Log($"[PieceMovement] Blocked: owner is {pieceComponent.owner} in offline mode.");
                                }
                                return;
                        }

                        // --- WŁĄCZ PODŚWIETLENIE (TYLKO W BITWIE) ---
                        pieceComponent.ToggleHighlight(true);
                }
                else if (pieceComponent.owner != PieceOwner.Player)
                {
                        return;
                }

                isDragging = true;
                startPosition = transform.position;

                if (sr)
                {
                        originalOrder = sr.sortingOrder;
                        sr.sortingOrder = 100;
                }
                transform.localScale *= 1.1f;
        }

        void OnMouseDrag()
        {
                if (isDragging)
                {
                        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        transform.position = new Vector3(mousePos.x, mousePos.y, -5);
                }
        }

        void OnMouseUp()
        {
                if (!isDragging) return;
                isDragging = false;

                if (sr) sr.sortingOrder = originalOrder;
                transform.localScale /= 1.1f;

                bool isBattle = SceneManager.GetActiveScene().name == "Battle";

                // --- WYCZ PODŚWIETLENIE (TYLKO W BITWIE) ---
                if (isBattle)
                {
                        pieceComponent.ToggleHighlight(false);
                }

                // Raycast i szukanie kafelka
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

                Tile targetTile = null;
                foreach (var hit in hits)
                {
                        Tile t = hit.collider.GetComponent<Tile>();
                        if (t != null) { targetTile = t; break; }
                }

                if (targetTile != null)
                {
                        // LOGIKA DLA SKLEPU
                        if (!isBattle)
                        {
                                if (pieceComponent.pieceType == PieceType.King && targetTile.isInventory)
                                {
                                        Debug.Log("Król nie może do inventory!");
                                        transform.position = startPosition;
                                        return;
                                }

                                if ((targetTile.isInventory || targetTile.boardType == BoardType.Player) && !targetTile.isOccupied)
                                {
                                        MoveToTile(targetTile);
                                        return;
                                }
                        }
                        // LOGIKA DLA BITWY
                        else
                        {
                                List<Tile> legalMoves = pieceComponent.GetLegalMoves();

                                if (legalMoves.Contains(targetTile))
                                {
                                        if (GameManager.Instance != null && GameManager.Instance.isMultiplayer && BattleMoveSync.Instance != null)
                                        {
                                                BattleMoveSync.Instance.SubmitMove(pieceComponent, targetTile);
                                                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                                                {
                                                        transform.position = startPosition;
                                                }
                                                return;
                                        }

                                        Piece capturedPiece = null;
                                        // Bicie
                                        if (targetTile.isOccupied && targetTile.currentPiece != null)
                                        {
                                                if (targetTile.currentPiece.owner != pieceComponent.owner)
                                                {
                                                        capturedPiece = targetTile.currentPiece;
                                                        Destroy(targetTile.currentPiece.gameObject);
                                                }
                                        }

                                        MoveToTile(targetTile);

                                        if (capturedPiece != null && capturedPiece.pieceType == PieceType.King && GameManager.Instance != null)
                                        {
                                                GameManager.Instance.GameOver(pieceComponent.owner == PieceOwner.Player);
                                                return;
                                        }

                                        GameManager.Instance.SwitchTurn();
                                        return;
                                }
                                else
                                {
                                        Debug.Log("Nielegalny ruch!");
                                }
                        }
                }

                // Powrót przy błędzie
                transform.position = startPosition;
        }

        void MoveToTile(Tile newTile)
        {
                if (pieceComponent.currentTile != null)
                {
                        pieceComponent.currentTile.isOccupied = false;
                        pieceComponent.currentTile.currentPiece = null;
                }

                newTile.isOccupied = true;
                newTile.currentPiece = pieceComponent;
                pieceComponent.currentTile = newTile;

                transform.SetParent(newTile.transform);
                transform.position = new Vector3(newTile.transform.position.x, newTile.transform.position.y, -1);
                startPosition = transform.position;
        }

        bool IsLocalPlayersPiece()
        {
                bool networkActive = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                        || (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
                        || (GameManager.Instance != null && GameManager.Instance.isMultiplayer)
                        || (BattleMoveSync.Instance != null && BattleMoveSync.Instance.IsSpawned);

                if (!networkActive)
                {
                        return pieceComponent.owner == PieceOwner.Player;
                }

                if (NetworkManager.Singleton == null)
                {
                        if (GameProgress.Instance != null)
                        {
                                return GameProgress.Instance.isHostPlayer
                                        ? pieceComponent.owner == PieceOwner.Player
                                        : pieceComponent.owner == PieceOwner.Enemy;
                        }

                        return pieceComponent.owner == PieceOwner.Player;
                }

                bool localIsHost = NetworkManager.Singleton.IsHost;
                return localIsHost ? pieceComponent.owner == PieceOwner.Player : pieceComponent.owner == PieceOwner.Enemy;
        }
}
