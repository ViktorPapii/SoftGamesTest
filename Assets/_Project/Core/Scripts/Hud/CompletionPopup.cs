using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.Core
{
    /// <summary>
    /// The shared end-of-run panel on the persistent root. Core owns the Menu button; Retry is a
    /// callback from whoever raised the popup, and a null one hides the button.
    /// </summary>
    public class CompletionPopup : MonoBehaviour, ICompletionPopup
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text message;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        private IGameNavigation _navigation;
        private Action _onRetry;

        public bool IsOpen { get; private set; }

        public void Show(string text, Action onRetry)
        {
            message.text = text;
            _onRetry = onRetry;
            retryButton.gameObject.SetActive(onRetry != null);
            SetVisible(true);
        }

        /// <summary>
        /// Closes, and drops the retry callback — it closes over a controller the next load
        /// destroys. Hooked to the navigator's SceneChanging, so every transition clears it.
        /// </summary>
        public void Dismiss()
        {
            _onRetry = null;
            SetVisible(false);
        }

        /// <summary>
        /// Handed its navigator by GameManager. Subscribing here rather than in Awake, because a
        /// composition root cannot run before every Awake on the prefab it is part of.
        /// </summary>
        public void Init(IGameNavigation navigation)
        {
            _navigation = navigation;
            _navigation.SceneChanging += Dismiss;
        }

        private void Awake()
        {
            retryButton.onClick.AddListener(OnRetry);
            menuButton.onClick.AddListener(OnMenu);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            retryButton.onClick.RemoveListener(OnRetry);
            menuButton.onClick.RemoveListener(OnMenu);

            // Null when a duplicate manager tore this down before the root ever handed it one.
            if (_navigation != null)
            {
                _navigation.SceneChanging -= Dismiss;
            }
        }

        private void OnRetry()
        {
            // Taken before Dismiss, which clears it.
            Action retry = _onRetry;
            Dismiss();
            retry?.Invoke();
        }

        private void OnMenu()
        {
            Dismiss();
            _navigation.ReturnToMenu();
        }

        private void SetVisible(bool visible)
        {
            IsOpen = visible;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private void Reset()
        {
            group = GetComponent<CanvasGroup>();
        }
    }
}
