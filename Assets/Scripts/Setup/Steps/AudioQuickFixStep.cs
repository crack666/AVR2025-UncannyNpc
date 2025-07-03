using UnityEngine;
using UnityEditor;

namespace Setup.Steps
{
    /// <summary>
    /// Quick audio fixes for common issues across different systems
    /// </summary>
    public class AudioQuickFixStep
    {
        private System.Action<string> log;
        
        public AudioQuickFixStep(System.Action<string> log)
        {
            this.log = log;
        }
        
        public void ExecuteSync(GameObject targetAvatar, GameObject npcSystem)
        {
            log("🔧 Step: Audio Quick Fix - Optimizing for cross-system compatibility");
            
            // Apply Unity audio settings optimizations
            ApplyUnityAudioSettings();
            
            // Configure RealtimeAudioManager for better compatibility
            ConfigureAudioManager(npcSystem);
            
            // Add diagnostic components
            AddDiagnosticComponents(npcSystem);
            
            log("✅ Audio quick fixes applied successfully");
        }
        
        private void ApplyUnityAudioSettings()
        {
            log("🎵 Applying optimal Unity audio settings...");
            
            var currentConfig = AudioSettings.GetConfiguration();
            var newConfig = currentConfig;
            bool needsUpdate = false;
            
            // Set optimal sample rate for OpenAI Realtime API
            if (currentConfig.sampleRate != 24000)
            {
                log($"   📊 Sample rate: {currentConfig.sampleRate}Hz → 24000Hz (OpenAI optimal)");
                newConfig.sampleRate = 24000;
                needsUpdate = true;
            }
            
            // Optimize DSP buffer for low latency
            if (currentConfig.dspBufferSize > 512)
            {
                log($"   ⚡ DSP buffer: {currentConfig.dspBufferSize} → 256 (lower latency)");
                newConfig.dspBufferSize = 256;
                needsUpdate = true;
            }
            
            // Ensure adequate voice count
            if (currentConfig.numRealVoices < 32)
            {
                log($"   🎤 Real voices: {currentConfig.numRealVoices} → 32 (better audio performance)");
                newConfig.numRealVoices = 32;
                needsUpdate = true;
            }
            
            if (needsUpdate)
            {
                AudioSettings.Reset(newConfig);
                log("   ✅ Unity audio settings optimized!");
            }
            else
            {
                log("   ✅ Unity audio settings already optimal");
            }
        }
        
        private void ConfigureAudioManager(GameObject npcSystem)
        {
            var audioManager = npcSystem.GetComponentInChildren<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            if (audioManager == null)
            {
                log("   ⚠️ RealtimeAudioManager not found - audio configuration skipped");
                return;
            }
            
            log("🎛️ Configuring RealtimeAudioManager for cross-system compatibility...");
            
            // Use reflection to set private fields for better compatibility
            var audioManagerType = audioManager.GetType();
            
            // Enable adaptive buffering for different system capabilities
            var useAdaptiveField = audioManagerType.GetField("useAdaptiveBuffering", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (useAdaptiveField != null)
            {
                useAdaptiveField.SetValue(audioManager, true);
                log("   ✅ Adaptive buffering enabled");
            }
            
            // Enable performance monitoring
            var perfMonitoringField = audioManagerType.GetField("enablePerformanceMonitoring", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (perfMonitoringField != null)
            {
                perfMonitoringField.SetValue(audioManager, true);
                log("   ✅ Performance monitoring enabled");
            }
            
            // Set conservative initial buffer size for compatibility
            var streamBufferField = audioManagerType.GetField("streamBufferSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (streamBufferField != null)
            {
                streamBufferField.SetValue(audioManager, 2048); // Conservative starting point
                log("   ✅ Stream buffer size set to 2048 (conservative)");
            }
            
            // Optimize VAD threshold for different microphone sensitivities
            var vadThresholdField = audioManagerType.GetField("vadThreshold", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (vadThresholdField != null)
            {
                vadThresholdField.SetValue(audioManager, 0.025f); // Slightly higher for noise immunity
                log("   ✅ VAD threshold optimized for noise immunity");
            }
            
            log("   ✅ RealtimeAudioManager configured successfully");
        }
        
        private void AddDiagnosticComponents(GameObject npcSystem)
        {
            log("🔍 Adding diagnostic components...");
            
            // Add AudioDiagnostics if not present
            if (npcSystem.GetComponent<Diagnostics.AudioDiagnostics>() == null)
            {
                var diagnostics = npcSystem.AddComponent<Diagnostics.AudioDiagnostics>();
                
                // Configure diagnostics using reflection
                var diagType = diagnostics.GetType();
                
                var runOnStartField = diagType.GetField("runDiagnosticsOnStart", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (runOnStartField != null)
                {
                    runOnStartField.SetValue(diagnostics, false); // Don't run automatically, only on demand
                }
                
                log("   ✅ AudioDiagnostics component added");
            }
            
            // Add AudioTroubleshooting if not present
            if (npcSystem.GetComponent<Diagnostics.AudioTroubleshooting>() == null)
            {
                var troubleshooting = npcSystem.AddComponent<Diagnostics.AudioTroubleshooting>();
                
                // Configure troubleshooting using reflection
                var troubleType = troubleshooting.GetType();
                
                var autoFixField = troubleType.GetField("autoFixCommonIssues", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (autoFixField != null)
                {
                    autoFixField.SetValue(troubleshooting, true); // Enable auto-fix
                }
                
                log("   ✅ AudioTroubleshooting component added with auto-fix enabled");
            }
        }
        
        // Helper method for runtime audio optimization
        public static void ApplyRuntimeAudioOptimizations()
        {
            Debug.Log("[AudioQuickFix] Applying runtime audio optimizations...");
            
            // Check current microphone setup
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[AudioQuickFix] ❌ No microphone devices found!");
                Debug.LogError("[AudioQuickFix] 💡 Check Windows Privacy Settings → Microphone permissions");
                return;
            }
            
            // Test default microphone
            string defaultMic = Microphone.devices[0];
            Microphone.GetDeviceCaps(defaultMic, out int minFreq, out int maxFreq);
            
            if (minFreq > 24000 || maxFreq < 24000)
            {
                Debug.LogWarning($"[AudioQuickFix] ⚠️ Default microphone '{defaultMic}' may not support 24kHz!");
                Debug.LogWarning("[AudioQuickFix] 💡 Consider using 48kHz in Windows sound settings");
            }
            else
            {
                Debug.Log($"[AudioQuickFix] ✅ Default microphone '{defaultMic}' supports required frequencies");
            }
            
            // Check Unity audio configuration
            var config = AudioSettings.GetConfiguration();
            if (config.sampleRate != 24000)
            {
                Debug.LogWarning($"[AudioQuickFix] ⚠️ Unity sample rate is {config.sampleRate}Hz, should be 24000Hz for optimal OpenAI compatibility");
            }
            
            if (config.dspBufferSize > 512)
            {
                Debug.LogWarning($"[AudioQuickFix] ⚠️ DSP buffer size is {config.dspBufferSize}, consider reducing for lower latency");
            }
            
            Debug.Log("[AudioQuickFix] Runtime optimization check completed");
        }
    }
}
