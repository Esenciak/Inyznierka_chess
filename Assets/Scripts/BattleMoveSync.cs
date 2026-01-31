using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BattleMoveSync : NetworkBehaviour
{
        public static BattleMoveSync Instance { get; private set; }

        public NetworkVariable<PieceOwner> CurrentTurn = new NetworkVariable<PieceOwner>(
                PieceOwner.Player,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public NetworkVariable<int> TurnIndexInRound = new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private void Awake()
        {
                if (Instance != null && Instance != this)
                {
                        Destroy(gameObject);
                        return;
                }
                Instance = this;
        }

        private void OnDestroy()
        {
                if (Instance == this)
                {
                        Instance = null;
                }
        }

        public override void OnNetworkSpawn()
        {
                CurrentTurn.OnValueChanged += HandleTurnChanged;
                if (IsServer)
                {
                        CurrentTurn.Value = PieceOwner.Player;
                        TurnIndexInRound.Value = 0;
                        SyncActiveTeam(CurrentTurn.Value);
                }
                HandleTurnChanged(CurrentTurn.Value, CurrentTurn.Value);
        }

        public override void OnNetworkDespawn()
        {
                CurrentTurn.OnValueChanged -= HandleTurnChanged;
        }

        public bool IsLocalPlayersTurn()
        {
                if (!IsSpawned || NetworkManager.Singleton == null)
                {
                        return true;
                }

                bool localIsHost = NetworkManager.Singleton.IsHost;
                PieceOwner localSide = localIsHost ? PieceOwner.Player : PieceOwner.Enemy;
                return CurrentTurn.Value == localSide;
        }

        public void SubmitMove(Piece piece, Tile targetTile)
        {
                if (!IsSpawned || !IsClient || piece == null || targetTile == null || piece.currentTile == null)
                {
                        return;
                }

                SubmitMoveServerRpc(piece.currentTile.globalRow, piece.currentTile.globalCol, targetTile.globalRow, targetTile.globalCol);
        }

        public void RequestResign()
        {
                if (!IsSpawned || !IsClient)
                {
                        return;
                }

                RequestResignServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestResignServerRpc(ServerRpcParams rpcParams = default)
        {
                if (NetworkManager.Singleton == null)
                {
                        return;
                }

                ulong senderId = rpcParams.Receive.SenderClientId;
                bool senderIsHost = senderId == NetworkManager.ServerClientId;
                bool hostWon = !senderIsHost;

                if (GameManager.Instance != null)
                {
			        GameManager.Instance.GameOver(hostWon, "ResignRound");
		        }

                SendResignToClients(hostWon);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitMoveServerRpc(int fromRow, int fromCol, int toRow, int toCol, ServerRpcParams rpcParams = default)
        {
                if (BoardManager.Instance == null)
                {
                        return;
                }

                ulong senderId = rpcParams.Receive.SenderClientId;
                bool senderIsHost = NetworkManager.Singleton != null && senderId == NetworkManager.ServerClientId;
                PieceOwner expectedOwner = senderIsHost ? PieceOwner.Player : PieceOwner.Enemy;

                if (CurrentTurn.Value != expectedOwner)
                {
                        return;
                }

                Vector2Int fromCoords = new Vector2Int(fromRow, fromCol);
                Vector2Int toCoords = new Vector2Int(toRow, toCol);
                if (!senderIsHost)
                {
                        fromCoords = MirrorGlobalCoords(fromCoords);
                        toCoords = MirrorGlobalCoords(toCoords);
                }

                Tile fromTile = BoardManager.Instance.GetTileGlobal(fromCoords.x, fromCoords.y);
                Tile toTile = BoardManager.Instance.GetTileGlobal(toCoords.x, toCoords.y);
                if (fromTile == null || toTile == null || fromTile.currentPiece == null)
                {
                        return;
                }

                Piece movingPiece = fromTile.currentPiece;
                if (movingPiece.owner != expectedOwner)
                {
                        return;
                }

                List<Tile> legalMoves = movingPiece.GetLegalMoves();
                if (!legalMoves.Contains(toTile))
                {
                        return;
                }

                bool capturedKing = false;
                Piece capturedPiece = null;
                string capturedPieceType = null;
                if (toTile.isOccupied && toTile.currentPiece != null && toTile.currentPiece.owner != expectedOwner)
                {
                        capturedKing = toTile.currentPiece.pieceType == PieceType.King;
                        capturedPiece = toTile.currentPiece;
                        capturedPieceType = TelemetryService.ToTelemetryPieceType(capturedPiece.pieceType);
                        Destroy(toTile.currentPiece.gameObject);
                }

                ApplyMoveLocal(movingPiece, toTile);

                int turnIndexInRound = TurnIndexInRound.Value;
                TurnIndexInRound.Value = turnIndexInRound + 1;

                if (TelemetryService.Instance != null && TelemetryService.Instance.IsLocalOwner(expectedOwner))
                {
                        string movingPieceType = TelemetryService.ToTelemetryPieceType(movingPiece.pieceType);
                        TelemetryService.Instance.LogPieceMoved(
                                movingPieceType,
                                fromCoords.y,
                                fromCoords.x,
                                toCoords.y,
                                toCoords.x,
                                turnIndexInRound);

                        if (capturedPieceType != null)
                        {
                                TelemetryService.Instance.LogPieceCaptured(
                                        movingPieceType,
                                        fromCoords.y,
                                        fromCoords.x,
                                        toCoords.y,
                                        toCoords.x,
                                        capturedPieceType,
                                        GetCaptureBoardSize(),
                                        GetCaptureRegion(toCoords.x),
                                        turnIndexInRound);
                        }
                }

                PieceOwner nextTurn = expectedOwner == PieceOwner.Player ? PieceOwner.Enemy : PieceOwner.Player;
                CurrentTurn.Value = nextTurn;
                SyncActiveTeam(nextTurn);

                if (capturedKing && GameManager.Instance != null)
                {
                        bool hostWon = expectedOwner == PieceOwner.Player;
                        GameManager.Instance.GameOver(hostWon, "KingCaptured");
                }

                SendMoveToClients(fromCoords.x, fromCoords.y, toCoords.x, toCoords.y, capturedKing, expectedOwner == PieceOwner.Player, turnIndexInRound);
        }

        private void SendMoveToClients(int fromRow, int fromCol, int toRow, int toCol, bool capturedKing, bool hostWon, int turnIndexInRound)
        {
                if (NetworkManager.Singleton == null)
                {
                        return;
                }

                List<ulong> targetClients = new List<ulong>();
                foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                        if (clientId == NetworkManager.Singleton.LocalClientId) continue;
                        targetClients.Add(clientId);
                }

                if (targetClients.Count == 0)
                {
                        return;
                }

                ClientRpcParams rpcParams = new ClientRpcParams
                {
                        Send = new ClientRpcSendParams
                        {
                                TargetClientIds = targetClients
                        }
                };

                ApplyMoveClientRpc(fromRow, fromCol, toRow, toCol, capturedKing, hostWon, turnIndexInRound, rpcParams);
        }

        private void SendResignToClients(bool hostWon)
        {
                if (NetworkManager.Singleton == null)
                {
                        return;
                }

                List<ulong> targetClients = new List<ulong>();
                foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                        if (clientId == NetworkManager.Singleton.LocalClientId) continue;
                        targetClients.Add(clientId);
                }

                if (targetClients.Count == 0)
                {
                        return;
                }

                ClientRpcParams rpcParams = new ClientRpcParams
                {
                        Send = new ClientRpcSendParams
                        {
                                TargetClientIds = targetClients
                        }
                };

                ApplyResignClientRpc(hostWon, rpcParams);
        }

        [ClientRpc]
        private void ApplyMoveClientRpc(int fromRow, int fromCol, int toRow, int toCol, bool capturedKing, bool hostWon, int turnIndexInRound, ClientRpcParams rpcParams = default)
        {
                if (BoardManager.Instance == null)
                {
                        return;
                }

                Vector2Int fromCoords = new Vector2Int(fromRow, fromCol);
                Vector2Int toCoords = new Vector2Int(toRow, toCol);
                if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost)
                {
                        fromCoords = MirrorGlobalCoords(fromCoords);
                        toCoords = MirrorGlobalCoords(toCoords);
                }

                Tile fromTile = BoardManager.Instance.GetTileGlobal(fromCoords.x, fromCoords.y);
                Tile toTile = BoardManager.Instance.GetTileGlobal(toCoords.x, toCoords.y);
                if (fromTile == null || toTile == null || fromTile.currentPiece == null)
                {
                        return;
                }

                Piece movingPiece = fromTile.currentPiece;
                string movingPieceType = null;
                if (movingPiece != null)
                {
                        movingPieceType = TelemetryService.ToTelemetryPieceType(movingPiece.pieceType);
                }

                string capturedPieceType = null;
                if (toTile.isOccupied && toTile.currentPiece != null && toTile.currentPiece != movingPiece)
                {
                        capturedPieceType = TelemetryService.ToTelemetryPieceType(toTile.currentPiece.pieceType);
                        Destroy(toTile.currentPiece.gameObject);
                }

                if (movingPiece != null)
                {
                        ApplyMoveLocal(movingPiece, toTile);
                }

                PieceOwner moveOwner = hostWon ? PieceOwner.Player : PieceOwner.Enemy;
                if (movingPieceType != null && TelemetryService.Instance != null && TelemetryService.Instance.IsLocalOwner(moveOwner))
                {
                        TelemetryService.Instance.LogPieceMoved(
                                movingPieceType,
                                fromCoords.y,
                                fromCoords.x,
                                toCoords.y,
                                toCoords.x,
                                turnIndexInRound);

                        if (capturedPieceType != null)
                        {
                                TelemetryService.Instance.LogPieceCaptured(
                                        movingPieceType,
                                        fromCoords.y,
                                        fromCoords.x,
                                        toCoords.y,
                                        toCoords.x,
                                        capturedPieceType,
                                        GetCaptureBoardSize(),
                                        GetCaptureRegion(toCoords.x),
                                        turnIndexInRound);
                        }
                }

                if (capturedKing && GameManager.Instance != null)
                {
                        bool localIsHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
                        bool localWon = localIsHost == hostWon;
                        GameManager.Instance.GameOver(localWon, "KingCaptured");
                }
        }

        [ClientRpc]
        private void ApplyResignClientRpc(bool hostWon, ClientRpcParams rpcParams = default)
        {
                if (GameManager.Instance == null)
                {
                        return;
                }

                bool localIsHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
                bool localWon = localIsHost == hostWon;
		        GameManager.Instance.GameOver(localWon, "ResignRound");

    	}

	private void ApplyMoveLocal(Piece piece, Tile targetTile)
        {
                if (piece.currentTile != null)
                {
                        piece.currentTile.isOccupied = false;
                        piece.currentTile.currentPiece = null;
                }

                targetTile.isOccupied = true;
                targetTile.currentPiece = piece;
                piece.currentTile = targetTile;

                piece.transform.position = new Vector3(targetTile.transform.position.x, targetTile.transform.position.y, -1f);
        }

        private Vector2Int MirrorGlobalCoords(Vector2Int coords)
        {
                if (BoardManager.Instance == null)
                {
                        return coords;
                }

                int totalCols = Mathf.Max(BoardManager.Instance.CenterCols, BoardManager.Instance.PlayerCols);
                int mirroredRow = BoardManager.Instance.totalRows - 1 - coords.x;
                int mirroredCol = totalCols - 1 - coords.y;
                return new Vector2Int(mirroredRow, mirroredCol);
        }

        private void HandleTurnChanged(PieceOwner previous, PieceOwner current)
        {
                if (GameManager.Instance != null)
                {
                        GameManager.Instance.currentTurn = current;
                }
                if (TurnIndicator.Instance != null)
                {
                        TurnIndicator.Instance.UpdateTurnText();
                }
        }

        private void SyncActiveTeam(PieceOwner turn)
        {
                if (!IsServer || BattleSession.Instance == null)
                {
                        return;
                }

                BattleSession.Instance.ActiveTeam.Value = turn == PieceOwner.Player ? 0 : 1;
        }

        private int? GetCaptureBoardSize()
        {
                if (BoardManager.Instance == null)
                {
                        return null;
                }

                return BoardManager.Instance.CenterRows;
        }

        private string GetCaptureRegion(int globalRow)
        {
                if (BoardManager.Instance == null)
                {
                        return null;
                }

                int playerRows = BoardManager.Instance.PlayerRows;
                int centerRows = BoardManager.Instance.CenterRows;
                if (globalRow < playerRows)
                {
                        return "Player";
                }

                if (globalRow < playerRows + centerRows)
                {
                        return "Center";
                }

                return "Enemy";
        }

        public int GetLastTurnIndexInRound()
        {
                return Mathf.Max(TurnIndexInRound.Value - 1, 0);
        }
}
