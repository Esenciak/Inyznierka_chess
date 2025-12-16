using UnityEngine;
using UnityEngine.SceneManagement;

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
		// 1. Sprawdzamy czy to figura gracza
		if (pieceComponent.owner != PieceOwner.Player) return;

		// 2. Jeœli to BITWA, sprawdzamy czy jest NASZA TURA
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
			sr.sortingOrder = 100; // Na wierzch
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
			// --- LOGIKA WALIDACJI RUCHU ---
			bool isBattle = SceneManager.GetActiveScene().name == "Battle";
			bool isAllowed = false;

			if (isBattle)
			{
				// W bitwie mo¿na iœæ na: Swoje, Œrodek i Wroga (Atak)
				// UWAGA: Tu w przysz³oœci dodasz logikê "Czy ruch jest zgodny z zasadami szachów"
				if (targetTile.boardType == BoardType.Player ||
					targetTile.boardType == BoardType.Center ||
					targetTile.boardType == BoardType.Enemy)
				{
					isAllowed = true;
				}
			}
			else // SKLEP
			{
				// W sklepie tylko: Inventory lub Plansza Gracza
				if (targetTile.isInventory || targetTile.boardType == BoardType.Player)
				{
					isAllowed = true;
				}
			}

			// Jeœli miejsce dozwolone i wolne (lub atakujemy - tu prosta wersja tylko na wolne)
			if (isAllowed && !targetTile.isOccupied)
			{
				MoveToTile(targetTile);

				// Jeœli to bitwa -> ZMIEÑ TURÊ
				if (isBattle)
				{
					GameManager.Instance.SwitchTurn();
				}
				return;
			}
		}

		// Nieudany ruch -> powrót
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