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

	[Header("Kamera Bitwy")]
	public float cameraTopPadding = 2.5f;
	public float cameraBottomPadding = 1.0f;
	public float cameraSidePadding = 1.0f;
	public float minCameraSize = 5.0f;

	// Tablice
	private GameObject[,] playerBoard;
	private GameObject[,] enemyBoard;
	private GameObject[,] centerBoard;

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
		// If Manager has GameProgress, DontDestroyOnLoad already works.
	}

	private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
	private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		InitBoard();
	}

	private void Start()
	{
		InitBoard();
	}

	void InitBoard()
	{
		IsReady = false;

		string sceneName = SceneManager.GetActiveScene().name;
		if (sceneName == "MainMenu")
		{
			// In the main menu we clear the board (if any exists) and stop.
			ClearAllBoards();
			return;
		}

		// Pobranie rozmiarów z zapisu gry
		if (GameProgress.Instance != null)
		{
			PlayerRows = GameProgress.Instance.playerBoardSize;
			PlayerCols = GameProgress.Instance.playerBoardSize;
			CenterRows = GameProgress.Instance.centerBoardSize;
			CenterCols = GameProgress.Instance.centerBoardSize;
		}

		RecalculateGlobalLayout();
		offsetCalculation(); // <-- Position calculation

		if (sceneName == "Shop") GenerateShopLayout();
		else GenerateBattleLayout();

		IsReady = true;

		if (sceneName == "Battle")
		{
			AdjustBattleCamera();
		}
	}

		// --- Generation (shortened for readability, logic unchanged) ---

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
				Vector3 pos = new Vector3(c + offset.x, r + offset.y, 0);
				GameObject tileGO = Instantiate(tilePrefab, pos, Quaternion.identity);
				tileGO.transform.parent = transform;

				if (colors != null && colors.Length > 0)
				{
					int gx = Mathf.RoundToInt(offset.x) + c;
					int gy = Mathf.RoundToInt(offset.y) + r;
					int idx = ((gx + gy) % colors.Length + colors.Length) % colors.Length;
					tileGO.GetComponent<SpriteRenderer>().color = new Color(colors[idx].r, colors[idx].g, colors[idx].b, 1.0f);
				}

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

	private void GenerateBoard(GameObject[,] board, Color[] colA, Color[] colB, Vector2 offset, BoardType boardType, int startGlobalRow, int startGlobalCol)
	{
		// Wersja Gradientowa (Center)
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
				tileGO.transform.parent = transform;

				float t = (rows > 1) ? (float)r / (rows - 1) : 0f;
				int gx = Mathf.RoundToInt(offset.x) + c;
				int gy = Mathf.RoundToInt(offset.y) + r;
				int idx = ((gx + gy) % len + len) % len;

				Color blended = Color.Lerp(colA[idx % colA.Length], colB[idx % colB.Length], t);
				blended.a = 1.0f;
				tileGO.GetComponent<SpriteRenderer>().color = blended;

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

	// *** TUTAJ JEST NAPRAWA POZYCJI W SKLEPIE ***
	private void offsetCalculation()
	{
		float centerX = 0f;
		float centerY = 0f;

		// Default (Battle)
		float playerOffsetX = centerX + (CenterCols - PlayerCols) / 2f;
		float playerOffsetY = centerY - PlayerRows;
		float enemyOffsetY = centerY + CenterRows;

		// If SHOP -> fixed position
		if (SceneManager.GetActiveScene().name == "Shop")
		{
			playerOffsetX = 3.5f; // Fixed X position
			playerOffsetY = 0f;   // Fixed Y position
			enemyOffsetY = 100f;  // Wyrzucamy wroga poza ekran
		}

		playerOffset = new Vector2(playerOffsetX, playerOffsetY);
		enemyOffset = new Vector2(playerOffsetX, enemyOffsetY);
		centerOffset = new Vector2(0f, 0f);
	}

	void AdjustBattleCamera()
	{
		Camera cam = Camera.main;
		if (cam == null || !cam.orthographic) return;

		float halfTile = 0.5f;
		float minX = Mathf.Min(playerOffset.x, centerOffset.x, enemyOffset.x) - halfTile - cameraSidePadding;
		float maxX = Mathf.Max(playerOffset.x + PlayerCols - 1, centerOffset.x + CenterCols - 1, enemyOffset.x + PlayerCols - 1) + halfTile + cameraSidePadding;

		float minY = Mathf.Min(playerOffset.y, centerOffset.y, enemyOffset.y) - halfTile - cameraBottomPadding;
		float maxY = Mathf.Max(playerOffset.y + PlayerRows - 1, centerOffset.y + CenterRows - 1, enemyOffset.y + PlayerRows - 1) + halfTile + cameraTopPadding;

		float centerX = centerOffset.x + (CenterCols - 1) / 2f;
		float centerY = centerOffset.y + (CenterRows - 1) / 2f;

		float halfWidthNeeded = Mathf.Max(Mathf.Abs(centerX - minX), Mathf.Abs(maxX - centerX));
		float halfHeightNeeded = Mathf.Max(Mathf.Abs(centerY - minY), Mathf.Abs(maxY - centerY));

		float aspect = cam.aspect;
		float sizeByWidth = halfWidthNeeded / Mathf.Max(0.01f, aspect);
		cam.orthographicSize = Mathf.Max(halfHeightNeeded, sizeByWidth, minCameraSize);
		cam.transform.position = new Vector3(centerX, centerY, cam.transform.position.z);
	}

	// --- PUBLIC API (Przywrócone metody) ---

	public Tile GetTileGlobal(int globalRow, int globalCol)
	{
		if (globalRow < playerStartRow + PlayerRows) return GetTile(BoardType.Player, globalRow - playerStartRow, globalCol - playerStartCol);
		else if (globalRow < centerStartRow + CenterRows) return GetTile(BoardType.Center, globalRow - centerStartRow, globalCol - centerStartCol);
		else return GetTile(BoardType.Enemy, globalRow - enemyStartRow, globalCol - enemyStartCol);
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
		if (targetBoard == null || row < 0 || row >= targetBoard.GetLength(0) || col < 0 || col >= targetBoard.GetLength(1)) return null;
		GameObject go = targetBoard[row, col];
		return go != null ? go.GetComponent<Tile>() : null;
	}

	public Tile GetPlayerCenterTile()
	{
		if (playerBoard == null) return null;
		return GetTile(BoardType.Player, PlayerRows / 2, PlayerCols / 2);
	}

	public Tile GetTileAtPosition(Vector2 worldPos)
	{
		RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
		if (hit.collider != null) return hit.collider.GetComponent<Tile>();
		return null;
	}
}
