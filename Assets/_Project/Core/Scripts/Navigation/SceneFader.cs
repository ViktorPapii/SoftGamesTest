using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.Core
{
    /// <summary>
    /// The black cover a scene change happens behind. Animates itself and never tweens — that is
    /// what lets SceneNavigationController sweep DOTween wholesale on unload.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;

        [Tooltip("The full-screen cover. Its colour is what a transition looks like.")]
        [SerializeField] private Image cover;

        [Min(0f)]
        [SerializeField] private float coverDuration = 0.25f;

        [Min(0f)]
        [SerializeField] private float revealDuration = 0.35f;

        [Tooltip("Alpha over the transition, sampled 0..1. Ends must sit at 0 and 1.")]
        [SerializeField] private AnimationCurve shape = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


        public Awaitable CoverAsync(CancellationToken token) => FadeAsync(1f, coverDuration, token);

        public Awaitable RevealAsync(CancellationToken token) => FadeAsync(0f, revealDuration, token);

        // Unscaled: a paused or hitching scene must not strand a half-faded screen.
        private async Awaitable FadeAsync(float targetAlpha, float duration, CancellationToken token)
        {
            // Blocked for the whole transition, so a click cannot cross between scenes.
            group.blocksRaycasts = true;

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                await Awaitable.NextFrameAsync(token);
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, shape.Evaluate(Mathf.Clamp01(elapsed / duration)));
            }

            group.alpha = targetAlpha;
            group.blocksRaycasts = targetAlpha > 0f;
        }

        private void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void Reset()
        {
            group = GetComponent<CanvasGroup>();
            cover = GetComponentInChildren<Image>();
        }
    }
}
