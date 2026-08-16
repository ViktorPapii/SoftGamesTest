using System;
using System.Collections.Generic;

namespace SoftGames.MagicWords
{
    /// <summary>
    /// Validates one payload into a playable script. Duplicate avatar entries keep the first, so a
    /// later broken url cannot replace a working one; speakers the avatars list never mentions are
    /// placed on whichever side has fewer characters, so the stage stays balanced.
    /// </summary>
    public sealed class DialogueScriptBuilder
    {
        private readonly List<string> _warnings = new();

        // Every recoverable defect found in the payload, for a single batched log at load.
        public IReadOnlyList<string> Warnings => _warnings;

        public DialogueScript Build(MagicWordsResponseDto response, EmojiTextComposer composer)
        {
            _warnings.Clear();

            if (response?.dialogue == null || response.dialogue.Length == 0)
            {
                _warnings.Add("Payload carries no dialogue lines.");
                return DialogueScript.Empty;
            }

            Dictionary<string, AvatarInfo> avatars = ReadAvatars(response.avatars);
            List<DialogueLineDto> validLines = ReadLines(response.dialogue);
            List<string> speakers = CollectSpeakers(validLines);
            Dictionary<string, DialogueCharacter> cast = BuildCast(speakers, avatars);

            List<DialogueLine> lines = new(validLines.Count);
            foreach (DialogueLineDto dto in validLines)
            {
                DialogueCharacter speaker = null;
                if (!string.IsNullOrWhiteSpace(dto.name))
                {
                    cast.TryGetValue(dto.name.Trim(), out speaker);
                }

                lines.Add(new DialogueLine(speaker, composer.Compose(dto.text)));
            }

            List<DialogueCharacter> orderedCast = new(speakers.Count);
            foreach (string speaker in speakers)
            {
                orderedCast.Add(cast[speaker]);
            }

            WarnAboutSilentAvatars(avatars, cast);

            return new DialogueScript(lines, orderedCast);
        }

        private Dictionary<string, AvatarInfo> ReadAvatars(AvatarDto[] dtos)
        {
            Dictionary<string, AvatarInfo> avatars = new(StringComparer.OrdinalIgnoreCase);
            if (dtos == null)
            {
                return avatars;
            }

            foreach (AvatarDto dto in dtos)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.name))
                {
                    _warnings.Add("Avatar entry without a name was dropped.");
                    continue;
                }

                // First entry wins, so a later duplicate cannot replace a working url or side.
                string name = dto.name.Trim();
                if (avatars.ContainsKey(name))
                {
                    continue;
                }

                string url = string.IsNullOrWhiteSpace(dto.url) ? null : dto.url.Trim();
                if (url == null)
                {
                    _warnings.Add($"Avatar '{name}' has no url; it will use a placeholder portrait.");
                }

                avatars.Add(name, new AvatarInfo(url, ReadSide(name, dto.position)));
            }

            return avatars;
        }

        private StageSide? ReadSide(string name, string position)
        {
            if (string.IsNullOrWhiteSpace(position))
            {
                _warnings.Add($"Avatar '{name}' has no position; its side will be balanced.");
                return null;
            }

            switch (position.Trim().ToLowerInvariant())
            {
                case "left":
                    return StageSide.Left;
                case "right":
                    return StageSide.Right;
                default:
                    _warnings.Add($"Avatar '{name}' has unrecognised position '{position}'; its side will be balanced.");
                    return null;
            }
        }

        private List<DialogueLineDto> ReadLines(DialogueLineDto[] dtos)
        {
            List<DialogueLineDto> valid = new(dtos.Length);

            for (int i = 0; i < dtos.Length; i++)
            {
                DialogueLineDto dto = dtos[i];
                if (dto == null || string.IsNullOrWhiteSpace(dto.text))
                {
                    _warnings.Add($"Dialogue line {i} has no text and was dropped.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dto.name))
                {
                    _warnings.Add($"Dialogue line {i} has no speaker; it will read as narration.");
                }

                valid.Add(dto);
            }

            return valid;
        }

        private static List<string> CollectSpeakers(List<DialogueLineDto> lines)
        {
            List<string> speakers = new();
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach (DialogueLineDto line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.name))
                {
                    continue;
                }

                string name = line.name.Trim();
                if (seen.Add(name))
                {
                    speakers.Add(name);
                }
            }

            return speakers;
        }

        private static Dictionary<string, DialogueCharacter> BuildCast(
            List<string> speakers, Dictionary<string, AvatarInfo> avatars)
        {
            Dictionary<string, StageSide> sides = new(StringComparer.OrdinalIgnoreCase);
            int left = 0;
            int right = 0;

            foreach (string speaker in speakers)
            {
                if (!avatars.TryGetValue(speaker, out AvatarInfo info) || !info.Side.HasValue)
                {
                    continue;
                }

                sides[speaker] = info.Side.Value;
                Count(info.Side.Value, ref left, ref right);
            }

            foreach (string speaker in speakers)
            {
                if (sides.ContainsKey(speaker))
                {
                    continue;
                }

                // Ties go left, which also makes an entirely position-less payload alternate: the
                // counts stop being equal the moment the first one lands.
                StageSide side = left <= right ? StageSide.Left : StageSide.Right;
                sides[speaker] = side;
                Count(side, ref left, ref right);
            }

            Dictionary<string, DialogueCharacter> cast = new(StringComparer.OrdinalIgnoreCase);
            foreach (string speaker in speakers)
            {
                avatars.TryGetValue(speaker, out AvatarInfo info);
                cast[speaker] = new DialogueCharacter(speaker, info.Url, sides[speaker]);
            }

            return cast;
        }

        private static void Count(StageSide side, ref int left, ref int right)
        {
            if (side == StageSide.Left)
            {
                left++;
            }
            else
            {
                right++;
            }
        }

        private void WarnAboutSilentAvatars(
            Dictionary<string, AvatarInfo> avatars, Dictionary<string, DialogueCharacter> cast)
        {
            foreach (string name in avatars.Keys)
            {
                if (!cast.ContainsKey(name))
                {
                    _warnings.Add($"Avatar '{name}' never speaks; it was left off the stage.");
                }
            }
        }

        private readonly struct AvatarInfo
        {
            public AvatarInfo(string url, StageSide? side)
            {
                Url = url;
                Side = side;
            }

            public string Url { get; }

            public StageSide? Side { get; }
        }
    }
}
