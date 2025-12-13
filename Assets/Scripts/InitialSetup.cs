using System.Collections;
using UnityEngine;

public class InitialSetup : MonoBehaviour
{
	public GameObject kingPrefab;
	public GameObject pawnPrefab;

	private IEnumerator Start()
	{
		yield return new WaitUntil(() =>
			BoardManager.Instance != null && BoardManager.Instance.IsReady);

		int rows = BoardManager.Instance.PlayerRows;
		int cols = BoardManager.Instance.PlayerCols;

		int midColLocal = cols / 2;

		int playerFrontRowLocal = rows - 1;
		int enemyFrontRowLocal = 0;

		SpawnPiece(kingPrefab, BoardType.Player, playerFrontRowLocal, midColLocal, PieceOwner.Player, PieceType.King);

		if (cols >= 3)
		{
			SpawnPiece(pawnPrefab, BoardType.Player, playerFrontRowLocal, midColLocal - 1, PieceOwner.Player, PieceType.Pawn);
			SpawnPiece(pawnPrefab, BoardType.Player, playerFrontRowLocal, midColLocal + 1, PieceOwner.Player, PieceType.Pawn);
		}

		SpawnPiece(kingPrefab, BoardType.Enemy, enemyFrontRowLocal, midColLocal, PieceOwner.Enemy, PieceType.King);

		if (cols >= 3)
		{
			SpawnPiece(pawnPrefab, BoardType.Enemy, enemyFrontRowLocal, midColLocal - 1, PieceOwner.Enemy, PieceType.Pawn);
			SpawnPiece(pawnPrefab, BoardType.Enemy, enemyFrontRowLocal, midColLocal + 1, PieceOwner.Enemy, PieceType.Pawn);
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
		//sss
		piece.owner = owner;
		piece.pieceType = type;
		piece.currentTile = tile;

		tile.isOccupied = true;
		tile.currentPiece = piece;
	}
}
