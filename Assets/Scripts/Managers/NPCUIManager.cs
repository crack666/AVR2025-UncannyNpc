using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NPC;

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

            // Stimmen-Dropdown initialisieren
            if (voiceDropdown != null)
            {
                voiceDropdown.ClearOptions();
                // Use descriptive voice names for better UX
                var descriptiveNames = OpenAIVoiceExtensions.GetAllVoiceDescriptions();
                voiceDropdown.AddOptions(new System.Collections.Generic.List<string>(descriptiveNames));
                
                // Sichere Voice-Initialisierung: Verwende alloy falls aktueller Wert ungültig
                int safeVoiceIndex = 0; // alloy ist Index 0
                if (OpenAIVoiceExtensions.IsValid(voice))
                {
                    safeVoiceIndex = (int)voice;
                }
                else
                {
                    Debug.LogWarning($"[UI] Invalid voice {voice} detected, using alloy instead");
                    voice = OpenAIVoiceExtensions.GetDefault();
                }
                
                voiceDropdown.value = safeVoiceIndex;
                voiceDropdown.RefreshShownValue();
                voiceDropdown.onValueChanged.AddListener(OnVoiceDropdownChanged);
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
            if (npcController != null)
            {
                npcController.ConnectToOpenAI();
                UpdateStatus("Connecting to OpenAI...", systemMessageColor);
            }
        }

        public void OnDisconnectClicked()
        {
            if (npcController != null)
            {
                npcController.DisconnectFromOpenAI();
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
            var enumValues = System.Enum.GetValues(typeof(OpenAIVoice));
            if (index >= 0 && index < enumValues.Length)
            {
                var newVoice = (OpenAIVoice)enumValues.GetValue(index);
                if (voice != newVoice)
                {
                    voice = newVoice;
                    
                    // OpenAI Settings zur Laufzeit anpassen - direct property access
                    var settings = Resources.Load<OpenAISettings>("OpenAISettings");
                    if (settings != null)
                    {
                        // Direct property access instead of reflection
                        settings.VoiceIndex = index;
                        Debug.Log($"[UI] OpenAISettings.VoiceIndex zur Laufzeit gesetzt: {index} ({newVoice})");
                    }
                    
                    // Falls Conversation läuft, muss diese neu gestartet werden
                    bool wasConversationActive = false;
                    if (npcController != null && npcController.CurrentState == NPCState.Speaking)
                    {
                        wasConversationActive = true;
                        npcController.StopConversation();
                        UpdateStatus("Stopping conversation to change voice...", systemMessageColor);
                    }
                    
                    // RealtimeClient-Instanz finden und SessionUpdate senden (falls verbunden)
                    var client = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeClient>();
                    if (client != null && client.IsConnected)
                    {
                        _ = client.SendSessionUpdateAsync();
                        
                        // Falls Conversation aktiv war, nach kurzer Delay wieder starten
                        if (wasConversationActive)
                        {
                            // HINWEIS: Coroutine entfernt für Editor-Kompatibilität
                            // StartCoroutine(RestartConversationAfterDelay(1.0f));
                            
                            // Direkter Neustart ohne Delay (Editor-kompatibel)
                            npcController.StartConversation();
                            UpdateStatus("Conversation restarted with new voice", systemMessageColor);
                        }
                    }
                    
                    UpdateStatus($"Voice changed to: {voice}", systemMessageColor);
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
            // TEMPORÄRER Status - wird durch UpdateUIState() überschrieben
            UpdateStatus("NPC finished speaking.", npcMessageColor);
            
            // Force UI update nach kurzer Delay
            StartCoroutine(DelayedUIUpdate());
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
