using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.MagicWords
{
    /// <summary>
    /// Backlog of the lines played so far, so a single-message box loses nothing. One row per line
    /// and a script is tens of lines long, so rows are built as they arrive and never recycled.
    /// </summary>
    public class DialogueHistoryView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;
        [SerializeField] private DialogueHistoryRow rowPrefab;
        [SerializeField] private GameObject emptyHint;

        private readonly List<DialogueHistoryRow> _rows = new();

        public bool IsOpen => root.activeSelf;

        public void Append(DialogueLine line, Sprite avatar)
        {
            DialogueHistoryRow row = Instantiate(rowPrefab, content);
            row.Bind(line, avatar);
            _rows.Add(row);

            if (emptyHint != null)
            {
                emptyHint.SetActive(false);
            }

            if (IsOpen)
            {
                StartCoroutine(ScrollToBottom());
            }
        }

        // A portrait that lands after its line was logged still reaches the rows already built.
        public void SetPortrait(string speakerName, Sprite avatar)
        {
            foreach (DialogueHistoryRow row in _rows)
            {
                if (string.Equals(row.SpeakerName, speakerName, StringComparison.OrdinalIgnoreCase))
                {
                    row.SetPortrait(avatar);
                }
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                root.SetActive(false);
                return;
            }

            root.SetActive(true);
            StartCoroutine(ScrollToBottom());
        }

        public void Close()
        {
            root.SetActive(false);
        }

        public void Clear()
        {
            _rows.Clear();

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }

            if (emptyHint != null)
            {
                emptyHint.SetActive(true);
            }
        }

        // The new row has no layout yet on the frame it is created.
        private IEnumerator ScrollToBottom()
        {
            yield return null;

            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }
}
