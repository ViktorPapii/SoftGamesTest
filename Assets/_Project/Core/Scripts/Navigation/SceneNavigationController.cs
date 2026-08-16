using System;
using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoftGames.Core
{
    // The only thing that loads a scene: cover, announce, sweep, load, reclaim, reveal.
    public class SceneNavigationController : MonoBehaviour, IGameNavigation
    {
        [SerializeField] private SceneCatalog catalog;
        [SerializeField] private SceneFader fader;

        [Tooltip("Frees textures the outgoing scene owned — the downloaded portraits, mostly. " +
                 "Costs a stall on the covered frame and pays for itself on WebGL.")]
        [SerializeField] private bool unloadUnusedAssets = true;

        private bool _isBusy;

        // True from the first frame of the cover to the last frame of the reveal.
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Asked by UI that has to behave differently on the menu. Answered here so nothing else
        /// has to know a scene name, or that the catalog is where the menu is recorded.
        /// </summary>
        public bool IsOnMenu => catalog.MainMenu.SceneName == SceneManager.GetActiveScene().name;

        public event Action SceneChanging;

        public event Action StateChanged;

        // Fire and forget; callers follow the transition through IsBusy or StateChanged.
        public void Load(string sceneName)
        {
            _ = LoadAsync(sceneName);
        }

        public void ReturnToMenu()
        {
            Load(catalog.MainMenu.SceneName);
        }

        private async Awaitable LoadAsync(string sceneName)
        {
            // A double-click on a menu button, or an Exit press mid-transition.
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            CancellationToken token = destroyCancellationToken;

            try
            {
                await fader.CoverAsync(token);

                // Anything holding state that belongs to the outgoing scene drops it here.
                SceneChanging?.Invoke();

                // A sweep, not the guarantee: every tween alive now belongs to the outgoing scene.
                // What it starts before the load finishes is caught by SetLink instead. Safe to do
                // wholesale only because SceneFader owns no tween.
                DOTween.KillAll();

                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

                if (unloadUnusedAssets)
                {
                    await Resources.UnloadUnusedAssets();
                }
            }
            catch (OperationCanceledException)
            {
                // Quitting mid-transition.
            }
            catch (Exception exception)
            {
                // A scene missing from Build Settings lands here.
                Debug.LogError($"[{name}] Loading '{sceneName}' failed: {exception}", this);
            }
            finally
            {
                // Revealed however the load ended. The cover blocks raycasts, and on the menu the
                // Exit button is hidden, so a swallowed throw would strand the app behind an opaque
                // screen with nothing left to press.
                if (!token.IsCancellationRequested)
                {
                    await fader.RevealAsync(token);
                }

                IsBusy = false;
            }
        }
    }
}
