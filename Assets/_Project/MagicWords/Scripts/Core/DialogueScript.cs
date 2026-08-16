using System.Collections.Generic;

namespace SoftGames.MagicWords
{
    // A playable dialogue: the lines in order, and every character that speaks at least one of them.
    public sealed class DialogueScript
    {
        public static readonly DialogueScript Empty =
            new(new List<DialogueLine>(), new List<DialogueCharacter>());

        public DialogueScript(IReadOnlyList<DialogueLine> lines, IReadOnlyList<DialogueCharacter> cast)
        {
            Lines = lines;
            Cast = cast;

            List<DialogueCharacter> requests = new();
            foreach (DialogueCharacter character in cast)
            {
                if (!string.IsNullOrEmpty(character.AvatarUrl))
                {
                    requests.Add(character);
                }
            }

            PortraitRequests = requests;
        }

        public IReadOnlyList<DialogueLine> Lines { get; }

        public IReadOnlyList<DialogueCharacter> Cast { get; }

        /// <summary>
        /// The only portraits worth fetching. Built from the cast, so an avatar belonging to nobody
        /// in the dialogue is never requested, and a duplicate entry's url never reaches the network.
        /// </summary>
        public IReadOnlyList<DialogueCharacter> PortraitRequests { get; }

        public bool IsEmpty => Lines.Count == 0;
    }
}
