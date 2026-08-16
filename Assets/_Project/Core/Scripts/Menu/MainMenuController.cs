using UnityEngine;

namespace SoftGames.Core
{
    /// <summary>
    /// Turns a press on an authored menu row into a scene load. The rows are laid out in the scene,
    /// so what the menu looks like is visible without entering play mode.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("The rows, in the order they appear. Each names the scene it opens.")]
        [SerializeField] private MenuEntryButton[] entries;

        private IGameNavigation _navigation;

        private void Start()
        {
            _navigation = GameManager.Instance.Navigation;

            foreach (MenuEntryButton entry in entries)
            {
                // Captured per row, so every button carries its own destination.
                string sceneName = entry.SceneName;
                entry.Button.onClick.AddListener(() => _navigation.Load(sceneName));
            }
        }
    }
}
