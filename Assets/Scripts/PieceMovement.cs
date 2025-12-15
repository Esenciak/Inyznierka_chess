using UnityEngine;

public class PieceMovement : MonoBehaviour
{
	private Vector3 startPosition;
	private bool isDragging = false;
	private Piece pieceComponent;

	void Start()
	{
		pieceComponent = GetComponent<Piece>();
	}

	void OnMouseDown()
	{
		// Pozwól przesuwaæ tylko swoje figury
		if (pieceComponent.owner == PieceOwner.Player)
		{
			isDragging = true;
			startPosition = transform.position;
			// Opcjonalnie: podnieœ lekko figurê wizualnie
		}
	}

	void OnMouseDrag()
	{
		if (isDragging)
		{
			// Pod¹¿aj za myszk¹
			Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			transform.position = new Vector3(mousePos.x, mousePos.y, -2); // -2 ¿eby byæ nad wszystkim
		}
	}

	void OnMouseUp()
	{
		if (!isDragging) return;
		isDragging = false;

		// Strzelamy promieniem, ¿eby zobaczyæ co jest pod myszk¹
		Vector3 dropPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D hit = Physics2D.Raycast(dropPos, Vector2.zero);

		if (hit.collider != null)
		{
			Tile tile = hit.collider.GetComponent<Tile>();

			// Logika upuszczania:
			// 1. Czy to jest kafelek?
			// 2. Czy kafelek jest wolny?
			// 3. Czy to strefa gracza (BoardType.Player)?
			if (tile != null && !tile.isOccupied && tile.boardType == BoardType.Player)
			{
				// Aktualizuj logicznie
				if (pieceComponent.currentTile != null)
				{
					pieceComponent.currentTile.isOccupied = false;
					pieceComponent.currentTile.currentPiece = null;
				}

				// Ustaw now¹ pozycjê
				transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);

				// Przypisz do kafelka
				tile.isOccupied = true;
				tile.currentPiece = pieceComponent;
				pieceComponent.currentTile = tile;
				return; // Sukces
			}
		}

		// Jeœli puszczono w z³ym miejscu, wróæ na start
		transform.position = startPosition;
	}
}