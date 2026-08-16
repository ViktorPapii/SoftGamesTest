using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SoftGames.PhoenixFlame
{
    /// <summary>
    /// The fire: five particle layers stacked from the ground glow up to the smoke, plus the 2D
    /// light they throw onto the scene. Colour is the only thing this drives; shape, timing and the
    /// alpha envelopes live in the prefab and are edited there.
    /// </summary>
    public class FlameView : MonoBehaviour
    {
        [Header("Particle layers")]
        [SerializeField] private ParticleSystem glow;
        [SerializeField] private ParticleSystem body;
        [SerializeField] private ParticleSystem core;
        [SerializeField] private ParticleSystem embers;
        [SerializeField] private ParticleSystem smoke;

        [Header("Light")]
        [SerializeField] private Light2D flameLight;

        [Min(0f)]
        [SerializeField] private float lightIntensity = 1.1f;

        [Tooltip("How far the flicker swings either side of the intensity.")]
        [Range(0f, 1f)]
        [SerializeField] private float flickerAmount = 0.22f;

        [Min(0f)]
        [SerializeField] private float flickerSpeed = 4.5f;

        private float _flickerOffset;

        // Repaints every layer. Safe to call each frame: the modules write through, nothing allocates.
        public void ApplyPalette(in FlamePalette palette)
        {
            SetStartColor(glow, palette.glow);
            SetStartColor(core, palette.core);
            SetStartColor(embers, palette.ember);
            SetStartColor(smoke, palette.smoke);

            // Two colours per particle, so the flame is not one flat shade: some rise as body
            // colour, some carry a little of the core's heat with them.
            SetStartColor(body, new ParticleSystem.MinMaxGradient(palette.body, Color.Lerp(palette.body, palette.core, 0.45f)));

            flameLight.color = FullStrength(palette.glow);
        }

        private static void SetStartColor(ParticleSystem system, ParticleSystem.MinMaxGradient color)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
        }

        // The glow colour is dimmed by its alpha for the particles; the light wants the hue at full
        // strength and takes its brightness from lightIntensity instead.
        private static Color FullStrength(Color color)
        {
            float peak = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            return peak <= Mathf.Epsilon ? Color.white : new Color(color.r / peak, color.g / peak, color.b / peak);
        }

        private void Awake()
        {
            _flickerOffset = Random.value * 100f;
        }

        private void LateUpdate()
        {
            float noise = Mathf.PerlinNoise(_flickerOffset + Time.time * flickerSpeed, 0f) * 2f - 1f;
            flameLight.intensity = lightIntensity * (1f + flickerAmount * noise);
        }
    }
}
