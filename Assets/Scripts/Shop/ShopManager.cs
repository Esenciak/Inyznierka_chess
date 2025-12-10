using UnityEngine;
using TMPro; 

public class ShopManager : MonoBehaviour
{
	[Header("UI")]
	public TextMeshProUGUI coinsText;           
	public TextMeshProUGUI playerBoardSizeText;
	public TextMeshProUGUI centerBoardSizeText;

	[Header("Koszty")]
	public int basePlayerBoardUpgradeCost = 20;
	public int costIncreasePerLevel = 10;

	private void Start()
	{
		RefreshUI();
	}

	private void RefreshUI()
	{
		if (GameProgress.Instance == null)
			return;

		if (coinsText != null)
			coinsText.text = "Coins: " + GameProgress.Instance.coins;

		if (playerBoardSizeText != null)
		{
			playerBoardSizeText.text = "Board Size: "
				+ GameProgress.Instance.playerBoardSize + "x"
				+ GameProgress.Instance.playerBoardSize;
		}


		if (centerBoardSizeText != null)
		{
			centerBoardSizeText.text = "Center Board Size: "
				+ GameProgress.Instance.centerBoardSize + "x"
				+ GameProgress.Instance.centerBoardSize;
		}
	}

	private int GetCurrentPlayerBoardUpgradeCost()
	{
		if (GameProgress.Instance == null)
			return 0;

		int currentSize = GameProgress.Instance.playerBoardSize;
		int level = currentSize - 3;
		if (level < 0) level = 0;

		return basePlayerBoardUpgradeCost + level * costIncreasePerLevel;
	}

	public void OnBuyPlayerBoardUpgrade()
	{
		if (GameProgress.Instance == null)
			return;

		if (GameProgress.Instance.playerBoardSize >= GameProgress.Instance.maxPlayerBoardSize)
		{
			Debug.Log("Player board is already at max size.");
			return;
		}

		int cost = GetCurrentPlayerBoardUpgradeCost();

		if (!GameProgress.Instance.SpendCoins(cost))
		{
			Debug.Log("Not enough coins.");
			return;
		}

		GameProgress.Instance.playerBoardSize++;
		Debug.Log("Player board upgraded to: "
				  + GameProgress.Instance.playerBoardSize + "x"
				  + GameProgress.Instance.playerBoardSize);

		RefreshUI();
	}

	public void OnStartBattle()
	{
		if (GameProgress.Instance == null)
			return;

		GameProgress.Instance.LoadScene("Battle"); // nazwa sceny z bitw¹
	}

	private void Update()
	{
		// Jeœli chcesz, mo¿esz tu okresowo odœwie¿aæ UI.
		// RefreshUI();
	}
}
