using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace OpenAI.RealtimeAPI
{
    /// <summary>
    /// Production-ready WebSocket client for OpenAI Realtime API
    /// Uses System.Net.WebSockets for real WebSocket connection
    /// </summary>
    public class RealtimeClientV2 : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private OpenAISettings settings;
        [SerializeField] private bool autoConnect = false;
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private float retryDelay = 5f;
        
        [Header("Events")]
        public UnityEvent<string> OnMessageReceived;
        public UnityEvent<AudioChunk> OnAudioReceived;
        public UnityEvent<string> OnTextReceived;
        public UnityEvent OnConnected;
        public UnityEvent OnDisconnected;
        public UnityEvent<string> OnError;
        
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
        private ConversationState conversationState;
        
        public bool IsConnected => isConnected;
        public string SessionId => sessionId;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            conversationState = new ConversationState();
            
            // Initialize events if not set
            OnMessageReceived ??= new UnityEvent<string>();
            OnAudioReceived ??= new UnityEvent<AudioChunk>();
            OnTextReceived ??= new UnityEvent<string>();
            OnConnected ??= new UnityEvent();
            OnDisconnected ??= new UnityEvent();
            OnError ??= new UnityEvent<string>();
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
            if (pauseStatus && isConnected)
            {
                _ = DisconnectAsync();
            }
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
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messageChunk);
                        
                        if (result.EndOfMessage)
                        {
                            var completeMessage = messageBuilder.ToString();
                            messageBuilder.Clear();
                            
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
                EnqueueMainThreadAction(() => OnError?.Invoke($"Receive error: {ex.Message}"));
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
                
                switch (eventData.type)
                {
                    case "response.audio.delta":
                        HandleAudioDelta(eventData);
                        break;
                    case "response.text.delta":
                        HandleTextDelta(eventData);
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
                OnError?.Invoke($"Message processing error: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleAudioDelta(RealtimeEvent eventData)
        {
            if (eventData.delta != null && !string.IsNullOrEmpty(eventData.delta))
            {
                try
                {
                    var audioBytes = Convert.FromBase64String(eventData.delta);
                    var audioChunk = new AudioChunk
                    {
                        data = audioBytes,
                        format = AudioFormat.PCM16,
                        sampleRate = 24000,
                        channels = 1
                    };
                    
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
            Debug.Log($"RealtimeClient: Conversation item created: {eventData.item?.id}");
        }
        
        private void HandleError(RealtimeEvent eventData)
        {
            var errorMsg = eventData.error?.message ?? "Unknown error";
            Debug.LogError($"RealtimeClient: API Error: {errorMsg}");
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
                    voice = settings?.VoiceModel ?? "alloy",
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
        
        public async Task CommitAudioBuffer()
        {
            if (!isConnected) return;
            
            var commitEvent = new
            {
                type = "input_audio_buffer.commit"
            };
            
            await SendJsonAsync(commitEvent);
            
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
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"RealtimeClient: Error sending message: {ex.Message}");
                OnError?.Invoke($"Send error: {ex.Message}");
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
                _ = CommitAudioBuffer();
                Debug.Log("RealtimeClient: Stopped listening, committed audio buffer");
            }
        }
        
        public void SendUserMessage(string message)
        {
            if (isConnected)
            {
                _ = SendTextAsync(message);
            }
        }
        
        #endregion
    }
}
