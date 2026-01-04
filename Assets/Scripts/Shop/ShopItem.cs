using UnityEngine;
using TMPro;

public class ShopItem : MonoBehaviour
{
	public PieceType type;
	public int price;
	private ShopManager manager;
	private Tile myTile;
	private Color originalTileColor;
	private GameObject priceTextObj;

	public void Setup(PieceType type, int price, ShopManager manager, Tile tile, GameObject textPrefab, Vector3 textOffset)
	{
		this.type = type;
		this.price = price;
		this.manager = manager;
		this.myTile = tile;

		if (myTile != null)
		{
			originalTileColor = myTile.GetComponent<SpriteRenderer>().color;
		}

		if (textPrefab != null)
		{
			// Tworzymy obiekt tekstu
			priceTextObj = Instantiate(textPrefab, transform.position + textOffset, Quaternion.identity);
			priceTextObj.transform.SetParent(transform);

			// Pobieramy komponent TextMeshPro (Wa¿ne: nie UGUI!)
			TextMeshPro tmp = priceTextObj.GetComponent<TextMeshPro>();

			if (tmp != null)
			{
				// 1. Ustawiamy tekst
				tmp.text = price.ToString();

				// 2. Formatowanie
				tmp.fontSize = 5; // Mo¿esz zwiêkszyæ jeœli jest za ma³e
				tmp.alignment = TextAlignmentOptions.Center;
				tmp.color = Color.yellow;

				// 3. KLUCZOWA POPRAWKA - WARSTWY I KOLEJNOŒÆ
				// Ustawiamy Sorting Order na bardzo wysoki, ¿eby by³ nad wszystkim
				tmp.GetComponent<MeshRenderer>().sortingOrder = 500;

				// Dla pewnoœci przysuwamy go te¿ do kamery (Z = -2)
				Vector3 fixPos = priceTextObj.transform.position;
				fixPos.z = -2f;
				priceTextObj.transform.position = fixPos;
			}
			else
			{
				Debug.LogError("B£¥D: Twój prefab ceny nie ma komponentu 'TextMeshPro'! Upewnij siê, ¿e u¿y³eœ '3D Object -> Text - TextMeshPro', a nie UI.");
			}
		}
	}

	private void Update()
	{
		if (myTile == null) return;

		SpriteRenderer tileSr = myTile.GetComponent<SpriteRenderer>();

		if (GameProgress.Instance.coins < price)
		{
			// Brak kasy -> Czerwony
			tileSr.color = new Color(1f, 0.3f, 0.3f);
		}
		else
		{
			// Jest kasa -> Normalny
			tileSr.color = originalTileColor;
		}
	}

	private void OnMouseDown()
	{
		manager.TryBuyPiece(this);
	}

	private void OnDestroy()
	{
		if (myTile != null)
		{
			myTile.GetComponent<SpriteRenderer>().color = originalTileColor;
		}
	}
}