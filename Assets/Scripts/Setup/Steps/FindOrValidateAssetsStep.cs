using UnityEngine;
using System.Collections;

namespace Setup.Steps
{
    /// <summary>
    /// Step 1: Asset Discovery and Validation
    /// </summary>
    public class FindOrValidateAssetsStep
    {
        private ScriptableObject openAISettings;
        private GameObject targetAvatar;
        private System.Action<string> log;
        public bool AvatarFound { get; private set; }
        public ScriptableObject OpenAISettings => openAISettings;
        public GameObject TargetAvatar => targetAvatar;

        public FindOrValidateAssetsStep(ScriptableObject openAISettings, GameObject targetAvatar, System.Action<string> log)
        {
            this.openAISettings = openAISettings;
            this.targetAvatar = targetAvatar;
            this.log = log;
        }

        public IEnumerator Execute()
        {
            log("📋 Step 1: Asset Discovery and Validation");
            if (openAISettings == null)
            {
                openAISettings = Resources.Load<ScriptableObject>("OpenAISettings");
                if (openAISettings == null)
                {
                    log("⚠️ OpenAISettings not found in Resources folder");
                    log("   → Create: Assets/Resources/OpenAISettings.asset");
                }
            }
            if (targetAvatar == null)
            {
                targetAvatar = FindReadyPlayerMeAvatar();
            }
            if (targetAvatar != null)
            {
                AvatarFound = true;
                log($"✅ Target Avatar: {targetAvatar.name}");
            }
            else
            {
                log("❌ No ReadyPlayerMe avatar found in scene");
                log("   → Import a ReadyPlayerMe avatar (.glb) first");
            }
            yield return null;
        }

        private GameObject FindReadyPlayerMeAvatar()
        {
            // Unity 2022+: FindObjectsOfType ist veraltet, nutze FindObjectsByType
            SkinnedMeshRenderer[] renderers = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Wolf3D") || renderer.name.ToLower().Contains("head") || (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 10))
                {
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
    }
}
