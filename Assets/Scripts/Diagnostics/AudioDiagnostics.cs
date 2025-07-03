using System;
using System.Linq;
using UnityEngine;

namespace Diagnostics
{
    /// <summary>
    /// Audio system diagnostics for troubleshooting audio issues across different systems
    /// </summary>
    public class AudioDiagnostics : MonoBehaviour
    {
        [Header("Diagnostic Controls")]
        [SerializeField] private bool runDiagnosticsOnStart = true;
        
        [Header("Audio Test Settings")]
        [SerializeField] private float testDuration = 3.0f;
        [SerializeField] private int testSampleRate = 24000;
        
        private void Start()
        {
            if (runDiagnosticsOnStart)
            {
                RunFullDiagnostics();
            }
        }
        
        [ContextMenu("Run Audio Diagnostics")]
        public void RunFullDiagnostics()
        {
            Debug.Log("=== AUDIO DIAGNOSTICS START ===");
            
            CheckSystemAudioCapabilities();
            CheckMicrophoneDevices();
            CheckUnityAudioSettings();
            CheckAudioDrivers();
            TestMicrophoneRecording();
            
            Debug.Log("=== AUDIO DIAGNOSTICS END ===");
        }
        
        private void CheckSystemAudioCapabilities()
        {
            Debug.Log("[DIAGNOSTICS] System Audio Capabilities:");
            
            // Unity Audio System
            var audioConfig = AudioSettings.GetConfiguration();
            Debug.Log($"  Unity Audio Config: Sample Rate = {audioConfig.sampleRate}Hz, DSP Buffer = {audioConfig.dspBufferSize}, Speaker Mode = {audioConfig.speakerMode}");
            
            // System Info
            Debug.Log($"  Operating System: {SystemInfo.operatingSystem}");
            Debug.Log($"  Audio Support: {(SystemInfo.supportsAudio ? "YES" : "NO")}");
            
            // Performance Info
            Debug.Log($"  System Memory: {SystemInfo.systemMemorySize}MB");
            Debug.Log($"  Graphics Memory: {SystemInfo.graphicsMemorySize}MB");
        }
        
        private void CheckMicrophoneDevices()
        {
            Debug.Log("[DIAGNOSTICS] Microphone Devices:");
            
            string[] devices = Microphone.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("  ❌ NO MICROPHONE DEVICES FOUND!");
                return;
            }
            
            Debug.Log($"  Found {devices.Length} microphone device(s):");
            for (int i = 0; i < devices.Length; i++)
            {
                string deviceName = devices[i];
                Debug.Log($"    [{i}] {deviceName}");
                
                // Test microphone capabilities
                TestMicrophoneCapabilities(deviceName);
            }
        }
        
        private void TestMicrophoneCapabilities(string deviceName)
        {
            try
            {
                // Test different sample rates
                int[] testRates = { 8000, 16000, 22050, 24000, 44100, 48000 };
                
                Debug.Log($"      Testing capabilities for '{deviceName}':");
                
                foreach (int sampleRate in testRates)
                {
                    // Try to get microphone capabilities
                    Microphone.GetDeviceCaps(deviceName, out int minFreq, out int maxFreq);
                    
                    bool supportsRate = (sampleRate >= minFreq && sampleRate <= maxFreq);
                    string status = supportsRate ? "✅" : "❌";
                    Debug.Log($"        {status} {sampleRate}Hz (Device range: {minFreq}-{maxFreq}Hz)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"      ❌ Error testing microphone '{deviceName}': {ex.Message}");
            }
        }
        
        private void CheckUnityAudioSettings()
        {
            Debug.Log("[DIAGNOSTICS] Unity Audio Settings:");
            
            var config = AudioSettings.GetConfiguration();
            Debug.Log($"  Sample Rate: {config.sampleRate}Hz");
            Debug.Log($"  DSP Buffer Size: {config.dspBufferSize}");
            Debug.Log($"  Num Real Voices: {config.numRealVoices}");
            Debug.Log($"  Num Virtual Voices: {config.numVirtualVoices}");
            Debug.Log($"  Speaker Mode: {config.speakerMode}");
            
            // Check if settings are optimal for our use case
            if (config.sampleRate != 24000)
            {
                Debug.LogWarning("  ⚠️ Unity sample rate is not 24kHz - this may cause audio quality issues!");
                Debug.LogWarning("  💡 Recommendation: Set Project Settings > Audio > System Sample Rate to 24000");
            }
            
            if (config.dspBufferSize > 512)
            {
                Debug.LogWarning("  ⚠️ DSP Buffer size is large - this may cause audio latency!");
                Debug.LogWarning("  💡 Recommendation: Set Project Settings > Audio > DSP Buffer Size to 'Best Latency'");
            }
        }
        
        private void CheckAudioDrivers()
        {
            Debug.Log("[DIAGNOSTICS] Audio Driver Information:");
            
            // This is platform-specific information
            #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Debug.Log("  Platform: Windows");
            Debug.Log("  💡 Common Issues on Windows:");
            Debug.Log("    - Exclusive mode microphone access by other applications");
            Debug.Log("    - Sample rate mismatch in Windows sound settings");
            Debug.Log("    - Audio enhancements interfering with recording");
            Debug.Log("  💡 Solutions:");
            Debug.Log("    - Check Windows Sound Settings > Recording > Microphone Properties");
            Debug.Log("    - Disable 'Exclusive Mode' and 'Audio Enhancements'");
            Debug.Log("    - Set microphone sample rate to 24000Hz in Windows");
            #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            Debug.Log("  Platform: macOS");
            Debug.Log("  💡 Common Issues on macOS:");
            Debug.Log("    - Audio Input permissions");
            Debug.Log("    - Sample rate conversion by Core Audio");
            Debug.Log("  💡 Solutions:");
            Debug.Log("    - Check System Preferences > Security & Privacy > Microphone");
            Debug.Log("    - Use Audio MIDI Setup to check sample rates");
            #else
            Debug.Log("  Platform: Other/Linux");
            Debug.Log("  💡 Check ALSA/PulseAudio configuration");
            #endif
        }
        
        private void TestMicrophoneRecording()
        {
            Debug.Log("[DIAGNOSTICS] Testing Microphone Recording:");
            
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("  ❌ Cannot test - no microphones available");
                return;
            }
            
            string defaultDevice = Microphone.devices[0];
            Debug.Log($"  Testing with device: '{defaultDevice}'");
            
            try
            {
                // Test recording at our target sample rate
                AudioClip testClip = Microphone.Start(defaultDevice, false, Mathf.CeilToInt(testDuration), testSampleRate);
                
                if (testClip != null)
                {
                    Debug.Log($"  ✅ Microphone recording started successfully");
                    Debug.Log($"  📊 Test Clip: {testClip.frequency}Hz, {testClip.channels} channel(s), {testClip.length}s");
                    
                    // Stop recording after a short test
                    StartCoroutine(StopTestRecording(defaultDevice, testClip));
                }
                else
                {
                    Debug.LogError("  ❌ Failed to start microphone recording");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"  ❌ Microphone test failed: {ex.Message}");
            }
        }
        
        private System.Collections.IEnumerator StopTestRecording(string device, AudioClip clip)
        {
            yield return new WaitForSeconds(1.0f); // Short test duration
            
            Microphone.End(device);
            
            if (clip != null)
            {
                // Check if we got audio data
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                
                float maxAmplitude = samples.Max(Mathf.Abs);
                float avgAmplitude = samples.Average(Mathf.Abs);
                
                Debug.Log($"  📊 Recording Results:");
                Debug.Log($"    Max Amplitude: {maxAmplitude:F4}");
                Debug.Log($"    Avg Amplitude: {avgAmplitude:F4}");
                
                if (maxAmplitude < 0.001f)
                {
                    Debug.LogWarning("  ⚠️ Very low amplitude detected - microphone may not be working or is muted");
                }
                else if (maxAmplitude > 0.1f)
                {
                    Debug.Log("  ✅ Good microphone signal detected");
                }
                else
                {
                    Debug.Log("  ℹ️ Microphone signal detected but may be quiet");
                }
                
                DestroyImmediate(clip);
            }
        }
        
        [ContextMenu("Print Recommended Settings")]
        public void PrintRecommendedSettings()
        {
            Debug.Log("=== RECOMMENDED UNITY SETTINGS ===");
            Debug.Log("Project Settings > Audio:");
            Debug.Log("  • System Sample Rate: 24000");
            Debug.Log("  • DSP Buffer Size: Best Latency");
            Debug.Log("  • Virtual Voice Count: 512");
            Debug.Log("  • Real Voice Count: 32");
            Debug.Log("");
            Debug.Log("RealtimeAudioManager Settings:");
            Debug.Log("  • Use Gapless Streaming: TRUE (always enabled)");
            Debug.Log("  • Stream Buffer Size: 1024");
            Debug.Log("  • Noise Gate Threshold: 0.01");
            Debug.Log("  • Server-side VAD: Enabled via OpenAI API");
            Debug.Log("================================");
        }
    }
}
