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

            // --- Create Panel ---
            Panel = new GameObject("NPC UI Panel");
            Panel.transform.SetParent(canvas.transform, false);
            var panelRect = Panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = panelPosition;
            var panelImage = Panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            log("✅ UI Panel created and configured.");

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
