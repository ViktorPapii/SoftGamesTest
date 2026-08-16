using System;

namespace SoftGames.Core
{
    /// <summary>
    /// The shared end-of-run panel, as a game sees it: a message and a retry that means whatever
    /// restarting that game means. A null retry hides the button.
    /// </summary>
    public interface ICompletionPopup
    {
        bool IsOpen { get; }

        void Show(string message, Action onRetry);

        void Dismiss();
    }
}
