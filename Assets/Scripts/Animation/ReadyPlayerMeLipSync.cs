using System.Collections;
using UnityEngine;
using OpenAI.RealtimeAPI;

namespace Animation
{
    /// <summary>
    /// Advanced Lip Sync system for ReadyPlayerMe avatars
    /// Controls mouthOpen and mouthSmile BlendShapes based on audio playback
    /// </summary>
    public class ReadyPlayerMeLipSync : MonoBehaviour
    {        [Header("Avatar Components")]
        [SerializeField] private SkinnedMeshRenderer headMeshRenderer;
        [SerializeField] private AudioSource audioSource; // Legacy - kept for fallback
        [SerializeField] private RealtimeAudioManager realtimeAudioManager;        [Header("Lip Sync Settings")]
        [SerializeField] private bool enableLipSync = true;
        [SerializeField] private float lipSyncSensitivity = 5.0f; // Increased sensitivity for more dramatic response
        [SerializeField] private float smoothingSpeed = 15.0f; // Faster response for more dynamic animation
        [SerializeField] private AnimationCurve lipSyncCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);        [Header("Mouth Animation")]
        [SerializeField] private float mouthOpenMultiplier = 18.0f; // Maximum multiplier for most dramatic animation
        [SerializeField] private float mouthOpenBoost = 0.25f; // Strong boost to make quiet parts more visible
        [SerializeField] private float amplitudeContrast = 3.0f; // Maximum contrast for dramatic differences
        [SerializeField] private float mouthSmileBase = 0.08f; // Slight base expression during speaking
        [SerializeField] private float mouthSmileIdle = 0.4f; // Friendly idle smile after speaking (40%)
        [SerializeField] private float mouthSmileVariation = 0.18f; // Good smile variation during speaking
        [SerializeField] private float speakingRate = 5.0f; // Very fast mouth movement for most dynamic animation[Header("Auto-Gain / Adaptive Normalization")]
        [SerializeField] private bool enableAutoGain = true;
        [SerializeField] private float autoGainTarget = 0.9f; // Target peak amplitude (90% mouth open for maximum dramatic effect)
        [SerializeField] private float autoGainSpeed = 0.5f; // How fast the gain adapts
        [SerializeField] private float minAmplitudeThreshold = 0.0001f; // Ignore very quiet audio
        [SerializeField] private float maxAmplitudeThreshold = 1.0f; // Allow full range up to 100%
        
        [Header("BlendShape Names")]
        [SerializeField] private string mouthOpenBlendShapeName = "mouthOpen";
        [SerializeField] private string mouthSmileBlendShapeName = "mouthSmile";
          [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool showBlendShapeValues = false;
        [SerializeField] private bool showValuesInZeroToOneRange = true; // Show values in 0-1 range instead of 0-100
        
        // Private fields
        private int mouthOpenIndex = -1;
        private int mouthSmileIndex = -1;
        private float[] audioSamples;
        private const int SAMPLE_WINDOW = 256;
        
        // Animation state
        private float currentMouthOpen = 0f;
        private float currentMouthSmile = 0f;
        private float targetMouthOpen = 0f;
        private float targetMouthSmile = 0f;
          // Speaking animation
        private bool isSpeaking = false;
        private float currentAmplitude = 0f;
        
        // Auto-Gain state
        private float currentGain = 1.0f;
        private float peakAmplitude = 0.001f; // Running peak amplitude for auto-gain
        private float amplitudeDecay = 0.95f; // How fast peak amplitude decays over time
        private float speakingTime = 0f;
        private Coroutine speakingCoroutine;
        
        // Debug tracking
        private float lastLoggedMouthOpen = -999f;
        private float lastLoggedMouthSmile = -999f;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Awake - Starting component search...");
            
            // Auto-find components if not assigned
            if (headMeshRenderer == null)
            {
                if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Searching for SkinnedMeshRenderer...");
                
                // Look for Wolf3D_Head or similar mesh renderer
                headMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if (headMeshRenderer != null)
                {
                    Debug.Log($"[ReadyPlayerMeLipSync] Found SkinnedMeshRenderer: {headMeshRenderer.name} on GameObject: {headMeshRenderer.gameObject.name}");
                }
                else
                {
                    if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] No SkinnedMeshRenderer found in children, searching all...");
                    // Search in child objects
                    var allRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
                    Debug.Log($"[ReadyPlayerMeLipSync] Found {allRenderers.Length} SkinnedMeshRenderers total");
                    
                    foreach (var renderer in allRenderers)
                    {
                        if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] Checking renderer: {renderer.name} (BlendShapes: {renderer.sharedMesh?.blendShapeCount ?? 0})");
                        if (renderer.name.ToLower().Contains("head") || 
                            (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0))
                        {
                            headMeshRenderer = renderer;
                            Debug.Log($"[ReadyPlayerMeLipSync] Selected renderer: {renderer.name}");
                            break;
                        }
                    }
                }
            }
            else
            {
                if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] HeadMeshRenderer already assigned: {headMeshRenderer.name}");
            }
              if (audioSource == null)
            {
                if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Searching for AudioSource...");
                audioSource = GetComponent<AudioSource>();
                
                if (audioSource == null)
                {
                    if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] No local AudioSource found");
                }
                else
                {
                    if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] Found local AudioSource: {audioSource.name}");
                }
            }
            else
            {
                if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] AudioSource already assigned: {audioSource.name}");
            }
            
            // Find RealtimeAudioManager for modern audio amplitude analysis
            if (realtimeAudioManager == null)
            {
                if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Searching for RealtimeAudioManager...");
                realtimeAudioManager = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
                if (realtimeAudioManager != null)
                {
                    if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] Found RealtimeAudioManager: {realtimeAudioManager.name}");
                }
                else
                {
                    Debug.LogWarning("[ReadyPlayerMeLipSync] No RealtimeAudioManager found! Audio amplitude analysis will use fallback AudioSource method.");
                }
            }
            else
            {
                if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] RealtimeAudioManager already assigned: {realtimeAudioManager.name}");
            }
              audioSamples = new float[SAMPLE_WINDOW];
            
            // Initialize with neutral expression
            currentMouthOpen = 0f;
            currentMouthSmile = 0f;
            targetMouthOpen = 0f;
            targetMouthSmile = 0f;
            
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Awake completed");
        }
          private void Start()
        {
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Starting initialization...");
            
            // Auto-discover components if not manually assigned
            AutoDiscoverComponents();
            
            InitializeBlendShapes();
            SetBaseFacialExpression();
            
            // Additional debug info
            Debug.Log($"[ReadyPlayerMeLipSync] Initialization complete - HeadMeshRenderer: {(headMeshRenderer != null ? "Found" : "NULL")}, RealtimeAudioManager: {(realtimeAudioManager != null ? "Found" : "NULL")}, AudioSource: {(audioSource != null ? "Found" : "NULL")}");
        }
        
        private void Update()
        {
            if (!enableLipSync || headMeshRenderer == null) return;
            
            UpdateLipSync();
            ApplyBlendShapes();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeBlendShapes()
        {
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] InitializeBlendShapes starting...");
            
            if (headMeshRenderer == null || headMeshRenderer.sharedMesh == null) 
            {
                Debug.LogError("[ReadyPlayerMeLipSync] No head mesh renderer found! HeadMeshRenderer is " + (headMeshRenderer == null ? "NULL" : "assigned but sharedMesh is NULL"));
                return;
            }
            
            var mesh = headMeshRenderer.sharedMesh;
            Debug.Log($"[ReadyPlayerMeLipSync] Mesh found: {mesh.name} with {mesh.blendShapeCount} BlendShapes");
            
            // Find BlendShape indices
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string shapeName = mesh.GetBlendShapeName(i);
                if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] BlendShape {i}: '{shapeName}'");
                
                if (shapeName.Equals(mouthOpenBlendShapeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    mouthOpenIndex = i;
                    Debug.Log($"[ReadyPlayerMeLipSync] ✅ Found mouthOpen at index {i}");
                }
                else if (shapeName.Equals(mouthSmileBlendShapeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    mouthSmileIndex = i;
                    Debug.Log($"[ReadyPlayerMeLipSync] ✅ Found mouthSmile at index {i}");
                }
            }
            
            // Log results
            Debug.Log($"[ReadyPlayerMeLipSync] BlendShape setup complete - MouthOpen: {mouthOpenIndex}, MouthSmile: {mouthSmileIndex}");
            
            if (mouthOpenIndex == -1)
            {
                Debug.LogWarning($"[ReadyPlayerMeLipSync] ❌ BlendShape '{mouthOpenBlendShapeName}' not found! Available shapes listed above.");
            }
            
            if (mouthSmileIndex == -1)
            {
                Debug.LogWarning($"[ReadyPlayerMeLipSync] ❌ BlendShape '{mouthSmileBlendShapeName}' not found! Available shapes listed above.");
            }
            
            // List all available BlendShapes for debugging
            if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] All available BlendShapes: {string.Join(", ", GetAllBlendShapeNames())}");
        }
        
        private string[] GetAllBlendShapeNames()
        {
            if (headMeshRenderer?.sharedMesh == null) return new string[0];
            
            var mesh = headMeshRenderer.sharedMesh;
            string[] names = new string[mesh.blendShapeCount];
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                names[i] = mesh.GetBlendShapeName(i);
            }
            
            return names;
        }
          #endregion
        
        #region Facial Expression Control
          private void SetBaseFacialExpression()
        {
            // Set friendly idle expression
            currentMouthSmile = mouthSmileIdle; // Start with friendly smile
            targetMouthSmile = mouthSmileIdle;
            currentMouthOpen = 0f; // Mouth closed when not speaking
            targetMouthOpen = 0f;
        }
          private void UpdateLipSync()
        {
            // Check if audio is playing from RealtimeAudioManager or AudioSource
            bool isAudioPlaying = (realtimeAudioManager != null && realtimeAudioManager.IsPlayingAudio()) ||
                                  (audioSource != null && audioSource.isPlaying);
            
            if (enableDebugLogging && Time.frameCount % 60 == 0) // Log every 60 frames (~1s at 60fps)
            {
                Debug.Log($"[ReadyPlayerMeLipSync] Audio Status - RealtimeAudioManager: {(realtimeAudioManager != null ? realtimeAudioManager.IsPlayingAudio().ToString() : "NULL")}, AudioSource: {(audioSource != null ? audioSource.isPlaying.ToString() : "NULL")}, Overall Playing: {isAudioPlaying}");
            }
            
            if (isAudioPlaying)
            {
                if (!isSpeaking)
                {
                    StartSpeaking();
                }
                
                // Get audio amplitude
                float amplitude = GetAudioAmplitude();                // Convert amplitude to mouth open value - dramatic contrast for dynamic animation
                float rawAmplitude = amplitude * lipSyncSensitivity;
                
                // Apply contrast enhancement to make differences more pronounced
                float enhancedAmplitude = Mathf.Pow(rawAmplitude, 1.0f / amplitudeContrast); // Power curve for more contrast
                float rawMouthOpen = lipSyncCurve.Evaluate(enhancedAmplitude) * mouthOpenMultiplier;
                
                // Apply smart boost - more boost for medium values, less for extremes
                if (rawMouthOpen > 0.01f) // Only boost if there's actual audio signal
                {
                    float boostFactor = Mathf.Sin(rawMouthOpen * Mathf.PI) * mouthOpenBoost; // Sine curve boost
                    targetMouthOpen = rawMouthOpen + boostFactor;
                }
                else
                {
                    targetMouthOpen = 0f; // Complete silence = closed mouth
                }
                
                // Add natural speaking variation to smile - dynamic during speaking
                float speakingSmileVariation = Mathf.Sin(speakingTime * speakingRate) * mouthSmileVariation;
                targetMouthSmile = mouthSmileBase + speakingSmileVariation;
                
                // Clamp values - allow dramatic range from 0 to 1
                targetMouthOpen = Mathf.Clamp(targetMouthOpen, 0f, 1.0f); // Full dramatic range: 0% to 100%
                targetMouthSmile = Mathf.Clamp(targetMouthSmile, 0f, 0.3f); // Speaking smile range
                
                // Debug log for amplitude and target values (throttled)
                if (enableDebugLogging && Time.frameCount % 90 == 0) // Log every 90 frames (~1.5s at 60fps)
                {
                    Debug.Log($"[ReadyPlayerMeLipSync] Audio amplitude: {amplitude:F4}, targetMouthOpen: {targetMouthOpen:F3}, targetMouthSmile: {targetMouthSmile:F3}");
                }
            }            else
            {
                if (isSpeaking)
                {
                    StopSpeaking();
                }
                  // Return to idle expression with a friendly smile
                targetMouthOpen = 0f; // Mouth closed when not speaking
                targetMouthSmile = mouthSmileIdle; // Friendly idle smile (40%)
            }
            
            // Smooth interpolation
            currentMouthOpen = Mathf.Lerp(currentMouthOpen, targetMouthOpen, Time.deltaTime * smoothingSpeed);
            currentMouthSmile = Mathf.Lerp(currentMouthSmile, targetMouthSmile, Time.deltaTime * smoothingSpeed);
            
            // Additional debug for current values
            if (enableDebugLogging && Time.frameCount % 120 == 0) // Log every 120 frames (~2s at 60fps)
            {
                Debug.Log($"[ReadyPlayerMeLipSync] Current Values - MouthOpen: {currentMouthOpen:F3} (target: {targetMouthOpen:F3}), MouthSmile: {currentMouthSmile:F3} (target: {targetMouthSmile:F3})");
            }
        }          private float GetAudioAmplitude()
        {
            float rawAmplitude = 0f;
            
            // Prefer RealtimeAudioManager for audio amplitude analysis
            if (realtimeAudioManager != null)
            {
                rawAmplitude = realtimeAudioManager.CurrentAudioAmplitude;
            }
            else if (audioSource != null)
            {
                // Fallback to legacy AudioSource method
                try
                {
                    audioSource.GetOutputData(audioSamples, 0);
                    
                    float sum = 0f;
                    for (int i = 0; i < SAMPLE_WINDOW; i++)
                    {
                        sum += Mathf.Abs(audioSamples[i]);
                    }
                    
                    rawAmplitude = sum / SAMPLE_WINDOW;
                }
                catch
                {
                    return 0f;
                }
            }
            
            // Store raw amplitude for debugging
            currentAmplitude = rawAmplitude;
            
            // Apply Auto-Gain if enabled
            if (enableAutoGain)
            {
                return ApplyAutoGain(rawAmplitude);
            }
            
            // Without auto-gain, just return raw amplitude
            if (enableDebugLogging && rawAmplitude > 0.001f) // Only log when there's actual audio
            {
                UnityEngine.Debug.Log($"[ReadyPlayerMeLipSync] Raw audio amplitude: {rawAmplitude:F4}");
            }
            return rawAmplitude;
        }
        
        private float ApplyAutoGain(float rawAmplitude)
        {
            // Ignore very quiet audio
            if (rawAmplitude < minAmplitudeThreshold)
            {
                return 0f;
            }
            
            // Update peak amplitude (with decay over time)
            peakAmplitude *= amplitudeDecay;
            if (rawAmplitude > peakAmplitude)
            {
                peakAmplitude = rawAmplitude;
            }
            
            // Calculate desired gain to reach target amplitude
            float desiredGain = 1.0f;
            if (peakAmplitude > minAmplitudeThreshold)
            {
                desiredGain = autoGainTarget / peakAmplitude;
            }
            
            // Smoothly adjust current gain towards desired gain
            currentGain = Mathf.Lerp(currentGain, desiredGain, autoGainSpeed * Time.deltaTime);
            
            // Apply gain and clamp to prevent overshooting
            float amplifiedAmplitude = rawAmplitude * currentGain;
            amplifiedAmplitude = Mathf.Clamp(amplifiedAmplitude, 0f, maxAmplitudeThreshold);
            
            // Debug logging (throttled)
            if (enableDebugLogging && Time.frameCount % 90 == 0 && rawAmplitude > minAmplitudeThreshold)
            {
                UnityEngine.Debug.Log($"[ReadyPlayerMeLipSync] Auto-Gain: Raw={rawAmplitude:F4}, Peak={peakAmplitude:F4}, Gain={currentGain:F2}, Final={amplifiedAmplitude:F4}");
            }
            
            return amplifiedAmplitude;
        }private void ApplyBlendShapes()
        {
            if (headMeshRenderer == null) 
            {
                if (enableDebugLogging) Debug.LogWarning("[ReadyPlayerMeLipSync] Cannot apply BlendShapes - headMeshRenderer is null!");
                return;
            }
            
            // Apply mouth open BlendShape (0-1 range for ReadyPlayerMe)
            if (mouthOpenIndex >= 0)
            {
                float mouthOpenValue = Mathf.Clamp01(currentMouthOpen); // Direct 0-1 range for ReadyPlayerMe
                headMeshRenderer.SetBlendShapeWeight(mouthOpenIndex, mouthOpenValue);
                
                // Debug only when value changes significantly or when showBlendShapeValues is enabled
                if (showBlendShapeValues || Mathf.Abs(mouthOpenValue - lastLoggedMouthOpen) > 0.02f)
                {
                    if (showValuesInZeroToOneRange)
                    {
                        Debug.Log($"[ReadyPlayerMeLipSync] Applied mouthOpen: {mouthOpenValue:F3} (0-1 range)");
                    }
                    else
                    {
                        Debug.Log($"[ReadyPlayerMeLipSync] Applied mouthOpen: {mouthOpenValue:F3} (from currentMouthOpen: {currentMouthOpen:F3})");
                    }
                    lastLoggedMouthOpen = mouthOpenValue;
                }
            }
            else if (enableDebugLogging && mouthOpenIndex == -1)
            {
                Debug.LogWarning("[ReadyPlayerMeLipSync] Cannot apply mouthOpen - BlendShape index not found!");
            }
            
            // Apply mouth smile BlendShape (0-1 range for ReadyPlayerMe)
            if (mouthSmileIndex >= 0)
            {
                float mouthSmileValue = Mathf.Clamp01(currentMouthSmile); // Direct 0-1 range for ReadyPlayerMe
                headMeshRenderer.SetBlendShapeWeight(mouthSmileIndex, mouthSmileValue);
                
                // Debug only when value changes significantly or when showBlendShapeValues is enabled
                if (showBlendShapeValues || Mathf.Abs(mouthSmileValue - lastLoggedMouthSmile) > 0.02f)
                {
                    if (showValuesInZeroToOneRange)
                    {
                        Debug.Log($"[ReadyPlayerMeLipSync] Applied mouthSmile: {mouthSmileValue:F3} (0-1 range)");
                    }
                    else
                    {
                        Debug.Log($"[ReadyPlayerMeLipSync] Applied mouthSmile: {mouthSmileValue:F3} (from currentMouthSmile: {currentMouthSmile:F3})");
                    }
                    lastLoggedMouthSmile = mouthSmileValue;
                }
            }
            else if (enableDebugLogging && mouthSmileIndex == -1)
            {
                Debug.LogWarning("[ReadyPlayerMeLipSync] Cannot apply mouthSmile - BlendShape index not found!");
            }
        }
        
        #endregion
        
        #region Speaking State Management
        
        private void StartSpeaking()
        {
            isSpeaking = true;
            speakingTime = 0f;
            
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Started speaking");
            
            if (speakingCoroutine != null)
            {
                StopCoroutine(speakingCoroutine);
            }
            
            speakingCoroutine = StartCoroutine(SpeakingAnimation());
        }
        
        private void StopSpeaking()
        {
            isSpeaking = false;
            
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Stopped speaking");
            
            if (speakingCoroutine != null)
            {
                StopCoroutine(speakingCoroutine);
                speakingCoroutine = null;
            }
        }
        
        private IEnumerator SpeakingAnimation()
        {
            while (isSpeaking)
            {
                speakingTime += Time.deltaTime;
                yield return null;
            }
        }
        
        #endregion
        
        #region Public API for External Control
        
        /// <summary>
        /// Start lip sync animation externally
        /// </summary>
        public void StartLipSync()
        {
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] External StartLipSync called");
            StartSpeaking();
        }
        
        /// <summary>
        /// Stop lip sync animation externally
        /// </summary>
        public void StopLipSync()
        {
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] External StopLipSync called");
            StopSpeaking();
        }
        
        /// <summary>
        /// Set speaking state externally
        /// </summary>
        public void SetSpeaking(bool speaking)
        {
            if (speaking && !isSpeaking)
            {
                StartSpeaking();
            }
            else if (!speaking && isSpeaking)
            {
                StopSpeaking();
            }
        }
          /// <summary>
        /// Set the target audio source for lip sync
        /// </summary>
        public void SetAudioSource(AudioSource newAudioSource)
        {
            audioSource = newAudioSource;
            if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] AudioSource set to: {(audioSource != null ? audioSource.name : "NULL")}");
        }
        
        /// <summary>
        /// Assigns the RealtimeAudioManager reference for audio amplitude analysis
        /// </summary>
        public void SetRealtimeAudioManager(RealtimeAudioManager manager)
        {
            realtimeAudioManager = manager;
            if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] RealtimeAudioManager assigned: {(manager != null ? manager.name : "NULL")}");
        }
        
        /// <summary>
        /// Get current lip sync state
        /// </summary>
        public bool IsLipSyncActive()
        {
            return isSpeaking && enableLipSync;
        }
          /// <summary>
        /// Manually set mouth open value (0-1)
        /// </summary>
        public void SetMouthOpen(float value)
        {
            if (headMeshRenderer != null && mouthOpenIndex >= 0)
            {
                float clampedValue = Mathf.Clamp01(value);
                headMeshRenderer.SetBlendShapeWeight(mouthOpenIndex, clampedValue);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[ReadyPlayerMeLipSync] SetMouthOpen: raw={value:F3}, clamped={clampedValue:F3} (0-1 range)");
                }
            }
        }
        
        /// <summary>
        /// Manually set base smile value (0-1)
        /// </summary>
        public void SetBaseSmile(float value)
        {
            mouthSmileBase = Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// Reset facial expression to neutral/base state
        /// </summary>
        public void ResetExpression()
        {
            if (headMeshRenderer != null && headMeshRenderer.sharedMesh != null)
            {
                // Reset mouth expressions to base values
                if (mouthOpenIndex >= 0)
                {
                    headMeshRenderer.SetBlendShapeWeight(mouthOpenIndex, 0f);
                }
                  if (mouthSmileIndex >= 0)
                {
                    headMeshRenderer.SetBlendShapeWeight(mouthSmileIndex, mouthSmileBase);
                }
                
                // Reset internal state
                currentMouthOpen = 0f;
                currentMouthSmile = mouthSmileBase;
                targetMouthOpen = 0f;
                targetMouthSmile = mouthSmileBase;
                
                if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Expression reset to neutral/base state");
            }
            else
            {
                Debug.LogError("[ReadyPlayerMeLipSync] Cannot reset expression - no head mesh renderer!");
            }
        }
        
        #endregion
        
        #region Context Menu Debug Methods
        
        [ContextMenu("Log Component Status")]
        public void DebugLogComponentStatus()
        {
            Debug.Log("=== ReadyPlayerMe Lip Sync Debug ===");
            Debug.Log($"HeadMeshRenderer: {(headMeshRenderer != null ? headMeshRenderer.name : "NULL")}");
            Debug.Log($"AudioSource: {(audioSource != null ? audioSource.name : "NULL")}");
            Debug.Log($"MouthOpen BlendShape Index: {mouthOpenIndex}");
            Debug.Log($"MouthSmile BlendShape Index: {mouthSmileIndex}");
            Debug.Log($"Is Speaking: {isSpeaking}");
            Debug.Log($"Lip Sync Enabled: {enableLipSync}");
            
            if (headMeshRenderer != null && headMeshRenderer.sharedMesh != null)
            {
                Debug.Log($"Mesh: {headMeshRenderer.sharedMesh.name} with {headMeshRenderer.sharedMesh.blendShapeCount} BlendShapes");
            }
        }        [ContextMenu("Test MouthOpen 50%")]
        public void TestMouthOpen()
        {
            if (headMeshRenderer != null && mouthOpenIndex >= 0)
            {
                headMeshRenderer.SetBlendShapeWeight(mouthOpenIndex, 0.5f);
                Debug.Log("[ReadyPlayerMeLipSync] Test: Set MouthOpen to 0.5 (50% in 0-1 range) - should be clearly visible!");
            }
            else
            {
                Debug.LogError("[ReadyPlayerMeLipSync] Cannot test MouthOpen - no renderer or BlendShape index!");
            }
        }
          [ContextMenu("Test MouthSmile 30%")]
        public void TestMouthSmile()
        {
            if (headMeshRenderer != null && mouthSmileIndex >= 0)
            {
                headMeshRenderer.SetBlendShapeWeight(mouthSmileIndex, 0.3f);
                Debug.Log("[ReadyPlayerMeLipSync] Test: Set MouthSmile to 0.3 (30% in 0-1 range) - should be clearly visible!");
            }
            else
            {
                Debug.LogError("[ReadyPlayerMeLipSync] Cannot test MouthSmile - no renderer or BlendShape index!");
            }
        }
          [ContextMenu("Test Strong Animation (80%)")]
        public void TestStrongAnimation()
        {
            if (headMeshRenderer != null)
            {
                if (mouthOpenIndex >= 0)
                {
                    headMeshRenderer.SetBlendShapeWeight(mouthOpenIndex, 0.8f);
                    Debug.Log("[ReadyPlayerMeLipSync] Test: Set MouthOpen to 0.8 (80%) - VERY visible!");
                }
                if (mouthSmileIndex >= 0)
                {
                    headMeshRenderer.SetBlendShapeWeight(mouthSmileIndex, 0.2f);
                    Debug.Log("[ReadyPlayerMeLipSync] Test: Set MouthSmile to 0.2 (20%) - clearly visible smile!");
                }
            }
            else
            {
                Debug.LogError("[ReadyPlayerMeLipSync] Cannot test - no head mesh renderer!");
            }
        }        [ContextMenu("Test Dramatic Animation")]
        public void TestDramaticAnimation()
        {
            if (headMeshRenderer != null)
            {
                Debug.Log("[ReadyPlayerMeLipSync] Testing dramatic mouth animation sequence...");
                StartCoroutine(DramaticAnimationSequence());
            }
            else
            {
                Debug.LogError("[ReadyPlayerMeLipSync] Cannot test - no head mesh renderer!");
            }
        }
        
        private System.Collections.IEnumerator DramaticAnimationSequence()
        {
            // Start with idle smile
            if (mouthSmileIndex >= 0)
            {
                headMeshRenderer.SetBlendShapeWeight(mouthSmileIndex, mouthSmileIdle);
                Debug.Log($"[ReadyPlayerMeLipSync] Set idle smile: {mouthSmileIdle:F1} (40%)");
            }
            
            yield return new WaitForSeconds(1f);
            
            // Simulate dramatic speech patterns
            float[] dramaticValues = { 0f, 0.8f, 0.1f, 1.0f, 0f, 0.6f, 0.05f, 0.9f, 0f };
            
            foreach (float value in dramaticValues)
            {
                if (mouthOpenIndex >= 0)
                {
                    headMeshRenderer.SetBlendShapeWeight(mouthOpenIndex, value);
                    Debug.Log($"[ReadyPlayerMeLipSync] Dramatic mouth: {value:F1} ({value*100:F0}%)");
                }
                yield return new WaitForSeconds(0.3f);
            }
            
            // Return to idle smile
            if (mouthOpenIndex >= 0) headMeshRenderer.SetBlendShapeWeight(mouthOpenIndex, 0f);
            if (mouthSmileIndex >= 0) headMeshRenderer.SetBlendShapeWeight(mouthSmileIndex, mouthSmileIdle);
            Debug.Log("[ReadyPlayerMeLipSync] Returned to idle smile");
        }
        
        [ContextMenu("Reset All BlendShapes")]
        public void ResetAllBlendShapes()
        {
            if (headMeshRenderer != null && headMeshRenderer.sharedMesh != null)
            {
                var mesh = headMeshRenderer.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    headMeshRenderer.SetBlendShapeWeight(i, 0f);
                }
                Debug.Log("[ReadyPlayerMeLipSync] Reset all BlendShapes to 0");
            }
            else
            {
                Debug.LogError("[ReadyPlayerMeLipSync] Cannot reset - no head mesh renderer!");
            }
        }
        
        [ContextMenu("Find All BlendShapes")]
        public void DebugFindAllBlendShapes()
        {
            if (headMeshRenderer != null && headMeshRenderer.sharedMesh != null)
            {
                var mesh = headMeshRenderer.sharedMesh;
                Debug.Log($"=== All BlendShapes in {mesh.name} ===");
                
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string shapeName = mesh.GetBlendShapeName(i);
                    float currentValue = headMeshRenderer.GetBlendShapeWeight(i);
                    Debug.Log($"[{i}] '{shapeName}' = {currentValue:F1}");
                }
            }
            else
            {
                Debug.LogError("[ReadyPlayerMeLipSync] No mesh found for BlendShape listing!");
            }
        }
        
        [ContextMenu("Reset to Neutral Expression")]
        public void ForceResetToNeutral()
        {
            currentMouthOpen = 0f;
            currentMouthSmile = 0f;
            targetMouthOpen = 0f;
            targetMouthSmile = 0f;
            
            if (headMeshRenderer != null)
            {
                if (mouthOpenIndex >= 0)
                    headMeshRenderer.SetBlendShapeWeight(mouthOpenIndex, 0f);
                if (mouthSmileIndex >= 0)
                    headMeshRenderer.SetBlendShapeWeight(mouthSmileIndex, 0f);
            }
            
            Debug.Log("[ReadyPlayerMeLipSync] Force reset to neutral expression completed");
        }
        
        [ContextMenu("Debug Current Values")]
        public void DebugCurrentValues()
        {
            Debug.Log($"[ReadyPlayerMeLipSync] Current Values:");
            Debug.Log($"  currentMouthOpen: {currentMouthOpen:F4} (target: {targetMouthOpen:F4})");
            Debug.Log($"  currentMouthSmile: {currentMouthSmile:F4} (target: {targetMouthSmile:F4})");
            Debug.Log($"  mouthSmileBase: {mouthSmileBase:F4}");
            Debug.Log($"  mouthSmileVariation: {mouthSmileVariation:F4}");
            Debug.Log($"  isSpeaking: {isSpeaking}");
            
            if (headMeshRenderer != null)
            {
                if (mouthOpenIndex >= 0)
                    Debug.Log($"  BlendShape mouthOpen: {headMeshRenderer.GetBlendShapeWeight(mouthOpenIndex):F1}");
                if (mouthSmileIndex >= 0)
                    Debug.Log($"  BlendShape mouthSmile: {headMeshRenderer.GetBlendShapeWeight(mouthSmileIndex):F1}");
            }
        }
        
        #endregion
        
        #region Auto-Discovery
        
        /// <summary>
        /// Automatically discovers and assigns ReadyPlayerMe components
        /// Call this method to auto-setup the LipSync system
        /// </summary>
        [ContextMenu("Auto-Discover Components")]
        public void AutoDiscoverComponents()
        {
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Starting auto-discovery...");
            
            // Find Head MeshRenderer (Wolf3D_Head)
            if (headMeshRenderer == null)
            {
                headMeshRenderer = FindWolf3DHeadRenderer();
            }
            
            // Find RealtimeAudioManager
            if (realtimeAudioManager == null)
            {
                realtimeAudioManager = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
                if (realtimeAudioManager != null)
                {
                    if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] ✅ Found RealtimeAudioManager: {realtimeAudioManager.name}");
                }
                else
                {
                    Debug.LogWarning("[ReadyPlayerMeLipSync] ❌ No RealtimeAudioManager found in scene!");
                }
            }
            
            // Find AudioSource (fallback)
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    // Try to find AudioSource in parent objects
                    Transform parent = transform.parent;
                    while (parent != null && audioSource == null)
                    {
                        audioSource = parent.GetComponent<AudioSource>();
                        parent = parent.parent;
                    }
                }
                
                if (audioSource != null)
                {
                    if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] ✅ Found AudioSource: {audioSource.name}");
                }
                else
                {
                    if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] ⚠️ No AudioSource found (will rely on RealtimeAudioManager)");
                }
            }
            
            if (enableDebugLogging) Debug.Log("[ReadyPlayerMeLipSync] Auto-discovery completed");
        }
        
        /// <summary>
        /// Finds the Wolf3D_Head SkinnedMeshRenderer in the ReadyPlayerMe avatar hierarchy
        /// </summary>
        private SkinnedMeshRenderer FindWolf3DHeadRenderer()
        {
            // Start search from current GameObject and go up/down the hierarchy
            Transform searchRoot = transform;
            
            // If we're on a child object, go to the root of the avatar
            while (searchRoot.parent != null && !searchRoot.name.Contains("Avatar"))
            {
                searchRoot = searchRoot.parent;
            }
            
            // Search in the hierarchy for Wolf3D_Head
            SkinnedMeshRenderer[] renderers = searchRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.name.Equals("Wolf3D_Head", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (enableDebugLogging) Debug.Log($"[ReadyPlayerMeLipSync] ✅ Found Wolf3D_Head renderer: {renderer.name}");
                    return renderer;
                }
            }
            
            // Fallback: look for any renderer with "head" in the name
            foreach (var renderer in renderers)
            {
                if (renderer.name.ToLower().Contains("head"))
                {
                    Debug.LogWarning($"[ReadyPlayerMeLipSync] ⚠️ Wolf3D_Head not found, using fallback: {renderer.name}");
                    return renderer;
                }
            }
            
            Debug.LogError("[ReadyPlayerMeLipSync] ❌ No head renderer found! Make sure this script is attached to a ReadyPlayerMe avatar.");
            return null;
        }
        
        #endregion

        #region Auto-Gain Debug Methods
        
        [ContextMenu("Reset Auto-Gain")]
        public void ResetAutoGain()
        {
            currentGain = 1.0f;
            peakAmplitude = 0.001f;
            Debug.Log("[ReadyPlayerMeLipSync] Auto-Gain reset to defaults");
        }
        
        [ContextMenu("Show Auto-Gain Status")]
        public void ShowAutoGainStatus()
        {
            Debug.Log($"[ReadyPlayerMeLipSync] Auto-Gain Status:\n" +
                     $"- Enabled: {enableAutoGain}\n" +
                     $"- Current Gain: {currentGain:F2}\n" +
                     $"- Peak Amplitude: {peakAmplitude:F4}\n" +
                     $"- Target Amplitude: {autoGainTarget:F2}\n" +
                     $"- Current Raw Amplitude: {currentAmplitude:F4}\n" +
                     $"- Current Final Amplitude: {(enableAutoGain ? ApplyAutoGain(currentAmplitude) : currentAmplitude):F4}");
        }
        
        [ContextMenu("Test Auto-Gain with Simulated Audio")]
        public void TestAutoGainWithSimulatedAudio()
        {
            Debug.Log("[ReadyPlayerMeLipSync] Testing Auto-Gain with simulated audio levels...");
            
            // Simulate very quiet audio
            float quietAudio = 0.001f;
            float quietResult = ApplyAutoGain(quietAudio);
            Debug.Log($"Quiet audio: {quietAudio:F4} -> {quietResult:F4} (Gain: {currentGain:F2})");
            
            // Simulate normal audio
            float normalAudio = 0.01f;
            float normalResult = ApplyAutoGain(normalAudio);
            Debug.Log($"Normal audio: {normalAudio:F4} -> {normalResult:F4} (Gain: {currentGain:F2})");
            
            // Simulate loud audio
            float loudAudio = 0.05f;
            float loudResult = ApplyAutoGain(loudAudio);
            Debug.Log($"Loud audio: {loudAudio:F4} -> {loudResult:F4} (Gain: {currentGain:F2})");
        }
        
        #endregion
    }
}
