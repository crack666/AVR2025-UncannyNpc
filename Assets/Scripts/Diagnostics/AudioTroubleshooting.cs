using System;
using System.Collections;
using UnityEngine;
using OpenAI.RealtimeAPI;

namespace Diagnostics
{
    /// <summary>
    /// Audio troubleshooting and automatic fixes for common audio issues
    /// </summary>
    public class AudioTroubleshooting : MonoBehaviour
    {
        [Header("Troubleshooting Settings")]
        [SerializeField] private bool autoFixCommonIssues = true;
        
        [Header("Audio Manager Reference")]
        [SerializeField] private RealtimeAudioManager audioManager;
        
        private void Start()
        {
            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<RealtimeAudioManager>();
            }
            
            if (autoFixCommonIssues)
            {
                StartCoroutine(RunTroubleshootingSequence());
            }
        }
        
        private IEnumerator RunTroubleshootingSequence()
        {
            yield return new WaitForSeconds(1.0f); // Wait for initialization
            
            Debug.Log("[TROUBLESHOOTING] Starting audio troubleshooting sequence...");
            
            // Step 1: Check Unity audio settings
            CheckAndFixUnityAudioSettings();
            
            yield return new WaitForSeconds(0.5f);
            
            // Step 2: Check microphone configuration
            CheckAndFixMicrophoneSettings();
            
            yield return new WaitForSeconds(0.5f);
            
            // Step 3: Check audio manager configuration
            CheckAndFixAudioManagerSettings();
            
            yield return new WaitForSeconds(0.5f);
            
            // Step 4: Test audio pipeline
            TestAudioPipeline();
            
            Debug.Log("[TROUBLESHOOTING] Troubleshooting sequence completed.");
        }
        
        [ContextMenu("Fix Common Audio Issues")]
        public void CheckAndFixUnityAudioSettings()
        {
            Debug.Log("[TROUBLESHOOTING] Checking Unity Audio Settings...");
            
            var currentConfig = AudioSettings.GetConfiguration();
            var newConfig = currentConfig;
            bool needsUpdate = false;
            
            // Fix sample rate
            if (currentConfig.sampleRate != 24000)
            {
                Debug.LogWarning($"[TROUBLESHOOTING] Sample rate is {currentConfig.sampleRate}Hz, should be 24000Hz");
                newConfig.sampleRate = 24000;
                needsUpdate = true;
            }
            
            // Fix DSP buffer size for low latency
            if (currentConfig.dspBufferSize > 512)
            {
                Debug.LogWarning($"[TROUBLESHOOTING] DSP buffer size is {currentConfig.dspBufferSize}, reducing for lower latency");
                newConfig.dspBufferSize = 256; // Lower latency
                needsUpdate = true;
            }
            
            // Ensure adequate voice count
            if (currentConfig.numRealVoices < 16)
            {
                Debug.LogWarning($"[TROUBLESHOOTING] Real voice count is {currentConfig.numRealVoices}, increasing for better audio performance");
                newConfig.numRealVoices = 32;
                needsUpdate = true;
            }
            
            if (needsUpdate)
            {
                Debug.Log("[TROUBLESHOOTING] Applying optimized audio settings...");
                AudioSettings.Reset(newConfig);
                Debug.Log("[TROUBLESHOOTING] ✅ Audio settings updated successfully!");
            }
            else
            {
                Debug.Log("[TROUBLESHOOTING] ✅ Unity audio settings are optimal");
            }
        }
        
        private void CheckAndFixMicrophoneSettings()
        {
            Debug.Log("[TROUBLESHOOTING] Checking microphone configuration...");
            
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[TROUBLESHOOTING] ❌ No microphone devices found!");
                Debug.LogError("[TROUBLESHOOTING] 💡 Solutions:");
                Debug.LogError("   • Check microphone is connected and enabled in Windows");
                Debug.LogError("   • Restart Unity after connecting microphone");
                Debug.LogError("   • Check Windows Privacy Settings > Microphone permissions");
                return;
            }
            
            string defaultMic = Microphone.devices[0];
            Debug.Log($"[TROUBLESHOOTING] Default microphone: '{defaultMic}'");
            
            // Test microphone capabilities
            Microphone.GetDeviceCaps(defaultMic, out int minFreq, out int maxFreq);
            Debug.Log($"[TROUBLESHOOTING] Microphone frequency range: {minFreq}Hz - {maxFreq}Hz");
            
            if (minFreq > 24000 || maxFreq < 24000)
            {
                Debug.LogWarning("[TROUBLESHOOTING] ⚠️ Microphone may not support 24kHz sampling!");
                Debug.LogWarning("[TROUBLESHOOTING] 💡 Try using 44.1kHz or 48kHz in Windows Sound Settings");
            }
            else
            {
                Debug.Log("[TROUBLESHOOTING] ✅ Microphone supports required sample rates");
            }
        }
        
        private void CheckAndFixAudioManagerSettings()
        {
            Debug.Log("[TROUBLESHOOTING] Checking RealtimeAudioManager configuration...");
            
            if (audioManager == null)
            {
                Debug.LogError("[TROUBLESHOOTING] ❌ RealtimeAudioManager not found!");
                return;
            }
            
            // Check audio sources
            var micSource = audioManager.GetComponent<AudioSource>();
            var playbackSource = GetPlaybackAudioSource();
            
            if (micSource == null)
            {
                Debug.LogWarning("[TROUBLESHOOTING] ⚠️ Microphone AudioSource missing, adding...");
                micSource = audioManager.gameObject.AddComponent<AudioSource>();
                micSource.loop = true;
                micSource.mute = true;
                micSource.playOnAwake = false;
            }
            
            if (playbackSource == null)
            {
                Debug.LogWarning("[TROUBLESHOOTING] ⚠️ Playback AudioSource missing or misconfigured");
            }
            else
            {
                // Optimize playback settings
                playbackSource.playOnAwake = false;
                playbackSource.loop = false;
                playbackSource.volume = 1.0f;
                playbackSource.spatialBlend = 0.0f; // 2D audio for UI
                Debug.Log("[TROUBLESHOOTING] ✅ Playback AudioSource configured");
            }
            
            Debug.Log("[TROUBLESHOOTING] ✅ RealtimeAudioManager configuration checked");
        }
        
        private AudioSource GetPlaybackAudioSource()
        {
            // Look for playback audio source
            AudioSource[] audioSources = audioManager.GetComponentsInChildren<AudioSource>();
            foreach (var source in audioSources)
            {
                if (!source.mute && source != audioManager.GetComponent<AudioSource>())
                {
                    return source;
                }
            }
            return null;
        }
        
        private void TestAudioPipeline()
        {
            Debug.Log("[TROUBLESHOOTING] Testing audio pipeline...");
            
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[TROUBLESHOOTING] ❌ Cannot test - no microphones available");
                return;
            }
            
            StartCoroutine(RunAudioPipelineTest());
        }
        
        private IEnumerator RunAudioPipelineTest()
        {
            string device = Microphone.devices[0];
            Debug.Log($"[TROUBLESHOOTING] Testing with microphone: '{device}'");
            
            // Test microphone recording
            AudioClip testClip = null;
            
            testClip = Microphone.Start(device, false, 2, 24000);
            if (testClip == null)
            {
                Debug.LogError("[TROUBLESHOOTING] ❌ Failed to start microphone recording");
                yield break;
            }
            
            Debug.Log("[TROUBLESHOOTING] 🎤 Microphone recording started...");
            yield return new WaitForSeconds(1.5f);
            
            try
            {
                Microphone.End(device);
                
                // Analyze recorded audio
                float[] samples = new float[testClip.samples];
                testClip.GetData(samples, 0);
                
                float maxAmplitude = 0f;
                float totalEnergy = 0f;
                
                foreach (float sample in samples)
                {
                    float absValue = Mathf.Abs(sample);
                    if (absValue > maxAmplitude) maxAmplitude = absValue;
                    totalEnergy += absValue;
                }
                
                float avgAmplitude = totalEnergy / samples.Length;
                
                Debug.Log($"[TROUBLESHOOTING] 📊 Audio analysis:");
                Debug.Log($"   Max Amplitude: {maxAmplitude:F4}");
                Debug.Log($"   Avg Amplitude: {avgAmplitude:F4}");
                Debug.Log($"   Sample Count: {samples.Length}");
                Debug.Log($"   Sample Rate: {testClip.frequency}Hz");
                
                // Provide feedback
                if (maxAmplitude < 0.001f)
                {
                    Debug.LogError("[TROUBLESHOOTING] ❌ No audio signal detected!");
                    Debug.LogError("[TROUBLESHOOTING] 💡 Possible causes:");
                    Debug.LogError("   • Microphone is muted or disabled");
                    Debug.LogError("   • Wrong microphone selected as default");
                    Debug.LogError("   • Microphone permission not granted");
                    Debug.LogError("   • Hardware issue");
                }
                else if (maxAmplitude < 0.01f)
                {
                    Debug.LogWarning("[TROUBLESHOOTING] ⚠️ Very weak audio signal");
                    Debug.LogWarning("[TROUBLESHOOTING] 💡 Try:");
                    Debug.LogWarning("   • Increase microphone volume in Windows");
                    Debug.LogWarning("   • Move closer to microphone");
                    Debug.LogWarning("   • Check microphone boost settings");
                }
                else if (maxAmplitude > 0.9f)
                {
                    Debug.LogWarning("[TROUBLESHOOTING] ⚠️ Audio signal may be clipping");
                    Debug.LogWarning("[TROUBLESHOOTING] 💡 Reduce microphone volume in Windows");
                }
                else
                {
                    Debug.Log("[TROUBLESHOOTING] ✅ Good audio signal detected!");
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TROUBLESHOOTING] ❌ Audio pipeline test failed: {ex.Message}");
            }
            finally
            {
                if (testClip != null)
                {
                    DestroyImmediate(testClip);
                }
            }
        }
        
        [ContextMenu("Print Troubleshooting Guide")]
        public void PrintTroubleshootingGuide()
        {
            Debug.Log("=== AUDIO TROUBLESHOOTING GUIDE ===");
            Debug.Log("");
            Debug.Log("🔧 COMMON ISSUES & SOLUTIONS:");
            Debug.Log("");
            Debug.Log("1. STUTTERING/CHOPPY AUDIO:");
            Debug.Log("   ✓ Set Unity Project Settings > Audio > System Sample Rate to 24000");
            Debug.Log("   ✓ Set DSP Buffer Size to 'Best Latency'");
            Debug.Log("   ✓ Increase Stream Buffer Size to 2048 or 4096");
            Debug.Log("   ✓ Check Windows Sound Settings - disable audio enhancements");
            Debug.Log("");
            Debug.Log("2. NO AUDIO RESPONSE:");
            Debug.Log("   ✓ Check OpenAI API key and quota");
            Debug.Log("   ✓ Verify internet connection stability");
            Debug.Log("   ✓ Check Unity Console for WebSocket errors");
            Debug.Log("   ✓ Test with shorter audio input (3-5 seconds max)");
            Debug.Log("");
            Debug.Log("3. MICROPHONE NOT WORKING:");
            Debug.Log("   ✓ Set correct default microphone in Windows");
            Debug.Log("   ✓ Grant microphone permissions to Unity");
            Debug.Log("   ✓ Disable 'Exclusive Mode' in microphone properties");
            Debug.Log("   ✓ Check microphone sample rate (should support 24kHz)");
            Debug.Log("");
            Debug.Log("4. AUDIO QUALITY ISSUES:");
            Debug.Log("   ✓ Use a good quality USB microphone");
            Debug.Log("   ✓ Reduce background noise");
            Debug.Log("   ✓ Adjust VAD threshold (0.015 - 0.03)");
            Debug.Log("   ✓ Enable noise gate with appropriate threshold");
            Debug.Log("");
            Debug.Log("=====================================");
        }
    }
}
