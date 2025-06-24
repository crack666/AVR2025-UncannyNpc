using UnityEngine;
using Animation;
using OpenAI.RealtimeAPI;

namespace DebugTools
{
    /// <summary>
    /// Test tool for debugging LipSync system connectivity and functionality
    /// </summary>
    public class LipSyncTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ReadyPlayerMeLipSync lipSyncController;
        [SerializeField] private RealtimeAudioManager audioManager;
        
        [Header("Test Controls")]
        [SerializeField] private bool enableContinuousLogging = false;
        [SerializeField] private float testAmplitude = 0.5f;
        
        private void Start()
        {
            // Find components if not assigned
            if (lipSyncController == null)
                lipSyncController = FindFirstObjectByType<ReadyPlayerMeLipSync>();
            
            if (audioManager == null)
                audioManager = FindFirstObjectByType<RealtimeAudioManager>();
            
            LogSystemStatus();
        }
        
        private void Update()
        {
            if (enableContinuousLogging)
            {
                LogAmplitudeData();
            }
        }
        
        [ContextMenu("Test System Status")]
        public void LogSystemStatus()
        {
            Debug.Log("=== LipSync System Status ===");
            Debug.Log($"LipSync Controller: {(lipSyncController != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"RealtimeAudioManager: {(audioManager != null ? "Found" : "NOT FOUND")}");
            
            if (lipSyncController != null)
            {
                Debug.Log($"LipSync Active: {lipSyncController.IsLipSyncActive()}");
            }
            
            if (audioManager != null)
            {
                Debug.Log($"Audio Manager Initialized: {audioManager.IsInitialized}");
                Debug.Log($"Current Audio Amplitude: {audioManager.CurrentAudioAmplitude:F4}");
            }
            Debug.Log("============================");
        }
        
        [ContextMenu("Test Connectivity")]
        public void TestConnectivity()
        {
            if (lipSyncController != null && audioManager != null)
            {
                lipSyncController.SetRealtimeAudioManager(audioManager);
                Debug.Log("[LipSyncTester] Manually connected LipSync to AudioManager");
            }
            else
            {
                Debug.LogWarning("[LipSyncTester] Cannot test connectivity - missing components");
            }
        }
        
        [ContextMenu("Force Start Speaking")]
        public void ForceStartSpeaking()
        {
            if (lipSyncController != null)
            {
                lipSyncController.SetSpeaking(true);
                Debug.Log("[LipSyncTester] Forced speaking state ON");
            }
        }
        
        [ContextMenu("Force Stop Speaking")]
        public void ForceStopSpeaking()
        {
            if (lipSyncController != null)
            {
                lipSyncController.SetSpeaking(false);
                Debug.Log("[LipSyncTester] Forced speaking state OFF");
            }
        }
        
        private void LogAmplitudeData()
        {
            if (audioManager != null && Time.frameCount % 60 == 0) // Log every second
            {
                float amplitude = audioManager.CurrentAudioAmplitude;
                if (amplitude > 0.001f)
                {
                    Debug.Log($"[LipSyncTester] Audio amplitude: {amplitude:F4}");
                }
            }
        }
    }
}
