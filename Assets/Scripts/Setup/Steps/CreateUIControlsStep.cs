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

            CreateVoiceCheckboxes();
            CreateVolumeSlider();
            CreateEnableVADToggle();
            
            // Setup Event Listeners für die neuen Controls
            SetupControlEventListeners();

            log("✅ All UI controls created and event listeners setup.");
        }

        private void CreateVoiceCheckboxes()
        {
            // Voice Selection Group Container
            GameObject voiceGroupGO = new GameObject("Voice Selection Group", typeof(RectTransform));
            voiceGroupGO.transform.SetParent(panel.transform, false);
            var groupRect = voiceGroupGO.GetComponent<RectTransform>();
            // Gleiche Position wie das alte Dropdown, aber etwas größer für alle Checkboxes
            groupRect.anchorMin = new Vector2(0.1f, 0.05f);
            groupRect.anchorMax = new Vector2(0.42f, 0.18f);
            groupRect.offsetMin = Vector2.zero;
            groupRect.offsetMax = Vector2.zero;

            // Title Label
            var titleGO = new GameObject("Voice Title", typeof(RectTransform));
            titleGO.transform.SetParent(voiceGroupGO.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.85f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleLabel = titleGO.AddComponent<TextMeshProUGUI>();
            titleLabel.text = "Voice:";
            titleLabel.fontSize = 10;
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.alignment = TextAlignmentOptions.Left;
            titleLabel.color = Color.white;

            // Get voice options
            var voiceDescriptions = OpenAIVoiceExtensions.GetAllVoiceDescriptions();
            var voiceCount = voiceDescriptions.Length;
            float checkboxHeight = 0.8f / voiceCount; // Verteilung über verfügbare Höhe

            // Create ToggleGroup for exclusive selection
            var toggleGroup = voiceGroupGO.AddComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = false; // Immer eine auswählen

            // Create checkboxes for each voice (vertically stacked)
            for (int i = 0; i < voiceCount; i++)
            {
                // Checkbox Container
                GameObject checkboxGO = new GameObject($"Voice_{voiceDescriptions[i]}_Checkbox", typeof(RectTransform));
                checkboxGO.transform.SetParent(voiceGroupGO.transform, false);
                var checkboxRect = checkboxGO.GetComponent<RectTransform>();
                
                // Position vertically based on index (stack from top to bottom)
                float startY = 0.8f - (i * checkboxHeight);
                float endY = startY - checkboxHeight;
                checkboxRect.anchorMin = new Vector2(0f, endY);
                checkboxRect.anchorMax = new Vector2(1f, startY);
                checkboxRect.offsetMin = new Vector2(2, 1); // Small padding
                checkboxRect.offsetMax = new Vector2(-2, -1);

                // Background
                var bgGO = new GameObject("Background", typeof(RectTransform));
                bgGO.transform.SetParent(checkboxGO.transform, false);
                var bgRect = bgGO.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0, 0.2f);
                bgRect.anchorMax = new Vector2(0.2f, 0.8f);
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                var bgImage = bgGO.AddComponent<Image>();
                bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                // Checkmark
                var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform));
                checkmarkGO.transform.SetParent(bgGO.transform, false);
                var checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
                checkmarkRect.anchorMin = new Vector2(0.1f, 0.1f);
                checkmarkRect.anchorMax = new Vector2(0.9f, 0.9f);
                checkmarkRect.offsetMin = Vector2.zero;
                checkmarkRect.offsetMax = Vector2.zero;
                var checkmarkImage = checkmarkGO.AddComponent<Image>();
                checkmarkImage.color = new Color(0f, 0.8f, 0f, 1f); // Bright green

                // Label
                var labelGO = new GameObject("Label", typeof(RectTransform));
                labelGO.transform.SetParent(checkboxGO.transform, false);
                var labelRect = labelGO.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.25f, 0f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelGO.AddComponent<TextMeshProUGUI>();
                label.text = voiceDescriptions[i];
                label.fontSize = 8; // Kleine Schrift für kompakte Darstellung
                label.fontStyle = FontStyles.Normal;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = Color.white;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;

                // Toggle Component
                var toggle = checkboxGO.AddComponent<Toggle>();
                toggle.targetGraphic = bgImage;
                toggle.graphic = checkmarkImage;
                toggle.group = toggleGroup;
                toggle.isOn = (i == 0); // Erste Option (alloy) ist standardmäßig ausgewählt

                // Enhanced colors for better visibility
                var colors = ColorBlock.defaultColorBlock;
                colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                colors.highlightedColor = new Color(0f, 0.6f, 1f, 1f); // Bright blue highlight
                colors.pressedColor = new Color(0f, 0.8f, 0f, 1f);     // Bright green pressed
                colors.selectedColor = new Color(0f, 0.8f, 0f, 0.8f);  // Green selected
                colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                toggle.colors = colors;

                // Set UI layer
                checkboxGO.layer = LayerMask.NameToLayer("UI");

                // Add VoiceCheckboxHandler component for proper event handling
                var handler = checkboxGO.AddComponent<UI.VoiceCheckboxHandler>();
                handler.SetVoiceIndex(i);
                
                // Explicitly set the checkbox reference using reflection
                var checkboxField = typeof(UI.VoiceCheckboxHandler).GetField("checkbox", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (checkboxField != null)
                {
                    checkboxField.SetValue(handler, toggle);
                }
                
                UnityEngine.Debug.Log($"[UI] Created voice checkbox {i} with handler and toggle reference");
            }

            // Keep VoiceDropdown property for compatibility (but it will be null)
            VoiceDropdown = null;
            log("✅ Created Voice Checkboxes (replaced dropdown).");
        }

        private void CreateVolumeSlider()
        {
            GameObject sliderGO = new GameObject("Volume Slider", typeof(RectTransform));
            sliderGO.transform.SetParent(panel.transform, false);
            var rect = sliderGO.GetComponent<RectTransform>();
            // Verschoben nach rechts um Überlappung zu vermeiden
            rect.anchorMin = new Vector2(0.46f, 0.13f); // Mehr Abstand zum Dropdown
            rect.anchorMax = new Vector2(0.9f, 0.18f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Background
            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill Area
            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10, 0);
            fillAreaRect.offsetMax = new Vector2(-10, 0);

            // Fill
            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f, 1f);

            // Handle Slide Area
            var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Handle
            var handleGO = new GameObject("Handle", typeof(RectTransform));
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = handleGO.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 40);
            var handleImage = handleGO.AddComponent<Image>();
            handleImage.color = Color.white;

            // Slider Component
            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
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

            // Background
            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(toggleGO.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Grauer Background statt grün

            // Checkmark - Einfacheres Design ohne dauerhaft sichtbares Inner Check
            var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform));
            checkmarkGO.transform.SetParent(bgGO.transform, false);
            var checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            var checkmarkImage = checkmarkGO.AddComponent<Image>();
            // Wird nur bei Aktivierung sichtbar
            checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(toggleGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(1.05f, 0);
            labelRect.anchorMax = new Vector2(2f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Enable VAD";
            label.fontSize = 14;
            label.alignment = TextAlignmentOptions.Left;
            label.color = Color.white;

            // Toggle Component mit verbesserter Farbkonfiguration
            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkmarkImage;
            toggle.isOn = false; // Standardmäßig aus
            
            // ColorBlock für bessere visuelle Rückmeldung
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            toggle.colors = colors;

            EnableVADToggle = toggle;
            log("✅ Created Enable VAD Toggle.");
        }

        private void SetupControlEventListeners()
        {
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            if (uiManagerType == null || panel == null) 
            {
                log("⚠️ Cannot setup event listeners - NpcUiManager type not found");
                return;
            }

            var uiManager = panel.GetComponent(uiManagerType);
            if (uiManager == null) 
            {
                log("⚠️ Cannot setup event listeners - NpcUiManager not found on panel");
                return;
            }

            // VAD Toggle Event
            if (EnableVADToggle != null)
            {
                var vadMethod = uiManagerType.GetMethod("OnVADToggleChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (vadMethod != null)
                {
                    EnableVADToggle.onValueChanged.AddListener((bool value) => vadMethod.Invoke(uiManager, new object[] { value }));
                    log("✅ VAD Toggle event listener added.");
                }
                else
                {
                    log("⚠️ OnVADToggleChanged method not found in NpcUiManager");
                }
            }

            // Volume Slider Event
            if (VolumeSlider != null)
            {
                var volumeMethod = uiManagerType.GetMethod("OnVolumeChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (volumeMethod != null)
                {
                    VolumeSlider.onValueChanged.AddListener((float value) => volumeMethod.Invoke(uiManager, new object[] { value }));
                    log("✅ Volume Slider event listener added.");
                }
                else
                {
                    log("⚠️ OnVolumeChanged method not found in NpcUiManager");
                }
            }

            // Voice Dropdown Event
            if (VoiceDropdown != null)
            {
                var voiceMethod = uiManagerType.GetMethod("OnVoiceDropdownChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (voiceMethod != null)
                {
                    VoiceDropdown.onValueChanged.AddListener((int value) => voiceMethod.Invoke(uiManager, new object[] { value }));
                    log("✅ Voice Dropdown event listener added.");
                }
                else
                {
                    log("⚠️ OnVoiceDropdownChanged method not found in NpcUiManager");
                }
            }
        }
    }
}
