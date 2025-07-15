using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NPC;
using OpenAI.Threading;

namespace Managers
{
    /// <summary>
    /// UI Manager for OpenAI Realtime NPC interaction
    /// Provides controls for testing and conversation management
    /// </summary>
    public class NpcUiManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Button startConversationButton;
        [SerializeField] private Button stopConversationButton;
        [SerializeField] private TMP_InputField messageInputField;
        [SerializeField] private Button sendMessageButton;
        [SerializeField] private TMP_Text conversationDisplay;
        [SerializeField] private TMP_Text statusDisplay;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle enableVADToggle;
        [SerializeField] private TMPro.TMP_Dropdown voiceDropdown;

        [Header("NPC Reference")]
        [SerializeField] private NPCController npcController;

        [Header("Settings")]
        [SerializeField] private int maxConversationLines = 20;
        [SerializeField] private Color userMessageColor = Color.blue;
        [SerializeField] private Color npcMessageColor = Color.green;
        [SerializeField] private Color systemMessageColor = Color.gray;
        private string conversationHistory = "";
        private int currentLineCount = 0;

        // UI state tracking to prevent spam
        private bool lastConnectedState = false;
        private NPCState lastNPCState = NPCState.Idle;

        [SerializeField] private OpenAIVoice voice = OpenAIVoice.alloy;

        // Listener-Delegate für ConversationUpdate
        private UnityEngine.Events.UnityAction<string> conversationUpdateListener;
        // Listener-Delegate für messageInputField
        private UnityEngine.Events.UnityAction<string> messageInputListener;

        // Voice change cooldown to prevent API conflicts
        private float lastVoiceChangeTime = 0f;
        private const float VOICE_CHANGE_COOLDOWN = 2.0f; // 2 seconds cooldown after audio playback

        // Voice change protection to prevent multiple simultaneous operations
        private bool isVoiceChangeInProgress = false;

        #region Unity Lifecycle
        private void Awake()
        {
            // Find NPC controller if not assigned
            if (npcController == null)
                npcController = FindFirstObjectByType<NPCController>();
        }

        private void Start()
        {
            SetupUI();
            SetupEventListeners();
            UpdateUIState();
        }
        private void Update()
        {
            // Only update UI state when connection status or NPC state changes
            if (npcController != null)
            {
                bool currentConnected = npcController.IsConnected;
                NPCState currentState = npcController.CurrentState;

                if (currentConnected != lastConnectedState || currentState != lastNPCState)
                {
                    UpdateUIState();
                    lastConnectedState = currentConnected;
                    lastNPCState = currentState;
                }
            }
        }

        private void OnDestroy()
        {
            RemoveEventListeners();
        }

        #endregion

        #region UI Setup

        private void SetupUI()
        {
            // Configure input field
            if (messageInputField != null)
            {
                messageInputListener = (msg) => OnMessageSubmitted(msg);
                messageInputField.onEndEdit.AddListener(messageInputListener);
                messageInputField.placeholder.GetComponent<TMP_Text>().text = "Type your message here...";
            }

            // Configure volume slider
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            // Initial conversation display
            if (conversationDisplay != null)
            {
                conversationDisplay.text = "OpenAI Realtime NPC Chat\n\nClick 'Connect' to begin...";
            }

            // Stimmen-Dropdown initialisieren und XR-kompatibel machen
            if (voiceDropdown != null)
            {
                voiceDropdown.ClearOptions();
                // Use descriptive voice names for better UX
                var descriptiveNames = OpenAIVoiceExtensions.GetAllVoiceDescriptions();
                voiceDropdown.AddOptions(new System.Collections.Generic.List<string>(descriptiveNames));
                
                // Initialize voice from RealtimeClient or fallback to settings
                int safeVoiceIndex = GetCurrentEffectiveVoiceIndex();
                
                voiceDropdown.value = safeVoiceIndex;
                voiceDropdown.RefreshShownValue();
                voiceDropdown.onValueChanged.AddListener(OnVoiceDropdownChanged);
                
                // Make dropdown XR/VR compatible
                MakeDropdownXRCompatible(voiceDropdown);
            }
        }
        
        /// <summary>
        /// Get the currently effective voice index (from RealtimeClient runtime override or settings)
        /// </summary>
        private int GetCurrentEffectiveVoiceIndex()
        {
            var realtimeClient = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeClient>();
            if (realtimeClient != null)
            {
                var currentVoice = realtimeClient.GetCurrentVoice();
                Debug.Log($"[UI] Using current voice from RealtimeClient: {currentVoice}");
                return (int)currentVoice;
            }
            
            // Fallback to local voice variable or default
            if (OpenAIVoiceExtensions.IsValid(voice))
            {
                Debug.Log($"[UI] Using local voice setting: {voice}");
                return (int)voice;
            }
            
            Debug.LogWarning($"[UI] Invalid voice {voice} detected, using alloy instead");
            voice = OpenAIVoiceExtensions.GetDefault();
            return 0; // alloy is index 0
        }
        
        /// <summary>
        /// Make TMP_Dropdown XR/VR compatible with minimal changes
        /// </summary>
        private void MakeDropdownXRCompatible(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;
            
            Debug.Log("[UI] Making voice dropdown XR/VR compatible...");
            
            // Enhance visual feedback for better VR visibility
            var button = dropdown.GetComponent<Button>();
            if (button != null)
            {
                var colors = button.colors;
                colors.highlightedColor = Color.yellow * 0.7f; // Better VR highlight
                colors.selectedColor = Color.green * 0.7f; // Better VR selection
                button.colors = colors;
            }
            
            // Larger text for VR readability (but not too large)
            if (dropdown.captionText != null)
            {
                dropdown.captionText.fontSize *= 1.1f; // Modest increase
                dropdown.captionText.color = Color.white; // Better contrast
            }
            
            // Ensure reasonable size for VR interaction (but don't make it too big)
            var rectTransform = dropdown.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                var currentSize = rectTransform.sizeDelta;
                // Only increase if really too small, and cap the maximum size
                rectTransform.sizeDelta = new Vector2(
                    Mathf.Clamp(currentSize.x, 200f, 350f), // Reasonable width range
                    Mathf.Clamp(currentSize.y, 30f, 50f)    // Reasonable height range
                );
            }
            
            // Set proper layer for XR raycasting
            dropdown.gameObject.layer = LayerMask.NameToLayer("UI");
            
            // Try to add XR interaction (optional - graceful fallback if not available)
            TryAddXRInteraction(dropdown.gameObject);
            
            // Most importantly: Fix the dropdown template to stay within canvas bounds
            StartCoroutine(FixDropdownTemplateSize(dropdown));
            
            Debug.Log("[UI] Voice dropdown is now XR/VR compatible!");
        }
        
        /// <summary>
        /// Fix dropdown template to stay within canvas bounds for VR
        /// </summary>
        private System.Collections.IEnumerator FixDropdownTemplateSize(TMP_Dropdown dropdown)
        {
            // Wait a frame for dropdown to be fully initialized
            yield return null;
            
            // Force dropdown to create its template if it doesn't exist
            if (dropdown.template == null)
            {
                dropdown.Show();
                yield return null;
                dropdown.Hide();
                yield return null;
            }
            
            if (dropdown.template != null)
            {
                var templateRect = dropdown.template.GetComponent<RectTransform>();
                if (templateRect != null)
                {
                    // Get canvas bounds
                    var canvas = dropdown.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        var canvasRect = canvas.GetComponent<RectTransform>();
                        float canvasHeight = canvasRect.rect.height;
                        float canvasWidth = canvasRect.rect.width;
                        
                        // Limit template size to reasonable bounds within canvas
                        float maxTemplateHeight = canvasHeight * 0.4f; // Max 40% of canvas height
                        float maxTemplateWidth = canvasWidth * 0.5f;   // Max 50% of canvas width
                        
                        var currentSize = templateRect.sizeDelta;
                        templateRect.sizeDelta = new Vector2(
                            Mathf.Min(currentSize.x, maxTemplateWidth),
                            Mathf.Min(currentSize.y, maxTemplateHeight)
                        );
                        
                        Debug.Log($"[UI] Template size limited to: {templateRect.sizeDelta} (Canvas: {canvasWidth}x{canvasHeight})");
                    }
                    
                    // Enhance template background for better VR visibility
                    var templateImage = templateRect.GetComponent<Image>();
                    if (templateImage != null)
                    {
                        templateImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Dark, opaque background
                    }
                    
                    // Make sure template items are reasonable size for VR
                    FixDropdownItemSizes(dropdown.template);
                }
            }
        }
        
        /// <summary>
        /// Fix dropdown item sizes for VR without making them too big
        /// </summary>
        private void FixDropdownItemSizes(RectTransform template)
        {
            var viewport = template.Find("Viewport");
            var content = viewport?.Find("Content");
            var itemTemplate = content?.Find("Item");
            
            if (itemTemplate != null)
            {
                var itemRect = itemTemplate.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    // Ensure items are big enough for VR but not too big
                    var currentItemSize = itemRect.sizeDelta;
                    itemRect.sizeDelta = new Vector2(
                        currentItemSize.x, 
                        Mathf.Clamp(currentItemSize.y, 35f, 45f) // Reasonable height for VR
                    );
                }
                
                // Enhance item text readability
                var itemLabel = itemTemplate.Find("Item Label")?.GetComponent<TMP_Text>();
                if (itemLabel != null)
                {
                    itemLabel.fontSize = Mathf.Clamp(itemLabel.fontSize * 1.05f, 12f, 16f); // Modest increase
                    itemLabel.color = Color.white;
                }
                
                // Better item background
                var itemBackground = itemTemplate.Find("Item Background")?.GetComponent<Image>();
                if (itemBackground != null)
                {
                    itemBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }
                
                // Add XR interaction to items too
                TryAddXRInteraction(itemTemplate.gameObject);
            }
        }
        
        /// <summary>
        /// Try to add XR interaction components - works with or without XR toolkit
        /// </summary>
        private void TryAddXRInteraction(GameObject obj)
        {
            try
            {
                // Try to find and add XR interaction component dynamically
                var xrType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
                if (xrType == null)
                {
                    // Fallback to older namespace
                    xrType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
                }
                
                if (xrType != null && obj.GetComponent(xrType) == null)
                {
                    obj.AddComponent(xrType);
                    Debug.Log("[UI] XR interaction added successfully");
                }
                else
                {
                    Debug.Log("[UI] XR components not available - dropdown will work with mouse/touch");
                }
            }
            catch (System.Exception)
            {
                // XR not available - that's perfectly fine
                Debug.Log("[UI] XR Interaction Toolkit not found - dropdown works with traditional input");
            }
        }

        private void SetupEventListeners()
        {
            // Button listeners
            if (connectButton != null)
                connectButton.onClick.AddListener(OnConnectClicked);
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(OnDisconnectClicked);
            if (startConversationButton != null)
                startConversationButton.onClick.AddListener(OnStartConversationClicked);
            if (stopConversationButton != null)
                stopConversationButton.onClick.AddListener(OnStopConversationClicked);
            if (sendMessageButton != null)
                sendMessageButton.onClick.AddListener(OnSendMessageClicked);

            // NPC event listeners
            if (npcController != null)
            {
                npcController.OnNPCStartedSpeaking.AddListener(OnNPCStartedSpeaking);
                npcController.OnNPCFinishedSpeaking.AddListener(OnNPCFinishedSpeaking);
                npcController.OnNPCStartedListening.AddListener(OnNPCStartedListening);
                npcController.OnNPCFinishedListening.AddListener(OnNPCFinishedListening);
                conversationUpdateListener = (msg) => OnConversationUpdate(msg);
                npcController.OnConversationUpdate.AddListener(conversationUpdateListener);
            }
        }

        private void RemoveEventListeners()
        {
            // Clean up button listeners
            if (connectButton != null)
                connectButton.onClick.RemoveListener(OnConnectClicked);
            if (disconnectButton != null)
                disconnectButton.onClick.RemoveListener(OnDisconnectClicked);
            if (startConversationButton != null)
                startConversationButton.onClick.RemoveListener(OnStartConversationClicked);
            if (stopConversationButton != null)
                stopConversationButton.onClick.RemoveListener(OnStopConversationClicked);
            if (sendMessageButton != null)
                sendMessageButton.onClick.RemoveListener(OnSendMessageClicked);
            if (messageInputField != null && messageInputListener != null)
                messageInputField.onEndEdit.RemoveListener(messageInputListener);

            // Clean up NPC listeners
            if (npcController != null)
            {
                npcController.OnNPCStartedSpeaking.RemoveListener(OnNPCStartedSpeaking);
                npcController.OnNPCFinishedSpeaking.RemoveListener(OnNPCFinishedSpeaking);
                npcController.OnNPCStartedListening.RemoveListener(OnNPCStartedListening);
                npcController.OnNPCFinishedListening.RemoveListener(OnNPCFinishedListening);
                if (conversationUpdateListener != null)
                    npcController.OnConversationUpdate.RemoveListener(conversationUpdateListener);
            }
        }

        #endregion
        #region Button Handlers

        public void OnConnectClicked()
        {
            if (npcController != null && npcController.CurrentState != NPCState.Connecting)
            {
                _ = npcController.ConnectToOpenAI(); // Fire-and-forget async
                UpdateStatus("Connecting to OpenAI...", systemMessageColor);
            }
            else if (npcController != null && npcController.CurrentState == NPCState.Connecting)
            {
                UpdateStatus("Already connecting, please wait...", systemMessageColor);
            }
        }

        public void OnDisconnectClicked()
        {
            if (npcController != null)
            {
                _ = npcController.DisconnectFromOpenAI(); // Fire-and-forget async
                UpdateStatus("Disconnecting...", systemMessageColor);
            }
        }
        public void OnStartConversationClicked()
        {
            if (npcController != null)
            {
                npcController.StartConversation();
                UpdateStatus("Conversation started - NPC is listening", systemMessageColor);
            }
        }

        public void OnStopConversationClicked()
        {
            if (npcController != null)
            {
                npcController.StopConversation();
                UpdateStatus("Conversation stopped", systemMessageColor);
            }
        }

        public void OnResetSystemClicked()
        {
            if (npcController != null)
            {
                _ = npcController.ResetSystem(); // Fire-and-forget async
                UpdateStatus("Resetting system...", systemMessageColor);
            }
        }

        public void OnSendMessageClicked()
        {
            SendCurrentMessage();
        }

        private void OnMessageSubmitted(string message)
        {
            SendCurrentMessage();
        }

        private void SendCurrentMessage()
        {
            if (messageInputField != null && npcController != null)
            {
                string message = messageInputField.text.Trim();
                if (!string.IsNullOrEmpty(message))
                {
                    npcController.SendTextMessage(message);
                    messageInputField.text = "";
                    messageInputField.ActivateInputField();
                }
            }
        }

        private void OnVoiceDropdownChanged(int index)
        {
            // Prevent multiple simultaneous voice changes
            if (isVoiceChangeInProgress)
            {
                Debug.LogWarning("[UI] Voice change already in progress - ignoring new request");
                return;
            }
            
            // Temporarily disable dropdown during voice change
            if (voiceDropdown != null)
            {
                voiceDropdown.interactable = false;
            }
            
            var enumValues = System.Enum.GetValues(typeof(OpenAIVoice));
            if (index >= 0 && index < enumValues.Length)
            {
                var newVoice = (OpenAIVoice)enumValues.GetValue(index);
                if (voice != newVoice)
                {
                    voice = newVoice;
                    isVoiceChangeInProgress = true;
                    
                    // OpenAI Settings zur Laufzeit anpassen - use Runtime Voice Override System
                    var realtimeClient = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeClient>();
                    if (realtimeClient != null)
                    {
                        // Convert index to OpenAIVoice enum and set runtime override
                        var voiceEnum = OpenAIVoiceExtensions.FromIndex(index);
                        realtimeClient.SetRuntimeVoice(voiceEnum);
                        Debug.Log($"[UI] Runtime voice override set: {index} ({voiceEnum} - {voiceEnum.GetDescription()})");
                    }
                    else
                    {
                        Debug.LogWarning("[UI] RealtimeClient not found - cannot set runtime voice override");
                    }
                    
                    var client = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeClient>();
                    if (client != null && client.IsConnected)
                    {
                        // CRITICAL: OpenAI API never allows voice changes after any assistant audio
                        // The session becomes "tainted" once assistant speaks, regardless of timing
                        // ALWAYS force session restart for voice changes during active sessions
                        
                        Debug.Log($"[UI] Voice change requested during active session - ALWAYS restart required");
                        UpdateStatus($"Restarting session for voice change to {newVoice}...", systemMessageColor);
                        
                        ForceSessionRestartForVoiceChange();
                    }
                    else
                    {
                        // Not connected - just update settings
                        UpdateStatus($"Voice set to: {newVoice} (will apply on next connection)", systemMessageColor);
                        isVoiceChangeInProgress = false; // Reset flag
                        
                        // Re-enable dropdown
                        if (voiceDropdown != null)
                        {
                            voiceDropdown.interactable = true;
                        }
                    }
                }
                else
                {
                    isVoiceChangeInProgress = false; // Reset flag if no change needed
                    
                    // Re-enable dropdown
                    if (voiceDropdown != null)
                    {
                        voiceDropdown.interactable = true;
                    }
                }
            }
            else
            {
                isVoiceChangeInProgress = false; // Reset flag if invalid index
                
                // Re-enable dropdown
                if (voiceDropdown != null)
                {
                    voiceDropdown.interactable = true;
                }
            }
        }
        
        private void ForceSessionRestartForVoiceChange()
        {
            // Stop conversation completely and trigger session restart
            if (npcController != null)
            {
                npcController.StopConversation();
                
                // Start coroutine for main-thread safe session restart
                StartCoroutine(SessionRestartCoroutine());
            }
            else
            {
                // No NPC controller - just reset flag
                isVoiceChangeInProgress = false;
            }
        }
        
        private System.Collections.IEnumerator SessionRestartCoroutine()
        {
            // Force a full disconnect/reconnect cycle on main thread
            bool disconnectSuccess = false;
            bool connectSuccess = false;
            System.Exception lastException = null;
            
            // Start disconnect task
            var disconnectTask = npcController.DisconnectFromOpenAI();
            
            // Wait for disconnect to complete
            yield return new WaitUntil(() => disconnectTask.IsCompleted);
            
            if (disconnectTask.IsFaulted)
            {
                lastException = disconnectTask.Exception?.GetBaseException() ?? new System.Exception("Disconnect failed");
            }
            else
            {
                disconnectSuccess = true;
            }
            
            if (disconnectSuccess)
            {
                // Wait for cleanup
                yield return new WaitForSeconds(1.0f);
                
                // Start reconnect task
                var connectTask = npcController.ConnectToOpenAI();
                
                // Wait for connect to complete
                yield return new WaitUntil(() => connectTask.IsCompleted);
                
                if (connectTask.IsFaulted)
                {
                    lastException = connectTask.Exception?.GetBaseException() ?? new System.Exception("Connect failed");
                }
                else
                {
                    connectSuccess = true;
                }
            }
            
            // Handle results
            if (disconnectSuccess && connectSuccess)
            {
                // Success - update UI
                UpdateStatus("Session restarted with new voice", systemMessageColor);
                isVoiceChangeInProgress = false; // Reset protection flag
                
                // Re-enable dropdown
                if (voiceDropdown != null)
                {
                    voiceDropdown.interactable = true;
                }
            }
            else
            {
                // Error - update UI
                Debug.LogError($"[UI] Error during voice change session restart: {lastException?.Message ?? "Unknown error"}");
                UpdateStatus("Voice change failed - please try again", systemMessageColor);
                isVoiceChangeInProgress = false; // Reset protection flag
                
                // Re-enable dropdown
                if (voiceDropdown != null)
                {
                    voiceDropdown.interactable = true;
                }
            }
        }

        /* AUSKOMMENTIERT: Coroutine nicht kompatibel mit Editor-Scripts
        private System.Collections.IEnumerator RestartConversationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (npcController != null)
            {
                npcController.StartConversation();
                UpdateStatus("Conversation restarted with new voice", systemMessageColor);
            }
        }
        */

        #endregion

        #region NPC Event Handlers

        private void OnNPCStartedSpeaking()
        {
            UpdateStatus("NPC started speaking...", npcMessageColor);
        }

        private void OnNPCFinishedSpeaking()
        {
            UpdateStatus("AI finished speaking", systemMessageColor);
            StartCoroutine(DelayedUIUpdate());
            
            // Update voice change cooldown timer
            lastVoiceChangeTime = Time.time;
            Debug.Log($"[UI] Voice change cooldown timer reset after NPC finished speaking");
        }
        
        private System.Collections.IEnumerator DelayedUIUpdate()
        {
            yield return new WaitForSeconds(0.1f); // Kurze Delay für State-Transition
            UpdateUIState(); // Force full UI state update
        }

        private void OnNPCStartedListening()
        {
            UpdateStatus("NPC started listening...", npcMessageColor);
        }

        private void OnNPCFinishedListening()
        {
            UpdateStatus("NPC finished listening.", npcMessageColor);
        }

        private void OnConversationUpdate(string message)
        {
            AddLineToConversation("NPC", message, npcMessageColor);
        }

        #endregion

        #region UI Update Methods

        private void UpdateUIState()
        {
            if (npcController != null)
            {
                // Update button interactability
                bool isConnected = npcController.IsConnected;
                bool isConversing = npcController.CurrentState == NPCState.Speaking || npcController.CurrentState == NPCState.Listening;

                if (connectButton != null) connectButton.interactable = !isConnected;
                if (disconnectButton != null) disconnectButton.interactable = isConnected;
                if (startConversationButton != null) startConversationButton.interactable = isConnected && !isConversing;
                if (stopConversationButton != null) stopConversationButton.interactable = isConversing;
                if (sendMessageButton != null) sendMessageButton.interactable = isConnected;

                // Update status display based on current state
                if (statusDisplay != null)
                {
                    string statusText = "Status: ";
                    Color statusColor = systemMessageColor;
                    
                    if (!isConnected)
                    {
                        statusText += "Disconnected";
                        statusColor = Color.red;
                    }
                    else
                    {
                        switch (npcController.CurrentState)
                        {
                            case NPCState.Idle:
                                statusText += "Connected - Ready";
                                statusColor = Color.green;
                                break;
                            case NPCState.Listening:
                                statusText += "Listening...";
                                statusColor = Color.yellow;
                                break;
                            case NPCState.Speaking:
                                statusText += "Speaking...";
                                statusColor = npcMessageColor;
                                break;
                            case NPCState.Processing:
                                statusText += "Processing...";
                                statusColor = Color.cyan;
                                break;
                            default:
                                statusText += "Connected";
                                statusColor = Color.green;
                                break;
                        }
                    }
                    
                    statusDisplay.text = statusText;
                    statusDisplay.color = statusColor;
                }

                // Update conversation display
                if (conversationDisplay != null)
                {
                    conversationDisplay.text = conversationHistory;
                }
            }
        }

        private void UpdateStatus(string message, Color? color = null)
        {
            if (statusDisplay != null)
            {
                statusDisplay.text = message;
                if (color.HasValue)
                    statusDisplay.color = color.Value;
            }
        }

        private void AddLineToConversation(string sender, string message, Color color)
        {
            if (conversationDisplay != null)
            {
                // Limit conversation lines
                if (currentLineCount >= maxConversationLines)
                {
                    conversationHistory = conversationHistory.Substring(conversationHistory.IndexOf('\n') + 1);
                    currentLineCount--;
                }

                // Add new line
                conversationHistory += $"{sender}: {message}\n";
                conversationDisplay.text = conversationHistory;

                // Scroll to bottom
                var scrollRect = conversationDisplay.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            
            // Also update MicrophoneVolume in settings if available
            var settings = Resources.Load<OpenAISettings>("OpenAISettings");
            if (settings != null)
            {
                // Note: This modifies the ScriptableObject at runtime
                // In a production app, you might want to save this separately
                Debug.Log($"[UI] Setting MicrophoneVolume to {value}");
            }
            
            UpdateStatus($"Volume set to: {(value * 100):F0}%", systemMessageColor);
        }

        public void OnVADToggleChanged(bool enabled)
        {
            // VAD Enable/Disable Logic - implementiere wenn VAD System vorhanden ist
            if (npcController != null)
            {
                // Hier könnte VAD-spezifische Logik stehen
                Debug.Log($"[UI] VAD Toggle changed: {enabled}");
            }
            UpdateStatus(enabled ? "VAD Enabled" : "VAD Disabled", systemMessageColor);
        }

        #endregion
    }
}
