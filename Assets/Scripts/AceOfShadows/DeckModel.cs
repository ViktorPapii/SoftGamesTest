using System;
using System.Collections.Generic;

namespace SoftGames.AceOfShadows
{
    /// <summary>
    /// Ordered stack of card ids for one deck. Plain C# with no UnityEngine dependency, so the
    /// transfer rules are testable without a scene. Counts change the moment a transfer starts, so
    /// a card in flight belongs to neither deck.
    /// </summary>
    public class DeckModel
    {
        private readonly List<int> _cards;

        // Carries the new count.
        public event Action<int> CountChanged;

        public DeckModel(int initialCount = 0)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCount));
            }

            _cards = new List<int>(initialCount);
            for (int i = 0; i < initialCount; i++)
            {
                _cards.Add(i);
            }
        }

        public int Count => _cards.Count;

        public bool IsEmpty => _cards.Count == 0;

        // Card id at depthFromTop, where 0 is the top card.
        public int PeekFromTop(int depthFromTop)
        {
            if (depthFromTop < 0 || depthFromTop >= _cards.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(depthFromTop));
            }

            return _cards[_cards.Count - 1 - depthFromTop];
        }

        public int TakeTop()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Cannot take a card from an empty deck.");
            }

            int cardId = _cards[^1];
            _cards.RemoveAt(_cards.Count - 1);
            CountChanged?.Invoke(_cards.Count);
            return cardId;
        }

        public void PutOnTop(int cardId)
        {
            _cards.Add(cardId);
            CountChanged?.Invoke(_cards.Count);
        }
    }
}
