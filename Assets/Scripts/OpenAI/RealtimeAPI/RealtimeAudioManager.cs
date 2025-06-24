using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
        [SerializeField] private float vadPrefixPadding = 0.3f;
        
        [Header("Audio Playback Settings")]
        [SerializeField] private bool useSmoothPlayback = true;
        [SerializeField] private float concatenationDelay = 0.2f;
        
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
          // Audio Playback Queue
        private Queue<AudioClip> audioPlaybackQueue = new Queue<AudioClip>();        private bool isPlayingAudio = false;
        private Coroutine playbackCoroutine = null;
          // Audio concatenation for smooth playback
        private List<AudioClip> pendingChunks = new List<AudioClip>();
        private bool isProcessingConcatenation = false;
        private float lastChunkReceiveTime = 0f;
        
        // LipSync Audio Analysis
        private const int AUDIO_SAMPLE_WINDOW = 1024;
        private float[] audioAnalysisSamples = new float[AUDIO_SAMPLE_WINDOW];
        private float currentAudioAmplitude = 0f;
        
        // Properties
        public bool IsRecording => isRecording;
        public bool IsInitialized => isInitialized;        public float CurrentMicrophoneLevel => currentMicrophoneLevel;
        public bool VoiceDetected => voiceDetected;
        public string CurrentMicrophone => currentMicrophone;
        
        /// <summary>
        /// Aktuelle Audio-Amplitude für LipSync (0-1 Bereich)
        /// </summary>
        public float CurrentAudioAmplitude => currentAudioAmplitude;
        
        /// <summary>
        /// Aktiviert/deaktiviert Smooth Playback Modus
        /// </summary>
        public bool UseSmoothPlayback 
        { 
            get => useSmoothPlayback; 
            set 
            { 
                useSmoothPlayback = value;
                Log($"[CONFIG] Smooth playback mode: {(value ? "ENABLED" : "DISABLED")}");
            } 
        }
        
        /// <summary>
        /// Setzt die Concatenation-Verzögerung
        /// </summary>
        public float ConcatenationDelay 
        { 
            get => concatenationDelay; 
            set 
            { 
                concatenationDelay = Mathf.Clamp(value, 0.05f, 1.0f);
                Log($"[CONFIG] Concatenation delay set to: {concatenationDelay:F2}s");
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
        }
        
        private void Start()
        {
            InitializeAudioManager();
        }
          private void Update()
        {
            if (!isInitialized) return;
            
            UpdateMicrophoneLevel();
            UpdateVoiceActivityDetection();
            UpdateAudioAmplitudeAnalysis();
            
            if (isRecording)
            {
                ProcessMicrophoneInput();
            }
        }
        
        private void OnDestroy()
        {
            StopRecording();
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
              // Subscribe to RealtimeClient events
            realtimeClient.OnAudioReceived.AddListener(PlayReceivedAudioChunk);
            
            isInitialized = true;
            
            Log("Audio Manager initialized successfully");
        }
        
        private void SetupAudioSources()
        {
            // Microphone AudioSource Setup
            if (microphoneAudioSource == null)
            {
                GameObject micObject = new GameObject("MicrophoneAudioSource");
                micObject.transform.SetParent(transform);
                microphoneAudioSource = micObject.AddComponent<AudioSource>();
            }
            
            microphoneAudioSource.loop = true;
            microphoneAudioSource.mute = true; // Verhindert Feedback
            microphoneAudioSource.volume = 0f;
            
            // Playback AudioSource Setup
            if (playbackAudioSource == null)
            {
                GameObject playbackObject = new GameObject("PlaybackAudioSource");
                playbackObject.transform.SetParent(transform);
                playbackAudioSource = playbackObject.AddComponent<AudioSource>();
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
            if (!isRecording) return;
            
            try
            {
                isRecording = false;
                
                if (recordingCoroutine != null)
                {
                    StopCoroutine(recordingCoroutine);
                    recordingCoroutine = null;
                }
                
                // Stoppe Mikrophone
                Microphone.End(currentMicrophone);
                
                if (microphoneClip != null)
                {
                    DestroyImmediate(microphoneClip);
                    microphoneClip = null;
                }
                  // Committe letzten Audio Buffer falls vorhanden
                if (realtimeClient != null && realtimeClient.IsConnected)
                {
                    _ = realtimeClient.CommitAudioBuffer();
                }
                
                OnRecordingStopped?.Invoke();
                Log("Recording stopped");
            }
            catch (Exception e)
            {
                LogError($"Failed to stop recording: {e.Message}");
            }
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
            for (int i = 0; i < samples.Length; i++)
            {
                microphoneBuffer[bufferPosition] = samples[i];
                bufferPosition++;
                
                // Buffer voll - sende Chunk
                if (bufferPosition >= chunkSampleCount)
                {
                    SendAudioChunk();
                    bufferPosition = 0;
                }
            }
        }
          private void SendAudioChunk()
        {
            if (realtimeClient == null || !realtimeClient.IsConnected) return;
            
            // Kopiere Buffer-Daten
            Array.Copy(microphoneBuffer, processingBuffer, chunkSampleCount);
            
            // Konvertiere zu PCM16
            byte[] pcmData = AudioChunk.FloatToPCM16(processingBuffer);
            
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
                  // Auto-commit bei VAD-Ende
                if (realtimeClient != null && realtimeClient.IsConnected)
                {
                    _ = realtimeClient.CommitAudioBuffer();
                }            }
        }
        
        /// <summary>
        /// Analysiert aktuelle Audio-Amplitude für LipSync
        /// </summary>
        private void UpdateAudioAmplitudeAnalysis()
        {
            currentAudioAmplitude = 0f;
            
            if (playbackAudioSource != null && playbackAudioSource.isPlaying)
            {
                try
                {
                    // Get audio data from the playback audio source
                    playbackAudioSource.GetOutputData(audioAnalysisSamples, 0);
                    
                    float sum = 0f;
                    for (int i = 0; i < AUDIO_SAMPLE_WINDOW; i++)
                    {
                        sum += Mathf.Abs(audioAnalysisSamples[i]);
                    }
                    
                    currentAudioAmplitude = sum / AUDIO_SAMPLE_WINDOW;
                }
                catch (System.Exception ex)
                {
                    // Fail gracefully if audio analysis fails
                    LogWarning($"Audio amplitude analysis failed: {ex.Message}");
                    currentAudioAmplitude = 0f;
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
            
            if (useSmoothPlayback)
            {
                // Use smooth concatenation playback
                PlayReceivedAudioSmooth(audioClip);
            }
            else
            {
                // Use traditional queue playback
                PlayReceivedAudioQueued(audioClip);
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
            if (audioChunk == null || !audioChunk.IsValid()) return;
            
            try
            {
                // Konvertiere AudioChunk zu AudioClip
                var audioClip = audioChunk.ToAudioClip($"ReceivedAudio_{Time.time}");
                PlayReceivedAudio(audioClip);
            }
            catch (System.Exception ex)
            {
                LogError($"Error playing received audio chunk: {ex.Message}");
            }
        }        private IEnumerator PlaybackQueueCoroutine()
        {
            Log($"[QUEUE] Started audio queue processing");
            
            while (true)
            {
                AudioClip clip = null;
                
                // Thread-safe dequeue
                lock (audioPlaybackQueue)
                {
                    if (audioPlaybackQueue.Count == 0)
                    {
                        break; // Exit if no more clips
                    }
                    clip = audioPlaybackQueue.Dequeue();
                }
                
                if (clip == null) break;
                
                Log($"[QUEUE] Dequeued audio clip: {clip.length:F2}s (Remaining: {audioPlaybackQueue.Count})");
                
                // Only stop if really necessary - avoid unnecessary interruptions
                if (playbackAudioSource.isPlaying && playbackAudioSource.clip != clip)
                {
                    Log("[QUEUE] Stopping previous audio for new clip");
                }
                
                playbackAudioSource.clip = clip;
                playbackAudioSource.Play();
                
                OnAudioPlaybackStarted?.Invoke();
                Log($"[QUEUE] Now playing audio: {clip.length:F2}s");
                
                // More precise timing - wait for the actual audio to finish
                float startTime = Time.time;
                float clipLength = clip.length;
                
                // Wait in smaller intervals for more responsive checking
                while (Time.time - startTime < clipLength && playbackAudioSource.isPlaying)
                {
                    yield return new WaitForSeconds(0.02f); // 50fps checking instead of 10fps
                }
                
                float actualPlayTime = Time.time - startTime;
                Log($"[QUEUE] Audio clip finished playing (played for {actualPlayTime:F2}s of {clipLength:F2}s)");
                
                // No artificial pause - immediately continue to next clip for smoother playback
                // yield return new WaitForSeconds(0.05f); // Removed to reduce stuttering
            }
            
            // Reset state when done
            isPlayingAudio = false;
            playbackCoroutine = null;
            OnAudioPlaybackFinished?.Invoke();
            Log("[QUEUE] Audio queue processing completed - all clips played");
        }
        
        /// <summary>
        /// Spielt empfangenen Audio-Clip ab (mit verbesserter Concatenation)
        /// </summary>
        public void PlayReceivedAudioSmooth(AudioClip audioClip)
        {
            if (audioClip == null || playbackAudioSource == null) return;
            
            try
            {
                // Add to pending chunks for concatenation
                lock (pendingChunks)
                {
                    pendingChunks.Add(audioClip);
                    lastChunkReceiveTime = Time.time;
                    Log($"[SMOOTH] Added chunk to pending list: {audioClip.length:F2}s (Total pending: {pendingChunks.Count})");
                }
                
                // Start concatenation process if not already running
                if (!isProcessingConcatenation)
                {
                    StartCoroutine(ProcessPendingChunks());
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to queue audio for smooth playback: {e.Message}");
            }
        }
          private IEnumerator ProcessPendingChunks()
        {
            isProcessingConcatenation = true;
            Log("[SMOOTH] Started processing pending chunks");
            
            while (true)
            {
                // Wait for a pause in incoming chunks
                yield return new WaitForSeconds(concatenationDelay);
                
                List<AudioClip> chunksToProcess = null;
                lock (pendingChunks)
                {
                    // Check if we have chunks and no new ones arrived recently
                    if (pendingChunks.Count > 0 && (Time.time - lastChunkReceiveTime) >= concatenationDelay)
                    {
                        chunksToProcess = new List<AudioClip>(pendingChunks);
                        pendingChunks.Clear();
                        Log($"[SMOOTH] Processing {chunksToProcess.Count} chunks for concatenation");
                    }
                    else if (pendingChunks.Count == 0)
                    {
                        // No more chunks to process
                        break;
                    }
                }
                
                if (chunksToProcess != null && chunksToProcess.Count > 0)
                {
                    // Concatenate chunks into single clip
                    AudioClip concatenatedClip = ConcatenateAudioClips(chunksToProcess);
                    if (concatenatedClip != null)
                    {
                        // Play the concatenated clip using the queue system
                        lock (audioPlaybackQueue)
                        {
                            audioPlaybackQueue.Enqueue(concatenatedClip);
                            Log($"[SMOOTH] Enqueued concatenated clip: {concatenatedClip.length:F2}s");
                        }
                        
                        // Start playback if not already playing
                        if (!isPlayingAudio && playbackCoroutine == null)
                        {
                            Log("[SMOOTH] Starting playback for concatenated audio");
                            isPlayingAudio = true;
                            playbackCoroutine = StartCoroutine(PlaybackQueueCoroutine());
                        }
                    }
                }
            }
            
            isProcessingConcatenation = false;
            Log("[SMOOTH] Finished processing pending chunks");
        }
        
        private AudioClip ConcatenateAudioClips(List<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0) return null;
            
            try
            {
                // Calculate total length and ensure all clips have same frequency/channels
                int frequency = clips[0].frequency;
                int channels = clips[0].channels;
                int totalSamples = 0;
                
                foreach (var clip in clips)
                {
                    if (clip.frequency != frequency || clip.channels != channels)
                    {
                        LogWarning($"[SMOOTH] Audio format mismatch - expected {frequency}Hz/{channels}ch, got {clip.frequency}Hz/{clip.channels}ch");
                        // Fallback to individual playback
                        return null;
                    }
                    totalSamples += clip.samples;
                }
                
                // Create new concatenated clip
                AudioClip concatenated = AudioClip.Create($"Concatenated_{Time.time}", totalSamples, channels, frequency, false);
                float[] concatenatedData = new float[totalSamples * channels];
                
                int currentIndex = 0;
                foreach (var clip in clips)
                {
                    float[] clipData = new float[clip.samples * channels];
                    clip.GetData(clipData, 0);
                    System.Array.Copy(clipData, 0, concatenatedData, currentIndex, clipData.Length);
                    currentIndex += clipData.Length;
                }
                
                concatenated.SetData(concatenatedData, 0);
                Log($"[SMOOTH] Created concatenated clip: {concatenated.length:F2}s from {clips.Count} chunks");
                
                return concatenated;
            }
            catch (Exception e)
            {
                LogError($"Failed to concatenate audio clips: {e.Message}");
                return null;
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
    }
}
