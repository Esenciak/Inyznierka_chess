using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;



[System.Serializable]
public class SavedPieceData
{
        public PieceType type;
        public int x;
        public int y;
}

[System.Serializable]
public class SavedInventoryData
{
        public PieceType type;
        public int row;
        public int col;
}

public class GameProgress : MonoBehaviour
{
        public static GameProgress Instance { get; private set; }

        [Header("Statystyki")]
        public int coins = 100;
        public int gamesPlayed = 0;
        public int wins = 0;
        public int losses = 0;

        [Header("Ekonomia")]
        public EconomyConfig economyConfig;

        [Header("Tryb gracza")]
        public bool isHostPlayer = true;

        [Header("Podsumowanie rundy")]
        public string lastWinnerMessage = string.Empty;

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



        public List<SavedPieceData> myArmy = new List<SavedPieceData>();


        public List<SavedInventoryData> inventoryPieces = new List<SavedInventoryData>();

        private void Awake()
        {
                if (Instance != null && Instance != this)
                {
                        Destroy(gameObject);
                        return;
                }
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (economyConfig != null)
                {
                        coins = economyConfig.startingCoins;
                }
        }

        public void ResetProgressForNewLobby()
        {
                gamesPlayed = 0;
                wins = 0;
                losses = 0;
                lastWinnerMessage = string.Empty;
                playerBoardSize = 3;
                myArmy.Clear();
                inventoryPieces.Clear();

                if (economyConfig != null)
                {
                        coins = economyConfig.startingCoins;
                }
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
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                        return NetworkManager.Singleton.IsHost;
                }

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
                        wins++;
                }
                else
                {
                        losses++;
                }
                AddCoins(playerWon ? winReward : loseReward);
        }

        public void LoadScene(string sceneName)
        {
                SceneFader.LoadSceneWithFade(sceneName);
        }
}
