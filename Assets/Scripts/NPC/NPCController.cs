using UnityEngine;
using UnityEngine.Events;
using OpenAI.RealtimeAPI;

namespace NPC
{
    /// <summary>
    /// Main NPC controller that integrates ReadyPlayerMe avatar with OpenAI Realtime API
    /// Handles conversation flow, animations, and audio playback
    /// </summary>
    public class NPCController : MonoBehaviour
    {        [Header("Components")]
        [SerializeField] private RealtimeClient realtimeClient;
        [SerializeField] private RealtimeAudioManager audioManager;
        [SerializeField] private Animator npcAnimator;
        [SerializeField] private AudioSource audioSource;
        
        [Header("NPC Configuration")]
        [SerializeField] private string npcName = "AI Assistant";
        [SerializeField] private string personality = "friendly and helpful";
        [SerializeField] private bool enableLipSync = true;
        [SerializeField] private bool enableGestures = true;
        
        [Header("Animation Settings")]
        [SerializeField] private float lipSyncSensitivity = 1.0f;
        [SerializeField] private AnimationCurve lipSyncCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private string talkingAnimationTrigger = "StartTalking";
        [SerializeField] private string listeningAnimationTrigger = "StartListening";
        [SerializeField] private string idleAnimationTrigger = "GoIdle";
        
        [Header("Events")]
        public UnityEvent<string> OnNPCStartedSpeaking;
        public UnityEvent OnNPCFinishedSpeaking;
        public UnityEvent OnNPCStartedListening;
        public UnityEvent OnNPCFinishedListening;
        public UnityEvent<string> OnConversationUpdate;
        
        // State management
        private NPCState currentState = NPCState.Idle;
        private bool isProcessingAudio = false;
        private AudioClip currentResponseClip;
        private string accumulatedText = "";
        
        // Audio processing for lip sync
        private float[] audioSamples;
        private int audioSampleRate = 44100;
        private const int SAMPLE_WINDOW = 256;
        
        public NPCState CurrentState => currentState;
        public bool IsConnected => realtimeClient != null && realtimeClient.IsConnected;
        
        #region Unity Lifecycle
        
        private void Awake()
        {            // Initialize components if not assigned
            if (realtimeClient == null)
                realtimeClient = FindObjectOfType<RealtimeClient>();
            if (audioManager == null)
                audioManager = FindObjectOfType<RealtimeAudioManager>();
            if (npcAnimator == null)
                npcAnimator = GetComponent<Animator>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }
        
        private void OnEnable()
        {
            if (realtimeClient != null)
            {
                realtimeClient.OnConnected.AddListener(OnRealtimeConnected);
                realtimeClient.OnDisconnected.AddListener(OnRealtimeDisconnected);
                realtimeClient.OnAudioReceived.AddListener(OnAudioReceived);
                realtimeClient.OnTextReceived.AddListener(OnTextReceived);
                realtimeClient.OnError.AddListener(OnRealtimeError);
            }
            
            if (audioManager != null)
            {
                audioManager.OnVoiceActivityStarted.AddListener(OnUserStartedSpeaking);
                audioManager.OnVoiceActivityStopped.AddListener(OnUserStoppedSpeaking);
            }
        }
        
        private void OnDisable()
        {
            if (realtimeClient != null)
            {
                realtimeClient.OnConnected.RemoveListener(OnRealtimeConnected);
                realtimeClient.OnDisconnected.RemoveListener(OnRealtimeDisconnected);
                realtimeClient.OnAudioReceived.RemoveListener(OnAudioReceived);
                realtimeClient.OnTextReceived.RemoveListener(OnTextReceived);
                realtimeClient.OnError.RemoveListener(OnRealtimeError);
            }
            
            if (audioManager != null)
            {
                audioManager.OnVoiceActivityStarted.RemoveListener(OnUserStartedSpeaking);
                audioManager.OnVoiceActivityStopped.RemoveListener(OnUserStoppedSpeaking);
            }
        }
        
        private void Update()
        {
            UpdateLipSync();
            UpdateAnimationState();
        }
        
        #endregion
        
        #region Public API
        
        public async void ConnectToOpenAI()
        {
            if (realtimeClient != null)
            {
                SetState(NPCState.Connecting);
                var success = await realtimeClient.ConnectAsync();
                
                if (!success)
                {
                    SetState(NPCState.Error);
                }
            }
        }
        
        public async void DisconnectFromOpenAI()
        {
            if (realtimeClient != null)
            {
                await realtimeClient.DisconnectAsync();
                SetState(NPCState.Idle);
            }
        }
        
        public void StartConversation()
        {
            if (IsConnected && currentState == NPCState.Idle)
            {
                SetState(NPCState.Listening);
                audioManager?.StartListening();
                realtimeClient?.StartListening();
            }
        }
        
        public void StopConversation()
        {
            if (currentState == NPCState.Listening || currentState == NPCState.Speaking)
            {
                audioManager?.StopListening();
                realtimeClient?.StopListening();
                SetState(NPCState.Idle);
            }
        }
        
        public void SendTextMessage(string message)
        {
            if (IsConnected && !string.IsNullOrEmpty(message))
            {
                realtimeClient?.SendUserMessage(message);
                OnConversationUpdate?.Invoke($"User: {message}");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnRealtimeConnected()
        {
            Debug.Log($"NPC '{npcName}' connected to OpenAI Realtime API");
            SetState(NPCState.Idle);
        }
        
        private void OnRealtimeDisconnected()
        {
            Debug.Log($"NPC '{npcName}' disconnected from OpenAI Realtime API");
            SetState(NPCState.Idle);
        }
        
        private void OnAudioReceived(AudioChunk audioChunk)
        {
            if (audioChunk != null && audioChunk.data != null)
            {
                // Convert audio chunk to AudioClip and play
                var audioClip = audioChunk.ToAudioClip($"NPCResponse_{Time.time}");
                PlayNPCResponse(audioClip);
            }
        }
        
        private void OnTextReceived(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                accumulatedText += text;
                OnConversationUpdate?.Invoke($"{npcName}: {accumulatedText}");
            }
        }
        
        private void OnRealtimeError(string error)
        {
            Debug.LogError($"NPC '{npcName}' Realtime API Error: {error}");
            SetState(NPCState.Error);
        }
        
        private void OnUserStartedSpeaking()
        {
            if (currentState == NPCState.Listening)
            {
                Debug.Log($"User started speaking to {npcName}");
                // Optionally change animation to show NPC is actively listening
                SetAnimationTrigger("UserSpeaking");
            }
        }
        
        private void OnUserStoppedSpeaking()
        {
            if (currentState == NPCState.Listening)
            {
                Debug.Log($"User stopped speaking to {npcName}");
                SetAnimationTrigger("UserFinishedSpeaking");
                // The realtime client will automatically process the audio
            }
        }
        
        #endregion
        
        #region Audio & Animation
        
        private void PlayNPCResponse(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                SetState(NPCState.Speaking);
                currentResponseClip = clip;
                audioSource.clip = clip;
                audioSource.Play();
                
                OnNPCStartedSpeaking?.Invoke(accumulatedText);
                accumulatedText = ""; // Reset for next response
                
                // Schedule end of speaking
                Invoke(nameof(OnResponseFinished), clip.length);
            }
        }
        
        private void OnResponseFinished()
        {
            OnNPCFinishedSpeaking?.Invoke();
            
            if (currentState == NPCState.Speaking)
            {
                SetState(NPCState.Listening);
            }
        }
        
        private void UpdateLipSync()
        {
            if (!enableLipSync || npcAnimator == null || !audioSource.isPlaying)
                return;
            
            // Simple amplitude-based lip sync
            if (currentResponseClip != null)
            {
                audioSamples = new float[SAMPLE_WINDOW];
                audioSource.GetOutputData(audioSamples, 0);
                
                float sum = 0f;
                for (int i = 0; i < SAMPLE_WINDOW; i++)
                {
                    sum += Mathf.Abs(audioSamples[i]);
                }
                
                float amplitude = sum / SAMPLE_WINDOW;
                float lipSyncValue = lipSyncCurve.Evaluate(amplitude * lipSyncSensitivity);
                
                // Apply to animator (assuming you have a "LipSync" float parameter)
                npcAnimator.SetFloat("LipSync", lipSyncValue);
            }
        }
        
        private void UpdateAnimationState()
        {
            if (npcAnimator == null) return;
            
            // Update animator with current state
            npcAnimator.SetBool("IsConnected", IsConnected);
            npcAnimator.SetBool("IsListening", currentState == NPCState.Listening);
            npcAnimator.SetBool("IsSpeaking", currentState == NPCState.Speaking);
            npcAnimator.SetBool("HasError", currentState == NPCState.Error);
        }
        
        private void SetAnimationTrigger(string triggerName)
        {
            if (npcAnimator != null && !string.IsNullOrEmpty(triggerName))
            {
                npcAnimator.SetTrigger(triggerName);
            }
        }
        
        #endregion
        
        #region State Management
        
        private void SetState(NPCState newState)
        {
            if (currentState != newState)
            {
                var previousState = currentState;
                currentState = newState;
                
                Debug.Log($"NPC '{npcName}' state changed: {previousState} -> {newState}");
                
                // Handle state-specific animations
                switch (newState)
                {
                    case NPCState.Idle:
                        SetAnimationTrigger(idleAnimationTrigger);
                        break;
                    case NPCState.Listening:
                        SetAnimationTrigger(listeningAnimationTrigger);
                        OnNPCStartedListening?.Invoke();
                        break;
                    case NPCState.Speaking:
                        SetAnimationTrigger(talkingAnimationTrigger);
                        break;
                    case NPCState.Error:
                        SetAnimationTrigger("Error");
                        break;
                }
            }
        }
        
        #endregion
        
        #region Debugging
        
        [ContextMenu("Test Connection")]
        private void TestConnection()
        {
            ConnectToOpenAI();
        }
        
        [ContextMenu("Start Test Conversation")]
        private void StartTestConversation()
        {
            StartConversation();
        }
        
        [ContextMenu("Send Test Message")]
        private void SendTestMessage()
        {
            SendTextMessage("Hello, how are you today?");
        }
        
        #endregion
    }
    
    public enum NPCState
    {
        Idle,
        Connecting,
        Listening,
        Processing,
        Speaking,
        Error
    }
}
