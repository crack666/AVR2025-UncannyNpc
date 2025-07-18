using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating all the UI buttons.
    /// </summary>
    public class CreateUIButtonsStep
    {
        private System.Action<string> log;
        private GameObject panel;

        public Button ConnectButton { get; private set; }
        public Button DisconnectButton { get; private set; }
        public Button StartConversationButton { get; private set; }
        public Button StopConversationButton { get; private set; }
        public Button SendMessageButton { get; private set; }

        public CreateUIButtonsStep(System.Action<string> log, GameObject panel)
        {
            this.log = log;
            this.panel = panel;
        }

        public void Execute()
        {
            log("🖲️ Step 2.3: UI Button Creation");

            ConnectButton = CreateButton("Connect Button", "Connect", 0);
            DisconnectButton = CreateButton("Disconnect Button", "Disconnect", 1);
            StartConversationButton = CreateButton("Start Conversation Button", "Start Listening", 2);
            StopConversationButton = CreateButton("Stop Conversation Button", "Stop Listening", 3);
            SendMessageButton = CreateButton("Send Message Button", "Send", 4);

            log("✅ All UI buttons created.");
        }

        private Button CreateButton(string name, string text, int index)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(panel.transform, false);

            Image buttonImage = buttonGO.AddComponent<Image>();
            if (name.Contains("Connect")) buttonImage.color = new Color(0.2f, 0.5f, 0.2f, 1f);
            else if (name.Contains("Disconnect")) buttonImage.color = new Color(0.5f, 0.2f, 0.2f, 1f);
            else if (name.Contains("Send")) buttonImage.color = new Color(0.2f, 0.2f, 0.5f, 1f);
            else buttonImage.color = new Color(0.2f, 0.6f, 1.0f, 1.0f);

            Button buttonComponent = buttonGO.AddComponent<Button>();
            RectTransform rectTransform = buttonGO.GetComponent<RectTransform>();

            // MainDemo 15.unity button positioning structure
            if (name.Contains("Send"))
            {
                rectTransform.anchorMin = new Vector2(0.91f, 0.05f);
                rectTransform.anchorMax = new Vector2(0.99f, 0.12f);
            }
            else
            {
                // MainDemo 15.unity button layout: Connect(0.1-0.26), Disconnect(0.28-0.44), etc.
                float[] buttonStarts = { 0.1f, 0.28f, 0.46f, 0.64f, 0.82f };
                float[] buttonEnds = { 0.26f, 0.44f, 0.62f, 0.8f, 0.98f };
                
                if (index < buttonStarts.Length)
                {
                    rectTransform.anchorMin = new Vector2(buttonStarts[index], 0.85f);
                    rectTransform.anchorMax = new Vector2(buttonEnds[index], 0.95f);
                }
                else
                {
                    // Fallback for additional buttons
                    float x = 0.1f + index * 0.18f;
                    rectTransform.anchorMin = new Vector2(x, 0.85f);
                    rectTransform.anchorMax = new Vector2(x + 0.16f, 0.95f);
                }
            }
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonGO.transform, false);
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 8; // Smaller font size like MainDemo 15.unity
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            log($"✅ Created button: {name} (MainDemo 15.unity structure)");
            return buttonComponent;
        }
    }
}
