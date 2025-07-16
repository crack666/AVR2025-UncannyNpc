using UnityEngine;
using System.Collections;

namespace Setup.Steps
{
    /// <summary>
    /// Orchestrator for the entire UI setup process.
    /// It uses smaller, single-responsibility steps to build the UI.
    /// </summary>
    public class CreateUISystemStep
    {
        private System.Action<string> log;

        public CreateUISystemStep(System.Action<string> log)
        {
            this.log = log;
        }

        // Synchronous version for Editor/Setup use
        // Note: canvasMode parameter is ignored in XR mode - always uses World Space
        public void ExecuteSync(Vector2 panelSize, Vector2 panelPosition, string canvasMode, Camera camera, float worldCanvasScale)
        {
            log("🎨 Step 2: XR UI System Creation");

            // 1. Setup XR Canvas (canvasMode is ignored - XR requires World Space)
            var canvasStep = new SetupCanvasStep(log);
            canvasStep.Execute("WorldSpace", camera, worldCanvasScale); // Force World Space for XR
            var canvas = canvasStep.Canvas;

            // 2. Create Panel and UI Manager
            var panelStep = new CreateUIPanelStep(log);
            panelStep.Execute(canvas, panelSize, panelPosition);
            var panel = panelStep.Panel;
            var uiManager = panelStep.UiManager;

            // 3. Create UI Elements
            var buttonsStep = new CreateUIButtonsStep(log, panel);
            buttonsStep.Execute();

            var textElementsStep = new CreateUITextElementsStep(log, panel);
            textElementsStep.Execute();

            var inputFieldsStep = new CreateUIInputFieldsStep(log, panel);
            inputFieldsStep.Execute();

            var loadAvatarsStep = new LoadAvatarsStep(log, -1); // Default keiner
            loadAvatarsStep.Execute();

            var controlsStep = new CreateUIControlsStep(log, panel);
            controlsStep.Execute();

            // 4. Setup XR Interaction guidance
            log("🎯 XR Interaction Setup Guidance:");
            log("   • Canvas is configured with both GraphicRaycaster and TrackedDeviceGraphicRaycaster");
            log("   • EventSystem has XRUIInputModule and PointableCanvasModule (if Meta XR SDK available)");
            log("   • Canvas has RayInteractable and PointableCanvas components (if Meta XR SDK available)");
            log("   • Add XR Ray Interactor components to hand/controller objects");
            log("   • Ensure 'Enable UI Interaction' is checked on Ray Interactors");
            log("   • Configure Interaction Layer Masks as needed");

            // 5. Link all references to the UI Manager
            var linkStep = new LinkUIManagerReferencesStep(log, uiManager);
            linkStep.Execute(
                buttonsStep.ConnectButton, buttonsStep.DisconnectButton, buttonsStep.StartConversationButton, buttonsStep.StopConversationButton, buttonsStep.SendMessageButton,
                textElementsStep.StatusDisplay, textElementsStep.ConversationDisplay,
                inputFieldsStep.MessageInputField,
                controlsStep.VoiceDropdown, controlsStep.VolumeSlider, controlsStep.EnableVADToggle
            );

            log("✅ UI System setup complete.");
        }

        // [Optional] Keep for compatibility, but mark as obsolete
        [System.Obsolete("Use ExecuteSync instead. Coroutines are not supported in Editor setup.")]
        public System.Collections.IEnumerator Execute(Vector2 panelSize, Vector2 panelPosition, string canvasMode, Camera camera, float worldCanvasScale)
        {
            ExecuteSync(panelSize, panelPosition, canvasMode, camera, worldCanvasScale);
            yield break;
        }
    }
}