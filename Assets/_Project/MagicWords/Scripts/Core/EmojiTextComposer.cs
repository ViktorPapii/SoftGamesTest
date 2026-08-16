using System;
using System.Collections.Generic;
using System.Text;

namespace SoftGames.MagicWords
{
    /// <summary>
    /// Turns one payload line into TMP rich text: {token} becomes the catalog's Unicode emoji,
    /// *emphasis* becomes italics, and every literal run is wrapped in noparse so markup arriving
    /// from the endpoint renders as text instead of being executed as tags.
    /// </summary>
    public sealed class EmojiTextComposer
    {
        private const string NoParseOpen = "<noparse>";
        private const string NoParseClose = "</noparse>";

        private readonly IEmojiResolver _resolver;
        private readonly List<string> _unknownTokens = new();
        private readonly StringBuilder _output = new();
        private readonly StringBuilder _literal = new();

        public EmojiTextComposer(IEmojiResolver resolver)
        {
            _resolver = resolver;
        }

        // Distinct tokens the catalog could not resolve, in first-seen order.
        public IReadOnlyList<string> UnknownTokens => _unknownTokens;

        public string Compose(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return string.Empty;
            }

            _output.Clear();
            _literal.Clear();
            bool inEmphasis = false;

            for (int i = 0; i < rawText.Length; i++)
            {
                char c = rawText[i];

                if (c == '{' && TryReadToken(rawText, i, out string token, out int afterToken))
                {
                    AppendToken(token);
                    i = afterToken - 1;
                    continue;
                }

                if (c == '*' && (inEmphasis || HasClosingAsterisk(rawText, i)))
                {
                    FlushLiteral();
                    _output.Append(inEmphasis ? "</i>" : "<i>");
                    inEmphasis = !inEmphasis;
                    continue;
                }

                _literal.Append(c);
            }

            FlushLiteral();

            if (inEmphasis)
            {
                _output.Append("</i>");
            }

            return _output.ToString();
        }

        private static bool TryReadToken(string text, int braceIndex, out string token, out int afterToken)
        {
            token = null;
            afterToken = braceIndex;

            int close = text.IndexOf('}', braceIndex + 1);
            if (close <= braceIndex + 1)
            {
                return false;
            }

            for (int i = braceIndex + 1; i < close; i++)
            {
                char c = text[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return false;
                }
            }

            token = text.Substring(braceIndex + 1, close - braceIndex - 1);
            afterToken = close + 1;
            return true;
        }

        // Markdown's rule: the run has to start and end against a non-space, so "2 * 3" is arithmetic.
        private static bool HasClosingAsterisk(string text, int openIndex)
        {
            if (openIndex + 1 >= text.Length || char.IsWhiteSpace(text[openIndex + 1]))
            {
                return false;
            }

            for (int i = openIndex + 2; i < text.Length; i++)
            {
                if (text[i] == '*' && !char.IsWhiteSpace(text[i - 1]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A resolved token interrupts the literal run; an unknown one is dropped and the run
        /// carries on unbroken. Substituting a stand-in glyph would put a mood in the line that the
        /// payload never sent, so the token is recorded for the load warning and nothing is drawn.
        /// </summary>
        private void AppendToken(string token)
        {
            if (_resolver != null && _resolver.TryResolve(token, out string emoji))
            {
                FlushLiteral();
                _output.Append(emoji);
                return;
            }

            if (!_unknownTokens.Contains(token))
            {
                _unknownTokens.Add(token);
            }
        }

        private void FlushLiteral()
        {
            if (_literal.Length == 0)
            {
                return;
            }

            string run = _literal.ToString();
            _literal.Clear();

            // A closing tag inside the payload would end the noparse block early and hand the rest
            // of the line back to the tag parser. TMP matches tags case-insensitively.
            int closeIndex = run.IndexOf(NoParseClose, StringComparison.OrdinalIgnoreCase);
            while (closeIndex >= 0)
            {
                run = run.Remove(closeIndex, NoParseClose.Length).Insert(closeIndex, "</ noparse>");
                closeIndex = run.IndexOf(NoParseClose, closeIndex + 1, StringComparison.OrdinalIgnoreCase);
            }

            _output.Append(NoParseOpen).Append(run).Append(NoParseClose);
        }
    }
}
