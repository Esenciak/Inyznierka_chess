using UnityEngine;

public class PieceMovement : MonoBehaviour
{
	private Vector3 startPosition;
	private bool isDragging = false;
	private Piece pieceComponent;

	// Do obs³ugi wygl¹du (¿eby figura by³a "nad" innymi podczas przeci¹gania)
	private SpriteRenderer sr;
	private int originalOrder;

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

			// Wizualny efekt podniesienia
			if (sr)
			{
				originalOrder = sr.sortingOrder;
				sr.sortingOrder = 100; // Na wierzch
			}
			transform.localScale *= 1.1f; // Lekkie powiêkszenie
		}
	}

	void OnMouseDrag()
	{
		if (isDragging)
		{
			// Przesuwamy figurê za kursorem
			Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			transform.position = new Vector3(mousePos.x, mousePos.y, -5);
		}
	}

	void OnMouseUp()
	{
		if (!isDragging) return;
		isDragging = false;

		// Reset wizualny
		if (sr) sr.sortingOrder = originalOrder;
		transform.localScale /= 1.1f;

		// --- KLUCZOWA POPRAWKA: RaycastAll ---
		// Strzelamy promieniem, przebijaj¹c figurê, któr¹ trzymamy, ¿eby znaleŸæ kafelek pod spodem
		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

		Tile targetTile = null;

		foreach (var hit in hits)
		{
			Tile t = hit.collider.GetComponent<Tile>();
			if (t != null)
			{
				targetTile = t;
				break; // ZnaleŸliœmy kafelek!
			}
		}

		// Jeœli trafiliœmy na kafelek...
		if (targetTile != null)
		{
			// Sprawdzamy czy to legalne miejsce (Plansza Gracza LUB Inventory)
			bool isAllowedBoard = (targetTile.boardType == BoardType.Player || targetTile.isInventory);

			// Oraz czy kafelek jest wolny
			if (isAllowedBoard && !targetTile.isOccupied)
			{
				MoveToTile(targetTile);
				return; // Sukces, koñczymy
			}
		}

		// Jeœli upuœciliœmy w z³ym miejscu (np. na zajête pole, albo poza planszê) -> wracamy
		transform.position = startPosition;
	}

	void MoveToTile(Tile newTile)
	{
		// 1. Zwalniamy stary kafelek
		if (pieceComponent.currentTile != null)
		{
			pieceComponent.currentTile.isOccupied = false;
			pieceComponent.currentTile.currentPiece = null;
		}

		// 2. Zajmujemy nowy
		newTile.isOccupied = true;
		newTile.currentPiece = pieceComponent;
		pieceComponent.currentTile = newTile;

		// 3. Ustawiamy pozycjê
		transform.position = new Vector3(newTile.transform.position.x, newTile.transform.position.y, -1);

		// Aktualizuj startPosition, ¿eby przy kolejnym ruchu wraca³ w to nowe miejsce
		startPosition = transform.position;
	}
}