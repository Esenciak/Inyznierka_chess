using UnityEngine;
using TMPro; // Jeœli u¿ywasz TextMeshPro do wyœwietlania ceny

public class ShopItem : MonoBehaviour
{
	public PieceType type;
	public int price;
	public ShopManager manager;

	// Mo¿esz tu dodaæ TextMeshPro, ¿eby wyœwietliæ cenê nad figur¹
	public TextMeshPro priceText;

	public void Setup(PieceType type, int price, ShopManager manager)
	{
		this.type = type;
		this.price = price;
		this.manager = manager;

		if (priceText != null)
			priceText.text = price.ToString() + "$";
	}

	private void OnMouseDown()
	{
		// Klikniêcie w figurê na pó³ce sklepowej = próba zakupu
		manager.TryBuyPiece(this);
	}
}