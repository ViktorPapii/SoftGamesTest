using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SoftGames.AceOfShadows
{
    // Shows a DeckModel's full count in a TextMeshPro label.
    public class DeckCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Tooltip("Scale punch applied when the count changes. Zero disables it.")]
        [SerializeField] private float punchStrength = 0.25f;

        [Min(0f)]
        [SerializeField] private float punchDuration = 0.2f;

        private DeckModel _boundModel;

        public void Bind(DeckModel model)
        {
            Unbind();

            _boundModel = model;
            _boundModel.CountChanged += OnCountChanged;
            SetText(model.Count);
        }

        private void Unbind()
        {
            if (_boundModel != null)
            {
                _boundModel.CountChanged -= OnCountChanged;
                _boundModel = null;
            }
        }

        private void OnCountChanged(int count)
        {
            SetText(count);

            if (punchStrength > 0f && punchDuration > 0f && label != null)
            {
                label.transform.DOKill(complete: true);
                label.transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, vibrato: 1)
                    .SetLink(label.gameObject);
            }
        }

        private void SetText(int count)
        {
            if (label != null)
            {
                label.SetText("{0}", count);
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Reset()
        {
            label = GetComponent<TMP_Text>();
        }
    }
}
