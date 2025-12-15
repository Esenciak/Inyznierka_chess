using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgress : MonoBehaviour
{
	public static GameProgress Instance { get; private set; }

	[Header("Waluta")]
	public int coins = 100; // Na start dajmy trochê kasy na testy
	public int coinsPerWin = 50;
	public int coinsPerLoss = 10;

	[Header("Statystyki")]
	public int gamesPlayed = 0;

	[Header("Ustawienia Planszy")]
	// 1. NAPRAWA: Dodajemy brakuj¹c¹ zmienn¹ playerBoardSize
	public int playerBoardSize = 5;

	// 2. NAPRAWA: Zamieniamy metodê GetCurrentCenterSize na w³aœciwoœæ centerBoardSize
	// Dziêki temu BoardManager mo¿e odwo³aæ siê do GameProgress.Instance.centerBoardSize
	public int centerBoardSize
	{
		get
		{
			// Startuje od 3. Co ka¿de 3 gry dodaje 2 do rozmiaru (3->5->7)
			int upgrades = gamesPlayed / 3;
			int size = 3 + (upgrades * 2);

			// Opcjonalnie limit, np. do 9x9
			return Mathf.Min(size, 9);
		}
	}

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
	}

	public bool SpendCoins(int amount)
	{
		if (coins < amount) return false;
		coins -= amount;
		return true;
	}

	public void RegisterMatchResult(bool playerWon)
	{
		gamesPlayed++;
		if (playerWon) AddCoins(coinsPerWin);
		else AddCoins(coinsPerLoss);
	}

	// Prosta metoda do ³adowania scen
	public void LoadScene(string sceneName)
	{
		SceneManager.LoadScene(sceneName);
	}
}