using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
	public PieceOwner owner;
	public PieceType pieceType;

	public Tile currentTile;

	private Vector3 dragStartPosition;
	private List<Tile> legalMoves = new List<Tile>();

	private Vector3 originalPosition;

	void Start()
	{
		originalPosition = transform.position;
	}

	void OnMouseDown()
	{
		if (!GameManager.Instance.CanPieceMove(this))
			return;

		dragStartPosition = transform.position;
		GetLegalMoves();
	}

	void OnMouseDrag()
	{
		Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		mouseWorld.z = -1;
		transform.position = mouseWorld;
	}

	void OnMouseUp()
	{
		Tile nearest = FindNearestTile();

		if (nearest != null && legalMoves.Contains(nearest))
		{
			MoveToTile(nearest);
		}
		else
		{
			transform.position = dragStartPosition;
		}
	}

	Tile FindNearestTile()
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

		if (owner == PieceOwner.Player)
			GameManager.Instance.EndPlayerMove();
		else
			GameManager.Instance.EndEnemyMove();
	}

	// liczy legalne ruchy
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

			default:
				break;
		}

		return legalMoves;
	}

	// ruchy jednopolowe globalRow + GetTileGlobal
	private void AddStepMoves(Vector2Int[] dirs)
	{
		foreach (var d in dirs)
		{
                        int gRow = currentTile.globalRow + d.x;
                        int col = currentTile.globalCol + d.y;

			Tile target = BoardManager.Instance.GetTileGlobal(gRow, col);

			if (target == null) continue;
			if (target.isOccupied) continue;

			legalMoves.Add(target);
		}
	}

	// ruchy piona  do przodu o 1 z globalRow
	private void AddPawnMoves()
	{
		int forward = (owner == PieceOwner.Player) ? 1 : -1;

                int gRow = currentTile.globalRow + forward;
                int col = currentTile.globalCol;

		Tile target = BoardManager.Instance.GetTileGlobal(gRow, col);

		if (target == null) return;
		if (target.isOccupied) return;

		legalMoves.Add(target);
	}

	// zestaw ruchow pionow

	private static readonly Vector2Int[] knightMoves = new Vector2Int[]
	{
		new Vector2Int(1, 2),
		new Vector2Int(2, 1),
		new Vector2Int(-1, 2),
		new Vector2Int(-2, 1),
		new Vector2Int(1, -2),
		new Vector2Int(2, -1),
		new Vector2Int(-1, -2),
		new Vector2Int(-2, -1)
	};

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

	private static readonly Vector2Int[] bishopDirections = new Vector2Int[]
	{
		new Vector2Int(1, 1),
		new Vector2Int(1, -1),
		new Vector2Int(-1, 1),
		new Vector2Int(-1, -1)
	};

	private static readonly Vector2Int[] rookDirections = new Vector2Int[]
	{
		new Vector2Int(1, 0),
		new Vector2Int(0, 1),
		new Vector2Int(-1, 0),
		new Vector2Int(0, -1)
	};

	private static readonly Vector2Int[] queenDirections = new Vector2Int[]
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

	// ai enemy

	public void MoveToTileFromAI(Tile target)
	{
		MoveInternal(target);
		GameManager.Instance.EndEnemyMove();
	}

	private void MoveInternal(Tile target)
	{
		if (target.isOccupied && target.currentPiece != null)
		{
			Piece other = target.currentPiece;

			if (other.owner != this.owner)
			{
				if (other.pieceType == PieceType.King)
				{
					bool playerWon = this.owner == PieceOwner.Player;
					GameManager.Instance.GameOver(playerWon);
				}

				Object.Destroy(other.gameObject);
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
