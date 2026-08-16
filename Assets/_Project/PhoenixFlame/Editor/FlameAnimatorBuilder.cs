using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SoftGames.PhoenixFlame.EditorTools
{
    /// <summary>
    /// Builds the colour cycle: one state per palette, named after it, chained on the "Next"
    /// trigger, every state playing the same fade clip. The clip keys <c>fade</c> from 0 to 1 on an
    /// ease — the colours themselves are not in here, they live in the palette set asset, which this
    /// tool seeds once and then leaves alone.
    ///
    /// Existing assets are rewritten in place rather than replaced — a fresh asset would take a new
    /// GUID and quietly empty the slot in the scene that referenced it.
    /// </summary>
    public static class FlameAnimatorBuilder
    {
        private const string Folder = "Assets/_Project/PhoenixFlame/Animation";
        private const string ControllerPath = Folder + "/FlameColor.controller";
        private const string ClipPath = Folder + "/Flame_Fade.anim";
        private const string PaletteSetPath = "Assets/_Project/PhoenixFlame/ScriptableObjects/FlamePalettes.asset";

        // Matches the trigger FlameColorController fires.
        private const string NextTrigger = "Next";

        private const float FadeSeconds = 1.25f;

        [MenuItem("Tools/SoftGames/Phoenix Flame/Rebuild Colour Animator")]
        public static void Rebuild()
        {
            EditorFolders.Ensure(Folder);
            SeedPaletteSet();

            FlamePaletteSet palettes = AssetDatabase.LoadAssetAtPath<FlamePaletteSet>(PaletteSetPath);
            if (palettes == null || palettes.Count == 0)
            {
                Debug.LogError($"Phoenix Flame: no palettes at {PaletteSetPath} to build states from.");
                return;
            }

            AnimationClip clip = BuildFadeClip();

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            foreach (ChildAnimatorState child in machine.states)
            {
                machine.RemoveState(child.state);
            }

            while (controller.parameters.Length > 0)
            {
                controller.RemoveParameter(0);
            }

            controller.AddParameter(NextTrigger, AnimatorControllerParameterType.Trigger);

            AnimatorState[] states = new AnimatorState[palettes.Count];

            for (int i = 0; i < palettes.Count; i++)
            {
                // The name is the link to the palette of the same name; FlameColorState reads it back.
                states[i] = machine.AddState(palettes.NameAt(i), new Vector3(300f, 40f + i * 80f, 0f));
                states[i].motion = clip;
                states[i].writeDefaultValues = false;
                states[i].AddStateMachineBehaviour<FlameColorState>();
            }

            machine.defaultState = states[0];

            for (int i = 0; i < states.Length; i++)
            {
                AnimatorStateTransition transition = states[i].AddTransition(states[(i + 1) % states.Length]);
                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                transition.AddCondition(AnimatorConditionMode.If, 0f, NextTrigger);

                // Instant, and it has to stay instant: every state plays the same clip, so a blend
                // would mix the outgoing fade — sitting at 1 — into the incoming one and start the
                // fade part-way through. The fade is the clip's job, not the transition's.
                transition.duration = 0f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"Phoenix Flame: colour animator written to {ControllerPath} with {states.Length} states.");
        }

        // One curve. Easing it here is the whole reason the fade is an animation and not a number.
        private static AnimationClip BuildFadeClip()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 60f;

            EditorCurveBinding binding =
                EditorCurveBinding.FloatCurve(string.Empty, typeof(FlameColorController), "fade");

            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.EaseInOut(0f, 0f, FadeSeconds, 1f));
            EditorUtility.SetDirty(clip);

            return clip;
        }

        // Created once with the three colours from the brief. Never overwritten: from then on the
        // asset is the authority, and editing it is how the fire's colours change.
        private static void SeedPaletteSet()
        {
            if (AssetDatabase.LoadAssetAtPath<FlamePaletteSet>(PaletteSetPath) != null)
            {
                return;
            }

            FlamePaletteSet set = ScriptableObject.CreateInstance<FlamePaletteSet>();
            AssetDatabase.CreateAsset(set, PaletteSetPath);

            SerializedObject serialized = new(set);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = FlamePalettes.Cycle.Length;

            for (int i = 0; i < FlamePalettes.Cycle.Length; i++)
            {
                (string name, FlamePalette palette) = FlamePalettes.Cycle[i];
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);

                entry.FindPropertyRelative("name").stringValue = name;
                SerializedProperty colors = entry.FindPropertyRelative("palette");
                colors.FindPropertyRelative("core").colorValue = palette.core;
                colors.FindPropertyRelative("body").colorValue = palette.body;
                colors.FindPropertyRelative("ember").colorValue = palette.ember;
                colors.FindPropertyRelative("smoke").colorValue = palette.smoke;
                colors.FindPropertyRelative("glow").colorValue = palette.glow;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"Phoenix Flame: palette set seeded at {PaletteSetPath}.");
        }
    }
}
