using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating and configuring XR Canvas and EventSystem for Virtual Reality applications.
    /// This step is specifically designed for VR and enforces World Space Canvas mode.
    /// </summary>
    public class SetupCanvasStep
    {
        private System.Action<string> log;
        public Canvas Canvas { get; private set; }

        public SetupCanvasStep(System.Action<string> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Creates an XR-compatible Canvas for VR applications.
        /// Note: canvasMode parameter is ignored as VR requires World Space mode.
        /// </summary>
        public void Execute(string canvasMode = "WorldSpace", Camera camera = null, float worldCanvasScale = 0.01f)
        {
            log("🎨 Step 2.1: XR UI Canvas Setup");

            // --- Cleanup: Remove old UI objects to prevent conflicts ---
            var oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null)
            {
                Object.DestroyImmediate(oldCanvas);
                log("✅ Removed existing UI Canvas.");
            }

            // --- Ensure EventSystem exists with XR support ---
            var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var esGO = new GameObject("EventSystem");
                eventSystem = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                log("✅ Created EventSystem.");
            }

            // Replace StandaloneInputModule with XRUIInputModule for XR support
            var standaloneInput = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standaloneInput != null)
            {
                Object.DestroyImmediate(standaloneInput);
                log("✅ Removed StandaloneInputModule.");
            }

            // Add XRUIInputModule for XR interaction support
            var xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
            if (xrInputModule == null)
            {
                xrInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
                log("✅ Added XRUIInputModule for XR interaction support.");
            }

            // Try to add Meta XR SDK PointableCanvasModule if available
            try
            {
                var pointableCanvasModuleType = System.Type.GetType("Oculus.Interaction.Input.PointableCanvasModule, Oculus.Interaction");
                if (pointableCanvasModuleType != null)
                {
                    var existingModule = eventSystem.GetComponent(pointableCanvasModuleType);
                    if (existingModule == null)
                    {
                        eventSystem.gameObject.AddComponent(pointableCanvasModuleType);
                        log("✅ Added PointableCanvasModule (Meta XR SDK) to EventSystem.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                log($"ℹ️ PointableCanvasModule (Meta XR SDK) not available: {ex.Message}");
            }

            // --- Create and configure XR Canvas ---
            GameObject canvasGO = new GameObject("Canvas");
            Canvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<CanvasScaler>();
            
            // Add both GraphicRaycaster and TrackedDeviceGraphicRaycaster for maximum compatibility
            canvasGO.AddComponent<GraphicRaycaster>();
            var xrRaycaster = canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            log("✅ Added GraphicRaycaster and TrackedDeviceGraphicRaycaster for XR ray interaction.");

            // Try to add Meta XR SDK components if available
            try
            {
                // Add RayInteractable component (Meta XR SDK)
                var rayInteractableType = System.Type.GetType("Oculus.Interaction.RayInteractable, Oculus.Interaction");
                if (rayInteractableType != null)
                {
                    canvasGO.AddComponent(rayInteractableType);
                    log("✅ Added RayInteractable (Meta XR SDK) component.");
                }

                // Add PointableCanvas component (Meta XR SDK)
                var pointableCanvasType = System.Type.GetType("Oculus.Interaction.PointableCanvas, Oculus.Interaction");
                if (pointableCanvasType != null)
                {
                    var pointableCanvas = canvasGO.AddComponent(pointableCanvasType);
                    // Set the canvas reference
                    var canvasField = pointableCanvasType.GetField("_canvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (canvasField != null)
                    {
                        canvasField.SetValue(pointableCanvas, Canvas);
                    }
                    log("✅ Added PointableCanvas (Meta XR SDK) component.");
                }
            }
            catch (System.Exception ex)
            {
                log($"⚠️ Meta XR SDK components not available: {ex.Message}");
                log("ℹ️ This is normal if you're not using Meta XR SDK or Oculus Integration.");
            }
            
            // Force World Space mode for VR (other modes don't work properly in VR)
            Canvas.renderMode = RenderMode.WorldSpace;
            Canvas.transform.position = new Vector3(0, 1.5f, 2f); // Default position in front of user
            Canvas.transform.rotation = Quaternion.identity;
            Canvas.transform.localScale = Vector3.one * worldCanvasScale;
            
            log($"✅ XR Canvas created with RenderMode: {Canvas.renderMode}");
            log($"ℹ️ Canvas positioned at {Canvas.transform.position} with scale {worldCanvasScale}");

            // --- Configure RectTransform for World Space ---
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(1920, 1080); // Set explicit size for World Space Canvas
            log("✅ Canvas RectTransform configured for XR World Space.");
            
            // Add helpful information about XR Canvas setup
            log("ℹ️ XR Canvas Setup Complete:");
            log("   • Canvas Mode: World Space (Required for VR)");
            log("   • Raycasters: GraphicRaycaster + TrackedDeviceGraphicRaycaster");
            log("   • InputModules: XRUIInputModule (+ PointableCanvasModule if Meta XR SDK available)");
            log("   • Meta XR SDK Components: RayInteractable + PointableCanvas (if available)");
            log("   • Ready for XR Ray Interactors and Hand Tracking");
        }
    }
}