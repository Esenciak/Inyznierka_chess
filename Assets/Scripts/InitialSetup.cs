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

		int rows = BoardManager.Instance.PlayerRows;
		int cols = BoardManager.Instance.PlayerCols;

		int midCol = cols / 2;

		// rz¹d przy centrum:
		int playerFrontRow = rows - 1; // górny rz¹d planszy gracza
		int enemyFrontRow = 0;        // dolny rz¹d planszy przeciwnika

		// GRACZ: król w œrodku, 2 pionki po bokach
		SpawnPiece(kingPrefab, BoardType.Player, playerFrontRow, midCol, PieceOwner.Player, PieceType.King);

		if (cols >= 3)
		{
			SpawnPiece(pawnPrefab, BoardType.Player, playerFrontRow, midCol - 1, PieceOwner.Player, PieceType.Pawn);
			SpawnPiece(pawnPrefab, BoardType.Player, playerFrontRow, midCol + 1, PieceOwner.Player, PieceType.Pawn);
		}

		// PRZECIWNIK: analogicznie
		SpawnPiece(kingPrefab, BoardType.Enemy, enemyFrontRow, midCol, PieceOwner.Enemy, PieceType.King);

		if (cols >= 3)
		{
			SpawnPiece(pawnPrefab, BoardType.Enemy, enemyFrontRow, midCol - 1, PieceOwner.Enemy, PieceType.Pawn);
			SpawnPiece(pawnPrefab, BoardType.Enemy, enemyFrontRow, midCol + 1, PieceOwner.Enemy, PieceType.Pawn);
		}
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
