using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenAI.RealtimeAPI;
using NPC;

namespace Managers
{
    /// <summary>
    /// Debug display for OpenAI NPC system status
    /// Can be used with ReadyPlayerMe DebugPanel or standalone
    /// </summary>
    public class OpenAINPCDebug : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text debugText; // Legacy Text component
        [SerializeField] private TMP_Text debugTextTMP; // TextMeshPro component
        
        [Header("Debug Settings")]
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 0.5f;
        
        // Component references
        private RealtimeClient realtimeClient;
        private NPCController npcController;
        private RealtimeAudioManager audioManager;
        
        // Update timer
        private float lastUpdate;
          private void Awake()
        {
            // Find components
            realtimeClient = FindFirstObjectByType<RealtimeClient>();
            npcController = FindFirstObjectByType<NPCController>();
            audioManager = FindFirstObjectByType<RealtimeAudioManager>();
            
            // Auto-find text component if not assigned
            if (debugText == null && debugTextTMP == null)
            {
                debugText = GetComponentInChildren<Text>();
                debugTextTMP = GetComponentInChildren<TMP_Text>();
            }
        }
        
        private void Update()
        {
            if (autoUpdate && Time.time - lastUpdate >= updateInterval)
            {
                UpdateDebugDisplay();
                lastUpdate = Time.time;
            }
        }
        
        public void UpdateDebugDisplay()
        {
            string debugInfo = GenerateDebugInfo();
            SetDebugText(debugInfo);
        }
        
        private string GenerateDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            
            info.AppendLine("=== OpenAI NPC Debug ===");
            info.AppendLine($"Time: {System.DateTime.Now:HH:mm:ss}");
            info.AppendLine();
            
            // Realtime Client Status
            if (realtimeClient != null)
            {
                info.AppendLine("🔌 Realtime Client:");
                info.AppendLine($"  Connected: {realtimeClient.IsConnected}");
                info.AppendLine($"  Session: {realtimeClient.SessionId ?? "None"}");
            }
            else
            {
                info.AppendLine("❌ Realtime Client: Not Found");
            }
            
            info.AppendLine();
            
            // NPC Controller Status
            if (npcController != null)
            {
                info.AppendLine("🤖 NPC Controller:");
                info.AppendLine($"  State: {npcController.CurrentState}");
                info.AppendLine($"  Connected: {npcController.IsConnected}");
            }
            else
            {
                info.AppendLine("❌ NPC Controller: Not Found");
            }
            
            info.AppendLine();
              // Audio Manager Status
            if (audioManager != null)
            {
                info.AppendLine("🎤 Audio Manager:");
                info.AppendLine($"  Recording: {audioManager.IsRecording}");
                info.AppendLine($"  VAD Active: {audioManager.VoiceDetected}");
                info.AppendLine($"  Microphone: {audioManager.CurrentMicrophone ?? "None"}");
            }
            else
            {
                info.AppendLine("❌ Audio Manager: Not Found");
            }
            
            info.AppendLine();
            
            // System Info
            info.AppendLine("💻 System:");
            info.AppendLine($"  FPS: {(1f / Time.deltaTime):F1}");
            info.AppendLine($"  Memory: {System.GC.GetTotalMemory(false) / 1024 / 1024:F1} MB");
            
            return info.ToString();
        }
        
        private void SetDebugText(string text)
        {
            if (debugTextTMP != null)
            {
                debugTextTMP.text = text;
            }
            else if (debugText != null)
            {
                debugText.text = text;
            }
        }
        
        // Public methods for manual control
        [ContextMenu("Update Debug Info")]
        public void ManualUpdate()
        {
            UpdateDebugDisplay();
        }
        
        [ContextMenu("Clear Debug Text")]
        public void ClearDebugText()
        {
            SetDebugText("Debug cleared at " + System.DateTime.Now.ToString("HH:mm:ss"));
        }
    }
}
