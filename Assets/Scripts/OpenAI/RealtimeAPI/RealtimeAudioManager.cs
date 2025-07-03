using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using OpenAI.Threading;

namespace OpenAI.RealtimeAPI
{
    /// <summary>
    /// Verwaltet Audio-Eingabe und -Ausgabe für die Realtime API
    /// Handles Microphone Input, Audio Processing und Playback
    /// </summary>
    public class RealtimeAudioManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private OpenAISettings settings;
        [SerializeField] private RealtimeClient realtimeClient;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource microphoneAudioSource;
        [SerializeField] private AudioSource playbackAudioSource;
        
        [Header("Microphone Settings")]
        [SerializeField] private bool useDefaultMicrophone = true;
        [SerializeField] private string specificMicrophone = "";
        [SerializeField] private float microphoneGain = 1.0f;
        [SerializeField] private bool enableNoiseGate = true;
        [SerializeField] private float noiseGateThreshold = 0.01f;
        
        [Header("Audio Playback Settings")]
        [SerializeField] private int streamBufferSize = 1024; // FIXED: Increased from 128 to 1024 for smooth playback
        
        [Header("Adaptive Audio Settings")]
        [SerializeField] private bool useAdaptiveBuffering = false; // TEMPORARILY DISABLED - causing buffer issues
        [SerializeField] private int[] adaptiveBufferSizes = { 512, 1024, 2048, 4096 }; // Buffer size options for adaptation
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float performanceCheckInterval = 5.0f; // Check every 5 seconds
        
        [Header("Events")]
        public UnityEvent OnRecordingStarted;
        public UnityEvent OnRecordingStopped;
        public UnityEvent OnAudioPlaybackStarted;
        public UnityEvent OnAudioPlaybackFinished;
        public UnityEvent<float> OnMicrophoneLevelChanged;
        
        // Private Fields
        private AudioClip microphoneClip;
        private bool isRecording;
        private bool isInitialized;
        private string currentMicrophone;
        private int lastMicrophonePosition;
        private float[] microphoneBuffer;
        private int bufferPosition;        
        // Audio Processing
        private float[] processingBuffer;
        private int chunkSampleCount;
        private Coroutine recordingCoroutine;
        
        // Adaptive Audio Performance Management
        private int currentBufferSizeIndex = 1; // Start with 1024
        private float lastPerformanceCheck = 0f;
        private int audioDropoutCount = 0;
        private int performanceCheckCount = 0;
        
        // === GAPLESS STREAMING IMPLEMENTATION (OpenAI Reference) ===
        private Queue<StreamBuffer> streamOutputBuffers = new Queue<StreamBuffer>();
        private StreamBuffer currentStreamWriteBuffer;
        private int streamWriteOffset = 0;
        private bool streamHasStarted = false;
        private bool streamHasInterrupted = false;
        private Dictionary<string, int> streamTrackSampleOffsets = new Dictionary<string, int>();
        private AudioClip streamAudioClip;
        private float[] streamAudioData;
        private float lastBufferAddTime = 0f; // Track when last buffer was added
        private const int STREAM_LENGTH_SECONDS = 30; // Rolling buffer
        private readonly object streamLock = new object();
        
        private struct StreamBuffer
        {
            public float[] buffer;
            public string trackId;
            
            public StreamBuffer(float[] data, string id)
            {
                buffer = data;
                trackId = id;
            }
        }
        
        // Public Properties
        public bool IsRecording => isRecording;
        public int BufferSampleCount => bufferPosition;
        public string CurrentMicrophone => currentMicrophone;


        
        // Thread-safe async recording stop
        private async void TriggerStopRecordingAsync()
        {
            try
            {
                if (isRecording)
                {
                    await StopRecordingAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealtimeAudioManager] Error in TriggerStopRecordingAsync: {ex.Message}");
            }
        }

        // Async version that doesn't block main thread
        public async System.Threading.Tasks.Task StopRecordingAsync()
        {
            if (!isRecording) return;

            try
            {
                isRecording = false;
                
                // Stop coroutine and microphone on main thread
                if (recordingCoroutine != null)
                {
                    StopCoroutine(recordingCoroutine);
                    recordingCoroutine = null;
                }
                Microphone.End(currentMicrophone);
                
                if (microphoneClip != null)
                {
                    DestroyImmediate(microphoneClip);
                    microphoneClip = null;
                }

                byte[] pcmData = null;
                if (bufferPosition > 0)
                {
                    int minSamples = settings.SampleRate / 20; // 50ms minimum
                    if (bufferPosition >= minSamples)
                    {
                        float[] lastSamples = new float[bufferPosition];
                        Array.Copy(microphoneBuffer, lastSamples, bufferPosition);
                        
                        // Background thread: Convert and send audio FIRST
                        pcmData = await System.Threading.Tasks.Task.Run(() => 
                            AudioChunk.FloatToPCM16(lastSamples, bufferPosition)
                        ).ConfigureAwait(false);
                        
                        AudioChunk chunk = new AudioChunk(pcmData, settings.SampleRate, 1);
                        await realtimeClient.SendAudioAsync(chunk.audioData).ConfigureAwait(false);
                        
                        // Wait for audio to be processed by API
                        await System.Threading.Tasks.Task.Delay(150).ConfigureAwait(false);
                        
                    }
                    bufferPosition = 0;
                }

                // SIMPLIFIED: Following official OpenAI pattern - no manual commit needed
                // The RealtimeClient.StopListening() will call CreateResponse() instead
                
                // No manual commit - let StopListening() handle response creation

                // Main thread: Trigger Unity events
                UnityMainThreadDispatcher.EnqueueAction(() => {
                    OnRecordingStopped?.Invoke();
                    Log("Recording stopped");
                });
            }
            catch (Exception e)
            {
                LogError($"Failed to stop recording: {e.Message}");
            }
        }




        /// Forces complete stop of all recording and resets state
        /// Used for session restarts and disconnect operations
        /// </summary>
        public void ForceStopAllRecording()
        {
            Debug.Log("[RealtimeAudioManager] ForceStopAllRecording: Forcing complete stop of all audio operations");
            
            try
            {
                // Force stop recording immediately
                isRecording = false;
                
                // Stop ALL microphones, not just current one
                string[] devices = Microphone.devices;
                foreach (string device in devices)
                {
                    if (Microphone.IsRecording(device))
                    {
                        Microphone.End(device);
                    }
                }
                
                // Also try stopping with null (default device)
                if (Microphone.IsRecording(null))
                {
                    Microphone.End(null);
                }
                
                // Stop coroutine
                if (recordingCoroutine != null)
                {
                    StopCoroutine(recordingCoroutine);
                    recordingCoroutine = null;
                }
                
                // Cleanup microphone clip
                if (microphoneClip != null)
                {
                    DestroyImmediate(microphoneClip);
                    microphoneClip = null;
                }
                
                // Reset all audio state
                bufferPosition = 0;
                lastMicrophonePosition = 0;
                
                // Clear audio streams
                StopGaplessStreaming();
                
                Debug.Log("[RealtimeAudioManager] ForceStopAllRecording: All audio operations stopped and state reset");
            }
            catch (Exception ex)
            {
                LogError($"Error in ForceStopAllRecording: {ex.Message}");
            }
        }

        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Lade Settings falls nicht zugewiesen
            if (settings == null)
            {
                settings = Resources.Load<OpenAISettings>("OpenAISettings");
            }
              // Finde RealtimeClient falls nicht zugewiesen
            if (realtimeClient == null)
            {
                realtimeClient = FindFirstObjectByType<RealtimeClient>();
            }
        }
        
        private void Start()
        {
            InitializeAudioManager();
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Monitor audio performance if enabled (but NOT adaptive buffering)
            if (enablePerformanceMonitoring && !useAdaptiveBuffering)
            {
                MonitorAudioPerformance();
            }
            
            // ZUSÄTZLICHE SICHERUNG: Prüfe ob AudioSource aufgehört hat zu spielen
            // aber das System denkt noch, dass Audio läuft
            if (streamHasStarted && playbackAudioSource != null && 
                !playbackAudioSource.isPlaying && streamOutputBuffers.Count == 0)
            {
                Log("[GAPLESS] SAFETY CHECK: AudioSource stopped but stream still active - triggering finish event");
                streamHasStarted = false;
                OnAudioPlaybackFinished?.Invoke();
            }
            
            // SMART COMPLETION DETECTION: If stream was started but no new buffers for a while
            if (streamHasStarted && streamOutputBuffers.Count == 0 && Time.time - lastBufferAddTime > 1.0f)
            {
                Log("[GAPLESS] Stream appears complete - no new buffers for 1 second, triggering finish");
                streamHasStarted = false;
                OnAudioPlaybackFinished?.Invoke();
            }
            
            
            if (isRecording)
            {
                ProcessMicrophoneInput();
            }
        }
        
        private void OnDestroy()
        {
            StopRecording();
            // Stoppe alle Playbacks
            StopGaplessStreaming();
            
            // Cleanup Gapless Streaming
            StopGaplessStreaming();
            if (microphoneAudioSource != null && microphoneAudioSource.gameObject != this.gameObject)
            {
                LogWarning($"Destroying dynamically created MicrophoneAudioSource: {microphoneAudioSource.gameObject.name}");
                DestroyImmediate(microphoneAudioSource.gameObject);
                microphoneAudioSource = null;
            }
            if (playbackAudioSource != null && playbackAudioSource.gameObject != this.gameObject)
            {
                LogWarning($"Destroying dynamically created PlaybackAudioSource: {playbackAudioSource.gameObject.name}");
                DestroyImmediate(playbackAudioSource.gameObject);
                playbackAudioSource = null;
            }
        }
        
        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeAudioManager()
        {
            if (settings == null)
            {
                Debug.LogError("[RealtimeAudioManager] OpenAISettings not found!");
                return;
            }
            
            if (realtimeClient == null)
            {
                Debug.LogError("[RealtimeAudioManager] RealtimeClient not found!");
                return;
            }
            
            // Setup Audio Sources
            SetupAudioSources();
            
            // Initialize Microphone
            InitializeMicrophone();
            
            // Setup Audio Buffers
            SetupAudioBuffers();
            
            // Setup Gapless Streaming
            SetupGaplessStreaming();
            
            // Subscribe to RealtimeClient events
            realtimeClient.OnAudioReceived.AddListener(PlayReceivedAudioChunk);
            
            isInitialized = true;
            
            Log("Audio Manager initialized successfully");
        }
        
        private void SetupAudioSources()
        {
            // Hinweis: Es wird empfohlen, die AudioSources im Editor fest zu verdrahten und im Inspector zuzuweisen.
            // Nur wenn keine zugewiesen ist, wird eine neue erzeugt.
            if (microphoneAudioSource == null)
            {
                var existingMic = transform.Find("MicrophoneAudioSource");
                if (existingMic != null)
                {
                    microphoneAudioSource = existingMic.GetComponent<AudioSource>();
                    Log("Found existing MicrophoneAudioSource child.");
                }
                else
                {
                    GameObject micObject = new GameObject("MicrophoneAudioSource");
                    micObject.transform.SetParent(transform);
                    microphoneAudioSource = micObject.AddComponent<AudioSource>();
                    LogWarning("Created new MicrophoneAudioSource dynamically. Für maximale Kontrolle im Editor zuweisen!");
                }
            }
            microphoneAudioSource.loop = true;
            microphoneAudioSource.mute = true;
            microphoneAudioSource.volume = 0f;

            if (playbackAudioSource == null)
            {
                var existingPlayback = transform.Find("PlaybackAudioSource");
                if (existingPlayback != null)
                {
                    playbackAudioSource = existingPlayback.GetComponent<AudioSource>();
                    Log("Found existing PlaybackAudioSource child.");
                }
                else
                {
                    GameObject playbackObject = new GameObject("PlaybackAudioSource");
                    playbackObject.transform.SetParent(transform);
                    playbackAudioSource = playbackObject.AddComponent<AudioSource>();
                    LogWarning("Created new PlaybackAudioSource dynamically. Für maximale Kontrolle im Editor zuweisen!");
                }
            }
            playbackAudioSource.loop = false;
            playbackAudioSource.volume = 1f;
        }
        
        private void InitializeMicrophone()
        {
            // Wähle Mikrophone
            currentMicrophone = useDefaultMicrophone ? null : specificMicrophone;
            
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[RealtimeAudioManager] No microphone devices found!");
                return;
            }
            
            // Log verfügbare Mikrofone
            Log($"Available microphones: {string.Join(", ", Microphone.devices)}");
            
            if (!useDefaultMicrophone && !string.IsNullOrEmpty(specificMicrophone))
            {
                bool microphoneFound = false;
                foreach (string device in Microphone.devices)
                {
                    if (device == specificMicrophone)
                    {
                        microphoneFound = true;
                        break;
                    }
                }
                
                if (!microphoneFound)
                {
                    LogWarning($"Specified microphone '{specificMicrophone}' not found. Using default.");
                    currentMicrophone = null;
                }
            }
            
            Log($"Using microphone: {currentMicrophone ?? "Default"}");
        }
        
        private void SetupAudioBuffers()
        {
            chunkSampleCount = (settings.SampleRate * settings.AudioChunkSizeMs) / 1000;
            microphoneBuffer = new float[chunkSampleCount * 2]; // Double buffer
            processingBuffer = new float[chunkSampleCount];
            
            Log($"Audio buffer size: {chunkSampleCount} samples ({settings.AudioChunkSizeMs}ms)");
        }
        
        /// <summary>
        /// Setup Gapless Streaming System (based on OpenAI Reference)
        /// </summary>
        private void SetupGaplessStreaming()
        {
            int sampleRate = settings?.SampleRate ?? 24000;
            int totalSamples = sampleRate * STREAM_LENGTH_SECONDS;
            streamAudioData = new float[totalSamples];
            
            // Create rolling audio stream clip with OnAudioRead callback
            streamAudioClip = AudioClip.Create("GaplessStream", totalSamples, 1, sampleRate, true, OnAudioRead);
            
            // Reset stream state
            streamHasStarted = false;
            streamHasInterrupted = false;
            streamWriteOffset = 0;
            lastBufferAddTime = Time.time; // Initialize buffer timer
            
            Log($"[GAPLESS] Stream initialized: {sampleRate}Hz, {totalSamples} samples, {STREAM_LENGTH_SECONDS}s buffer");
        }
        
        /// <summary>
        /// Unity's OnAudioRead callback - equivalent to AudioWorkletProcessor.process()
        /// This is called from Unity's audio thread for gapless playback
        /// </summary>
        private void OnAudioRead(float[] data)
        {
            lock (streamLock)
            {
                if (streamHasInterrupted)
                {
                    // Stream stopped - output silence
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = 0f;
                    }
                    return;
                }
                
                // CRITICAL FIX: Fill the ENTIRE data array, not just one buffer
                int dataIndex = 0;
                
                while (dataIndex < data.Length && streamOutputBuffers.Count > 0)
                {
                    streamHasStarted = true;
                    StreamBuffer buffer = streamOutputBuffers.Dequeue();
                    
                    // Copy as much as possible from this buffer
                    int samplesToCopy = Mathf.Min(buffer.buffer.Length, data.Length - dataIndex);
                    
                    for (int i = 0; i < samplesToCopy; i++)
                    {
                        data[dataIndex + i] = buffer.buffer[i];
                    }
                    
                    dataIndex += samplesToCopy;
                    
                    // Update track sample offsets
                    if (!string.IsNullOrEmpty(buffer.trackId))
                    {
                        if (!streamTrackSampleOffsets.ContainsKey(buffer.trackId))
                        {
                            streamTrackSampleOffsets[buffer.trackId] = 0;
                        }
                        streamTrackSampleOffsets[buffer.trackId] += samplesToCopy;
                    }
                    
                    // If we didn't use the whole buffer, we need to handle remaining data
                    if (samplesToCopy < buffer.buffer.Length)
                    {
                        // Create new buffer with remaining data
                        float[] remainingData = new float[buffer.buffer.Length - samplesToCopy];
                        System.Array.Copy(buffer.buffer, samplesToCopy, remainingData, 0, remainingData.Length);
                        StreamBuffer remainingBuffer = new StreamBuffer(remainingData, buffer.trackId);
                        
                        // Put it back at the front of the queue
                        var tempList = new List<StreamBuffer> { remainingBuffer };
                        tempList.AddRange(streamOutputBuffers);
                        streamOutputBuffers.Clear();
                        foreach (var b in tempList)
                        {
                            streamOutputBuffers.Enqueue(b);
                        }
                        break; // Data array is full
                    }
                }
                
                // Fill remaining space with silence if needed
                while (dataIndex < data.Length)
                {
                    data[dataIndex] = 0f;
                    dataIndex++;
                }
                
                // Debug only occasionally to avoid spam - BUT LOG FIRST FEW!
                if (streamOutputBuffers.Count % 50 == 0 || streamOutputBuffers.Count < 5)
                {
                    Log($"[GAPLESS] OnAudioRead filled {data.Length} samples. Remaining buffers: {streamOutputBuffers.Count}");
                }
                
                // Log when we run out of buffers - but DON'T immediately finish
                if (streamOutputBuffers.Count == 0 && streamHasStarted)
                {
                    Log("[GAPLESS] OnAudioRead: No buffers available, outputting silence - but keeping stream alive");
                    
                    // DON'T reset stream state here - this happens too early!
                    // The stream might just be temporarily empty but more audio is coming
                    // Only the explicit StopGaplessStreaming() should trigger the finished event
                }
                
                // Log if we have buffers but haven't started
                if (streamOutputBuffers.Count > 0 && !streamHasStarted)
                {
                    Log($"[GAPLESS] OnAudioRead: Have {streamOutputBuffers.Count} buffers but not started yet");
                }
            }
        }
        
        /// <summary>
        /// Write audio data to gapless stream (equivalent to writeData in OpenAI reference)
        /// </summary>
        private void WriteAudioDataToStream(float[] float32Array, string trackId = null)
        {
            lock (streamLock)
            {
                if (currentStreamWriteBuffer.buffer == null)
                {
                    currentStreamWriteBuffer = new StreamBuffer(new float[streamBufferSize], trackId);
                }
                
                float[] buffer = currentStreamWriteBuffer.buffer;
                int offset = streamWriteOffset;
                
                for (int i = 0; i < float32Array.Length; i++)
                {
                    buffer[offset++] = float32Array[i];
                    
                    if (offset >= buffer.Length)
                    {
                        // Buffer full - enqueue it
                        streamOutputBuffers.Enqueue(currentStreamWriteBuffer);
                        lastBufferAddTime = Time.time; // Track when we added a buffer
                        
                        // Create new buffer
                        currentStreamWriteBuffer = new StreamBuffer(new float[streamBufferSize], trackId);
                        buffer = currentStreamWriteBuffer.buffer;
                        offset = 0;
                        
                        //Log($"[GAPLESS] Buffer enqueued. Total buffers: {streamOutputBuffers.Count}");
                    }
                }
                
                streamWriteOffset = offset;
            }
        }
        
        /// <summary>
        /// Convert AudioChunk to stream format (equivalent to Add16BitPCM in OpenAI reference)
        /// </summary>
        private void AddAudioChunkToStream(AudioChunk audioChunk, string trackId = "default")
        {
            if (audioChunk == null || !audioChunk.IsValid()) return;
            
            // PREEMPTIVE: Start streaming BEFORE first chunk to prevent loss
            if (!playbackAudioSource.isPlaying || playbackAudioSource.clip != streamAudioClip)
            {
                StartGaplessStreaming();
                Log("[GAPLESS] Auto-started streaming on first chunk");
                
                // CRITICAL FIX: Wait one frame for Unity AudioSource to actually start
                // This prevents the first chunks from being lost
                if (!streamHasStarted)
                {
                    Log("[GAPLESS] Buffering first chunk while AudioSource starts up");
                }
            }
            
            byte[] pcmData = audioChunk.audioData;
            if (pcmData.Length % 2 != 0) return;
            
            // Convert Int16 PCM to Float32 (like StreamProcessor)
            int sampleCount = pcmData.Length / 2;
            float[] float32Array = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                short int16Sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
                float32Array[i] = int16Sample / 32768.0f; // Convert to -1.0 to 1.0
            }
            
            WriteAudioDataToStream(float32Array, trackId);
            lastBufferAddTime = Time.time; // Track when we added audio data
            
            //Log($"[GAPLESS] Added {sampleCount} samples to stream. Track: {trackId}");
        }
        
        /// <summary>
        /// Start gapless streaming
        /// </summary>
        private void StartGaplessStreaming()
        {
            if (playbackAudioSource == null)
            {
                LogError("[GAPLESS] StartGaplessStreaming failed: playbackAudioSource is null");
                return;
            }
            
            if (streamAudioClip == null)
            {
                LogError("[GAPLESS] StartGaplessStreaming failed: streamAudioClip is null");
                return;
            }
            
            // Stop any current playback
            if (playbackAudioSource.isPlaying)
            {
                playbackAudioSource.Stop();
                Log("[GAPLESS] Stopped current playback before starting stream");
            }
            
            playbackAudioSource.clip = streamAudioClip;
            playbackAudioSource.loop = true;
            playbackAudioSource.Play();
            
            streamHasStarted = false;
            streamHasInterrupted = false;
            
            Log($"[GAPLESS] Started gapless audio streaming. AudioSource.isPlaying: {playbackAudioSource.isPlaying}, Clip: {streamAudioClip.name}");
            
            // Force trigger audio events
            OnAudioPlaybackStarted?.Invoke();
        }
        
        /// <summary>
        /// Stop gapless streaming
        /// </summary>
        private void StopGaplessStreaming()
        {
            bool wasPlaying = streamHasStarted;
            
            lock (streamLock)
            {
                streamHasInterrupted = true;
                streamHasStarted = false;
                streamOutputBuffers.Clear();
                streamTrackSampleOffsets.Clear();
                streamWriteOffset = 0;
            }
            
            Log("[GAPLESS] Stopped gapless audio streaming");
            
            // Trigger finished event if stream was actually playing
            if (wasPlaying)
            {
                Log("[GAPLESS] Stream was playing - triggering OnAudioPlaybackFinished");
                OnAudioPlaybackFinished?.Invoke();
            }
        }
        
        #endregion
        
        #region Recording Control
        
        /// <summary>
        /// Startet Mikrophone-Aufnahme
        /// </summary>
        public void StartRecording()
        {
            if (!isInitialized)
            {
                LogWarning("Audio Manager not initialized");
                return;
            }
            
            if (isRecording)
            {
                LogWarning("Already recording");
                return;
            }
            
            try
            {
                // Starte Mikrophone-Aufnahme
                microphoneClip = Microphone.Start(
                    currentMicrophone,
                    true, // Loop
                    10,   // Length in seconds (wird geloopt)
                    settings.SampleRate
                );
                
                if (microphoneClip == null)
                {
                    LogError("Failed to start microphone");
                    return;
                }
                
                // Setup AudioSource mit Mikrophone
                microphoneAudioSource.clip = microphoneClip;
                microphoneAudioSource.loop = true;
                
                isRecording = true;
                lastMicrophonePosition = 0;
                bufferPosition = 0;
                
                // Starte Recording Coroutine
                recordingCoroutine = StartCoroutine(RecordingLoop());
                
                OnRecordingStarted?.Invoke();
                Log("Recording started");
            }
            catch (Exception e)
            {
                LogError($"Failed to start recording: {e.Message}");
            }
        }
        
        /// <summary>
        /// Stoppt Mikrophone-Aufnahme
        /// </summary>
        public void StopRecording()
        {
            // Fire and forget - no blocking
            _ = StopRecordingAsync();
        }
        
        #endregion
        
        #region Audio Processing
        
        private IEnumerator RecordingLoop()
        {
            while (isRecording)
            {
                yield return new WaitForEndOfFrame();
            }
        }
        
        private void ProcessMicrophoneInput()
        {
            if (microphoneClip == null) return;
            
            int currentPosition = Microphone.GetPosition(currentMicrophone);
            
            if (currentPosition < 0 || currentPosition == lastMicrophonePosition)
                return;
            
            // Handle wrap-around
            int sampleCount;
            if (currentPosition > lastMicrophonePosition)
            {
                sampleCount = currentPosition - lastMicrophonePosition;
            }
            else
            {
                sampleCount = (microphoneClip.samples - lastMicrophonePosition) + currentPosition;
            }
            
            if (sampleCount <= 0) return;
            
            // Lese neue Samples
            float[] samples = new float[sampleCount];
            microphoneClip.GetData(samples, lastMicrophonePosition);
            
            // Wende Gain an
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= microphoneGain * settings.MicrophoneVolume;
            }
            
            // Noise Gate
            if (enableNoiseGate)
            {
                ApplyNoiseGate(samples);
            }
            
            // Füge zu Buffer hinzu
            AddToBuffer(samples);
            
            lastMicrophonePosition = currentPosition;
        }
        
        private void AddToBuffer(float[] samples)
        {
            // SAFETY CHECK: Don't process if not recording or not connected
            if (!isRecording || realtimeClient == null || !realtimeClient.IsConnected)
            {
                Debug.LogWarning($"[RealtimeAudioManager] AddToBuffer: Ignoring samples - isRecording={isRecording}, isConnected={realtimeClient?.IsConnected}");
                return;
            }
            
            // REMOVED: This blocking causes lost audio samples
            // Allow buffer processing even during response
            
            //Debug.Log($"[RealtimeAudioManager] AddToBuffer called with {samples.Length} samples. BufferPos: {bufferPosition}/{microphoneBuffer.Length}");
            
            for (int i = 0; i < samples.Length; i++)
            {
                microphoneBuffer[bufferPosition] = samples[i];
                bufferPosition++;
                
                // Buffer full - send chunk (even during response)
                if (bufferPosition >= chunkSampleCount)
                {
                    Debug.Log($"[RealtimeAudioManager] Buffer full ({bufferPosition} samples), sending audio chunk.");
                    SendAudioChunk();
                    bufferPosition = 0;
                }
            }
        }
          private void SendAudioChunk()
        {
            if (realtimeClient == null || !realtimeClient.IsConnected) return;
            Debug.Log($"[RealtimeAudioManager] SendAudioChunk called. chunkSampleCount: {chunkSampleCount}");
            // Kopiere Buffer-Daten
            Array.Copy(microphoneBuffer, processingBuffer, chunkSampleCount);
            
            // Konvertiere zu PCM16
            byte[] pcmData = AudioChunk.FloatToPCM16(processingBuffer);
            Debug.Log($"[RealtimeAudioManager] Sending audio chunk: {pcmData.Length} bytes");
            // Erstelle Audio Chunk für bessere Datenstruktur
            AudioChunk chunk = new AudioChunk(pcmData, settings.SampleRate, 1);
            
            // Sende an RealtimeClient (verwende die verfügbare Methode)
            _ = realtimeClient.SendAudioAsync(chunk.audioData);
        }
        
        private void ApplyNoiseGate(float[] samples)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) < noiseGateThreshold)
                {
                    samples[i] = 0f;
                }
            }
        }
        
        #endregion
        
        #region Audio Playback        /// <summary>
        /// Spielt empfangenen Audio-Clip ab
        /// </summary>
        public void PlayReceivedAudio(AudioClip audioClip)
        {
            if (audioClip == null || playbackAudioSource == null) return;
            
            // Use gapless streaming system (OpenAI Reference approach)
            PlayReceivedAudioGapless(audioClip);
        }
        
        /// <summary>
        /// Spielt Audio über Gapless Streaming ab (OpenAI Reference approach)
        /// </summary>
        private void PlayReceivedAudioGapless(AudioClip audioClip)
        {
            if (audioClip == null) return;
            
            try
            {
                // Start streaming if not already started
                if (!playbackAudioSource.isPlaying || playbackAudioSource.clip != streamAudioClip)
                {
                    StartGaplessStreaming();
                }
                
                // Convert AudioClip to float array and add to stream
                float[] clipData = new float[audioClip.samples * audioClip.channels];
                audioClip.GetData(clipData, 0);
                
                // Convert stereo to mono if needed
                if (audioClip.channels == 2)
                {
                    float[] monoData = new float[audioClip.samples];
                    for (int i = 0; i < audioClip.samples; i++)
                    {
                        monoData[i] = (clipData[i * 2] + clipData[i * 2 + 1]) / 2.0f;
                    }
                    WriteAudioDataToStream(monoData, audioClip.name);
                }
                else
                {
                    WriteAudioDataToStream(clipData, audioClip.name);
                }
                
                Log($"[GAPLESS] Added AudioClip to stream: {audioClip.length:F2}s, {audioClip.samples} samples");
                OnAudioPlaybackStarted?.Invoke();
            }
            catch (Exception e)
            {
                LogError($"Failed to play audio via gapless streaming: {e.Message}");
            }
        }
        
        /// <summary>
        /// Spielt empfangenen AudioChunk ab
        /// </summary>
        public void PlayReceivedAudioChunk(AudioChunk audioChunk)
        {
            Debug.Log($"[AUDIO] PlayReceivedAudioChunk called. audioChunk valid: {audioChunk != null && audioChunk.IsValid()}");
            if (audioChunk == null || !audioChunk.IsValid()) return;
            
            try
            {
                // DIRECT: Feed AudioChunk directly to gapless stream (most efficient)
                AddAudioChunkToStream(audioChunk, $"chunk_{Time.time}");
            }
            catch (System.Exception ex)
            {
                LogError($"Error playing received audio chunk: {ex.Message}");
            }
        }

        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// Gibt Liste der verfügbaren Mikrofone zurück
        /// </summary>
        public string[] GetAvailableMicrophones()
        {
            return Microphone.devices;
        }
        
        /// <summary>
        /// Setzt spezifisches Mikrophone
        /// </summary>
        public void SetMicrophone(string microphoneName)
        {
            if (isRecording)
            {
                LogWarning("Cannot change microphone while recording");
                return;
            }
            
            specificMicrophone = microphoneName;
            useDefaultMicrophone = string.IsNullOrEmpty(microphoneName);
            currentMicrophone = useDefaultMicrophone ? null : microphoneName;
            
            Log($"Microphone set to: {currentMicrophone ?? "Default"}");
        }
        
        /// <summary>
        /// Gibt aktuellen durchschnittlichen Audio-Level zurück
        /// <summary>
        /// Stoppt alle Audio-Wiedergabe und leert die Warteschlange
        /// </summary>
        /// <summary>
        /// Stoppt alle Audio-Wiedergabe
        /// </summary>
        public void StopAllAudioPlayback()
        {
            Log("Stopping all audio playback");
            
            // Stop current playback
            if (playbackAudioSource != null && playbackAudioSource.isPlaying)
            {
                playbackAudioSource.Stop();
            }
            
            // Stop gapless streaming
            StopGaplessStreaming();
            
            OnAudioPlaybackFinished?.Invoke();
        }
        
        /// <summary>
        /// Gibt zurück, ob gerade Audio abgespielt wird
        /// </summary>
        public bool IsPlayingAudio()
        {
            return playbackAudioSource != null && playbackAudioSource.isPlaying;
        }

        /// <summary>
        /// Gibt die aktuelle AudioSource für Playback zurück (für Lip Sync)
        /// </summary>
        public AudioSource GetPlaybackAudioSource()
        {
            return playbackAudioSource;
        }
        
        /// <summary>
        /// Gibt Debug-Informationen über das Gapless Streaming zurück
        /// </summary>
        public string GetGaplessStreamDebugInfo()
        {
            lock (streamLock)
            {
                return $"Gapless Streaming: ENABLED | Started: {streamHasStarted} | " +
                       $"Buffers: {streamOutputBuffers.Count} | WriteOffset: {streamWriteOffset} | " +
                       $"Tracks: {streamTrackSampleOffsets.Count} | Interrupted: {streamHasInterrupted}";
            }
        }
        
        #endregion
        #region Logging
        
        private void Log(string message)
        {
            if (settings != null && settings.EnableDebugLogging)
            {
                Debug.Log($"[RealtimeAudioManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            if (settings != null && settings.EnableDebugLogging)
            {
                Debug.LogWarning($"[RealtimeAudioManager] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[RealtimeAudioManager] {message}");
        }
        
        #endregion

        /// <summary>
        /// Setzt den AudioManager nach einem Fehler zurück (Buffer, Flags, State)
        /// </summary>
        public void ResetAfterError()
        {
            Debug.LogWarning("[RealtimeAudioManager] ResetAfterError: Resetting audio state after error.");
            isRecording = false;
            bufferPosition = 0;
            lastMicrophonePosition = 0;
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
                recordingCoroutine = null;
            }
            if (microphoneClip != null)
            {
                DestroyImmediate(microphoneClip);
                microphoneClip = null;
            }
            Microphone.End(currentMicrophone);
            // Optionale Events
            OnRecordingStopped?.Invoke();
            Log("AudioManager state reset after error");
        }
        
        #region Adaptive Audio Performance Management
        
        private void MonitorAudioPerformance()
        {
            // SIMPLIFIED: Only monitor performance, no adaptive buffering
            if (Time.time - lastPerformanceCheck < performanceCheckInterval) return;
            lastPerformanceCheck = Time.time;
            
            // Just log quality issues for debugging - no buffer changes
            bool hasQualityIssues = CheckAudioQualityIssues();
            
            if (hasQualityIssues)
            {
                Debug.LogWarning($"[AudioManager] Audio quality issue detected (monitoring only - no automatic changes)");
            }
        }
        
        private bool CheckAudioQualityIssues()
        {
            // Check various indicators of audio quality issues
            
            // 1. Check if audio stream has issues - BUT only if we expect audio to be playing
            if (IsPlayingAudio() && streamHasStarted && streamOutputBuffers.Count == 0)
            {
                Debug.LogWarning("[AudioManager] Audio stream empty while playing - possible buffer underrun");
                return true;
            }
            
            // 2. Check Unity's audio performance
            if (AudioSettings.dspTime == 0)
            {
                Debug.LogWarning("[AudioManager] DSP time is zero - audio system may be struggling");
                return true;
            }
            
            // 3. Check frame rate (low FPS can affect audio)
            if (Time.unscaledDeltaTime > 0.05f) // More than 50ms frame time (< 20 FPS)
            {
                Debug.LogWarning("[AudioManager] Low frame rate detected - may affect audio quality");
                return true;
            }
            
            return false;
        }
        
        private void AdaptBufferSize(bool increase)
        {
            int oldIndex = currentBufferSizeIndex;
            
            if (increase && currentBufferSizeIndex < adaptiveBufferSizes.Length - 1)
            {
                currentBufferSizeIndex++;
            }
            else if (!increase && currentBufferSizeIndex > 0)
            {
                currentBufferSizeIndex--;
            }
            else
            {
                return; // No change possible
            }
            
            int newBufferSize = adaptiveBufferSizes[currentBufferSizeIndex];
            int oldBufferSize = adaptiveBufferSizes[oldIndex];
            
            Debug.Log($"[AudioManager] Adapting buffer size: {oldBufferSize} -> {newBufferSize} ({(increase ? "increased" : "decreased")} for {(increase ? "stability" : "lower latency")})");
            
            // Apply new buffer size
            streamBufferSize = newBufferSize;
            
            // If currently streaming, restart with new buffer size
            if (IsPlayingAudio())
            {
                Debug.Log("[AudioManager] Restarting audio stream with new buffer size");
                // Note: In practice, you might want to do this more gracefully
            }
        }
        
        [ContextMenu("Force Audio Performance Check")]
        public void ForcePerformanceCheck()
        {
            MonitorAudioPerformance();
        }
        
        [ContextMenu("Reset Adaptive Settings")]
        public void ResetAdaptiveSettings()
        {
            currentBufferSizeIndex = 1; // Reset to 1024
            audioDropoutCount = 0;
            performanceCheckCount = 0;
            streamBufferSize = adaptiveBufferSizes[currentBufferSizeIndex];
            Debug.Log($"[AudioManager] Adaptive settings reset. Buffer size: {streamBufferSize}");
        }
        
        #endregion
        
        #region Audio System Diagnostics
        
        [ContextMenu("Run Audio Diagnostics")]
        public void RunAudioDiagnostics()
        {
            Debug.Log("=== AUDIO MANAGER DIAGNOSTICS ===");
            
            // Basic state
            Debug.Log($"Recording: {isRecording}");
            Debug.Log($"Playing Audio: {IsPlayingAudio()}");
            Debug.Log($"Stream Buffer Count: {streamOutputBuffers.Count}");
            Debug.Log($"Current Microphone: {currentMicrophone}");
            
            // Settings
            Debug.Log($"Stream Buffer Size: {streamBufferSize}");
            Debug.Log($"Gapless Streaming: ENABLED (always)");
            Debug.Log($"Adaptive Buffering: {useAdaptiveBuffering}");
            
            // Performance
            if (useAdaptiveBuffering)
            {
                Debug.Log($"Current Buffer Size Index: {currentBufferSizeIndex}");
                Debug.Log($"Audio Dropout Count: {audioDropoutCount}");
                Debug.Log($"Performance Check Count: {performanceCheckCount}");
            }
            
            // Audio Sources
            if (microphoneAudioSource != null)
            {
                Debug.Log($"Microphone AudioSource: {microphoneAudioSource.name} (Muted: {microphoneAudioSource.mute})");
            }
            else
            {
                Debug.LogWarning("Microphone AudioSource: NOT FOUND");
            }
            
            if (playbackAudioSource != null)
            {
                Debug.Log($"Playback AudioSource: {playbackAudioSource.name} (Volume: {playbackAudioSource.volume})");
            }
            else
            {
                Debug.LogWarning("Playback AudioSource: NOT FOUND");
            }
            
            // Unity Audio Settings
            var audioConfig = AudioSettings.GetConfiguration();
            Debug.Log($"Unity Audio: {audioConfig.sampleRate}Hz, DSP: {audioConfig.dspBufferSize}, Voices: {audioConfig.numRealVoices}");
            
            Debug.Log("=== END DIAGNOSTICS ===");
        }
        
        #endregion
    }
}
