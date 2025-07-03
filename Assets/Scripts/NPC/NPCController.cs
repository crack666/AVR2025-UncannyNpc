using UnityEngine;
using UnityEngine.Events;
using OpenAI.RealtimeAPI;
using OpenAI.Threading;
using System;

namespace NPC
{
    /// <summary>
    /// Main NPC controller that integrates ReadyPlayerMe avatar with OpenAI Realtime API
    /// Handles conversation flow, animations, and audio playback
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RealtimeClient realtimeClient;
        [SerializeField] private RealtimeAudioManager audioManager;
        [SerializeField] private Animator npcAnimator;
        [SerializeField] private AudioSource audioSource;

        [Header("NPC Configuration")]
        [SerializeField] private string npcName = "AI Assistant";
        [SerializeField] private bool enableLipSync = true;
        [SerializeField] private bool autoReturnToListening = true;

        [Header("Animation Settings")]
        [SerializeField] private AnimationCurve lipSyncCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private string talkingAnimationTrigger = "StartTalking";
        [SerializeField] private string listeningAnimationTrigger = "StartListening";
        [SerializeField] private string idleAnimationTrigger = "GoIdle";

        [Header("Events")]
        public UnityEvent OnNPCStartedSpeaking;
        public UnityEvent OnNPCFinishedSpeaking;
        public UnityEvent OnNPCStartedListening;
        public UnityEvent OnNPCFinishedListening;
        public UnityEvent<string> OnConversationUpdate;

        // State management
        private NPCState currentState = NPCState.Idle;
        private AudioClip currentResponseClip;
        private string accumulatedText = "";

        // Audio processing for lip sync
        private float[] audioSamples;
        private const int SAMPLE_WINDOW = 256;

        public NPCState CurrentState => currentState;
        public bool IsConnected => realtimeClient != null && realtimeClient.IsConnected;
        public bool IsAwaitingResponse => realtimeClient != null && realtimeClient.IsAwaitingResponse;

        #region Unity Lifecycle

        private void Awake()
        {            // Initialize components if not assigned
            if (realtimeClient == null)
                realtimeClient = FindFirstObjectByType<RealtimeClient>();
            if (audioManager == null)
                audioManager = FindFirstObjectByType<RealtimeAudioManager>();
            if (npcAnimator == null)
                npcAnimator = GetComponent<Animator>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        // Handle voice change session restart
        private async void RestartSessionForVoiceChange()
        {
            try
            {
                Debug.Log($"[NPCController] Restarting session for voice change for {npcName}");
                
                // Stop current session completely
                SetState(NPCState.Connecting);
                
                if (audioManager != null)
                {
                    audioManager.ForceStopAllRecording();
                }
                
                await realtimeClient.DisconnectAsync();
                
                // Wait longer for complete cleanup of audio operations
                await System.Threading.Tasks.Task.Delay(1000); // Increased from 500ms
                
                // Restart connection
                var success = await realtimeClient.ConnectAsync();
                
                if (success)
                {
                    // Ensure clean state after successful voice change restart
                    realtimeClient.ResetSessionState();
                    Debug.Log($"[NPCController] Session restarted successfully for voice change for {npcName}");
                    SetState(NPCState.Idle);
                }
                else
                {
                    Debug.LogError($"[NPCController] Failed to restart session for voice change for {npcName}");
                    SetState(NPCState.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NPCController] Error during session restart for voice change: {ex.Message}");
                SetState(NPCState.Error);
            }
        }

        private void OnEnable()
        {
            Debug.Log("[NPCController] OnEnable: Registering event listeners");
            if (realtimeClient != null)
            {
                realtimeClient.OnConnected.AddListener(OnRealtimeConnected);
                realtimeClient.OnDisconnected.AddListener(OnRealtimeDisconnected);
                realtimeClient.OnAudioReceived.AddListener(OnAudioReceived);
                realtimeClient.OnTextReceived.AddListener(OnTextReceived);
                realtimeClient.OnError.AddListener(OnRealtimeError);
                realtimeClient.OnResponseCompleted.AddListener(OnResponseCompleted);
            }
            if (audioManager != null)
            {
                audioManager.OnRecordingStarted.AddListener(OnUserStartedSpeaking);
                audioManager.OnRecordingStopped.AddListener(OnUserStoppedSpeaking);
                audioManager.OnAudioPlaybackStarted.AddListener(OnAudioPlaybackStarted);
                audioManager.OnAudioPlaybackFinished.AddListener(OnAudioPlaybackFinished);
            }
        }

        private void OnDisable()
        {
            Debug.Log("[NPCController] OnDisable: Removing event listeners");
            if (realtimeClient != null)
            {
                realtimeClient.OnConnected.RemoveListener(OnRealtimeConnected);
                realtimeClient.OnDisconnected.RemoveListener(OnRealtimeDisconnected);
                realtimeClient.OnAudioReceived.RemoveListener(OnAudioReceived);
                realtimeClient.OnTextReceived.RemoveListener(OnTextReceived);
                realtimeClient.OnError.RemoveListener(OnRealtimeError);
                realtimeClient.OnResponseCompleted.RemoveListener(OnResponseCompleted);
            }
            if (audioManager != null)
            {
                audioManager.OnRecordingStarted.RemoveListener(OnUserStartedSpeaking);
                audioManager.OnRecordingStopped.RemoveListener(OnUserStoppedSpeaking);
                audioManager.OnAudioPlaybackStarted.RemoveListener(OnAudioPlaybackStarted);
                audioManager.OnAudioPlaybackFinished.RemoveListener(OnAudioPlaybackFinished);
            }
        }

        private void Update()
        {
            UpdateLipSync();
            UpdateAnimationState();
        }

        #endregion

        #region Public API

        public async System.Threading.Tasks.Task ConnectToOpenAI()
        {
            if (realtimeClient != null)
            {
                SetState(NPCState.Connecting);
                var success = await realtimeClient.ConnectAsync();

                if (success)
                {
                    // Ensure clean state after successful connection
                    realtimeClient.ResetSessionState();
                }
                else
                {
                    SetState(NPCState.Error);
                }
            }
        }

        public async System.Threading.Tasks.Task DisconnectFromOpenAI()
        {
            if (realtimeClient != null)
            {
                Debug.Log($"[NPCController] Disconnecting {npcName} from OpenAI");
                
                // FORCE stop all audio operations FIRST and WAIT
                if (audioManager != null)
                {
                    audioManager.ForceStopAllRecording();
                    
                    // Give time for operations to fully stop
                    await System.Threading.Tasks.Task.Delay(200);
                }
                
                // Then disconnect from API
                await realtimeClient.DisconnectAsync();
                
                // Wait for full disconnect
                await System.Threading.Tasks.Task.Delay(100);
                
                SetState(NPCState.Idle);
                Debug.Log($"[NPCController] {npcName} fully disconnected from OpenAI");
            }
        }
        public void StartConversation()
        {
            Debug.Log($"[NPCController] StartConversation called. State: {currentState}, Connected: {IsConnected}, AwaitingResponse: {IsAwaitingResponse}");
            
            if (IsConnected && currentState == NPCState.Idle && !IsAwaitingResponse)
            {
                SetState(NPCState.Listening);
                if (audioManager != null)
                {
                    audioManager.StartRecording();
                }
                if (realtimeClient != null)
                {
                    realtimeClient.StartListening();
                }
                Debug.Log($"[NPCController] Conversation started for {npcName}");
            }
            else
            {
                Debug.LogWarning($"[NPCController] Cannot start conversation: Connected={IsConnected}, State={currentState}, AwaitingResponse={IsAwaitingResponse}");
            }
        }

        public void StopConversation()
        {
            Debug.Log($"[NPCController] StopConversation called. State: {currentState}, AwaitingResponse: {IsAwaitingResponse}");
            
            if (currentState == NPCState.Listening && !IsAwaitingResponse)
            {
                // Use async method for stopping - fire and forget
                if (audioManager != null)
                {
                    _ = audioManager.StopRecordingAsync();
                }
                
                if (realtimeClient != null)
                {
                    realtimeClient.StopListening();
                }
                
                SetState(NPCState.Idle);
                Debug.Log($"[NPCController] Conversation stopped for {npcName}");
            }
            else if (currentState == NPCState.Speaking)
            {
                // If stopped during speaking, just change state
                SetState(NPCState.Idle);
                Debug.Log($"[NPCController] Conversation stopped during speaking for {npcName}");
            }
            else
            {
                Debug.LogWarning($"[NPCController] Cannot stop conversation: State={currentState}, AwaitingResponse={IsAwaitingResponse}");
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
                // Audio playback is now handled by RealtimeAudioManager queue system
                // State management is handled by OnAudioPlaybackStarted/Finished events
                Debug.Log($"NPC '{npcName}' received audio chunk, queued for playback");
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
            
            // Granular error handling
            
            // 1. Ignore harmless "buffer too small" errors
            if (error.Contains("buffer too small"))
            {
                Debug.LogWarning($"NPC '{npcName}' ignoring harmless buffer-too-small error: {error}");
                return; // No state change
            }
            
            // 2. Handle "Already has active response" as race condition
            if (error.Contains("already has an active response"))
            {
                Debug.LogWarning($"NPC '{npcName}' detected race condition: {error}");
                // NO error state, just audio reset
                if (audioManager != null)
                {
                    audioManager.ResetAfterError();
                }
                return; // No state change to Error
            }
            
            // 3. Handle voice change errors - requires session restart
            if (error.Contains("Cannot update a conversation's voice if assistant audio is present"))
            {
                Debug.LogWarning($"NPC '{npcName}' voice change during session not allowed - waiting briefly for UI-initiated restart: {error}");
                
                // Force stop all audio first to clear any assistant audio
                if (audioManager != null)
                {
                    audioManager.ForceStopAllRecording();
                }
                
                // Small delay to allow UI-initiated restart to take precedence
                // This prevents race conditions between UI and error handler restarts
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(200); // Brief delay
                    
                    // Check if we're still connected (UI restart might have already started)
                    if (realtimeClient != null && realtimeClient.IsConnected)
                    {
                        Debug.LogWarning($"NPC '{npcName}' triggering fallback session restart for voice change");
                        
                        // Ensure restart runs on main thread due to Unity API calls
                        OpenAI.Threading.UnityMainThreadDispatcher.EnqueueAction(() =>
                        {
                            RestartSessionForVoiceChange();
                        });
                    }
                    else
                    {
                        Debug.Log($"NPC '{npcName}' session already disconnected - UI restart likely in progress");
                    }
                });
                
                return; // Don't set error state, handle restart
            }
            
            // 4. Other network/connection errors
            if (error.Contains("connection") || error.Contains("network") || error.Contains("timeout"))
            {
                Debug.LogWarning($"NPC '{npcName}' connection error, attempting recovery: {error}");
                // Try reconnection instead of error state
                SetState(NPCState.Connecting);
                _ = ConnectToOpenAI(); // Fire-and-forget async reconnect
                return;
            }
            
            // 5. Only set error state for real critical errors
            Debug.LogError($"NPC '{npcName}' critical error, setting error state: {error}");
            SetState(NPCState.Error);
            
            // Audio reset for all errors
            if (audioManager != null)
            {
                audioManager.ResetAfterError();
            }
        }



        private void OnUserStartedSpeaking()
        {
            if (currentState == NPCState.Listening)
            {
                Debug.Log($"User started speaking to {npcName} (recording started)");
                // Show NPC is actively listening - visual feedback only
                SetAnimationTrigger("UserSpeaking");
            }
        }

        private void OnUserStoppedSpeaking()
        {
            if (currentState == NPCState.Listening)
            {
                Debug.Log($"User stopped speaking to {npcName} (recording stopped)");
                SetAnimationTrigger("UserFinishedSpeaking");
                
                // Recording has already stopped - no need to stop it again
                // The API will now process the user's input
                // Just provide visual feedback and wait for response
            }
        }



        private void OnAudioPlaybackStarted()
        {
            // Audio playback is starting - NPC should be in speaking state
            SetState(NPCState.Speaking);
            Debug.Log($"NPC '{npcName}' started speaking (audio playback started)");
        }

        private void OnAudioPlaybackFinished()
        {
            Debug.Log($"[NPCController] OnAudioPlaybackFinished called for {npcName} - audio stream finished");
            
            // Audio playback has actually finished - now we can transition states
            if (currentState == NPCState.Speaking)
            {
                Debug.Log($"[NPCController] Audio playback finished, transitioning from Speaking state for {npcName}");
                
                // Trigger finished event
                OnNPCFinishedSpeaking?.Invoke();
                
                // Return to listening if auto-return is enabled
                if (autoReturnToListening && IsConnected && !IsAwaitingResponse)
                {
                    StartCoroutine(DelayedReturnToListening());
                }
                else
                {
                    SetState(NPCState.Idle);
                    Debug.Log($"NPC '{npcName}' audio finished - staying idle");
                }
            }
            else
            {
                Debug.Log($"[NPCController] Audio finished in non-speaking state ({currentState}) for {npcName}");
            }
        }
        
        private System.Collections.IEnumerator DelayedReturnToListening()
        {
            // Wait a frame to ensure all audio cleanup is complete
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(0.1f); // Small delay for cleanup
            
            // DETAILED debugging of conditions
            bool isConnected = IsConnected;
            bool isAwaitingResponse = IsAwaitingResponse;
            bool isSpeaking = currentState == NPCState.Speaking;
            bool isRecording = audioManager?.IsRecording ?? false;
            
            Debug.Log($"[NPCController] DelayedReturnToListening check for {npcName}: " +
                     $"IsConnected={isConnected}, IsAwaitingResponse={isAwaitingResponse}, " +
                     $"CurrentState={currentState}, IsSpeaking={isSpeaking}, IsRecording={isRecording}");
            
            // FIXED: Handle all relevant states and focus on starting recording
            if (isConnected && !isAwaitingResponse)
            {
                // Ensure we're in listening state
                if (currentState != NPCState.Listening)
                {
                    SetState(NPCState.Listening);
                }
                
                // Start recording if not already recording
                if (audioManager != null && !audioManager.IsRecording)
                {
                    audioManager.StartRecording();
                    Debug.Log($"NPC '{npcName}' successfully started recording (State: {currentState})");
                }
                else if (audioManager != null && audioManager.IsRecording)
                {
                    Debug.Log($"NPC '{npcName}' already recording - no action needed");
                }
                else
                {
                    Debug.LogWarning($"NPC '{npcName}' cannot start recording: audioManager is null");
                    SetState(NPCState.Idle);
                }
            }
            else
            {
                Debug.Log($"NPC '{npcName}' conditions not met for listening. Setting to Idle. IsConnected={isConnected}, IsAwaitingResponse={isAwaitingResponse}");
                SetState(NPCState.Idle);
            }
        }

        private void OnResponseCompleted()
        {
            Debug.Log($"[NPCController] OpenAI response completed for {npcName}");
            
            // OpenAI has finished sending audio chunks, but audio may still be playing
            // DO NOT stop audio playback here - let the audio system finish naturally
            if (currentState == NPCState.Speaking)
            {
                Debug.Log($"[NPCController] Response completed, but waiting for audio playback to finish for {npcName}");
                
                // Just mark that OpenAI response is done - audio system will handle the rest
                // The audio system will call OnAudioPlaybackFinished when actually done
                
                // Reset accumulated text for next response
                accumulatedText = ""; // Reset for next response
                
                // Do NOT change state here - wait for audio to finish
            }
            else
            {
                Debug.Log($"[NPCController] Response completed in non-speaking state for {npcName}");
            }
        }

        #endregion

        #region Audio & Animation        
        private void UpdateLipSync()
        {
            if (!enableLipSync || npcAnimator == null)
                return;

            // Get audio data from RealtimeAudioManager's playback source
            var audioManager = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            if (audioManager == null || !audioManager.IsPlayingAudio())
                return;

            // Note: We need to access the playback audio source from RealtimeAudioManager
            // For now, disable lip sync until we can properly integrate with the audio manager
            // TODO: Add GetPlaybackAudioSource() method to RealtimeAudioManager for lip sync

            /*
            // Simple amplitude-based lip sync
            audioSamples = new float[SAMPLE_WINDOW];
            playbackAudioSource.GetOutputData(audioSamples, 0);
            
            float sum = 0f;
            for (int i = 0; i < SAMPLE_WINDOW; i++)
            {
                sum += Mathf.Abs(audioSamples[i]);
            }
            
            float amplitude = sum / SAMPLE_WINDOW;
            float lipSyncValue = lipSyncCurve.Evaluate(amplitude * lipSyncSensitivity);
            
            // Apply to animator (assuming you have a "LipSync" float parameter)
            npcAnimator.SetFloat("LipSync", lipSyncValue);
            */
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
            _ = ConnectToOpenAI(); // Fire-and-forget for menu item
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

        [ContextMenu("Reset Error State")]
        private void ResetErrorState()
        {
            Debug.Log("[NPCController] ResetErrorState: Setting state to Idle");
            SetState(NPCState.Idle);
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
