using System.Collections;
using DG.Tweening;
using SoftGames.Core;
using UnityEngine;

namespace SoftGames.AceOfShadows
{
    /// <summary>
    /// Sends the source deck's top card to the target deck <see cref="transferSpeed"/> times a
    /// second until the source is empty. Deck state lives in <see cref="DeckModel"/> and changes the
    /// instant a transfer starts, never read back from the scene — a card in flight is in neither
    /// deck, so several can be airborne at once.
    /// </summary>
    public class AceOfShadowsController : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private DeckView sourceDeck;
        [SerializeField] private DeckView targetDeck;
        [SerializeField] private CardPool pool;
        [SerializeField] private CardFlightAnimator flightAnimator;
        [SerializeField] private CardDeckAsset deckAsset;
        [SerializeField] private DeckCounterView sourceCounter;
        [SerializeField] private DeckCounterView targetCounter;

        [Tooltip("Parent for cards while airborne. Defaults to this transform.")]
        [SerializeField] private Transform flightRoot;

        [Header("Rules")]
        [Min(0)]
        [SerializeField] private int totalCards = 144;

        [Tooltip("Cards moved per second. The brief specifies 1; raise it to reach the end state sooner.")]
        [Min(0.01f)]
        [SerializeField] private float transferSpeed = 1f;

        [Tooltip("Slowest rate the run can be set to.")]
        [Min(0.01f)]
        [SerializeField] private float minSpeed = 0.5f;

        [Tooltip("Fastest rate. Past one card per flight duration, cards overlap in the air.")]
        [Min(0.02f)]
        [SerializeField] private float maxSpeed = 10f;

        [Tooltip("Draw order floor for airborne cards. Must sit above both decks.")]
        [SerializeField] private int flightSortingOrder = 1000;

        [Tooltip("Deal as soon as the scene is up. Off leaves both decks empty until Begin.")]
        [SerializeField] private bool autoStart = true;

        private DeckModel _sourceModel;
        private DeckModel _targetModel;
        private Coroutine _transferRoutine;
        private int _inFlight;
        private int _launchCount;

        public float MinSpeed => minSpeed;

        // Guards an inverted pair in the inspector, which would otherwise make the range empty.
        public float MaxSpeed => Mathf.Max(maxSpeed, minSpeed);

        /// <summary>
        /// Cards moved per second, clamped to MinSpeed..MaxSpeed. Safe to change mid-run: the wait
        /// is measured against the current value each frame, so a new value applies immediately.
        /// </summary>
        public float TransferSpeed
        {
            get => transferSpeed;
            set => transferSpeed = Mathf.Clamp(value, MinSpeed, MaxSpeed);
        }

        // Entry point: tears any running deal down and redeals. Also the popup's Retry.
        private void Begin()
        {
            if (!HasFaceSprites())
            {
                return;
            }

            Stop();

            _sourceModel = new DeckModel(totalCards);
            _targetModel = new DeckModel();

            // Stop() has handed every card back, so the pool can be fully reclaimed here.
            pool.TrimToPrewarm();
            pool.Prewarm();

            sourceDeck.Initialize(pool, deckAsset);
            targetDeck.Initialize(pool, deckAsset);
            sourceDeck.Fill(_sourceModel);
            targetDeck.Fill(_targetModel);

            sourceCounter.Bind(_sourceModel);
            targetCounter.Bind(_targetModel);

            _inFlight = 0;
            _launchCount = 0;
            _transferRoutine = StartCoroutine(RunTransfers());
        }

        private void Stop()
        {
            if (_transferRoutine != null)
            {
                StopCoroutine(_transferRoutine);
                _transferRoutine = null;
            }

            sourceDeck.ReleaseAll();
            targetDeck.ReleaseAll();
            ReclaimAirborneCards();
        }

        /// <summary>
        /// Pools cards caught mid-flight. They belong to neither deck, so the flight parent is the
        /// only record of them. Pooling kills the flight sequence, so its landing callback never
        /// runs against the torn-down run.
        /// </summary>
        private void ReclaimAirborneCards()
        {
            Transform root = flightRoot != null ? flightRoot : transform;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                if (root.GetChild(i).TryGetComponent(out CardView card))
                {
                    pool.Release(card);
                }
            }

            _inFlight = 0;
        }

        private IEnumerator RunTransfers()
        {
            float sinceLastTransfer = 0f;

            while (!_sourceModel.IsEmpty)
            {
                yield return null;

                sinceLastTransfer += Time.deltaTime;

                float interval = 1f / transferSpeed;
                if (sinceLastTransfer < interval)
                {
                    continue;
                }

                // Keep the overshoot so intervals don't quantise to the frame rate.
                sinceLastTransfer -= interval;
                TransferTopCard();
            }

            // The last cards are still in the air, and their landings still have to settle.
            while (_inFlight > 0 || sourceDeck.IsAnimating || targetDeck.IsAnimating)
            {
                yield return null;
            }

            yield return new WaitForSeconds(targetDeck.RestackDuration);

            _transferRoutine = null;

            // Retry is Begin, which tears the run down and redeals from scratch.
            GameManager.Instance.Completion.Show($"All {totalCards} cards have landed.", Begin);
        }

        private void TransferTopCard()
        {
            CardView card = sourceDeck.DetachTop();
            if (card == null)
            {
                Debug.LogError($"[{name}] Source deck had no visible card to detach.", this);
                return;
            }

            // Only now commit: counters and any concurrent launch read this immediately.
            int cardId = _sourceModel.TakeTop();
            sourceDeck.SyncWithModel(_sourceModel);

            // Reserve the landing slot now, so the target restacks during the flight.
            targetDeck.NotifyIncoming();

            card.CachedTransform.SetParent(flightRoot != null ? flightRoot : transform, worldPositionStays: true);

            // Later launches render above earlier ones, and both above either deck.
            _launchCount++;
            card.SetSortingOrder(flightSortingOrder + _launchCount);

            _inFlight++;
            Vector3 destination = targetDeck.GetSlotWorldPosition(0);

            flightAnimator.Fly(card, destination).OnComplete(() =>
            {
                _targetModel.PutOnTop(cardId);
                targetDeck.ReceiveCard(card);
                _inFlight--;
            });
        }

        // The asset exists but may hold no sprites, which an exception would not explain. A
        // missing reference is a wiring mistake and throws on its own, where the stack names it.
        private bool HasFaceSprites()
        {
            if (!deckAsset.IsValid)
            {
                Debug.LogError($"[{name}] Card deck asset has no face sprites. Run its " +
                               "'Load Sprites From Folder' context menu.", this);
                return false;
            }

            return true;
        }

        private void Start()
        {
            if (autoStart)
            {
                Begin();
            }
        }
    }
}
