using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Diagnostics
{
    /// <summary>
    /// Script to fix Canvas positioning and scaling for VR interaction.
    /// Addresses common issues where VR controller rays can't reach the UI.
    /// </summary>
    public class FixCanvasForVR : MonoBehaviour
    {
        [Header("Canvas Configuration")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private float canvasDistance = 2.0f; // Distance from player
        [SerializeField] private float canvasScale = 0.001f; // Appropriate scale for VR
        [SerializeField] private Vector2 canvasSize = new Vector2(1920, 1080);
        
        [Header("Ray Interactor Configuration")]
        [SerializeField] private float maxRaycastDistance = 10.0f;
        [SerializeField] private bool autoFixRayInteractors = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private void Start()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }
            
            if (targetCanvas != null)
            {
                FixCanvasConfiguration();
            }
            
            if (autoFixRayInteractors)
            {
                FixRayInteractors();
            }
        }

        /// <summary>
        /// Fixes Canvas positioning and scaling for optimal VR interaction
        /// </summary>
        [ContextMenu("Fix Canvas for VR")]
        public void FixCanvasConfiguration()
        {
            if (targetCanvas == null)
            {
                Debug.LogError("No Canvas assigned!");
                return;
            }

            Debug.Log("🔧 Fixing Canvas for VR...");

            // Ensure World Space rendering
            targetCanvas.renderMode = RenderMode.WorldSpace;
            
            // Set optimal position (in front of player)
            targetCanvas.transform.position = new Vector3(0, 1.5f, canvasDistance);
            targetCanvas.transform.rotation = Quaternion.identity;
            
            // Set appropriate scale for VR
            targetCanvas.transform.localScale = Vector3.one * canvasScale;
            
            // Configure RectTransform
            var rectTransform = targetCanvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = canvasSize;
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            Debug.Log($"✅ Canvas fixed - Position: {targetCanvas.transform.position}, Scale: {targetCanvas.transform.localScale}");
            
            if (showDebugInfo)
            {
                LogCanvasInfo();
            }
        }

        /// <summary>
        /// Fixes XR Ray Interactor settings for proper UI interaction
        /// </summary>
        [ContextMenu("Fix Ray Interactors")]
        public void FixRayInteractors()
        {
            var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            
            if (rayInteractors.Length == 0)
            {
                Debug.LogWarning("⚠️ No XR Ray Interactors found in scene!");
                return;
            }

            Debug.Log($"🔧 Fixing {rayInteractors.Length} Ray Interactor(s)...");

            foreach (var rayInteractor in rayInteractors)
            {
                // Enable UI interaction
                rayInteractor.enableUIInteraction = true;
                
                // Set maximum raycast distance
                rayInteractor.maxRaycastDistance = maxRaycastDistance;
                
                // Ensure line renderer is configured
                var lineRenderer = rayInteractor.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    // Set line renderer end point based on max distance
                    lineRenderer.useWorldSpace = false;
                    var positions = new Vector3[2];
                    positions[0] = Vector3.zero;
                    positions[1] = Vector3.forward * maxRaycastDistance;
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPositions(positions);
                    
                    Debug.Log($"✅ Fixed Ray Interactor: {rayInteractor.name} (Max Distance: {maxRaycastDistance})");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Ray Interactor {rayInteractor.name} has no LineRenderer component!");
                }
            }
        }

        /// <summary>
        /// Logs detailed Canvas configuration info
        /// </summary>
        private void LogCanvasInfo()
        {
            if (targetCanvas == null) return;

            Debug.Log("📊 Canvas Configuration:");
            Debug.Log($"   • Position: {targetCanvas.transform.position}");
            Debug.Log($"   • Rotation: {targetCanvas.transform.rotation.eulerAngles}");
            Debug.Log($"   • Scale: {targetCanvas.transform.localScale}");
            Debug.Log($"   • Render Mode: {targetCanvas.renderMode}");
            
            var rectTransform = targetCanvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log($"   • Size Delta: {rectTransform.sizeDelta}");
                Debug.Log($"   • Anchored Position: {rectTransform.anchoredPosition}");
            }

            // Calculate effective canvas size in world units
            var effectiveSize = canvasSize * canvasScale;
            Debug.Log($"   • Effective World Size: {effectiveSize} units");
            Debug.Log($"   • Distance from Origin: {Vector3.Distance(Vector3.zero, targetCanvas.transform.position):F2} units");
        }

        /// <summary>
        /// Shows debugging gizmos in Scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            if (targetCanvas == null || !showDebugInfo) return;

            // Draw canvas bounds
            Gizmos.color = Color.green;
            var effectiveSize = canvasSize * canvasScale;
            Gizmos.DrawWireCube(targetCanvas.transform.position, new Vector3(effectiveSize.x, effectiveSize.y, 0.01f));
            
            // Draw distance sphere
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Vector3.zero, maxRaycastDistance);
            
            // Draw ray from origin to canvas
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, targetCanvas.transform.position);
        }

        /// <summary>
        /// Auto-configure based on existing Canvas in scene
        /// </summary>
        [ContextMenu("Auto-Configure from Scene")]
        public void AutoConfigureFromScene()
        {
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            
            foreach (var canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    targetCanvas = canvas;
                    Debug.Log($"✅ Found World Space Canvas: {canvas.name}");
                    break;
                }
            }
            
            if (targetCanvas != null)
            {
                // Analyze current settings
                var currentDistance = Vector3.Distance(Vector3.zero, targetCanvas.transform.position);
                var currentScale = targetCanvas.transform.localScale.x;
                
                Debug.Log($"📊 Current Canvas Analysis:");
                Debug.Log($"   • Distance: {currentDistance:F2} units");
                Debug.Log($"   • Scale: {currentScale:F4}");
                
                if (currentScale < 0.1f)
                {
                    Debug.LogWarning($"⚠️ Canvas scale ({currentScale:F4}) is very small! This may cause interaction issues.");
                }
                
                if (currentDistance > maxRaycastDistance)
                {
                    Debug.LogWarning($"⚠️ Canvas distance ({currentDistance:F2}) exceeds max raycast distance ({maxRaycastDistance})!");
                }
            }
        }
    }
}
