using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BattleLoader : MonoBehaviour
{
	[Header("Lista Prefabów (Musi pasowaæ kolejnoœci¹ do Enuma PieceType!)")]
	// Kolejnoœæ: 0:Pawn, 1:Knight, 2:Bishop, 3:Rook, 4:Queen, 5:King
	public GameObject[] piecePrefabs;

	private void Start()
	{
		// Sprawdzamy czy to na pewno scena Battle
		if (SceneManager.GetActiveScene().name == "Battle")
		{
			// Czekamy u³amek sekundy, a¿ BoardManager zbuduje planszê (kafle musz¹ istnieæ, ¿eby postawiæ na nich figury)
			Invoke("LoadArmyFromSave", 0.1f);
		}
	}

	void LoadArmyFromSave()
	{
		// Pobieramy zapisan¹ listê
		List<SavedPieceData> savedArmy = GameProgress.Instance.myArmy;

		if (savedArmy.Count == 0)
		{
			Debug.LogWarning("Brak zapisanej armii! Czy wyszed³eœ ze sklepu przyciskiem Start?");
			return;
		}

		Debug.Log($"Wczytujê {savedArmy.Count} figur...");

		foreach (var data in savedArmy)
		{
			// 1. ZnajdŸ kafelek na planszy BITWY, który odpowiada zapisanym koordynatom
			Tile tile = BoardManager.Instance.GetTile(BoardType.Player, data.y, data.x);

			if (tile != null)
			{
				// 2. ZnajdŸ odpowiedni prefab (np. Wie¿ê)
				GameObject prefab = GetPrefabByType(data.type);

				if (prefab != null)
				{
					// 3. Stwórz fizyczn¹ figurê
					Vector3 pos = tile.transform.position;
					pos.z = -1; // Ustawiamy na wierzchu (przed kafelkiem)
					GameObject go = Instantiate(prefab, pos, Quaternion.identity);

					// 4. Ustaw logikê (W³aœciciel, Typ, Przypisanie do kafelka)
					Piece piece = go.GetComponent<Piece>();
					piece.owner = PieceOwner.Player;
					piece.pieceType = data.type;
					piece.currentTile = tile;

					tile.isOccupied = true;
					tile.currentPiece = piece;

					// Opcjonalnie: W bitwie mo¿esz usun¹æ komponent ruchu, ¿eby gracz nie przesuwa³ figur myszk¹
					// Destroy(go.GetComponent<PieceMovement>());
				}
			}
		}
	}

	// Pomocnicza funkcja do wyboru prefabu
	GameObject GetPrefabByType(PieceType type)
	{
		// Upewnij siê, ¿e w Inspektorze masz te prefaby przypisane w tej kolejnoœci!
		switch (type)
		{
			case PieceType.Pawn: return piecePrefabs[0];
			case PieceType.Knight: return piecePrefabs[1];
			case PieceType.Bishop: return piecePrefabs[2];
			case PieceType.Rook: return piecePrefabs[3];
			case PieceType.queen: return piecePrefabs[4]; // Uwaga na wielkoœæ liter w Enumie
			case PieceType.King: return piecePrefabs[5];
		}
		return piecePrefabs[0]; // Domyœlnie pionek, jak coœ pójdzie nie tak
	}
}