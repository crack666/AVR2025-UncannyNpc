using UnityEngine;
using UnityEditor;
using System.IO;

namespace Setup.Tools
{
    /// <summary>
    /// Utility to load specific avatar prefabs into the scene
    /// </summary>
    public class AvatarPrefabLoader : EditorWindow
    {
        private string avatarId = "682cd77aff222706b8291007";
        private GameObject loadedAvatarPrefab;
        private bool showLoadResult = false;
        private string loadResultMessage = "";

        [MenuItem("OpenAI NPC/Avatar Tools/Load Avatar Prefab", false, 100)]
        public static void ShowWindow()
        {
            AvatarPrefabLoader window = GetWindow<AvatarPrefabLoader>(true, "Avatar Prefab Loader");
            window.position = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 200, 600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("🎭 Avatar Prefab Loader", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("This tool loads Ready Player Me avatar prefabs into the scene.\n" +
                                   "Enter the avatar ID to load the corresponding prefab.", MessageType.Info);

            GUILayout.Space(10);

            // Avatar ID Input
            EditorGUILayout.LabelField("Avatar ID:", EditorStyles.label);
            avatarId = EditorGUILayout.TextField(avatarId);

            GUILayout.Space(10);

            // Load Button
            if (GUILayout.Button("🔄 Load Avatar Prefab", GUILayout.Height(30)))
            {
                LoadAvatarPrefab();
            }

            GUILayout.Space(10);

            // Quick Load Button for specific avatar
            if (GUILayout.Button("⚡ Quick Load: 682cd77aff222706b8291007", GUILayout.Height(30)))
            {
                avatarId = "682cd77aff222706b8291007";
                LoadAvatarPrefab();
            }

            GUILayout.Space(10);

            // Show result
            if (showLoadResult)
            {
                var messageType = loadedAvatarPrefab != null ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(loadResultMessage, messageType);
            }

            GUILayout.Space(10);

            // Current scene avatars
            EditorGUILayout.LabelField("Current Scene Avatars:", EditorStyles.boldLabel);
            var avatarsInScene = FindAllAvatarsInScene();
            if (avatarsInScene.Length > 0)
            {
                foreach (var avatar in avatarsInScene)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {avatar.name}", GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = avatar;
                        SceneView.FrameLastActiveSceneView();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No avatars found in the current scene.", MessageType.Warning);
            }

            GUILayout.Space(10);

            // Instructions
            EditorGUILayout.HelpBox("After loading the avatar:\n" +
                                   "1. Check that the avatar appears in the scene\n" +
                                   "2. Use 'OpenAI NPC/Quick Setup' to configure the avatar\n" +
                                   "3. The setup will automatically find the loaded avatar", MessageType.Info);
        }

        private void LoadAvatarPrefab()
        {
            showLoadResult = false;
            loadedAvatarPrefab = null;

            if (string.IsNullOrEmpty(avatarId))
            {
                loadResultMessage = "❌ Please enter an avatar ID";
                showLoadResult = true;
                return;
            }

            // Try to find the avatar prefab
            string[] possiblePaths = {
                $"Assets/Ready Player Me/Avatars/{avatarId}",
                $"Assets/Ready Player Me/Avatars/{avatarId}/2fac66e374c947c41bc74325c6e3d934",
                $"Assets/Ready Player Me/Avatars/{avatarId}/*/",
            };

            string prefabPath = "";
            foreach (var basePath in possiblePaths)
            {
                // Search for .prefab files
                string[] prefabFiles = Directory.GetFiles(Application.dataPath.Replace("Assets", "") + basePath, "*.prefab", SearchOption.AllDirectories);
                foreach (var file in prefabFiles)
                {
                    var relativePath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                    if (Path.GetFileNameWithoutExtension(file) == avatarId)
                    {
                        prefabPath = relativePath;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(prefabPath)) break;
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                // Try direct path
                prefabPath = $"Assets/Ready Player Me/Avatars/{avatarId}/2fac66e374c947c41bc74325c6e3d934/{avatarId}.prefab";
            }

            Debug.Log($"[Avatar Loader] Attempting to load: {prefabPath}");

            // Load the prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                loadResultMessage = $"❌ Avatar prefab not found at: {prefabPath}\n" +
                                   "Make sure the avatar is properly imported into the project.";
                showLoadResult = true;
                return;
            }

            // Instantiate the prefab in the scene
            loadedAvatarPrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (loadedAvatarPrefab == null)
            {
                loadResultMessage = $"❌ Failed to instantiate avatar prefab: {prefab.name}";
                showLoadResult = true;
                return;
            }

            // Set position and name
            loadedAvatarPrefab.transform.position = new Vector3(0.894f, 0.076f, -7.871f);
            loadedAvatarPrefab.transform.eulerAngles = new Vector3(0f, 180f, 0f);
            loadedAvatarPrefab.name = avatarId;

            // Register with undo system
            Undo.RegisterCreatedObjectUndo(loadedAvatarPrefab, "Load Avatar Prefab");

            // Select the avatar
            Selection.activeGameObject = loadedAvatarPrefab;
            SceneView.FrameLastActiveSceneView();

            loadResultMessage = $"✅ Avatar '{avatarId}' loaded successfully!\n" +
                               $"Position: {loadedAvatarPrefab.transform.position}\n" +
                               $"You can now run the OpenAI NPC setup.";
            showLoadResult = true;

            Debug.Log($"[Avatar Loader] Avatar '{avatarId}' loaded successfully at position {loadedAvatarPrefab.transform.position}");
        }

        private GameObject[] FindAllAvatarsInScene()
        {
            var avatars = new System.Collections.Generic.List<GameObject>();
            
            // Find all SkinnedMeshRenderer components
            var renderers = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Wolf3D") || 
                    renderer.name.ToLower().Contains("head") || 
                    (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 10))
                {
                    // Find the root avatar GameObject
                    Transform current = renderer.transform;
                    while (current.parent != null && 
                           !current.name.ToLower().Contains("avatar") && 
                           !current.name.ToLower().Contains("readyplayerme") &&
                           !current.name.Contains("682cd77aff222706b8291007"))
                    {
                        current = current.parent;
                    }
                    
                    if (!avatars.Contains(current.gameObject))
                    {
                        avatars.Add(current.gameObject);
                    }
                }
            }
            
            return avatars.ToArray();
        }
    }
}
