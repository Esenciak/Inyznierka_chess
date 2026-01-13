using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
        public static SceneFader Instance { get; private set; }

        [SerializeField] private float fadeDuration = 1f;

        private CanvasGroup canvasGroup;
        private Coroutine activeFade;
        private bool isNetworkSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnLoad()
        {
                EnsureInstance();
        }

        public static void EnsureInstance()
        {
                if (Instance != null)
                {
                        return;
                }

                GameObject go = new GameObject("SceneFader");
                go.AddComponent<SceneFader>();
        }

        public static void LoadSceneWithFade(string sceneName)
        {
                if (Instance == null)
                {
                        SceneManager.LoadScene(sceneName);
                        return;
                }

                Instance.FadeOutAndLoad(sceneName);
        }

        public static void FadeOutThen(Action onComplete)
        {
                if (Instance == null)
                {
                        onComplete?.Invoke();
                        return;
                }

                Instance.FadeOutAndRun(onComplete);
        }

        private void Awake()
        {
                if (Instance != null && Instance != this)
                {
                        Destroy(gameObject);
                        return;
                }

                Instance = this;
                DontDestroyOnLoad(gameObject);
                BuildCanvas();
        }

        private void OnEnable()
        {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                TrySubscribeToNetworkEvents();
        }

        private void OnDisable()
        {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                UnsubscribeFromNetworkEvents();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
                FadeIn();
                TrySubscribeToNetworkEvents();
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject imageObject = new GameObject("FadeImage", typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = Color.black;

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            canvasGroup = imageObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void FadeOutAndLoad(string sceneName)
        {
                FadeOutAndRun(() => SceneManager.LoadScene(sceneName));
        }

        private void FadeOutAndRun(Action onComplete)
        {
                StartFade(1f, onComplete);
        }

        private void FadeIn()
        {
                StartFade(0f, null);
        }

        private void StartFade(float targetAlpha, Action onComplete)
        {
                if (canvasGroup == null)
                {
                        onComplete?.Invoke();
                        return;
                }

                if (activeFade != null)
                {
                        StopCoroutine(activeFade);
                }

                activeFade = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
        }

        private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
        {
                float startAlpha = canvasGroup.alpha;
                float time = 0f;
                while (time < fadeDuration)
                {
                        time += Time.unscaledDeltaTime;
                        float t = Mathf.Clamp01(time / fadeDuration);
                        canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                        yield return null;
                }

                canvasGroup.alpha = targetAlpha;
                onComplete?.Invoke();
        }

        private void TrySubscribeToNetworkEvents()
        {
                if (isNetworkSubscribed || Unity.Netcode.NetworkManager.Singleton == null)
                {
                        return;
                }

                var sceneManager = Unity.Netcode.NetworkManager.Singleton.SceneManager;
                if (sceneManager == null)
                {
                        return;
                }

                sceneManager.OnSceneEvent += HandleNetworkSceneEvent;
                isNetworkSubscribed = true;
        }

        private void UnsubscribeFromNetworkEvents()
        {
                if (!isNetworkSubscribed || Unity.Netcode.NetworkManager.Singleton == null)
                {
                        return;
                }

                var sceneManager = Unity.Netcode.NetworkManager.Singleton.SceneManager;
                if (sceneManager == null)
                {
                        return;
                }

                sceneManager.OnSceneEvent -= HandleNetworkSceneEvent;
                isNetworkSubscribed = false;
        }

        private void HandleNetworkSceneEvent(Unity.Netcode.SceneEvent sceneEvent)
        {
                if (sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.Load)
                {
                        StartFade(1f, null);
                }
        }
}
