using TMPro;
using UnityEngine;

namespace SoftGames.Common
{
    /// <summary>
    /// Frame rate readout. Reads unscaled time, so a paused or slowed scene still reports what the
    /// renderer is actually doing.
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Tooltip("Seconds between readings.")]
        [Min(0.05f)]
        [SerializeField] private float refreshInterval = 0.5f;

        [Header("Thresholds")]
        [SerializeField] private Color goodColor = new(0.55f, 0.9f, 0.55f);
        [SerializeField] private Color warningColor = new(0.96f, 0.79f, 0.35f);
        [SerializeField] private Color badColor = new(0.94f, 0.42f, 0.38f);

        [Min(1f)]
        [SerializeField] private float goodThreshold = 50f;

        [Min(1f)]
        [SerializeField] private float warningThreshold = 30f;

        private FpsSampler _sampler;

        private void Awake()
        {
            _sampler = new FpsSampler(refreshInterval);
        }

        private void Update()
        {
            if (label == null || !_sampler.AddFrame(Time.unscaledDeltaTime))
            {
                return;
            }

            // SetText with an argument formats into TMP's own buffer; string.Format would allocate
            // a new string twice a second, forever.
            label.SetText("{0:0} FPS", _sampler.Fps);
            label.color = ColorFor(_sampler.Fps);
        }

        private Color ColorFor(float fps)
        {
            if (fps >= goodThreshold)
            {
                return goodColor;
            }

            return fps >= warningThreshold ? warningColor : badColor;
        }

        private void Reset()
        {
            label = GetComponent<TMP_Text>();
        }
    }
}
