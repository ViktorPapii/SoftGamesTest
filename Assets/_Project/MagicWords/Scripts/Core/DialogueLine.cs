namespace SoftGames.MagicWords
{
    public sealed class DialogueLine
    {
        public DialogueLine(DialogueCharacter speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }

        // Null for a line whose payload carried no name; the view renders it as narration.
        public DialogueCharacter Speaker { get; }

        // Composed rich text: emoji resolved, literal runs escaped. Views never see the raw payload.
        public string Text { get; }
    }
}
