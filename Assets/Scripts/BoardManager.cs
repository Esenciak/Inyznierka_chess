using UnityEngine;

public class BoardManager : MonoBehaviour
{
	public static BoardManager Instance { get; private set; }
	public bool IsReady { get; private set; }

	public int PlayerRows = 3;
	public int PlayerCols = 3;

	public int CenterRows = 5;
	public int CenterCols = 5;

	public GameObject tilePrefab;

	public Color[] playerColors;
	public Color[] enemyColors;

	public Vector2 playerOffset = new Vector2(0, -5);
	public Vector2 enemyOffset = new Vector2(0, 5);
	public Vector2 centerOffset = new Vector2(0, 0);

	private GameObject[,] playerBoard;
	private GameObject[,] enemyBoard;
	private GameObject[,] centerBoard;

	private int playerStartRow;
	private int centerStartRow;
	private int enemyStartRow;
	private int totalRows;

	private int playerStartCol;
	private int centerStartCol;
	private int enemyStartCol;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			RefreshBoards();
		}
	}


	private void Start()
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

		if (CenterRows % 2 == 0) CenterRows += 1;
		if (CenterCols % 2 == 0) CenterCols += 1;

		RecalculateGlobalLayout();
		offsetCalculation();
		CreateAndGenerateBoards();

		IsReady = true;
	}

	private void RecalculateGlobalLayout()
	{
		playerStartRow = 0;
		centerStartRow = PlayerRows;
		enemyStartRow = PlayerRows + CenterRows;
		totalRows = PlayerRows + CenterRows + PlayerRows;

		centerStartCol = 0;

		int diff = CenterCols - PlayerCols;
		if (diff < 0) diff = 0;

		playerStartCol = diff / 2;
		enemyStartCol = diff / 2;
	}

	private void CreateAndGenerateBoards()
	{
		DestroyBoard(playerBoard);
		DestroyBoard(enemyBoard);
		DestroyBoard(centerBoard);

		playerBoard = new GameObject[PlayerRows, PlayerCols];
		enemyBoard = new GameObject[PlayerRows, PlayerCols];
		centerBoard = new GameObject[CenterRows, CenterCols];

		GenerateBoard(playerBoard, playerColors, playerOffset, BoardType.Player, playerStartRow, playerStartCol);
		GenerateBoard(enemyBoard, enemyColors, enemyOffset, BoardType.Enemy, enemyStartRow, enemyStartCol);
		GenerateBoard(centerBoard, playerColors, enemyColors, centerOffset, BoardType.Center, centerStartRow, centerStartCol);
	}

	private void DestroyBoard(GameObject[,] board)
	{
		if (board == null) return;
		foreach (var go in board)
			if (go != null) Destroy(go);
	}

	private void offsetCalculation()
	{
		float CenterWidth = CenterCols;
		float CenterHeight = CenterRows;

		float playerWidth = PlayerCols;
		float playerHeight = PlayerRows;

		float enemyWidth = PlayerCols;

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

	private void GenerateBoard(GameObject[,] board, Color[] colors, Vector2 offset, BoardType boardType, int startGlobalRow, int startGlobalCol)
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
					tileComponent.globalCol = startGlobalCol + c;
				}

				board[r, c] = tileGO;
			}
		}
	}

	private void GenerateBoard(GameObject[,] board, Color[] playerColor, Color[] enemyColor, Vector2 offset, BoardType boardType, int startGlobalRow, int startGlobalCol)
	{
		int rows = board.GetLength(0);
		int cols = board.GetLength(1);

		int len = Mathf.Min(playerColor != null ? playerColor.Length : 0, enemyColor != null ? enemyColor.Length : 0);
		if (len <= 0) len = 1;

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector2 pos = new Vector2(c + offset.x, r + offset.y);
				GameObject tileGO = Instantiate(tilePrefab, pos, Quaternion.identity);

				float t = (rows > 1) ? (float)r / (rows - 1) : 0f;

				int gx = Mathf.RoundToInt(offset.x) + c;
				int gy = Mathf.RoundToInt(offset.y) + r;
				int idx = ((gx + gy) % len + len) % len;

				Color a = (playerColor != null && playerColor.Length > 0) ? playerColor[idx % playerColor.Length] : Color.white;
				Color b = (enemyColor != null && enemyColor.Length > 0) ? enemyColor[idx % enemyColor.Length] : Color.white;

				Color blended = Color.Lerp(a, b, t);

				SpriteRenderer sr = tileGO.GetComponent<SpriteRenderer>();
				if (sr != null) sr.color = new Color(blended.r, blended.g, blended.b);

				Tile tileComponent = tileGO.GetComponent<Tile>();
				if (tileComponent != null)
				{
					tileComponent.row = r;
					tileComponent.col = c;
					tileComponent.boardType = boardType;

					tileComponent.globalRow = startGlobalRow + r;
					tileComponent.globalCol = startGlobalCol + c;
				}

				board[r, c] = tileGO;
			}
		}
	}

	public Tile GetTile(BoardType boardType, int row, int col)
	{
		GameObject[,] boardArray = null;

		switch (boardType)
		{
			case BoardType.Player: boardArray = playerBoard; break;
			case BoardType.Center: boardArray = centerBoard; break;
			case BoardType.Enemy: boardArray = enemyBoard; break;
		}

		if (boardArray == null) return null;

		int rows = boardArray.GetLength(0);
		int cols = boardArray.GetLength(1);

		if (row < 0 || row >= rows || col < 0 || col >= cols) return null;

		GameObject tileGO = boardArray[row, col];
		if (tileGO == null) return null;

		return tileGO.GetComponent<Tile>();
	}

	public Tile GetTileGlobal(int globalRow, int globalCol)
	{
		if (globalRow < 0 || globalRow >= totalRows) return null;
		if (globalCol < 0 || globalCol >= CenterCols) return null;

		if (globalRow < centerStartRow)
		{
			int localRow = globalRow - playerStartRow;
			int localCol = globalCol - playerStartCol;
			return GetTile(BoardType.Player, localRow, localCol);
		}
		else if (globalRow < enemyStartRow)
		{
			int localRow = globalRow - centerStartRow;
			int localCol = globalCol - centerStartCol;
			return GetTile(BoardType.Center, localRow, localCol);
		}
		else
		{
			int localRow = globalRow - enemyStartRow;
			int localCol = globalCol - enemyStartCol;
			return GetTile(BoardType.Enemy, localRow, localCol);
		}
	}

	public void ResizeCenterBoard(int newRows, int newCols)
	{
		if (newRows % 2 == 0) newRows += 1;
		if (newCols % 2 == 0) newCols += 1;

		CenterRows = newRows;
		CenterCols = newCols;

		RecalculateGlobalLayout();
		offsetCalculation();
		CreateAndGenerateBoards();
	}

	public void ResizePlayerAndEnemyBoards(int newRows, int newCols)
	{
		PlayerRows = newRows;
		PlayerCols = newCols;

		RecalculateGlobalLayout();
		offsetCalculation();
		CreateAndGenerateBoards();
	}
	public void RefreshBoards()
	{
		RecalculateGlobalLayout();
		offsetCalculation();
		CreateAndGenerateBoards();
	}


}
