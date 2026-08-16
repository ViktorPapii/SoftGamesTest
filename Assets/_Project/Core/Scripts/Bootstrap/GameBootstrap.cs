using UnityEngine;

namespace SoftGames.Core
{
    /// <summary>
    /// Builds the <see cref="GameManager"/> if none is alive. Every scene needs one: without it a
    /// scene has no HUD, no way back and no EventSystem, and logs nothing to say so. BootstrapTests
    /// fails when a built scene is missing one.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("The persistent manager prefab. Instantiated only if one is not already alive.")]
        [SerializeField] private GameManager managerPrefab;

        private void Awake()
        {
            if (GameManager.Instance != null)
            {
                return;
            }

            // Instantiate runs the manager's Awake, so Instance is set before any Start.
            GameManager manager = Instantiate(managerPrefab);
            manager.name = managerPrefab.name;
        }
    }
}
