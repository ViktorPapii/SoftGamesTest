using UnityEngine;

namespace SoftGames.Core
{
    // Frame rate readout drawn with IMGUI, so it costs no canvas rebuild.
    public class FpsCounter : MonoBehaviour
    {
        [Tooltip("Screen pixels past the anchor's right edge. Y of 0 centres on it.")]
        [SerializeField] private Vector2 offset = new(16f, 0f);

        [Tooltip("Screen pixels from the top left, used when there is no anchor.")]
        [SerializeField] private Vector2 corner = new(24f, 14f);

        [Tooltip("Share of screen height, so it reads the same size on any display.")]
        [Range(0.01f, 0.06f)]
        [SerializeField] private float relativeFontSize = 0.026f;

        [Tooltip("The kit's .ttf, not its TMP asset. Empty uses Unity's built-in font.")]
        [SerializeField] private Font font;

        [Tooltip("Seconds for the reading to catch up. 0 shows the raw frame rate.")]
        [Min(0f)]
        [SerializeField] private float smoothing = 0.25f;

        private static readonly Color Good = new(0.55f, 0.9f, 0.55f);
        private static readonly Color Warning = new(0.96f, 0.79f, 0.35f);
        private static readonly Color Bad = new(0.94f, 0.42f, 0.38f);

        private readonly Vector3[] _corners = new Vector3[4];

        private GUIStyle _style;
        private string _text = "-- FPS";
        private float _fps;
        private int _shown = -1;

        // The rect to sit beside, or null for the corner. GameHud owns this.
        public RectTransform Anchor { get; set; }


        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            if (delta <= 0f)
            {
                return;
            }

            float instant = 1f / delta;

            // Weighted by elapsed time, so the reading means the same at 30 and at 240 fps.
            _fps = _fps > 0f && smoothing > 0f
                ? _fps + (instant - _fps) * (1f - Mathf.Exp(-delta / smoothing))
                : instant;

            int rounded = Mathf.RoundToInt(_fps);
            if (rounded != _shown)
            {
                _shown = rounded;
                _text = rounded + " FPS";
            }
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            int fontSize = Mathf.RoundToInt(Screen.height * relativeFontSize);

            _style ??= new GUIStyle(GUI.skin.label) { font = font };
            _style.fontSize = fontSize;
            _style.normal.textColor = _fps >= 50f ? Good : _fps >= 30f ? Warning : Bad;

            Vector2 origin = corner;

            if (Anchor != null)
            {
                // An overlay canvas reports world corners already in screen pixels, bottom-up,
                // while IMGUI counts down from the top.
                Anchor.GetWorldCorners(_corners);
                origin.x = _corners[2].x + offset.x;
                origin.y = Screen.height - (_corners[0].y + _corners[1].y) * 0.5f - fontSize * 0.6f + offset.y;
            }

            GUI.Label(new Rect(origin.x, origin.y, 300f, fontSize * 2f), _text, _style);
        }
    }
}
