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

	[Header("Kamera Bitwy")]
	public float battleCameraPadding = 1.5f;
	public float battleCameraMinSize = 6f;
	[Range(0f, 1f)] public float battleBackgroundBlend = 0.5f;
	[Range(0f, 1f)] public float battleBackgroundDarken = 0.35f;

	[Header("Pozycje (Offsety)")]
	public Vector2 playerOffset = new Vector2(0, -5);
	public Vector2 enemyOffset = new Vector2(0, 5);
	public Vector2 centerOffset = new Vector2(0, 0);

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
		// Jeli jest na obiekcie Manager z GameProgress, to DontDestroyOnLoad ju¿ dzia³a.
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
			// W menu g³ównym czycimy planszê (jeli jaka zosta³a) i koñczymy
			ClearAllBoards();
			return;
		}

		// Pobranie rozmiarów z zapisu gry
		if (GameProgress.Instance != null)
		{
			if (GameManager.Instance != null && GameManager.Instance.isMultiplayer && BattleSession.Instance != null)
			{
				GameProgress.Instance.gamesPlayed = BattleSession.Instance.SharedGamesPlayed.Value;
				GameProgress.Instance.playerBoardSize = BattleSession.Instance.SharedPlayerBoardSize.Value;
			}

			PlayerRows = GameProgress.Instance.playerBoardSize;
			PlayerCols = GameProgress.Instance.playerBoardSize;
			CenterRows = GameProgress.Instance.centerBoardSize;
			CenterCols = GameProgress.Instance.centerBoardSize;
		}

		ApplyBattleBackground(sceneName);
		RecalculateGlobalLayout();
		offsetCalculation(); // <-- Tutaj dzieje siê magia z pozycj¹

		if (sceneName == "Shop") GenerateShopLayout();
		else GenerateBattleLayout();

		if (sceneName == "Battle")
		{
			AdjustBattleCamera();
		}

		IsReady = true;
	}

	private void ApplyBattleBackground(string sceneName)
	{
		if (sceneName != "Battle")
		{
			return;
		}

		Camera cam = Camera.main;
		if (cam == null)
		{
			return;
		}

		Color colorA = AverageColors(playerColors, Color.black);
		Color colorB = AverageColors(enemyColors, colorA);
		Color blended = Color.Lerp(colorA, colorB, battleBackgroundBlend);
		cam.backgroundColor = Color.Lerp(blended, Color.black, battleBackgroundDarken);
	}

	// --- Generowanie (Skrócone dla czytelnoci, logika bez zmian) ---

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
				CreateUnderlay(tileGO);

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
				CreateUnderlay(tileGO);

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

		// Domylne (Bitwa)
		float playerOffsetX = centerX + (CenterCols - PlayerCols) / 2f;
		float playerOffsetY = centerY - PlayerRows;
		float enemyOffsetY = centerY + CenterRows;

		// Jeli SKLEP -> Sztywna pozycja
		if (SceneManager.GetActiveScene().name == "Shop")
		{
			playerOffsetX = 3.5f; // Sta³a pozycja X
			playerOffsetY = 0f;   // Sta³a pozycja Y
			enemyOffsetY = 100f;  // Wyrzucamy wroga poza ekran
		}

		playerOffset = new Vector2(playerOffsetX, playerOffsetY);
		enemyOffset = new Vector2(playerOffsetX, enemyOffsetY);
		centerOffset = new Vector2(0f, 0f);
	}

	// --- PUBLIC API (Przywrócone metody) ---

	private void CreateUnderlay(GameObject tileGO)
	{
		if (tileGO == null)
		{
			return;
		}

		SpriteRenderer tileRenderer = tileGO.GetComponent<SpriteRenderer>();
		if (tileRenderer == null || tileRenderer.sprite == null)
		{
			return;
		}

		GameObject underlay = new GameObject("TileUnderlay");
		underlay.transform.SetParent(tileGO.transform, false);
		underlay.transform.localPosition = new Vector3(0f, 0f, 0.1f);
		underlay.transform.localScale = Vector3.one * 1.1f;

		SpriteRenderer underlayRenderer = underlay.AddComponent<SpriteRenderer>();
		underlayRenderer.sprite = tileRenderer.sprite;
		underlayRenderer.color = Color.black;
		underlayRenderer.sortingLayerID = tileRenderer.sortingLayerID;
		underlayRenderer.sortingOrder = tileRenderer.sortingOrder - 1;
	}

	private void AdjustBattleCamera()
	{
		Camera cam = Camera.main;
		if (cam == null || !cam.orthographic)
		{
			return;
		}

		float minX = Mathf.Min(playerOffset.x, enemyOffset.x, centerOffset.x);
		float maxX = Mathf.Max(playerOffset.x + PlayerCols - 1, enemyOffset.x + PlayerCols - 1, centerOffset.x + CenterCols - 1);
		float minY = playerOffset.y;
		float maxY = enemyOffset.y + PlayerRows - 1;

		float centerX = (minX + maxX) * 0.5f;
		float centerY = (minY + maxY) * 0.5f;
		cam.transform.position = new Vector3(centerX, centerY, cam.transform.position.z);

		float halfHeight = (maxY - minY + 1) * 0.5f + battleCameraPadding;
		float halfWidth = (maxX - minX + 1) * 0.5f + battleCameraPadding;
		float targetSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.01f, cam.aspect));
		cam.orthographicSize = Mathf.Max(battleCameraMinSize, targetSize);
	}

	private static Color AverageColors(Color[] colors, Color fallback)
	{
		if (colors == null || colors.Length == 0)
		{
			return fallback;
		}

		Color sum = Color.black;
		for (int i = 0; i < colors.Length; i++)
		{
			sum += colors[i];
		}

		return sum / colors.Length;
	}

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
