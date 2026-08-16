using System;

namespace SoftGames.Core
{
    /// <summary>
    /// All UI is allowed to know about loading a scene. Keeps scene names, SceneManager and the
    /// catalog's shape out of every button and panel that only needs to ask a question or leave.
    /// </summary>
    public interface IGameNavigation
    {
        /// <summary>
        /// Raised behind the cover, before the outgoing scene is torn down. Anything holding state
        /// that belongs to that scene drops it here, without the navigator knowing who listens.
        /// </summary>
        event Action SceneChanging;

        /// <summary>
        /// Raised when <see cref="IsBusy"/> flips, which is the only moment either answer here can
        /// change: a transition starting hides scene chrome, and by the time one ends
        /// <see cref="IsOnMenu"/> already describes the scene that arrived.
        /// </summary>
        event Action StateChanged;

        // True from the first frame of the cover to the last frame of the reveal.
        bool IsBusy { get; }

        bool IsOnMenu { get; }

        void Load(string sceneName);

        void ReturnToMenu();
    }
}
