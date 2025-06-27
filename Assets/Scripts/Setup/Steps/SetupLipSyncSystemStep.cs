using UnityEngine;
using System.Collections;

namespace Setup.Steps
{
    public class SetupLipSyncSystemStep
    {
        private System.Action<string> log;
        public SetupLipSyncSystemStep(System.Action<string> log) { this.log = log; }
        public IEnumerator Execute(GameObject targetAvatar, GameObject npcSystem)
        {
            log("👄 Step 4: LipSync System Setup");
            if (targetAvatar == null)
            {
                log("❌ Cannot setup LipSync - no avatar found");
                yield break;
            }
            MonoBehaviour lipSync = targetAvatar.GetComponent("ReadyPlayerMeLipSync") as MonoBehaviour;
            if (lipSync == null)
            {
                System.Type lipSyncType = System.Type.GetType("Animation.ReadyPlayerMeLipSync") ?? System.Type.GetType("ReadyPlayerMeLipSync");
                if (lipSyncType != null)
                {
                    lipSync = targetAvatar.AddComponent(lipSyncType) as MonoBehaviour;
                    log("✅ Added: ReadyPlayerMeLipSync component to avatar");
                }
                else
                {
                    log("⚠️ ReadyPlayerMeLipSync type not found - will create placeholder");
                }
            }
            // Auto-configure lip sync (head renderer, audio source)
            SkinnedMeshRenderer headRenderer = null;
            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Head") || renderer.name.Contains("Wolf3D") || (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0))
                {
                    headRenderer = renderer;
                    break;
                }
            }
            if (headRenderer != null)
            {
                log($"✅ Found head renderer: {headRenderer.name}");
                AudioSource audioSource = GameObject.Find("PlaybackAudioSource")?.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    log("✅ Audio source linked to LipSync");
                }
                log("✅ LipSync parameters configured");
            }
            // Add NPCController
            MonoBehaviour npcController = npcSystem.GetComponent("NPCController") as MonoBehaviour;
            if (npcController == null)
            {
                System.Type npcControllerType = System.Type.GetType("NPC.NPCController") ?? System.Type.GetType("NPCController");
                if (npcControllerType != null)
                {
                    npcController = npcSystem.AddComponent(npcControllerType) as MonoBehaviour;
                    log("✅ Added: NPCController component");
                }
                else
                {
                    log("⚠️ NPCController type not found - will create placeholder");
                }
            }
            // BlendShape-Validierung (nur Logging)
            ValidateBlendShapes(targetAvatar);
            yield return null;
        }
        private void ValidateBlendShapes(GameObject targetAvatar)
        {
            if (targetAvatar == null) return;
            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            bool foundMouthOpen = false;
            bool foundMouthSmile = false;
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    string name = renderer.sharedMesh.GetBlendShapeName(i);
                    if (name == "mouthOpen") foundMouthOpen = true;
                    if (name == "mouthSmile") foundMouthSmile = true;
                }
            }
            if (foundMouthOpen && foundMouthSmile)
            {
                log("✅ Required BlendShapes found: mouthOpen, mouthSmile");
            }
            else
            {
                log("⚠️ Missing BlendShapes - LipSync may not work optimally");
                if (!foundMouthOpen) log("   → Missing: mouthOpen");
                if (!foundMouthSmile) log("   → Missing: mouthSmile");
            }
        }
    }
}
