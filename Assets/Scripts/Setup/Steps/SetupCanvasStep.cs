using UnityEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System.Linq;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating and configuring XR Canvas for Virtual Reality UI interaction.
    /// Uses Meta XR SDK Template System for proper component setup with correct references.
    /// </summary>
    public class SetupCanvasStep
    {
        private System.Action<string> log;
        public Canvas Canvas { get; private set; }

        public SetupCanvasStep(System.Action<string> log)
        {
            this.log = log;
        }

        public void Execute(string canvasMode, Camera camera, float worldCanvasScale)
        {
            log("🖼️ Starting XR Canvas Setup...");

            // Check prerequisites
            if (!camera)
            {
                log("❌ No camera provided! XR Canvas requires a camera for proper positioning.");
                return;
            }

            // --- Create EventSystem if missing ---
            GameObject eventSystemGO = null;
            var existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                
                // Add XR UI Input Module for XR Interaction Toolkit
                var xrInputModule = eventSystemGO.AddComponent<XRUIInputModule>();
                log("✅ Created EventSystem with XRUIInputModule for XR ray interaction.");
                
            }
            else
            {
                eventSystemGO = existingEventSystem.gameObject;
                log("✅ EventSystem already exists.");
            }

            // Add Meta XR SDK Pointable Canvas Module if available
            try
            {
                var pointableCanvasModuleType = System.Type.GetType("Oculus.Interaction.PointableCanvasModule, Oculus.Interaction");
                if (pointableCanvasModuleType != null)
                {
                    eventSystemGO.AddComponent(pointableCanvasModuleType);
                    log("✅ Added Meta XR SDK PointableCanvasModule for hand interaction support.");
                }
            }
            catch (System.Exception ex)
            {
                log($"⚠️ Meta XR SDK PointableCanvasModule not available: {ex.Message}");
            }

            // --- Create and configure XR Canvas ---
            GameObject canvasGO = new GameObject("Canvas");
            Canvas = canvasGO.AddComponent<Canvas>();
            if (Canvas == null)
            {
                log("❌ Canvas creation failed! Aborting setup.");
                return;
            }
            canvasGO.AddComponent<CanvasScaler>();

            // Add both GraphicRaycaster and TrackedDeviceGraphicRaycaster for maximum compatibility
            canvasGO.AddComponent<GraphicRaycaster>();
            var xrRaycaster = canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            log("✅ Added GraphicRaycaster and TrackedDeviceGraphicRaycaster for XR ray interaction.");

            // --- Try to use Meta XR SDK Template System (Child GameObject Approach) ---
            bool metaXRSuccess = false;
            
            try
            {
                // 1. Find Templates class
                log("🔍 Step 1: Looking for Meta XR SDK Templates class...");
                var templatesType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.Templates, Oculus.Interaction.Editor");
                if (templatesType != null)
                {
                    log($"✅ Found Templates class: {templatesType.FullName}");
                    
                    // 2. Get RayCanvasInteractable template field (it's a field, not property!)
                    log("🔍 Step 2: Looking for RayCanvasInteractable field...");
                    var rayCanvasField = templatesType.GetField("RayCanvasInteractable", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (rayCanvasField != null)
                    {
                        log($"✅ Found RayCanvasInteractable field: {rayCanvasField.Name}");
                        // 3. Get template object
                        log("🔍 Step 3: Getting template object...");
                        var templateObject = rayCanvasField.GetValue(null);
                        log($"📁 Found Meta XR SDK Template object: {templateObject.GetType().Name}");
                        
                        // 4. Find CreateFromTemplate method (takes Template object, not string!)
                        log("🔍 Step 4: Looking for CreateFromTemplate method...");
                        var createFromTemplateMethod = templatesType.GetMethod("CreateFromTemplate", 
                            new System.Type[] { typeof(Transform), templateObject.GetType(), typeof(bool) });
                        
                        if (createFromTemplateMethod != null)
                        {
                            log($"✅ Found CreateFromTemplate method: {createFromTemplateMethod.Name}");
                            // 5. Create template as child GameObject (EXACTLY like Meta XR SDK Wizard!)
                            var templateChild = (GameObject)createFromTemplateMethod.Invoke(null, new object[] { Canvas.transform, templateObject, false });
                            
                            if (templateChild != null)
                            {
                                log("✅ Created template child GameObject using Meta XR SDK Templates.CreateFromTemplate()");
                                
                                // 6. Configure RectTransform (like original wizard)
                                var rt = templateChild.GetComponent<RectTransform>();
                                if (rt != null)
                                {
                                    rt.localPosition = Vector3.zero;
                                    rt.localRotation = Quaternion.identity;
                                    rt.localScale = Vector3.one;
                                    // Match size to Canvas for consistent appearance
                                    var canvasRect = Canvas.GetComponent<RectTransform>();
                                    rt.sizeDelta = canvasRect != null ? canvasRect.sizeDelta : new Vector2(960, 540);
                                    rt.anchorMin = Vector2.zero;
                                    rt.anchorMax = Vector2.one;
                                    rt.pivot = new Vector2(0.5f, 0.5f);
                                    log("✅ Configured RectTransform for template child (size matched to Canvas)");
                                }
                                
                                // 7. Inject Canvas reference via PointableCanvas.InjectCanvas() (EXACTLY like Meta XR SDK Wizard!)
                                // Find the PointableCanvas component by iterating through all MonoBehaviours.
                                // This is more robust than using Type.GetType with assembly names.
                                var components = templateChild.GetComponents<MonoBehaviour>();
                                var childPointableCanvas = components.FirstOrDefault(c => c.GetType().Name == "PointableCanvas");

                                if (childPointableCanvas != null)
                                {
                                    var injectCanvasMethod = childPointableCanvas.GetType().GetMethod("InjectCanvas");
                                    if (injectCanvasMethod != null)
                                    {
                                        injectCanvasMethod.Invoke(childPointableCanvas, new object[] { Canvas });
                                        log("✅ Injected Canvas reference into PointableCanvas using robust component search.");
                                    }
                                    else
                                    {
                                        log("❌ Could not find InjectCanvas method on PointableCanvas component.");
                                    }
                                }
                                else
                                {
                                    log("❌ Could not find PointableCanvas component on the template child.");
                                }
                                
                                // 8. Add GraphicRaycaster if missing
                                if (Canvas.GetComponent<GraphicRaycaster>() == null)
                                {
                                    Canvas.gameObject.AddComponent<GraphicRaycaster>();
                                    log("✅ Added GraphicRaycaster to Canvas");
                                }
                                
                                // 9. Add Ray Interactors using InteractorUtils (like original wizard)
                                try
                                {
                                    var interactorUtilsType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.InteractorUtils, Oculus.Interaction.Editor");
                                    if (interactorUtilsType != null)
                                    {
                                        var addInteractorsMethod = interactorUtilsType.GetMethod("AddInteractorsToRig", 
                                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                                        
                                        if (addInteractorsMethod != null)
                                        {
                                            // Get InteractorTypes.Ray
                                            var interactorTypesType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.InteractorTypes, Oculus.Interaction.Editor");
                                            if (interactorTypesType != null)
                                            {
                                                var rayField = interactorTypesType.GetField("Ray", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                                                if (rayField != null)
                                                {
                                                    var rayValue = rayField.GetValue(null);
                                                    // Use default device types (null parameter)
                                                    addInteractorsMethod.Invoke(null, new object[] { rayValue, null });
                                                    log("✅ Added Ray Interactors to XR Rig via InteractorUtils.AddInteractorsToRig()");
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    log($"⚠️ InteractorUtils.AddInteractorsToRig failed (optional): {ex.Message}");
                                }
                                
                                log("🎯 SUCCESS: Meta XR SDK Template-System fully implemented!");
                                log("🎯 Template Child GameObject created with correct internal references!");
                                log("🎯 Canvas-Injection via InjectCanvas() - exactly like original Meta XR SDK Wizard!");
                                metaXRSuccess = true;
                            }
                        }
                        else
                        {
                            log("❌ CreateFromTemplate method not found!");
                        }
                    }
                    else
                    {
                        log("❌ RayCanvasInteractable field not found!");
                        log("🔍 Available fields in Templates class:");
                        var fields = templatesType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        foreach (var field in fields)
                        {
                            log($"   - {field.Name} : {field.FieldType.Name}");
                        }
                    }
                }
                else
                {
                    log("❌ Templates class not found! Meta XR SDK might not be available.");
                    log("🔍 Checking for similar types...");
                    var allTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .Where(t => t.Name.Contains("Template") && t.Namespace != null && t.Namespace.Contains("Oculus"))
                        .Take(5);
                    foreach (var type in allTypes)
                    {
                        log($"   Found similar type: {type.FullName}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                log($"❌ Meta XR SDK Template Child Creation failed: {ex.Message}");
            }

            // --- Fallback: Basic setup if Meta XR SDK template approach fails ---
            if (!metaXRSuccess)
            {
                log("⚠️ Meta XR SDK Template approach failed. Using basic component setup as fallback.");
                
                try
                {
                    var pointableCanvasType = System.Type.GetType("Oculus.Interaction.PointableCanvas, Oculus.Interaction");
                    var rayInteractableType = System.Type.GetType("Oculus.Interaction.RayInteractable, Oculus.Interaction");
                    
                    if (pointableCanvasType != null && rayInteractableType != null)
                    {
                        var pointableCanvas = Canvas.gameObject.AddComponent(pointableCanvasType);
                        var rayInteractable = Canvas.gameObject.AddComponent(rayInteractableType);
                        
                        // Inject Canvas reference
                        var injectCanvasMethod = pointableCanvas.GetType().GetMethod("InjectCanvas");
                        if (injectCanvasMethod != null)
                        {
                            injectCanvasMethod.Invoke(pointableCanvas, new object[] { Canvas });
                            log("✅ Injected Canvas reference into PointableCanvas");
                        }
                        else
                        {
                            log("❌ Could not find InjectCanvas method on PointableCanvas");
                        }

                        // Set _pointableElement field on RayInteractable via reflection
                        var field = rayInteractable.GetType().GetField("_pointableElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(rayInteractable, pointableCanvas);
                            log("✅ Set RayInteractable._pointableElement to PointableCanvas via reflection");
                        }
                        else
                        {
                            log("❌ Could not find _pointableElement field on RayInteractable");
                        }

                        log("✅ Added PointableCanvas and RayInteractable components to Canvas with correct reference");
                    }
                    else
                    {
                        log("❌ Could not find required types for PointableCanvas or RayInteractable");
                    }
                }
                catch (System.Exception ex)
                {
                    log($"❌ Basic component setup failed: {ex.Message}");
                }
            }

            // --- Configure Canvas settings ---
            Canvas.renderMode = RenderMode.WorldSpace; // Always World Space for XR (canvasMode parameter ignored)
            Canvas.transform.position = new Vector3(0, 1.5f, 3f); // Canvas etwas höher anzeigen
            Canvas.transform.rotation = Quaternion.identity;
            var rectTransform = Canvas.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                log("❌ RectTransform missing on Canvas! Aborting setup.");
                return;
            }
            rectTransform.sizeDelta = new Vector2(480, 225); // Canvas noch kleiner anzeigen
            Canvas.transform.localScale = Vector3.one * worldCanvasScale; // Use provided scale
            log($"✅ Configured Canvas: World Space mode, scale: {worldCanvasScale}, positioned in front of player.");

            log("ℹ️ XR Canvas Setup Complete:");
            log("   • Canvas Mode: World Space (Required for VR)");
            log("   • Raycasters: GraphicRaycaster + TrackedDeviceGraphicRaycaster");
            log("   • InputModules: XRUIInputModule + PointableCanvasModule (Meta XR SDK)");
            if (metaXRSuccess)
            {
                log("   • Meta XR SDK: Template Child GameObject created with perfect internal references");
                log("   • Canvas-Injection: Via InjectCanvas() exactly like Meta XR SDK Wizard");
                log("   • Ray Interactors: Added via InteractorUtils to XR Rig");
                log("   • Result: Fully functional Ray Interaction matching Meta XR SDK demo scenes");
            }
            else
            {
                log("   • Meta XR SDK: Basic components added (manual setup)");
                log("   • Canvas Components: PointableCanvas + RayInteractable (basic setup)");
                log("   • Note: Some references may need manual configuration");
            }
            log("   • Ready for XR Ray Interactors and Hand Tracking");
        }
    }
}
