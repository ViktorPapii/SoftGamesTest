using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SoftGames.PhoenixFlame.EditorTools
{
    /// <summary>
    /// Assembles Flame.prefab: five particle layers and the 2D light, wired into a
    /// <see cref="FlameView"/>. Run it after the textures and materials exist.
    ///
    /// The layers are, back to front: smoke drifting off the top, a wide glow pooling on the
    /// ground, the flame body, a hotter core inside it, and embers lifting away.
    ///
    /// Flame and smoke draw a 3x3 flipbook over each particle's life, which is where the fire gets
    /// its detail; the glow and the embers are plain shapes. Flame_Additive and Smoke_Alpha came
    /// from a third party pack — see Assets/_Project/PhoenixFlame/Art/README.md — the other two are ours.
    /// </summary>
    public static class FlamePrefabBuilder
    {
        private const string PrefabFolder = "Assets/_Project/PhoenixFlame/Prefabs";
        private const string PrefabPath = PrefabFolder + "/Flame.prefab";

        private const string MaterialFolder = "Assets/_Project/PhoenixFlame/Art/Materials";
        private const string FlameMaterial = MaterialFolder + "/Flame_Additive.mat";
        private const string SmokeMaterial = MaterialFolder + "/Smoke_Alpha.mat";
        private const string GlowMaterial = MaterialFolder + "/Glow_Additive.mat";
        private const string EmberMaterial = MaterialFolder + "/Ember_Additive.mat";

        // Both flipbook sheets are 3x3.
        private const int SheetColumns = 3;
        private const int SheetRows = 3;

        [MenuItem("Tools/SoftGames/Phoenix Flame/Rebuild Flame Prefab")]
        public static void Rebuild()
        {
            EditorFolders.Ensure(PrefabFolder);

            FlamePalette palette = FlamePalettes.Cycle[0].Palette;
            GameObject root = new("Flame");

            ParticleSystem smoke = BuildSmoke(root.transform, palette);
            ParticleSystem glow = BuildGlow(root.transform, palette);
            ParticleSystem body = BuildBody(root.transform, palette);
            ParticleSystem core = BuildCore(root.transform, palette);
            ParticleSystem embers = BuildEmbers(root.transform, palette);
            Light2D light = BuildLight(root.transform, palette);

            FlameView view = root.AddComponent<FlameView>();
            SerializedObject serialized = new(view);
            serialized.FindProperty("glow").objectReferenceValue = glow;
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.FindProperty("core").objectReferenceValue = core;
            serialized.FindProperty("embers").objectReferenceValue = embers;
            serialized.FindProperty("smoke").objectReferenceValue = smoke;
            serialized.FindProperty("flameLight").objectReferenceValue = light;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"Phoenix Flame: prefab written to {PrefabPath}.");
        }

        private static ParticleSystem BuildBody(Transform parent, FlamePalette palette)
        {
            ParticleSystem system = CreateLayer(parent, "Body", FlameMaterial, sortingOrder: 10);

            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 3.1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            main.startColor = palette.body;
            main.maxParticles = 200;

            SetEmission(system, 26f);
            SetShape(system, width: 0.85f, height: 0.08f);
            SetRise(system, minSpeed: 2.2f, maxSpeed: 3.2f, drift: 0.2f, buoyancy: 1.5f);
            SetSize(system, Curve((0f, 0.5f), (0.2f, 1f), (0.65f, 0.6f), (1f, 0.05f)));
            SetAlpha(system, (0f, 0f), (0.12f, 0.85f), (0.5f, 0.7f), (1f, 0f));
            SetSheet(system);
            SetNoise(system, strength: 0.3f, frequency: 0.8f, scrollSpeed: 1f);

            // Buoyancy would otherwise keep accelerating the oldest particles into streaks; this is
            // where the plume slows and spreads instead.
            ParticleSystem.LimitVelocityOverLifetimeModule drag = system.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = new ParticleSystem.MinMaxCurve(3f);
            drag.dampen = 0.2f;

            return system;
        }

        // Sits inside the body and burns out sooner, which is what keeps the base white hot.
        private static ParticleSystem BuildCore(Transform parent, FlamePalette palette)
        {
            ParticleSystem system = CreateLayer(parent, "Core", FlameMaterial, sortingOrder: 12);

            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 1.7f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            main.startColor = palette.core;
            main.maxParticles = 140;

            SetEmission(system, 20f);
            SetShape(system, width: 0.5f, height: 0.05f);
            SetRise(system, minSpeed: 1.7f, maxSpeed: 2.5f, drift: 0.1f, buoyancy: 1f);
            SetSize(system, Curve((0f, 0.6f), (0.3f, 1f), (1f, 0.05f)));
            SetAlpha(system, (0f, 0f), (0.1f, 0.75f), (0.6f, 0.55f), (1f, 0f));
            SetSheet(system);
            SetNoise(system, strength: 0.22f, frequency: 1.1f, scrollSpeed: 1.2f);

            return system;
        }

        private static ParticleSystem BuildEmbers(Transform parent, FlamePalette palette)
        {
            ParticleSystem system = CreateLayer(parent, "Embers", EmberMaterial, sortingOrder: 14);

            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
            main.startColor = palette.ember;
            main.maxParticles = 180;

            SetEmission(system, 32f);
            SetShape(system, width: 0.8f, height: 0.06f);
            SetRise(system, minSpeed: 2.6f, maxSpeed: 4f, drift: 0.35f, buoyancy: 0.35f);
            SetSize(system, Curve((0f, 1f), (0.7f, 0.75f), (1f, 0f)));

            // Uneven alpha, so embers wink on the way up instead of fading evenly.
            SetAlpha(system, (0f, 0f), (0.08f, 1f), (0.3f, 0.35f), (0.5f, 1f), (0.75f, 0.3f), (1f, 0f));
            SetNoise(system, strength: 0.6f, frequency: 1.3f, scrollSpeed: 1.3f);

            return system;
        }

        private static ParticleSystem BuildSmoke(Transform parent, FlamePalette palette)
        {
            ParticleSystem system = CreateLayer(parent, "Smoke", SmokeMaterial, sortingOrder: 5);

            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.6f, 3.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = palette.smoke;
            main.maxParticles = 90;

            SetEmission(system, 8f);
            SetShape(system, width: 0.5f, height: 0.06f);
            SetRise(system, minSpeed: 1.8f, maxSpeed: 2.4f, drift: 0.25f, buoyancy: 0.3f);
            SetSize(system, Curve((0f, 0.6f), (1f, 2.1f)));
            SetAlpha(system, (0f, 0f), (0.25f, 1f), (1f, 0f));
            SetSheet(system);
            SetNoise(system, strength: 0.35f, frequency: 0.4f, scrollSpeed: 0.5f);

            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

            return system;
        }

        // The pool of light on the ground. Few, large, barely there.
        private static ParticleSystem BuildGlow(Transform parent, FlamePalette palette)
        {
            ParticleSystem system = CreateLayer(parent, "Glow", GlowMaterial, sortingOrder: 8);

            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 1.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(5.5f, 7f);
            main.startColor = palette.glow;
            main.maxParticles = 20;

            SetEmission(system, 5f);
            SetShape(system, width: 0.3f, height: 0.02f);
            SetRise(system, minSpeed: 0.05f, maxSpeed: 0.2f, drift: 0.02f, buoyancy: 0f);
            SetSize(system, Curve((0f, 0.75f), (0.35f, 1f), (1f, 0.7f)));
            SetAlpha(system, (0f, 0f), (0.3f, 1f), (1f, 0f));

            return system;
        }

        private static Light2D BuildLight(Transform parent, FlamePalette palette)
        {
            GameObject holder = new("Light");
            holder.transform.SetParent(parent, worldPositionStays: false);
            holder.transform.localPosition = new Vector3(0f, 1f, 0f);

            Light2D light = holder.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = palette.glow;
            light.intensity = 1.5f;
            light.pointLightInnerRadius = 1f;
            light.pointLightOuterRadius = 9f;
            light.falloffIntensity = 0.6f;

            return light;
        }

        private static ParticleSystem CreateLayer(Transform parent, string name, string materialPath, int sortingOrder)
        {
            GameObject holder = new(name);
            holder.transform.SetParent(parent, worldPositionStays: false);

            ParticleSystem system = holder.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = system.main;
            main.duration = 4f;
            main.loop = true;
            main.prewarm = true;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0f;

            // Hierarchy scaling, so the whole fire can be resized from the root transform.
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystemRenderer renderer = holder.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            renderer.sortingOrder = sortingOrder;

            return system;
        }

        private static void SetEmission(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
        }

        // A thin slab at the base, so the flame has a width to grow out of rather than a point.
        private static void SetShape(ParticleSystem system, float width, float height)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width, height, 0.01f);
            shape.randomDirectionAmount = 0f;
        }

        // Start speed stays at zero: the climb is velocity plus a buoyancy force, which accelerates
        // particles as they rise instead of letting them coast.
        private static void SetRise(ParticleSystem system, float minSpeed, float maxSpeed, float drift, float buoyancy)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            velocity.x = new ParticleSystem.MinMaxCurve(-drift, drift);

            // All three axes have to share a mode; leaving z on its default constant makes the
            // system log an error every frame it simulates.
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            if (buoyancy <= 0f)
            {
                return;
            }

            ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.Local;
            force.y = new ParticleSystem.MinMaxCurve(buoyancy);
        }

        // Plays the pack's sheet across the particle's life, from a random frame so no two particles
        // burn in step. Without it a particle shows the whole grid of frames at once.
        private static void SetSheet(ParticleSystem system)
        {
            ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.numTilesX = SheetColumns;
            sheet.numTilesY = SheetRows;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 1f);
            sheet.cycleCount = 1;
        }

        private static void SetSize(ParticleSystem system, AnimationCurve curve)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        // Alpha only. Hue is the palette's job, and a colour key here would fight it.
        private static void SetAlpha(ParticleSystem system, params (float time, float alpha)[] keys)
        {
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                alphaKeys[i] = new GradientAlphaKey(keys[i].alpha, keys[i].time);
            }

            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                alphaKeys);

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void SetNoise(ParticleSystem system, float strength, float frequency, float scrollSpeed)
        {
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.scrollSpeed = scrollSpeed;
            noise.damping = true;
            noise.octaveCount = 2;
            noise.quality = ParticleSystemNoiseQuality.Medium;
        }

        private static AnimationCurve Curve(params (float time, float value)[] points)
        {
            Keyframe[] frames = new Keyframe[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                frames[i] = new Keyframe(points[i].time, points[i].value);
            }

            AnimationCurve curve = new(frames);
            for (int i = 0; i < frames.Length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }

            return curve;
        }
    }
}
