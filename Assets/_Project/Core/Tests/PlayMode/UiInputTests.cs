using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SoftGames.Core.Tests
{
    // An EventSystem on the legacy StandaloneInputModule looks correct in the inspector while every
    // button and slider stops responding. Only driving a pointer catches that.
    public class UiInputTests
    {
        [UnityTest]
        public IEnumerator EventSystemRunsTheInputSystemModule()
        {
            yield return TestSession.Enter();

            EventSystem events = EventSystem.current;
            Assert.IsNotNull(events, "No EventSystem is current.");

            Assert.IsInstanceOf<InputSystemUIInputModule>(events.currentInputModule,
                "The active input module is not the Input System one. With Player Settings on the " +
                "Input System package, the legacy module receives nothing.");

            InputSystemUIInputModule module = (InputSystemUIInputModule)events.currentInputModule;
            Assert.IsNotNull(module.actionsAsset, "The module has no actions asset, so it has no " +
                                                  "pointer or click to listen to.");
            Assert.IsTrue(module.isActiveAndEnabled, "The module is present but disabled.");
        }

        [UnityTest]
        public IEnumerator PointerClickOnAMenuEntry_StartsATransition()
        {
            yield return TestSession.Enter();

            MenuEntryButton entry = Object.FindFirstObjectByType<MenuEntryButton>();
            Assert.IsNotNull(entry, "The menu built no entries.");

            Button button = entry.GetComponent<Button>();
            Vector2 screenPoint = ScreenCentreOf(button.GetComponent<RectTransform>());

            // Raycast the way the EventSystem does. This is the half that breaks on a canvas with
            // no raycaster, a wrong sorting order, or something invisible covering the screen.
            PointerEventData pointer = new(EventSystem.current) { position = screenPoint };
            List<RaycastResult> hits = new();
            EventSystem.current.RaycastAll(pointer, hits);

            Assert.IsNotEmpty(hits, "Nothing under the menu button. The canvas has no working raycaster.");

            GameObject top = hits[0].gameObject;
            Assert.IsTrue(top.transform.IsChildOf(button.transform) || top == button.gameObject,
                $"'{top.name}' is on top of the menu button and swallows the click.");

            IGameNavigation navigation = GameManager.Instance.Navigation;
            Assert.IsFalse(navigation.IsBusy, "Already mid-transition before the click.");

            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;

            Assert.IsTrue(navigation.IsBusy, "The click did not start a scene transition.");

            while (navigation.IsBusy)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SpeedSlider_RespondsToADrag()
        {
            yield return TestSession.Enter();
            yield return TestSession.Navigate("AceOfShadows");

            Slider slider = Object.FindFirstObjectByType<Slider>();
            Assert.IsNotNull(slider, "Ace of Shadows has no slider.");

            float before = slider.value;
            RectTransform rect = slider.GetComponent<RectTransform>();

            // Drag to the far end of the track, through the same handler chain a real drag uses.
            PointerEventData pointer = new(EventSystem.current)
            {
                position = ScreenCentreOf(rect),
                button = PointerEventData.InputButton.Left,
            };

            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerDownHandler);

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;

            Assert.AreNotEqual(before, slider.value,
                "Dragging the slider changed nothing. This is what a dead input module looks like.");
            Assert.AreEqual(slider.maxValue, slider.value, 0.001f,
                "The drag landed, but not where it was aimed.");

            yield return TestSession.ReturnToMenu();
        }

        private static Vector2 ScreenCentreOf(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 centre = corners.Aggregate(Vector3.zero, (sum, corner) => sum + corner) / 4f;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            return RectTransformUtility.WorldToScreenPoint(camera, centre);
        }
    }
}
