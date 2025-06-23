using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NPC;

namespace Managers
{
    /// <summary>
    /// UI Manager for OpenAI Realtime NPC interaction
    /// Provides controls for testing and conversation management
    /// </summary>
    public class NPCUIManager : MonoBehaviour
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
        {            // Configure input field
            if (messageInputField != null)
            {
                messageInputField.onSubmit.AddListener(OnMessageSubmitted);
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
                npcController.OnConversationUpdate.AddListener(OnConversationUpdate);
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
            
            // Clean up NPC listeners
            if (npcController != null)
            {
                npcController.OnNPCStartedSpeaking.RemoveListener(OnNPCStartedSpeaking);
                npcController.OnNPCFinishedSpeaking.RemoveListener(OnNPCFinishedSpeaking);
                npcController.OnNPCStartedListening.RemoveListener(OnNPCStartedListening);
                npcController.OnNPCFinishedListening.RemoveListener(OnNPCFinishedListening);
                npcController.OnConversationUpdate.RemoveListener(OnConversationUpdate);
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
        
        #endregion
        
        #region NPC Event Handlers
        
        private void OnNPCStartedSpeaking(string message)
        {
            UpdateStatus("NPC is speaking...", npcMessageColor);
        }
        
        private void OnNPCFinishedSpeaking()
        {
            UpdateStatus("NPC finished speaking", systemMessageColor);
        }
        
        private void OnNPCStartedListening()
        {
            UpdateStatus("NPC is listening...", systemMessageColor);
        }
        
        private void OnNPCFinishedListening()
        {
            UpdateStatus("NPC stopped listening", systemMessageColor);
        }
        
        private void OnConversationUpdate(string message)
        {
            AddToConversation(message);
        }
          #endregion
        
        #region UI Updates
        
        private void UpdateUIState()
        {
            if (npcController == null) return;
            
            bool isConnected = npcController.IsConnected;
            bool isIdle = npcController.CurrentState == NPCState.Idle;
            bool isListening = npcController.CurrentState == NPCState.Listening;
            bool isSpeaking = npcController.CurrentState == NPCState.Speaking;
            
            // Only log when state actually changes (not every frame)
            Debug.Log($"UI State Update - Connected: {isConnected}, State: {npcController.CurrentState}");
            
            // Update button states
            if (connectButton != null)
                connectButton.interactable = !isConnected;
            if (disconnectButton != null)
                disconnectButton.interactable = isConnected;
            if (startConversationButton != null)
                startConversationButton.interactable = isConnected && (isIdle || isSpeaking);
            if (stopConversationButton != null)
                stopConversationButton.interactable = isConnected && isListening;
            if (sendMessageButton != null)
                sendMessageButton.interactable = isConnected;
            if (messageInputField != null)
                messageInputField.interactable = isConnected;
        }
        
        private void UpdateStatus(string status, Color color)
        {
            if (statusDisplay != null)
            {
                statusDisplay.text = $"Status: {status}";
                statusDisplay.color = color;
            }
            
            Debug.Log($"NPC UI Status: {status}");
            
            // Update UI state after status change
            Invoke(nameof(UpdateUIState), 0.1f);
        }
        
        private void AddToConversation(string message)
        {
            if (conversationDisplay == null) return;
            
            // Add timestamp
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] {message}";
              // Add to history
            conversationHistory += formattedMessage + "\n";
            currentLineCount++;
            
            // Trim if too many lines
            if (currentLineCount > maxConversationLines)
            {
                var lines = conversationHistory.Split('\n');
                var trimmedLines = new string[maxConversationLines];
                System.Array.Copy(lines, lines.Length - maxConversationLines, trimmedLines, 0, maxConversationLines);
                conversationHistory = string.Join("\n", trimmedLines);
                currentLineCount = maxConversationLines;
            }
            
            // Update display
            conversationDisplay.text = conversationHistory;
            
            // Auto-scroll to bottom (if using ScrollRect)
            var scrollRect = conversationDisplay.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private void OnVolumeChanged(float volume)
        {
            AudioListener.volume = volume;
        }
        
        #endregion
        
        #region Public API
          public void ClearConversation()
        {
            conversationHistory = "";
            currentLineCount = 0;
            if (conversationDisplay != null)
            {
                conversationDisplay.text = "Conversation cleared.\n";
            }
        }
        
        public void SetNPCController(NPCController controller)
        {
            // Remove old listeners
            RemoveEventListeners();
            
            // Set new controller
            npcController = controller;
            
            // Add new listeners
            SetupEventListeners();
            UpdateUIState();
        }
        
        #endregion
        
        #region Context Menu Actions
        
        [ContextMenu("Test UI Connection")]
        private void TestUIConnection()
        {
            OnConnectClicked();
        }
        
        [ContextMenu("Clear Conversation")]
        private void TestClearConversation()
        {
            ClearConversation();
        }
        
        [ContextMenu("Send Test Message")]
        private void TestSendMessage()
        {
            if (messageInputField != null)
            {
                messageInputField.text = "Hello, this is a test message!";
                SendCurrentMessage();
            }
        }
        
        [ContextMenu("Stop All Audio Playback")]
        private void TestStopAllAudio()
        {
            if (npcController != null)
            {
                // Try to find the audio manager through the NPC controller
                var audioManager = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
                if (audioManager != null)
                {
                    audioManager.StopAllAudioPlayback();
                    UpdateStatus("Stopped all audio playback", systemMessageColor);
                }
                else
                {
                    Debug.LogWarning("Could not find RealtimeAudioManager to stop audio");
                }
            }
        }
        
        [ContextMenu("Show Audio Queue Status")]
        private void TestShowAudioQueueStatus()
        {
            var audioManager = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            if (audioManager != null)
            {
                int queueCount = audioManager.GetAudioQueueCount();
                bool isPlaying = audioManager.IsPlayingAudio();
                string status = $"Audio Queue: {queueCount} clips, Playing: {isPlaying}";
                UpdateStatus(status, systemMessageColor);
                Debug.Log($"[DEBUG] {status}");
            }
        }
          [ContextMenu("Toggle Smooth Audio Playback")]
        private void TestToggleSmoothPlayback()
        {
            var audioManager = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            if (audioManager != null)
            {
                bool newMode = !audioManager.UseSmoothPlayback;
                audioManager.UseSmoothPlayback = newMode;
                string status = $"Smooth audio playback: {(newMode ? "ON" : "OFF")}";
                UpdateStatus(status, systemMessageColor);
                Debug.Log($"[DEBUG] {status}");
            }
        }
        
        #endregion
    }
}
