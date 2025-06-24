using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DebugTools
{
    /// <summary>
    /// Debug tool to inspect and test BlendShapes on SkinnedMeshRenderers
    /// Especially useful for ReadyPlayerMe avatars
    /// </summary>
    public class BlendShapeDebugger : MonoBehaviour
    {
        [Header("Target Mesh")]
        [SerializeField] private SkinnedMeshRenderer targetRenderer;
          [Header("BlendShape Testing")]
        [SerializeField] private string testBlendShapeName = "mouthOpen";
        [Range(0f, 1f)]
        [SerializeField] private float testBlendShapeValue = 0f;
        [SerializeField] private bool applyTestValue = false;
        
        [Header("Info Display")]
        [SerializeField] private bool showAllBlendShapes = true;
        
        // Runtime info
        [System.Serializable]
        public class BlendShapeInfo
        {
            public string name;
            public int index;
            public float currentValue;
        }
        
        [SerializeField] private List<BlendShapeInfo> blendShapeList = new List<BlendShapeInfo>();
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && applyTestValue)
            {
                ApplyTestBlendShape();
            }
            
            UpdateBlendShapeList();
        }
        #endif
        
        private void Awake()
        {
            if (targetRenderer == null)
            {
                Debug.Log("[BlendShapeDebugger] Auto-finding SkinnedMeshRenderer...");
                FindTargetRenderer();
            }
            
            UpdateBlendShapeList();
            LogBlendShapeInfo();
        }
        
        private void Update()
        {
            if (applyTestValue)
            {
                ApplyTestBlendShape();
            }
            
            // Update current values
            UpdateCurrentValues();
        }
          private void FindTargetRenderer()
        {
            Debug.Log("[BlendShapeDebugger] Starting comprehensive SkinnedMeshRenderer search...");
            
            // Look for SkinnedMeshRenderer in this GameObject first
            targetRenderer = GetComponent<SkinnedMeshRenderer>();
            
            if (targetRenderer == null)
            {
                // Look in children using GetComponentsInChildren which is safer
                var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true); // Include inactive
                Debug.Log($"[BlendShapeDebugger] Found {renderers.Length} SkinnedMeshRenderers in children");
                
                foreach (var renderer in renderers)
                {
                    try
                    {
                        var mesh = renderer.sharedMesh;
                        int blendShapeCount = mesh?.blendShapeCount ?? 0;
                        
                        Debug.Log($"[BlendShapeDebugger] Checking: {renderer.name} (GameObject: {renderer.gameObject.name}) - BlendShapes: {blendShapeCount}");
                        
                        // Prefer renderers with BlendShapes, especially head-related ones
                        if (mesh != null && blendShapeCount > 0)
                        {
                            string rendererName = renderer.name.ToLower();
                            
                            // Prioritize head meshes
                            if (rendererName.Contains("head") || rendererName.Contains("wolf3d_head"))
                            {
                                targetRenderer = renderer;
                                Debug.Log($"[BlendShapeDebugger] ✅ Selected HEAD renderer: {renderer.name} with {blendShapeCount} BlendShapes");
                                break;
                            }
                            
                            // If no head mesh found yet, take any mesh with BlendShapes
                            if (targetRenderer == null)
                            {
                                targetRenderer = renderer;
                                Debug.Log($"[BlendShapeDebugger] ⚠️ Selected renderer with BlendShapes: {renderer.name} with {blendShapeCount} BlendShapes");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[BlendShapeDebugger] Error checking renderer {renderer.name}: {ex.Message}");
                    }
                }
                
                if (targetRenderer == null && renderers.Length > 0)
                {
                    // Last resort: take the first renderer even without BlendShapes
                    foreach (var renderer in renderers)
                    {
                        try
                        {
                            var mesh = renderer.sharedMesh;
                            if (mesh != null)
                            {
                                targetRenderer = renderer;
                                Debug.Log($"[BlendShapeDebugger] ⚠️ Using first available renderer: {renderer.name} (no BlendShapes)");
                                break;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BlendShapeDebugger] Error accessing renderer {renderer.name}: {ex.Message}");
                        }
                    }
                }
            }
            
            if (targetRenderer != null)
            {
                Debug.Log($"[BlendShapeDebugger] ✅ Final selection: {targetRenderer.name} on {targetRenderer.gameObject.name}");
            }
            else
            {
                Debug.LogError("[BlendShapeDebugger] ❌ No usable SkinnedMeshRenderer found!");
            }
        }
        
        private void UpdateBlendShapeList()
        {
            blendShapeList.Clear();
            
            if (targetRenderer == null || targetRenderer.sharedMesh == null)
                return;
                
            var mesh = targetRenderer.sharedMesh;
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                blendShapeList.Add(new BlendShapeInfo
                {
                    name = mesh.GetBlendShapeName(i),
                    index = i,
                    currentValue = targetRenderer.GetBlendShapeWeight(i)
                });
            }
        }
        
        private void UpdateCurrentValues()
        {
            if (targetRenderer == null) return;
            
            foreach (var info in blendShapeList)
            {
                info.currentValue = targetRenderer.GetBlendShapeWeight(info.index);
            }
        }
        
        private void ApplyTestBlendShape()
        {
            if (targetRenderer == null || targetRenderer.sharedMesh == null)
                return;
                
            var mesh = targetRenderer.sharedMesh;
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string shapeName = mesh.GetBlendShapeName(i);
                if (shapeName.Equals(testBlendShapeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    targetRenderer.SetBlendShapeWeight(i, testBlendShapeValue);
                    Debug.Log($"[BlendShapeDebugger] Applied {testBlendShapeName} = {testBlendShapeValue}");
                    return;
                }
            }
            
            Debug.LogWarning($"[BlendShapeDebugger] BlendShape '{testBlendShapeName}' not found!");
        }
        
        private void LogBlendShapeInfo()
        {
            if (targetRenderer == null)
            {
                Debug.LogError("[BlendShapeDebugger] No SkinnedMeshRenderer found!");
                return;
            }
            
            if (targetRenderer.sharedMesh == null)
            {
                Debug.LogError("[BlendShapeDebugger] SkinnedMeshRenderer has no mesh!");
                return;
            }
            
            var mesh = targetRenderer.sharedMesh;
            Debug.Log($"[BlendShapeDebugger] Target: {targetRenderer.name} (GameObject: {targetRenderer.gameObject.name})");
            Debug.Log($"[BlendShapeDebugger] Mesh: {mesh.name} with {mesh.blendShapeCount} BlendShapes");
            
            if (mesh.blendShapeCount == 0)
            {
                Debug.LogWarning("[BlendShapeDebugger] No BlendShapes found in mesh!");
                return;
            }
            
            Debug.Log("[BlendShapeDebugger] Available BlendShapes:");
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string shapeName = mesh.GetBlendShapeName(i);
                float currentValue = targetRenderer.GetBlendShapeWeight(i);
                Debug.Log($"  [{i}] '{shapeName}' = {currentValue:F1}");
            }
        }
        
        #region Context Menu Debug Methods
        
        [ContextMenu("Log BlendShape Info")]
        public void DebugLogBlendShapeInfo()
        {
            LogBlendShapeInfo();
        }
        
        [ContextMenu("Test MouthOpen 50%")]
        public void TestMouthOpen50()
        {
            TestBlendShape("mouthOpen", 50f);
        }
        
        [ContextMenu("Test MouthSmile 30%")]
        public void TestMouthSmile30()
        {
            TestBlendShape("mouthSmile", 30f);
        }
        
        [ContextMenu("Reset All BlendShapes")]
        public void ResetAllBlendShapes()
        {
            if (targetRenderer == null || targetRenderer.sharedMesh == null)
                return;
                
            var mesh = targetRenderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                targetRenderer.SetBlendShapeWeight(i, 0f);
            }
            
            Debug.Log("[BlendShapeDebugger] Reset all BlendShapes to 0");
        }
        
        [ContextMenu("Find Mouth BlendShapes")]
        public void FindMouthBlendShapes()
        {
            if (targetRenderer == null || targetRenderer.sharedMesh == null)
                return;
                
            var mesh = targetRenderer.sharedMesh;
            Debug.Log("[BlendShapeDebugger] Searching for mouth-related BlendShapes:");
            
            string[] mouthKeywords = {"mouth", "lip", "jaw", "smile", "open", "close"};
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string shapeName = mesh.GetBlendShapeName(i).ToLower();
                
                foreach (string keyword in mouthKeywords)
                {
                    if (shapeName.Contains(keyword))
                    {
                        Debug.Log($"  🔍 Found mouth-related: [{i}] '{mesh.GetBlendShapeName(i)}'");
                        break;
                    }
                }
            }
        }
        
        [ContextMenu("Debug GameObject Hierarchy")]
        public void DebugGameObjectHierarchy()
        {
            Debug.Log("=== GameObject Hierarchy Analysis ===");
            Debug.Log($"Current GameObject: {gameObject.name}");
            Debug.Log($"Parent: {(transform.parent != null ? transform.parent.name : "None")}");
            
            // Check all components on this GameObject
            var components = GetComponents<Component>();
            Debug.Log($"Components on {gameObject.name}:");
            foreach (var comp in components)
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
              // Check all child objects
            Debug.Log($"Children of {gameObject.name}:");
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var childRenderer = child.GetComponent<SkinnedMeshRenderer>();
                SkinnedMeshRenderer actualRenderer = null;
                Mesh childMesh = null;
                
                // Safely get the renderer and mesh
                if (childRenderer != null)
                {
                    try
                    {
                        actualRenderer = childRenderer;
                        childMesh = childRenderer.sharedMesh;
                    }
                    catch (System.Exception)
                    {
                        // Ignore missing component errors
                    }
                }
                
                Debug.Log($"  [{i}] {child.name} - SkinnedMeshRenderer: {(actualRenderer != null ? "YES" : "NO")} - BlendShapes: {(childMesh?.blendShapeCount ?? 0)}");
                
                // Check grandchildren too
                for (int j = 0; j < child.childCount; j++)
                {
                    var grandchild = child.GetChild(j);
                    var grandchildRenderer = grandchild.GetComponent<SkinnedMeshRenderer>();
                    SkinnedMeshRenderer actualGrandchildRenderer = null;
                    Mesh grandchildMesh = null;
                    
                    // Safely get the renderer and mesh
                    if (grandchildRenderer != null)
                    {
                        try
                        {
                            actualGrandchildRenderer = grandchildRenderer;
                            grandchildMesh = grandchildRenderer.sharedMesh;
                        }
                        catch (System.Exception)
                        {
                            // Ignore missing component errors
                        }
                    }
                    
                    Debug.Log($"    [{j}] {grandchild.name} - SkinnedMeshRenderer: {(actualGrandchildRenderer != null ? "YES" : "NO")} - BlendShapes: {(grandchildMesh?.blendShapeCount ?? 0)}");
                }
            }
            
            // Search in parent hierarchy
            Debug.Log("=== Parent Hierarchy ===");
            Transform current = transform.parent;
            int level = 1;
            while (current != null && level <= 3)
            {
                var parentRenderer = current.GetComponent<SkinnedMeshRenderer>();
                var parentMesh = parentRenderer?.sharedMesh;
                Debug.Log($"Parent Level {level}: {current.name} - SkinnedMeshRenderer: {(parentRenderer != null ? "YES" : "NO")} - BlendShapes: {(parentMesh?.blendShapeCount ?? 0)}");
                
                current = current.parent;
                level++;
            }
            
            // Search all SkinnedMeshRenderers in scene
            Debug.Log("=== All SkinnedMeshRenderers in Scene ===");
            var allRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            Debug.Log($"Found {allRenderers.Length} SkinnedMeshRenderers in scene:");
            
            foreach (var renderer in allRenderers)
            {
                var mesh = renderer.sharedMesh;
                Debug.Log($"  - {renderer.name} (GameObject: {renderer.gameObject.name}) - Mesh: {(mesh != null ? mesh.name : "NULL")} - BlendShapes: {(mesh?.blendShapeCount ?? 0)}");
            }
        }
        
        private void TestBlendShape(string shapeName, float value)
        {
            if (targetRenderer == null || targetRenderer.sharedMesh == null)
            {
                Debug.LogError("[BlendShapeDebugger] Cannot test - no mesh renderer!");
                return;
            }
            
            var mesh = targetRenderer.sharedMesh;
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (mesh.GetBlendShapeName(i).Equals(shapeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    targetRenderer.SetBlendShapeWeight(i, value);
                    Debug.Log($"[BlendShapeDebugger] ✅ Set {shapeName} = {value}");
                    return;
                }
            }
            
            Debug.LogWarning($"[BlendShapeDebugger] ❌ BlendShape '{shapeName}' not found!");
        }
        
        #endregion
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DebugTools.BlendShapeDebugger))]
public class BlendShapeDebuggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        var debugger = (DebugTools.BlendShapeDebugger)target;
          EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("🔍 Debug GameObject Hierarchy", GUILayout.Height(25)))
        {
            debugger.DebugGameObjectHierarchy();
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Log BlendShapes"))
        {
            debugger.DebugLogBlendShapeInfo();
        }
        if (GUILayout.Button("Find Mouth Shapes"))
        {
            debugger.FindMouthBlendShapes();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Test MouthOpen"))
        {
            debugger.TestMouthOpen50();
        }
        if (GUILayout.Button("Test MouthSmile"))
        {
            debugger.TestMouthSmile30();
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Reset All BlendShapes"))
        {
            debugger.ResetAllBlendShapes();
        }
    }
}
#endif
