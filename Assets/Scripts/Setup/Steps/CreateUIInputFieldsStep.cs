using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating the message input field.
    /// </summary>
    public class CreateUIInputFieldsStep
    {
        private System.Action<string> log;
        private GameObject panel;

        public TMP_InputField MessageInputField { get; private set; }

        public CreateUIInputFieldsStep(System.Action<string> log, GameObject panel)
        {
            this.log = log;
            this.panel = panel;
        }

        public void Execute()
        {
            log("⌨️ Step 2.5: UI Input Field Creation");

            GameObject inputFieldGO = new GameObject("Message Input Field");
            inputFieldGO.transform.SetParent(panel.transform, false);

            Image inputImage = inputFieldGO.AddComponent<Image>();
            inputImage.color = new Color(1f, 1f, 1f, 0.1f);

            MessageInputField = inputFieldGO.AddComponent<TMP_InputField>();
            RectTransform rectTransform = inputFieldGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.05f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.12f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // --- Text Area ---
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputFieldGO.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);

            // --- Placeholder ---
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Type your message here...";
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(1f, 1f, 1f, 0.5f);
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.alignment = TextAlignmentOptions.Left;
            RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            // --- Text ---
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

            // --- Link components ---
            MessageInputField.textViewport = textAreaRect;
            MessageInputField.textComponent = textComponent;
            MessageInputField.placeholder = placeholderText;

            log("✅ UI input field created.");
        }
    }
}
