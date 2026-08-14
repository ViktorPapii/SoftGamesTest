using System;

namespace SoftGames.MagicWords
{
    // Field names mirror the JSON payload; JsonUtility keys on them and cannot be renamed.
    [Serializable]
    public class MagicWordsResponseDto
    {
        public DialogueLineDto[] dialogue;
        public AvatarDto[] avatars;
    }

    [Serializable]
    public class DialogueLineDto
    {
        public string name;
        public string text;
    }

    [Serializable]
    public class AvatarDto
    {
        public string name;
        public string url;
        public string position;
    }
}
