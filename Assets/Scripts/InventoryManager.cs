using UnityEngine;

public class InventoryManager : MonoBehaviour
{
	public int rows = 3;
	public int cols = 3;

	public GameObject tilePrefab;       // kafelek inventory
	public GameObject pawnPrefab;       // prefab pionka
	public GameObject kingPrefab;       // prefab króla
	public Color[] inventoryColors;     // kolory

	private GameObject[,] inventoryTiles;

	void Start()
	{
		inventoryTiles = new GameObject[rows, cols];
		GenerateInventory();
		SpawnRandomPieces();
	}

	void GenerateInventory()
	{
		// przesuniêcie obok g³ównej planszy (ale chyba to musi pójsæ do shop)
		float offsetX = cols + 5;

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Color color2 = new Color(inventoryColors[(c + r) % 2].r, inventoryColors[(c + r) % 2].g, inventoryColors[(c + r) % 2].b);

				Vector2 pos = new Vector2(c + offsetX, r);
				GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);

				// kolory indeksy 2 i 3 (ale to chyba zmienie na osobne)
				SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
				sr.color = color2;

				inventoryTiles[r, c] = tile;
			}
		}
	}

	void SpawnRandomPieces()
	{
		// losowe pola
		int kingRow = Random.Range(0, rows);
		int kingCol = Random.Range(0, cols);


		Vector3 kingPos = inventoryTiles[kingRow, kingCol].transform.position;
		kingPos.z = -1;
		Instantiate(kingPrefab, kingPos, Quaternion.identity);

		// dwa pionki w innych polach
		for (int i = 0; i < 2; i++)
		{
			int r, c;
			do
			{
				r = Random.Range(0, rows);
				c = Random.Range(0, cols);
			} while ((r == kingRow && c == kingCol)); // unikam pola krpola

			Vector3 pawnPos = inventoryTiles[r, c].transform.position;
			pawnPos.z = -1;
			Instantiate(pawnPrefab, pawnPos, Quaternion.identity);

		}


	}
}