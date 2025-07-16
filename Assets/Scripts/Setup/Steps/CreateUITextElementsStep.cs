using UnityEngine;
using TMPro;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating all the TextMeshPro UI elements.
    /// </summary>
    public class CreateUITextElementsStep
    {
        private System.Action<string> log;
        private GameObject panel;

        public TextMeshProUGUI StatusDisplay { get; private set; }
        public TextMeshProUGUI ConversationDisplay { get; private set; }

        public CreateUITextElementsStep(System.Action<string> log, GameObject panel)
        {
            this.log = log;
            this.panel = panel;
        }

        public void Execute()
        {
            log("📄 Step 2.4: UI Text Element Creation");

            // MainDemo 15.unity positioning structure
            StatusDisplay = CreateTextMeshPro("Status Display", "Status: Disconnected", 0.45f, 0.5f, 10);
            ConversationDisplay = CreateTextMeshPro("Conversation Display", "OpenAI Realtime NPC Chat...", 0.15f, 0.4f, 10);

            log("✅ All UI text elements created.");
        }

        private TextMeshProUGUI CreateTextMeshPro(string name, string text, float yMin, float yMax, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(panel.transform, false);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.raycastTarget = false; // Important for not blocking clicks

            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            // MainDemo 15.unity exact positioning structure
            rectTransform.anchorMin = new Vector2(0.1f, yMin);
            rectTransform.anchorMax = new Vector2(0.9f, yMax);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            log($"✅ Created text element: {name} (MainDemo 15.unity structure)");
            return textComponent;
        }
    }
}
