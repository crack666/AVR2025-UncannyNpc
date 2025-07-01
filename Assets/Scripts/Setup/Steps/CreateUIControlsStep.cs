using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating UI controls like Dropdowns, Sliders, and Toggles.
    /// </summary>
    public class CreateUIControlsStep
    {
        private System.Action<string> log;
        private GameObject panel;

        public TMP_Dropdown VoiceDropdown { get; private set; }
        public Slider VolumeSlider { get; private set; }
        public Toggle EnableVADToggle { get; private set; }

        public CreateUIControlsStep(System.Action<string> log, GameObject panel)
        {
            this.log = log;
            this.panel = panel;
        }

        public void Execute()
        {
            log("🎛️ Step 2.6: UI Control Creation");

            CreateVoiceDropdown();
            CreateVolumeSlider();
            CreateEnableVADToggle();

            log("✅ All UI controls created.");
        }

        private void CreateVoiceDropdown()
        {
            GameObject dropdownGO = new GameObject("Voice Dropdown", typeof(RectTransform));
            dropdownGO.transform.SetParent(panel.transform, false);
            var rect = dropdownGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.13f);
            rect.anchorMax = new Vector2(0.4f, 0.18f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();

            // Create default UI elements for the dropdown
            CreateDropdownTemplate(dropdown);

            var enumNames = System.Enum.GetNames(typeof(OpenAIVoice));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(enumNames));

            VoiceDropdown = dropdown;
            log("✅ Created Voice Dropdown.");
        }

        private void CreateVolumeSlider()
        {
            GameObject sliderGO = new GameObject("Volume Slider", typeof(RectTransform));
            sliderGO.transform.SetParent(panel.transform, false);
            var rect = sliderGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.45f, 0.13f);
            rect.anchorMax = new Vector2(0.9f, 0.18f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Add default slider graphics
            var bgImage = CreateImage(sliderGO, "Background", new Color(0.2f, 0.2f, 0.2f, 1f));
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGO.transform, false);
            var fillImage = CreateImage(fillArea, "Fill", new Color(0.3f, 0.7f, 1f, 1f));
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGO.transform, false);
            var handleImage = CreateImage(handleArea, "Handle", Color.white);

            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillImage.rectTransform;
            slider.handleRect = handleImage.rectTransform;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            VolumeSlider = slider;
            log("✅ Created Volume Slider.");
        }

        private void CreateEnableVADToggle()
        {
            GameObject toggleGO = new GameObject("Enable VAD Toggle", typeof(RectTransform));
            toggleGO.transform.SetParent(panel.transform, false);
            var rect = toggleGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.20f);
            rect.anchorMax = new Vector2(0.4f, 0.25f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var toggle = toggleGO.AddComponent<Toggle>();
            var bgImage = CreateImage(toggleGO, "Background", new Color(0.2f, 0.2f, 0.2f, 1f));
            var checkmarkImage = CreateImage(bgImage.gameObject, "Checkmark", Color.green);
            var label = CreateLabel(toggleGO, "Label", "Enable VAD");

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkmarkImage;
            toggle.isOn = true;

            EnableVADToggle = toggle;
            log("✅ Created Enable VAD Toggle.");
        }

        // Helper methods to reduce boilerplate
        private Image CreateImage(GameObject parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        private TextMeshProUGUI CreateLabel(GameObject parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1.05f, 0);
            rect.anchorMax = new Vector2(2f, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return label;
        }

        private void CreateDropdownTemplate(TMP_Dropdown dropdown)
        {
            // This is a simplified version of the template creation from the original script
            // It creates the necessary hierarchy for a TMP_Dropdown to function.
            var background = CreateImage(dropdown.gameObject, "Background", new Color(0.2f, 0.2f, 0.2f, 1f));
            dropdown.targetGraphic = background;

            var label = CreateLabel(dropdown.gameObject, "Label", "Voice");
            dropdown.captionText = label;

            var template = new GameObject("Template", typeof(RectTransform), typeof(ScrollRect));
            template.transform.SetParent(dropdown.transform, false);
            dropdown.template = template.GetComponent<RectTransform>();
            template.SetActive(false);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(template.transform, false);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemLabel = CreateLabel(item, "Item Label", "Option");
            dropdown.itemText = itemLabel;
        }
    }
}
