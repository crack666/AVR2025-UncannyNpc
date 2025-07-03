using UnityEngine;
using System.Collections;

namespace Setup.Steps
{
    /// <summary>
    /// Orchestrator for the LipSync setup process.
    /// Detects the available system and runs the appropriate setup steps.
    /// </summary>
    public class SetupLipSyncSystemStep
    {
        private System.Action<string> log;

        public SetupLipSyncSystemStep(System.Action<string> log)
        {
            this.log = log;
        }

        // Synchronous version for Editor/Setup use
        public void ExecuteSync(GameObject targetAvatar, GameObject npcSystem)
        {
            log("👄 Step 5: Advanced LipSync System Setup");

            if (targetAvatar == null)
            {
                log("❌ Cannot setup LipSync - no avatar found.");
                return;
            }

            // 1. Detect available system
            var detectionStep = new DetectLipSyncSystemStep(log);
            detectionStep.Execute();
            var systemInfo = detectionStep.SystemInfo;

            // 2. Run the appropriate setup
            if (systemInfo.HasULipSync)
            {
                log($"[DEBUG REMOVE LATER] Calling SetupULipSyncStep: targetAvatar={(targetAvatar != null ? targetAvatar.name : "null")}, npcSystem={(npcSystem != null ? npcSystem.name : "null")}");
                var uLipSyncSetup = new SetupULipSyncStep(log);
                uLipSyncSetup.ExecuteSync(targetAvatar, npcSystem);
            }
            else
            {
                var fallbackSetup = new SetupFallbackLipSyncStep(log);
                fallbackSetup.ExecuteSync(targetAvatar, npcSystem);

                if (systemInfo.CanInstallULipSync)
                {
                    log("💡 For professional-grade lip animation, install uLipSync from the Package Manager:");
                    log("   → git+https://github.com/hecomi/uLipSync.git#upm");
                }
            }

            // 3. Add NPCController (This could also be its own step)
            SetupNPCControllerSync(npcSystem);

            // 4. Validate the final setup
            var validationStep = new ValidateLipSyncSetupStep(log);
            validationStep.Execute(targetAvatar, npcSystem, systemInfo);
        }

        // [Optional] Keep for compatibility, but mark as obsolete
        [System.Obsolete("Use ExecuteSync instead. Coroutines are not supported in Editor setup.")]
        public System.Collections.IEnumerator Execute(GameObject targetAvatar, GameObject npcSystem)
        {
            ExecuteSync(targetAvatar, npcSystem);
            yield break;
        }

        private void SetupNPCControllerSync(GameObject npcSystem)
        {
            log("🤖 Setting up NPCController...");
            var npcControllerType = System.Type.GetType("NPC.NPCController, Assembly-CSharp");
            if (npcControllerType != null)
            {
                if (npcSystem.GetComponent(npcControllerType) == null)
                {
                    npcSystem.AddComponent(npcControllerType);
                    log("✅ Added NPCController component.");
                }
            }
            else
            {
                log("⚠️ NPCController type not found.");
            }
        }
    }
}
