using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.AceOfShadows
{
    /// <summary>
    /// Keeps the camera's orthographic size in step with the CanvasScaler and raises an event when
    /// the viewport changes.
    ///
    /// Unity has no resolution-changed event, and the UI layout message fires mid-rebuild while
    /// anchored elements are still a frame behind, so this polls: two int comparisons per frame for
    /// the whole scene. Camera size and the notification live together so they cannot arrive out of
    /// order — anything positioning itself in world space needs the new size already applied.
    /// </summary>
    [ExecuteAlways]
    public class ScreenLayoutWatcher : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private RectTransform canvasRect;

        [Tooltip("Orthographic size that frames the scene correctly at the scaler's reference resolution.")]
        [Min(0.01f)]
        [SerializeField] private float referenceOrthographicSize = 5f;

        public event Action Changed;

        public Camera WorldCamera => worldCamera;

        public RectTransform CanvasRect => canvasRect;

        private int _width;
        private int _height;
        private Vector2 _canvasSize;

        /// <summary>Settles the UI, rescales the camera and notifies listeners.</summary>
        public void Refresh()
        {
            Canvas.ForceUpdateCanvases();
            ApplyWorldScale();
            Changed?.Invoke();
        }

        /// <summary>
        /// Matches the camera's orthographic size to the CanvasScaler's scale factor, so a card and
        /// a UI element of the same on-screen size at the reference resolution stay that way.
        /// </summary>
        public void ApplyWorldScale()
        {
            if (worldCamera == null || scaler == null || !worldCamera.orthographic)
            {
                return;
            }

            Vector2 reference = scaler.referenceResolution;
            if (reference.x <= 0f || reference.y <= 0f)
            {
                return;
            }

            // The weighting CanvasScaler itself uses for Match Width Or Height.
            float logWidth = Mathf.Log(worldCamera.pixelWidth / reference.x, 2f);
            float logHeight = Mathf.Log(worldCamera.pixelHeight / reference.y, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));

            worldCamera.orthographicSize = worldCamera.pixelHeight * referenceOrthographicSize / (reference.y * scale);
        }

        private void OnEnable()
        {
            _width = 0;
            _height = 0;
            _canvasSize = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (worldCamera == null)
            {
                return;
            }

            // The camera's render target, not Screen: in the editor Screen reports whatever surface
            // was last rendered, so it misses a Game view resolution change entirely.
            //
            // The canvas rect is watched as well because it resizes a frame after the camera does.
            // Watching only the camera fires once against a stale rect and then never again, which
            // leaves everything positioned for the previous resolution.
            Vector2 canvasSize = canvasRect != null ? canvasRect.rect.size : Vector2.zero;

            if (worldCamera.pixelWidth == _width &&
                worldCamera.pixelHeight == _height &&
                canvasSize == _canvasSize)
            {
                return;
            }

            _width = worldCamera.pixelWidth;
            _height = worldCamera.pixelHeight;
            _canvasSize = canvasSize;
            Refresh();
        }

        private void Reset()
        {
            worldCamera = Camera.main;
            scaler = GetComponent<CanvasScaler>();
            canvasRect = transform as RectTransform;
        }
    }
}
