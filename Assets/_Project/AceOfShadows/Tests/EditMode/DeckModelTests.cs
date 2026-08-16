using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SoftGames.AceOfShadows.Tests
{
    // The transfer rules, with no scene and no engine. DeckModel is what decides which card moves
    // and when a deck is empty; the views only draw the answer.
    public class DeckModelTests
    {
        [Test]
        public void NewDeck_IsDealtDistinctIds()
        {
            DeckModel deck = new(144);

            HashSet<int> seen = new();
            while (!deck.IsEmpty)
            {
                Assert.IsTrue(seen.Add(deck.TakeTop()), "The same card id was dealt twice.");
            }

            Assert.AreEqual(144, seen.Count);
        }

        [Test]
        public void EmptyDeck_IsTheDefault()
        {
            DeckModel deck = new();

            Assert.AreEqual(0, deck.Count);
            Assert.IsTrue(deck.IsEmpty);
        }

        [Test]
        public void NegativeCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeckModel(-1));
        }

        [Test]
        public void TakeTop_ReturnsTheLastCardPutOn()
        {
            DeckModel deck = new();
            deck.PutOnTop(7);
            deck.PutOnTop(9);

            Assert.AreEqual(9, deck.TakeTop(), "A deck is a stack; the last card on is the first off.");
            Assert.AreEqual(7, deck.TakeTop());
        }

        [Test]
        public void TakeTop_OnEmpty_Throws()
        {
            DeckModel deck = new();

            Assert.Throws<InvalidOperationException>(() => deck.TakeTop());
        }

        [Test]
        public void PeekFromTop_CountsDownFromTheTop()
        {
            DeckModel deck = new();
            deck.PutOnTop(1);
            deck.PutOnTop(2);
            deck.PutOnTop(3);

            Assert.AreEqual(3, deck.PeekFromTop(0));
            Assert.AreEqual(2, deck.PeekFromTop(1));
            Assert.AreEqual(1, deck.PeekFromTop(2));
        }

        [Test]
        public void PeekFromTop_DoesNotRemove()
        {
            DeckModel deck = new(3);

            deck.PeekFromTop(0);

            Assert.AreEqual(3, deck.Count, "Peeking took the card off the deck.");
        }

        [Test]
        public void PeekFromTop_PastTheBottom_Throws()
        {
            DeckModel deck = new(2);

            Assert.Throws<ArgumentOutOfRangeException>(() => deck.PeekFromTop(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => deck.PeekFromTop(-1));
        }

        // DeckView draws visibleDepth cards, so it peeks below the top on every relayout. An
        // off-by-one here empties the deck a card early.
        [Test]
        public void PeekFromTop_AtTheBottomCard_IsInRange()
        {
            DeckModel deck = new(2);

            Assert.DoesNotThrow(() => deck.PeekFromTop(1));
        }

        [Test]
        public void CountChanged_CarriesTheNewCount()
        {
            DeckModel deck = new(2);
            List<int> counts = new();
            deck.CountChanged += counts.Add;

            deck.TakeTop();
            deck.PutOnTop(99);

            CollectionAssert.AreEqual(new[] { 1, 2 }, counts);
        }

        [Test]
        public void CountChanged_IsSilentUntilSomethingMoves()
        {
            bool raised = false;
            DeckModel deck = new(5);
            deck.CountChanged += _ => raised = true;

            _ = deck.Count;
            deck.PeekFromTop(0);

            Assert.IsFalse(raised, "Reading the deck reported a change.");
        }

        // The counters commit at launch, not at landing, so a card in the air is in neither deck.
        // That is the whole reason several cards can be flying at once.
        [Test]
        public void CardInFlight_BelongsToNeitherDeck()
        {
            DeckModel source = new(10);
            DeckModel target = new();

            int card = source.TakeTop();

            Assert.AreEqual(9, source.Count);
            Assert.AreEqual(0, target.Count, "The target counted a card that has not landed.");

            target.PutOnTop(card);

            Assert.AreEqual(10, source.Count + target.Count);
        }

        [Test]
        public void DealingEveryCard_MovesThemInTopDownOrder()
        {
            DeckModel source = new(4);
            DeckModel target = new();

            while (!source.IsEmpty)
            {
                target.PutOnTop(source.TakeTop());
            }

            Assert.IsTrue(source.IsEmpty);
            Assert.AreEqual(4, target.Count);

            // Source dealt 3,2,1,0 off the top, so the target's top is the card that started at the
            // source's bottom.
            Assert.AreEqual(0, target.PeekFromTop(0));
            Assert.AreEqual(3, target.PeekFromTop(3));
        }
    }
}
