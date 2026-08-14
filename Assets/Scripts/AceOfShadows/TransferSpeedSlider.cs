using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoftGames.AceOfShadows
{
    // Drives AceOfShadowsController.TransferSpeed from a UI slider during play.
    public class TransferSpeedSlider : MonoBehaviour
    {
        [SerializeField] private AceOfShadowsController controller;
        [SerializeField] private Slider slider;

        [Tooltip("Shows the number only; any caption belongs in its own label.")]
        [SerializeField] private TMP_Text valueLabel;

        private void Start()
        {
            if (controller == null || slider == null)
            {
                Debug.LogError($"[{name}] Controller or slider is not assigned.", this);
                enabled = false;
                return;
            }

            // The controller owns the range; the slider only presents it.
            slider.minValue = controller.MinSpeed;
            slider.maxValue = controller.MaxSpeed;
            slider.SetValueWithoutNotify(controller.TransferSpeed);

            slider.onValueChanged.AddListener(OnSliderChanged);
            UpdateLabel(slider.value);
        }

        private void OnDestroy()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderChanged);
            }
        }

        private void OnSliderChanged(float value)
        {
            controller.TransferSpeed = value;
            UpdateLabel(value);
        }

        private void UpdateLabel(float value)
        {
            if (valueLabel != null)
            {
                valueLabel.SetText("{0:1}", value);
            }
        }
    }
}
