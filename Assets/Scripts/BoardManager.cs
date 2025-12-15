using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardManager : MonoBehaviour
{
	public static BoardManager Instance { get; private set; }
	public bool IsReady { get; private set; } = false;

	[Header("Ustawienia Rozmiarów")]
	public int PlayerRows = 3;
	public int PlayerCols = 3;
	public int CenterRows = 5;
	public int CenterCols = 5;

	[Header("Prefabrykaty i Kolory")]
	public GameObject tilePrefab;
	public Color[] playerColors;
	public Color[] enemyColors;

	[Header("Pozycje (Offsety)")]
	public Vector2 playerOffset = new Vector2(0, -5);
	public Vector2 enemyOffset = new Vector2(0, 5);
	public Vector2 centerOffset = new Vector2(0, 0);

	// Tablice
	private GameObject[,] playerBoard;
	private GameObject[,] enemyBoard;
	private GameObject[,] centerBoard;

	// Zmienne globalne
	private int playerStartRow, centerStartRow, enemyStartRow;
	public int totalRows { get; private set; }
	private int playerStartCol, centerStartCol, enemyStartCol;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	private void Start()
	{
		if (GameProgress.Instance != null)
		{
			PlayerRows = GameProgress.Instance.playerBoardSize;
			PlayerCols = GameProgress.Instance.playerBoardSize;
			CenterRows = GameProgress.Instance.centerBoardSize;
			CenterCols = GameProgress.Instance.centerBoardSize;
		}

		RecalculateGlobalLayout();
		offsetCalculation();

		string sceneName = SceneManager.GetActiveScene().name;

		if (sceneName == "Shop")
		{
			GenerateShopLayout();
		}
		else
		{
			GenerateBattleLayout();
		}

		IsReady = true;
	}

	void GenerateShopLayout()
	{
		ClearAllBoards();
		playerBoard = new GameObject[PlayerRows, PlayerCols];
		GenerateBoard(playerBoard, playerColors, playerOffset, BoardType.Player, playerStartRow, playerStartCol);
	}

	void GenerateBattleLayout()
	{
		ClearAllBoards();
		playerBoard = new GameObject[PlayerRows, PlayerCols];
		enemyBoard = new GameObject[PlayerRows, PlayerCols];
		centerBoard = new GameObject[CenterRows, CenterCols];

		GenerateBoard(playerBoard, playerColors, playerOffset, BoardType.Player, playerStartRow, playerStartCol);
		GenerateBoard(enemyBoard, enemyColors, enemyOffset, BoardType.Enemy, enemyStartRow, enemyStartCol);
		GenerateBoard(centerBoard, playerColors, enemyColors, centerOffset, BoardType.Center, centerStartRow, centerStartCol);
	}

	void ClearAllBoards()
	{
		DestroyBoard(playerBoard);
		DestroyBoard(enemyBoard);
		DestroyBoard(centerBoard);
	}

	private void GenerateBoard(GameObject[,] board, Color[] colors, Vector2 offset, BoardType boardType, int startGlobalRow, int startGlobalCol)
	{
		int rows = board.GetLength(0);
		int cols = board.GetLength(1);

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				// Ustawiamy Z na 0, ¿eby na pewno by³o widaæ
				Vector3 pos = new Vector3(c + offset.x, r + offset.y, 0);
				GameObject tileGO = Instantiate(tilePrefab, pos, Quaternion.identity);
				tileGO.transform.parent = null;

				// --- POPRAWKA KOLORÓW ---
				if (colors != null && colors.Length > 0)
				{
					int gx = Mathf.RoundToInt(offset.x) + c;
					int gy = Mathf.RoundToInt(offset.y) + r;
					int idx = ((gx + gy) % colors.Length + colors.Length) % colors.Length;

					SpriteRenderer sr = tileGO.GetComponent<SpriteRenderer>();
					if (sr != null)
					{
						// Tu dodajemy 1.0f na koñcu, ¿eby Alpha by³a 100% (nieprzezroczysta)
						sr.color = new Color(colors[idx].r, colors[idx].g, colors[idx].b, 1.0f);
					}
				}
				// -------------------------

				Tile tile = tileGO.GetComponent<Tile>();
				if (tile != null)
				{
					tile.row = r;
					tile.col = c;
					tile.boardType = boardType;
					tile.globalRow = startGlobalRow + r;
					tile.globalCol = startGlobalCol + c;
					tile.isInventory = false;
				}
				board[r, c] = tileGO;
			}
		}
	}

	// Wersja dla œrodka (Gradient)
	private void GenerateBoard(GameObject[,] board, Color[] colA, Color[] colB, Vector2 offset, BoardType boardType, int startGlobalRow, int startGlobalCol)
	{
		int rows = board.GetLength(0);
		int cols = board.GetLength(1);
		int len = Mathf.Min(colA.Length, colB.Length);
		if (len == 0) len = 1;

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector3 pos = new Vector3(c + offset.x, r + offset.y, 0);
				GameObject tileGO = Instantiate(tilePrefab, pos, Quaternion.identity);
				tileGO.transform.parent = null;

				float t = (rows > 1) ? (float)r / (rows - 1) : 0f;
				int gx = Mathf.RoundToInt(offset.x) + c;
				int gy = Mathf.RoundToInt(offset.y) + r;
				int idx = ((gx + gy) % len + len) % len;

				Color blended = Color.Lerp(colA[idx % colA.Length], colB[idx % colB.Length], t);

				// Tutaj te¿ wymuszamy pe³n¹ widocznoœæ (Alpha = 1)
				blended.a = 1.0f;

				SpriteRenderer sr = tileGO.GetComponent<SpriteRenderer>();
				if (sr != null) sr.color = blended;

				Tile tile = tileGO.GetComponent<Tile>();
				if (tile != null)
				{
					tile.row = r;
					tile.col = c;
					tile.boardType = boardType;
					tile.globalRow = startGlobalRow + r;
					tile.globalCol = startGlobalCol + c;
				}
				board[r, c] = tileGO;
			}
		}
	}

	// --- Helpery ---
	private void DestroyBoard(GameObject[,] board)
	{
		if (board == null) return;
		foreach (var go in board) if (go != null) Destroy(go);
	}

	private void RecalculateGlobalLayout()
	{
		playerStartRow = 0;
		centerStartRow = PlayerRows;
		enemyStartRow = PlayerRows + CenterRows;
		totalRows = PlayerRows + CenterRows + PlayerRows;

		int diff = CenterCols - PlayerCols;
		if (diff < 0) diff = 0;
		playerStartCol = diff / 2;
		centerStartCol = 0;
		enemyStartCol = diff / 2;
	}

	private void offsetCalculation()
	{
		float centerX = 0f;
		float pOffX = centerX + (CenterCols - PlayerCols) / 2f;
		float pOffY = -PlayerRows;
		float eOffY = CenterRows;

		if (SceneManager.GetActiveScene().name == "Shop")
		{
			pOffX += 3.5f;
			pOffY = 0;
		}

		playerOffset = new Vector2(pOffX, pOffY);
		enemyOffset = new Vector2(pOffX, eOffY);
		centerOffset = Vector2.zero;
	}

	// --- PUBLIC API ---

	public Tile GetTileGlobal(int globalRow, int globalCol)
	{
		if (globalRow < playerStartRow + PlayerRows)
			return GetTile(BoardType.Player, globalRow - playerStartRow, globalCol - playerStartCol);
		else if (globalRow < centerStartRow + CenterRows)
			return GetTile(BoardType.Center, globalRow - centerStartRow, globalCol - centerStartCol);
		else
			return GetTile(BoardType.Enemy, globalRow - enemyStartRow, globalCol - enemyStartCol);
	}

	public Tile GetTile(BoardType type, int row, int col)
	{
		GameObject[,] targetBoard = null;
		switch (type)
		{
			case BoardType.Player: targetBoard = playerBoard; break;
			case BoardType.Center: targetBoard = centerBoard; break;
			case BoardType.Enemy: targetBoard = enemyBoard; break;
		}

		if (targetBoard == null) return null;
		if (row < 0 || row >= targetBoard.GetLength(0)) return null;
		if (col < 0 || col >= targetBoard.GetLength(1)) return null;

		GameObject go = targetBoard[row, col];
		if (go == null) return null;
		return go.GetComponent<Tile>();
	}

	public Tile GetTileAtPosition(Vector2 worldPos)
	{
		RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
		if (hit.collider != null)
		{
			return hit.collider.GetComponent<Tile>();
		}
		return null;
	}

	public Tile GetPlayerCenterTile()
	{
		if (playerBoard == null) return null;
		return GetTile(BoardType.Player, PlayerRows / 2, PlayerCols / 2);
	}
}