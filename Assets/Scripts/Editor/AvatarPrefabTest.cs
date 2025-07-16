using UnityEngine;
using UnityEditor;

namespace Setup.Tools
{
    /// <summary>
    /// Simple test to verify avatar prefab loading
    /// </summary>
    public class AvatarPrefabTest : EditorWindow
    {
        [MenuItem("OpenAI NPC/Avatar Tools/Test Avatar Prefab", false, 101)]
        public static void TestAvatarPrefab()
        {
            string avatarId = "682cd77aff222706b8291007";
            string prefabPath = $"Assets/Ready Player Me/Avatars/{avatarId}/2fac66e374c947c41bc74325c6e3d934/{avatarId}.prefab";
            
            Debug.Log($"[Avatar Test] Testing avatar prefab at: {prefabPath}");
            
            // Test if prefab file exists
            if (!System.IO.File.Exists(prefabPath))
            {
                Debug.LogError($"[Avatar Test] ❌ Prefab file not found at: {prefabPath}");
                return;
            }
            
            Debug.Log($"[Avatar Test] ✅ Prefab file exists");
            
            // Test loading the prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Avatar Test] ❌ Failed to load prefab from: {prefabPath}");
                return;
            }
            
            Debug.Log($"[Avatar Test] ✅ Prefab loaded successfully: {prefab.name}");
            
            // Test instantiation
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                Debug.LogError($"[Avatar Test] ❌ Failed to instantiate prefab: {prefab.name}");
                return;
            }
            
            Debug.Log($"[Avatar Test] ✅ Prefab instantiated successfully: {instance.name}");
            
            // Check for SkinnedMeshRenderer
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            Debug.Log($"[Avatar Test] Found {renderers.Length} SkinnedMeshRenderer components:");
            
            foreach (var renderer in renderers)
            {
                Debug.Log($"  - {renderer.name} (BlendShapes: {renderer.sharedMesh?.blendShapeCount ?? 0})");
            }
            
            // Check if it would be found by the avatar detection system
            bool wouldBeFound = false;
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Wolf3D") || 
                    renderer.name.ToLower().Contains("head") || 
                    (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 10))
                {
                    wouldBeFound = true;
                    Debug.Log($"[Avatar Test] ✅ Avatar would be detected via renderer: {renderer.name}");
                    break;
                }
            }
            
            if (!wouldBeFound)
            {
                Debug.LogWarning($"[Avatar Test] ⚠️ Avatar might not be detected by the setup system");
                Debug.Log($"[Avatar Test] Renderer names: {string.Join(", ", System.Array.ConvertAll(renderers, r => r.name))}");
            }
            
            // Position the avatar
            instance.transform.position = new Vector3(0.894f, 0.076f, -7.871f);
            instance.transform.eulerAngles = new Vector3(0f, 180f, 0f);
            instance.name = avatarId;
            
            // Register with undo
            Undo.RegisterCreatedObjectUndo(instance, "Test Avatar Prefab");
            
            // Select the avatar
            Selection.activeGameObject = instance;
            SceneView.FrameLastActiveSceneView();
            
            Debug.Log($"[Avatar Test] ✅ Test completed successfully! Avatar '{avatarId}' is now in the scene.");
        }
    }
}
