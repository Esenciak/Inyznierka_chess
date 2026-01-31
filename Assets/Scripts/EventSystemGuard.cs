using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

public class EventSystemGuard : MonoBehaviour
{
	private static EventSystemGuard _instance;

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
			return;
		}

		_instance = this;
		DontDestroyOnLoad(gameObject);

		SceneManager.sceneLoaded += OnSceneLoaded;

		EnsureSingleEventSystem();
	}

	private void OnDestroy()
	{
		if (_instance == this)
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		EnsureSingleEventSystem();
	}

	private static void EnsureSingleEventSystem()
	{
		var systems = Object.FindObjectsOfType<EventSystem>(true);

		if (systems == null || systems.Length == 0)
		{
			CreateEventSystem();
			return;
		}

		// zostaw pierwszy, usuñ resztê
		var keep = systems[0];
		for (int i = 1; i < systems.Length; i++)
		{
			if (systems[i] != null)
				Object.Destroy(systems[i].gameObject);
		}

		if (!keep.gameObject.activeInHierarchy) keep.gameObject.SetActive(true);
		if (!keep.enabled) keep.enabled = true;

		EnsureCorrectInputModule(keep.gameObject);
	}

	private static void CreateEventSystem()
	{
		var go = new GameObject("EventSystem");
		go.AddComponent<EventSystem>();
		EnsureCorrectInputModule(go);
		Object.DontDestroyOnLoad(go);
	}

	private static void EnsureCorrectInputModule(GameObject eventSystemGO)
	{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New Input System only
        var legacy = eventSystemGO.GetComponent<StandaloneInputModule>();
        if (legacy != null) Object.Destroy(legacy);

        if (eventSystemGO.GetComponent<InputSystemUIInputModule>() == null)
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
#else
		// Legacy (albo Both) - zapewnij StandaloneInputModule
		// Usuñ InputSystemUIInputModule jeœli ktoœ go przyniós³ prefabem/scen¹ (bez twardej referencji)
		var inputSystemModule = eventSystemGO.GetComponent("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
		if (inputSystemModule != null) Object.Destroy(inputSystemModule);

		if (eventSystemGO.GetComponent<StandaloneInputModule>() == null)
			eventSystemGO.AddComponent<StandaloneInputModule>();
#endif
	}
}
