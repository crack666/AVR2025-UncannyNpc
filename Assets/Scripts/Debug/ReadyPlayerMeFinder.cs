using UnityEngine;

namespace DebugTools
{
    /// <summary>
    /// Simple tool to find and connect ReadyPlayerMe components
    /// </summary>
    public class ReadyPlayerMeFinder : MonoBehaviour
    {
        [Header("Search Results")]
        [SerializeField] private SkinnedMeshRenderer[] foundRenderers;
        [SerializeField] private GameObject[] avatarRoots;
        
        [ContextMenu("Find ReadyPlayerMe Avatar")]
        public void FindReadyPlayerMeAvatar()
        {
            Debug.Log("=== ReadyPlayerMe Avatar Search ===");
            
            // Search for all SkinnedMeshRenderers
            var allRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            Debug.Log($"Found {allRenderers.Length} SkinnedMeshRenderers in scene");
            
            // Filter for ReadyPlayerMe-like renderers
            var validRenderers = new System.Collections.Generic.List<SkinnedMeshRenderer>();
            var rootObjects = new System.Collections.Generic.List<GameObject>();
            
            foreach (var renderer in allRenderers)
            {
                var mesh = renderer.sharedMesh;
                Debug.Log($"Checking: {renderer.name} (GameObject: {renderer.gameObject.name})");
                Debug.Log($"  Mesh: {(mesh != null ? mesh.name : "NULL")}");
                Debug.Log($"  BlendShapes: {(mesh?.blendShapeCount ?? 0)}");
                
                if (mesh != null && mesh.blendShapeCount > 0)
                {
                    validRenderers.Add(renderer);
                    
                    // Find root avatar object
                    Transform root = renderer.transform;
                    while (root.parent != null && !IsLikelyAvatarRoot(root.parent))
                    {
                        root = root.parent;
                    }
                    
                    if (!rootObjects.Contains(root.gameObject))
                    {
                        rootObjects.Add(root.gameObject);
                        Debug.Log($"  ✅ Found potential avatar root: {root.name}");
                    }
                }
            }
            
            foundRenderers = validRenderers.ToArray();
            avatarRoots = rootObjects.ToArray();
            
            Debug.Log($"=== Search Results ===");
            Debug.Log($"Valid Renderers: {foundRenderers.Length}");
            Debug.Log($"Avatar Roots: {avatarRoots.Length}");
            
            if (foundRenderers.Length > 0)
            {
                Debug.Log("✅ Found ReadyPlayerMe-compatible avatars!");
                Debug.Log("Use the arrays in this component to see the results.");
                Debug.Log("Add BlendShapeDebugger to one of the Avatar Root objects.");
            }
            else
            {
                Debug.LogWarning("❌ No avatars with BlendShapes found!");
                Debug.LogWarning("Make sure your ReadyPlayerMe avatar is in the scene.");
            }
        }
        
        private bool IsLikelyAvatarRoot(Transform transform)
        {
            string name = transform.name.ToLower();
            return name.Contains("avatar") || 
                   name.Contains("player") || 
                   name.Contains("character") ||
                   name.Contains("npc") ||
                   transform.parent == null; // Root of scene
        }
        
        [ContextMenu("Auto-Setup BlendShape Debugger")]
        public void AutoSetupBlendShapeDebugger()
        {
            if (avatarRoots == null || avatarRoots.Length == 0)
            {
                Debug.LogWarning("No avatar roots found. Run 'Find ReadyPlayerMe Avatar' first.");
                return;
            }
            
            foreach (var avatarRoot in avatarRoots)
            {
                if (avatarRoot == null) continue;
                
                var existingDebugger = avatarRoot.GetComponent<BlendShapeDebugger>();
                if (existingDebugger == null)
                {
                    var debugger = avatarRoot.AddComponent<BlendShapeDebugger>();
                    Debug.Log($"✅ Added BlendShapeDebugger to {avatarRoot.name}");
                    
                    // Find the best renderer for this avatar
                    var renderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                        {
                            // Use reflection to set the private field
                            var field = typeof(BlendShapeDebugger).GetField("targetRenderer", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            field?.SetValue(debugger, renderer);
                            
                            Debug.Log($"  Assigned renderer: {renderer.name} with {renderer.sharedMesh.blendShapeCount} BlendShapes");
                            break;
                        }
                    }
                }
                else
                {
                    Debug.Log($"BlendShapeDebugger already exists on {avatarRoot.name}");
                }
            }
        }
    }
}
