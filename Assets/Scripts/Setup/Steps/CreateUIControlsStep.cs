using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Setup;

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
            // Subscribe to custom avatar loaded event
            Setup.AvatarManager.Instance.OnCustomAvatarLoaded += OnCustomAvatarLoaded;
        }

        private void OnCustomAvatarLoaded(GameObject customAvatar)
        {
            log("🔄 Custom Avatar loaded, refreshing Select Avatar UI...");
            CreateSelectAvatarUI();
        }

        public void Execute()
        {
            log("🎛️ Step 2.6: UI Control Creation");

            CreateVoiceCheckboxes();
            CreateSelectAvatarUI();
            CreateVolumeSlider();
            CreateEnableVADToggle();
            
            // Setup Event Listeners für die neuen Controls
            SetupControlEventListeners();

            log("✅ All UI controls created and event listeners setup.");
        }

        private void CreateVoiceCheckboxes()
        {
            // Voice Selection Group Container - positioned on the right side of canvas, moved higher
            GameObject voiceGroupGO = new GameObject("Voice Selection Group", typeof(RectTransform));
            voiceGroupGO.transform.SetParent(panel.transform, false);
            var groupRect = voiceGroupGO.GetComponent<RectTransform>();
            // Position on right side: from 55% to 95% horizontally, moved up higher from 0.15f to 0.25f
        
            // Set absolute position and size: left -240.6, top 34.3, right 240.6, bottom -34.3, pos z 0
            groupRect.anchorMin = new Vector2(0.5f, 0.5f);
            groupRect.anchorMax = new Vector2(0.5f, 0.5f);
            groupRect.pivot = new Vector2(0.5f, 0.5f);
            groupRect.anchoredPosition = Vector2.zero;
            groupRect.sizeDelta = new Vector2(240.6f + 240.6f, 34.3f + 34.3f); // width: 481.2, height: 68.6
            groupRect.localPosition = new Vector3(50f, 150f, 0f); // pos z 0

            // Title Label
            var titleGO = new GameObject("Voice Title", typeof(RectTransform));
            titleGO.transform.SetParent(voiceGroupGO.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.88f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleLabel = titleGO.AddComponent<TextMeshProUGUI>();
            titleLabel.text = "Voice Selection";
            titleLabel.fontSize = 8; // Verkleinerte Schrift
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.alignment = TextAlignmentOptions.Left;
            titleLabel.color = Color.white;

            // Get voice options with better descriptions
            var voiceCount = OpenAIVoiceExtensions.GetVoiceCount();
            var voiceDescriptions = new string[voiceCount];
            
            // Use the existing GetDescription method for better descriptions
            for (int i = 0; i < voiceCount; i++)
            {
                var voice = OpenAIVoiceExtensions.FromIndex(i);
                voiceDescriptions[i] = voice.GetDescription();
            }
            
            float availableHeight = 0.80f; // Weniger Platz für kleinere Abstände
            float checkboxHeight = availableHeight / (voiceCount + 0.5f); // Etwas enger gestapelt

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
                
                // Engere vertikale Stapelung
                float startY = availableHeight - (i * checkboxHeight);
                float endY = startY - checkboxHeight + 0.01f; // Minimale Überlappung für weniger Abstand
                checkboxRect.anchorMin = new Vector2(0f, endY);
                checkboxRect.anchorMax = new Vector2(0.25f, startY); // kleinerer selektierbarer Bereich
                checkboxRect.offsetMin = new Vector2(2, 1); // Weniger Padding oben/unten
                checkboxRect.offsetMax = new Vector2(-2, -1);

                // Background - smaller checkbox area
                var bgGO = new GameObject("Background", typeof(RectTransform));
                bgGO.transform.SetParent(checkboxGO.transform, false);
                var bgRect = bgGO.GetComponent<RectTransform>();
                // Background wieder kompakt wie ursprünglich
                bgRect.anchorMin = new Vector2(0, 0.2f);
                bgRect.anchorMax = new Vector2(0.11f, 0.9f); // kompakte Checkbox
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

                // Label - adjusted for smaller checkbox
                var labelGO = new GameObject("Label", typeof(RectTransform));
                labelGO.transform.SetParent(checkboxGO.transform, false);
                var labelRect = labelGO.GetComponent<RectTransform>();
                // Label deutlich breiter und höher, Background bleibt kompakt
                labelRect.anchorMin = new Vector2(0.13f, 0f); // direkt nach der kompakten Checkbox
                labelRect.anchorMax = new Vector2(4f, 1f); // deutlich breiter als das Parent
                labelRect.offsetMin = new Vector2(8, 4); // mehr Padding
                labelRect.offsetMax = new Vector2(-8, -4);
                labelRect.sizeDelta = new Vector2(0, 38); // explizit mehr Höhe
                var label = labelGO.AddComponent<TextMeshProUGUI>();
                label.text = voiceDescriptions[i];
                label.fontSize = 6; // größer
                label.fontStyle = FontStyles.Normal;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = Color.white;
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Overflow;

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

        private void CreateSelectAvatarUI()
        {
            log("🎭 Creating Select Avatar UI...");

            // Remove old Select Avatar UI if it exists
            Transform old = panel.transform.Find("Select Avatar");
            if (old != null)
            {
                GameObject.DestroyImmediate(old.gameObject);
            }

            // Create the main Select Avatar container
            GameObject selectAvatarGO = new GameObject("Select Avatar", typeof(RectTransform));
            selectAvatarGO.transform.SetParent(panel.transform, false);
            selectAvatarGO.layer = LayerMask.NameToLayer("UI");
            
            var selectAvatarRect = selectAvatarGO.GetComponent<RectTransform>();
            // Position like MainDemo 15.unity: right side with proper anchors
            selectAvatarRect.anchorMin = new Vector2(0.52f, 0.6f);
            selectAvatarRect.anchorMax = new Vector2(0.95f, 0.95f);
            selectAvatarRect.offsetMin = Vector2.zero;
            selectAvatarRect.offsetMax = Vector2.zero;
            
            // Create Buttons container
            GameObject buttonsGO = new GameObject("Buttons", typeof(RectTransform));
            buttonsGO.transform.SetParent(selectAvatarGO.transform, false);
            buttonsGO.layer = LayerMask.NameToLayer("UI");
            
            var buttonsRect = buttonsGO.GetComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0f, 0f);
            buttonsRect.anchorMax = new Vector2(1f, 0.4f);
            buttonsRect.offsetMin = Vector2.zero;
            buttonsRect.offsetMax = Vector2.zero;
            
            // Create Images container
            GameObject imagesGO = new GameObject("Images", typeof(RectTransform));
            imagesGO.transform.SetParent(selectAvatarGO.transform, false);
            imagesGO.layer = LayerMask.NameToLayer("UI");
            
            var imagesRect = imagesGO.GetComponent<RectTransform>();
            imagesRect.anchorMin = new Vector2(0f, 0.6f);
            imagesRect.anchorMax = new Vector2(1f, 1f);
            imagesRect.offsetMin = Vector2.zero;
            imagesRect.offsetMax = Vector2.zero;
            
            // Create Description Text
            GameObject descriptionGO = new GameObject("Description_Text (TMP)", typeof(RectTransform));
            descriptionGO.transform.SetParent(selectAvatarGO.transform, false);
            descriptionGO.layer = LayerMask.NameToLayer("UI");
            
            var descriptionRect = descriptionGO.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0f, 0.50f); // weiter oben
            descriptionRect.anchorMax = new Vector2(1f, 0.75f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;
            
            // Add TextMeshProUGUI component
            var descriptionText = descriptionGO.AddComponent<TMPro.TextMeshProUGUI>();
            descriptionText.text = "Select Avatar";
            descriptionText.fontSize = 8f; // Verkleinert für kompaktes Design
            descriptionText.alignment = TMPro.TextAlignmentOptions.Center;
            descriptionText.color = Color.white;
            descriptionText.material = null;
            descriptionText.raycastTarget = true;
            
            // Create Avatar Buttons with original names and positioning
            // Standard-Avatare
            List<string> avatarNames = new List<string> { "Robert Button", "Leonard Button", "RPM Button" };
            List<string> avatarImageNames = new List<string> { "Robert_Raw_Image", "Leonard_Raw_Image", "RPM_Raw_Image" };
            List<string> imageResourcePaths = new List<string> { "Robert", "Leonard", "RPM" };
            List<string> avatarGameObjectNames = new List<string> { "Robert", "Leonard", "682cd77aff222706b8291007" };
            List<Vector2> buttonPositions = new List<Vector2> {
                new Vector2(-65f, -20.9f),
                new Vector2(0f, -20.9f),
                new Vector2(65f, -20.9f)
            };
            List<Vector2> imagePositions = new List<Vector2> {
                new Vector2(-65f, -40.9f),
                new Vector2(0f, -40.9f),
                new Vector2(65f, -40.9f)
            };

            // Prüfe, ob ein Custom Avatar geladen ist
            var customAvatarGO = AvatarManager.Instance.GetAvatar("CustomAvatar");
            if (customAvatarGO != null)
            {
                // Füge Custom Avatar als vierten Button in die bestehende Reihe ein
                avatarNames.Add("Custom Avatar");
                avatarImageNames.Add("Custom_Raw_Image");
                imageResourcePaths.Add("Custom"); // Verwende Custom.png als Bild
                avatarGameObjectNames.Add("CustomAvatar");
                // Platziere alle vier Buttons gleichmäßig nebeneinander
                float spacing = 65f;
                buttonPositions.Clear();
                imagePositions.Clear();
                int count = avatarNames.Count;
                float startX = -spacing * (count - 1) / 2f;
                for (int i = 0; i < count; i++)
                {
                    buttonPositions.Add(new Vector2(startX + i * spacing, -20.9f));
                    imagePositions.Add(new Vector2(startX + i * spacing, -40.9f));
                }
            }

            for (int i = 0; i < avatarNames.Count; i++)
            {
                GameObject buttonGO = new GameObject(avatarNames[i], typeof(RectTransform));
                buttonGO.transform.SetParent(buttonsGO.transform, false);
                buttonGO.layer = LayerMask.NameToLayer("UI");

                var buttonRect = buttonGO.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.sizeDelta = new Vector2(52.74f, 11.21f);
                buttonRect.anchoredPosition = buttonPositions[i];

                var buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = Color.white;
                buttonImage.raycastTarget = true;
                buttonImage.maskable = true;
                buttonImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
                buttonImage.type = Image.Type.Sliced;
                buttonImage.fillCenter = true;

                var button = buttonGO.AddComponent<Button>();
                button.targetGraphic = buttonImage;
                button.interactable = true;

                var colors = new ColorBlock();
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
                colors.pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
                colors.selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
                colors.disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                button.colors = colors;

                button.transition = Selectable.Transition.ColorTint;

                int avatarIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    // Alle Avatare (inkl. Custom) durchgehen und nur den gewählten aktivieren
                    for (int j = 0; j < avatarGameObjectNames.Count; j++)
                    {
                        var go = AvatarManager.Instance.GetAvatar(avatarGameObjectNames[j]);
                        if (go != null)
                            go.SetActive(j == avatarIndex);
                    }
                    // UI-Feedback
                    if (descriptionText != null)
                        descriptionText.text = $"{avatarNames[avatarIndex].Replace(" Button", "")} Selected";
                    // Logging
                    UnityEngine.Debug.Log($"[Avatar] {avatarNames[avatarIndex]} selected - only this avatar is active");
                });

                // Persistent Calls für Editor (optional, wie bisher)
                SetupPersistentCallsWhenReady(button, avatarIndex, avatarNames[i], avatarGameObjectNames.ToArray());

                GameObject buttonTextGO = new GameObject("Text", typeof(RectTransform));
                buttonTextGO.transform.SetParent(buttonGO.transform, false);
                buttonTextGO.layer = LayerMask.NameToLayer("UI");

                var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
                buttonTextRect.anchorMin = Vector2.zero;
                buttonTextRect.anchorMax = Vector2.one;
                buttonTextRect.sizeDelta = Vector2.zero;
                buttonTextRect.anchoredPosition = Vector2.zero;

                var buttonText = buttonTextGO.AddComponent<TMPro.TextMeshProUGUI>();
                buttonText.text = avatarNames[i].Replace(" Button", "");
                buttonText.fontSize = 8f;
                buttonText.alignment = TMPro.TextAlignmentOptions.Center;
                buttonText.color = Color.black;
                buttonText.raycastTarget = false;

                UnityEngine.Debug.Log($"[UI] Created avatar button: {avatarNames[i]} at position {buttonPositions[i]}");
            }
            
            // Create Raw Images with actual textures from Assets/Images
            for (int i = 0; i < avatarImageNames.Count; i++)
            {
                GameObject imageGO = new GameObject(avatarImageNames[i], typeof(RectTransform));
                imageGO.transform.SetParent(imagesGO.transform, false);
                imageGO.layer = LayerMask.NameToLayer("UI");
                
                var imageRect = imageGO.GetComponent<RectTransform>();
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.sizeDelta = new Vector2(100f, 100f);
                imageRect.anchoredPosition = imagePositions[i];
                imageRect.localScale = new Vector3(0.37f, 0.37f, 0.37f); // Original scale
                
                // Add RawImage component (matching original)
                var rawImage = imageGO.AddComponent<UnityEngine.UI.RawImage>();
                rawImage.color = Color.white;
                rawImage.raycastTarget = true;
                rawImage.maskable = true;
                
                // Load the actual texture from Assets/Images
                Texture2D texture = null;
                
                #if UNITY_EDITOR
                // Try to load from Assets/Images using AssetDatabase (Editor only)
                string[] possiblePaths = {
                    $"Assets/Images/{imageResourcePaths[i]}.png",
                    $"Assets/Images/{imageResourcePaths[i]}.PNG",
                    $"Assets/Images/{imageResourcePaths[i]}.jpg",
                    $"Assets/Images/{imageResourcePaths[i]}.JPG"
                };
                
                foreach (string path in possiblePaths)
                {
                    texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null)
                    {
                        UnityEngine.Debug.Log($"[UI] Loaded texture for {avatarImageNames[i]} from: {path}");
                        break;
                    }
                }
                #endif
                
                // Fallback: Try Resources.Load
                if (texture == null)
                {
                    texture = Resources.Load<Texture2D>($"Images/{imageResourcePaths[i]}");
                    if (texture == null)
                    {
                        texture = Resources.Load<Texture2D>(imageResourcePaths[i]);
                    }
                }
                
                if (texture != null)
                {
                    rawImage.texture = texture;
                    UnityEngine.Debug.Log($"[UI] Successfully loaded texture for {avatarImageNames[i]}: {imageResourcePaths[i]}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[UI] Could not load texture for {avatarImageNames[i]}: {imageResourcePaths[i]}");
                    // Create fallback colored texture
                    texture = new Texture2D(64, 64);
                    Color[] colors = new Color[64 * 64];
                    Color avatarColor = i == 0 ? Color.red : i == 1 ? Color.green : Color.blue;
                    for (int j = 0; j < colors.Length; j++)
                    {
                        colors[j] = avatarColor;
                    }
                    texture.SetPixels(colors);
                    texture.Apply();
                    rawImage.texture = texture;
                }
                
                UnityEngine.Debug.Log($"[UI] Created avatar image: {avatarImageNames[i]} at position {imagePositions[i]}");
            }
            
            log("✅ Created Select Avatar UI with proper button and image configuration.");
        }
        
        private void SetupPersistentCallsWhenReady(UnityEngine.UI.Button button, int avatarIndex, string avatarName, string[] avatarGameObjectNames)
        {
            if (AvatarManager.Instance.AreAvatarsLoaded())
            {
                // Avatars already loaded, setup immediately
                CreatePersistentCalls(button, avatarIndex, avatarName, avatarGameObjectNames);
            }
            else
            {
                // Wait for avatars to be loaded
                AvatarManager.Instance.OnAvatarsLoaded += (avatars) => {
                    CreatePersistentCalls(button, avatarIndex, avatarName, avatarGameObjectNames);
                };
            }
        }

        private void CreatePersistentCalls(UnityEngine.UI.Button button, int avatarIndex, string avatarName, string[] avatarGameObjectNames)
        {
            #if UNITY_EDITOR
            try 
            {
                var serializedObject = new UnityEditor.SerializedObject(button);
                var onClickProperty = serializedObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                
                onClickProperty.arraySize = 0;
                
                for (int j = 0; j < avatarGameObjectNames.Length; j++)
                {
                    // Use AvatarManager to get avatar reference
                    GameObject targetObj = AvatarManager.Instance.GetAvatar(avatarGameObjectNames[j]);
                    if (targetObj != null)
                    {
                        onClickProperty.arraySize++;
                        var callProperty = onClickProperty.GetArrayElementAtIndex(onClickProperty.arraySize - 1);
                        
                        callProperty.FindPropertyRelative("m_Target").objectReferenceValue = targetObj;
                        callProperty.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "UnityEngine.GameObject, UnityEngine";
                        callProperty.FindPropertyRelative("m_MethodName").stringValue = "SetActive";
                        callProperty.FindPropertyRelative("m_Mode").intValue = 6; // Bool mode
                        callProperty.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = (j == avatarIndex);
                        callProperty.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly
                        
                        UnityEngine.Debug.Log($"[UI] Added persistent call for {avatarName} -> {avatarGameObjectNames[j]} (SetActive: {j == avatarIndex})");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[UI] Avatar GameObject not found: {avatarGameObjectNames[j]}");
                    }
                }
                
                serializedObject.ApplyModifiedProperties();
                UnityEngine.Debug.Log($"[UI] Successfully created {onClickProperty.arraySize} persistent calls for button: {avatarName}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[UI] Failed to create persistent calls for {avatarName}: {ex.Message}");
            }
            #endif
        }
        
        private void OnAvatarButtonClicked(int avatarIndex, string avatarName, string[] avatarGameObjectNames, TMPro.TextMeshProUGUI descriptionText)
        {
            UnityEngine.Debug.Log($"[Avatar] Button clicked: {avatarName} (Index: {avatarIndex})");
            
            // Update description text
            if (descriptionText != null)
            {
                descriptionText.text = $"{avatarName.Replace(" Button", "")} Selected";
            }
            
            // Use AvatarManager to get avatar references
            for (int i = 0; i < avatarGameObjectNames.Length; i++)
            {
                string avatarObjName = avatarGameObjectNames[i];
                GameObject avatarObj = AvatarManager.Instance.GetAvatar(avatarObjName);
                
                if (avatarObj != null)
                {
                    if (i == avatarIndex)
                    {
                        // Activate the selected avatar
                        avatarObj.SetActive(true);
                        UnityEngine.Debug.Log($"[Avatar] Activated: {avatarObjName}");
                    }
                    else
                    {
                        // Deactivate the other avatars
                        avatarObj.SetActive(false);
                        UnityEngine.Debug.Log($"[Avatar] Deactivated: {avatarObjName}");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[Avatar] Could not find avatar GameObject: {avatarObjName}");
                }
            }
            
            // Log the selection
            switch (avatarIndex)
            {
                case 0: // Robert Button
                    UnityEngine.Debug.Log("[Avatar] Robert avatar selected - Robert activated, Leonard and RPM deactivated");
                    break;
                case 1: // Leonard Button
                    UnityEngine.Debug.Log("[Avatar] Leonard avatar selected - Leonard activated, Robert and RPM deactivated");
                    break;
                case 2: // RPM Button
                    UnityEngine.Debug.Log("[Avatar] RPM avatar selected - RPM activated, Robert and Leonard deactivated");
                    break;
            }
        }
        
        private void OnAvatarSelected(int avatarIndex, string avatarName, TMPro.TextMeshProUGUI descriptionText, GameObject buttonsContainer, Color[] avatarColors)
        {
            UnityEngine.Debug.Log($"[Avatar] Selected: {avatarName}");
            
            // Update description text
            if (descriptionText != null)
            {
                descriptionText.text = $"{avatarName} Selected";
            }
            
            // Update button colors to show selection
            for (int i = 0; i < buttonsContainer.transform.childCount; i++)
            {
                var childButton = buttonsContainer.transform.GetChild(i).GetComponent<Button>();
                if (childButton != null)
                {
                    var buttonImage = childButton.GetComponent<Image>();
                    if (i == avatarIndex)
                    {
                        buttonImage.color = avatarColors[i]; // Selected color
                    }
                    else
                    {
                        buttonImage.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Default color
                    }
                }
            }
            
            // TODO: Add your avatar selection logic here
            // For example: NPCManager.Instance.ChangeAvatar(avatarIndex);
        }

        private void CreateVolumeSlider()
        {
            GameObject sliderGO = new GameObject("Volume Slider", typeof(RectTransform));
            sliderGO.transform.SetParent(panel.transform, false);
            var rect = sliderGO.GetComponent<RectTransform>();
            // Weiter nach oben verschoben
            rect.anchorMin = new Vector2(0.46f, 0.21f); // Y erhöht
            rect.anchorMax = new Vector2(0.9f, 0.23f);
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
            handleRect.sizeDelta = new Vector2(14, 18); // kleinerer Handle
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

            // Background - smaller checkbox to match voice checkboxes
            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(toggleGO.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.2f);
            bgRect.anchorMax = new Vector2(0.10f, 0.8f); // Reduced to match voice checkboxes (0.10f)
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

            // Label - adjusted position to match smaller checkbox
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(toggleGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.12f, 0); // Adjusted from 1.05f to 0.12f to start right after checkbox
            labelRect.anchorMax = new Vector2(2f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Enable VAD";
            label.fontSize = 8;
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
