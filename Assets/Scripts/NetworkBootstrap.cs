using Unity.Netcode;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviour
{
	[Header("Przypisz tu prefab BattleSystem")]
	public GameObject battleSystemPrefab;

	private void Awake()
	{
		NetworkManager networkManager = GetComponent<NetworkManager>();
		if (NetworkManager.Singleton != null && NetworkManager.Singleton != networkManager)
		{
			Destroy(gameObject);
			return;
		}

		DontDestroyOnLoad(gameObject);
	}

	void Start()
	{
		if (NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.OnServerStarted += OnServerStarted;
		}
	}

	private void OnServerStarted()
	{
		if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
		{
			Debug.Log("Serwer wystartowa³ - spawnowanie BattleSystem...");
			GameObject go = Instantiate(battleSystemPrefab);
			go.GetComponent<NetworkObject>().Spawn(); 
		}
	}
}