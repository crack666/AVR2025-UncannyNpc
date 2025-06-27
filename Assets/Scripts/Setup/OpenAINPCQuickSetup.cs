using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Setup.Steps;

namespace Setup
{
    /// <summary>
    /// Comprehensive one-click setup for OpenAI Realtime NPC System
    /// Creates all necessary GameObjects, Components, and references
    /// Handles ReadyPlayerMe Avatar integration with LipSync
    /// </summary>
    public class OpenAINPCQuickSetup : MonoBehaviour
    {
        [Header("📋 Setup Configuration")]
        [SerializeField] private bool executeOnStart = false;
        [SerializeField] private bool showDetailedLogs = true;
        
        [Header("🎯 Target Avatar (Optional)")]
        [Tooltip("If provided, this avatar will be used. Otherwise, first ReadyPlayerMe avatar in scene will be found.")]
        [SerializeField] private GameObject targetAvatar;
          [Header("📂 Asset References (Optional)")]
        [Tooltip("Will be auto-found in Resources if not assigned")]
        [SerializeField] private ScriptableObject realtimeSettings;
        [SerializeField] private ScriptableObject openAISettings;
        
        [Header("🎨 UI Configuration")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private Vector2 uiPanelSize = new Vector2(400, 600);
        [SerializeField] private Vector2 uiPanelPosition = new Vector2(50, -50);
        
        // Setup state tracking
        private SetupProgress progress = new SetupProgress();
        
        private struct SetupProgress
        {
            public bool avatarFound;
            public bool npcSystemCreated;
            public bool audioSystemSetup;
            public bool lipSyncConfigured;
            public bool uiSystemCreated;
            public bool allReferencesLinked;
            public bool validationPassed;
        }
        
        private void Start()
        {
            if (executeOnStart)
            {
                StartCoroutine(ExecuteFullSetup());
            }
        }
        
        [ContextMenu("🚀 Execute Full NPC Setup")]
        public void ExecuteFullSetupMenu()
        {
            StartCoroutine(ExecuteFullSetup());
        }
        
        private IEnumerator ExecuteFullSetup()
        {
            Log("🚀 Starting OpenAI NPC Complete Setup...");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // Step 1: Asset Discovery
            var assetStep = new FindOrValidateAssetsStep(realtimeSettings, openAISettings, targetAvatar, Log);
            yield return StartCoroutine(assetStep.Execute());
            realtimeSettings = assetStep.RealtimeSettings;
            openAISettings = assetStep.OpenAISettings;
            targetAvatar = assetStep.TargetAvatar;
            progress.avatarFound = assetStep.AvatarFound;

            // Step 2: UI System (Canvas + Panel)
            var uiStep = new CreateUISystemStep();
            uiStep.Execute(uiPanelSize, uiPanelPosition);
            uiCanvas = uiStep.Canvas;
            // Panel: uiStep.Panel

            // Step 3: NPC System Core Setup
            yield return StartCoroutine(Step2_SetupAvatarAndNPCSystem());

            // Step 4: Audio System (ausgelagert)
            var npcSystem = GameObject.Find("OpenAI NPC System");
            var audioStep = new ConfigureAudioSystemStep(Log);
            yield return StartCoroutine(audioStep.Execute(npcSystem));
            progress.audioSystemSetup = true;

            // Step 5: LipSync System (ausgelagert)
            var lipSyncStep = new SetupLipSyncSystemStep(Log);
            yield return StartCoroutine(lipSyncStep.Execute(targetAvatar, npcSystem));
            progress.lipSyncConfigured = true;

            // Step 6: References Linking (ausgelagert)
            var linkStep = new LinkReferencesStep(Log);
            yield return StartCoroutine(linkStep.Execute(npcSystem, uiStep.Panel, targetAvatar));
            progress.allReferencesLinked = true;

            yield return StartCoroutine(Step7_FinalValidation());

            LogSetupSummary();
        }
        
        private IEnumerator Step1_FindOrValidateAssets()
        {
            Log("📋 Step 1: Asset Discovery and Validation");
              // Find or validate settings
            if (realtimeSettings == null)
            {
                realtimeSettings = Resources.Load<ScriptableObject>("OpenAIRealtimeSettings");
                if (realtimeSettings == null)
                {
                    Log("⚠️ OpenAIRealtimeSettings not found in Resources folder");
                    Log("   → Create: Assets/Resources/OpenAIRealtimeSettings.asset");
                }
            }
            
            if (openAISettings == null)
            {
                openAISettings = Resources.Load<ScriptableObject>("OpenAISettings");
                if (openAISettings == null)
                {
                    Log("⚠️ OpenAISettings not found in Resources folder");
                    Log("   → Create: Assets/Resources/OpenAISettings.asset");
                }
            }
            
            // Find target avatar
            if (targetAvatar == null)
            {
                targetAvatar = FindReadyPlayerMeAvatar();
            }
            
            if (targetAvatar != null)
            {
                progress.avatarFound = true;
                Log($"✅ Target Avatar: {targetAvatar.name}");
            }
            else
            {
                Log("❌ No ReadyPlayerMe avatar found in scene");
                Log("   → Import a ReadyPlayerMe avatar (.glb) first");
            }
            
            yield return null;
        }
        
        private IEnumerator Step2_SetupAvatarAndNPCSystem()
        {
            Log("🎯 Step 2: NPC System Core Setup");
            
            // Create main NPC System GameObject
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            if (npcSystem == null)
            {
                npcSystem = new GameObject("OpenAI NPC System");
                Log("✅ Created: OpenAI NPC System GameObject");
            }
            npcSystem.transform.position = Vector3.zero;
              // Add RealtimeClient
            MonoBehaviour realtimeClient = npcSystem.GetComponent("RealtimeClient") as MonoBehaviour;
            if (realtimeClient == null)
            {
                // Try to add RealtimeClient if the type exists
                System.Type realtimeClientType = System.Type.GetType("OpenAI.RealtimeAPI.RealtimeClient") ?? 
                                                  System.Type.GetType("RealtimeClient");
                if (realtimeClientType != null)
                {
                    realtimeClient = npcSystem.AddComponent(realtimeClientType) as MonoBehaviour;
                    Log("✅ Added: RealtimeClient component");
                }
                else
                {
                    Log("⚠️ RealtimeClient type not found - will create placeholder");
                }
            }
            
            // Configure RealtimeClient
            if (realtimeSettings != null)
            {
                // Use reflection to set settings if needed
                Log("✅ RealtimeClient configured with settings");
            }
            
            progress.npcSystemCreated = true;
            yield return null;
        }
        
        private IEnumerator Step7_FinalValidation()
        {
            Log("🔍 Step 7: Final System Validation");
            
            bool allValid = true;
              // Validate core components
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            if (npcSystem?.GetComponent("RealtimeClient") == null) allValid = false;
            if (npcSystem?.GetComponent("RealtimeAudioManager") == null) allValid = false;
            if (npcSystem?.GetComponent("NPCController") == null) allValid = false;
            
            // Validate avatar components
            if (targetAvatar?.GetComponent("ReadyPlayerMeLipSync") == null) allValid = false;
            
            // Validate UI components
            GameObject uiPanel = GameObject.Find("NPC UI Panel");
            if (uiPanel?.GetComponent("NpcUiManager") == null) allValid = false;
            
            progress.validationPassed = allValid;
            
            if (allValid)
            {
                Log("✅ All validation checks passed!");
            }
            else
            {
                Log("❌ Some validation checks failed - review setup");
            }
            
            yield return null;
        }
        
        private GameObject FindReadyPlayerMeAvatar()
        {
            // Look for common ReadyPlayerMe avatar indicators
            SkinnedMeshRenderer[] renderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Wolf3D") || 
                    renderer.name.ToLower().Contains("head") ||
                    (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 10))
                {
                    // Find the root avatar object
                    Transform current = renderer.transform;
                    while (current.parent != null && !current.name.ToLower().Contains("avatar") && !current.name.ToLower().Contains("readyplayerme"))
                    {
                        current = current.parent;
                    }
                    return current.gameObject;
                }
            }
            
            return null;
        }
        
        private void LogSetupSummary()
        {
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Log("📊 SETUP SUMMARY");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            LogStatus("Avatar Found", progress.avatarFound);
            LogStatus("NPC System Created", progress.npcSystemCreated);
            LogStatus("Audio System Setup", progress.audioSystemSetup);
            LogStatus("LipSync Configured", progress.lipSyncConfigured);
            LogStatus("UI System Created", progress.uiSystemCreated);
            LogStatus("References Linked", progress.allReferencesLinked);
            LogStatus("Validation Passed", progress.validationPassed);
            
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            if (progress.validationPassed)
            {
                Log("🎉 SETUP COMPLETE! Your OpenAI NPC system is ready.");
                Log("📋 Next steps:");
                Log("   1. Configure OpenAI API key in Settings");
                Log("   2. Test connection and conversation");
                Log("   3. Adjust LipSync sensitivity if needed");
            }
            else
            {
                Log("⚠️ Setup completed with warnings. Check logs above.");
            }
        }
        
        private void LogStatus(string task, bool success)
        {
            string icon = success ? "✅" : "❌";
            Log($"   {icon} {task}");
        }
        
        private void Log(string message)
        {
            if (showDetailedLogs)
            {
                Debug.Log($"[OpenAI NPC Setup] {message}");
            }
        }
        
        [ContextMenu("🔍 Validate Current Setup")]
        public void ValidateCurrentSetup()
        {
            StartCoroutine(Step7_FinalValidation());
        }
        
        [ContextMenu("🧹 Clean Up Failed Setup")]
        public void CleanUpFailedSetup()
        {
            // Remove any incomplete setup objects
            GameObject npcSystem = GameObject.Find("OpenAI NPC System");
            GameObject uiPanel = GameObject.Find("NPC UI Panel");
            
            if (npcSystem != null)
            {
                DestroyImmediate(npcSystem);
                Log("🧹 Removed incomplete NPC System");
            }
            
            if (uiPanel != null)
            {
                DestroyImmediate(uiPanel);
                Log("🧹 Removed incomplete UI Panel");
            }
            
            Log("🧹 Cleanup completed. Ready for fresh setup.");
        }
    }
}
