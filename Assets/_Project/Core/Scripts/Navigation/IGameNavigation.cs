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
        /// Raised when <see cref="IsBusy"/> or <see cref="IsSwapping"/> flips, which between them
        /// cover every moment an answer here can change — <see cref="IsOnMenu"/> included, since a
        /// scene only ever arrives while a swap is ending.
        /// </summary>
        event Action StateChanged;

        // True from the first frame of the cover to the last frame of the reveal.
        bool IsBusy { get; }

        /// <summary>
        /// True from the first frame of the cover until the incoming scene is in place, which is
        /// still behind it. Scene chrome hides on this rather than on <see cref="IsBusy"/>, so it
        /// is positioned while the screen is black instead of appearing once the reveal has run.
        /// </summary>
        bool IsSwapping { get; }

        bool IsOnMenu { get; }

        void Load(string sceneName);

        void ReturnToMenu();
    }
}
