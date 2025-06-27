using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Setup.Steps
{
    /// <summary>
    /// Step: Create and configure the main UI Canvas, Panel, Buttons, TextFields, InputField, and Manager
    /// </summary>
    public class CreateUISystemStep
    {
        public Canvas Canvas { get; private set; }
        public GameObject Panel { get; private set; }
        public MonoBehaviour UiManager { get; private set; }

        public void Execute(Vector2 panelSize, Vector2 panelPosition)
        {
            // Canvas
            GameObject canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("Canvas");
                Canvas = canvasGO.AddComponent<Canvas>();
            }
            else
            {
                Canvas = canvasGO.GetComponent<Canvas>();
                if (Canvas == null) Canvas = canvasGO.AddComponent<Canvas>();
            }
            if (canvasGO.GetComponent<CanvasScaler>() == null) canvasGO.AddComponent<CanvasScaler>();
            if (canvasGO.GetComponent<GraphicRaycaster>() == null) canvasGO.AddComponent<GraphicRaycaster>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = canvasGO.GetComponent<RectTransform>() ?? canvasGO.AddComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            // Panel
            Panel = GameObject.Find("NPC UI Panel") ?? new GameObject("NPC UI Panel");
            Panel.transform.SetParent(Canvas.transform, false);
            var panelRect = Panel.GetComponent<RectTransform>() ?? Panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = panelPosition;
            var panelImage = Panel.GetComponent<Image>() ?? Panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f,0.1f,0.1f,0.8f);

            // Buttons
            CreateButton("Connect Button", "Connect", 0);
            CreateButton("Disconnect Button", "Disconnect", 1);
            CreateButton("Start Conversation Button", "Start Listening", 2);
            CreateButton("Stop Conversation Button", "Stop Listening", 3);
            CreateButton("Send Message Button", "Send", 4);

            // TextFields
            CreateTextMeshPro("Status Display", "Status: Disconnected", 0.45f, 0.5f, 14);
            CreateTextMeshPro("Conversation Display", "OpenAI Realtime NPC Chat...", 0.15f, 0.4f, 12);

            // InputField
            CreateInputField();

            // Add NpcUiManager
            var uiManagerType = System.Type.GetType("Managers.NpcUiManager") ?? System.Type.GetType("NpcUiManager");
            if (uiManagerType != null)
            {
                UiManager = Panel.GetComponent(uiManagerType) as MonoBehaviour ?? Panel.AddComponent(uiManagerType) as MonoBehaviour;
            }
        }

        private void CreateButton(string name, string text, int index)
        {
            if (GameObject.Find(name) != null) return;
            GameObject button = new GameObject(name);
            button.transform.SetParent(Panel.transform, false);
            Image buttonImage = button.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1.0f, 1.0f);
            Button buttonComponent = button.AddComponent<Button>();
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.9f - (index * 0.12f));
            rectTransform.anchorMax = new Vector2(0.9f, 0.95f - (index * 0.12f));
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void CreateTextMeshPro(string name, string text, float yMin, float yMax, int fontSize)
        {
            if (GameObject.Find(name) != null) return;
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(Panel.transform, false);
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.textWrappingMode = TMPro.TextWrappingModes.Normal;
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, yMin);
            rectTransform.anchorMax = new Vector2(0.9f, yMax);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateInputField()
        {
            if (GameObject.Find("Message Input Field") != null) return;
            GameObject inputField = new GameObject("Message Input Field");
            inputField.transform.SetParent(Panel.transform, false);
            Image inputImage = inputField.AddComponent<Image>();
            inputImage.color = new Color(1f, 1f, 1f, 0.1f);
            TMP_InputField inputComponent = inputField.AddComponent<TMP_InputField>();
            RectTransform rectTransform = inputField.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.05f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.12f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            // Text Area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputField.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);
            // Placeholder
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Type your message here...";
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(1f, 1f, 1f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.Left;
            RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            // Text
            GameObject text = new GameObject("Text");
            text.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            // Link to input field
            inputComponent.textViewport = textAreaRect;
            inputComponent.textComponent = textComponent;
            inputComponent.placeholder = placeholderText;
        }
    }
}
