using UnityEngine;

public class PieceMovement : MonoBehaviour
{
	private Vector3 startPosition;
	private bool isDragging = false;
	private Piece pieceComponent;

	// Sortowanie warstw, ¿eby podnoszona figura by³a nad innymi
	private int originalSortingOrder;
	private SpriteRenderer sr;

	void Start()
	{
		pieceComponent = GetComponent<Piece>();
		sr = GetComponent<SpriteRenderer>();
	}

	void OnMouseDown()
	{
		// Pozwalamy ruszaæ tylko naszymi figurami
		if (pieceComponent != null && pieceComponent.owner == PieceOwner.Player)
		{
			isDragging = true;
			startPosition = transform.position;

			if (sr)
			{
				originalSortingOrder = sr.sortingOrder;
				sr.sortingOrder = 100; // Figurka na wierzch
			}
			transform.localScale *= 1.1f; // Lekkie powiêkszenie
		}
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

		if (sr) sr.sortingOrder = originalSortingOrder;
		transform.localScale /= 1.1f;

		Vector3 dropPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Tile hitTile = BoardManager.Instance.GetTileAtPosition(dropPos);

		if (hitTile != null)
		{
			// Mo¿emy postawiæ figurê jeœli:
			// 1. To plansza gracza LUB Ekwipunek
			// 2. Kafelek jest pusty
			bool validBoard = (hitTile.boardType == BoardType.Player || hitTile.isInventory);

			if (validBoard && !hitTile.isOccupied)
			{
				MoveToTile(hitTile);
				return;
			}
		}

		// Jeœli upuœciliœmy w z³ym miejscu -> wracamy na start
		transform.position = startPosition;
	}

	void MoveToTile(Tile newTile)
	{
		// Zwolnij stary kafelek
		if (pieceComponent.currentTile != null)
		{
			pieceComponent.currentTile.isOccupied = false;
			pieceComponent.currentTile.currentPiece = null;
		}

		// Zajmij nowy
		newTile.isOccupied = true;
		newTile.currentPiece = pieceComponent;
		pieceComponent.currentTile = newTile;

		// Ustaw pozycjê
		transform.position = new Vector3(newTile.transform.position.x, newTile.transform.position.y, -1);
		// Aktualizuj startPosition na wypadek kolejnego ruchu
		startPosition = transform.position;
	}
}