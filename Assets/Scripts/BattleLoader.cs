using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleLoader : MonoBehaviour
{
	// Przypisz te same prefaby co w ShopManager + KRÓL!
	public GameObject[] piecePrefabs; // 0:Pawn, 1:Knight, 2:Bishop, 3:Rook, 4:Queen, 5:King

	private void Start()
	{
		// Uruchamiamy tylko w scenie Battle
		if (SceneManager.GetActiveScene().name == "Battle")
		{
			// Czekamy u³amek sekundy, a¿ BoardManager stworzy kafelki
			Invoke("LoadArmyFromSave", 0.1f);
		}
	}

	void LoadArmyFromSave()
	{
		if (GameProgress.Instance.savedArmy.Count == 0)
		{
			Debug.LogWarning("Brak zapisanej armii! Czy wyszed³eœ ze sklepu przyciskiem Start?");
			return;
		}

		Debug.Log("Wczytujê armiê...");

		foreach (var data in GameProgress.Instance.savedArmy)
		{
			// 1. ZnajdŸ kafelek
			Tile tile = BoardManager.Instance.GetTile(BoardType.Player, data.row, data.col);

			if (tile != null)
			{
				// 2. ZnajdŸ prefab
				GameObject prefab = GetPrefabByType(data.type);

				if (prefab != null)
				{
					// 3. Stwórz figurê
					Vector3 pos = tile.transform.position;
					pos.z = -1; // Na wierzchu
					GameObject go = Instantiate(prefab, pos, Quaternion.identity);

					// 4. Ustaw dane logiczne
					Piece piece = go.GetComponent<Piece>();
					piece.owner = PieceOwner.Player;
					piece.pieceType = data.type;
					piece.currentTile = tile;

					tile.isOccupied = true;
					tile.currentPiece = piece;

					// Jeœli nie chcesz, ¿eby w bitwie da³o siê przesuwaæ figury rêk¹,
					// mo¿esz usun¹æ komponent ruchu:
					// Destroy(go.GetComponent<PieceMovement>());
				}
			}
		}
	}

	GameObject GetPrefabByType(PieceType type)
	{
		// Dopasuj indeksy do swojej tablicy w Inspektorze!
		switch (type)
		{
			case PieceType.Pawn: return piecePrefabs[0];
			case PieceType.Knight: return piecePrefabs[1];
			case PieceType.Bishop: return piecePrefabs[2];
			case PieceType.Rook: return piecePrefabs[3];
			case PieceType.queen: return piecePrefabs[4]; // Uwaga na ma³¹/wielk¹ literê w enumie
			case PieceType.King: return piecePrefabs[5];
		}
		return piecePrefabs[0];
	}
}