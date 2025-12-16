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
		if (GameManager.Instance == null) return;

		bool canInteract = false;

		// Sprawdzamy Fazy Gry
		if (GameManager.Instance.currentPhase == GamePhase.Battle)
		{
			// W walce: tylko w swojej turze i swoje pionki
			// POPRAWKA: U¿ywamy PieceOwner zamiast Turn
			if (GameManager.Instance.currentTurn == PieceOwner.Player && owner == PieceOwner.Player)
				canInteract = true;
		}
		else if (GameManager.Instance.currentPhase == GamePhase.Placement)
		{
			// W sklepie: zawsze swoje pionki
			if (owner == PieceOwner.Player)
				canInteract = true;
		}

		if (!canInteract) return;

		isDragging = true;
		startPosition = transform.position;
		startTile = currentTile;

		// Oblicz offset, ¿eby figura nie "skaka³a" do œrodka myszki
		Vector3 mouseWorldPos = GetMouseWorldPos();
		dragOffset = transform.position - mouseWorldPos;

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

		// U¿ywamy GetTileAtPosition z BoardManagera (który ma RaycastAll)
		// Jeœli nie masz tej metody w BoardManagerze, u¿yj Raycasta tutaj
		Tile targetTile = BoardManager.Instance.GetTileAtPosition(GetMouseWorldPos());

		// Jeœli BoardManager nie ma GetTileAtPosition, odkomentuj to:
		/*
        Tile targetTile = null;
        RaycastHit2D[] hits = Physics2D.RaycastAll(GetMouseWorldPos(), Vector2.zero);
        foreach(var hit in hits) {
            Tile t = hit.collider.GetComponent<Tile>();
            if(t != null) { targetTile = t; break; }
        }
        */

		if (GameManager.Instance.currentPhase == GamePhase.Placement)
		{
			HandlePlacementDrop(targetTile);
		}
		else if (GameManager.Instance.currentPhase == GamePhase.Battle)
		{
			HandleBattleDrop(targetTile);
		}
	}

	// --- LOGIKA UPUSZCZANIA: SKLEP ---
	private void HandlePlacementDrop(Tile target)
	{
		// Jeœli upuszczono w kosmos lub na zajête (chyba ¿e to samo pole)
		if (target == null || (target.isOccupied && target != startTile))
		{
			ResetToStart();
			return;
		}

		// Król nie do Inventory
		if (pieceType == PieceType.King && target.isInventory)
		{
			Debug.Log("Król nie mo¿e zejœæ do ekwipunku!");
			ResetToStart();
			return;
		}

		// Tylko pola gracza
		if (target.boardType != BoardType.Player && !target.isInventory)
		{
			Debug.Log("Mo¿esz rozstawiaæ tylko na swoim polu!");
			ResetToStart();
			return;
		}

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
					Destroy(target.currentPiece.gameObject);

					if (target.currentPiece.pieceType == PieceType.King)
					{
						GameManager.Instance.GameOver(owner == PieceOwner.Player);
						return;
					}
				}
			}

			MovePieceProcess(target);
			GameManager.Instance.EndPlayerMove();
		}
		else
		{
			ResetToStart();
		}
	}

	private void MovePieceProcess(Tile target)
	{
		// Zwolnij stare pole
		if (currentTile != null)
		{
			currentTile.currentPiece = null;
			currentTile.isOccupied = false;
		}

		// Zajmij nowe
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

	// --- RUCHY SZACHOWE ---
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
				MoveLine(row, col, 1, 0, moves);
				MoveLine(row, col, -1, 0, moves);
				MoveLine(row, col, 0, 1, moves);
				MoveLine(row, col, 0, -1, moves);
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

	private void CalculatePawnMoves(int row, int col, List<Tile> moves)
	{
		int dir = (owner == PieceOwner.Player) ? 1 : -1;

		// Ruch do przodu o 1
		Tile f1 = BoardManager.Instance.GetTileGlobal(row + dir, col);
		if (f1 != null && !f1.isOccupied)
		{
			moves.Add(f1);

			// Ruch o 2 (na start)
			// Zak³adamy, ¿e gracz startuje w rzêdach 0 i 1 (dla BoardType.Player), a wróg na górze
			// Uproszczony warunek: jeœli jeszcze siê nie rusza³ (opcjonalnie mo¿na dodaæ flagê hasMoved)
			// Tutaj prosta logika oparta na pozycji
			bool isStart = false;
			if (owner == PieceOwner.Player && row <= 1) isStart = true;
			// Dla wroga trzeba by sprawdziæ BoardManager.totalRows - 2, ale to zale¿y od mapy

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
			if (t == null) break; // Koniec planszy

			if (!t.isOccupied)
			{
				moves.Add(t);
			}
			else
			{
				// Jeœli wróg - mo¿na biæ i koniec ruchu
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
		if (t != null && (!t.isOccupied || t.currentPiece.owner != owner))
			moves.Add(t);
	}

	private void HighlightMoves(bool show)
	{
		foreach (var tile in possibleMoves)
		{
			// Zak³adam, ¿e w Tile masz metodê SetHighlight. 
			// Jeœli nie, musisz zmieniæ kolor rêcznie, np: tile.GetComponent<SpriteRenderer>().color = ...
			if (show) tile.GetComponent<SpriteRenderer>().color = Color.yellow;
			else tile.GetComponent<SpriteRenderer>().color = tile.originalColor; // Musisz mieæ zapamiêtany kolor w Tile
		}
	}
}