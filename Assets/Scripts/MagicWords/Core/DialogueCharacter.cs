namespace SoftGames.MagicWords
{
    // One member of the cast. Side is resolved once at build time and never changes during playback.
    public sealed class DialogueCharacter
    {
        public DialogueCharacter(string name, string avatarUrl, StageSide side)
        {
            Name = name;
            AvatarUrl = avatarUrl;
            Side = side;
        }

        public string Name { get; }

        // Null when the payload has no avatar entry for this speaker, or its url was unusable.
        public string AvatarUrl { get; }

        public StageSide Side { get; }
    }
}
