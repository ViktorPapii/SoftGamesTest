using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

namespace SoftGames.AceOfShadows
{
    // Recycles card views — only a handful of the 144 are on screen at once.
    public class CardPool : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;

        [Tooltip("Cards created up front. Worst case in use is (visible slots x 2 decks) + cards in flight.")]
        [Min(1)]
        [SerializeField] private int prewarmCount = 10;

        [Tooltip("Hard ceiling on idle cards. A ceiling, not a target — set at or below prewarmCount " +
                 "it would destroy and re-instantiate a card on every transfer.")]
        [Min(1)]
        [SerializeField] private int maxSize = 64;

        private ObjectPool<CardView> _pool;
        private int _nameCounter;
        private bool _isPrewarming;
        private bool _warnedAboutGrowth;

        // Creates cards until prewarmCount are sitting idle.
        public void Prewarm()
        {
            EnsurePool();

            int missing = prewarmCount - _pool.CountInactive;
            if (missing <= 0)
            {
                return;
            }

            // ObjectPool has no prewarm; get-then-release is the idiom. The flag suppresses the
            // growth warning for these creations.
            _isPrewarming = true;

            CardView[] warm = new CardView[missing];
            for (int i = 0; i < missing; i++)
            {
                warm[i] = _pool.Get();
            }

            for (int i = 0; i < missing; i++)
            {
                _pool.Release(warm[i]);
            }

            _isPrewarming = false;
        }

        /// <summary>
        /// Destroys idle cards above <see cref="prewarmCount"/>; call <see cref="Prewarm"/> to
        /// refill. No-op unless the pool grew. Expects every card handed back first, since
        /// <see cref="ObjectPool{T}.Clear"/> discards the whole idle set.
        /// </summary>
        public void TrimToPrewarm()
        {
            _warnedAboutGrowth = false;

            if (_pool == null || _pool.CountAll <= prewarmCount)
            {
                return;
            }

            int before = _pool.CountAll;
            _pool.Clear();
            Debug.Log($"[{name}] Trimmed pool from {before} to {_pool.CountAll}; refilling to {prewarmCount}.", this);
        }

        public CardView Get()
        {
            EnsurePool();
            return _pool.Get();
        }

        public void Release(CardView card)
        {
            if (card == null)
            {
                return;
            }

            EnsurePool();
            _pool.Release(card);
        }

        private void EnsurePool()
        {
            _pool ??= new ObjectPool<CardView>(
                createFunc: CreateCard,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyCard,
                collectionCheck: true,
                defaultCapacity: prewarmCount,
                maxSize: Mathf.Max(maxSize, prewarmCount));
        }

        // Called only when no idle card is available, so it doubles as the growth warning.
        private CardView CreateCard()
        {
            if (!_isPrewarming && !_warnedAboutGrowth)
            {
                _warnedAboutGrowth = true;
                Debug.LogWarning(
                    $"[{name}] Pool of {prewarmCount} exhausted; growing. Raise prewarmCount or " +
                    "shorten the flight duration.", this);
            }

            CardView card = Instantiate(cardPrefab, transform);
            card.name = $"PooledCard_{_nameCounter++:D2}";
            return card;
        }

        private void OnGet(CardView card)
        {
            card.gameObject.SetActive(true);
        }

        private void OnRelease(CardView card)
        {
            // Clear everything from the card's previous life.
            card.CachedTransform.DOKill();
            card.SlotIndex = CardView.NoSlot;
            card.gameObject.SetActive(false);
            card.CachedTransform.SetParent(transform, false);
            card.CachedTransform.localPosition = Vector3.zero;
            card.CachedTransform.localRotation = Quaternion.identity;
            card.CachedTransform.localScale = Vector3.one;
        }

        private void OnDestroyCard(CardView card)
        {
            if (card == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(card.gameObject);
            }
            else
            {
                DestroyImmediate(card.gameObject);
            }
        }
    }
}
