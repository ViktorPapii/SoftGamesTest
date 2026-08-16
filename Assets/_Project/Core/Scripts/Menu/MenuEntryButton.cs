using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.Core
{
    /// <summary>
    /// One row on the menu, authored in the scene. It names the scene it opens and nothing else —
    /// MainMenuController is what turns a press into a load.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuEntryButton : MonoBehaviour
    {
        [Tooltip("Must match a catalog entry. MenuEntryTests fails if it does not.")]
        [SerializeField] private string sceneName;

        [SerializeField] private Button button;

        public string SceneName => sceneName;

        public Button Button => button;

        private void Reset()
        {
            button = GetComponent<Button>();
        }
    }
}
