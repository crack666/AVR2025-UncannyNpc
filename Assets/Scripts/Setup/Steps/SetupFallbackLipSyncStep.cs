using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Setup.Steps
{
    /// <summary>
    /// Step to set up the fallback LipSync system (ReadyPlayerMeLipSync).
    /// </summary>
    public class SetupFallbackLipSyncStep
    {
        private System.Action<string> log;

        public SetupFallbackLipSyncStep(System.Action<string> log)
        {
            this.log = log;
        }

        public void ExecuteSync(GameObject targetAvatar, GameObject npcSystem)
        {
            log("🔄 Step 5.2: Setting up Fallback LipSync System");

            if (targetAvatar == null)
            {
                log("❌ Cannot setup Fallback LipSync - no avatar found");
                return;
            }

            // Add ReadyPlayerMeLipSync component
            var lipSyncType = System.Type.GetType("Animation.ReadyPlayerMeLipSync");
            if (lipSyncType == null)
            {
                log("❌ ReadyPlayerMeLipSync type not found in namespace 'Animation'. Make sure the script exists.");
                return;
            }

            var lipSyncComponent = targetAvatar.GetComponent(lipSyncType) as MonoBehaviour;
            if (lipSyncComponent == null)
            {
                lipSyncComponent = targetAvatar.AddComponent(lipSyncType) as MonoBehaviour;
                log("✅ Added ReadyPlayerMeLipSync component to avatar.");
            }

            // Configure the component
            ConfigureFallbackLipSync(targetAvatar, npcSystem);
        }

        // [Optional] Keep for compatibility, but mark as obsolete
        [System.Obsolete("Use ExecuteSync instead. Coroutines are not supported in Editor setup.")]
        public System.Collections.IEnumerator Execute(GameObject targetAvatar, GameObject npcSystem)
        {
            ExecuteSync(targetAvatar, npcSystem);
            yield break;
        }

        private void ConfigureFallbackLipSync(GameObject targetAvatar, GameObject npcSystem)
        {
            SkinnedMeshRenderer headRenderer = FindFacialRenderer(targetAvatar);
            if (headRenderer != null)
            {
                log($"✅ Found facial renderer: {headRenderer.name}");
                // The ReadyPlayerMeLipSync component will find its own references in its Start() method.
                // We just need to ensure it's added to the avatar.
                log("✅ Fallback LipSync configured (basic mouth movement).");
            }
            else
            {
                log("⚠️ No facial renderer found - LipSync may not work correctly.");
            }
        }

        private SkinnedMeshRenderer FindFacialRenderer(GameObject targetAvatar)
        {
            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.name == "Renderer_Head") return renderer;
            }
            
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Head") && !renderer.name.Contains("Eye")) return renderer;
            }

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                {
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        string shapeName = renderer.sharedMesh.GetBlendShapeName(i);
                        if (shapeName.ToLower().Contains("mouth")) return renderer;
                    }
                }
            }
            
            return null;
        }
    }
}
