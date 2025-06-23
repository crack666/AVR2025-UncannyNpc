using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

// HINWEIS: Für die finale Implementierung benötigen wir eine WebSocket-Bibliothek
// Hier verwenden wir eine abstrakte Implementierung als Placeholder
// Empfohlene Pakete: WebSocketSharp-netstandard oder Unity's com.unity.netcode.gameobjects

namespace OpenAI.RealtimeAPI
{
    /// <summary>
    /// Unity WebSocket Client für OpenAI Realtime API
    /// Verwaltet die Verbindung und Message-Handling
    /// </summary>
    public class RealtimeClient : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private OpenAISettings settings;
        
        [Header("Events")]
        public UnityEvent OnConnected;
        public UnityEvent OnDisconnected;
        public UnityEvent<string> OnError;
        public UnityEvent<AudioClip> OnAudioReceived;
        public UnityEvent<string> OnTextReceived;
        public UnityEvent<string> OnMessageReceived;
        
        // Private Fields
        private IWebSocket webSocket; // Interface für WebSocket-Implementierung
        private ConversationState conversationState;
        private Queue<string> messageQueue;
        private Queue<AudioChunk> audioQueue;
        private bool isInitialized;
        
        // Audio Buffer für eingehende Audio-Chunks
        private List<byte> audioBuffer;
        private string currentResponseId;
        
        // Properties
        public bool IsConnected => conversationState?.isConnected ?? false;
        public ConversationState State => conversationState;
        public OpenAISettings Settings => settings;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            conversationState = new ConversationState();
            messageQueue = new Queue<string>();
            audioQueue = new Queue<AudioChunk>();
            audioBuffer = new List<byte>();
            
            // Lade Settings falls nicht zugewiesen
            if (settings == null)
            {
                settings = Resources.Load<OpenAISettings>("OpenAISettings");
            }
        }
        
        private void Start()
        {
            if (settings == null)
            {
                LogError("OpenAISettings not found! Please create and assign OpenAISettings asset.");
                return;
            }
            
            if (!settings.IsValid())
            {
                LogError("OpenAISettings configuration is invalid!");
                return;
            }
            
            isInitialized = true;
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            ProcessMessageQueue();
            ProcessAudioQueue();
        }
        
        private void OnDestroy()
        {
            DisconnectAsync();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Verbindet zum OpenAI Realtime API WebSocket
        /// </summary>
        public async void ConnectAsync()
        {
            if (IsConnected)
            {
                LogWarning("Already connected to OpenAI Realtime API");
                return;
            }
            
            if (!isInitialized)
            {
                LogError("RealtimeClient not properly initialized");
                return;
            }
            
            try
            {
                Log("Connecting to OpenAI Realtime API...");
                
                // TODO: Echte WebSocket-Implementierung
                // webSocket = new WebSocketSharp.WebSocket(settings.GetWebSocketUrl());
                // webSocket.SetCredentials("Bearer", settings.ApiKey, true);
                
                // Simuliere Connection für Entwicklung
                StartCoroutine(SimulateConnection());
                
            }
            catch (Exception e)
            {
                LogError($"Failed to connect: {e.Message}");
                OnError?.Invoke(e.Message);
            }
        }
        
        /// <summary>
        /// Trennt die WebSocket-Verbindung
        /// </summary>
        public void DisconnectAsync()
        {
            if (!IsConnected) return;
            
            try
            {
                Log("Disconnecting from OpenAI Realtime API...");
                
                // TODO: Echte WebSocket-Implementierung
                // webSocket?.Close();
                
                conversationState.Reset();
                ClearQueues();
                
                OnDisconnected?.Invoke();
                Log("Disconnected successfully");
            }
            catch (Exception e)
            {
                LogError($"Error during disconnect: {e.Message}");
            }
        }
        
        /// <summary>
        /// Sendet Audio-Chunk an die API
        /// </summary>
        public void SendAudioChunk(AudioChunk audioChunk)
        {
            if (!IsConnected)
            {
                LogWarning("Cannot send audio - not connected");
                return;
            }
            
            if (!audioChunk.IsValid())
            {
                LogWarning("Invalid audio chunk");
                return;
            }
            
            try
            {
                var audioEvent = new InputAudioBufferAppendEvent
                {
                    audio = audioChunk.ToBase64()
                };
                
                SendEvent(audioEvent);
                
                conversationState.totalAudioChunksSent++;
                conversationState.lastAudioInputTime = Time.time;
                
                if (settings.LogAudioData)
                {
                    Log($"Sent audio chunk: {audioChunk.audioData.Length} bytes");
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to send audio chunk: {e.Message}");
            }
        }
        
        /// <summary>
        /// Sendet Text-Message an die API
        /// </summary>
        public void SendTextMessage(string message)
        {
            if (!IsConnected)
            {
                LogWarning("Cannot send text - not connected");
                return;
            }
            
            if (string.IsNullOrEmpty(message))
            {
                LogWarning("Cannot send empty message");
                return;
            }
            
            try
            {
                var conversationItem = new ConversationItemCreateEvent
                {
                    item = new ConversationItem
                    {
                        id = Guid.NewGuid().ToString(),
                        type = "message",
                        role = "user",
                        content = new ContentPart[]
                        {
                            new ContentPart
                            {
                                type = "input_text",
                                text = message
                            }
                        }
                    }
                };
                
                SendEvent(conversationItem);
                
                // Trigger response generation
                var responseEvent = new ResponseCreateEvent();
                SendEvent(responseEvent);
                
                conversationState.isWaitingForResponse = true;
                
                Log($"Sent text message: {message}");
            }
            catch (Exception e)
            {
                LogError($"Failed to send text message: {e.Message}");
            }
        }
        
        /// <summary>
        /// Committed Audio Buffer (signalisiert Ende der Eingabe)
        /// </summary>
        public void CommitAudioBuffer()
        {
            if (!IsConnected) return;
            
            try
            {
                var commitEvent = new InputAudioBufferCommitEvent();
                SendEvent(commitEvent);
                
                // Trigger response generation
                var responseEvent = new ResponseCreateEvent();
                SendEvent(responseEvent);
                
                conversationState.isWaitingForResponse = true;
                
                Log("Audio buffer committed");
            }
            catch (Exception e)
            {
                LogError($"Failed to commit audio buffer: {e.Message}");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void SendEvent(RealtimeEvent eventData)
        {
            if (webSocket == null) return;
            
            try
            {
                string json = JsonConvert.SerializeObject(eventData);
                
                // TODO: Echte WebSocket-Implementierung
                // webSocket.Send(json);
                
                if (settings.EnableDebugLogging)
                {
                    Log($"Sent: {eventData.type}");
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to send event {eventData.type}: {e.Message}");
            }
        }
        
        private void ProcessMessageQueue()
        {
            while (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                ProcessMessage(message);
            }
        }
        
        private void ProcessAudioQueue()
        {
            while (audioQueue.Count > 0)
            {
                AudioChunk chunk = audioQueue.Dequeue();
                ProcessAudioChunk(chunk);
            }
        }
        
        private void ProcessMessage(string message)
        {
            try
            {
                var eventData = JsonConvert.DeserializeObject<RealtimeEvent>(message);
                
                switch (eventData.type)
                {
                    case "session.created":
                        HandleSessionCreated(message);
                        break;
                        
                    case "response.audio.delta":
                        HandleAudioDelta(message);
                        break;
                        
                    case "response.audio.done":
                        HandleAudioDone(message);
                        break;
                        
                    case "response.text.delta":
                        HandleTextDelta(message);
                        break;
                        
                    case "error":
                        HandleError(message);
                        break;
                        
                    default:
                        if (settings.EnableDebugLogging)
                        {
                            Log($"Unhandled event type: {eventData.type}");
                        }
                        break;
                }
                
                OnMessageReceived?.Invoke(message);
            }
            catch (Exception e)
            {
                LogError($"Failed to process message: {e.Message}");
            }
        }
        
        private void HandleSessionCreated(string message)
        {
            Log("Session created successfully");
            conversationState.isConnected = true;
            OnConnected?.Invoke();
        }
        
        private void HandleAudioDelta(string message)
        {
            try
            {
                var audioEvent = JsonConvert.DeserializeObject<ResponseAudioDeltaEvent>(message);
                
                if (!string.IsNullOrEmpty(audioEvent.delta))
                {
                    byte[] audioData = Convert.FromBase64String(audioEvent.delta);
                    audioBuffer.AddRange(audioData);
                    
                    currentResponseId = audioEvent.response_id;
                    conversationState.totalAudioChunksReceived++;
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to handle audio delta: {e.Message}");
            }
        }
        
        private void HandleAudioDone(string message)
        {
            try
            {
                if (audioBuffer.Count > 0)
                {
                    // Konvertiere akkumulierte Audio-Daten zu AudioClip
                    AudioClip clip = CreateAudioClip(audioBuffer.ToArray());
                    
                    if (clip != null)
                    {
                        OnAudioReceived?.Invoke(clip);
                        conversationState.lastAudioOutputTime = Time.time;
                        
                        // Berechne Latenz
                        float latency = Time.time - conversationState.lastAudioInputTime;
                        conversationState.UpdateLatency(latency);
                    }
                    
                    audioBuffer.Clear();
                }
                
                conversationState.isWaitingForResponse = false;
                Log("Audio response completed");
            }
            catch (Exception e)
            {
                LogError($"Failed to handle audio done: {e.Message}");
            }
        }
        
        private void HandleTextDelta(string message)
        {
            try
            {
                var textEvent = JsonConvert.DeserializeObject<ResponseTextDeltaEvent>(message);
                
                if (!string.IsNullOrEmpty(textEvent.delta))
                {
                    OnTextReceived?.Invoke(textEvent.delta);
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to handle text delta: {e.Message}");
            }
        }
        
        private void HandleError(string message)
        {
            try
            {
                var errorEvent = JsonConvert.DeserializeObject<ErrorEvent>(message);
                string errorMessage = $"API Error: {errorEvent.error.type} - {errorEvent.error.message}";
                
                LogError(errorMessage);
                OnError?.Invoke(errorMessage);
            }
            catch (Exception e)
            {
                LogError($"Failed to handle error event: {e.Message}");
            }
        }
        
        private AudioClip CreateAudioClip(byte[] pcmData)
        {
            try
            {
                float[] samples = AudioChunk.PCM16ToFloat(pcmData);
                
                AudioClip clip = AudioClip.Create(
                    "RealtimeAudio",
                    samples.Length,
                    1, // Mono
                    settings.SampleRate,
                    false
                );
                
                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception e)
            {
                LogError($"Failed to create audio clip: {e.Message}");
                return null;
            }
        }
        
        private void ProcessAudioChunk(AudioChunk chunk)
        {
            // Verarbeite eingehende Audio-Chunks
            // Hier können weitere Audio-Processing-Schritte implementiert werden
        }
        
        private void ClearQueues()
        {
            messageQueue.Clear();
            audioQueue.Clear();
            audioBuffer.Clear();
        }
        
        // Simulation für Entwicklung - wird durch echte WebSocket-Implementierung ersetzt
        private IEnumerator SimulateConnection()
        {
            yield return new WaitForSeconds(1f);
            
            Log("Simulated connection established");
            conversationState.isConnected = true;
            OnConnected?.Invoke();
            
            // Simuliere Session Created Event
            HandleSessionCreated("{}");
        }
        
        #endregion
        
        #region Logging
        
        private void Log(string message)
        {
            if (settings.EnableDebugLogging)
            {
                Debug.Log($"[RealtimeClient] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            if (settings.EnableDebugLogging)
            {
                Debug.LogWarning($"[RealtimeClient] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[RealtimeClient] {message}");
        }
        
        #endregion
    }
    
    // Interface für WebSocket-Abstraktion
    public interface IWebSocket
    {
        void Connect();
        void Close();
        void Send(string message);
        event System.Action OnOpen;
        event System.Action OnClose;
        event System.Action<string> OnMessage;
        event System.Action<string> OnError;
    }
}
