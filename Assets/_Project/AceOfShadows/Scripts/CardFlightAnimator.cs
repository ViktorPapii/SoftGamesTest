using DG.Tweening;
using UnityEngine;

namespace SoftGames.AceOfShadows
{
    // Builds the flight tween for one card and returns it; the caller adds its own OnComplete.
    public class CardFlightAnimator : MonoBehaviour
    {
        [Min(0.05f)]
        [SerializeField] private float duration = 0.6f;

        [Tooltip("Height of the toss arc, in world units.")]
        [SerializeField] private float arcHeight = 1.5f;

        [Tooltip("Z-axis spin over the flight. 360 returns the card to its original orientation.")]
        [SerializeField] private float spinDegrees = 360f;

        [SerializeField] private Ease spinEase = Ease.InOutSine;

        [Tooltip("Peak scale mid-flight, selling the card lifting toward the viewer.")]
        [SerializeField] private float scalePunch = 1.12f;

        public Sequence Fly(CardView card, Vector3 targetWorldPosition)
        {
            Transform cardTransform = card.CachedTransform;

            // Drop anything left from this card's previous flight or restack.
            cardTransform.DOKill();

            Sequence sequence = DOTween.Sequence();
            sequence.Append(cardTransform.DOJump(targetWorldPosition, arcHeight, 1, duration));
            sequence.Join(cardTransform
                .DOLocalRotate(new Vector3(0f, 0f, spinDegrees), duration, RotateMode.LocalAxisAdd)
                .SetEase(spinEase));

            if (!Mathf.Approximately(scalePunch, 1f))
            {
                sequence.Join(cardTransform
                    .DOScale(Vector3.one * scalePunch, duration * 0.5f)
                    .SetLoops(2, LoopType.Yoyo));
            }

            // Sequences are killable only by target; untargeted, this outlives the pooled card.
            sequence.SetTarget(cardTransform);
            sequence.SetLink(card.gameObject);

            return sequence;
        }
    }
}
