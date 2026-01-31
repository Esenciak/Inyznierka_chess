using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleCoordinateOverlay : MonoBehaviour
{
    private const string BattleSceneName = "Battle";
    private const string LabelObjectName = "BattleCoordLabel";
    private const int LabelSortingOrder = 400;
    private static BattleCoordinateOverlay instance;

    private TelemetryConfig config;
    private readonly List<GameObject> labels = new List<GameObject>();
    private Coroutine buildRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject overlayRoot = new GameObject("BattleCoordinateOverlay");
        instance = overlayRoot.AddComponent<BattleCoordinateOverlay>();
        DontDestroyOnLoad(overlayRoot);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != BattleSceneName)
        {
            ClearLabels();
            return;
        }

        if (buildRoutine != null)
        {
            StopCoroutine(buildRoutine);
        }

        buildRoutine = StartCoroutine(BuildWhenReady());
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        if (scene.name == BattleSceneName)
        {
            ClearLabels();
        }
    }

    private IEnumerator BuildWhenReady()
    {
        config = config != null ? config : Resources.Load<TelemetryConfig>("Telemetry/TelemetryConfig");
        if (config == null || !config.showBattleCoordsDebug)
        {
            yield break;
        }

        while (BoardManager.Instance == null || !BoardManager.Instance.IsReady)
        {
            yield return null;
        }

        BuildLabels();
    }

    private void BuildLabels()
    {
        ClearLabels();

        Tile[] tiles = FindObjectsOfType<Tile>(true);
        foreach (Tile tile in tiles)
        {
            if (tile == null)
            {
                continue;
            }

            if (tile.boardType != BoardType.Player && tile.boardType != BoardType.Center && tile.boardType != BoardType.Enemy)
            {
                continue;
            }

            GameObject labelObject = new GameObject(LabelObjectName, typeof(TextMeshPro));
            labelObject.transform.SetParent(tile.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.2f);

            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.text = $"{tile.globalCol},{tile.globalRow}";
            label.fontSize = 2.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            MeshRenderer meshRenderer = label.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = LabelSortingOrder;
            }

            labels.Add(labelObject);
        }
    }

    private void ClearLabels()
    {
        if (buildRoutine != null)
        {
            StopCoroutine(buildRoutine);
            buildRoutine = null;
        }

        foreach (GameObject label in labels)
        {
            if (label != null)
            {
                Destroy(label);
            }
        }

        labels.Clear();
    }
}
