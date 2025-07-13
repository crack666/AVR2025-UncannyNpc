using UnityEngine;
using Setup.Steps;

namespace Setup
{
    /// <summary>
    /// Static utility for modular, one-click OpenAI NPC setup from Editor menu.
    /// </summary>
    public static class OpenAINPCSetupUtility
    {
        public static void ExecuteFullSetup(
            ScriptableObject openAISettings,
            GameObject targetAvatar,
            Vector2 uiPanelSize,
            Vector2 uiPanelPosition,
            System.Action<string> logAction,
            System.Action<bool> onComplete = null)
        {
            logAction?.Invoke("🚀 Starting OpenAI NPC Complete Setup...");
            logAction?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // Step 1: Asset Discovery
            var assetStep = new FindOrValidateAssetsStep(openAISettings, targetAvatar, logAction);
            assetStep.ExecuteSync();
            openAISettings = assetStep.OpenAISettings;
            targetAvatar = assetStep.TargetAvatar;
            bool avatarFound = assetStep.AvatarFound;

            // Step 2: UI System (Canvas + Panel)
            var uiStep = new CreateUISystemStep(logAction);
            uiStep.ExecuteSync(uiPanelSize, uiPanelPosition, "ScreenSpaceOverlay", null, 0.01f);
            // Canvas und Panel werden jetzt per GameObject.Find gesucht
            var uiCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            var uiPanel = GameObject.Find("NPC UI Panel");

            // Step 3: NPC System Core Setup
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            if (npcSystem == null)
            {
                npcSystem = new GameObject("OpenAI NPC System");
                logAction?.Invoke("✅ Created: OpenAI NPC System GameObject");
            }
            npcSystem.transform.position = Vector3.zero;
            // Add RealtimeClient
            MonoBehaviour realtimeClient = npcSystem.GetComponent("RealtimeClient") as MonoBehaviour;
            if (realtimeClient == null)
            {
                System.Type realtimeClientType = System.Type.GetType("OpenAI.RealtimeAPI.RealtimeClient") ?? 
                                                  System.Type.GetType("RealtimeClient");
                if (realtimeClientType != null)
                {
                    realtimeClient = npcSystem.AddComponent(realtimeClientType) as MonoBehaviour;
                    logAction?.Invoke("✅ Added: RealtimeClient component");
                }
                else
                {
                    logAction?.Invoke("⚠️ RealtimeClient type not found - will create placeholder");
                }
            }

            // Step 4: Audio System
            var audioStep = new ConfigureAudioSystemStep(logAction);
            audioStep.ExecuteSync(npcSystem);

            // Step 5: LipSync System
            var lipSyncStep = new SetupLipSyncSystemStep(logAction);
            lipSyncStep.ExecuteSync(targetAvatar, npcSystem);

            // Step 6: References Linking
            var linkStep = new LinkReferencesStep(logAction);
            linkStep.ExecuteSync(npcSystem, uiPanel, targetAvatar, openAISettings);

            // Step 7: Final Validation
            bool allValid = true;
            if (npcSystem?.GetComponent("RealtimeClient") == null) allValid = false;
            if (npcSystem?.GetComponent("RealtimeAudioManager") == null) allValid = false;
            if (npcSystem?.GetComponent("NPCController") == null) allValid = false;
            // Check for uLipSync first, then fallback
            bool hasLipSync = targetAvatar?.GetComponent("uLipSync.uLipSyncBlendShape") != null ||
                             targetAvatar?.GetComponent("ReadyPlayerMeLipSync") != null;
            if (!hasLipSync) allValid = false;
            if (uiPanel?.GetComponent("NpcUiManager") == null) allValid = false;

            logAction?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            logAction?.Invoke("📊 SETUP SUMMARY");
            logAction?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            logAction?.Invoke($"   {(avatarFound ? "✅" : "❌")} Avatar Found");
            logAction?.Invoke($"   {(npcSystem != null ? "✅" : "❌")} NPC System Created");
            logAction?.Invoke($"   {(npcSystem?.GetComponent("RealtimeAudioManager") != null ? "✅" : "❌")} Audio System Setup");
            logAction?.Invoke($"   {(hasLipSync ? "✅" : "❌")} LipSync Configured");
            logAction?.Invoke($"   {(uiPanel?.GetComponent("NpcUiManager") != null ? "✅" : "❌")} UI System Created");
            logAction?.Invoke($"   {(allValid ? "✅" : "❌")} Validation Passed");
            logAction?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            if (allValid)
            {
                logAction?.Invoke("🎉 SETUP COMPLETE! Your OpenAI NPC system is ready.");
                logAction?.Invoke("📋 Next steps:");
                logAction?.Invoke("   1. Configure OpenAI API key in Settings");
                logAction?.Invoke("   2. Test connection and conversation");
                logAction?.Invoke("   3. Adjust LipSync sensitivity if needed");
            }
            else
            {
                logAction?.Invoke("⚠️ Setup completed with warnings. Check logs above.");
            }
            onComplete?.Invoke(allValid);
        }

        // Überladene Variante für erweiterte Optionen (Canvas-Modus etc.)
        public static void ExecuteFullSetup(
            ScriptableObject openAISettings,
            GameObject targetAvatar,
            Vector2 uiPanelSize,
            Vector2 uiPanelPosition,
            System.Action<string> logAction,
            System.Action<bool> onComplete,
            object options)
        {
            string canvasMode = "WorldSpace"; // Default to WorldSpace for XR
            Camera camera = null;
            float worldCanvasScale = 0.01f;
            if (options != null)
            {
                var t = options.GetType();
                var cmProp = t.GetProperty("canvasMode");
                // Note: canvasMode from options is ignored in XR mode, always use WorldSpace
                var camProp = t.GetProperty("camera");
                if (camProp != null) camera = camProp.GetValue(options) as Camera;
                var scaleProp = t.GetProperty("worldCanvasScale");
                if (scaleProp != null) worldCanvasScale = (float)scaleProp.GetValue(options);
            }

            logAction?.Invoke($"[XR Setup] Forced WorldSpace mode for VR, Camera: {(camera ? camera.name : "null")}, WorldScale: {worldCanvasScale}");

            // Step 1: Asset Discovery
            var assetStep = new FindOrValidateAssetsStep(openAISettings, targetAvatar, logAction);
            assetStep.ExecuteSync();
            openAISettings = assetStep.OpenAISettings;
            targetAvatar = assetStep.TargetAvatar;
            bool avatarFound = assetStep.AvatarFound;

            // Step 2: XR UI System (Canvas + Panel)
            var uiStep = new CreateUISystemStep(logAction);
            uiStep.ExecuteSync(uiPanelSize, uiPanelPosition, canvasMode, camera, worldCanvasScale);
            var uiCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            var uiPanel = GameObject.Find("NPC UI Panel");

            // Step 3: NPC System Core Setup
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            if (npcSystem == null)
            {
                npcSystem = new GameObject("OpenAI NPC System");
                logAction?.Invoke("✅ Created: OpenAI NPC System GameObject");
            }
            npcSystem.transform.position = Vector3.zero;
            // Add RealtimeClient
            MonoBehaviour realtimeClient = npcSystem.GetComponent("RealtimeClient") as MonoBehaviour;
            if (realtimeClient == null)
            {
                System.Type realtimeClientType = System.Type.GetType("OpenAI.RealtimeAPI.RealtimeClient") ?? 
                                                  System.Type.GetType("RealtimeClient");
                if (realtimeClientType != null)
                {
                    realtimeClient = npcSystem.AddComponent(realtimeClientType) as MonoBehaviour;
                    logAction?.Invoke("✅ Added: RealtimeClient component");
                }
                else
                {
                    logAction?.Invoke("⚠️ RealtimeClient type not found - will create placeholder");
                }
            }

            // Step 4: Audio System
            var audioStep = new ConfigureAudioSystemStep(logAction);
            audioStep.ExecuteSync(npcSystem);

            // Step 5: LipSync System
            var lipSyncStep = new SetupLipSyncSystemStep(logAction);
            lipSyncStep.ExecuteSync(targetAvatar, npcSystem);

            // Step 6: References Linking
            var linkStep = new LinkReferencesStep(logAction);
            linkStep.ExecuteSync(npcSystem, uiPanel, targetAvatar, openAISettings);

            // Step 7: Final Validation
            bool allValid = true;
            if (npcSystem?.GetComponent("RealtimeClient") == null) allValid = false;
            if (npcSystem?.GetComponent("RealtimeAudioManager") == null) allValid = false;
            if (npcSystem?.GetComponent("NPCController") == null) allValid = false;
            // Check for uLipSync first, then fallback
            bool hasLipSync = targetAvatar?.GetComponent("uLipSync.uLipSyncBlendShape") != null ||
                             targetAvatar?.GetComponent("ReadyPlayerMeLipSync") != null;
            if (!hasLipSync) allValid = false;
            if (uiPanel?.GetComponent("NpcUiManager") == null) allValid = false;

            logAction?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            logAction?.Invoke("📊 SETUP SUMMARY");
            logAction?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            logAction?.Invoke($"   {(avatarFound ? "✅" : "❌")} Avatar Found");
            logAction?.Invoke($"   {(npcSystem != null ? "✅" : "❌")} NPC System Created");
            logAction?.Invoke($"   {(npcSystem?.GetComponent("RealtimeAudioManager") != null ? "✅" : "❌")} Audio System Setup");
            logAction?.Invoke($"   {(hasLipSync ? "✅" : "❌")} LipSync Configured");
            logAction?.Invoke($"   {(uiPanel?.GetComponent("NpcUiManager") != null ? "✅" : "❌")} UI System Created");
            logAction?.Invoke($"   {(allValid ? "✅" : "❌")} Validation Passed");
            logAction?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            if (allValid)
            {
                logAction?.Invoke("🎉 SETUP COMPLETE! Your OpenAI NPC system is ready.");
                logAction?.Invoke("📋 Next steps:");
                logAction?.Invoke("   1. Configure OpenAI API key in Settings");
                logAction?.Invoke("   2. Test connection and conversation");
                logAction?.Invoke("   3. Adjust LipSync sensitivity if needed");
            }
            else
            {
                logAction?.Invoke("⚠️ Setup completed with warnings. Check logs above.");
            }
            onComplete?.Invoke(allValid);
        }
    }
}
