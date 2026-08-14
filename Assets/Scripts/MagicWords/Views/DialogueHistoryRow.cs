using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.MagicWords
{
    public class DialogueHistoryRow : MonoBehaviour
    {
        [SerializeField] private GameObject avatarRoot;
        [SerializeField] private Image portrait;

        [Tooltip("Used when the speaker's portrait never loaded.")]
        [SerializeField] private Sprite placeholderPortrait;

        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text bodyLabel;

        public string SpeakerName { get; private set; }

        public void Bind(DialogueLine line, Sprite avatar)
        {
            bool hasSpeaker = line.Speaker != null;

            avatarRoot.SetActive(hasSpeaker);
            nameLabel.gameObject.SetActive(hasSpeaker);
            bodyLabel.text = line.Text;
            SpeakerName = hasSpeaker ? line.Speaker.Name : null;

            if (!hasSpeaker)
            {
                return;
            }

            nameLabel.text = line.Speaker.Name;
            SetPortrait(avatar);
        }

        public void SetPortrait(Sprite avatar)
        {
            portrait.sprite = avatar != null ? avatar : placeholderPortrait;
        }
    }
}
