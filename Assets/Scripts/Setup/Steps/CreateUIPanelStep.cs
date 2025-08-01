using UnityEngine;
using UnityEngine.UI;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating and configuring the main UI Panel.
    /// </summary>
    public class CreateUIPanelStep
    {
        private System.Action<string> log;
        public GameObject Panel { get; private set; }
        public MonoBehaviour UiManager { get; private set; }

        public CreateUIPanelStep(System.Action<string> log)
        {
            this.log = log;
        }

        public void Execute(Canvas canvas, Vector2 panelSize, Vector2 panelPosition)
        {
            log("🖼️ Step 2.2: UI Panel Creation");

            if (canvas == null)
            {
                log("❌ Canvas is null! Cannot create UI Panel.");
                return;
            }

            // --- Cleanup: Remove old panel ---
            var oldPanel = GameObject.Find("NPC UI Panel");
            if (oldPanel != null)
            {
                Object.DestroyImmediate(oldPanel);
                log("✅ Removed existing NPC UI Panel.");
            }

            // --- Create Panel (following MainDemo 15.unity structure) ---
            Panel = new GameObject("NPC UI Panel");
            Panel.transform.SetParent(canvas.transform, false);
            Panel.layer = LayerMask.NameToLayer("UI");
            var panelRect = Panel.AddComponent<RectTransform>();
            
            // MainDemo 15.unity Panel positioning: anchored to bottom center
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            // Set panel size to match Canvas size for consistent scaling
            var canvasRect = canvas.GetComponent<RectTransform>();
            panelRect.sizeDelta = canvasRect != null ? canvasRect.sizeDelta : new Vector2(960, 540);
            panelRect.anchoredPosition = panelPosition; // (0, 0) for bottom center
            panelRect.localScale = Vector3.one;
            var panelImage = Panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            log("✅ UI Panel created and size matched to Canvas.");

            // --- Add NpcUiManager ---
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            if (uiManagerType != null)
            {
                UiManager = Panel.AddComponent(uiManagerType) as MonoBehaviour;
                log($"✅ {uiManagerType.Name} component added to Panel.");
            }
            else
            {
                log($"❌ NpcUiManager type not found. Please ensure the script exists and is in the correct namespace.");
            }

            // Note: Advanced layout management will be handled automatically by improved positioning in CreateUIControlsStep
            log("ℹ️ UI layout optimized for both Desktop and VR compatibility");
        }
    }
}
