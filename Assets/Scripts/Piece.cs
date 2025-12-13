using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
	public PieceOwner owner;
	public PieceType pieceType;

	public Tile currentTile;

	private Vector3 dragStartPosition;
	private Tile startTile;

	private List<Tile> legalMoves = new List<Tile>();

	private void OnMouseDown()
	{
		if (GameManager.Instance != null && !GameManager.Instance.CanPieceMove(this))
			return;

		startTile = currentTile;
		dragStartPosition = transform.position;
		GetLegalMoves();
	}

	private void OnMouseDrag()
	{
		Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		mouseWorld.z = -1;
		transform.position = mouseWorld;
	}

	private void OnMouseUp()
	{
		Tile nearest = FindNearestTile();

		if (nearest != null && legalMoves.Contains(nearest))
		{
			MoveToTile(nearest);
		}
		else
		{
			transform.position = dragStartPosition;
			currentTile = startTile;
		}
	}

	private Tile FindNearestTile()
	{
		Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
		Tile nearest = null;
		float minDist = Mathf.Infinity;

		foreach (Tile t in allTiles)
		{
			float dist = Vector2.Distance(transform.position, t.transform.position);
			if (dist < minDist)
			{
				minDist = dist;
				nearest = t;
			}
		}

		return (minDist < 1.5f) ? nearest : null;
	}

	private void MoveToTile(Tile target)
	{
		MoveInternal(target);

		if (GameManager.Instance == null) return;

		if (owner == PieceOwner.Player) GameManager.Instance.EndPlayerMove();
		else GameManager.Instance.EndEnemyMove();
	}

	public List<Tile> GetLegalMoves()
	{
		legalMoves.Clear();
		if (currentTile == null) return legalMoves;

		switch (pieceType)
		{
			case PieceType.King:
				AddStepMoves(kingDirs);
				break;
			case PieceType.Pawn:
				AddPawnMoves();
				break;
		}

		return legalMoves;
	}

	private void AddStepMoves(Vector2Int[] dirs)
	{
		foreach (var d in dirs)
		{
			int gr = currentTile.globalRow + d.x;
			int gc = currentTile.globalCol + d.y;

			Tile target = BoardManager.Instance.GetTileGlobal(gr, gc);
			if (target == null) continue;
			if (target.isOccupied) continue;

			legalMoves.Add(target);
		}
	}

	private void AddPawnMoves()
	{
		int forward = (owner == PieceOwner.Player) ? 1 : -1;

		int gr = currentTile.globalRow + forward;
		int gc = currentTile.globalCol;

		Tile target = BoardManager.Instance.GetTileGlobal(gr, gc);
		if (target == null) return;
		if (target.isOccupied) return;

		legalMoves.Add(target);
	}

	private static readonly Vector2Int[] kingDirs = new Vector2Int[]
	{
		new Vector2Int(1, 0),
		new Vector2Int(0, 1),
		new Vector2Int(-1, 0),
		new Vector2Int(0, -1),
		new Vector2Int(1, 1),
		new Vector2Int(1, -1),
		new Vector2Int(-1, 1),
		new Vector2Int(-1, -1)
	};



	public void MoveToTileFromAI(Tile target)
	{
		MoveInternal(target);
		if (GameManager.Instance != null) GameManager.Instance.EndEnemyMove();
	}

	private void MoveInternal(Tile target)
	{
		if (target.isOccupied && target.currentPiece != null)
		{
			Piece other = target.currentPiece;

			if (other.owner != this.owner)
			{
				if (GameManager.Instance != null && other.pieceType == PieceType.King)
				{
					bool playerWon = this.owner == PieceOwner.Player;
					GameManager.Instance.GameOver(playerWon);
				}

				Destroy(other.gameObject);
			}
		}

		if (currentTile != null)
		{
			currentTile.isOccupied = false;
			currentTile.currentPiece = null;
		}

		currentTile = target;
		currentTile.isOccupied = true;
		currentTile.currentPiece = this;

		transform.position = currentTile.transform.position;
	}
}
