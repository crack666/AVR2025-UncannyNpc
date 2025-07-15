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
            if (autoConnect && settings != null && !string.IsNullOrEmpty(settings.ApiKey))
            {
                _ = ConnectAsync();
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
                var error = "OpenAI API key not configured";
                Debug.LogError($"RealtimeClient: {error}");
                OnError?.Invoke(error);
                return false;
            }

            isConnecting = true;
            currentRetries = 0;

            while (currentRetries < maxRetries && !isConnected)
            {
                try
                {
                    await AttemptConnection();
                    break;
                }
                catch (Exception ex)
                {
                    currentRetries++;
                    Debug.LogError($"RealtimeClient: Connection attempt {currentRetries} failed: {ex.Message}");

                    if (currentRetries < maxRetries)
                    {
                        Debug.Log($"RealtimeClient: Retrying in {retryDelay} seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(retryDelay));
                    }
                    else
                    {
                        OnError?.Invoke($"Failed to connect after {maxRetries} attempts: {ex.Message}");
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
            // Clean up any existing connection
            if (webSocket != null)
            {
                webSocket.Dispose();
            }

            cancellationTokenSource = new CancellationTokenSource();
            webSocket = new ClientWebSocket();

            // Configure headers
            webSocket.Options.SetRequestHeader("Authorization", $"Bearer {settings.ApiKey}");
            webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

            // Connect to OpenAI Realtime API
            var uri = new Uri("wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01");
            await webSocket.ConnectAsync(uri, cancellationTokenSource.Token);

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
                        model = "whisper-1"
                    },
                    turn_detection = new
                    {
                        type = "server_vad",
                        threshold = 0.5,
                        prefix_padding_ms = 300,
                        silence_duration_ms = 200
                    }
                }
            };

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
        /// Convert VoiceIndex from OpenAISettings to voice name string
        /// </summary>
        private string GetVoiceNameFromSettings(OpenAISettings settings)
        {
            if (settings == null) return "alloy";

            int voiceIndex = settings.VoiceIndex;
            string[] voiceNames = { "alloy", "ash", "ballad", "coral", "echo", "sage", "shimmer", "verse" };

            if (voiceIndex >= 0 && voiceIndex < voiceNames.Length)
            {
                return voiceNames[voiceIndex];
            }

            Debug.LogWarning($"[RealtimeClient] Invalid voice index {voiceIndex}, using default 'alloy'");
            return "alloy";
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
    }
}
#endregion
