using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BattleSession : NetworkBehaviour
{
	public static BattleSession Instance { get; private set; }

	// Listy sieciowe (synchronizowane automatycznie)
	public NetworkList<NetworkArmyPiece> HostArmy;
	public NetworkList<NetworkArmyPiece> ClientArmy;

	// Flagi gotowoci
	public NetworkVariable<bool> IsHostReady = new NetworkVariable<bool>(false);
	public NetworkVariable<bool> IsClientReady = new NetworkVariable<bool>(false);
	public NetworkVariable<int> SharedGamesPlayed = new NetworkVariable<int>(0);
	public NetworkVariable<int> SharedPlayerBoardSize = new NetworkVariable<int>(3);

	public NetworkVariable<int> ActiveTeam = new NetworkVariable<int>(0);
	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		// Inicjalizacja list sieciowych
		HostArmy = new NetworkList<NetworkArmyPiece>();
		ClientArmy = new NetworkList<NetworkArmyPiece>();
	}

	public override void OnNetworkSpawn()
	{
		if (IsServer && GameProgress.Instance != null)
		{
			SharedGamesPlayed.Value = GameProgress.Instance.gamesPlayed;
			SharedPlayerBoardSize.Value = GameProgress.Instance.playerBoardSize;
		}
	}

	// Tê metodê wo³a Twój ShopManager
	public void PlayerReady(List<SavedPieceData> localArmy)
	{
		// Konwersja na format sieciowy
		NetworkArmyPiece[] networkArmy = new NetworkArmyPiece[localArmy.Count];
		for (int i = 0; i < localArmy.Count; i++)
		{
			networkArmy[i] = new NetworkArmyPiece(localArmy[i].type, localArmy[i].x, localArmy[i].y);
		}

		if (IsServer)
		{
			SubmitArmyServerRpc(networkArmy, true);
		}
		else
		{
			SubmitArmyServerRpc(networkArmy, false);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	void SubmitArmyServerRpc(NetworkArmyPiece[] armyData, bool isHost)
	{
		if (isHost)
		{
			HostArmy.Clear();
			foreach (var p in armyData) HostArmy.Add(p);
			IsHostReady.Value = true;
			Debug.Log($"Host jest gotowy. Liczba jednostek: {HostArmy.Count}");
		}
		else
		{
			ClientArmy.Clear();
			foreach (var p in armyData) ClientArmy.Add(p);
			IsClientReady.Value = true;
			Debug.Log($"Klient jest gotowy. Liczba jednostek: {ClientArmy.Count}");
		}

		CheckStartBattle();
	}

	void CheckStartBattle()
	{
		if (IsHostReady.Value && IsClientReady.Value)
		{
			Debug.Log("Obaj gracze gotowi! Ładowanie Bitwy...");
			SceneFader.FadeOutThen(() =>
			{
				NetworkManager.Singleton.SceneManager.LoadScene("Battle", LoadSceneMode.Single);
			});
		}
	}

	public void ResetSessionState()
	{
		if (!IsServer)
		{
			return;
		}

		IsHostReady.Value = false;
		IsClientReady.Value = false;
		HostArmy.Clear();
		ClientArmy.Clear();
	}

	public void SwapTurn()
	{
		if (!IsServer) return; // Tylko serwer może zmieniać NetworkVariable

		ActiveTeam.Value = (ActiveTeam.Value == 0) ? 1 : 0;

		Debug.Log($"[BattleSession] Tura zmieniona. Teraz ruch gracza: {ActiveTeam.Value}");
	}
}
