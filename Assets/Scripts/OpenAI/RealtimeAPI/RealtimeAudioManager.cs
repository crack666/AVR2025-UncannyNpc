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
        
        [Header("Voice Activity Detection")]
        [SerializeField] private bool enableVAD = true;
        [SerializeField] private float vadThreshold = 0.02f;
        [SerializeField] private float vadSilenceDuration = 1.0f;
        
        [Header("Audio Playback Settings")]
        [SerializeField] private bool useGaplessStreaming = true; // Stream-based gapless playback
        [SerializeField] private int streamBufferSize = 1024; // FIXED: Increased from 128 to 1024 for smooth playback
        
        [Header("Events")]
        public UnityEvent OnRecordingStarted;
        public UnityEvent OnRecordingStopped;
        public UnityEvent OnAudioPlaybackStarted;
        public UnityEvent OnAudioPlaybackFinished;
        public UnityEvent<float> OnMicrophoneLevelChanged;
        public UnityEvent<bool> OnVoiceDetected;
        
        // Private Fields
        private AudioClip microphoneClip;
        private bool isRecording;
        private bool isInitialized;
        private string currentMicrophone;
        private int lastMicrophonePosition;
        private float[] microphoneBuffer;
        private int bufferPosition;
        

        // Voice Activity Detection
        private bool voiceDetected;
        private float lastVoiceTime;
        private float currentMicrophoneLevel;
        private Queue<float> audioLevelHistory;
        private const int AUDIO_LEVEL_HISTORY_SIZE = 10;
        
        // Audio Processing
        private float[] processingBuffer;
        private int chunkSampleCount;
        private Coroutine recordingCoroutine;
        // Audio Playback Queue (fallback system)
        private Queue<AudioClip> audioPlaybackQueue = new Queue<AudioClip>();
        private bool isPlayingAudio = false;
        private Coroutine playbackCoroutine = null;
        
        // === GAPLESS STREAMING IMPLEMENTATION (OpenAI Reference) ===
        private Queue<StreamBuffer> streamOutputBuffers = new Queue<StreamBuffer>();
        private StreamBuffer currentStreamWriteBuffer;
        private int streamWriteOffset = 0;
        private bool streamHasStarted = false;
        private bool streamHasInterrupted = false;
        private bool audioPlaybackFinishedPending = false; // Flag für Update() Fallback
        private Dictionary<string, int> streamTrackSampleOffsets = new Dictionary<string, int>();
        private AudioClip streamAudioClip;
        private float[] streamAudioData;
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
        public float CurrentMicrophoneLevel => currentMicrophoneLevel;
        public string CurrentMicrophone => currentMicrophone;
        public bool VoiceDetected => voiceDetected;

        
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
            Debug.Log($"[RealtimeAudioManager] StopRecordingAsync called. bufferPosition={bufferPosition}, isRecording={isRecording}");
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
                        Debug.Log($"[RealtimeAudioManager] Processing remaining {bufferPosition} samples before commit.");
                        
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
                        
                        Debug.Log($"[RealtimeAudioManager] Remaining buffer sent successfully: {pcmData.Length} bytes");
                    }
                    else
                    {
                        Debug.Log($"[RealtimeAudioManager] Discarding too small buffer ({bufferPosition} samples < {minSamples} minimum)");
                    }
                    bufferPosition = 0;
                }

                // SIMPLIFIED: Following official OpenAI pattern - no manual commit needed
                // The RealtimeClient.StopListening() will call CreateResponse() instead
                Debug.Log($"[RealtimeAudioManager] StopRecordingAsync: Audio stopped, {pcmData?.Length ?? 0} bytes sent in final buffer");
                
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
                        Debug.Log($"[RealtimeAudioManager] ForceStopAllRecording: Stopped microphone: {device}");
                    }
                }
                
                // Also try stopping with null (default device)
                if (Microphone.IsRecording(null))
                {
                    Microphone.End(null);
                    Debug.Log("[RealtimeAudioManager] ForceStopAllRecording: Stopped default microphone");
                }
                
                // Stop coroutine
                if (recordingCoroutine != null)
                {
                    StopCoroutine(recordingCoroutine);
                    recordingCoroutine = null;
                    Debug.Log("[RealtimeAudioManager] ForceStopAllRecording: Recording coroutine stopped");
                }
                
                // Cleanup microphone clip
                if (microphoneClip != null)
                {
                    DestroyImmediate(microphoneClip);
                    microphoneClip = null;
                    Debug.Log("[RealtimeAudioManager] ForceStopAllRecording: Microphone clip destroyed");
                }
                
                // Reset all audio state
                bufferPosition = 0;
                lastMicrophonePosition = 0;
                voiceDetected = false;
                
                // Clear audio queues
                StopAllAudioPlayback();
                
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
            
            audioLevelHistory = new Queue<float>();
            Debug.Log("[RealtimeAudioManager] Awake: AudioManager created");
        }
        
        private void Start()
        {
            InitializeAudioManager();
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Handle pending audio playback finished event (Fallback)
            if (audioPlaybackFinishedPending)
            {
                audioPlaybackFinishedPending = false;
                OnAudioPlaybackFinished?.Invoke();
                Log("[GAPLESS] Audio playback finished - triggered from Update()");
            }
            
            // ZUSÄTZLICHE SICHERUNG: Prüfe ob AudioSource aufgehört hat zu spielen
            // aber das System denkt noch, dass Audio läuft
            if (useGaplessStreaming && streamHasStarted && playbackAudioSource != null && 
                !playbackAudioSource.isPlaying && streamOutputBuffers.Count == 0)
            {
                Log("[GAPLESS] SAFETY CHECK: AudioSource stopped but stream still active - triggering finish event");
                streamHasStarted = false;
                OnAudioPlaybackFinished?.Invoke();
            }
            
            UpdateMicrophoneLevel();
            UpdateVoiceActivityDetection();
            
            if (isRecording)
            {
                ProcessMicrophoneInput();
            }
        }
        
        private void OnDestroy()
        {
            StopRecording();
            // Stoppe alle Playbacks und lösche dynamisch erzeugte AudioSources
            StopAllAudioPlayback();
            
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
            Debug.Log("[RealtimeAudioManager] OnEnable: Registering event listeners");
        }

        private void OnDisable()
        {
            Debug.Log("[RealtimeAudioManager] OnDisable: Removing event listeners");
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
            
            // Setup Gapless Streaming if enabled
            if (useGaplessStreaming)
            {
                SetupGaplessStreaming();
            }
            
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
            
            Log($"[GAPLESS] Stream initialized: {sampleRate}Hz, {totalSamples} samples, {STREAM_LENGTH_SECONDS}s buffer");
        }
        
        /// <summary>
        /// Unity's OnAudioRead callback - equivalent to AudioWorkletProcessor.process()
        /// This is called from Unity's audio thread for gapless playback
        /// </summary>
        private void OnAudioRead(float[] data)
        {
            if (!useGaplessStreaming)
            {
                // Fallback: silence
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0f;
                }
                return;
            }
            
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
                
                // Log when we run out of buffers
                if (streamOutputBuffers.Count == 0 && streamHasStarted)
                {
                    Log("[GAPLESS] OnAudioRead: No buffers available, outputting silence");
                    
                    // CRITICAL: Reset stream state and trigger audio finished event
                    streamHasStarted = false;
                    
                    // ROBUSTERE LÖSUNG: Direct invoke on main thread + Fallback
                    if (UnityMainThreadDispatcher.Instance != null)
                    {
                        UnityMainThreadDispatcher.EnqueueAction(() => {
                            OnAudioPlaybackFinished?.Invoke();
                            Log("[GAPLESS] Audio playback finished - ready for next recording");
                        });
                    }
                    else
                    {
                        // Fallback: Set flag for Update() method to handle
                        audioPlaybackFinishedPending = true;
                        Log("[GAPLESS] Audio playback finished - will trigger on next Update()");
                    }
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
            if (!useGaplessStreaming) return;
            
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
            
            // CRITICAL: Start streaming immediately when first chunk arrives
            if (!playbackAudioSource.isPlaying || playbackAudioSource.clip != streamAudioClip)
            {
                StartGaplessStreaming();
                Log("[GAPLESS] Auto-started streaming on first chunk");
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
            
            //Log($"[GAPLESS] Added {sampleCount} samples to stream. Track: {trackId}");
        }
        
        /// <summary>
        /// Start gapless streaming
        /// </summary>
        private void StartGaplessStreaming()
        {
            if (!useGaplessStreaming) 
            {
                Log("[GAPLESS] StartGaplessStreaming called but useGaplessStreaming=false");
                return;
            }
            
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
            if (!useGaplessStreaming) return;
            
            lock (streamLock)
            {
                streamHasInterrupted = true;
                streamOutputBuffers.Clear();
                streamTrackSampleOffsets.Clear();
                streamWriteOffset = 0;
            }
            
            Log("[GAPLESS] Stopped gapless audio streaming");
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
                Debug.Log("[RealtimeAudioManager] StartRecording: Recording started");
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
        
        #region Voice Activity Detection
        
        private void UpdateMicrophoneLevel()
        {
            if (!isRecording || microphoneClip == null) return;
            
            // Berechne aktuelles Audio-Level
            float level = 0f;
            int position = Microphone.GetPosition(currentMicrophone);
            
            if (position > 0)
            {
                int sampleCount = Mathf.Min(1024, position);
                float[] samples = new float[sampleCount];
                microphoneClip.GetData(samples, position - sampleCount);
                
                for (int i = 0; i < samples.Length; i++)
                {
                    level += Mathf.Abs(samples[i]);
                }
                
                level /= samples.Length;
            }
            
            currentMicrophoneLevel = level;
            OnMicrophoneLevelChanged?.Invoke(level);
            
            // Update Audio Level History
            audioLevelHistory.Enqueue(level);
            if (audioLevelHistory.Count > AUDIO_LEVEL_HISTORY_SIZE)
            {
                audioLevelHistory.Dequeue();
            }
        }
        

        
        private void UpdateVoiceActivityDetection()
        {
            if (!enableVAD) return;
            
            bool currentVoiceDetected = currentMicrophoneLevel > vadThreshold;

            if (currentVoiceDetected)
            {
                lastVoiceTime = Time.time;
                if (!voiceDetected)
                {
                    voiceDetected = true;
                    OnVoiceDetected?.Invoke(true);
                    Log("Voice activity detected");
                }
            }
            else if (voiceDetected && Time.time - lastVoiceTime > vadSilenceDuration)
            {
                voiceDetected = false;
                OnVoiceDetected?.Invoke(false);
                Log("Voice activity ended");
                
                // Thread-safe: Trigger stop recording via main thread
                UnityMainThreadDispatcher.EnqueueAction(() => {
                    TriggerStopRecordingAsync();
                });
            }
        }
        
        #endregion
        
        #region Audio Playback        /// <summary>
        /// Spielt empfangenen Audio-Clip ab
        /// </summary>
        public void PlayReceivedAudio(AudioClip audioClip)
        {
            if (audioClip == null || playbackAudioSource == null) return;
            
            if (useGaplessStreaming)
            {
                // PRIMARY: Use gapless streaming system (OpenAI Reference approach)
                PlayReceivedAudioGapless(audioClip);
            }
            else
            {
                // FALLBACK: Use traditional queue playback
                PlayReceivedAudioQueued(audioClip);
            }
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
        /// Spielt Audio über traditionelle Queue ab
        /// </summary>
        private void PlayReceivedAudioQueued(AudioClip audioClip)
        {
            try
            {
                // Thread-safe enqueue
                lock (audioPlaybackQueue)
                {
                    audioPlaybackQueue.Enqueue(audioClip);
                    Log($"[QUEUE] Enqueued audio clip: {audioClip.length:F2}s (Queue size: {audioPlaybackQueue.Count}, isPlaying: {isPlayingAudio})");
                }
                
                // Starte Playback Coroutine falls nicht bereits aktiv
                if (!isPlayingAudio && playbackCoroutine == null)
                {
                    Log("[QUEUE] Starting new playback queue coroutine");
                    isPlayingAudio = true; // Set flag immediately to prevent race condition
                    playbackCoroutine = StartCoroutine(PlaybackQueueCoroutine());
                }
                else
                {
                    Log("[QUEUE] Audio already playing, added to queue");
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to play audio: {e.Message}");
                isPlayingAudio = false; // Reset flag on error
                playbackCoroutine = null;
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
                if (useGaplessStreaming)
                {
                    // DIRECT: Feed AudioChunk directly to gapless stream (most efficient)
                    AddAudioChunkToStream(audioChunk, $"chunk_{Time.time}");
                }
                else
                {
                    // FALLBACK: Convert to AudioClip for legacy systems
                    var audioClip = audioChunk.ToAudioClip($"ReceivedAudio_{Time.time}");
                    Debug.Log($"[AUDIO] Converted AudioChunk to AudioClip. Length: {audioClip.length:F2}s, Samples: {audioClip.samples}, Channels: {audioClip.channels}");
                    PlayReceivedAudio(audioClip);
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Error playing received audio chunk: {ex.Message}");
            }
        }

        private IEnumerator PlaybackQueueCoroutine()
        {
            Log($"[QUEUE] Started audio queue processing");
            
            while (true)
            {
                AudioClip clip = null;
                bool hasClip = false;
                
                // Get next clip from queue
                try
                {
                    lock (audioPlaybackQueue)
                    {
                        if (audioPlaybackQueue.Count > 0)
                        {
                            clip = audioPlaybackQueue.Dequeue();
                            hasClip = true;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    LogError($"[QUEUE] Error accessing queue: {ex.Message}");
                    break;
                }
                
                // Exit if no clips
                if (!hasClip)
                    break;
                    
                // Validate clip and audio source
                if (clip == null)
                {
                    LogError("[QUEUE] Dequeued null AudioClip! Skipping.");
                    continue;
                }
                
                if (playbackAudioSource == null || !playbackAudioSource.gameObject.activeInHierarchy)
                {
                    LogError("[QUEUE] PlaybackAudioSource is null or inactive! Aborting playback.");
                    break;
                }
                
                // Play the audio clip
                bool playbackSuccessful = false;
                float clipLength = 0f;
                AudioClip currentClip = null;
                
                try
                {
                    Log($"[QUEUE] Playing audio clip: {clip.length:F2}s");
                    
                    if (playbackAudioSource.isPlaying)
                    {
                        Log("[QUEUE] Stopping previous audio");
                        playbackAudioSource.Stop();
                    }
                    
                    playbackAudioSource.clip = clip;
                    currentClip = clip; // Store reference for validation
                    playbackAudioSource.Play();
                    
                    Debug.Log($"[AUDIO] Playback started for clip: {clip.name}, Length: {clip.length:F2}s");
                    OnAudioPlaybackStarted?.Invoke();
                    
                    clipLength = clip.length;
                    playbackSuccessful = true;
                }
                catch (System.Exception ex)
                {
                    LogError($"[QUEUE] Exception during playback setup: {ex.Message}");
                    break;
                }
                
                // Wait for clip to finish (outside try-catch)
                if (playbackSuccessful)
                {
                    float elapsed = 0f;
                    float checkInterval = 0.05f; // Increased check interval for stability
                    float allowedEndTolerance = 0.1f; // Allow clip to end 100ms early
                    
                    while (elapsed < clipLength)
                    {
                        // More robust interruption check
                        bool shouldStop = false;
                        
                        if (playbackAudioSource == null)
                        {
                            LogWarning($"[AUDIO] AudioSource became null at {elapsed:F2}s");
                            shouldStop = true;
                        }
                        else if (!playbackAudioSource.gameObject.activeInHierarchy)
                        {
                            LogWarning($"[AUDIO] AudioSource GameObject became inactive at {elapsed:F2}s");
                            shouldStop = true;
                        }
                        else if (playbackAudioSource.clip != currentClip)
                        {
                            LogWarning($"[AUDIO] AudioSource clip changed at {elapsed:F2}s - external interruption");
                            shouldStop = true;
                        }
                        else if (!playbackAudioSource.isPlaying && elapsed < (clipLength - allowedEndTolerance))
                        {
                            // Only consider it interrupted if stopped significantly before end
                            LogWarning($"[AUDIO] AudioSource stopped playing at {elapsed:F2}s of {clipLength:F2}s (tolerance: {allowedEndTolerance:F2}s)");
                            shouldStop = true;
                        }
                        
                        if (shouldStop)
                            break;
                        
                        elapsed += checkInterval;
                        yield return null; // CRITICAL FIX: Use yield return null instead of WaitForSeconds for gapless playback
                    }
                    
                    // Log completion status
                    if (elapsed >= (clipLength - allowedEndTolerance))
                    {
                        Debug.Log($"[AUDIO] Playback completed successfully for clip: {currentClip?.name}, Played: {elapsed:F2}s of {clipLength:F2}s");
                    }
                    else
                    {
                        Debug.Log($"[AUDIO] Playback interrupted for clip: {currentClip?.name}, Played: {elapsed:F2}s of {clipLength:F2}s");
                    }
                }
                
                // NO DELAY - go directly to next clip for gapless playback!
                // yield return new WaitForSeconds() removed for seamless audio
            }
            
            // Cleanup
            isPlayingAudio = false;
            playbackCoroutine = null;
            OnAudioPlaybackFinished?.Invoke();
            Log("[QUEUE] Audio queue processing completed");
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
        /// </summary>
        public float GetAverageAudioLevel()
        {
            if (audioLevelHistory.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (float level in audioLevelHistory)
            {
                sum += level;
            }
            
            return sum / audioLevelHistory.Count;
        }
          /// <summary>
        /// Stoppt alle Audio-Wiedergabe und leert die Warteschlange
        /// </summary>
        public void StopAllAudioPlayback()
        {
            Log("[QUEUE] Stopping all audio playback and clearing queue");
            
            // Stop current playback
            if (playbackAudioSource != null && playbackAudioSource.isPlaying)
            {
                playbackAudioSource.Stop();
            }
            
            // Stop coroutine if running
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }
            
            // Clear queue
            lock (audioPlaybackQueue)
            {
                int clearedCount = audioPlaybackQueue.Count;
                audioPlaybackQueue.Clear();
                Log($"[QUEUE] Cleared {clearedCount} audio clips from queue");
            }
            
            // Reset state
            isPlayingAudio = false;
            
            OnAudioPlaybackFinished?.Invoke();
        }
        
        /// <summary>
        /// Gibt die aktuelle Anzahl der Audio-Clips in der Warteschlange zurück
        /// </summary>
        public int GetAudioQueueCount()
        {
            lock (audioPlaybackQueue)
            {
                return audioPlaybackQueue.Count;
            }
        }
        
        /// <summary>
        /// Gibt zurück, ob gerade Audio abgespielt wird
        /// </summary>
        public bool IsPlayingAudio()
        {
            return isPlayingAudio && playbackAudioSource != null && playbackAudioSource.isPlaying;
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
            if (!useGaplessStreaming) return "Gapless Streaming: DISABLED";
            
            lock (streamLock)
            {
                return $"Gapless Streaming: ENABLED | Started: {streamHasStarted} | " +
                       $"Buffers: {streamOutputBuffers.Count} | WriteOffset: {streamWriteOffset} | " +
                       $"Tracks: {streamTrackSampleOffsets.Count} | Interrupted: {streamHasInterrupted}";
            }
        }
        
        /// <summary>
        /// Toggle Gapless Streaming Mode
        /// </summary>
        public void ToggleGaplessStreaming()
        {
            useGaplessStreaming = !useGaplessStreaming;
            
            if (useGaplessStreaming)
            {
                if (isInitialized)
                {
                    SetupGaplessStreaming();
                    Log("[CONFIG] Gapless Streaming ENABLED");
                }
            }
            else
            {
                StopGaplessStreaming();
                Log("[CONFIG] Gapless Streaming DISABLED");
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
            voiceDetected = false;
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
    }
}
