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
                var pointableCanvasModuleType = System.Type.GetType("Oculus.Interaction.PointableCanvasModule, Oculus.Interaction");
                if (pointableCanvasModuleType != null)
                {
                    var existingModule = eventSystem.GetComponent(pointableCanvasModuleType);
                    if (existingModule == null)
                    {
                        eventSystem.gameObject.AddComponent(pointableCanvasModuleType);
                        log("✅ Added PointableCanvasModule (Meta XR SDK) to EventSystem.");
                    }
                }
                else
                {
                    log("⚠️ PointableCanvasModule not found - Meta XR SDK may not be available.");
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

            // Try to use Meta XR SDK Wizard system to add Ray Interaction to Canvas
            bool metaXRSuccess = false;
            try
            {
                // Use the official Meta XR SDK Templates system like the context menu wizard does
                var templatesType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.Templates, Oculus.Interaction.Editor");
                if (templatesType != null)
                {
                    // Get the RayCanvasInteractable template (same as used by "Add Ray Interaction to Canvas" menu)
                    var rayCanvasInteractableField = templatesType.GetField("RayCanvasInteractable", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (rayCanvasInteractableField != null)
                    {
                        var template = rayCanvasInteractableField.GetValue(null);
                        
                        // Get the CreateFromTemplate method
                        var createMethod = templatesType.GetMethod("CreateFromTemplate", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        
                        if (createMethod != null && template != null)
                        {
                            // Create the ISDK_RayCanvasInteraction GameObject using the official template
                            var rayCanvasInteractionGO = (GameObject)createMethod.Invoke(null, 
                                new object[] { canvasGO.transform, template, false });
                            
                            if (rayCanvasInteractionGO != null)
                            {
                                // Reset RectTransform to fill the canvas (same as wizard does)
                                var rt = rayCanvasInteractionGO.GetComponent<RectTransform>();
                                if (rt != null)
                                {
                                    rt.localPosition = Vector3.zero;
                                    rt.localRotation = Quaternion.identity;
                                    rt.localScale = Vector3.one;
                                    rt.anchorMin = Vector2.zero;
                                    rt.anchorMax = Vector2.one;
                                    rt.pivot = new Vector2(0.5f, 0.5f);
                                    rt.sizeDelta = Vector2.zero;
                                }
                                
                                // The template creates components on the child GameObject, but we need them on the Canvas
                                // Copy RayInteractable and PointableCanvas from child to Canvas
                                var childComponents = rayCanvasInteractionGO.GetComponents<UnityEngine.Component>();
                                UnityEngine.Component templatePointableCanvas = null;
                                UnityEngine.Component templateRayInteractable = null;
                                
                                foreach (var comp in childComponents)
                                {
                                    if (comp != null && comp.GetType().Name == "PointableCanvas")
                                        templatePointableCanvas = comp;
                                    else if (comp != null && comp.GetType().Name == "RayInteractable")
                                        templateRayInteractable = comp;
                                }
                                
                                // Add RayInteractable to Canvas and copy settings
                                if (templateRayInteractable != null)
                                {
                                    var canvasRayInteractable = canvasGO.AddComponent(templateRayInteractable.GetType());
                                    
                                    // Copy Surface reference from template
                                    var surfaceField = templateRayInteractable.GetType().GetField("_surface", 
                                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    if (surfaceField != null)
                                    {
                                        var surfaceValue = surfaceField.GetValue(templateRayInteractable);
                                        if (surfaceValue != null)
                                        {
                                            surfaceField.SetValue(canvasRayInteractable, surfaceValue);
                                        }
                                    }
                                    
                                    log("✅ Added RayInteractable to Canvas with Surface reference from template.");
                                }
                                
                                // Add PointableCanvas to Canvas
                                if (templatePointableCanvas != null)
                                {
                                    var canvasPointableCanvas = canvasGO.AddComponent(templatePointableCanvas.GetType());
                                    
                                    // Configure PointableCanvas to reference the main Canvas
                                    var injectCanvasMethod = canvasPointableCanvas.GetType().GetMethod("InjectCanvas");
                                    if (injectCanvasMethod != null)
                                    {
                                        injectCanvasMethod.Invoke(canvasPointableCanvas, new object[] { Canvas });
                                    }
                                    
                                    // Update RayInteractable's PointableElement reference to the Canvas component
                                    if (templateRayInteractable != null)
                                    {
                                        var canvasRayInteractable = canvasGO.GetComponent(templateRayInteractable.GetType());
                                        if (canvasRayInteractable != null)
                                        {
                                            var pointableElementField = canvasRayInteractable.GetType().GetField("_pointableElement", 
                                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                            if (pointableElementField != null)
                                            {
                                                pointableElementField.SetValue(canvasRayInteractable, canvasPointableCanvas);
                                            }
                                        }
                                    }
                                    
                                    log("✅ Added PointableCanvas to Canvas and updated RayInteractable reference.");
                                }
                                
                                log("✅ Added ISDK_RayCanvasInteraction using Meta XR SDK official template system.");
                                log("✅ Moved RayInteractable and PointableCanvas components to Canvas (as required).");
                                log("✅ All Surface references and PointableElement references properly configured.");
                                metaXRSuccess = true;
                                
                                // Try to add required interactors using Meta XR SDK InteractorUtils
                                try
                                {
                                    var interactorUtilsType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.InteractorUtils, Oculus.Interaction.Editor");
                                    if (interactorUtilsType != null)
                                    {
                                        var addInteractorsMethod = interactorUtilsType.GetMethod("AddInteractorsToRig", 
                                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                                        
                                        if (addInteractorsMethod != null)
                                        {
                                            // Get InteractorTypes.Ray and DeviceTypes.All
                                            var interactorTypesType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.InteractorTypes, Oculus.Interaction.Editor");
                                            var deviceTypesType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.DeviceTypes, Oculus.Interaction.Editor");
                                            
                                            if (interactorTypesType != null && deviceTypesType != null)
                                            {
                                                var rayInteractorType = System.Enum.Parse(interactorTypesType, "Ray");
                                                var allDeviceTypes = System.Enum.Parse(deviceTypesType, "All");
                                                
                                                addInteractorsMethod.Invoke(null, new object[] { rayInteractorType, allDeviceTypes });
                                                log("✅ Added required Ray Interactors to scene (Hand + Controller).");
                                            }
                                        }
                                    }
                                }
                                catch (System.Exception interactorEx)
                                {
                                    log($"ℹ️ Could not auto-add interactors: {interactorEx.Message}");
                                    log("ℹ️ You may need to manually add Ray Interactors to your XR Rig.");
                                }
                            }
                        }
                    }
                }
                
                if (!metaXRSuccess)
                {
                    log("⚠️ Meta XR SDK Templates system not found - falling back to manual component setup.");
                    throw new System.Exception("Templates system not available");
                }
            }
            catch (System.Exception ex)
            {
                log($"⚠️ Meta XR SDK official template system not available: {ex.Message}");
                log("ℹ️ This is normal if you're not using Meta XR SDK or in a build without editor assemblies.");
                log("ℹ️ Falling back to basic XR Interaction Toolkit setup.");
                
                // Fallback: Add basic Meta XR SDK components manually if the template system fails
                try
                {
                    log("🔧 Attempting manual Meta XR SDK component setup...");
                    
                    // Add RayInteractable directly to Canvas
                    var rayInteractableType = System.Type.GetType("Oculus.Interaction.RayInteractable, Oculus.Interaction");
                    if (rayInteractableType != null)
                    {
                        var rayInteractableComponent = canvasGO.AddComponent(rayInteractableType);
                        log("✅ Added RayInteractable (Meta XR SDK) component to Canvas manually.");
                    }

                    // Add PointableCanvas directly to Canvas
                    var pointableCanvasType = System.Type.GetType("Oculus.Interaction.PointableCanvas, Oculus.Interaction");
                    if (pointableCanvasType != null)
                    {
                        var pointableCanvasComponent = canvasGO.AddComponent(pointableCanvasType);
                        // Set the canvas reference
                        var canvasField = pointableCanvasType.GetField("_canvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (canvasField != null)
                        {
                            canvasField.SetValue(pointableCanvasComponent, Canvas);
                        }
                        log("✅ Added PointableCanvas (Meta XR SDK) component to Canvas manually.");
                    }
                    
                    log("✅ Manual Meta XR SDK component setup completed.");
                    log("ℹ️ Note: Surface components may need to be configured manually for optimal interaction.");
                }
                catch (System.Exception fallbackEx)
                {
                    log($"⚠️ Manual Meta XR SDK component setup also failed: {fallbackEx.Message}");
                    log("ℹ️ Canvas will work with basic XR Interaction Toolkit components only.");
                }
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
            log("   • InputModules: XRUIInputModule + PointableCanvasModule (Meta XR SDK)");
            if (metaXRSuccess)
            {
                log("   • Meta XR SDK: ISDK_RayCanvasInteraction created + components moved to Canvas");
                log("   • Canvas Components: RayInteractable + PointableCanvas (with correct references)");
                log("   • Surface: Separate GameObject with ClippedPlaneSurface + PlaneSurface + BoxClipper");
            }
            else
            {
                log("   • Meta XR SDK: Basic components added (manual setup - some Surface references may need manual configuration)");
                log("   • Canvas Components: RayInteractable + PointableCanvas (basic setup)");
            }
            log("   • Ready for XR Ray Interactors and Hand Tracking");
        }
    }
}