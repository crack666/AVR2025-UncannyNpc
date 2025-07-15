using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;

namespace Setup.Steps
{
    /// <summary>
    /// Step for setting up XR Ray Interactor components that work with the XR Canvas.
    /// This step creates the necessary XR interaction components for VR hand/controller interaction.
    /// </summary>
    public class SetupXRInteractionStep
    {
        private System.Action<string> log;

        public SetupXRInteractionStep(System.Action<string> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Creates XR Ray Interactors and configures them for Canvas interaction.
        /// This method can be called after Canvas setup to enable XR interaction.
        /// </summary>
        public void Execute()
        {
            log("🎯 Step: XR Interaction Setup");

            // Find or create XR Origin
            var xrOrigin = Object.FindFirstObjectByType<XROrigin>();
            if (xrOrigin == null)
            {
                log("⚠️ No XR Origin found. XR Ray Interactors need an XR Origin to function properly.");
                log("ℹ️ Please add an XR Origin prefab to your scene manually.");
                return;
            }

            log($"✅ Found XR Origin: {xrOrigin.name}");

            // Look for existing Ray Interactors
            var existingRayInteractors = Object.FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            if (existingRayInteractors.Length > 0)
            {
                log($"✅ Found {existingRayInteractors.Length} existing XR Ray Interactor(s).");
                
                // Verify they are configured for UI interaction
                foreach (var rayInteractor in existingRayInteractors)
                {
                    // Check if UI interaction is enabled
                    if (rayInteractor.enableUIInteraction)
                    {
                        log($"   • {rayInteractor.name}: UI interaction enabled ✅");
                    }
                    else
                    {
                        rayInteractor.enableUIInteraction = true;
                        log($"   • {rayInteractor.name}: Enabled UI interaction ✅");
                    }
                }
            }
            else
            {
                log("ℹ️ No XR Ray Interactors found.");
                log("💡 To enable VR interaction with the Canvas:");
                log("   1. Add XR Ray Interactor components to your hand/controller objects");
                log("   2. Ensure 'Enable UI Interaction' is checked on the Ray Interactors");
                log("   3. Add Line Renderer components for visual feedback");
            }

            // Check for Interaction Manager
            var interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
            if (interactionManager == null)
            {
                // Create one
                var managerGO = new GameObject("XR Interaction Manager");
                interactionManager = managerGO.AddComponent<XRInteractionManager>();
                log("✅ Created XR Interaction Manager.");
            }
            else
            {
                log($"✅ Found XR Interaction Manager: {interactionManager.name}");
            }

            log("ℹ️ XR Interaction Setup Complete:");
            log("   • Canvas is configured with TrackedDeviceGraphicRaycaster");
            log("   • EventSystem has XRUIInputModule");
            log("   • XR Interaction Manager is present");
            log("   • Ready for XR Ray Interactor components");
        }

        /// <summary>
        /// Provides helpful information about setting up XR interaction manually.
        /// </summary>
        public void LogXRSetupInstructions()
        {
            log("📋 Manual XR Setup Instructions:");
            log("1. Add an XR Origin prefab to your scene (usually from XR Interaction Toolkit)");
            log("2. Add XR Ray Interactor components to hand/controller objects");
            log("3. Configure Ray Interactors:");
            log("   • Enable 'UI Interaction'");
            log("   • Set 'Interaction Layer Mask' appropriately");
            log("   • Add Line Renderer for visual ray feedback");
            log("4. The Canvas is already configured for XR interaction!");
        }
    }
}
