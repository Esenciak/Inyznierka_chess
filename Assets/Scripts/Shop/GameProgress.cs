using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgress : MonoBehaviour
{
	public static GameProgress Instance { get; private set; }

	[Header("Waluta / ekonomia")]
	public int coins = 0;
	public int coinsPerWin = 10;
	public int coinsPerLoss = 5;

	[Header("Rozmiar planszy gracza (dla obu: player + enemy)")]
	public int playerBoardSize = 3;     // 3 oznacza 3x3
	public int maxPlayerBoardSize = 5;  // max 5x5

	[Header("Rozmiar planszy centralnej")]
	public int centerBoardSize = 3;          // 3 oznacza 3x3
	public int maxCenterBoardSize = 5;
	public int gamesPerCenterUpgrade = 3;    // co ile gier powiêkszaæ œrodek

	[Header("Statystyki meta")]
	public int gamesPlayed = 0;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public void AddCoins(int amount)
	{
		coins += amount;
		if (coins < 0) coins = 0;
	}

	public bool SpendCoins(int amount)
	{
		if (coins < amount) return false;
		coins -= amount;
		return true;
	}

	/// <summary>
	/// Wywo³uj na koñcu ka¿dej gry (po wygranej/przegranej).
	/// </summary>
	public void RegisterMatchResult(bool playerWon)
	{
		// 1. Monety
		if (playerWon)
			AddCoins(coinsPerWin);
		else
			AddCoins(coinsPerLoss);

		// 2. Licznik gier
		gamesPlayed++;

		// 3. Auto-upgrade œrodka co X gier
		if (gamesPlayed % gamesPerCenterUpgrade == 0)
		{
			if (centerBoardSize < maxCenterBoardSize)
			{
				centerBoardSize++;
				Debug.Log("Center board upgraded to: " + centerBoardSize + "x" + centerBoardSize);
			}
		}
	}

	/// <summary>
	/// Metoda pomocnicza do ³adowania scen.
	/// </summary>
	public void LoadScene(string sceneName)
	{
		SceneManager.LoadScene(sceneName);
	}
}
