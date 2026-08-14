using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.MagicWords
{
    /// <summary>
    /// Shows exactly one line at a time. The name plate carries the speaker's portrait and moves to
    /// their side, so the box says who is talking without a second element on screen.
    /// </summary>
    public class DialogueBoxView : MonoBehaviour
    {
        [SerializeField] private RectTransform namePlate;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Image speakerPortrait;

        [Tooltip("Shown when the speaker's portrait is missing or failed to load.")]
        [SerializeField] private Sprite placeholderPortrait;

        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private GameObject continueHint;

        [Header("Reveal")]
        [Min(1f)]
        [SerializeField] private float charactersPerSecond = 45f;

        [Tooltip("Extra beat held after sentence punctuation.")]
        [Min(0f)]
        [SerializeField] private float punctuationPause = 0.12f;

        [Tooltip("Distance the name plate sits from the panel edge.")]
        [SerializeField] private float sideOffset = 40f;

        private Coroutine _reveal;
        private float _namePlateY;

        public bool IsRevealing => _reveal != null;

        // TMP's own count, so sprites count once and rich text markup counts not at all.
        public int CharacterCount { get; private set; }

        public void Show(DialogueLine line, Sprite portrait, int index, int total)
        {
            StopReveal();

            bool hasSpeaker = line.Speaker != null;

            nameLabel.text = hasSpeaker ? line.Speaker.Name : string.Empty;
            namePlate.gameObject.SetActive(hasSpeaker);
            SetPortrait(portrait);

            if (hasSpeaker)
            {
                PlaceOnSide(line.Speaker.Side);
            }

            progressLabel.SetText("{0} / {1}", index + 1, total);

            bodyLabel.alignment = hasSpeaker ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Center;
            bodyLabel.text = line.Text;
            bodyLabel.ForceMeshUpdate();
            CharacterCount = bodyLabel.textInfo.characterCount;
            bodyLabel.maxVisibleCharacters = 0;

            SetContinueHint(false);

            // A line that composes to nothing would finish Reveal before StartCoroutine returns,
            // leaving _reveal holding a dead coroutine and IsRevealing stuck true.
            if (CharacterCount == 0)
            {
                CompleteReveal();
                return;
            }

            _reveal = StartCoroutine(Reveal());
        }

        // Also called when a portrait lands after its line is already on screen.
        public void SetPortrait(Sprite portrait)
        {
            speakerPortrait.sprite = portrait != null ? portrait : placeholderPortrait;
        }

        public void CompleteReveal()
        {
            StopReveal();
            bodyLabel.maxVisibleCharacters = int.MaxValue;
            SetContinueHint(true);
        }

        public void Clear()
        {
            StopReveal();
            bodyLabel.text = string.Empty;
            CharacterCount = 0;
            nameLabel.text = string.Empty;
            progressLabel.text = string.Empty;
            SetContinueHint(false);
        }

        private IEnumerator Reveal()
        {
            float secondsPerCharacter = 1f / charactersPerSecond;
            float budget = 0f;
            int shown = 0;

            while (shown < CharacterCount)
            {
                budget += Time.deltaTime;

                while (budget >= secondsPerCharacter && shown < CharacterCount)
                {
                    budget -= secondsPerCharacter;
                    shown++;
                    bodyLabel.maxVisibleCharacters = shown;

                    if (IsSentenceBreak(bodyLabel.textInfo.characterInfo[shown - 1].character))
                    {
                        budget -= punctuationPause;
                    }
                }

                yield return null;
            }

            _reveal = null;
            SetContinueHint(true);
        }

        private static bool IsSentenceBreak(char character)
        {
            return character is '.' or ',' or '!' or '?' or ';' or ':';
        }

        private void PlaceOnSide(StageSide side)
        {
            bool left = side == StageSide.Left;
            Vector2 anchor = new(left ? 0f : 1f, 1f);

            namePlate.anchorMin = anchor;
            namePlate.anchorMax = anchor;
            namePlate.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            namePlate.anchoredPosition = new Vector2(left ? sideOffset : -sideOffset, _namePlateY);
        }

        private void SetContinueHint(bool visible)
        {
            continueHint.SetActive(visible);
        }

        private void StopReveal()
        {
            if (_reveal != null)
            {
                StopCoroutine(_reveal);
                _reveal = null;
            }
        }

        private void Awake()
        {
            _namePlateY = namePlate.anchoredPosition.y;
        }
    }
}
