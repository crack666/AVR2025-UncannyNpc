using UnityEngine;

namespace Animation
{
    /// <summary>
    /// Fallback LipSync Component for basic mouth animation.
    /// Used only when uLipSync is not available.
    /// </summary>
    public class ReadyPlayerMeLipSync : MonoBehaviour
    {
        [Header("ReadyPlayerMe Integration")]
        [SerializeField] private SkinnedMeshRenderer facialRenderer;
        [SerializeField] private string mouthOpenBlendShape = "mouthOpen";
        [SerializeField] private string mouthSmileBlendShape = "mouthSmile";

        [Header("Audio Settings")]
        [SerializeField] private float sensitivity = 2.0f;
        [SerializeField] private float smoothing = 8.0f;
        [SerializeField] private float minMouthOpen = 0.0f;
        [SerializeField] private float maxMouthOpen = 0.7f;

        // New fields for smile, threshold, formant etc.
        [SerializeField] private float currentMouthSmile = 0f;
        [SerializeField] private float targetMouthSmile = 0f;
        [SerializeField] private float volumeThreshold = 0.01f;
        [SerializeField] private bool useFormantAnalysis = false;
        [SerializeField] private float formantSensitivity = 8.0f;

        private int mouthOpenIndex = -1;
        private int mouthSmileIndex = -1;
        private float currentMouthOpen = 0f;
        private float targetMouthOpen = 0f;
        private OpenAI.RealtimeAPI.RealtimeAudioManager audioManager;
        private AudioSource connectedAudioSource;
        private float[] audioSamples = new float[1024];
        private float[] spectrumData = new float[1024];

        // Formant analysis for better phoneme detection
        private float lastF1 = 0f; // First formant (jaw opening)
        private float lastF2 = 0f; // Second formant (tongue position)

        private void Start()
        {
            // Auto-find facial renderer
            if (facialRenderer == null)
            {
                // Search in children for facial renderer
                SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                    {
                        // Check for mouth BlendShapes
                        for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                        {
                            string shapeName = renderer.sharedMesh.GetBlendShapeName(i);
                            if (shapeName.ToLower().Contains("mouth"))
                            {
                                facialRenderer = renderer;
                                break;
                            }
                        }
                        if (facialRenderer != null) break;
                    }
                }
            }
            // Get BlendShape indices
            if (facialRenderer != null && facialRenderer.sharedMesh != null)
            {
                mouthOpenIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(mouthOpenBlendShape);
                mouthSmileIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(mouthSmileBlendShape);
                // Try alternatives if not found
                if (mouthOpenIndex < 0)
                {
                    string[] alternatives = { "jawOpen", "JawOpen", "mouth_open" };
                    foreach (string alt in alternatives)
                    {
                        mouthOpenIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(alt);
                        if (mouthOpenIndex >= 0)
                        {
                            mouthOpenBlendShape = alt;
                            break;
                        }
                    }
                }
                if (mouthSmileIndex < 0)
                {
                    string[] alternatives = { "mouthSmileLeft", "mouthSmileRight", "mouth_smile", "Mouth_Smile" };
                    foreach (string alt in alternatives)
                    {
                        mouthSmileIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(alt);
                        if (mouthSmileIndex >= 0)
                        {
                            mouthSmileBlendShape = alt;
                            break;
                        }
                    }
                }
                Debug.Log($"[ReadyPlayerMeLipSync] BlendShapes - mouthOpen: {mouthOpenIndex} ({mouthOpenBlendShape}), mouthSmile: {mouthSmileIndex} ({mouthSmileBlendShape})");
            }
            // Find and connect to RealtimeAudioManager
            #if UNITY_2022_1_OR_NEWER
            audioManager = FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            #else
            audioManager = FindObjectOfType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            #endif
            if (audioManager != null)
            {
                // Subscribe to audio level changes
                // audioManager.OnMicrophoneLevelChanged.AddListener(OnAudioLevelChanged);
                Debug.Log("[ReadyPlayerMeLipSync] Connected to RealtimeAudioManager");
            }
            else
            {
                Debug.LogWarning("[ReadyPlayerMeLipSync] RealtimeAudioManager not found - manual control only");
            }
        }

        private void Update()
        {
            // Audioanalyse für LipSync
            AnalyzeAudio();
            // Smooth mouth movement
            currentMouthOpen = Mathf.Lerp(currentMouthOpen, targetMouthOpen, Time.deltaTime * smoothing);
            currentMouthSmile = Mathf.Lerp(currentMouthSmile, targetMouthSmile, Time.deltaTime * smoothing);
            // Apply BlendShapes
            ApplyBlendShapes();
        }

        /// <summary>
        /// Analysiert Audio für LipSync
        /// </summary>
        private void AnalyzeAudio()
        {
            if (connectedAudioSource == null || !connectedAudioSource.isPlaying)
            {
                // Fade to neutral when not speaking
                targetMouthOpen = 0f;
                targetMouthSmile = 0f;
                return;
            }

            // Get audio data
            connectedAudioSource.GetOutputData(audioSamples, 0);
            connectedAudioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

            // Calculate RMS (volume)
            float rms = CalculateRMS(audioSamples);

            if (rms < volumeThreshold)
            {
                // No significant audio - fade to neutral
                targetMouthOpen = 0f;
                targetMouthSmile = 0f;
                return;
            }

            if (useFormantAnalysis)
            {
                AnalyzeFormants(rms);
            }
            else
            {
                // Simple volume-based animation
                targetMouthOpen = Mathf.Clamp(rms * sensitivity, minMouthOpen, maxMouthOpen);
                targetMouthSmile = 0f;
            }
        }

        /// <summary>
        /// Analysiert Formanten für bessere Phonem-Erkennung
        /// </summary>
        private void AnalyzeFormants(float volume)
        {
            // Find dominant frequencies (simplified formant analysis)
            float f1 = FindFormant(spectrumData, 200, 800);   // First formant (jaw opening)
            float f2 = FindFormant(spectrumData, 800, 2500);  // Second formant (tongue position)

            // Smooth formant changes
            lastF1 = Mathf.Lerp(lastF1, f1, Time.deltaTime * formantSensitivity);
            lastF2 = Mathf.Lerp(lastF2, f2, Time.deltaTime * formantSensitivity);

            // Map formants to mouth shapes
            float openness = Mathf.InverseLerp(800f, 200f, lastF1); // Inverted - lower F1 = more open
            float frontness = Mathf.InverseLerp(800f, 2500f, lastF2); // Higher F2 = more front/smile

            // Apply volume scaling
            targetMouthOpen = Mathf.Clamp(openness * volume * sensitivity, minMouthOpen, maxMouthOpen);
            targetMouthSmile = Mathf.Clamp(frontness * volume * sensitivity * 0.5f, 0f, 0.6f);
        }

        /// <summary>
        /// Findet dominante Frequenz in einem Bereich
        /// </summary>
        private float FindFormant(float[] spectrum, float minFreq, float maxFreq)
        {
            int minIndex = Mathf.RoundToInt(minFreq / 22050f * spectrum.Length);
            int maxIndex = Mathf.RoundToInt(maxFreq / 22050f * spectrum.Length);

            float maxMagnitude = 0f;
            int peakIndex = minIndex;

            for (int i = minIndex; i < maxIndex && i < spectrum.Length; i++)
            {
                if (spectrum[i] > maxMagnitude)
                {
                    maxMagnitude = spectrum[i];
                    peakIndex = i;
                }
            }

            return (float)peakIndex / spectrum.Length * 22050f;
        }

        /// <summary>
        /// Berechnet RMS (Root Mean Square) für Lautstärke
        /// </summary>
        private float CalculateRMS(float[] samples)
        {
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }
            return Mathf.Sqrt(sum / samples.Length);
        }

        /// <summary>
        /// Wendet BlendShapes an
        /// </summary>
        private void ApplyBlendShapes()
        {
            if (facialRenderer == null) return;

            if (mouthOpenIndex >= 0)
            {
                facialRenderer.SetBlendShapeWeight(mouthOpenIndex, currentMouthOpen * 100f);
            }

            if (mouthSmileIndex >= 0)
            {
                facialRenderer.SetBlendShapeWeight(mouthSmileIndex, currentMouthSmile * 100f);
            }
        }

        /// <summary>
        /// Manueller Audio Level Callback (für RealtimeAudioManager Events)
        /// </summary>
        // TODO: Obsolete?
        public void OnAudioLevelChanged(float level)
        {
            if (!useFormantAnalysis)
            {
                targetMouthOpen = Mathf.Clamp(level * sensitivity, minMouthOpen, maxMouthOpen);
            }
        }

        /// <summary>
        /// Öffentliche Methode zum manuellen Setzen der Mund-Animation
        /// </summary>
        // TODO: Obsolete?
        public void SetMouthAnimation(float openAmount, float smileAmount)
        {
            targetMouthOpen = Mathf.Clamp(openAmount, minMouthOpen, maxMouthOpen);
            targetMouthSmile = Mathf.Clamp(smileAmount, 0f, 1f);
        }

        /// <summary>
        /// Kalibriert die Sensitivität basierend auf aktueller Audio-Eingabe
        /// </summary>
        // TODO: Obsolete?
        public void CalibrateAudioSensitivity()
        {
            if (connectedAudioSource != null && connectedAudioSource.isPlaying)
            {
                connectedAudioSource.GetOutputData(audioSamples, 0);
                float currentRMS = CalculateRMS(audioSamples);

                if (currentRMS > 0.001f) // Avoid division by zero
                {
                    float optimalSensitivity = 0.5f / currentRMS; // Target 50% mouth opening for current volume
                    sensitivity = Mathf.Clamp(optimalSensitivity, 0.5f, 10f);
                    Debug.Log($"[ReadyPlayerMeLipSync] Auto-calibrated sensitivity to: {sensitivity:F2}");
                }
            }
        }

        #region Debug and Utilities

        [System.Serializable]
        public class LipSyncDebugInfo
        {
            public float currentVolume;
            public float f1Frequency;
            public float f2Frequency;
            public float mouthOpenValue;
            public float mouthSmileValue;
            public bool isConnectedToAudio;
            public string connectedAudioSourceName;
        }

        /// <summary>
        /// Gibt Debug-Informationen zurück
        /// </summary>
        // TODO: Obsolete?
        public LipSyncDebugInfo GetDebugInfo()
        {
            var info = new LipSyncDebugInfo();

            if (connectedAudioSource != null)
            {
                connectedAudioSource.GetOutputData(audioSamples, 0);
                info.currentVolume = CalculateRMS(audioSamples);
                info.isConnectedToAudio = true;
                info.connectedAudioSourceName = connectedAudioSource.name;
            }

            info.f1Frequency = lastF1;
            info.f2Frequency = lastF2;
            info.mouthOpenValue = currentMouthOpen;
            info.mouthSmileValue = currentMouthSmile;

            return info;
        }

        /// <summary>
        /// Zeigt verfügbare BlendShapes im Log
        /// </summary>
        // TODO: Obsolete?
        [ContextMenu("Log Available BlendShapes")]
        public void LogAvailableBlendShapes()
        {
            if (facialRenderer?.sharedMesh != null)
            {
                Debug.Log($"[ReadyPlayerMeLipSync] Available BlendShapes on {facialRenderer.name}:");
                for (int i = 0; i < facialRenderer.sharedMesh.blendShapeCount; i++)
                {
                    string shapeName = facialRenderer.sharedMesh.GetBlendShapeName(i);
                    Debug.Log($"   [{i}] {shapeName}");
                }
            }
        }

        /// <summary>
        /// Test-Methode für BlendShape Animation
        /// </summary>
        // TODO: Obsolete?
        [ContextMenu("Test Mouth Animation")]
        public void TestMouthAnimation()
        {
            StartCoroutine(TestAnimationCoroutine());
        }

        private System.Collections.IEnumerator TestAnimationCoroutine()
        {
            Debug.Log("[ReadyPlayerMeLipSync] Testing mouth animation...");

            // Test mouth open
            SetMouthAnimation(0.8f, 0f);
            yield return new UnityEngine.WaitForSeconds(1f);

            // Test mouth smile
            SetMouthAnimation(0f, 0.8f);
            yield return new UnityEngine.WaitForSeconds(1f);

            // Test combined
            SetMouthAnimation(0.5f, 0.5f);
            yield return new UnityEngine.WaitForSeconds(1f);

            // Return to neutral
            SetMouthAnimation(0f, 0f);

            Debug.Log("[ReadyPlayerMeLipSync] Animation test complete");
        }

        #endregion
    }
}
