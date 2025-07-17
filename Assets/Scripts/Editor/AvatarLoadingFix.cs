using UnityEngine;
using UnityEditor;

namespace Setup.Tools
{
    /// <summary>
    /// Quick fix for avatar loading issues
    /// </summary>
    public class AvatarLoadingFix
    {
        [MenuItem("OpenAI NPC/Avatar Tools/Fix Avatar Loading (682cd77aff222706b8291007)", false, 99)]
        public static void FixAvatarLoading()
        {
            string avatarId = "682cd77aff222706b8291007";
            
            Debug.Log($"[Avatar Fix] 🔧 Attempting to fix avatar loading for: {avatarId}");
            
            // First, check if avatar is already in the scene
            GameObject existingAvatar = FindAvatarInScene(avatarId);
            if (existingAvatar != null)
            {
                Debug.Log($"[Avatar Fix] ✅ Avatar already in scene: {existingAvatar.name}");
                Selection.activeGameObject = existingAvatar;
                SceneView.FrameLastActiveSceneView();
                return;
            }
            
            // Try to load the prefab
            string prefabPath = $"Assets/Ready Player Me/Avatars/{avatarId}/2fac66e374c947c41bc74325c6e3d934/{avatarId}.prefab";
            
            Debug.Log($"[Avatar Fix] 📂 Loading prefab from: {prefabPath}");
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Avatar Fix] ❌ Prefab not found at: {prefabPath}");
                Debug.LogError($"[Avatar Fix] Please ensure the avatar is properly imported into the project.");
                EditorUtility.DisplayDialog("Avatar Loading Error", 
                    $"Avatar prefab not found at:\n{prefabPath}\n\nPlease ensure the avatar is properly imported into the project.", 
                    "OK");
                return;
            }
            
            Debug.Log($"[Avatar Fix] ✅ Prefab loaded: {prefab.name}");
            
            // Instantiate the prefab
            GameObject avatarInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (avatarInstance == null)
            {
                Debug.LogError($"[Avatar Fix] ❌ Failed to instantiate prefab: {prefab.name}");
                EditorUtility.DisplayDialog("Avatar Loading Error", 
                    $"Failed to instantiate avatar prefab: {prefab.name}", 
                    "OK");
                return;
            }
            
            // Configure the avatar
            avatarInstance.transform.position = new Vector3(0.0f, 2.0f, -6.0f); // Avatar exakt vor dem Canvas (Canvas z = -7f, y = 2f)
            avatarInstance.transform.eulerAngles = new Vector3(0f, 180f, 0f);
            avatarInstance.transform.localScale = Vector3.one;
            avatarInstance.name = avatarId;
            
            // Register with undo system
            Undo.RegisterCreatedObjectUndo(avatarInstance, "Fix Avatar Loading");
            
            // Select the avatar
            Selection.activeGameObject = avatarInstance;
            SceneView.FrameLastActiveSceneView();
            
            Debug.Log($"[Avatar Fix] ✅ Avatar '{avatarId}' loaded successfully!");
            Debug.Log($"[Avatar Fix] Position: {avatarInstance.transform.position}");
            Debug.Log($"[Avatar Fix] Rotation: {avatarInstance.transform.eulerAngles}");
            
            // Verify the avatar has the expected components
            var renderers = avatarInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            Debug.Log($"[Avatar Fix] Found {renderers.Length} SkinnedMeshRenderer components");
            
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[Avatar Fix] ⚠️ No SkinnedMeshRenderer components found - this might cause issues");
            }
            
            // Check if it would be detected by the setup system
            bool wouldBeDetected = false;
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Wolf3D") || 
                    renderer.name.ToLower().Contains("head") || 
                    (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 10))
                {
                    wouldBeDetected = true;
                    break;
                }
            }
            
            if (!wouldBeDetected)
            {
                Debug.LogWarning($"[Avatar Fix] ⚠️ Avatar might not be detected by the setup system");
                Debug.LogWarning($"[Avatar Fix] This could cause the setup to not find the avatar");
            }
            
            // Show success dialog
            EditorUtility.DisplayDialog("Avatar Loading Fixed", 
                $"Avatar '{avatarId}' has been successfully loaded into the scene!\n\n" +
                $"Position: {avatarInstance.transform.position}\n" +
                $"Rotation: {avatarInstance.transform.eulerAngles}\n\n" +
                $"You can now run the OpenAI NPC setup.", 
                "OK");
        }
        
        private static GameObject FindAvatarInScene(string avatarId)
        {
            // Look for GameObject with specific name
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == avatarId)
                    return obj;
            }
            
            // Look for avatars by SkinnedMeshRenderer
            var renderers = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Wolf3D") || 
                    renderer.name.ToLower().Contains("head") || 
                    (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 10))
                {
                    Transform current = renderer.transform;
                    while (current.parent != null && 
                           !current.name.ToLower().Contains("avatar") && 
                           !current.name.ToLower().Contains("readyplayerme") &&
                           !current.name.Contains(avatarId))
                    {
                        current = current.parent;
                    }
                    if (current.name.Contains(avatarId))
                        return current.gameObject;
                }
            }
            
            return null;
        }
    }
}
