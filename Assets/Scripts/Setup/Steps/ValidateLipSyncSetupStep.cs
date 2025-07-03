using UnityEngine;
using System.Collections.Generic;

namespace Setup.Steps
{
    /// <summary>
    /// Step to validate the final LipSync setup.
    /// </summary>
    public class ValidateLipSyncSetupStep
    {
        private System.Action<string> log;

        public ValidateLipSyncSetupStep(System.Action<string> log)
        {
            this.log = log;
        }

        public void Execute(GameObject targetAvatar, GameObject npcSystem, LipSyncSystemInfo info)
        {
            log("🔍 Step 5.4: Final LipSync Validation");

            ValidateBlendShapes(targetAvatar);

            bool hasLipSyncComponent = false;
            if (info.HasULipSync)
            {
                hasLipSyncComponent = targetAvatar.GetComponent("uLipSync.uLipSyncBlendShape") != null;
                log($"   uLipSyncBlendShape: {(hasLipSyncComponent ? "✅" : "❌")}");
                var audioSource = LipSyncSetupHelpers.FindPlaybackAudioSource(npcSystem);
                bool hasAnalyzer = audioSource?.GetComponent("uLipSync.uLipSync") != null;
                log($"   uLipSync Analyzer: {(hasAnalyzer ? "✅" : "❌")}");
            }
            else
            {
                hasLipSyncComponent = targetAvatar.GetComponent("Animation.ReadyPlayerMeLipSync") != null;
                log($"   ReadyPlayerMeLipSync: {(hasLipSyncComponent ? "✅" : "❌")}");
            }

            var audioManagerType = System.Type.GetType("OpenAI.RealtimeAPI.RealtimeAudioManager, Assembly-CSharp");
            bool hasAudioManager = npcSystem.GetComponent(audioManagerType) != null;
            log($"   Audio Integration: {(hasAudioManager ? "✅" : "❌")}");

            bool setupComplete = hasLipSyncComponent && hasAudioManager;
            log($"🎭 LipSync Setup: {(setupComplete ? "✅ COMPLETE" : "⚠️ INCOMPLETE")}");
        }

        private void ValidateBlendShapes(GameObject targetAvatar)
        {
            if (targetAvatar == null) return;
            var renderer = LipSyncSetupHelpers.FindFacialRenderer(targetAvatar);
            if (renderer == null || renderer.sharedMesh == null)
            {
                log("   ⚠️ No renderer with blendshapes found.");
                return;
            }

            bool foundMouthOpen = false;
            bool foundMouthSmile = false;
            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                string name = renderer.sharedMesh.GetBlendShapeName(i);
                if (name == "mouthOpen" || name == "jawOpen") foundMouthOpen = true;
                if (name == "mouthSmile") foundMouthSmile = true;
            }

            if (foundMouthOpen && foundMouthSmile)
            {
                log("   ✅ Required BlendShapes (mouthOpen, mouthSmile) found.");
            }
            else
            {
                log("   ⚠️ Missing optimal BlendShapes (mouthOpen, mouthSmile).");
            }
        }
    }
}
