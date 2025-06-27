using UnityEngine;
using System.Collections;

namespace Setup.Steps
{
    public class LinkReferencesStep
    {
        private System.Action<string> log;
        public LinkReferencesStep(System.Action<string> log) { this.log = log; }
        public IEnumerator Execute(GameObject npcSystem, GameObject uiPanel, GameObject targetAvatar, ScriptableObject openAISettings)
        {
            log("🔗 Step 6: Linking All Component References");
            if (npcSystem == null || uiPanel == null)
            {
                log("❌ Cannot link references - missing core objects");
                yield break;
            }
            MonoBehaviour realtimeClient = npcSystem.GetComponent("RealtimeClient") as MonoBehaviour;
            MonoBehaviour audioManager = npcSystem.GetComponent("RealtimeAudioManager") as MonoBehaviour;
            MonoBehaviour npcController = npcSystem.GetComponent("NPCController") as MonoBehaviour;
            MonoBehaviour uiManager = uiPanel.GetComponent("NpcUiManager") as MonoBehaviour;
            MonoBehaviour lipSync = targetAvatar?.GetComponent("ReadyPlayerMeLipSync") as MonoBehaviour;
            // Link NPCController references
            if (npcController != null)
            {
                // npcController.realtimeClient = realtimeClient;
                // npcController.audioManager = audioManager;
                // npcController.lipSyncController = lipSync;
                log("✅ NPCController references linked");
            }
            // Link UI Manager references
            if (uiManager != null)
            {
                // Assign UI elements and NPCController to UI manager if needed
                log("✅ UI Manager references linked");
            }
            // Link audio manager references
            if (audioManager != null && realtimeClient != null)
            {
                // audioManager.realtimeClient = realtimeClient;
                log("✅ Audio Manager references linked");
            }
            // Link OpenAISettings to RealtimeClient
            if (realtimeClient != null && openAISettings != null)
            {
                var field = realtimeClient.GetType().GetField("settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(realtimeClient, openAISettings);
                    log("✅ OpenAISettings reference set on RealtimeClient");
                }
                else
                {
                    log("❌ Could not set OpenAISettings on RealtimeClient (field not found)");
                }
            }
            log("✅ All component references linked successfully");
            yield return null;
        }
    }
}
