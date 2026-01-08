using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// To jest ta Twoja struktura danych (zamiast Stringa/Inta mamy obiekt)
// [System.Serializable] jest KLUCZOWE - pozwala Unity widzieć i zapisywać tę klasę
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

        [Header("Ustawienia Startu")]
        public int startingCoins = 100;

        [Header("Serie Zwycięstw/Porażek")]
        public int winStreak = 0;
        public int loseStreak = 0;

        [System.Serializable]
        public class StreakReward
        {
                public int streak = 1;
                public float multiplier = 1f;
        }

        public List<StreakReward> winStreakRewards = new List<StreakReward>();
        public List<StreakReward> loseStreakRewards = new List<StreakReward>();

        [Header("Tryb gracza")]
        public bool isHostPlayer = true;

        [Header("Ustawienia Planszy")]
        public int playerBoardSize = 3;

        // Dynamiczny rozmiar środka
        public int centerBoardSize
        {
                get
                {
                        int upgrades = gamesPlayed / 3;
                        return Mathf.Min(3 + (upgrades * 2), 9);
                }
        }

        // --- PAMIĘĆ ARMII ---
        // Tu trzymamy zapisany układ (to jest bezpieczniejsze niż GameObjecty)
        public List<SavedPieceData> myArmy = new List<SavedPieceData>();

        [Header("Pamięć Inventory")]
        public List<PieceType> inventoryPieces = new List<PieceType>();

        private void Awake()
        {
                if (Instance != null && Instance != this)
                {
                        Destroy(gameObject);
                        return;
                }
                Instance = this;
                DontDestroyOnLoad(gameObject); // To sprawia, że GameProgress przetrwa zmianę sceny
        }

        public bool SpendCoins(int amount)
        {
                if (coins < amount) return false;
                coins -= amount;
                return true;
        }

        public void AddCoins(int amount)
        {
                coins += amount;
        }

        public bool IsLocalPlayerWhite()
        {
                if (GameManager.Instance != null && GameManager.Instance.isMultiplayer)
                {
                        return isHostPlayer;
                }

                return true;
        }

        public void CompleteRound(bool playerWon, int winReward, int loseReward)
        {
                gamesPlayed++;

                if (playerWon)
                {
                        winStreak++;
                        loseStreak = 0;
                        int reward = Mathf.RoundToInt(winReward * GetStreakMultiplier(winStreakRewards, winStreak));
                        AddCoins(reward);
                }
                else
                {
                        loseStreak++;
                        winStreak = 0;
                        int reward = Mathf.RoundToInt(loseReward * GetStreakMultiplier(loseStreakRewards, loseStreak));
                        AddCoins(reward);
                }
        }

        public void ResetProgress()
        {
                coins = startingCoins;
                gamesPlayed = 0;
                winStreak = 0;
                loseStreak = 0;
                myArmy.Clear();
                inventoryPieces.Clear();
        }

        float GetStreakMultiplier(List<StreakReward> rewards, int streak)
        {
                float multiplier = 1f;
                foreach (var reward in rewards)
                {
                        if (reward != null && reward.streak <= streak)
                        {
                                multiplier = reward.multiplier;
                        }
                }
                return multiplier;
        }

        public void LoadScene(string sceneName)
        {
                SceneManager.LoadScene(sceneName);
        }
}
