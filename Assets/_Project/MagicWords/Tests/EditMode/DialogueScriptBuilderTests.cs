using System.Linq;
using NUnit.Framework;

namespace SoftGames.MagicWords.Tests
{
    public class DialogueScriptBuilderTests
    {
        [Test]
        public void DuplicateAvatarKeepsTheFirstEntry()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[] { Line("Sheldon", "Hi") },
                avatars = new[]
                {
                    Avatar("Sheldon", "good.png", "left"),
                    Avatar("Sheldon", "broken.png", "right")
                }
            };

            DialogueCharacter sheldon = Build(payload).Cast[0];

            Assert.AreEqual("good.png", sheldon.AvatarUrl);
            Assert.AreEqual(StageSide.Left, sheldon.Side);
        }

        [Test]
        public void SpeakerWithoutAnAvatarLandsOnTheLighterSide()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[]
                {
                    Line("Sheldon", "Hi"),
                    Line("Leonard", "Hi"),
                    Line("Penny", "Hi"),
                    Line("Neighbour", "Hi")
                },
                avatars = new[]
                {
                    Avatar("Sheldon", "s.png", "left"),
                    Avatar("Penny", "p.png", "right"),
                    Avatar("Leonard", "l.png", "right")
                }
            };

            DialogueCharacter neighbour = Build(payload).Cast[3];

            Assert.AreEqual(StageSide.Left, neighbour.Side);
            Assert.IsNull(neighbour.AvatarUrl);
        }

        [Test]
        public void PositionlessCastAlternatesSides()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[] { Line("A", "1"), Line("B", "2"), Line("C", "3") },
                avatars = null
            };

            DialogueScript script = Build(payload);

            Assert.AreEqual(StageSide.Left, script.Cast[0].Side);
            Assert.AreEqual(StageSide.Right, script.Cast[1].Side);
            Assert.AreEqual(StageSide.Left, script.Cast[2].Side);
        }

        [Test]
        public void LineWithoutTextIsDropped()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[] { Line("Sheldon", "Hi"), Line("Penny", " "), Line("Penny", "Ok") }
            };

            Assert.AreEqual(2, Build(payload).Lines.Count);
        }

        [Test]
        public void LineWithoutASpeakerReadsAsNarration()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[] { Line(null, "The room falls silent.") }
            };

            DialogueScript script = Build(payload);

            Assert.IsNull(script.Lines[0].Speaker);
            CollectionAssert.IsEmpty(script.Cast);
        }

        [Test]
        public void AvatarThatNeverSpeaksIsLeftOffTheStage()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[] { Line("Sheldon", "Hi") },
                avatars = new[] { Avatar("Sheldon", "s.png", "left"), Avatar("Nobody", "n.png", "right") }
            };

            DialogueScript script = Build(payload);

            Assert.AreEqual(1, script.Cast.Count);
            Assert.AreEqual("Sheldon", script.Cast[0].Name);
        }

        // Mirrors the shape of the live payload: a duplicated speaker whose second url hangs, and an
        // avatar belonging to nobody. Neither url may reach the network.
        [Test]
        public void PortraitsAreRequestedOnlyForSpeakersWithAUsableUrl()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[]
                {
                    Line("Sheldon", "1"), Line("Leonard", "2"), Line("Penny", "3"), Line("Neighbour", "4")
                },
                avatars = new[]
                {
                    Avatar("Sheldon", "sheldon.png", "left"),
                    Avatar("Penny", "penny.png", "right"),
                    Avatar("Leonard", "leonard.png", "right"),
                    Avatar("Nobody", "nobody.png", "right"),
                    Avatar("Sheldon", "hangs-forever.png", "right")
                }
            };

            string[] requested = Build(payload).PortraitRequests.Select(character => character.AvatarUrl).ToArray();

            CollectionAssert.AreEquivalent(new[] { "sheldon.png", "leonard.png", "penny.png" }, requested);
        }

        [Test]
        public void SpeakerIsSharedAcrossTheirLines()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[] { Line("Sheldon", "Hi"), Line("Sheldon", "Again") }
            };

            DialogueScript script = Build(payload);

            Assert.AreEqual(1, script.Cast.Count);
            Assert.AreSame(script.Lines[0].Speaker, script.Lines[1].Speaker);
        }

        [Test]
        public void LineTextIsComposedAtBuildTime()
        {
            MagicWordsResponseDto payload = new()
            {
                dialogue = new[] { Line("Sheldon", "Fine {laughing}") }
            };

            Assert.AreEqual("<noparse>Fine </noparse>\U0001F602", Build(payload).Lines[0].Text);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MissingDialogueYieldsAnEmptyScript(bool nullResponse)
        {
            MagicWordsResponseDto payload = nullResponse ? null : new MagicWordsResponseDto();

            Assert.IsTrue(Build(payload).IsEmpty);
        }

        private static DialogueScript Build(MagicWordsResponseDto payload)
        {
            return new DialogueScriptBuilder().Build(payload, new EmojiTextComposer(new StubResolver()));
        }

        private static DialogueLineDto Line(string name, string text) => new() { name = name, text = text };

        private static AvatarDto Avatar(string name, string url, string position) =>
            new() { name = name, url = url, position = position };

        private sealed class StubResolver : IEmojiResolver
        {
            public bool TryResolve(string token, out string emoji)
            {
                emoji = token == "laughing" ? "\U0001F602" : null;
                return emoji != null;
            }
        }
    }
}
