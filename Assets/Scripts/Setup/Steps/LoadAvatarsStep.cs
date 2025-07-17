using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Setup.Steps
{
    /// <summary>
    /// Dedicated step for loading all avatars before UI creation.
    /// Loads all three avatars but only shows the selected one.
    /// </summary>
    public class LoadAvatarsStep
    {
        private System.Action<string> log;
        private int selectedAvatarIndex;
        
        // Avatar configurations
        private static readonly AvatarConfig[] AVATAR_CONFIGS = {
            new AvatarConfig {
                name = "Robert",
                assetPath = "Assets/Same Gev Dudios/Sci-Fi Robots Bundle/Models/Robert",
                fileExtension = ".fbx",
                avatarType = AvatarType.SciFiRobot,
                index = 0
            },
            new AvatarConfig {
                name = "RPM_Male", 
                assetPath = "Assets/Ready Player Me/Avatars/6879563862addb2fa351a7e8/2fac66e374c947c41bc74325c6e3d934/6879563862addb2fa351a7e8",
                fileExtension = ".prefab",
                avatarType = AvatarType.ReadyPlayerMe,
                index = 1
            },
            new AvatarConfig {
                name = "RPM_Female",
                assetPath = "Assets/Ready Player Me/Avatars/682cd77aff222706b8291007/2fac66e374c947c41bc74325c6e3d934/682cd77aff222706b8291007",
                fileExtension = ".prefab",
                avatarType = AvatarType.ReadyPlayerMe,
                index = 2
            }
        };
        
        public LoadAvatarsStep(System.Action<string> log, int selectedAvatarIndex = 0)
        {
            this.log = log;
            this.selectedAvatarIndex = selectedAvatarIndex;
        }
        
        public void Execute()
        {
            log("🎭 Step 2.5: Avatar Loading");
            
            // 1. Load all avatars
            LoadAllAvatars();
            
            // 2. Set initial visibility
            SetInitialAvatarVisibility();
            
            // 3. Fix persistent calls for existing UI buttons (if they exist)
            FixExistingButtonPersistentCalls();
            
            // 4. Notify AvatarManager that all avatars are loaded
            AvatarManager.Instance.NotifyAvatarsLoaded();
            
            log("✅ All avatars loaded and registered with AvatarManager");
        }
        
        private void LoadAllAvatars()
        {
            log("🎭 Loading all avatars...");
            
            // Use AvatarManager's standard position for consistency
            Vector3 referencePosition = AvatarManager.GetStandardAvatarPosition();
            
            foreach (var config in AVATAR_CONFIGS)
            {
                LoadSingleAvatar(config, referencePosition);
            }
        }
        
        private void LoadSingleAvatar(AvatarConfig config, Vector3 position)
        {
            // Check if avatar already exists
            GameObject existingAvatar = GameObject.Find(config.name);
            if (existingAvatar != null)
            {
                log($"ℹ️ Avatar '{config.name}' already exists - registering with AvatarManager");
                RegisterLoadedAvatar(config.name, existingAvatar);
                return;
            }
            
            #if UNITY_EDITOR
            GameObject avatarPrefab = LoadAvatarAsset(config);
            
            if (avatarPrefab != null)
            {
                // Instantiate avatar
                GameObject avatarInstance = UnityEngine.Object.Instantiate(avatarPrefab);
                avatarInstance.name = config.name;
                
                // Add Animation Controller
                AddAnimationController(avatarInstance, config);
                
                // Initially deactivate (will be activated by visibility logic)
                avatarInstance.SetActive(false);
                
                // Register with AvatarManager (handles positioning)
                RegisterLoadedAvatar(config.name, avatarInstance);
                
                log($"✅ Loaded avatar: {config.name}");
            }
            else
            {
                log($"❌ Failed to load avatar: {config.name}");
                CreateFallbackAvatar(config.name, position);
            }
            #else
            log($"⚠️ Avatar loading only available in Editor mode");
            CreateFallbackAvatar(config.name, position);
            #endif
        }
        
        #if UNITY_EDITOR
        private GameObject LoadAvatarAsset(AvatarConfig config)
        {
            string[] possiblePaths = GeneratePossiblePaths(config);
            
            foreach (string path in possiblePaths)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null)
                {
                    log($"🎯 Found avatar asset: {path}");
                    return asset;
                }
            }
            
            log($"⚠️ No asset found for {config.name}");
            return null;
        }
        
        private string[] GeneratePossiblePaths(AvatarConfig config)
        {
            return new string[] {
                $"{config.assetPath}{config.fileExtension}",
                $"{config.assetPath}/{config.name}{config.fileExtension}",
                $"{config.assetPath}/{config.name.ToLower()}{config.fileExtension}",
                $"{config.assetPath}/avatar{config.fileExtension}",
                $"{config.assetPath}/Avatar{config.fileExtension}"
            };
        }
        
        private void AddAnimationController(GameObject avatar, AvatarConfig config)
        {
            // Check if avatar already has an Animator component
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null)
            {
                animator = avatar.AddComponent<Animator>();
                log($"✅ Added Animator component to {config.name}");
            }
            
            // Load appropriate animation controller based on avatar type
            RuntimeAnimatorController controller = LoadAnimationController(config.avatarType);
            
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                log($"✅ Added Animation Controller to {config.name}: {controller.name}");
            }
            else
            {
                log($"⚠️ No Animation Controller found for {config.name} (type: {config.avatarType})");
            }
        }
        
        private RuntimeAnimatorController LoadAnimationController(AvatarType avatarType)
        {
            string[] possiblePaths = GetAnimationControllerPaths(avatarType);
            
            foreach (string path in possiblePaths)
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                if (controller != null)
                {
                    log($"🎯 Found Animation Controller: {path}");
                    return controller;
                }
            }
            
            log($"⚠️ No Animation Controller found for type: {avatarType}");
            return null;
        }
        
        private string[] GetAnimationControllerPaths(AvatarType avatarType)
        {
            // Use RPM Animation Controller for all avatar types
            return new string[] {
                "Assets/Ready Player Me/Core/Samples/AvatarCreatorSamples/AvatarCreatorElements/Animation/AnimationController.controller",
                "Assets/Ready Player Me/Core/Samples/QuickStart/Animations/RpmPlayer.controller",
                "Assets/Plugins/ReadyPlayerMe/Resources/Animations/RpmPlayer.controller"
            };
        }
        #endif
        
        private void RegisterLoadedAvatar(string name, GameObject avatar)
        {
            AvatarManager.Instance.RegisterAvatar(name, avatar);
            log($"✅ Registered avatar: {name} at standard position");
        }
        
        private void SetInitialAvatarVisibility()
        {
            log($"🎯 Setting initial avatar visibility (selected: {selectedAvatarIndex})");
            
            for (int i = 0; i < AVATAR_CONFIGS.Length; i++)
            {
                string avatarName = AVATAR_CONFIGS[i].name;
                GameObject avatar = AvatarManager.Instance.GetAvatar(avatarName);
                
                if (avatar != null)
                {
                    bool shouldBeVisible = (i == selectedAvatarIndex);
                    avatar.SetActive(shouldBeVisible);
                    
                    string status = shouldBeVisible ? "VISIBLE" : "HIDDEN";
                    log($"🎭 Avatar '{avatarName}': {status}");
                }
                else
                {
                    log($"⚠️ Avatar '{avatarName}' not found for visibility setting");
                }
            }
        }
        
        private void CreateFallbackAvatar(string avatarName, Vector3 position)
        {
            log($"🔧 Creating fallback GameObject for '{avatarName}'");
            
            GameObject fallbackAvatar = new GameObject(avatarName);
            fallbackAvatar.SetActive(false);
            
            // Add a simple identifier component
            fallbackAvatar.AddComponent<FallbackAvatarMarker>();
            
            // Register with AvatarManager (handles positioning)
            RegisterLoadedAvatar(avatarName, fallbackAvatar);
            
            log($"✅ Created fallback avatar: {avatarName}");
        }
        
        /// <summary>
        /// Gets the selected avatar index for external use
        /// </summary>
        public int GetSelectedAvatarIndex()
        {
            return selectedAvatarIndex;
        }
        
        /// <summary>
        /// Sets a new selected avatar and updates visibility
        /// </summary>
        public void SetSelectedAvatar(int newIndex)
        {
            if (newIndex >= 0 && newIndex < AVATAR_CONFIGS.Length)
            {
                selectedAvatarIndex = newIndex;
                SetInitialAvatarVisibility();
                log($"🎯 Changed selected avatar to index: {newIndex}");
            }
            else
            {
                log($"❌ Invalid avatar index: {newIndex}");
            }
        }
        
        /// <summary>
        /// Fixes persistent calls for existing Select Avatar buttons after avatars are loaded.
        /// This ensures the persistent calls reference the correct GameObjects.
        /// </summary>
        private void FixExistingButtonPersistentCalls()
        {
            log("🔧 Fixing persistent calls for existing Select Avatar buttons...");
            
            #if UNITY_EDITOR
            // Find Select Avatar buttons
            string[] buttonNames = { "Robert Button", "RPM_Male Button", "RPM_Female Button" };
            string[] avatarGameObjectNames = { "Robert", "RPM_Male", "RPM_Female" };
            
            for (int i = 0; i < buttonNames.Length; i++)
            {
                GameObject buttonGO = GameObject.Find(buttonNames[i]);
                if (buttonGO != null)
                {
                    var button = buttonGO.GetComponent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        try
                        {
                            // Create persistent calls like in the original scene
                            var serializedObject = new UnityEditor.SerializedObject(button);
                            var onClickProperty = serializedObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                            
                            // Clear existing calls
                            onClickProperty.arraySize = 0;
                            
                            // Add calls for each avatar (activate one, deactivate others)
                            for (int j = 0; j < avatarGameObjectNames.Length; j++)
                            {
                                // Use AvatarManager to get avatar reference
                                GameObject targetObj = AvatarManager.Instance.GetAvatar(avatarGameObjectNames[j]);
                                if (targetObj != null)
                                {
                                    onClickProperty.arraySize++;
                                    var callProperty = onClickProperty.GetArrayElementAtIndex(j);
                                    
                                    callProperty.FindPropertyRelative("m_Target").objectReferenceValue = targetObj;
                                    callProperty.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "UnityEngine.GameObject, UnityEngine";
                                    callProperty.FindPropertyRelative("m_MethodName").stringValue = "SetActive";
                                    callProperty.FindPropertyRelative("m_Mode").intValue = 6; // Bool mode
                                    callProperty.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = (j == i);
                                    callProperty.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly
                                    
                                    log($"🔗 Added persistent call: {buttonNames[i]} -> {avatarGameObjectNames[j]}.SetActive({j == i})");
                                }
                                else
                                {
                                    log($"⚠️ Avatar GameObject not found: {avatarGameObjectNames[j]}");
                                }
                            }
                            
                            serializedObject.ApplyModifiedProperties();
                            log($"✅ Fixed persistent calls for button: {buttonNames[i]}");
                        }
                        catch (System.Exception ex)
                        {
                            log($"❌ Could not fix persistent calls for {buttonNames[i]}: {ex.Message}");
                        }
                    }
                }
            }
            #endif
        }
        
        /// <summary>
        /// Avatar configuration data structure
        /// </summary>
        [System.Serializable]
        public class AvatarConfig
        {
            public string name;
            public string assetPath;
            public string fileExtension;
            public AvatarType avatarType;
            public int index;
        }
        
        /// <summary>
        /// Avatar type enumeration
        /// </summary>
        public enum AvatarType
        {
            SciFiRobot,
            Mixamo,
            ReadyPlayerMe
        }
        
        /// <summary>
        /// Marker component for fallback avatars
        /// </summary>
        public class FallbackAvatarMarker : MonoBehaviour
        {
            // Empty marker component
        }
    }
}
