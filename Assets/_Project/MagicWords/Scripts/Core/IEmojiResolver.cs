namespace SoftGames.MagicWords
{
    public interface IEmojiResolver
    {
        // Unicode string for a {token}, e.g. "laughing" -> "\U0001F602".
        bool TryResolve(string token, out string emoji);
    }
}
