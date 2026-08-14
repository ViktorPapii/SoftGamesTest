using System;
using UnityEngine;

namespace SoftGames.MagicWords
{
    /// <summary>
    /// Swaps a set of RectTransforms between a landscape and a portrait arrangement. A canvas
    /// scaler only scales one layout; the stage needs the portraits to move above the box when the
    /// window is taller than it is wide.
    /// </summary>
    public class ResponsiveLayout : MonoBehaviour
    {
        [Serializable]
        private struct RectPreset
        {
            public RectTransform target;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
            public Vector3 scale;
        }

        [Serializable]
        private class Profile
        {
            public RectPreset[] rects;
        }

        [SerializeField] private Profile landscape = new();
        [SerializeField] private Profile portrait = new();

        [Tooltip("Width divided by height. Below this the portrait profile applies.")]
        [Min(0.1f)]
        [SerializeField] private float portraitAspectThreshold = 1f;

        private bool _isPortrait;
        private bool _applied;

        private void Update()
        {
            bool wantsPortrait = (float)Screen.width / Screen.height < portraitAspectThreshold;
            if (_applied && wantsPortrait == _isPortrait)
            {
                return;
            }

            _isPortrait = wantsPortrait;
            _applied = true;
            Apply(_isPortrait ? portrait : landscape);
        }

        private static void Apply(Profile profile)
        {
            if (profile?.rects == null)
            {
                return;
            }

            foreach (RectPreset preset in profile.rects)
            {
                if (preset.target == null)
                {
                    continue;
                }

                preset.target.anchorMin = preset.anchorMin;
                preset.target.anchorMax = preset.anchorMax;
                preset.target.pivot = preset.pivot;
                preset.target.offsetMin = preset.offsetMin;
                preset.target.offsetMax = preset.offsetMax;
                preset.target.localScale = preset.scale == Vector3.zero ? Vector3.one : preset.scale;
            }
        }

        // Arrange the scene, then capture it into the profile whose targets are already listed.
        [ContextMenu("Capture current as landscape")]
        private void CaptureLandscape() => Capture(landscape, "Capture landscape layout");

        [ContextMenu("Capture current as portrait")]
        private void CapturePortrait() => Capture(portrait, "Capture portrait layout");

        private void Capture(Profile profile, string undoLabel)
        {
#if UNITY_EDITOR
            // A context menu edit is not tracked on its own; without this the capture is lost on the
            // next domain reload.
            UnityEditor.Undo.RecordObject(this, undoLabel);
#endif
            Write(profile);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private static void Write(Profile profile)
        {
            if (profile?.rects == null)
            {
                return;
            }

            for (int i = 0; i < profile.rects.Length; i++)
            {
                RectTransform target = profile.rects[i].target;
                if (target == null)
                {
                    continue;
                }

                profile.rects[i] = new RectPreset
                {
                    target = target,
                    anchorMin = target.anchorMin,
                    anchorMax = target.anchorMax,
                    pivot = target.pivot,
                    offsetMin = target.offsetMin,
                    offsetMax = target.offsetMax,
                    scale = target.localScale
                };
            }
        }
    }
}
