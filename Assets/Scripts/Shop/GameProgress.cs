using UnityEngine;
using System.Collections.Generic;

// Prosta klasa do zapamiêtania figury
[System.Serializable]
public class SavedPiece
{
	public PieceType type;
	public int row;
	public int col;
}

public class GameProgress : MonoBehaviour
{
	public static GameProgress Instance { get; private set; }

	[Header("Waluta")]
	public int coins = 100;
	public int coinsPerWin = 50;
	public int coinsPerLoss = 10;

	[Header("Statystyki")]
	public int gamesPlayed = 0;

	[Header("Ustawienia Planszy")]
	public int playerBoardSize = 3;

	public int centerBoardSize
	{
		get
		{
			int upgrades = gamesPlayed / 3;
			return Mathf.Min(3 + (upgrades * 2), 9);
		}
	}

	// --- NOWOŒÆ: Pamiêæ Armii ---
	public List<SavedPiece> savedArmy = new List<SavedPiece>();

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

	public void AddCoins(int amount) => coins += amount;

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

	public void LoadScene(string sceneName)
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
	}
}