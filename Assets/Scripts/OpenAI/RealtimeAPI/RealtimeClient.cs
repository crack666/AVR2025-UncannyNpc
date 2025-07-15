using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using OpenAI.Threading;

namespace OpenAI.RealtimeAPI
{    /// <summary>
     /// Production-ready WebSocket client for OpenAI Realtime API
     /// Uses System.Net.WebSockets for real WebSocket connection
     /// </summary>
    public class RealtimeClient : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private OpenAISettings settings;
        [SerializeField] private bool autoConnect = false; // Disabled to prevent conflicts with manual connect
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private float retryDelay = 5f;

        [Header("Events")]
        public UnityEvent<string> OnMessageReceived;
        public UnityEvent<AudioChunk> OnAudioReceived;
        public UnityEvent<string> OnTextReceived;
        public UnityEvent OnConnected;
        public UnityEvent OnDisconnected;
        public UnityEvent<string> OnError;
        public UnityEvent OnResponseCompleted; // NEW: When OpenAI response is fully completed

        // Connection state
        private ClientWebSocket webSocket;
        private bool isConnected = false;
        private bool isConnecting = false;
        private CancellationTokenSource cancellationTokenSource;
        private int currentRetries = 0;

        // Message queue for Unity main thread
        private Queue<string> messageQueue = new Queue<string>();
        private readonly object queueLock = new object();

        // Audio processing
        private Queue<AudioChunk> audioQueue = new Queue<AudioChunk>();
        private readonly object audioQueueLock = new object();
        
        // Runtime Voice Override System
        private OpenAIVoice? runtimeVoiceOverride = null; // Null means use settings default
        
        // Session management
        private string sessionId;
        private SessionState sessionState;

        // --- NEW: Track if a response is currently running ---
        private bool isAwaitingResponse = false;

        // Event counting for aggregation (following OpenAI reference pattern)
        private Dictionary<string, int> eventCounts = new Dictionary<string, int>();
        private Dictionary<string, float> lastEventTimes = new Dictionary<string, float>();

        public bool IsConnected => isConnected;
        public string SessionId => sessionId;
        public bool IsAwaitingResponse
        {
            get { return isAwaitingResponse; }
        }

        #region Unity Lifecycle
        private void Awake()
        {
            sessionState = new SessionState();

            // Auto-load settings from Resources if not assigned
            if (settings == null)
            {
                settings = Resources.Load<OpenAISettings>("OpenAISettings");
                if (settings != null)
                {
                    Debug.Log("RealtimeClient: Auto-loaded OpenAISettings from Resources");
                }
                else
                {
                    Debug.LogError("RealtimeClient: No OpenAISettings found! Please create one in Assets/Resources/");
                }
            }

            // Initialize events if not set
            OnMessageReceived ??= new UnityEvent<string>();
            OnAudioReceived ??= new UnityEvent<AudioChunk>();
            OnTextReceived ??= new UnityEvent<string>();
            OnConnected ??= new UnityEvent();
            OnDisconnected ??= new UnityEvent();
            OnError ??= new UnityEvent<string>();
            OnResponseCompleted ??= new UnityEvent();
        }

        private void Start()
        {
            // Perform runtime validation
            ValidateConfiguration();
            
            if (autoConnect && settings != null && !string.IsNullOrEmpty(settings.ApiKey))
            {
                _ = ConnectAsync();
            }
        }

        /// <summary>
        /// Validates the RealtimeClient configuration and logs any issues
        /// </summary>
        private void ValidateConfiguration()
        {
            Debug.Log("RealtimeClient: Performing configuration validation...");
            
            if (settings == null)
            {
                Debug.LogError("RealtimeClient: OpenAISettings not assigned! Please assign it in the inspector or ensure it exists in Resources/");
                return;
            }
            
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                Debug.LogError("RealtimeClient: API Key is empty! Please configure your OpenAI API key in the OpenAISettings asset.");
                return;
            }
            
            if (settings.ApiKey.Length < 50)
            {
                Debug.LogWarning("RealtimeClient: API Key seems too short. Please verify it's a valid OpenAI API key.");
            }
            
            // Validate that settings are being used
            Debug.Log($"RealtimeClient: Configuration validation:");
            Debug.Log($"  - API Key: {settings.ApiKey.Substring(0, 7)}... (length: {settings.ApiKey.Length})");
            Debug.Log($"  - Model: {settings.Model}");
            Debug.Log($"  - BaseUrl: {settings.BaseUrl}");
            Debug.Log($"  - WebSocket URL: {settings.GetWebSocketUrl()}");
            Debug.Log($"  - Voice: {GetVoiceNameFromSettings(settings)} (VoiceName: {settings.VoiceName})");
            Debug.Log($"  - Temperature: {settings.Temperature}");
            Debug.Log($"  - System Prompt: {settings.SystemPrompt.Substring(0, Math.Min(50, settings.SystemPrompt.Length))}...");
            Debug.Log($"  - Sample Rate: {settings.SampleRate}Hz");
            Debug.Log($"  - Audio Chunk Size: {settings.AudioChunkSizeMs}ms");
            Debug.Log($"  - Microphone Volume: {settings.MicrophoneVolume}");
            Debug.Log($"  - Transcription Model: {settings.TranscriptionModel}");
            Debug.Log($"  - VAD Type: {settings.VadType}");
            Debug.Log($"  - VAD Threshold: {settings.VadThreshold}");
            Debug.Log($"  - VAD Prefix Padding: {settings.VadPrefixPaddingMs}ms");
            Debug.Log($"  - VAD Silence Duration: {settings.VadSilenceDurationMs}ms");
            Debug.Log($"  - Debug Logging: {settings.EnableDebugLogging}");
            
            if (!settings.IsValid())
            {
                Debug.LogWarning("RealtimeClient: Settings validation failed! Some settings may be invalid.");
            }
            else
            {
                Debug.Log("RealtimeClient: All settings validation passed ✅");
            }
        }

        private void Update()
        {
            ProcessMessageQueue();
            ProcessAudioQueue();
        }
        private void OnApplicationPause(bool pauseStatus)
        {
            // Only disconnect on pause for mobile platforms to save battery
#if UNITY_ANDROID || UNITY_IOS
            if (pauseStatus && isConnected)
            {
                Debug.Log("RealtimeClient: App paused, disconnecting to save resources");
                _ = DisconnectAsync();
            }
#endif
        }

        private void OnDestroy()
        {
            _ = DisconnectAsync();
        }

        #endregion

        #region Connection Management

        public async Task<bool> ConnectAsync()
        {
            if (isConnected || isConnecting)
            {
                Debug.LogWarning("RealtimeClient: Already connected or connecting");
                return isConnected;
            }

            if (settings == null || string.IsNullOrEmpty(settings.ApiKey))
            {
                string error;
                if (settings == null)
                {
                    error = "OpenAI Settings not found! Please assign OpenAISettings in the RealtimeClient inspector or ensure it exists in Assets/Resources/";
                }
                else
                {
                    error = "OpenAI API key not configured! Please set your API key in the OpenAISettings asset.";
                }
                
                Debug.LogError($"RealtimeClient: {error}");
                OnError?.Invoke(error);
                return false;
            }

            Debug.Log($"RealtimeClient: Starting connection attempt. API Key configured: {!string.IsNullOrEmpty(settings.ApiKey)}, Model: {settings.Model}");
            
            isConnecting = true;
            currentRetries = 0;

            while (currentRetries < maxRetries && !isConnected)
            {
                try
                {
                    Debug.Log($"RealtimeClient: Attempting connection (attempt {currentRetries + 1}/{maxRetries})");
                    await AttemptConnection();
                    Debug.Log("RealtimeClient: Connection successful");
                    break;
                }
                catch (Exception ex)
                {
                    currentRetries++;
                    Debug.LogError($"RealtimeClient: Connection attempt {currentRetries} failed: {ex.Message}");
                    Debug.LogError($"RealtimeClient: Exception type: {ex.GetType().Name}");
                    Debug.LogError($"RealtimeClient: Full stack trace: {ex.StackTrace}");

                    if (currentRetries < maxRetries)
                    {
                        Debug.Log($"RealtimeClient: Retrying in {retryDelay} seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(retryDelay));
                    }
                    else
                    {
                        var errorMsg = $"Failed to connect after {maxRetries} attempts: {ex.Message}";
                        Debug.LogError($"RealtimeClient: {errorMsg}");
                        OnError?.Invoke(errorMsg);
                    }
                }
            }

            isConnecting = false;
            
            // Ensure clean state on failed connection
            if (!isConnected)
            {
                isAwaitingResponse = false;
            }
            
            return isConnected;
        }

        private async Task AttemptConnection()
        {
            Debug.Log("RealtimeClient: Starting AttemptConnection()");
            
            // Clean up any existing connection
            if (webSocket != null)
            {
                Debug.Log("RealtimeClient: Disposing existing WebSocket");
                webSocket.Dispose();
            }

            cancellationTokenSource = new CancellationTokenSource();
            webSocket = new ClientWebSocket();

            Debug.Log("RealtimeClient: Configuring WebSocket headers");
            
            // Configure headers
            try
            {
                webSocket.Options.SetRequestHeader("Authorization", $"Bearer {settings.ApiKey}");
                webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");
                Debug.Log("RealtimeClient: Headers configured successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"RealtimeClient: Failed to set headers: {ex.Message}");
                throw;
            }

            // Connect to OpenAI Realtime API using settings
            string wsUrl;
            if (settings != null && !string.IsNullOrEmpty(settings.BaseUrl) && !string.IsNullOrEmpty(settings.Model))
            {
                // Use GetWebSocketUrl() method from settings
                wsUrl = settings.GetWebSocketUrl();
                Debug.Log($"RealtimeClient: Using settings BaseUrl: {settings.BaseUrl}, Model: {settings.Model}");
            }
            else
            {
                // Fallback to hardcoded URL
                wsUrl = "wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01";
                Debug.LogWarning("RealtimeClient: Using fallback URL - settings not properly configured");
            }
            
            var uri = new Uri(wsUrl);
            Debug.Log($"RealtimeClient: Attempting WebSocket connection to: {uri}");
            
            try
            {
                await webSocket.ConnectAsync(uri, cancellationTokenSource.Token);
                Debug.Log("RealtimeClient: WebSocket connection established");
            }
            catch (Exception ex)
            {
                Debug.LogError($"RealtimeClient: WebSocket connection failed: {ex.Message}");
                throw;
            }

            isConnected = true;
            sessionId = Guid.NewGuid().ToString();
            
            // Reset awaiting response state for fresh session
            isAwaitingResponse = false;

            Debug.Log("RealtimeClient: Connected to OpenAI Realtime API");

            // Start message receiving loop
            _ = Task.Run(ReceiveLoop);

            // Initialize session
            await SendSessionUpdateAsync();

            // Notify connection success
            EnqueueMainThreadAction(() => OnConnected?.Invoke());
        }

        public async Task DisconnectAsync()
        {
            if (!isConnected && webSocket == null)
                return;

            isConnected = false;
            isConnecting = false; // Also reset connecting state
            isAwaitingResponse = false; // Reset awaiting response state

            try
            {
                cancellationTokenSource?.Cancel();

                if (webSocket?.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"RealtimeClient: Error during disconnect: {ex.Message}");
            }
            finally
            {
                webSocket?.Dispose();
                webSocket = null;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;

                EnqueueMainThreadAction(() => OnDisconnected?.Invoke());
                Debug.Log("RealtimeClient: Disconnected");
            }
        }

        #endregion

        #region Message Handling

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            var messageBuilder = new StringBuilder();

            try
            {
                while (isConnected && webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationTokenSource.Token
                    ).ConfigureAwait(false); // Critical: Stay on background thread

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messageChunk);

                        if (result.EndOfMessage)
                        {
                            var completeMessage = messageBuilder.ToString();
                            messageBuilder.Clear();

                            // Thread-safe: Enqueue for main thread processing
                            EnqueueMessage(completeMessage);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("RealtimeClient: WebSocket closed by server");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("RealtimeClient: Receive loop cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"RealtimeClient: Error in receive loop: {ex.Message}");

                // Thread-safe: Error callback to main thread
                UnityMainThreadDispatcher.EnqueueAction(() => {
                    OnError?.Invoke($"Receive error: {ex.Message}");
                });
            }

            isConnected = false;
        }

        private void EnqueueMessage(string message)
        {
            lock (queueLock)
            {
                messageQueue.Enqueue(message);
            }
        }

        private void ProcessMessageQueue()
        {
            if (!isConnected) return;

            lock (queueLock)
            {
                while (messageQueue.Count > 0)
                {
                    var message = messageQueue.Dequeue();
                    ProcessReceivedMessage(message);
                }
            }
        }

        private void ProcessReceivedMessage(string jsonMessage)
        {
            try
            {
                var eventData = JsonConvert.DeserializeObject<RealtimeEvent>(jsonMessage);

                // Correct isAwaitingResponse management
                switch (eventData.type)
                {
                    case "response.created":
                        // Response was created - we now expect deltas
                        isAwaitingResponse = true;
                        Debug.Log("[RealtimeClient] Response created - now awaiting deltas");
                        break;

                    case "response.audio.delta":
                        // DO NOT reset here - response is still running
                        HandleAudioDelta(eventData);
                        break;

                    case "response.text.delta":
                        // DO NOT reset here - response is still running
                        HandleTextDelta(eventData);
                        break;

                    case "response.done":
                    case "response.cancelled":
                    case "response.failed":
                        // ONLY HERE mark response as finished
                        isAwaitingResponse = false;
                        Debug.Log($"[RealtimeClient] Response finished: {eventData.type}");
                        
                        // CRITICAL: Trigger response completed event for NPC state management
                        OnResponseCompleted?.Invoke();
                        break;

                    case "session.created":
                        HandleSessionCreated(eventData);
                        break;

                    case "conversation.item.created":
                        HandleItemCreated(eventData);
                        break;

                    case "error":
                        HandleError(eventData);
                        break;
                }

                OnMessageReceived?.Invoke(jsonMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"RealtimeClient: Error processing message: {ex.Message}");
                Debug.LogError($"RealtimeClient: Problematic message: {jsonMessage}");
                OnError?.Invoke($"Message processing error: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleAudioDelta(RealtimeEvent eventData) {
            if (eventData.delta != null && !string.IsNullOrEmpty(eventData.delta))
            {
                try
                {
                    var audioBytes = Convert.FromBase64String(eventData.delta);
                    var audioChunk = new AudioChunk(audioBytes, 24000, 1);

                    lock (audioQueueLock)
                    {
                        audioQueue.Enqueue(audioChunk);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"RealtimeClient: Error processing audio delta: {ex.Message}");
                }
            }
        }

        private void HandleTextDelta(RealtimeEvent eventData)
        {
            if (!string.IsNullOrEmpty(eventData.delta))
            {
                OnTextReceived?.Invoke(eventData.delta);
            }
        }

        private void HandleSessionCreated(RealtimeEvent eventData)
        {
            Debug.Log($"RealtimeClient: Session created with ID: {eventData.session?.id}");
            if (eventData.session != null)
            {
                sessionId = eventData.session.id;
            }
        }

        private void HandleItemCreated(RealtimeEvent eventData)
        {
            string itemType = eventData.item?.type ?? "unknown";
            string itemRole = eventData.item?.role ?? "unknown";
            string itemId = eventData.item?.id ?? "unknown";
            
            // Aggregate similar events (following OpenAI reference pattern)
            string eventKey = $"item.created.{itemType}.{itemRole}";
            float currentTime = Time.time;
            
            if (eventCounts.ContainsKey(eventKey) && 
                lastEventTimes.ContainsKey(eventKey) && 
                (currentTime - lastEventTimes[eventKey]) < 1.0f) // Within 1 second
            {
                eventCounts[eventKey]++;
                lastEventTimes[eventKey] = currentTime;
                
                // Only log every 5th event or after 1 second gap
                if (eventCounts[eventKey] % 5 == 0)
                {
                    Debug.Log($"RealtimeClient: Conversation items created: {itemType}/{itemRole} (count: {eventCounts[eventKey]})");
                }
            }
            else
            {
                eventCounts[eventKey] = 1;
                lastEventTimes[eventKey] = currentTime;
                Debug.Log($"RealtimeClient: Conversation item created: {itemId} (type: {itemType}, role: {itemRole})");
            }
        }

        private void HandleError(RealtimeEvent eventData)
        {
            var errorMsg = eventData.error?.message ?? "Unknown error";
            Debug.LogError($"RealtimeClient: API Error: {errorMsg}");

            // Improved "buffer too small" handling
            if (errorMsg.Contains("buffer too small"))
            {
                // Reset isAwaitingResponse so the system can recover
                isAwaitingResponse = false;
                Debug.LogWarning("[RealtimeClient] Resetting isAwaitingResponse after 'buffer too small' error.");

                // Check if audio playback has already started
                var audioManager = FindFirstObjectByType<RealtimeAudioManager>();
                if (audioManager != null && audioManager.IsPlayingAudio())
                {
                    Debug.LogWarning("[RealtimeClient] Ignoring 'buffer too small' error because audio playback is active.");
                    return; // Do not propagate error, just log and continue
                }

                // Also ignore other "harmless" errors to prevent state disruption
                Debug.LogWarning("[RealtimeClient] Ignoring harmless 'buffer too small' error to prevent state disruption.");
                return;
            }

            // Handle "Conversation already has an active response" more gracefully
            if (errorMsg.Contains("already has an active response"))
            {
                Debug.LogWarning("[RealtimeClient] 'Already has active response' - this suggests a race condition. Resetting state.");
                isAwaitingResponse = false; // Reset state
                return; // Don't propagate this error
            }

            // Only propagate real errors
            OnError?.Invoke(errorMsg);
        }

        private void ProcessAudioQueue()
        {
            if (!isConnected) return;

            lock (audioQueueLock)
            {
                while (audioQueue.Count > 0)
                {
                    var audioChunk = audioQueue.Dequeue();
                    OnAudioReceived?.Invoke(audioChunk);
                }
            }
        }

        #endregion

        #region Sending Messages

        public async Task SendSessionUpdateAsync()
        {
            if (!isConnected) return;

            var sessionConfig = new
            {
                type = "session.update",
                session = new
                {
                    modalities = new[] { "text", "audio" },
                    instructions = settings?.SystemPrompt ?? "You are a helpful AI assistant.",
                    voice = GetVoiceNameFromSettings(settings),
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    input_audio_transcription = new
                    {
                        model = GetValidTranscriptionModel(settings?.TranscriptionModel)
                    },
                    turn_detection = new
                    {
                        type = settings?.VadType ?? "server_vad",
                        threshold = settings?.VadThreshold ?? 0.5,
                        prefix_padding_ms = settings?.VadPrefixPaddingMs ?? 300,
                        silence_duration_ms = settings?.VadSilenceDurationMs ?? 200
                    },
                    temperature = settings?.Temperature ?? 0.8f // Use temperature from settings
                }
            };

            Debug.Log($"RealtimeClient: Sending session update:");
            Debug.Log($"  - Voice: {GetVoiceNameFromSettings(settings)}");
            Debug.Log($"  - Temperature: {settings?.Temperature ?? 0.8f}");
            Debug.Log($"  - Transcription Model: {settings?.TranscriptionModel ?? "whisper-1"}");
            Debug.Log($"  - VAD Type: {settings?.VadType ?? "server_vad"}");
            Debug.Log($"  - VAD Threshold: {settings?.VadThreshold ?? 0.5}");
            Debug.Log($"  - VAD Prefix Padding: {settings?.VadPrefixPaddingMs ?? 300}ms");
            Debug.Log($"  - VAD Silence Duration: {settings?.VadSilenceDurationMs ?? 200}ms");
            
            await SendJsonAsync(sessionConfig);
        }

        public async Task SendAudioAsync(byte[] audioData)
        {
            if (!isConnected || audioData == null || audioData.Length == 0) return;

            var audioEvent = new
            {
                type = "input_audio_buffer.append",
                audio = Convert.ToBase64String(audioData)
            };

            await SendJsonAsync(audioEvent);
        }

        public async Task SendTextAsync(string text)
        {
            if (!isConnected || string.IsNullOrEmpty(text)) return;

            var textEvent = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = text
                        }
                    }
                }
            };

            await SendJsonAsync(textEvent);

            // Trigger response generation
            var responseEvent = new
            {
                type = "response.create"
            };

            await SendJsonAsync(responseEvent);
        }



        private async Task SendJsonAsync(object data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var bytes = Encoding.UTF8.GetBytes(json);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationTokenSource.Token
                ).ConfigureAwait(false); // Critical: Stay on background thread
            }
            catch (Exception ex)
            {
                Debug.LogError($"RealtimeClient: Error sending message: {ex.Message}");

                // Thread-safe: Error callback to main thread
                UnityMainThreadDispatcher.EnqueueAction(() => {
                    OnError?.Invoke($"Send error: {ex.Message}");
                });
            }
        }

        #endregion

        #region Utility

        private void EnqueueMainThreadAction(System.Action action)
        {
            // Since we're processing in Update(), we can just invoke directly
            // In a more complex scenario, you might want to use a proper main thread dispatcher
            action?.Invoke();
        }

        #endregion

        #region Public API

        public void StartListening()
        {
            if (isConnected)
            {
                // This would typically trigger VAD or manual audio recording
                Debug.Log("RealtimeClient: Started listening for audio input");
            }
        }

        public void StopListening()
        {
            if (isConnected)
            {
                // Simply create response - no manual audio buffer commit needed
                // The official OpenAI implementation only calls createResponse()
                CreateResponse();
                Debug.Log("RealtimeClient: Stopped listening, creating response");
            }
        }
        
        /// <summary>
        /// Create a response from the current conversation context
        /// This replaces the manual audio buffer commit pattern
        /// </summary>
        public void CreateResponse()
        {
            if (!isConnected) return;
            
            var responseEvent = new { type = "response.create" };
            _ = SendJsonAsync(responseEvent);
            Debug.Log("RealtimeClient: Creating response");
        }

        public void SendUserMessage(string message)
        {
            if (isConnected)
            {
                _ = SendTextAsync(message);
            }
        }

        /// <summary>
        /// Synchronous wrapper for ConnectAsync() - Unity UI Button compatible
        /// </summary>
        public void Connect()
        {
            if (!isConnected && !isConnecting)
            {
                _ = ConnectAsync();
            }
        }

        /// <summary>
        /// Synchronous wrapper for DisconnectAsync() - Unity UI Button compatible
        /// </summary>
        public void Disconnect()
        {
            if (isConnected)
            {
                _ = DisconnectAsync();
            }
        }

        /// <summary>
        /// Get voice name using Runtime Override System (runtime override takes precedence over settings)
        /// </summary>
        private string GetVoiceNameFromSettings(OpenAISettings settings)
        {
            // Runtime override takes precedence
            if (runtimeVoiceOverride.HasValue)
            {
                string runtimeVoice = runtimeVoiceOverride.Value.ToApiString();
                Debug.Log($"[RealtimeClient] Using runtime voice override: {runtimeVoiceOverride.Value} -> {runtimeVoice}");
                return runtimeVoice;
            }
            
            // Fallback to settings
            if (settings == null) 
            {
                Debug.LogWarning("[RealtimeClient] Settings is null, using default voice 'alloy'");
                return "alloy";
            }

            Debug.Log($"[RealtimeClient] Using voice from settings: {settings.Voice} -> {settings.VoiceName}");
            return settings.VoiceName; // Uses the new VoiceName property which calls ToApiString()
        }

        /// <summary>
        /// Validates transcription model and returns a valid one
        /// Available models: whisper-1 (faster, good quality), whisper-large-v3 (highest quality, slower)
        /// </summary>
        private string GetValidTranscriptionModel(string model)
        {
            string[] validModels = { "whisper-1", "whisper-large-v3" };
            if (!string.IsNullOrEmpty(model) && System.Array.Exists(validModels, m => m == model))
            {
                return model;
            }
            
            Debug.LogWarning($"[RealtimeClient] Invalid transcription model '{model}', using default 'whisper-1'");
            return "whisper-1";
        }

        /// <summary>
        /// Resets all session state flags to ensure clean restart
        /// </summary>
        public void ResetSessionState()
        {
            isAwaitingResponse = false;
            Debug.Log("[RealtimeClient] Session state reset - ready for new conversation");
        }

        /// <summary>
        /// Force reset connection state - use when connection state is inconsistent
        /// </summary>
        public void ForceResetConnectionState()
        {
            isConnected = false;
            isConnecting = false;
            isAwaitingResponse = false;
            Debug.Log("[RealtimeClient] Connection state force reset - ready for fresh connection");
        }

        #region Runtime Voice Override System
        
        /// <summary>
        /// Set voice for runtime use (overrides OpenAISettings default)
        /// </summary>
        public void SetRuntimeVoice(OpenAIVoice voice)
        {
            runtimeVoiceOverride = voice;
            Debug.Log($"[RealtimeClient] Runtime voice override set to: {voice} ({voice.ToApiString()})");
        }
        
        /// <summary>
        /// Clear runtime voice override (use OpenAISettings default)
        /// </summary>
        public void ClearRuntimeVoice()
        {
            runtimeVoiceOverride = null;
            Debug.Log("[RealtimeClient] Runtime voice override cleared - using OpenAISettings default");
        }
        
        /// <summary>
        /// Get current effective voice (runtime override or settings default)
        /// </summary>
        public OpenAIVoice GetCurrentVoice()
        {
            if (runtimeVoiceOverride.HasValue)
            {
                return runtimeVoiceOverride.Value;
            }
            
            return settings?.Voice ?? OpenAIVoice.alloy;
        }
        
        /// <summary>
        /// Get current effective voice as API string
        /// </summary>
        public string GetCurrentVoiceApiString()
        {
            return GetCurrentVoice().ToApiString();
        }
        
        #endregion
    }
}
#endregion
