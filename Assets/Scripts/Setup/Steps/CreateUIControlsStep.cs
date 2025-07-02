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
            
            // Setup Event Listeners für die neuen Controls
            SetupControlEventListeners();

            log("✅ All UI controls created and event listeners setup.");
        }

        private void CreateVoiceDropdown()
        {
            GameObject dropdownGO = new GameObject("Voice Dropdown", typeof(RectTransform));
            dropdownGO.transform.SetParent(panel.transform, false);
            var rect = dropdownGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.13f);
            rect.anchorMax = new Vector2(0.6f, 0.18f); // Breiter für längere Texte
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Background
            var backgroundGO = new GameObject("Background", typeof(RectTransform));
            backgroundGO.transform.SetParent(dropdownGO.transform, false);
            var bgRect = backgroundGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = backgroundGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // TMP_Dropdown
            var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = bgImage;

            // Label - Verbesserter Text-Overflow
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(dropdownGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-25, 0);
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Voice: alloy";
            label.fontSize = 11; // Etwas kleiner
            label.alignment = TextAlignmentOptions.Left;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            dropdown.captionText = label;

            // Arrow
            var arrowGO = new GameObject("Arrow", typeof(RectTransform));
            arrowGO.transform.SetParent(dropdownGO.transform, false);
            var arrowRect = arrowGO.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            var arrowImage = arrowGO.AddComponent<Image>();
            arrowImage.color = Color.white;

            // Template - Optimale Größe für ca. 5-6 sichtbare Items mit Scrolling
            var templateGO = new GameObject("Template", typeof(RectTransform));
            templateGO.transform.SetParent(dropdownGO.transform, false);
            templateGO.SetActive(false);
            var templateRect = templateGO.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1); // Top-center pivot
            templateRect.anchoredPosition = new Vector2(0, 0); // Direkt unterhalb des Dropdowns
            templateRect.sizeDelta = new Vector2(250, 240); // 6 Items * 40px = 240px visible height
            var templateImage = templateGO.AddComponent<Image>();
            templateImage.color = new Color(0.15f, 0.15f, 0.15f, 0.98f);

            // Viewport
            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(templateGO.transform, false);
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportMask = viewportGO.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f);

            // Content - Korrigiertes Anchoring für proper top alignment
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); // Links-oben anchor
            contentRect.anchorMax = new Vector2(1, 1); // Rechts-oben anchor  
            contentRect.pivot = new Vector2(0.5f, 1); // Top-center pivot
            contentRect.anchoredPosition = Vector2.zero; // Startet ganz oben
            contentRect.sizeDelta = new Vector2(0, 320); // Volle Höhe für alle 8 Items (8*40=320px)
            
            // VerticalLayoutGroup für automatisches Layout der Dropdown-Items
            var layoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = 0;
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            
            // ContentSizeFitter damit Content sich an Items anpasst
            var sizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // ScrollRect für flexibles Scrolling
            var scrollRect = templateGO.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Item (Option) - Angepasst für VerticalLayoutGroup
            var itemGO = new GameObject("Item", typeof(RectTransform));
            itemGO.transform.SetParent(contentGO.transform, false);
            var itemRect = itemGO.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 1); // Top-left anchor für VLG
            itemRect.anchorMax = new Vector2(1, 1); // Top-right anchor für VLG
            itemRect.pivot = new Vector2(0.5f, 1); // Top-center pivot
            itemRect.sizeDelta = new Vector2(0, 40); // Höhe für LayoutGroup
            
            // LayoutElement für VerticalLayoutGroup
            var layoutElement = itemGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = 40;
            layoutElement.preferredHeight = 40;
            layoutElement.flexibleHeight = 0;

            // Toggle für Item
            var toggle = itemGO.AddComponent<Toggle>();

            // Item Background
            var itemBGGO = new GameObject("Item Background", typeof(RectTransform));
            itemBGGO.transform.SetParent(itemGO.transform, false);
            var itemBGRect = itemBGGO.GetComponent<RectTransform>();
            itemBGRect.anchorMin = Vector2.zero;
            itemBGRect.anchorMax = Vector2.one;
            itemBGRect.offsetMin = Vector2.zero;
            itemBGRect.offsetMax = Vector2.zero;
            var itemBGImage = itemBGGO.AddComponent<Image>();
            itemBGImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            toggle.targetGraphic = itemBGImage;

            // Item Checkmark - Nur der äußere Rahmen, ohne dauerhaft sichtbares Inner Check
            var checkmarkGO = new GameObject("Item Checkmark", typeof(RectTransform));
            checkmarkGO.transform.SetParent(itemGO.transform, false);
            var checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(16, 16);
            checkmarkRect.anchoredPosition = new Vector2(12, 0);
            var checkmarkImage = checkmarkGO.AddComponent<Image>();
            // Transparenter Hintergrund, wird nur bei Auswahl grün
            checkmarkImage.color = new Color(0.4f, 0.4f, 0.4f, 0.3f);
            
            toggle.graphic = checkmarkImage;

            // Item Label - Mehr Platz für längeren Text
            var itemLabelGO = new GameObject("Item Label", typeof(RectTransform));
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            var itemLabelRect = itemLabelGO.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = new Vector2(0, 0);
            itemLabelRect.anchorMax = new Vector2(1, 1);
            itemLabelRect.offsetMin = new Vector2(30, 2); // Padding oben/unten
            itemLabelRect.offsetMax = new Vector2(-5, -2); // Weniger rechts padding
            var itemLabel = itemLabelGO.AddComponent<TextMeshProUGUI>();
            itemLabel.text = "Option";
            itemLabel.fontSize = 11; // Etwas kleiner für bessere Lesbarkeit
            itemLabel.alignment = TextAlignmentOptions.Left;
            itemLabel.color = Color.white;
            itemLabel.textWrappingMode = TextWrappingModes.NoWrap; // Kein Wrapping
            itemLabel.overflowMode = TextOverflowModes.Ellipsis; // Ellipsis bei zu langem Text

            // Set Dropdown Template References
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            dropdown.captionText = label;

            // Optionen befüllen mit OpenAI Voice Descriptions
            var descriptiveNames = OpenAIVoiceExtensions.GetAllVoiceDescriptions();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(descriptiveNames));
            
            // Setze korrekten Standardwert (alloy = Index 0)
            dropdown.value = 0; // alloy ist das erste Element
            dropdown.RefreshShownValue(); // Force UI Update

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
