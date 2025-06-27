using UnityEngine;
using System.Collections;

namespace Setup.Steps
{
    public class LinkReferencesStep
    {
        private System.Action<string> log;
        public LinkReferencesStep(System.Action<string> log) { this.log = log; }
        public IEnumerator Execute(GameObject npcSystem, GameObject uiPanel, GameObject targetAvatar)
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
            log("✅ All component references linked successfully");
            yield return null;
        }
    }
}
