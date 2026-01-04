using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PieceMovement : MonoBehaviour
{
	private Vector3 startPosition;
	private bool isDragging = false;
	private Piece pieceComponent;
	private SpriteRenderer sr;
	private int originalOrder;

	void Start()
	{
		pieceComponent = GetComponent<Piece>();
		sr = GetComponent<SpriteRenderer>();
	}

	void OnMouseDown()
	{
		if (pieceComponent.owner != PieceOwner.Player) return;

		if (SceneManager.GetActiveScene().name == "Battle")
		{
			if (GameManager.Instance.currentTurn != PieceOwner.Player)
			{
				Debug.Log("To nie twoja tura!");
				return;
			}
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

		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

		Tile targetTile = null;
		foreach (var hit in hits)
		{
			Tile t = hit.collider.GetComponent<Tile>();
			if (t != null)
			{
				targetTile = t;
				break;
			}
		}

		if (targetTile != null)
		{
			// Sprawdzamy, w jakiej jesteœmy scenie
			bool isBattle = SceneManager.GetActiveScene().name == "Battle";

			// --- WALIDACJA DLA SKLEPU ---
			if (!isBattle)
			{
				// Król nie mo¿e do inventory
				if (pieceComponent.pieceType == PieceType.King && targetTile.isInventory)
				{
					Debug.Log("Król nie mo¿e do inventory!");
					transform.position = startPosition;
					return;
				}

				// W sklepie mo¿na przestawiaæ dowolnie (Inventory <-> Plansza Gracza)
				// pod warunkiem, ¿e pole jest wolne
				if ((targetTile.isInventory || targetTile.boardType == BoardType.Player) && !targetTile.isOccupied)
				{
					MoveToTile(targetTile);
					return;
				}
			}
			// --- WALIDACJA DLA BITWY ---
			else
			{
				// 1. Sprawdzamy czy to legalny ruch szachowy
				List<Tile> legalMoves = pieceComponent.GetLegalMoves();

				if (legalMoves.Contains(targetTile))
				{
					// 2. Obs³uga bicia (jeœli na polu stoi wróg)
					if (targetTile.isOccupied && targetTile.currentPiece != null)
					{
						if (targetTile.currentPiece.owner != pieceComponent.owner)
						{
							Destroy(targetTile.currentPiece.gameObject);

							// Jeœli zbiliœmy Króla -> wygrana
							if (targetTile.currentPiece.pieceType == PieceType.King)
							{
								GameManager.Instance.GameOver(true);
							}
						}
					}

					// 3. Wykonaj ruch
					MoveToTile(targetTile);
					GameManager.Instance.SwitchTurn();
					return;
				}
				else
				{
					Debug.Log("Nielegalny ruch!");
				}
			}
		}

		// Jeœli ruch siê nie uda³ -> wracamy
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

		transform.position = new Vector3(newTile.transform.position.x, newTile.transform.position.y, -1);
		startPosition = transform.position;
	}
}