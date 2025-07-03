using UnityEngine;

namespace Setup
{
    /// <summary>
    /// Static helper methods used by various LipSync setup steps.
    /// </summary>
    public static class LipSyncSetupHelpers
    {
        public static AudioSource FindPlaybackAudioSource(GameObject npcSystem)
        {
            if (npcSystem == null) return null;
            var audioManagerType = System.Type.GetType("OpenAI.RealtimeAPI.RealtimeAudioManager, Assembly-CSharp");
            if (audioManagerType != null)
            {
                var audioManager = npcSystem.GetComponentInChildren(audioManagerType);
                if (audioManager != null)
                {
                    var field = audioManager.GetType().GetField("playbackAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) return field.GetValue(audioManager) as AudioSource;
                }
            }
            var sourceTransform = npcSystem.transform.Find("PlaybackAudioSource");
            return sourceTransform?.GetComponent<AudioSource>();
        }

        public static SkinnedMeshRenderer FindFacialRenderer(GameObject targetAvatar)
        {
            if (targetAvatar == null) return null;
            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers) if (renderer.name == "Renderer_Head") return renderer;
            foreach (var renderer in renderers) if (renderer.name.Contains("Head")) return renderer;
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh != null)
                {
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        if (renderer.sharedMesh.GetBlendShapeName(i).ToLower().Contains("mouth")) return renderer;
                    }
                }
            }
            return null;
        }
    }
}
