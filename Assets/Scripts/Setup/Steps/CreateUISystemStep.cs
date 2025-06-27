using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Setup.Steps
{
    /// <summary>
    /// Step: Create and configure the main UI Canvas, Panel, Buttons, TextFields, InputField, and Manager
    /// </summary>
    public class CreateUISystemStep
    {
        public Canvas Canvas { get; private set; }
        public GameObject Panel { get; private set; }
        public MonoBehaviour UiManager { get; private set; }

        public void Execute(Vector2 panelSize, Vector2 panelPosition)
        {
            Debug.Log($"[UI-Setup][CreateUISystemStep] started with Panel Size: {panelSize}, Position: {panelPosition}");

            // --- Cleanup: Entferne alte UI-Objekte, damit keine Altlasten stören ---
            var oldPanel = GameObject.Find("NPC UI Panel");
            if (oldPanel != null)
            {
                Object.DestroyImmediate(oldPanel);
                Debug.Log("[UI-Setup] Altes NPC UI Panel entfernt.");
            }
            var oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null)
            {
                Object.DestroyImmediate(oldCanvas);
                Debug.Log("[UI-Setup] Alter Canvas entfernt.");
            }

            // --- Sicherstellen, dass ein EventSystem existiert (nach Canvas-Löschung!) ---
            // Unity 2022+: FindObjectsOfType ist veraltet, nutze FindObjectsByType
            var eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            if (eventSystems == null || eventSystems.Length == 0)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Debug.Log("[UI-Setup] EventSystem erstellt.");
            }
            else
            {
                Debug.Log("[UI-Setup] EventSystem bereits vorhanden.");
            }

            // Canvas
            GameObject canvasGO = new GameObject("Canvas");
            Canvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<CanvasScaler>();
            var gr = canvasGO.AddComponent<GraphicRaycaster>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = canvasGO.GetComponent<RectTransform>() ?? canvasGO.AddComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            Debug.Log("[UI-Setup] Canvas und GraphicRaycaster erstellt.");

            // Panel
            Panel = new GameObject("NPC UI Panel");
            Panel.transform.SetParent(Canvas.transform, false);
            var panelRect = Panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = panelPosition;
            var panelImage = Panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f,0.1f,0.1f,0.8f);
            Debug.Log("[UI-Setup] Panel erstellt und Canvas zugewiesen.");

            // Add NpcUiManager direkt nach Panel-Erstellung
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            if (uiManagerType != null)
            {
                UiManager = Panel.GetComponent(uiManagerType) as MonoBehaviour ?? Panel.AddComponent(uiManagerType) as MonoBehaviour;
                if (UiManager != null)
                {
                    Debug.Log($"[UI-Setup] NpcUiManager erfolgreich als Komponente hinzugefügt: {UiManager.GetType().FullName}");
                }
                else
                {
                    Debug.LogError($"[UI-Setup] NpcUiManager-Typ gefunden, aber konnte nicht als Komponente hinzugefügt werden!");
                }
            }
            else
            {
                Debug.LogError("[UI-Setup] NpcUiManager-Typ konnte nicht gefunden werden! Prüfe Namespace und Klassennamen.");
            }

            // Buttons
            CreateButton("Connect Button", "ConnectAAA", 0);
            CreateButton("Disconnect Button", "Disconnect", 1);
            CreateButton("Start Conversation Button", "Start Listening", 2);
            CreateButton("Stop Conversation Button", "Stop Listening", 3);
            CreateButton("Send Message Button", "Send", 4);

            // TextFields
            CreateTextMeshPro("Status Display", "Status: Disconnected", 0.45f, 0.5f, 14);
            CreateTextMeshPro("Conversation Display", "OpenAI Realtime NPC Chat...", 0.15f, 0.4f, 12);

            // InputField
            CreateInputField();
            Debug.Log($"[UI-Setup][CreateUISystemStep] CreateVoiceDropdown started");
            CreateVoiceDropdown();
            CreateVolumeSlider();
            CreateEnableVADToggle();

            // Debug: Check if all UI elements exist and are children of the panel
            DebugUIHierarchy();
            // Abschließende Prüfung und Logging aller Referenzen
            FinalizeAndLogSetup();
        }

        private void FinalizeAndLogSetup()
        {
            Debug.Log("[UI-Setup][Check] --- Abschlussprüfung aller UI-Komponenten und Referenzen ---");
            // Prüfe NpcUiManager
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            var uiManager = Panel != null && uiManagerType != null ? Panel.GetComponent(uiManagerType) : null;
            if (uiManager != null)
            {
                Debug.Log("[UI-Setup][Check] NpcUiManager ist vorhanden und korrekt am Panel.");
            }
            else
            {
                Debug.LogError("[UI-Setup][Check] NpcUiManager fehlt am Panel!");
            }
            // Prüfe Buttons
            string[] buttonNames = { "Connect Button", "Disconnect Button", "Start Conversation Button", "Stop Conversation Button", "Send Message Button" };
            foreach (var btn in buttonNames)
            {
                var go = GameObject.Find(btn);
                var comp = go != null ? go.GetComponent<Button>() : null;
                if (go == null)
                    Debug.LogError($"[UI-Setup][Check] Button GameObject '{btn}' fehlt!");
                else if (comp == null)
                    Debug.LogError($"[UI-Setup][Check] Button-Komponente fehlt an '{btn}'!");
                else if (!comp.interactable)
                    Debug.LogWarning($"[UI-Setup][Check] Button '{btn}' ist NICHT interactable!");
                else
                    Debug.Log($"[UI-Setup][Check] Button '{btn}' korrekt vorhanden und interactable.");
            }
            // Prüfe Dropdown
            var dropdownGO = GameObject.Find("Voice Dropdown");
            var dropdown = dropdownGO != null ? dropdownGO.GetComponent<TMP_Dropdown>() : null;
            if (dropdown == null)
                Debug.LogError("[UI-Setup][Check] Voice Dropdown fehlt oder hat keine TMP_Dropdown-Komponente!");
            else
                Debug.Log("[UI-Setup][Check] Voice Dropdown korrekt vorhanden.");
            // Prüfe Volume Slider
            var sliderGO = GameObject.Find("Volume Slider");
            var slider = sliderGO != null ? sliderGO.GetComponent<Slider>() : null;
            if (slider == null)
                Debug.LogError("[UI-Setup][Check] Volume Slider fehlt oder hat keine Slider-Komponente!");
            else
                Debug.Log("[UI-Setup][Check] Volume Slider korrekt vorhanden.");
            // Prüfe VAD Toggle
            var toggleGO = GameObject.Find("Enable VAD Toggle");
            var toggle = toggleGO != null ? toggleGO.GetComponent<Toggle>() : null;
            if (toggle == null)
                Debug.LogError("[UI-Setup][Check] Enable VAD Toggle fehlt oder hat keine Toggle-Komponente!");
            else
                Debug.Log("[UI-Setup][Check] Enable VAD Toggle korrekt vorhanden.");
            // Prüfe Statusfeld Raycast
            var statusGO = GameObject.Find("Status Display");
            var statusText = statusGO != null ? statusGO.GetComponent<TextMeshProUGUI>() : null;
            if (statusText != null && statusText.raycastTarget)
                Debug.LogWarning("[UI-Setup][Check] Statusfeld blockiert Raycasts! (raycastTarget=true)");
            else
                Debug.Log("[UI-Setup][Check] Statusfeld blockiert keine Raycasts.");
        }

        private void CreateButton(string name, string text, int index)
        {
            if (GameObject.Find(name) != null) return;
            GameObject button = new GameObject(name);
            button.transform.SetParent(Panel.transform, false);
            Image buttonImage = button.AddComponent<Image>();
            // Farbgebung wie "alte" Version
            if (name.Contains("Connect"))
                buttonImage.color = new Color(0.2f, 0.5f, 0.2f, 1f); // grün
            else if (name.Contains("Disconnect"))
                buttonImage.color = new Color(0.5f, 0.2f, 0.2f, 1f); // rot
            else if (name.Contains("Send"))
                buttonImage.color = new Color(0.2f, 0.2f, 0.5f, 1f); // blau
            else
                buttonImage.color = new Color(0.2f, 0.6f, 1.0f, 1.0f); // Standard
            Button buttonComponent = button.AddComponent<Button>();
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            // Button-Placement: Send Button neben Message Box, Rest oben horizontal
            if (name.Contains("Send"))
            {
                rectTransform.anchorMin = new Vector2(0.91f, 0.05f);
                rectTransform.anchorMax = new Vector2(0.99f, 0.12f);
            }
            else
            {
                float x = 0.1f + index * 0.18f;
                rectTransform.anchorMin = new Vector2(x, 0.85f);
                rectTransform.anchorMax = new Vector2(x + 0.16f, 0.95f);
            }
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Button-Referenz im NpcUiManager setzen (per Reflection, falls Feld vorhanden)
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            if (uiManagerType != null && Panel != null)
            {
                var uiManager = Panel.GetComponent(uiManagerType);
                if (uiManager != null)
                {
                    string fieldName = null;
                    if (name.StartsWith("Connect")) fieldName = "connectButton";
                    else if (name.StartsWith("Disconnect")) fieldName = "disconnectButton";
                    else if (name.StartsWith("Start")) fieldName = "startConversationButton";
                    else if (name.StartsWith("Stop")) fieldName = "stopConversationButton";
                    else if (name.StartsWith("Send")) fieldName = "sendMessageButton";
                    if (fieldName != null)
                    {
                        var field = uiManagerType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (field != null)
                        {
                            field.SetValue(uiManager, buttonComponent);
                            Debug.Log($"[UI-Setup] Button-Referenz '{fieldName}' im NpcUiManager gesetzt.");
                        }
                        else
                        {
                            Debug.LogWarning($"[UI-Setup] Feld '{fieldName}' im NpcUiManager nicht gefunden.");
                        }
                    }
                }
            }
        }

        private void CreateTextMeshPro(string name, string text, float yMin, float yMax, int fontSize)
        {
            if (GameObject.Find(name) != null) return;
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(Panel.transform, false);
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.textWrappingMode = TMPro.TextWrappingModes.Normal;
            textComponent.raycastTarget = false; // Blockiert keine Klicks!
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, yMin);
            rectTransform.anchorMax = new Vector2(0.9f, yMax);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateInputField()
        {
            if (GameObject.Find("Message Input Field") != null) return;
            GameObject inputField = new GameObject("Message Input Field");
            inputField.transform.SetParent(Panel.transform, false);
            Image inputImage = inputField.AddComponent<Image>();
            inputImage.color = new Color(1f, 1f, 1f, 0.1f);
            TMP_InputField inputComponent = inputField.AddComponent<TMP_InputField>();
            RectTransform rectTransform = inputField.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.05f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.12f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            // Text Area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputField.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);
            // Placeholder
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Type your message here...";
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(1f, 1f, 1f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.Left;
            RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            // Text
            GameObject text = new GameObject("Text");
            text.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            // Link to input field
            inputComponent.textViewport = textAreaRect;
            inputComponent.textComponent = textComponent;
            inputComponent.placeholder = placeholderText;
        }

        private void CreateVoiceDropdown()
        {
            // Immer neu erstellen, keine Existenzprüfung
            GameObject dropdownGO = new GameObject("Voice Dropdown", typeof(RectTransform));
            dropdownGO.transform.SetParent(Panel.transform, false);
            var rect = dropdownGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.13f);
            rect.anchorMax = new Vector2(0.4f, 0.18f);
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

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(dropdownGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-25, 0);
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Voice";
            label.fontSize = 14;
            label.alignment = TextAlignmentOptions.Left;
            label.color = Color.white;
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
            // (Optional: Set sprite for arrow if available)

            // Template
            var templateGO = new GameObject("Template", typeof(RectTransform));
            templateGO.transform.SetParent(dropdownGO.transform, false);
            templateGO.SetActive(false);
            var templateRect = templateGO.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 120);
            var templateImage = templateGO.AddComponent<Image>();
            templateImage.color = new Color(0.15f, 0.15f, 0.15f, 0.98f);
            templateImage.raycastTarget = true;
            var scrollRect = templateGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

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
            viewportImage.color = new Color(1, 1, 1, 0.1f);
            scrollRect.viewport = viewportRect;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 28);
            scrollRect.content = contentRect;

            // Item (Option) mit Toggle (Pflicht für TMP_Dropdown!)
            var itemGO = new GameObject("Item", typeof(RectTransform));
            itemGO.transform.SetParent(contentGO.transform, false);
            var itemRect = itemGO.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 28);
            // Toggle
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
            // Item Checkmark
            var checkmarkGO = new GameObject("Item Checkmark", typeof(RectTransform));
            checkmarkGO.transform.SetParent(itemGO.transform, false);
            var checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(20, 20);
            checkmarkRect.anchoredPosition = new Vector2(10, 0);
            var checkmarkImage = checkmarkGO.AddComponent<Image>();
            checkmarkImage.color = Color.green;
            toggle.graphic = checkmarkImage;
            // Item Label
            var itemLabelGO = new GameObject("Item Label", typeof(RectTransform));
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            var itemLabelRect = itemLabelGO.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = new Vector2(0, 0);
            itemLabelRect.anchorMax = new Vector2(1, 1);
            itemLabelRect.offsetMin = new Vector2(30, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);
            var itemLabel = itemLabelGO.AddComponent<TextMeshProUGUI>();
            itemLabel.text = "Option";
            itemLabel.fontSize = 14;
            itemLabel.alignment = TextAlignmentOptions.Left;
            itemLabel.color = Color.white;

            // Set Dropdown Template References
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            dropdown.captionText = label;
            dropdown.itemImage = itemBGImage;
            dropdown.targetGraphic = bgImage;

            // Hierarchy for TMP_Dropdown (Unity expects this structure)
            // Template
            //   Viewport
            //     Content
            //       Item (mit Toggle!)
            //         Item Background
            //         Item Checkmark
            //         Item Label
            //   (Scrollbar optional)

            // Optionen befüllen
            var enumNames = System.Enum.GetNames(typeof(OpenAIVoice));
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string>(enumNames));

            // Panel-Manager referenzieren und Dropdown setzen
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            if (uiManagerType != null)
            {
                var uiManager = Panel.GetComponent(uiManagerType);
                var field = uiManagerType.GetField("voiceDropdown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(uiManager, dropdown);
                }
            }
        }

        private void CreateVolumeSlider()
        {
            GameObject sliderGO = new GameObject("Volume Slider", typeof(RectTransform));
            sliderGO.transform.SetParent(Panel.transform, false);
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

            // Slider
            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Reference in UI Manager
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            if (uiManagerType != null)
            {
                var uiManager = Panel.GetComponent(uiManagerType);
                var field = uiManagerType.GetField("volumeSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(uiManager, slider);
                }
            }
        }

        private void CreateEnableVADToggle()
        {
            GameObject toggleGO = new GameObject("Enable VAD Toggle", typeof(RectTransform));
            toggleGO.transform.SetParent(Panel.transform, false);
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
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Checkmark
            var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform));
            checkmarkGO.transform.SetParent(bgGO.transform, false);
            var checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.25f, 0.25f);
            checkmarkRect.anchorMax = new Vector2(0.75f, 0.75f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            var checkmarkImage = checkmarkGO.AddComponent<Image>();
            checkmarkImage.color = Color.green;

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

            // Toggle
            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.graphic = checkmarkImage;
            toggle.targetGraphic = bgImage;
            toggle.isOn = false;

            // Reference in UI Manager
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            if (uiManagerType != null)
            {
                var uiManager = Panel.GetComponent(uiManagerType);
                var field = uiManagerType.GetField("enableVADToggle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(uiManager, toggle);
                }
            }
        }

        private void DebugUIHierarchy()
        {
            string[] names = { "Voice Dropdown", "Volume Slider", "Enable VAD Toggle" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go == null)
                {
                    Debug.LogError($"[UI-Setup] GameObject '{n}' wurde NICHT gefunden!");
                }
                else if (go.transform.parent != Panel.transform)
                {
                    Debug.LogWarning($"[UI-Setup] GameObject '{n}' ist NICHT direktes Child von NPC UI Panel!");
                }
                else
                {
                    Debug.Log($"[UI-Setup] GameObject '{n}' korrekt erstellt und im Panel platziert.");
                }
            }

            // Zusätzliche Prüfung: Buttons
            string[] buttonNames = { "Connect Button", "Disconnect Button", "Start Conversation Button", "Stop Conversation Button", "Send Message Button" };
            foreach (var btnName in buttonNames)
            {
                var btnGO = GameObject.Find(btnName);
                if (btnGO == null)
                {
                    Debug.LogError($"[UI-Setup] Button '{btnName}' wurde NICHT gefunden!");
                }
                else if (!btnGO.activeInHierarchy)
                {
                    Debug.LogError($"[UI-Setup] Button '{btnName}' ist NICHT aktiv!");
                }
                else if (btnGO.GetComponent<Button>() == null)
                {
                    Debug.LogError($"[UI-Setup] Button '{btnName}' hat KEIN Button-Component!");
                }
                else
                {
                    Debug.Log($"[UI-Setup] Button '{btnName}' korrekt erstellt, aktiv und Button-Component vorhanden.");
                }
            }

            // Prüfung: NpcUiManager
            if (UiManager == null)
            {
                Debug.LogError("[UI-Setup] NpcUiManager wurde NICHT als Komponente am Panel gefunden!");
            }
            else
            {
                Debug.Log($"[UI-Setup] NpcUiManager ist vorhanden: {UiManager.GetType().FullName}");
            }
        }
    }
}
