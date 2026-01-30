using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class EventSystemGuard : MonoBehaviour
{
	private void Awake()
	{
		SceneManager.sceneLoaded += (_, __) => Cleanup();
		Cleanup();
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= (_, __) => Cleanup();
	}

	private void Cleanup()
	{
		var systems = FindObjectsOfType<EventSystem>(true);
		if (systems == null || systems.Length <= 1) return;

		// Preferuj EventSystem z aktywnej sceny
		EventSystem keep = null;
		var active = SceneManager.GetActiveScene();
		foreach (var es in systems)
		{
			if (es != null && es.gameObject.scene == active)
			{
				keep = es;
				break;
			}
		}
		if (keep == null) keep = systems[0];

		foreach (var es in systems)
		{
			if (es != null && es != keep)
				Destroy(es.gameObject);
		}
	}
}
