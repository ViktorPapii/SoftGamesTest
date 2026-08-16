using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.Core
{
    /// <summary>
    /// The chrome that is on screen in every scene: the way back to the menu and the frame rate
    /// readout. Owns what it does, what shows and where, so neither piece decides any of that for
    /// itself or knows the other exists.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private Button exitButton;

        [Tooltip("Toggled instead of the button's GameObject, which would take its click binding.")]
        [SerializeField] private CanvasGroup exitGroup;

        [Tooltip("The Exit button's own rect; the readout is parked past its right edge.")]
        [SerializeField] private RectTransform exitRect;

        [Tooltip("Parked past the Exit button while it shows, and sent to its corner when it does not.")]
        [SerializeField] private FpsCounter counter;

        private IGameNavigation _navigation;

        // Handed its navigator by GameManager; the views above are its own.
        public void Init(IGameNavigation navigation)
        {
            _navigation = navigation;
            _navigation.StateChanged += Apply;
            Apply();
        }

        private void Awake()
        {
            exitButton.onClick.AddListener(ReturnToMenu);
        }

        private void OnDestroy()
        {
            exitButton.onClick.RemoveListener(ReturnToMenu);

            // Null when a duplicate manager tore this down before the root ever handed it one.
            if (_navigation != null)
            {
                _navigation.StateChanged -= Apply;
            }
        }

        private void Apply()
        {
            // IsSwapping, not IsBusy: it drops while the screen is still covered, so the button and
            // the readout beside it are in place — and past the button's own colour fade — by the
            // time the reveal starts.
            bool showExit = !_navigation.IsOnMenu && !_navigation.IsSwapping;

            exitGroup.alpha = showExit ? 1f : 0f;
            exitGroup.interactable = showExit;
            exitGroup.blocksRaycasts = showExit;

            counter.Anchor = showExit ? exitRect : null;
        }

        private void ReturnToMenu()
        {
            _navigation.ReturnToMenu();
        }

        private void Reset()
        {
            exitButton = GetComponentInChildren<Button>(true);

            if (exitButton != null)
            {
                exitGroup = exitButton.GetComponent<CanvasGroup>();
                exitRect = exitButton.GetComponent<RectTransform>();
            }

            counter = GetComponent<FpsCounter>();
        }
    }
}
