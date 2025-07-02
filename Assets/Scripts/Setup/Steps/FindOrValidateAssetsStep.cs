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

        public void ExecuteSync()
        {
            log("📋 Step 1: Asset Discovery and Validation");
            if (openAISettings == null)
            {
                openAISettings = Resources.Load<ScriptableObject>("OpenAISettings");
                if (openAISettings == null)
                {
                    log("⚠️ OpenAISettings not found in Resources folder");
                    log("   → Attempting to create OpenAISettings.asset automatically...");
                    
                    // Direkte Erstellung der OpenAISettings
                    openAISettings = CreateOpenAISettingsDirectly();
                    
                    if (openAISettings != null)
                    {
                        log("✅ OpenAISettings.asset created successfully!");
                        log("   → Location: Assets/Resources/OpenAISettings.asset");
                        log("   → Don't forget to set your API key in the Inspector");
                    }
                    else
                    {
                        log("❌ Failed to create OpenAISettings.asset automatically");
                        log("   → Manual creation required: Assets/Resources/OpenAISettings.asset");
                        log("   → Right-click in Resources folder → Create → OpenAI → Settings");
                    }
                }
                else
                {
                    log("✅ OpenAISettings found in Resources folder");
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
        }

        // [Optional] Keep for compatibility, but mark as obsolete
        [System.Obsolete("Use ExecuteSync instead. Coroutines are not supported in Editor setup.")]
        public System.Collections.IEnumerator Execute()
        {
            ExecuteSync();
            yield break;
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

        /// <summary>
        /// Alternative method to create OpenAISettings directly without external dependency
        /// </summary>
        private ScriptableObject CreateOpenAISettingsDirectly()
        {
#if UNITY_EDITOR
            // Prüfe ob Resources Ordner existiert
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                log("📁 Created Resources folder");
            }

            // Finde OpenAISettings Type
            var openAISettingsType = System.Type.GetType("OpenAISettings");
            if (openAISettingsType == null)
            {
                // Suche in allen Assemblies
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == "OpenAISettings" && type.IsSubclassOf(typeof(ScriptableObject)))
                        {
                            openAISettingsType = type;
                            break;
                        }
                    }
                    if (openAISettingsType != null) break;
                }
            }

            if (openAISettingsType == null)
            {
                log("❌ OpenAISettings class not found in project");
                return null;
            }

            // Erstelle Instanz
            var newSettings = ScriptableObject.CreateInstance(openAISettingsType);
            if (newSettings == null)
            {
                log("❌ Failed to create OpenAISettings instance");
                return null;
            }

            // Speichere Asset
            string assetPath = "Assets/Resources/OpenAISettings.asset";
            UnityEditor.AssetDatabase.CreateAsset(newSettings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            log($"✅ Created OpenAISettings at: {assetPath}");
            return newSettings;
#else
            return null;
#endif
        }
    }
}
