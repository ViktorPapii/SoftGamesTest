using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace SoftGames.Core.Tests
{
    // Gets a play mode test into a running game, entering through the menu the way a player does.
    internal static class TestSession
    {
        internal static IEnumerator Enter()
        {
            if (GameManager.Instance == null)
            {
                yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

                for (int frame = 0; frame < 10 && GameManager.Instance == null; frame++)
                {
                    yield return null;
                }

                Assert.IsNotNull(GameManager.Instance,
                    "Loading MainMenu produced no GameManager. The scene should carry a " +
                    "GameBootstrap with its manager prefab assigned.");
            }
            else
            {
                yield return ReturnToMenu();
            }

            // The menu builds its rows in Start.
            yield return null;
        }

        internal static IEnumerator ReturnToMenu()
        {
            IGameNavigation navigation = GameManager.Instance.Navigation;
            navigation.ReturnToMenu();

            while (navigation.IsBusy)
            {
                yield return null;
            }
        }

        // Awaitable is not an IEnumerator, so the transition is followed through IsBusy, which
        // Load sets before its first await.
        internal static IEnumerator Navigate(string sceneName)
        {
            IGameNavigation navigation = GameManager.Instance.Navigation;
            navigation.Load(sceneName);

            while (navigation.IsBusy)
            {
                yield return null;
            }
        }
    }
}
