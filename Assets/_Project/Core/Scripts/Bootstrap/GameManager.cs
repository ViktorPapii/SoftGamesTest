using UnityEngine;

namespace SoftGames.Core
{
    /// <summary>
    /// The one object that outlives a scene change, carrying the HUD, the fader, the completion
    /// popup and the single EventSystem. Built by the <see cref="GameBootstrap"/> in each scene.
    ///
    /// Also the composition root: it holds the concrete pieces and hands each one what it needs, so
    /// nothing below it looks anything up or knows a sibling's type.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Composition")]
        [SerializeField] private SceneNavigationController navigation;

        [Tooltip("Shared end-of-run panel. Games raise it through the Completion property below.")]
        [SerializeField] private CompletionPopup completion;

        [SerializeField] private GameHud hud;

        [Tooltip("Frame cap applied at startup. Not applied on WebGL, which follows the browser.")]
        [Min(0)]
        [SerializeField] private int targetFrameRate = 60;

        public static GameManager Instance { get; private set; }

        public IGameNavigation Navigation => navigation;

        public ICompletionPopup Completion => completion;

        private void Awake()
        {
            // Backstop; GameBootstrap already checks before instantiating.
            if (Instance != null && Instance != this)
            {
                // Deactivate first: Destroy lands at end of frame, and until then this copy's
                // canvases would draw a second HUD over the first.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Wiring happens here, not in each component's Awake, because Awake order within a
            // prefab is not defined — a component cannot rely on having been handed anything yet.
            completion.Init(navigation);
            hud.Init(navigation);

            // Setting this on WebGL swaps requestAnimationFrame for setTimeout scheduling, which
            // drifts against the browser's vsync and shows up as periodic dropped frames.
#if !UNITY_WEBGL
            if (targetFrameRate > 0)
            {
                Application.targetFrameRate = targetFrameRate;
            }
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
