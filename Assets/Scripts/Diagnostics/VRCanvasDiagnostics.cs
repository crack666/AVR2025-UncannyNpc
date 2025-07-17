using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

namespace Diagnostics
{
    /// <summary>
    /// Diagnostic tool for VR Canvas interaction issues.
    /// Specifically designed to troubleshoot WIP2 scene problems.
    /// </summary>
    public class VRCanvasDiagnostics : MonoBehaviour
    {
        [Header("Diagnostic Settings")]
        [SerializeField] private bool runDiagnosticsOnStart = true;
        [SerializeField] private bool fixIssuesAutomatically = false;
        
        private void Start()
        {
            if (runDiagnosticsOnStart)
            {
                RunFullDiagnostics();
            }
        }

        /// <summary>
        /// Runs comprehensive diagnostics for VR Canvas interaction
        /// </summary>
        [ContextMenu("Run Full Diagnostics")]
        public void RunFullDiagnostics()
        {
            Debug.Log("🔍 Starting VR Canvas Diagnostics...");
            Debug.Log("==========================================");
            
            DiagnoseCanvasIssues();
            DiagnoseRayInteractorIssues();
            DiagnoseXRSetup();
            CheckCommonIssues();
            
            Debug.Log("==========================================");
            Debug.Log("✅ Diagnostics Complete!");
        }

        /// <summary>
        /// Diagnoses Canvas-related issues
        /// </summary>
        private void DiagnoseCanvasIssues()
        {
            Debug.Log("📋 Canvas Diagnostics:");
            
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length == 0)
            {
                Debug.LogError("❌ No Canvas found in scene!");
                return;
            }

            foreach (var canvas in canvases)
            {
                Debug.Log($"\n🖼️ Canvas: {canvas.name}");
                
                // Check render mode
                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    Debug.LogWarning($"⚠️ Canvas {canvas.name} is not in World Space mode (current: {canvas.renderMode})");
                    if (fixIssuesAutomatically)
                    {
                        canvas.renderMode = RenderMode.WorldSpace;
                        Debug.Log("✅ Fixed: Set to World Space mode");
                    }
                }
                else
                {
                    Debug.Log("✅ Render Mode: World Space");
                }
                
                // Check position and scale
                var position = canvas.transform.position;
                var scale = canvas.transform.localScale;
                var distance = Vector3.Distance(Vector3.zero, position);
                
                Debug.Log($"   • Position: {position}");
                Debug.Log($"   • Scale: {scale}");
                Debug.Log($"   • Distance from Origin: {distance:F2} units");
                
                // Detect problematic scale (WIP2 issue)
                if (scale.x < 0.1f || scale.y < 0.1f || scale.z < 0.1f)
                {
                    Debug.LogWarning($"⚠️ Canvas scale is very small ({scale})! This makes it hard to interact with.");
                    if (fixIssuesAutomatically)
                    {
                        canvas.transform.localScale = Vector3.one * 0.001f;
                        Debug.Log("✅ Fixed: Set appropriate VR scale (0.001)");
                    }
                }
                
                // Check if Canvas is too far away
                if (distance > 10f)
                {
                    Debug.LogWarning($"⚠️ Canvas is {distance:F2} units away - may be beyond Ray Interactor reach");
                    if (fixIssuesAutomatically)
                    {
                        canvas.transform.position = new Vector3(0, 1.5f, 2f);
                        Debug.Log("✅ Fixed: Moved Canvas to reasonable distance (2 units)");
                    }
                }
                
                // Check Canvas components
                var graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
                if (graphicRaycaster == null)
                {
                    Debug.LogWarning($"⚠️ Canvas {canvas.name} missing GraphicRaycaster component");
                    if (fixIssuesAutomatically)
                    {
                        canvas.gameObject.AddComponent<GraphicRaycaster>();
                        Debug.Log("✅ Fixed: Added GraphicRaycaster");
                    }
                }
                else
                {
                    Debug.Log("✅ GraphicRaycaster: Present");
                }
                
                // Check RectTransform
                var rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Debug.Log($"   • RectTransform Size: {rectTransform.sizeDelta}");
                    
                    // Calculate effective size in world units
                    var effectiveSize = rectTransform.sizeDelta * scale.x;
                    Debug.Log($"   • Effective World Size: {effectiveSize} units");
                    
                    if (effectiveSize.x < 0.1f || effectiveSize.y < 0.1f)
                    {
                        Debug.LogWarning("⚠️ Canvas effective size is very small - may be hard to hit with rays");
                    }
                }
            }
        }

        /// <summary>
        /// Diagnoses XR Ray Interactor issues
        /// </summary>
        private void DiagnoseRayInteractorIssues()
        {
            Debug.Log("\n🎯 Ray Interactor Diagnostics:");
            
            var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            if (rayInteractors.Length == 0)
            {
                Debug.LogWarning("⚠️ No XR Ray Interactors found in scene!");
                Debug.Log("💡 Add XR Ray Interactor components to hand/controller objects");
                return;
            }

            Debug.Log($"✅ Found {rayInteractors.Length} Ray Interactor(s)");
            
            foreach (var rayInteractor in rayInteractors)
            {
                Debug.Log($"\n🔫 Ray Interactor: {rayInteractor.name}");
                
                // Check UI interaction
                if (!rayInteractor.enableUIInteraction)
                {
                    Debug.LogWarning("⚠️ UI Interaction is disabled!");
                    if (fixIssuesAutomatically)
                    {
                        rayInteractor.enableUIInteraction = true;
                        Debug.Log("✅ Fixed: Enabled UI interaction");
                    }
                }
                else
                {
                    Debug.Log("✅ UI Interaction: Enabled");
                }
                
                // Check max raycast distance
                var maxDistance = rayInteractor.maxRaycastDistance;
                Debug.Log($"   • Max Raycast Distance: {maxDistance} units");
                
                if (maxDistance < 5f)
                {
                    Debug.LogWarning($"⚠️ Max raycast distance ({maxDistance}) might be too short for Canvas interaction");
                    if (fixIssuesAutomatically)
                    {
                        rayInteractor.maxRaycastDistance = 10f;
                        Debug.Log("✅ Fixed: Increased max raycast distance to 10 units");
                    }
                }
                
                // Check Line Renderer
                var lineRenderer = rayInteractor.GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    Debug.LogWarning("⚠️ No LineRenderer component - ray won't be visible");
                    if (fixIssuesAutomatically)
                    {
                        var lr = rayInteractor.gameObject.AddComponent<LineRenderer>();
                        lr.material = new Material(Shader.Find("Sprites/Default"));
                        lr.startColor = Color.red;
                        lr.endColor = Color.red;
                        lr.widthMultiplier = 0.01f;
                        lr.positionCount = 2;
                        Debug.Log("✅ Fixed: Added LineRenderer component");
                    }
                }
                else
                {
                    Debug.Log("✅ LineRenderer: Present");
                    Debug.Log($"   • Line Width: {lineRenderer.widthMultiplier}");
                    Debug.Log($"   • Position Count: {lineRenderer.positionCount}");
                }
                
                // Check interaction layers
                var interactionLayerMask = rayInteractor.interactionLayers;
                Debug.Log($"   • Interaction Layer Mask: {interactionLayerMask}");
            }
        }

        /// <summary>
        /// Diagnoses general XR setup
        /// </summary>
        private void DiagnoseXRSetup()
        {
            Debug.Log("\n🥽 XR Setup Diagnostics:");
            
            // Check for XR Origin
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogWarning("⚠️ No XR Origin found in scene!");
                Debug.Log("💡 Add an XR Origin prefab for VR functionality");
            }
            else
            {
                Debug.Log($"✅ XR Origin: {xrOrigin.name}");
            }
            
            // Check for XR Interaction Manager
            var interactionManager = FindFirstObjectByType<XRInteractionManager>();
            if (interactionManager == null)
            {
                Debug.LogWarning("⚠️ No XR Interaction Manager found!");
                if (fixIssuesAutomatically)
                {
                    var managerGO = new GameObject("XR Interaction Manager");
                    managerGO.AddComponent<XRInteractionManager>();
                    Debug.Log("✅ Fixed: Created XR Interaction Manager");
                }
            }
            else
            {
                Debug.Log($"✅ XR Interaction Manager: {interactionManager.name}");
            }
            
            // Check EventSystem
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("⚠️ No EventSystem found!");
                if (fixIssuesAutomatically)
                {
                    var eventSystemGO = new GameObject("EventSystem");
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                    Debug.Log("✅ Fixed: Created EventSystem with XRUIInputModule");
                }
            }
            else
            {
                Debug.Log($"✅ EventSystem: {eventSystem.name}");
                
                // Check for XR UI Input Module
                var xrInputModule = eventSystem.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                if (xrInputModule == null)
                {
                    Debug.LogWarning("⚠️ EventSystem missing XRUIInputModule");
                    if (fixIssuesAutomatically)
                    {
                        eventSystem.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                        Debug.Log("✅ Fixed: Added XRUIInputModule");
                    }
                }
                else
                {
                    Debug.Log("✅ XRUIInputModule: Present");
                }
            }
        }

        /// <summary>
        /// Checks for common VR Canvas interaction issues
        /// </summary>
        private void CheckCommonIssues()
        {
            Debug.Log("\n🔍 Common Issue Checks:");
            
            // Issue 1: Canvas too small (WIP2 specific)
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.transform.localScale.x <= 0.01f)
                {
                    Debug.LogWarning($"❌ FOUND WIP2 ISSUE: Canvas '{canvas.name}' has scale {canvas.transform.localScale.x:F4} - this is the exact problem!");
                    Debug.Log("💡 Solution: Increase Canvas scale to 0.001 or higher");
                }
            }
            
            // Issue 2: Canvas too far away
            foreach (var canvas in canvases)
            {
                var distance = Vector3.Distance(Vector3.zero, canvas.transform.position);
                if (distance > 10f)
                {
                    Debug.LogWarning($"❌ Canvas '{canvas.name}' is {distance:F2} units away - likely beyond ray reach");
                }
            }
            
            // Issue 3: No ray interactors
            var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            if (rayInteractors.Length == 0)
            {
                Debug.LogError("❌ CRITICAL: No Ray Interactors found - VR interaction impossible!");
            }
            
            // Issue 4: Ray interactors with UI interaction disabled
            foreach (var ray in rayInteractors)
            {
                if (!ray.enableUIInteraction)
                {
                    Debug.LogWarning($"❌ Ray Interactor '{ray.name}' has UI interaction disabled");
                }
            }
        }

        /// <summary>
        /// Quick fix for WIP2 scene specific issues
        /// </summary>
        [ContextMenu("Quick Fix WIP2 Issues")]
        public void QuickFixWIP2Issues()
        {
            Debug.Log("🔧 Applying WIP2 Quick Fixes...");
            
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                // Fix the tiny scale issue
                if (canvas.transform.localScale.x <= 0.01f)
                {
                    canvas.transform.localScale = Vector3.one * 0.001f;
                    Debug.Log($"✅ Fixed {canvas.name} scale: 0.01 → 0.001");
                }
                
                // Fix position if too far
                var distance = Vector3.Distance(Vector3.zero, canvas.transform.position);
                if (distance > 5f)
                {
                    canvas.transform.position = new Vector3(0, 1.5f, 2f);
                    Debug.Log($"✅ Fixed {canvas.name} position: moved closer to player");
                }
                
                // Ensure World Space
                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    Debug.Log($"✅ Fixed {canvas.name} render mode: World Space");
                }
            }
            
            Debug.Log("✅ WIP2 Quick Fix Complete!");
        }
    }
}
