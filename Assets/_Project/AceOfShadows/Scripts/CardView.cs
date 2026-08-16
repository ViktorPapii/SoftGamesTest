using UnityEngine;

namespace SoftGames.AceOfShadows
{
    // Face sprite and draw order for one card. No update loop, no knowledge of decks.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CardView : MonoBehaviour
    {
        public const int NoSlot = -1;

        [SerializeField] private SpriteRenderer spriteRenderer;

        // Slot this card is laid out into. Lets the deck skip a relayout that would restart a running tween.
        public int SlotIndex { get; set; } = NoSlot;

        public Transform CachedTransform { get; private set; }

        public void SetFace(Sprite sprite)
        {
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        public void SetSortingOrder(int order)
        {
            spriteRenderer.sortingOrder = order;
        }

        private void Awake()
        {
            CachedTransform = transform;
        }

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
