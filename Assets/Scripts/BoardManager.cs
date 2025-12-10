using UnityEngine;

public class BoardManager : MonoBehaviour
{
	public bool IsReady { get; private set; }

	[Header("Rozmiar planszy graczy")]
	public int PlayerRows = 3;
	public int PlayerCols = 3;

	[Header("Rozmiar planszy centralnej")]
	public int CenterRows = 5;
	public int CenterCols = 5;

	public GameObject tilePrefab;

	[Header("Kolory kafelków")]
	public Color[] playerColors;
	public Color[] enemyColors;
	public Color[] centerColors;

	private GameObject[,] playerBoard;
	private GameObject[,] enemyBoard;
	private GameObject[,] centerBoard;

	[Header("Pozycje planszy na scenie")]
	public Vector2 playerOffset = new Vector2(0, -5);
	public Vector2 enemyOffset = new Vector2(0, 5);
	public Vector2 centerOffset = new Vector2(0, 0);

	// globalne rzêdy w "wie¿y"
	private int playerStartRow;
	private int centerStartRow;
	private int enemyStartRow;
	private int totalRows;

	public static BoardManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	void Start()
	{
		if (GameProgress.Instance != null)
		{
			int sizePlayer = GameProgress.Instance.playerBoardSize;
			int sizeCenter = GameProgress.Instance.centerBoardSize;

			PlayerRows = sizePlayer;
			PlayerCols = sizePlayer;

			CenterRows = sizeCenter;
			CenterCols = sizeCenter;
		}

		// wymuszamy nieparzysty rozmiar center
		if (CenterRows % 2 == 0) CenterRows += 1;
		if (CenterCols % 2 == 0) CenterCols += 1;

		RecalculateGlobalRows();
		offsetCalculation();

		CreateAndGenerateBoards();

		IsReady = true;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			// testowo – przegenerowanie z aktualnymi rozmiarami
			RecalculateGlobalRows();
			offsetCalculation();
			CreateAndGenerateBoards();
		}
	}

	private void RecalculateGlobalRows()
	{
		playerStartRow = 0;
		centerStartRow = PlayerRows;
		enemyStartRow = PlayerRows + CenterRows;
		totalRows = PlayerRows + CenterRows + PlayerRows;
	}

	private void DestroyBoard(GameObject[,] board)
	{
		if (board == null) return;

		foreach (GameObject tile in board)
		{
			if (tile != null)
				Destroy(tile);
		}
	}

	private void CreateAndGenerateBoards()
	{
		// kasujemy stare
		DestroyBoard(playerBoard);
		DestroyBoard(enemyBoard);
		DestroyBoard(centerBoard);

		// tworzymy nowe tablice
		playerBoard = new GameObject[PlayerRows, PlayerCols];
		enemyBoard = new GameObject[PlayerRows, PlayerCols];
		centerBoard = new GameObject[CenterRows, CenterCols];

		GenerateBoard(playerBoard, playerColors, playerOffset, BoardType.Player, playerStartRow);
		GenerateBoard(enemyBoard, enemyColors, enemyOffset, BoardType.Enemy, enemyStartRow);
		GenerateBoard(centerBoard, playerColors, enemyColors, centerOffset, BoardType.Center, centerStartRow);
	}

	// plansza gracza / przeciwnika
	void GenerateBoard(GameObject[,] board, Color[] colors, Vector2 offset, BoardType boardType, int startGlobalRow)
	{
		int rows = board.GetLength(0);
		int cols = board.GetLength(1);

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector2 pos = new Vector2(c + offset.x, r + offset.y);
				GameObject tileGO = Instantiate(tilePrefab, pos, Quaternion.identity);

				if (colors != null && colors.Length > 0)
				{
					SpriteRenderer sr = tileGO.GetComponent<SpriteRenderer>();

					int gx = Mathf.RoundToInt(offset.x) + c;
					int gy = Mathf.RoundToInt(offset.y) + r;
					int idx = ((gx + gy) % colors.Length + colors.Length) % colors.Length;

					sr.color = new Color(colors[idx].r, colors[idx].g, colors[idx].b);
				}

				Tile tileComponent = tileGO.GetComponent<Tile>();
				if (tileComponent != null)
				{
					tileComponent.row = r;
					tileComponent.col = c;
					tileComponent.boardType = boardType;

					tileComponent.globalRow = startGlobalRow + r;
					tileComponent.globalCol = c;
				}

				board[r, c] = tileGO;
			}
		}
	}

	// plansza centralna – blend kolorów
	void GenerateBoard(GameObject[,] board, Color[] playerColor, Color[] enemyColor, Vector2 offset, BoardType boardType, int startGlobalRow)
	{
		int rows = board.GetLength(0);
		int cols = board.GetLength(1);

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector2 pos = new Vector2(c + offset.x, r + offset.y);
				GameObject tileGO = Instantiate(tilePrefab, pos, Quaternion.identity);

				float t = (rows > 1) ? (float)r / (rows - 1) : 0f;

				int len = Mathf.Min(playerColor.Length, enemyColor.Length);

				int gx = Mathf.RoundToInt(offset.x) + c;
				int gy = Mathf.RoundToInt(offset.y) + r;
				int idx = ((gx + gy) % len + len) % len;

				Color blended = Color.Lerp(playerColor[idx], enemyColor[idx], t);

				SpriteRenderer sr = tileGO.GetComponent<SpriteRenderer>();
				sr.color = new Color(blended.r, blended.g, blended.b);

				Tile tileComponent = tileGO.GetComponent<Tile>();
				if (tileComponent != null)
				{
					tileComponent.row = r;
					tileComponent.col = c;
					tileComponent.boardType = boardType;

					tileComponent.globalRow = startGlobalRow + r;
					tileComponent.globalCol = c;
				}

				board[r, c] = tileGO;
			}
		}
	}

	private void offsetCalculation()
	{
		float CenterWidth = CenterCols;
		float CenterHeight = CenterRows;

		float playerWidth = PlayerCols;
		float playerHeight = PlayerRows;

		float enemyWidth = PlayerCols;
		float enemyHeight = PlayerRows;

		float centerX = 0f;

		float playerOffsetX = centerX + (CenterWidth - playerWidth) / 2f;
		float enemyOffsetX = centerX + (CenterWidth - enemyWidth) / 2f;

		float centerY = 0f;
		float playerOffsetY = centerY - playerHeight;
		float enemyOffsetY = centerY + CenterHeight;

		playerOffset = new Vector2(playerOffsetX, playerOffsetY);
		enemyOffset = new Vector2(enemyOffsetX, enemyOffsetY);
		centerOffset = new Vector2(0f, 0f);
	}

	public void ResizeCenterBoard(int newRows, int newCols)
	{
		if (newRows % 2 == 0) newRows += 1;
		if (newCols % 2 == 0) newCols += 1;

		CenterRows = newRows;
		CenterCols = newCols;

		RecalculateGlobalRows();
		offsetCalculation();
		CreateAndGenerateBoards();
	}

	public void ResizePlayerAndEnemyBoards(int newRows, int newCols)
	{
		PlayerRows = newRows;
		PlayerCols = newCols;

		RecalculateGlobalRows();
		offsetCalculation();
		CreateAndGenerateBoards();
	}

	public Tile GetTile(BoardType boardType, int row, int col)
	{
		GameObject[,] boardArray = null;

		switch (boardType)
		{
			case BoardType.Player:
				boardArray = playerBoard;
				break;
			case BoardType.Center:
				boardArray = centerBoard;
				break;
			case BoardType.Enemy:
				boardArray = enemyBoard;
				break;
		}

		if (boardArray == null)
			return null;

		int rows = boardArray.GetLength(0);
		int cols = boardArray.GetLength(1);

		if (row < 0 || row >= rows || col < 0 || col >= cols)
			return null;

		GameObject tileGO = boardArray[row, col];
		if (tileGO == null) return null;

		return tileGO.GetComponent<Tile>();
	}

	// NOWE: pobieranie kafelka po globalnym rzêdzie (jedna wspólna plansza)
	public Tile GetTileGlobal(int globalRow, int col)
	{
		if (globalRow < 0 || globalRow >= totalRows)
			return null;

		if (col < 0 || col >= PlayerCols) // zak³adamy te same kolumny
			return null;

		// Player
		if (globalRow < centerStartRow)
		{
			int localRow = globalRow - playerStartRow;
			return GetTile(BoardType.Player, localRow, col);
		}
		// Center
		else if (globalRow < enemyStartRow)
		{
			int localRow = globalRow - centerStartRow;
			return GetTile(BoardType.Center, localRow, col);
		}
		// Enemy
		else
		{
			int localRow = globalRow - enemyStartRow;
			return GetTile(BoardType.Enemy, localRow, col);
		}
	}
}
