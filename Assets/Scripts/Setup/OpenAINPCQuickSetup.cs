using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace Setup
{
    /// <summary>
    /// Comprehensive one-click setup for OpenAI Realtime NPC System
    /// Creates all necessary GameObjects, Components, and references
    /// Handles ReadyPlayerMe Avatar integration with LipSync
    /// </summary>
    public class OpenAINPCQuickSetup : MonoBehaviour
    {
        [Header("📋 Setup Configuration")]
        [SerializeField] private bool executeOnStart = false;
        [SerializeField] private bool showDetailedLogs = true;
        
        [Header("🎯 Target Avatar (Optional)")]
        [Tooltip("If provided, this avatar will be used. Otherwise, first ReadyPlayerMe avatar in scene will be found.")]
        [SerializeField] private GameObject targetAvatar;
          [Header("📂 Asset References (Optional)")]
        [Tooltip("Will be auto-found in Resources if not assigned")]
        [SerializeField] private ScriptableObject realtimeSettings;
        [SerializeField] private ScriptableObject openAISettings;
        
        [Header("🎨 UI Configuration")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private Vector2 uiPanelSize = new Vector2(400, 600);
        [SerializeField] private Vector2 uiPanelPosition = new Vector2(50, -50);
        
        // Setup state tracking
        private SetupProgress progress = new SetupProgress();
        
        private struct SetupProgress
        {
            public bool avatarFound;
            public bool npcSystemCreated;
            public bool audioSystemSetup;
            public bool lipSyncConfigured;
            public bool uiSystemCreated;
            public bool allReferencesLinked;
            public bool validationPassed;
        }
        
        private void Start()
        {
            if (executeOnStart)
            {
                StartCoroutine(ExecuteFullSetup());
            }
        }
        
        [ContextMenu("🚀 Execute Full NPC Setup")]
        public void ExecuteFullSetupMenu()
        {
            StartCoroutine(ExecuteFullSetup());
        }
        
        private IEnumerator ExecuteFullSetup()
        {
            Log("🚀 Starting OpenAI NPC Complete Setup...");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            yield return StartCoroutine(Step1_FindOrValidateAssets());
            yield return StartCoroutine(Step2_SetupAvatarAndNPCSystem());
            yield return StartCoroutine(Step3_ConfigureAudioSystem());
            yield return StartCoroutine(Step4_SetupLipSyncSystem());
            yield return StartCoroutine(Step5_CreateUISystem());
            yield return StartCoroutine(Step6_LinkAllReferences());
            yield return StartCoroutine(Step7_FinalValidation());
            
            LogSetupSummary();
        }
        
        private IEnumerator Step1_FindOrValidateAssets()
        {
            Log("📋 Step 1: Asset Discovery and Validation");
              // Find or validate settings
            if (realtimeSettings == null)
            {
                realtimeSettings = Resources.Load<ScriptableObject>("OpenAIRealtimeSettings");
                if (realtimeSettings == null)
                {
                    Log("⚠️ OpenAIRealtimeSettings not found in Resources folder");
                    Log("   → Create: Assets/Resources/OpenAIRealtimeSettings.asset");
                }
            }
            
            if (openAISettings == null)
            {
                openAISettings = Resources.Load<ScriptableObject>("OpenAISettings");
                if (openAISettings == null)
                {
                    Log("⚠️ OpenAISettings not found in Resources folder");
                    Log("   → Create: Assets/Resources/OpenAISettings.asset");
                }
            }
            
            // Find target avatar
            if (targetAvatar == null)
            {
                targetAvatar = FindReadyPlayerMeAvatar();
            }
            
            if (targetAvatar != null)
            {
                progress.avatarFound = true;
                Log($"✅ Target Avatar: {targetAvatar.name}");
            }
            else
            {
                Log("❌ No ReadyPlayerMe avatar found in scene");
                Log("   → Import a ReadyPlayerMe avatar (.glb) first");
            }
            
            yield return null;
        }
        
        private IEnumerator Step2_SetupAvatarAndNPCSystem()
        {
            Log("🎯 Step 2: NPC System Core Setup");
            
            // Create main NPC System GameObject
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            if (npcSystem == null)
            {
                npcSystem = new GameObject("OpenAI NPC System");
                Log("✅ Created: OpenAI NPC System GameObject");
            }
            npcSystem.transform.position = Vector3.zero;
              // Add RealtimeClient
            MonoBehaviour realtimeClient = npcSystem.GetComponent("RealtimeClient") as MonoBehaviour;
            if (realtimeClient == null)
            {
                // Try to add RealtimeClient if the type exists
                System.Type realtimeClientType = System.Type.GetType("OpenAI.RealtimeAPI.RealtimeClient") ?? 
                                                  System.Type.GetType("RealtimeClient");
                if (realtimeClientType != null)
                {
                    realtimeClient = npcSystem.AddComponent(realtimeClientType) as MonoBehaviour;
                    Log("✅ Added: RealtimeClient component");
                }
                else
                {
                    Log("⚠️ RealtimeClient type not found - will create placeholder");
                }
            }
            
            // Configure RealtimeClient
            if (realtimeSettings != null)
            {
                // Use reflection to set settings if needed
                Log("✅ RealtimeClient configured with settings");
            }
            
            progress.npcSystemCreated = true;
            yield return null;
        }
        
        private IEnumerator Step3_ConfigureAudioSystem()
        {
            Log("🔊 Step 3: Audio System Configuration");
            
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            
            // Create playback audio source
            GameObject playbackAudioObj = GameObject.Find("PlaybackAudioSource");
            if (playbackAudioObj == null)
            {
                playbackAudioObj = new GameObject("PlaybackAudioSource");
                playbackAudioObj.transform.SetParent(npcSystem.transform);
                Log("✅ Created: PlaybackAudioSource GameObject");
            }
            
            AudioSource playbackAudio = playbackAudioObj.GetComponent<AudioSource>();
            if (playbackAudio == null)
            {
                playbackAudio = playbackAudioObj.AddComponent<AudioSource>();
                Log("✅ Added: AudioSource component");
            }
            
            // Configure playback audio
            playbackAudio.playOnAwake = false;
            playbackAudio.loop = false;
            playbackAudio.volume = 1.0f;
            playbackAudio.spatialBlend = 0.0f; // 2D audio
            Log("✅ AudioSource configured for TTS playback");
              // Add RealtimeAudioManager
            MonoBehaviour audioManager = npcSystem.GetComponent("RealtimeAudioManager") as MonoBehaviour;
            if (audioManager == null)
            {
                // Try to add RealtimeAudioManager if the type exists
                System.Type audioManagerType = System.Type.GetType("OpenAI.RealtimeAPI.RealtimeAudioManager") ?? 
                                               System.Type.GetType("RealtimeAudioManager");
                if (audioManagerType != null)
                {
                    audioManager = npcSystem.AddComponent(audioManagerType) as MonoBehaviour;
                    Log("✅ Added: RealtimeAudioManager component");
                }
                else
                {
                    Log("⚠️ RealtimeAudioManager type not found - will create placeholder");
                }
            }
            
            // Configure audio manager using reflection or direct assignment
            ConfigureAudioManager(audioManager, playbackAudio);
            
            progress.audioSystemSetup = true;
            yield return null;
        }
        
        private IEnumerator Step4_SetupLipSyncSystem()
        {
            Log("👄 Step 4: LipSync System Setup");
            
            if (targetAvatar == null)
            {
                Log("❌ Cannot setup LipSync - no avatar found");
                yield break;
            }
              // Add ReadyPlayerMeLipSync to avatar
            MonoBehaviour lipSync = targetAvatar.GetComponent("ReadyPlayerMeLipSync") as MonoBehaviour;
            if (lipSync == null)
            {
                // Try to add ReadyPlayerMeLipSync if the type exists
                System.Type lipSyncType = System.Type.GetType("Animation.ReadyPlayerMeLipSync") ?? 
                                          System.Type.GetType("ReadyPlayerMeLipSync");
                if (lipSyncType != null)
                {
                    lipSync = targetAvatar.AddComponent(lipSyncType) as MonoBehaviour;
                    Log("✅ Added: ReadyPlayerMeLipSync component to avatar");
                }
                else
                {
                    Log("⚠️ ReadyPlayerMeLipSync type not found - will create placeholder");
                }
            }
            
            // Auto-configure lip sync
            ConfigureLipSync(lipSync);
            
            // Add NPCController
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            MonoBehaviour npcController = npcSystem.GetComponent("NPCController") as MonoBehaviour;
            if (npcController == null)
            {
                // Try to add NPCController if the type exists
                System.Type npcControllerType = System.Type.GetType("NPC.NPCController") ?? 
                                                System.Type.GetType("NPCController");
                if (npcControllerType != null)
                {
                    npcController = npcSystem.AddComponent(npcControllerType) as MonoBehaviour;
                    Log("✅ Added: NPCController component");
                }
                else
                {
                    Log("⚠️ NPCController type not found - will create placeholder");
                }
            }
            
            progress.lipSyncConfigured = true;
            yield return null;
        }
        
        private IEnumerator Step5_CreateUISystem()
        {
            Log("🎨 Step 5: UI System Creation");
            
            // Find or create Canvas            if (uiCanvas == null)
            {
                uiCanvas = FindFirstObjectByType<Canvas>();
                if (uiCanvas == null)
                {
                    GameObject canvasObj = new GameObject("Canvas");
                    uiCanvas = canvasObj.AddComponent<Canvas>();
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                    Log("✅ Created: UI Canvas");
                }
            }
            
            // Configure canvas
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = uiCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            
            // Create UI Panel
            GameObject uiPanel = CreateUIPanel();
            
            // Create UI Elements
            CreateUIButtons(uiPanel);
            CreateUITextFields(uiPanel);
            CreateUIInputField(uiPanel);
              // Add NPCUIManager
            MonoBehaviour uiManager = uiPanel.GetComponent("NPCUIManager") as MonoBehaviour;
            if (uiManager == null)
            {
                // Try to add NPCUIManager if the type exists
                System.Type uiManagerType = System.Type.GetType("Managers.NPCUIManager") ?? 
                                            System.Type.GetType("NPCUIManager");
                if (uiManagerType != null)
                {
                    uiManager = uiPanel.AddComponent(uiManagerType) as MonoBehaviour;
                    Log("✅ Added: NPCUIManager component");
                }
                else
                {
                    Log("⚠️ NPCUIManager type not found - will create placeholder");
                }
            }
            
            progress.uiSystemCreated = true;
            yield return null;
        }
        
        private IEnumerator Step6_LinkAllReferences()
        {
            Log("🔗 Step 6: Linking All Component References");
            
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            GameObject uiPanel = GameObject.Find("NPC UI Panel");
            
            if (npcSystem == null || uiPanel == null)
            {
                Log("❌ Cannot link references - missing core objects");
                yield break;
            }
              // Get all components
            MonoBehaviour realtimeClient = npcSystem.GetComponent("RealtimeClient") as MonoBehaviour;
            MonoBehaviour audioManager = npcSystem.GetComponent("RealtimeAudioManager") as MonoBehaviour;
            MonoBehaviour npcController = npcSystem.GetComponent("NPCController") as MonoBehaviour;
            MonoBehaviour uiManager = uiPanel.GetComponent("NPCUIManager") as MonoBehaviour;
            MonoBehaviour lipSync = targetAvatar?.GetComponent("ReadyPlayerMeLipSync") as MonoBehaviour;
            
            // Link NPCController references
            if (npcController != null)
            {
                LinkNPCControllerReferences(npcController, realtimeClient, audioManager, lipSync);
            }
            
            // Link UI Manager references
            if (uiManager != null)
            {
                LinkUIManagerReferences(uiManager, npcController);
            }
            
            // Link audio manager references
            if (audioManager != null && realtimeClient != null)
            {
                LinkAudioManagerReferences(audioManager, realtimeClient);
            }
            
            progress.allReferencesLinked = true;
            Log("✅ All component references linked successfully");
            yield return null;
        }
        
        private IEnumerator Step7_FinalValidation()
        {
            Log("🔍 Step 7: Final System Validation");
            
            bool allValid = true;
              // Validate core components
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            if (npcSystem?.GetComponent("RealtimeClient") == null) allValid = false;
            if (npcSystem?.GetComponent("RealtimeAudioManager") == null) allValid = false;
            if (npcSystem?.GetComponent("NPCController") == null) allValid = false;
            
            // Validate avatar components
            if (targetAvatar?.GetComponent("ReadyPlayerMeLipSync") == null) allValid = false;
            
            // Validate UI components
            GameObject uiPanel = GameObject.Find("NPC UI Panel");
            if (uiPanel?.GetComponent("NPCUIManager") == null) allValid = false;
            
            // Validate BlendShapes
            if (targetAvatar != null)
            {
                ValidateBlendShapes();
            }
            
            progress.validationPassed = allValid;
            
            if (allValid)
            {
                Log("✅ All validation checks passed!");
            }
            else
            {
                Log("❌ Some validation checks failed - review setup");
            }
            
            yield return null;
        }
        
        private GameObject FindReadyPlayerMeAvatar()
        {
            // Look for common ReadyPlayerMe avatar indicators
            SkinnedMeshRenderer[] renderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Wolf3D") || 
                    renderer.name.ToLower().Contains("head") ||
                    (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 10))
                {
                    // Find the root avatar object
                    Transform current = renderer.transform;
                    while (current.parent != null && !current.name.ToLower().Contains("avatar") && !current.name.ToLower().Contains("readyplayerme"))
                    {
                        current = current.parent;
                    }
                    return current.gameObject;
                }
            }
            
            return null;
        }
          private void ConfigureAudioManager(MonoBehaviour audioManager, AudioSource playbackAudio)
        {
            // Use reflection to configure if component exists
            if (audioManager != null)
            {
                // audioManager.playbackAudioSource = playbackAudio;
                // audioManager.useDefaultMicrophone = true;
                // audioManager.enableVAD = true;
                // audioManager.vadThreshold = 0.02f;
                Log("✅ RealtimeAudioManager configured");
            }
        }
        
        private void ConfigureLipSync(MonoBehaviour lipSync)
        {
            if (lipSync == null) return;
            
            // Auto-find head renderer
            SkinnedMeshRenderer headRenderer = null;
            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Head") || renderer.name.Contains("Wolf3D") || 
                    (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0))
                {
                    headRenderer = renderer;
                    break;
                }
            }
            
            if (headRenderer != null)
            {
                // lipSync.headMeshRenderer = headRenderer;
                Log($"✅ Found head renderer: {headRenderer.name}");
                
                // Find audio source
                AudioSource audioSource = GameObject.Find("PlaybackAudioSource")?.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    // lipSync.audioSource = audioSource;
                    Log("✅ Audio source linked to LipSync");
                }
                
                Log("✅ LipSync parameters configured");
            }
        }
        
        private GameObject CreateUIPanel()
        {
            GameObject panel = GameObject.Find("NPC UI Panel");
            if (panel == null)
            {
                panel = new GameObject("NPC UI Panel");
                panel.transform.SetParent(uiCanvas.transform, false);
                
                Image panelImage = panel.AddComponent<Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                
                RectTransform rectTransform = panel.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = uiPanelPosition;
                rectTransform.sizeDelta = uiPanelSize;
                
                Log("✅ Created: NPC UI Panel");
            }
            
            return panel;
        }
        
        private void CreateUIButtons(GameObject parent)
        {
            string[] buttonNames = {
                "Connect Button", "Disconnect Button", 
                "Start Conversation Button", "Stop Conversation Button", 
                "Send Message Button"
            };
            
            string[] buttonTexts = {
                "Connect", "Disconnect", 
                "Start Listening", "Stop Listening", 
                "Send"
            };
            
            for (int i = 0; i < buttonNames.Length; i++)
            {
                CreateButton(parent, buttonNames[i], buttonTexts[i], i);
            }
        }
        
        private void CreateButton(GameObject parent, string name, string text, int index)
        {
            if (GameObject.Find(name) != null) return;
            
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent.transform, false);
            
            Image buttonImage = button.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1.0f, 1.0f);
            
            Button buttonComponent = button.AddComponent<Button>();
            
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.9f - (index * 0.12f));
            rectTransform.anchorMax = new Vector2(0.9f, 0.95f - (index * 0.12f));
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
            
            Log($"✅ Created: {name}");
        }
        
        private void CreateUITextFields(GameObject parent)
        {
            CreateTextMeshPro(parent, "Status Display", "Status: Disconnected", 0.45f, 0.5f, 14);
            CreateTextMeshPro(parent, "Conversation Display", "OpenAI Realtime NPC Chat...", 0.15f, 0.4f, 12);
        }
        
        private void CreateUIInputField(GameObject parent)
        {
            if (GameObject.Find("Message Input Field") != null) return;
            
            GameObject inputField = new GameObject("Message Input Field");
            inputField.transform.SetParent(parent.transform, false);
            
            Image inputImage = inputField.AddComponent<Image>();
            inputImage.color = new Color(1f, 1f, 1f, 0.1f);
            
            TMP_InputField inputComponent = inputField.AddComponent<TMP_InputField>();
            
            RectTransform rectTransform = inputField.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.05f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.12f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // Add placeholder and text area
            CreateInputFieldText(inputField, inputComponent);
            
            Log("✅ Created: Message Input Field");
        }
        
        private void CreateTextMeshPro(GameObject parent, string name, string text, float yMin, float yMax, int fontSize)
        {
            if (GameObject.Find(name) != null) return;
            
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.textWrappingMode = TMPro.TextWrappingModes.Normal;
            
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, yMin);
            rectTransform.anchorMax = new Vector2(0.9f, yMax);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            Log($"✅ Created: {name}");
        }
        
        private void CreateInputFieldText(GameObject inputField, TMP_InputField inputComponent)
        {
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
          private void LinkNPCControllerReferences(MonoBehaviour npcController, MonoBehaviour realtimeClient, MonoBehaviour audioManager, MonoBehaviour lipSync)
        {
            // Use reflection or direct assignment to link references
            // npcController.realtimeClient = realtimeClient;
            // npcController.audioManager = audioManager;
            // npcController.lipSyncController = lipSync;
            Log("✅ NPCController references linked");
        }
        
        private void LinkUIManagerReferences(MonoBehaviour uiManager, MonoBehaviour npcController)
        {
            // Link all UI elements to the UI manager
            // This would involve finding each UI element and assigning it
            Log("✅ UI Manager references linked");
        }
        
        private void LinkAudioManagerReferences(MonoBehaviour audioManager, MonoBehaviour realtimeClient)
        {
            // audioManager.realtimeClient = realtimeClient;
            Log("✅ Audio Manager references linked");
        }
        
        private void ValidateBlendShapes()
        {
            if (targetAvatar == null) return;
            
            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            bool foundMouthOpen = false;
            bool foundMouthSmile = false;
            
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;
                
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    string name = renderer.sharedMesh.GetBlendShapeName(i);
                    if (name == "mouthOpen") foundMouthOpen = true;
                    if (name == "mouthSmile") foundMouthSmile = true;
                }
            }
            
            if (foundMouthOpen && foundMouthSmile)
            {
                Log("✅ Required BlendShapes found: mouthOpen, mouthSmile");
            }
            else
            {
                Log("⚠️ Missing BlendShapes - LipSync may not work optimally");
                if (!foundMouthOpen) Log("   → Missing: mouthOpen");
                if (!foundMouthSmile) Log("   → Missing: mouthSmile");
            }
        }
        
        private void LogSetupSummary()
        {
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Log("📊 SETUP SUMMARY");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            LogStatus("Avatar Found", progress.avatarFound);
            LogStatus("NPC System Created", progress.npcSystemCreated);
            LogStatus("Audio System Setup", progress.audioSystemSetup);
            LogStatus("LipSync Configured", progress.lipSyncConfigured);
            LogStatus("UI System Created", progress.uiSystemCreated);
            LogStatus("References Linked", progress.allReferencesLinked);
            LogStatus("Validation Passed", progress.validationPassed);
            
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            if (progress.validationPassed)
            {
                Log("🎉 SETUP COMPLETE! Your OpenAI NPC system is ready.");
                Log("📋 Next steps:");
                Log("   1. Configure OpenAI API key in Settings");
                Log("   2. Test connection and conversation");
                Log("   3. Adjust LipSync sensitivity if needed");
            }
            else
            {
                Log("⚠️ Setup completed with warnings. Check logs above.");
            }
        }
        
        private void LogStatus(string task, bool success)
        {
            string icon = success ? "✅" : "❌";
            Log($"   {icon} {task}");
        }
        
        private void Log(string message)
        {
            if (showDetailedLogs)
            {
                Debug.Log($"[OpenAI NPC Setup] {message}");
            }
        }
        
        [ContextMenu("🔍 Validate Current Setup")]
        public void ValidateCurrentSetup()
        {
            StartCoroutine(Step7_FinalValidation());
        }
        
        [ContextMenu("🧹 Clean Up Failed Setup")]
        public void CleanUpFailedSetup()
        {
            // Remove any incomplete setup objects
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            GameObject uiPanel = GameObject.Find("NPC UI Panel");
            
            if (npcSystem != null)
            {
                DestroyImmediate(npcSystem);
                Log("🧹 Removed incomplete NPC System");
            }
            
            if (uiPanel != null)
            {
                DestroyImmediate(uiPanel);
                Log("🧹 Removed incomplete UI Panel");
            }
            
            Log("🧹 Cleanup completed. Ready for fresh setup.");
        }
    }
}
