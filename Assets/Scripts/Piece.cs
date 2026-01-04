using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
	public PieceOwner owner;
	public PieceType pieceType;
	public Tile currentTile;

	private void Start()
	{
		// Ustawienie Z na starcie
		Vector3 pos = transform.position;
		pos.z = -1f;
		transform.position = pos;
	}

	// --- G£ÓWNA METODA: Zwraca listê pól, na które ta figura mo¿e wejœæ ---
	public List<Tile> GetLegalMoves()
	{
		List<Tile> moves = new List<Tile>();
		if (currentTile == null) return moves;

		int row = currentTile.globalRow;
		int col = currentTile.globalCol;

		switch (pieceType)
		{
			case PieceType.Pawn:
				CalculatePawnMoves(row, col, moves);
				break;
			case PieceType.Rook:
				MoveLine(row, col, 1, 0, moves);  // Prawo
				MoveLine(row, col, -1, 0, moves); // Lewo
				MoveLine(row, col, 0, 1, moves);  // Góra
				MoveLine(row, col, 0, -1, moves); // Dó³
				break;
			case PieceType.Bishop:
				MoveLine(row, col, 1, 1, moves);
				MoveLine(row, col, 1, -1, moves);
				MoveLine(row, col, -1, 1, moves);
				MoveLine(row, col, -1, -1, moves);
				break;
			case PieceType.queen:
				MoveLine(row, col, 1, 0, moves); MoveLine(row, col, -1, 0, moves);
				MoveLine(row, col, 0, 1, moves); MoveLine(row, col, 0, -1, moves);
				MoveLine(row, col, 1, 1, moves); MoveLine(row, col, 1, -1, moves);
				MoveLine(row, col, -1, 1, moves); MoveLine(row, col, -1, -1, moves);
				break;
			case PieceType.Knight:
				MovePoint(row + 2, col + 1, moves); MovePoint(row + 2, col - 1, moves);
				MovePoint(row - 2, col + 1, moves); MovePoint(row - 2, col - 1, moves);
				MovePoint(row + 1, col + 2, moves); MovePoint(row + 1, col - 2, moves);
				MovePoint(row - 1, col + 2, moves); MovePoint(row - 1, col - 2, moves);
				break;
			case PieceType.King:
				MovePoint(row + 1, col, moves); MovePoint(row - 1, col, moves);
				MovePoint(row, col + 1, moves); MovePoint(row, col - 1, moves);
				MovePoint(row + 1, col + 1, moves); MovePoint(row + 1, col - 1, moves);
				MovePoint(row - 1, col + 1, moves); MovePoint(row - 1, col - 1, moves);
				break;
		}
		return moves;
	}

	// --- HELPERY DO RUCHÓW ---

	private void CalculatePawnMoves(int row, int col, List<Tile> moves)
	{
		// Player idzie w górê (+1), Enemy w dó³ (-1)
		int dir = (owner == PieceOwner.Player) ? 1 : -1;

		// Ruch o 1
		Tile f1 = BoardManager.Instance.GetTileGlobal(row + dir, col);
		if (f1 != null && !f1.isOccupied)
		{
			moves.Add(f1);

			// Ruch o 2 (tylko na starcie)
			// Uproszczone: Player startuje z do³u, Enemy z góry
			bool isStart = (owner == PieceOwner.Player && row <= 1) ||
						   (owner == PieceOwner.Enemy && row >= BoardManager.Instance.totalRows - 2);

			if (isStart)
			{
				Tile f2 = BoardManager.Instance.GetTileGlobal(row + dir * 2, col);
				if (f2 != null && !f2.isOccupied) moves.Add(f2);
			}
		}
		// Atak pionka (na ukos)
		CheckPawnAttack(row + dir, col + 1, moves);
		CheckPawnAttack(row + dir, col - 1, moves);
	}

	private void CheckPawnAttack(int r, int c, List<Tile> moves)
	{
		Tile t = BoardManager.Instance.GetTileGlobal(r, c);
		// Atakujemy tylko, jeœli jest tam wróg
		if (t != null && t.isOccupied && t.currentPiece != null && t.currentPiece.owner != owner)
			moves.Add(t);
	}

	private void MoveLine(int row, int col, int xDir, int yDir, List<Tile> moves)
	{
		int currRow = row + yDir;
		int currCol = col + xDir;
		while (true)
		{
			Tile t = BoardManager.Instance.GetTileGlobal(currRow, currCol);
			if (t == null) break; // Koniec planszy

			if (!t.isOccupied)
			{
				moves.Add(t);
			}
			else
			{
				// Jeœli wróg -> dodajemy jako cel ataku i przerywamy liniê
				if (t.currentPiece.owner != owner) moves.Add(t);
				break; // Blokada
			}
			currRow += yDir; currCol += xDir;
		}
	}

	private void MovePoint(int r, int c, List<Tile> moves)
	{
		Tile t = BoardManager.Instance.GetTileGlobal(r, c);
		// Jeœli pole istnieje I (jest wolne LUB zajête przez wroga)
		if (t != null && (!t.isOccupied || (t.currentPiece != null && t.currentPiece.owner != owner)))
			moves.Add(t);
	}
}