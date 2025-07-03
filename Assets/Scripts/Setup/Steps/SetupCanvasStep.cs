using UnityEngine;
using UnityEngine.UI;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating and configuring the main UI Canvas and EventSystem.
    /// </summary>
    public class SetupCanvasStep
    {
        private System.Action<string> log;
        public Canvas Canvas { get; private set; }

        public SetupCanvasStep(System.Action<string> log)
        {
            this.log = log;
        }

        public void Execute(string canvasMode = "ScreenSpaceOverlay", Camera camera = null, float worldCanvasScale = 0.01f)
        {
            log("🎨 Step 2.1: UI Canvas Setup");

            // --- Cleanup: Remove old UI objects to prevent conflicts ---
            var oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null)
            {
                Object.DestroyImmediate(oldCanvas);
                log("✅ Removed existing UI Canvas.");
            }

            // --- Ensure EventSystem exists ---
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                log("✅ Created EventSystem.");
            }

            // --- Create and configure Canvas ---
            GameObject canvasGO = new GameObject("Canvas");
            Canvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Set Canvas Render Mode
            switch (canvasMode)
            {
                case "ScreenSpaceCamera":
                    Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    Canvas.worldCamera = camera ? camera : Camera.main;
                    if (Canvas.worldCamera == null) log("⚠️ ScreenSpaceCamera mode, but no camera assigned or found.");
                    break;
                case "WorldSpace":
                    Canvas.renderMode = RenderMode.WorldSpace;
                    Canvas.transform.position = new Vector3(0, 1.5f, 2f); // Default position
                    Canvas.transform.rotation = Quaternion.identity;
                    Canvas.transform.localScale = Vector3.one * worldCanvasScale;
                    break;
                default:
                    Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    break;
            }
            log($"✅ Canvas created with RenderMode: {Canvas.renderMode}");

            // --- Configure RectTransform ---
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            log("✅ Canvas RectTransform configured for full screen.");
        }
    }
}