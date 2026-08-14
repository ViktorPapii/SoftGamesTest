using System.Collections.Generic;
using NUnit.Framework;

namespace SoftGames.MagicWords.Tests
{
    public class EmojiTextComposerTests
    {
        private const string Laughing = "\U0001F602";

        [Test]
        public void ResolvesKnownToken()
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual($"<noparse>Ha </noparse>{Laughing}", composer.Compose("Ha {laughing}"));
        }

        // Dropped, not replaced: a stand-in glyph would assert a mood the payload never sent.
        [Test]
        public void UnknownTokenIsDropped()
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual("<noparse>Ha </noparse>", composer.Compose("Ha {shrugging}"));
        }

        [Test]
        public void DroppedTokenLeavesTheSurroundingRunUnbroken()
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual("<noparse>Ha  ha</noparse>", composer.Compose("Ha {shrugging} ha"));
        }

        [Test]
        public void UnknownTokensAreRecordedOncePerToken()
        {
            EmojiTextComposer composer = new(Resolver());

            composer.Compose("{shrugging} and {shrugging}");
            composer.Compose("{sobbing}");

            CollectionAssert.AreEqual(new[] { "shrugging", "sobbing" }, composer.UnknownTokens);
        }

        [Test]
        public void PayloadMarkupIsWrappedInsteadOfParsed()
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual("<noparse>a <b>bold</b> claim</noparse>", composer.Compose("a <b>bold</b> claim"));
        }

        [Test]
        public void ClosingNoParseInPayloadCannotEscapeTheWrapper()
        {
            EmojiTextComposer composer = new(Resolver());

            string composed = composer.Compose("done </NOPARSE><color=red>red");

            Assert.AreEqual("<noparse>done </ noparse><color=red>red</noparse>", composed);
        }

        [Test]
        public void PairedAsterisksBecomeItalics()
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual("<noparse>play </noparse><i><noparse>after</noparse></i><noparse> this</noparse>",
                composer.Compose("play *after* this"));
        }

        [Test]
        public void LoneAsteriskStaysLiteral()
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual("<noparse>2 * 3</noparse>", composer.Compose("2 * 3"));
        }

        // The arithmetic asterisk must not open a run that the later emphasis would close.
        [Test]
        public void SpacedAsteriskDoesNotSwallowLaterEmphasis()
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual("<noparse>2 * 3 and </noparse><i><noparse>word</noparse></i>",
                composer.Compose("2 * 3 and *word*"));
        }

        [TestCase("{}")]
        [TestCase("{unclosed")]
        [TestCase("{not a token}")]
        public void MalformedTokenStaysLiteral(string raw)
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual($"<noparse>{raw}</noparse>", composer.Compose(raw));
            CollectionAssert.IsEmpty(composer.UnknownTokens);
        }

        [TestCase(null)]
        [TestCase("")]
        public void EmptyInputComposesToEmpty(string raw)
        {
            EmojiTextComposer composer = new(Resolver());

            Assert.AreEqual(string.Empty, composer.Compose(raw));
        }

        private static IEmojiResolver Resolver() => new FakeResolver();

        private sealed class FakeResolver : IEmojiResolver
        {
            private readonly Dictionary<string, string> _map = new() { { "laughing", Laughing } };

            public bool TryResolve(string token, out string emoji) => _map.TryGetValue(token, out emoji);
        }
    }
}
