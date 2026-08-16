using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;

namespace SoftGames.PhoenixFlame.Tests
{
    // The palette asset and the animator graph are joined by state name alone. Renaming an entry
    // compiles, imports and plays — it only turns into a red console the moment that colour comes
    // round. This is what catches it at import instead.
    public class FlamePaletteGraphTests
    {
        private const string PalettesPath = "Assets/_Project/PhoenixFlame/ScriptableObjects/FlamePalettes.asset";
        private const string ControllerPath = "Assets/_Project/PhoenixFlame/Animation/FlameColor.controller";

        private FlamePaletteSet _palettes;
        private string[] _stateNames;

        [OneTimeSetUp]
        public void LoadAssets()
        {
            _palettes = AssetDatabase.LoadAssetAtPath<FlamePaletteSet>(PalettesPath);
            Assert.IsNotNull(_palettes, $"No palette set at {PalettesPath}.");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(controller, $"No animator controller at {ControllerPath}.");

            _stateNames = controller.layers[0].stateMachine.states
                .Select(state => state.state.name)
                .ToArray();
        }

        [Test]
        public void PaletteSetIsNotEmpty()
        {
            Assert.Greater(_palettes.Count, 0, "The palette set has no entries; the fire has no colour.");
        }

        [Test]
        public void EveryPaletteHasAStateOfTheSameName()
        {
            for (int i = 0; i < _palettes.Count; i++)
            {
                Assert.Contains(_palettes.NameAt(i), _stateNames,
                    $"Palette '{_palettes.NameAt(i)}' has no animator state of that name, so the " +
                    "fire never fades to it. Run Tools ▸ SoftGames ▸ Phoenix Flame ▸ Rebuild " +
                    "Colour Animator.");
            }
        }

        [Test]
        public void EveryStateHasAPaletteOfTheSameName()
        {
            string[] paletteNames = Enumerable.Range(0, _palettes.Count)
                .Select(_palettes.NameAt)
                .ToArray();

            foreach (string state in _stateNames)
            {
                Assert.Contains(state, paletteNames,
                    $"Animator state '{state}' has no palette of that name; entering it logs an " +
                    "error and leaves the colour where it was.");
            }
        }

        [Test]
        public void PaletteNamesAreDistinct()
        {
            string[] names = Enumerable.Range(0, _palettes.Count).Select(_palettes.NameAt).ToArray();

            // Two entries of one name collapse to one state, so the second is unreachable.
            CollectionAssert.AllItemsAreUnique(names);
        }
    }
}
