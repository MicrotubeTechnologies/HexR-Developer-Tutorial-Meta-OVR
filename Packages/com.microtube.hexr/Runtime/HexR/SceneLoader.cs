using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HexR
{
    /// <summary>
    /// Manages scene transitions across the application.
    /// Attach to the same persistent GameObject as HexRManager.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Singleton
        // ─────────────────────────────────────────────

        public static SceneLoader Instance { get; private set; }

        // ─────────────────────────────────────────────
        // Events
        // ─────────────────────────────────────────────

        public event System.Action OnSceneLoadStarted;
        public event System.Action OnSceneLoadCompleted;

        // ─────────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────────

        [Header("Settings")]
        [SerializeField] private float transitionDelay = 0f;

        private bool _isLoading;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {

        }
        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>Load a scene by name.</summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>Load a scene by build index.</summary>
        public void LoadScene(int sceneIndex)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneRoutine(sceneIndex.ToString(), sceneIndex));
        }

        /// <summary>Reload the currently active scene.</summary>
        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Load the next scene in build order.</summary>
        public void LoadNextScene()
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("[SceneLoader] No next scene — already at last scene.");
                return;
            }

            LoadScene(nextIndex);
        }

        /// <summary>Load the previous scene in build order.</summary>
        public void LoadPreviousScene()
        {
            int prevIndex = SceneManager.GetActiveScene().buildIndex - 1;

            if (prevIndex < 0)
            {
                Debug.LogWarning("[SceneLoader] No previous scene — already at first scene.");
                return;
            }

            LoadScene(prevIndex);
        }

        /// <summary>Returns the name of the currently active scene.</summary>
        public string GetCurrentSceneName() => SceneManager.GetActiveScene().name;

        /// <summary>Returns the build index of the currently active scene.</summary>
        public int GetCurrentSceneIndex() => SceneManager.GetActiveScene().buildIndex;

        // ─────────────────────────────────────────────
        // Coroutines
        // ─────────────────────────────────────────────

        private IEnumerator LoadSceneRoutine(string sceneName, int sceneIndex = -1)
        {
            _isLoading = true;
            OnSceneLoadStarted?.Invoke();

            if (transitionDelay > 0f)
                yield return new WaitForSeconds(transitionDelay);

            AsyncOperation operation = sceneIndex >= 0
                ? SceneManager.LoadSceneAsync(sceneIndex)
                : SceneManager.LoadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene: {sceneName}. Is it added to Build Settings?");
                _isLoading = false;
                yield break;
            }

            // Optionally hold the scene from activating (useful if you want a loading screen)
            // operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                // operation.progress goes from 0 to 0.9 then jumps to 1 when done
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                Debug.Log($"[SceneLoader] Loading: {Mathf.RoundToInt(progress * 100)}%");
                yield return null;
            }

            _isLoading = false;
            OnSceneLoadCompleted?.Invoke();
        }
    }
}
