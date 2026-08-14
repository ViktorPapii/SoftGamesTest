using TMPro;
using UnityEngine;

namespace SoftGames.PhoenixFlame
{
    /// <summary>
    /// Paints the fire the colour of whichever animator state is current. The button fires the
    /// trigger that walks to the next state, the state's clip keys <see cref="fade"/> from 0 to 1,
    /// and the fire is the mix of the colour it is leaving and the one it is entering.
    ///
    /// The controller graph owns the cycle — which colours, in what order. This owns only the mixing,
    /// and the colours themselves live in a <see cref="FlamePaletteSet"/> that can be edited while
    /// the game runs.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class FlameColorController : MonoBehaviour
    {
        [Tooltip("The Animator on this object. The fade clip keys the field below, which binds by path.")]
        [SerializeField] private Animator animator;

        [SerializeField] private FlameView flame;

        [Tooltip("Each entry's name must match an animator state name.")]
        [SerializeField] private FlamePaletteSet palettes;

        [Tooltip("Optional. Names the colour the fire is heading for.")]
        [SerializeField] private TMP_Text colorLabel;

        [Tooltip("Keyed by the fade clip. A value set here lasts until the animator writes the next frame.")]
        [Range(0f, 1f)]
        [SerializeField] private float fade = 1f;

        // Matches the trigger on every transition in the controller.
        private static readonly int NextTrigger = Animator.StringToHash("Next");

        private int[] _stateHashes;
        private FlamePalette _leaving;
        private int _to;

        // Wired to the colour button's OnClick.
        public void Next()
        {
            animator.SetTrigger(NextTrigger);
        }

        /// <summary>
        /// Starts the fade into the palette named by an animator state. Called by
        /// <see cref="FlameColorState"/> as the state is entered, which is also when its clip
        /// restarts — so the fade always begins from whatever mix is on screen.
        /// </summary>
        internal void BeginFadeTo(int stateHash)
        {
            int index = IndexOf(stateHash);

            if (index < 0)
            {
                Debug.LogError($"[{name}] Animator state has no palette of the same name in {palettes}.", this);
                return;
            }

            _leaving = Mix();
            _to = index;
            ShowName();
        }

        private void Awake()
        {
            fade = 1f;
            _to = 0;

            if (animator == null)
            {
                Debug.LogError($"[{name}] No animator assigned; the colour button will do nothing.", this);
            }

            if (palettes == null || palettes.Count == 0)
            {
                Debug.LogError($"[{name}] No palette set assigned.", this);
                return;
            }

            _stateHashes = new int[palettes.Count];
            for (int i = 0; i < palettes.Count; i++)
            {
                _stateHashes[i] = Animator.StringToHash(palettes.NameAt(i));
            }

            _leaving = palettes.PaletteAt(0);
            Repaint();
            ShowName();
        }

        /// <summary>
        /// Called by the animation system once the clip has written this frame's fade. It keeps
        /// firing after the fade has settled, because the clip holds at 1 — which is what makes an
        /// edit to the palette asset show up live. Six colour writes a frame is cheaper than the
        /// bookkeeping to skip them.
        /// </summary>
        private void OnDidApplyAnimationProperties()
        {
            Repaint();
        }

        private void Repaint()
        {
            if (_stateHashes == null)
            {
                return;
            }

            FlamePalette palette = Mix();

            if (flame != null)
            {
                flame.ApplyPalette(palette);
            }

            if (colorLabel != null)
            {
                colorLabel.color = palette.body;
            }
        }

        private FlamePalette Mix() => FlamePalette.Lerp(_leaving, palettes.PaletteAt(_to), fade);

        private int IndexOf(int stateHash)
        {
            if (_stateHashes == null)
            {
                return -1;
            }

            for (int i = 0; i < _stateHashes.Length; i++)
            {
                if (_stateHashes[i] == stateHash)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ShowName()
        {
            if (colorLabel != null)
            {
                colorLabel.text = palettes.NameAt(_to);
            }
        }

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }
    }
}
