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
                var enumNames = System.Enum.GetNames(typeof(OpenAIVoice));
                voiceDropdown.AddOptions(new System.Collections.Generic.List<string>(enumNames));
                voiceDropdown.value = (int)voice;
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
            var newVoice = (OpenAIVoice)index;
            if (voice != newVoice)
            {
                voice = newVoice;
                // OpenAISettings zur Laufzeit anpassen
                var settings = Resources.Load<OpenAISettings>("OpenAISettings");
                if (settings != null)
                {
                    var field = typeof(OpenAISettings).GetField("voice", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(settings, newVoice);
                        Debug.Log($"[UI] OpenAISettings.voice zur Laufzeit gesetzt: {newVoice}");
                    }
                }
                // RealtimeClient-Instanz finden und SessionUpdate senden
                var client = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeClient>();
                if (client != null && client.IsConnected)
                {
                    _ = client.SendSessionUpdateAsync();
                }
                UpdateStatus($"Voice changed to: {voice}", systemMessageColor);
            }
        }

        #endregion

        #region NPC Event Handlers

        private void OnNPCStartedSpeaking()
        {
            UpdateStatus("NPC started speaking...", npcMessageColor);
        }

        private void OnNPCFinishedSpeaking()
        {
            UpdateStatus("NPC finished speaking.", npcMessageColor);
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
                bool isConversing = npcController.CurrentState == NPCState.Speaking;

                connectButton.interactable = !isConnected;
                disconnectButton.interactable = isConnected;
                startConversationButton.interactable = isConnected && !isConversing;
                stopConversationButton.interactable = isConversing;
                sendMessageButton.interactable = isConversing;

                // Update status display
                statusDisplay.text = isConnected ? "Connected to OpenAI" : "Disconnected from OpenAI";

                // Update conversation display
                conversationDisplay.text = conversationHistory;
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
            UpdateStatus($"Volume set to: {value}", systemMessageColor);
        }

        #endregion
    }
}
