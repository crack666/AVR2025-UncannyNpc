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

        // Default transform values for ReadyPlayerMe avatar
        private static readonly Vector3 DefaultAvatarPosition = new Vector3(0.894f, 0.076f, -7.871f);
        private static readonly Vector3 DefaultAvatarRotation = new Vector3(0f, 180f, 0f);
        private static readonly Vector3 DefaultAvatarScale = Vector3.one;

        public FindOrValidateAssetsStep(ScriptableObject openAISettings, GameObject targetAvatar, System.Action<string> log)
        {
            this.openAISettings = openAISettings;
            this.targetAvatar = targetAvatar;
            this.log = log;
        }

        public void SetDefaultAvatarTransform()
        {
            if (targetAvatar != null)
            {
                targetAvatar.transform.position = DefaultAvatarPosition;
                targetAvatar.transform.eulerAngles = DefaultAvatarRotation;
                targetAvatar.transform.localScale = DefaultAvatarScale;
                log($"ℹ️ Set default transform for avatar '{targetAvatar.name}' (pos: {DefaultAvatarPosition}, rot: {DefaultAvatarRotation}, scale: {DefaultAvatarScale})");
            }
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
                SetDefaultAvatarTransform();
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
