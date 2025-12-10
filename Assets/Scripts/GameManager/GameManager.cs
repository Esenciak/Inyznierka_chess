using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	public GamePhase currentPhase = GamePhase.Battle; // na razie od razu walka
	public Turn currentTurn = Turn.Player;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	public bool CanPieceMove(Piece piece)
	{
		if (currentPhase != GamePhase.Battle)
			return false;

		if (currentTurn == Turn.Player && piece.owner != PieceOwner.Player)
			return false;

		if (currentTurn == Turn.Enemy && piece.owner != PieceOwner.Enemy)
			return false;

		return true;
	}

	public void GameOver(bool playerWon)
	{
		if (GameProgress.Instance != null)
		{
			GameProgress.Instance.RegisterMatchResult(playerWon);
			GameProgress.Instance.LoadScene("Shop");
		}
		else
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene("Shop");
		}
	}

	public void EndPlayerMove()
	{
		currentTurn = Turn.Enemy;
		//EnemyAI.Instance.MakeMove();  // dodamy w Etapie 4
	}

	public void EndEnemyMove()
	{
		currentTurn = Turn.Player;
	}

	public void EndMatch(bool playerWon)
	{
		if (GameProgress.Instance != null)
		{
			GameProgress.Instance.RegisterMatchResult(playerWon);
			GameProgress.Instance.LoadScene("Shop");
		}
		else
		{
			// awaryjnie, gdyby singleton nie istnia³
			UnityEngine.SceneManagement.SceneManager.LoadScene("Shop");
		}
	}
}
