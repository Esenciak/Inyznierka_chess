using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// To jest ta Twoja struktura danych (zamiast Stringa/Inta mamy obiekt)
// [System.Serializable] jest KLUCZOWE - pozwala Unity widzieæ i zapisywaæ tê klasê
[System.Serializable]
public class SavedPieceData
{
	public PieceType type; // Jaka to figura?
	public int x;          // Kolumna (Col)
	public int y;          // Wiersz (Row)
}

public class GameProgress : MonoBehaviour
{
	public static GameProgress Instance { get; private set; }

	[Header("Statystyki")]
	public int coins = 100;
	public int gamesPlayed = 0;

	[Header("Ustawienia Planszy")]
	public int playerBoardSize = 3;

	// Dynamiczny rozmiar œrodka
	public int centerBoardSize
	{
		get
		{
			int upgrades = gamesPlayed / 3;
			return Mathf.Min(3 + (upgrades * 2), 9);
		}
	}

	// --- PAMIÊÆ ARMII ---
	// Tu trzymamy zapisany uk³ad (to jest bezpieczniejsze ni¿ GameObjecty)
	public List<SavedPieceData> myArmy = new List<SavedPieceData>();

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject); // To sprawia, ¿e GameProgress prze¿ywa zmianê sceny
	}

	public bool SpendCoins(int amount)
	{
		if (coins < amount) return false;
		coins -= amount;
		return true;
	}

	public void LoadScene(string sceneName)
	{
		SceneManager.LoadScene(sceneName);
	}
}