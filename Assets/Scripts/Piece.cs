using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Piece : MonoBehaviour
{
	public PieceOwner owner;
	public PieceType pieceType;
	public Tile currentTile;

	private void Start()
	{
		Vector3 pos = transform.position;
		pos.z = -1f;
		transform.position = pos;
	}

	// --- NOWA METODA DLA PIECEMOVEMENT ---
	public void ToggleHighlight(bool show)
	{
		// 1. Obliczamy gdzie mo¿emy iœæ
		List<Tile> moves = GetLegalMoves();

		// 2. Ka¿demu kafelkowi z listy mówimy "zmieñ kolor"
		foreach (Tile t in moves)
		{
			if (t != null) t.SetHighlight(show);
		}
	}

	// --- RUCHY SZACHOWE (Bez zmian) ---
	public List<Tile> GetLegalMoves()
	{
		List<Tile> moves = new List<Tile>();
		if (currentTile == null) return moves;

		int row = currentTile.globalRow;
		int col = currentTile.globalCol;

		switch (pieceType)
		{
			case PieceType.Pawn: CalculatePawnMoves(row, col, moves); break;
			case PieceType.Rook: MoveLine(row, col, 1, 0, moves); MoveLine(row, col, -1, 0, moves); MoveLine(row, col, 0, 1, moves); MoveLine(row, col, 0, -1, moves); break;
			case PieceType.Bishop: MoveLine(row, col, 1, 1, moves); MoveLine(row, col, 1, -1, moves); MoveLine(row, col, -1, 1, moves); MoveLine(row, col, -1, -1, moves); break;
			case PieceType.queen:
				MoveLine(row, col, 1, 0, moves); MoveLine(row, col, -1, 0, moves); MoveLine(row, col, 0, 1, moves); MoveLine(row, col, 0, -1, moves);
				MoveLine(row, col, 1, 1, moves); MoveLine(row, col, 1, -1, moves); MoveLine(row, col, -1, 1, moves); MoveLine(row, col, -1, -1, moves); break;
			case PieceType.Knight:
				MovePoint(row + 2, col + 1, moves); MovePoint(row + 2, col - 1, moves); MovePoint(row - 2, col + 1, moves); MovePoint(row - 2, col - 1, moves);
				MovePoint(row + 1, col + 2, moves); MovePoint(row + 1, col - 2, moves); MovePoint(row - 1, col + 2, moves); MovePoint(row - 1, col - 2, moves); break;
			case PieceType.King:
				MovePoint(row + 1, col, moves); MovePoint(row - 1, col, moves); MovePoint(row, col + 1, moves); MovePoint(row, col - 1, moves);
				MovePoint(row + 1, col + 1, moves); MovePoint(row + 1, col - 1, moves); MovePoint(row - 1, col + 1, moves); MovePoint(row - 1, col - 1, moves); break;
		}
		return moves;
	}

	private void CalculatePawnMoves(int row, int col, List<Tile> moves)
	{
		int dir = (owner == PieceOwner.Player) ? 1 : -1;
		if (NetworkManager.Singleton != null
				&& NetworkManager.Singleton.IsClient
				&& !NetworkManager.Singleton.IsHost
				&& GameManager.Instance != null
				&& GameManager.Instance.isMultiplayer)
		{
			dir *= -1;
		}
		Tile f1 = BoardManager.Instance.GetTileGlobal(row + dir, col);
		if (f1 != null && !f1.isOccupied)
		{
			moves.Add(f1);
			bool isStart = dir == 1 ? row <= 1 : row >= BoardManager.Instance.totalRows - 2;
			if (isStart)
			{
				Tile f2 = BoardManager.Instance.GetTileGlobal(row + dir * 2, col);
				if (f2 != null && !f2.isOccupied) moves.Add(f2);
			}
		}
		CheckPawnAttack(row + dir, col + 1, moves);
		CheckPawnAttack(row + dir, col - 1, moves);
	}

	private void CheckPawnAttack(int r, int c, List<Tile> moves)
	{
		Tile t = BoardManager.Instance.GetTileGlobal(r, c);
		if (t != null && t.isOccupied && t.currentPiece != null && t.currentPiece.owner != owner) moves.Add(t);
	}

	private void MoveLine(int row, int col, int xDir, int yDir, List<Tile> moves)
	{
		int currRow = row + yDir;
		int currCol = col + xDir;
		while (true)
		{
			Tile t = BoardManager.Instance.GetTileGlobal(currRow, currCol);
			if (t == null) break;
			if (!t.isOccupied) moves.Add(t);
			else
			{
				if (t.currentPiece.owner != owner) moves.Add(t);
				break;
			}
			currRow += yDir; currCol += xDir;
		}
	}

	private void MovePoint(int r, int c, List<Tile> moves)
	{
		Tile t = BoardManager.Instance.GetTileGlobal(r, c);
		if (t != null && (!t.isOccupied || (t.currentPiece != null && t.currentPiece.owner != owner))) moves.Add(t);
	}
}