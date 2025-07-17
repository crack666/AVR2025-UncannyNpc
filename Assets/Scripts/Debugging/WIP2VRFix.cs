using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;

namespace Debugging
{
    /// <summary>
    /// One-click solution to fix WIP2 VR Canvas interaction issues.
    /// This script addresses the specific problems found in the WIP2 scene.
    /// </summary>
    public class WIP2VRFix : MonoBehaviour
    {
        [Header("Ray Interactor Settings")]
        [SerializeField] private float maxRaycastDistance = 15.0f; // Increased for better reach
        [SerializeField] private bool addVisualRays = true;
        [SerializeField] private Color rayColor = Color.red;
        [SerializeField] private float rayWidth = 0.005f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private void Start()
        {
            // Auto-fix on start
            FixWIP2Issues();
        }

        /// <summary>
        /// Fixes all VR interaction issues in WIP2 scene
        /// </summary>
        [ContextMenu("Fix WIP2 VR Issues")]
        public void FixWIP2Issues()
        {
            Debug.Log("🔧 Starting WIP2 VR Fix...");
            
            // Step 1: Verify Canvas is properly configured (already fixed in scene file)
            VerifyCanvasConfiguration();
            
            // Step 2: Find or create XR Origin
            EnsureXROrigin();
            
            // Step 3: Add Ray Interactors if missing
            AddRayInteractors();
            
            // Step 4: Configure Ray Interactor settings
            ConfigureRayInteractors();
            
            // Step 5: Ensure proper XR setup
            EnsureXRSetup();
            
            Debug.Log("✅ WIP2 VR Fix Complete! You should now be able to interact with the Canvas using VR controllers.");
        }

        private void VerifyCanvasConfiguration()
        {
            Debug.Log("📋 Verifying Canvas configuration...");
            
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var position = canvas.transform.position;
                var scale = canvas.transform.localScale;
                
                Debug.Log($"   • Canvas Position: {position}");
                Debug.Log($"   • Canvas Scale: {scale}");
                
                if (scale.x > 0.005f)
                {
                    Debug.LogWarning($"⚠️ Canvas scale ({scale.x:F4}) might still be too large. Consider 0.001 for VR.");
                }
                else
                {
                    Debug.Log("✅ Canvas scale looks good for VR interaction");
                }
            }
            else
            {
                Debug.LogError("❌ No Canvas found in scene!");
            }
        }

        private void EnsureXROrigin()
        {
            Debug.Log("🥽 Checking XR Origin...");
            
            var xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogWarning("⚠️ No XR Origin found. You need to add an XR Origin prefab to the scene manually.");
                Debug.Log("💡 Add: XR Origin (VR) prefab from XR Interaction Toolkit");
            }
            else
            {
                Debug.Log($"✅ XR Origin found: {xrOrigin.name}");
            }
        }

        private void AddRayInteractors()
        {
            Debug.Log("🎯 Checking Ray Interactors...");
            
            var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            if (rayInteractors.Length == 0)
            {
                Debug.LogWarning("⚠️ No XR Ray Interactors found!");
                
                // Try to find hand anchors and add ray interactors
                var leftHandAnchor = GameObject.Find("LeftHandAnchor");
                var rightHandAnchor = GameObject.Find("RightHandAnchor");
                
                if (leftHandAnchor != null)
                {
                    AddRayInteractorToHand(leftHandAnchor, "Left");
                }
                
                if (rightHandAnchor != null)
                {
                    AddRayInteractorToHand(rightHandAnchor, "Right");
                }
                
                if (leftHandAnchor == null && rightHandAnchor == null)
                {
                    Debug.LogError("❌ No hand anchors found! Please add an XR Origin prefab with hand tracking.");
                }
            }
            else
            {
                Debug.Log($"✅ Found {rayInteractors.Length} Ray Interactor(s)");
            }
        }

        private void AddRayInteractorToHand(GameObject handAnchor, string handName)
        {
            Debug.Log($"🔫 Adding Ray Interactor to {handName} Hand...");
            
            // Create Ray Interactor GameObject
            var rayInteractorGO = new GameObject($"{handName} Ray Interactor");
            rayInteractorGO.transform.SetParent(handAnchor.transform);
            rayInteractorGO.transform.localPosition = Vector3.zero;
            rayInteractorGO.transform.localRotation = Quaternion.identity;
            
            // Add XR Ray Interactor component
            var rayInteractor = rayInteractorGO.AddComponent<XRRayInteractor>();
            rayInteractor.maxRaycastDistance = maxRaycastDistance;
            rayInteractor.enableUIInteraction = true;
            
            // Add Line Renderer for visual feedback
            if (addVisualRays)
            {
                var lineRenderer = rayInteractorGO.AddComponent<LineRenderer>();
                lineRenderer.material = CreateRayMaterial();
                lineRenderer.startColor = rayColor;
                lineRenderer.endColor = rayColor;
                lineRenderer.widthMultiplier = rayWidth;
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = false;
                
                // Set line positions
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, Vector3.forward * maxRaycastDistance);
                
                Debug.Log($"✅ Added visual ray to {handName} Hand");
            }
            
            Debug.Log($"✅ Ray Interactor added to {handName} Hand with max distance: {maxRaycastDistance}");
        }

        private Material CreateRayMaterial()
        {
            // Create a simple unlit material for the ray
            var material = new Material(Shader.Find("Unlit/Color"));
            material.color = rayColor;
            return material;
        }

        private void ConfigureRayInteractors()
        {
            Debug.Log("⚙️ Configuring Ray Interactors...");
            
            var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            
            foreach (var rayInteractor in rayInteractors)
            {
                // Ensure UI interaction is enabled
                if (!rayInteractor.enableUIInteraction)
                {
                    rayInteractor.enableUIInteraction = true;
                    Debug.Log($"✅ Enabled UI interaction for {rayInteractor.name}");
                }
                
                // Set max raycast distance
                if (rayInteractor.maxRaycastDistance < maxRaycastDistance)
                {
                    rayInteractor.maxRaycastDistance = maxRaycastDistance;
                    Debug.Log($"✅ Set max raycast distance to {maxRaycastDistance} for {rayInteractor.name}");
                }
                
                // Ensure line renderer is configured
                var lineRenderer = rayInteractor.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(1, Vector3.forward * maxRaycastDistance);
                    Debug.Log($"✅ Updated line renderer length for {rayInteractor.name}");
                }
            }
        }

        private void EnsureXRSetup()
        {
            Debug.Log("🛠️ Ensuring XR setup...");
            
            // Check for XR Interaction Manager
            var interactionManager = FindFirstObjectByType<XRInteractionManager>();
            if (interactionManager == null)
            {
                var managerGO = new GameObject("XR Interaction Manager");
                interactionManager = managerGO.AddComponent<XRInteractionManager>();
                Debug.Log("✅ Created XR Interaction Manager");
            }
            
            // Check for EventSystem
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                Debug.Log("✅ Created EventSystem with XRUIInputModule");
            }
            else
            {
                // Ensure XR UI Input Module exists
                var xrInputModule = eventSystem.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                if (xrInputModule == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                    Debug.Log("✅ Added XRUIInputModule to existing EventSystem");
                }
            }
        }

        /// <summary>
        /// Quick diagnostic info
        /// </summary>
        [ContextMenu("Show VR Status")]
        public void ShowVRStatus()
        {
            Debug.Log("📊 WIP2 VR Status Report:");
            
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"   • Canvas Distance: {Vector3.Distance(Vector3.zero, canvas.transform.position):F2} units");
                Debug.Log($"   • Canvas Scale: {canvas.transform.localScale.x:F4}");
            }
            
            var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            Debug.Log($"   • Ray Interactors: {rayInteractors.Length}");
            
            foreach (var ray in rayInteractors)
            {
                Debug.Log($"     - {ray.name}: Max Distance {ray.maxRaycastDistance}, UI Enabled: {ray.enableUIInteraction}");
            }
            
            var xrOrigin = FindFirstObjectByType<XROrigin>();
            Debug.Log($"   • XR Origin: {(xrOrigin != null ? "Present" : "Missing")}");
            
            var interactionManager = FindFirstObjectByType<XRInteractionManager>();
            Debug.Log($"   • Interaction Manager: {(interactionManager != null ? "Present" : "Missing")}");
        }
    }
}
