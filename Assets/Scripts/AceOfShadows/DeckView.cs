using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SoftGames.AceOfShadows
{
    /// <summary>
    /// Shows a deck as <see cref="visibleDepth"/> stacked slots; the rest of it is only a count.
    /// At 144 cards the per-card offset would fall below a pixel, so the extra cards are not objects.
    /// <see cref="Relayout"/> is the single pass — slots and draw order both come from it.
    /// </summary>
    public class DeckView : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("How many cards are physically shown. The rest of the deck exists only as a count.")]
        [Min(1)]
        [SerializeField] private int visibleDepth = 2;

        [Tooltip("Local offset between one slot and the next; slot 0 is the top card.")]
        [SerializeField] private Vector3 slotOffset = new(-0.22f, -0.26f, 0f);

        [Tooltip("Draw order floor for this deck. Must differ between decks by more than visibleDepth.")]
        [SerializeField] private int baseSortingOrder;

        [Header("Restack animation")]
        [Min(0f)]
        [SerializeField] private float restackDuration = 0.25f;

        [SerializeField] private Ease restackEase = Ease.OutQuad;

        [Tooltip("Hide cards authored into the scene at edit time; they are layout aids, not runtime objects.")]
        [SerializeField] private bool hideAuthoredChildrenOnInit = true;

        // Visible cards, index 0 being the most recently landed.
        private readonly List<CardView> _residents = new();

        // Cards pushed out of the stack, still playing their exit. Several overlap once transfers
        // come faster than restackDuration.
        private readonly List<CardView> _retiring = new();

        private CardPool _pool;
        private CardDeckAsset _deckAsset;
        private int _retireOrder;

        // Cards in flight toward this deck.
        private int _pendingIncoming;

        public float RestackDuration => restackDuration;

        // Deepest visible slot, where a retired card waits to be covered.
        private int CoveredSlot => visibleDepth - 1;

        /// <summary>
        /// Slots held for cards in the air, capped so the deck always keeps one visible card — the
        /// one that covers the retired card.
        /// </summary>
        private int ReservedSlots => Mathf.Min(_pendingIncoming, visibleDepth - 1);

        public Vector3 GetSlotWorldPosition(int slot) => transform.TransformPoint(slotOffset * slot);

        public void Initialize(CardPool cardPool, CardDeckAsset cards)
        {
            _pool = cardPool;
            _deckAsset = cards;

            if (hideAuthoredChildrenOnInit)
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        // Rebuilds the visible slots from model with no animation.
        public void Fill(DeckModel model)
        {
            ReleaseAll();
            _pendingIncoming = 0;
            SyncWithModel(model, animated: false);
        }

        // Removes and returns the top card; the caller reparents it for flight.
        public CardView DetachTop()
        {
            if (_residents.Count == 0)
            {
                return null;
            }

            CardView card = _residents[0];
            _residents.RemoveAt(0);
            card.SlotIndex = CardView.NoSlot;
            return card;
        }

        // Reveals a deeper card from the pool once the top one is uncovered.
        public void SyncWithModel(DeckModel model, bool animated = true)
        {
            int desired = Mathf.Clamp(visibleDepth - ReservedSlots, 0, model.Count);

            while (_residents.Count < desired)
            {
                int depth = _residents.Count;
                CardView card = TakeFromPool(model.PeekFromTop(depth));

                // Revealed from inside the deck: place it, never slide it in.
                card.CachedTransform.localPosition = slotOffset * (depth + ReservedSlots);
                _residents.Add(card);
            }

            Relayout(animated);
        }

        // Holds a slot for a card that has just launched toward this deck.
        public void NotifyIncoming()
        {
            _pendingIncoming++;
            Relayout(animated: true);
        }

        // Accepts a card that has finished its flight.
        public void ReceiveCard(CardView card)
        {
            _pendingIncoming = Mathf.Max(0, _pendingIncoming - 1);

            // Keep the position the flight ended on.
            card.CachedTransform.SetParent(transform, worldPositionStays: true);
            _residents.Insert(0, card);

            // Its flight sequence is mid-callback; killing tweens on it would kill that sequence.
            Relayout(animated: true, skipKillFor: card);
        }

        public void ReleaseAll()
        {
            for (int i = _residents.Count - 1; i >= 0; i--)
            {
                _pool.Release(_residents[i]);
                _residents.RemoveAt(i);
            }

            for (int i = _retiring.Count - 1; i >= 0; i--)
            {
                _pool.Release(_retiring[i]);
            }

            _retiring.Clear();
            _pendingIncoming = 0;
        }

        // True while cards are still settling, so a caller can wait for the deck to come to rest.
        public bool IsAnimating => _retiring.Count > 0;

        // Gives every resident its slot and retires whatever no longer fits.
        private void Relayout(bool animated, CardView skipKillFor = null)
        {
            for (int i = _residents.Count - 1; i >= 0; i--)
            {
                if (i + ReservedSlots >= visibleDepth)
                {
                    Retire(i, animated);
                }
            }

            for (int i = 0; i < _residents.Count; i++)
            {
                CardView card = _residents[i];
                int slot = i + ReservedSlots;
                Vector3 target = slotOffset * slot;
                bool mayKill = card != skipKillFor;

                bool slotUnchanged = card.SlotIndex == slot;
                card.SlotIndex = slot;
                card.SetSortingOrder(baseSortingOrder + (visibleDepth - slot));

                if (!animated || restackDuration <= 0f)
                {
                    if (mayKill)
                    {
                        card.CachedTransform.DOKill();
                    }

                    card.CachedTransform.localPosition = target;
                    card.CachedTransform.localRotation = Quaternion.identity;
                    card.CachedTransform.localScale = Vector3.one;
                    continue;
                }

                // Destination unchanged: let the running settle finish. Restarting resets its clock,
                // and below restackDuration it never arrives.
                if (slotUnchanged && mayKill)
                {
                    continue;
                }

                if (mayKill)
                {
                    card.CachedTransform.DOKill();
                }

                card.CachedTransform.DOLocalMove(target, restackDuration)
                    .SetEase(restackEase)
                    .SetLink(card.gameObject);

                // Undo the flight spin.
                card.CachedTransform.DOLocalRotate(Vector3.zero, restackDuration)
                    .SetEase(restackEase)
                    .SetLink(card.gameObject);

                card.CachedTransform.DOScale(Vector3.one, restackDuration)
                    .SetEase(restackEase)
                    .SetLink(card.gameObject);
            }
        }

        /// <summary>
        /// Slides the card onto the covered slot, where its replacement lands on top and hides it,
        /// then pools it when that slide ends. Both tweens start in this same pass with the same
        /// duration, so the card is covered exactly when it is released.
        /// </summary>
        private void Retire(int index, bool animated)
        {
            CardView card = _residents[index];
            _residents.RemoveAt(index);
            card.SlotIndex = CardView.NoSlot;

            if (!animated || restackDuration <= 0f)
            {
                _pool.Release(card);
                return;
            }

            // Distinct orders while several overlap, all below every resident.
            if (_retiring.Count == 0)
            {
                _retireOrder = baseSortingOrder;
            }

            card.SetSortingOrder(--_retireOrder);
            _retiring.Add(card);

            Transform cardTransform = card.CachedTransform;
            cardTransform.DOKill();

            Sequence exit = DOTween.Sequence();
            exit.Append(cardTransform.DOLocalMove(slotOffset * CoveredSlot, restackDuration).SetEase(restackEase));
            exit.Join(cardTransform.DOLocalRotate(Vector3.zero, restackDuration).SetEase(restackEase));
            exit.Join(cardTransform.DOScale(Vector3.one, restackDuration).SetEase(restackEase));

            // Sequences are killable only by target; untargeted, this outlives the pooled card.
            exit.SetTarget(cardTransform);
            exit.SetLink(card.gameObject);
            exit.OnComplete(() =>
            {
                _retiring.Remove(card);
                _pool.Release(card);
            });
        }

        private CardView TakeFromPool(int cardId)
        {
            CardView card = _pool.Get();
            card.SetFace(_deckAsset.GetFace(cardId));

            Transform cardTransform = card.CachedTransform;
            cardTransform.SetParent(transform, worldPositionStays: false);
            cardTransform.localRotation = Quaternion.identity;
            cardTransform.localScale = Vector3.one;

            return card;
        }
    }
}
