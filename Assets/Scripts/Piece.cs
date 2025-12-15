using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
	public PieceOwner owner;
	public PieceType pieceType;

	public Tile currentTile;

	private Vector3 startPosition;
	private Tile startTile;
	private bool isDragging = false;
	private Vector3 dragOffset;

	private List<Tile> possibleMoves = new List<Tile>();

	private void Start()
	{
		UpdateZPosition();
	}

	private void OnMouseDown()
	{
		// 1. Sprawdzenie uprawnieñ w zale¿noœci od Fazy Gry
		if (GameManager.Instance == null) return;

		bool canInteract = false;

		if (GameManager.Instance.currentPhase == GamePhase.Battle)
		{
			// W walce: tylko w swojej turze i swoje pionki
			if (GameManager.Instance.currentTurn == Turn.Player && owner == PieceOwner.Player)
				canInteract = true;
		}
		else if (GameManager.Instance.currentPhase == GamePhase.Placement)
		{
			// W sklepie/rozstawianiu: zawsze swoje pionki
			if (owner == PieceOwner.Player)
				canInteract = true;
		}

		if (!canInteract) return;

		isDragging = true;
		startPosition = transform.position;
		startTile = currentTile;
		dragOffset = transform.position - GetMouseWorldPos();

		// Obliczamy ruchy tylko w fazie walki
		if (GameManager.Instance.currentPhase == GamePhase.Battle)
		{
			possibleMoves = GetLegalMoves();
			HighlightMoves(true);
		}
	}

	private void OnMouseDrag()
	{
		if (!isDragging) return;
		Vector3 newPos = GetMouseWorldPos() + dragOffset;
		newPos.z = -2f; // Unieœ nad planszê
		transform.position = newPos;
	}

	private void OnMouseUp()
	{
		if (!isDragging) return;
		isDragging = false;

		if (GameManager.Instance.currentPhase == GamePhase.Battle) HighlightMoves(false);

		Tile targetTile = BoardManager.Instance.GetTileAtPosition(GetMouseWorldPos());

		if (GameManager.Instance.currentPhase == GamePhase.Placement)
		{
			HandlePlacementDrop(targetTile);
		}
		else if (GameManager.Instance.currentPhase == GamePhase.Battle)
		{
			HandleBattleDrop(targetTile);
		}
	}

	// --- LOGIKA UPUSZCZANIA: SKLEP / ROZSTAWIANIE ---
	private void HandlePlacementDrop(Tile target)
	{
		// Jeœli upuszczono poza kafelki lub na zajêty kafelek (inny ni¿ startowy)
		if (target == null || (target.isOccupied && target != startTile))
		{
			ResetToStart();
			return;
		}

		// ZASADA: Król nie mo¿e wejœæ do Inventory
		if (pieceType == PieceType.King && target.isInventory)
		{
			Debug.Log("Król nie mo¿e zejœæ do ekwipunku!");
			ResetToStart();
			return;
		}

		// ZASADA: Dozwolone tylko pola Gracza (Plansza Gracza lub Inventory)
		// Zak³adamy, ¿e BoardType.Player to strefa gracza, a BoardType.Enemy/Center to strefy zakazane w fazie Placement
		if (target.boardType != BoardType.Player && !target.isInventory)
		{
			Debug.Log("Mo¿esz rozstawiaæ tylko na swoim polu!");
			ResetToStart();
			return;
		}

		// Ruch dozwolony - przenieœ
		MovePieceProcess(target);
	}

	// --- LOGIKA UPUSZCZANIA: BITWA ---
	private void HandleBattleDrop(Tile target)
	{
		if (target != null && possibleMoves.Contains(target))
		{
			// Bicie
			if (target.isOccupied && target.currentPiece != null)
			{
				if (target.currentPiece.owner != owner)
				{
					// PERMADEATH: Jeœli to wróg, niszczymy go. 
					// Jeœli to Twój system in¿ynierski, tutaj powinieneœ usun¹æ go z listy "GameProgress"
					Destroy(target.currentPiece.gameObject);

					if (target.currentPiece.pieceType == PieceType.King)
					{
						GameManager.Instance.GameOver(owner == PieceOwner.Player);
						return;
					}
				}
			}

			MovePieceProcess(target);

			// Koniec tury
			GameManager.Instance.EndPlayerMove();
		}
		else
		{
			ResetToStart();
		}
	}

	// Wspólna metoda fizycznego przeniesienia
	private void MovePieceProcess(Tile target)
	{
		if (currentTile != null)
		{
			currentTile.currentPiece = null;
			currentTile.isOccupied = false;
		}

		currentTile = target;
		currentTile.currentPiece = this;
		currentTile.isOccupied = true;

		transform.position = target.transform.position;
		UpdateZPosition();
	}

	private void ResetToStart()
	{
		transform.position = startPosition;
		UpdateZPosition();
		if (startTile != null) startTile.currentPiece = this;
	}

	private void UpdateZPosition()
	{
		Vector3 pos = transform.position;
		pos.z = -1f;
		transform.position = pos;
	}

	private Vector3 GetMouseWorldPos()
	{
		Vector3 m = Input.mousePosition;
		m.z = -Camera.main.transform.position.z;
		return Camera.main.ScreenToWorldPoint(m);
	}

	// --- PE£NA LOGIKA RUCHÓW SZACHOWYCH ---
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
				MoveLine(row, col, 0, 1, moves);  // Góra (w Unity Y to rows)
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

	// --- IMPLEMENTACJA MOVELINE I INNYCH (Skopiuj dok³adnie te metody) ---

	private void CalculatePawnMoves(int row, int col, List<Tile> moves)
	{
		int dir = (owner == PieceOwner.Player) ? 1 : -1;
		Tile f1 = BoardManager.Instance.GetTileGlobal(row + dir, col);
		if (f1 != null && !f1.isOccupied)
		{
			moves.Add(f1);
			// Pierwszy ruch podwójny
			bool isStart = (owner == PieceOwner.Player && row <= 1) || (owner == PieceOwner.Enemy && row >= BoardManager.Instance.totalRows - 2);
			if (isStart)
			{
				Tile f2 = BoardManager.Instance.GetTileGlobal(row + dir * 2, col);
				if (f2 != null && !f2.isOccupied) moves.Add(f2);
			}
		}
		// Bicie
		CheckPawnAttack(row + dir, col + 1, moves);
		CheckPawnAttack(row + dir, col - 1, moves);
	}

	private void CheckPawnAttack(int r, int c, List<Tile> moves)
	{
		Tile t = BoardManager.Instance.GetTileGlobal(r, c);
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
			if (t == null) break;
			if (!t.isOccupied)
			{
				moves.Add(t);
			}
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
		if (t != null && (!t.isOccupied || t.currentPiece.owner != owner))
			moves.Add(t);
	}

	private void HighlightMoves(bool show)
	{
		foreach (var tile in possibleMoves) tile.SetHighlight(show);
	}
}