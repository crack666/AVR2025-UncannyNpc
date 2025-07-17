using UnityEngine;
using System.Collections;
using System.Linq;

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

            // 1. Detect available system
            var detectionStep = new DetectLipSyncSystemStep(log);
            detectionStep.Execute();
            var systemInfo = detectionStep.SystemInfo;

            // 2. Get all ReadyPlayerMe avatars for LipSync setup
            var rpmAvatars = AvatarManager.Instance.GetReadyPlayerMeAvatars();
            log($"🎭 Found {rpmAvatars.Count} ReadyPlayerMe avatars for LipSync setup");

            if (rpmAvatars.Count == 0)
            {
                log("⚠️ No ReadyPlayerMe avatars found. Using fallback for targetAvatar if available.");
                // Fallback to single avatar setup if no RPM avatars found
                if (targetAvatar != null)
                {
                    SetupLipSyncForSingleAvatar(targetAvatar, npcSystem, systemInfo);
                }
                else
                {
                    log("❌ No target avatar provided and no RPM avatars found.");
                }
            }
            else
            {
                // 3. Setup LipSync for all ReadyPlayerMe avatars
                foreach (var avatarKvp in rpmAvatars)
                {
                    string avatarName = avatarKvp.Key;
                    GameObject avatar = avatarKvp.Value;
                    
                    if (avatar != null)
                    {
                        log($"🎯 Setting up LipSync for RPM avatar: {avatarName}");
                        SetupLipSyncForSingleAvatar(avatar, npcSystem, systemInfo);
                    }
                    else
                    {
                        log($"⚠️ Avatar '{avatarName}' is null, skipping LipSync setup");
                    }
                }
            }

            // 4. Add NPCController (This could also be its own step)
            SetupNPCControllerSync(npcSystem);

            // 5. Validate the final setup using the primary target avatar or first RPM avatar
            GameObject validationAvatar = targetAvatar ?? rpmAvatars.Values.FirstOrDefault();
            if (validationAvatar != null)
            {
                var validationStep = new ValidateLipSyncSetupStep(log);
                validationStep.Execute(validationAvatar, npcSystem, systemInfo);
            }
        }
        
        /// <summary>
        /// Setup LipSync for a single avatar - reusable method to avoid code duplication
        /// </summary>
        private void SetupLipSyncForSingleAvatar(GameObject avatar, GameObject npcSystem, LipSyncSystemInfo systemInfo)
        {
            if (avatar == null)
            {
                log("❌ Cannot setup LipSync - avatar is null");
                return;
            }

            // Run the appropriate setup based on available system
            if (systemInfo.HasULipSync)
            {
                log($"[DEBUG REMOVE LATER] Calling SetupULipSyncStep for: {avatar.name}");
                var uLipSyncSetup = new SetupULipSyncStep(log);
                uLipSyncSetup.ExecuteSync(avatar, npcSystem);
            }
            else
            {
                var fallbackSetup = new SetupFallbackLipSyncStep(log);
                fallbackSetup.ExecuteSync(avatar, npcSystem);

                if (systemInfo.CanInstallULipSync)
                {
                    log("💡 For professional-grade lip animation, install uLipSync from the Package Manager:");
                    log("   → git+https://github.com/hecomi/uLipSync.git#upm");
                }
            }
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
