using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

namespace Setup.Steps
{
    /// <summary>
    /// Step for linking all UI elements to the NpcUiManager.
    /// </summary>
    public class LinkUIManagerReferencesStep
    {
        private System.Action<string> log;
        private MonoBehaviour uiManager;

        public LinkUIManagerReferencesStep(System.Action<string> log, MonoBehaviour uiManager)
        {
            this.log = log;
            this.uiManager = uiManager;
        }

        public void Execute(
            Button connectButton, Button disconnectButton, Button startButton, Button stopButton, Button sendButton,
            TextMeshProUGUI statusDisplay, TextMeshProUGUI conversationDisplay,
            TMP_InputField messageInput,
            TMP_Dropdown voiceDropdown, Slider volumeSlider, Toggle vadToggle)
        {
            log("🔗 Step 2.7: Linking UI Manager References");

            if (uiManager == null)
            {
                log("❌ NpcUiManager not found. Cannot link references.");
                return;
            }

            var uiManagerType = uiManager.GetType();

            // Link Buttons
            SetField(uiManager, uiManagerType, "connectButton", connectButton);
            SetField(uiManager, uiManagerType, "disconnectButton", disconnectButton);
            SetField(uiManager, uiManagerType, "startConversationButton", startButton);
            SetField(uiManager, uiManagerType, "stopConversationButton", stopButton);
            SetField(uiManager, uiManagerType, "sendMessageButton", sendButton);

            // Link Text Displays
            SetField(uiManager, uiManagerType, "statusDisplay", statusDisplay);
            SetField(uiManager, uiManagerType, "conversationDisplay", conversationDisplay);

            // Link Input Field
            SetField(uiManager, uiManagerType, "messageInputField", messageInput);

            // Link Controls
            SetField(uiManager, uiManagerType, "voiceDropdown", voiceDropdown);
            SetField(uiManager, uiManagerType, "volumeSlider", volumeSlider);
            SetField(uiManager, uiManagerType, "enableVADToggle", vadToggle);

            log("✅ All UI references linked to NpcUiManager.");
        }

        private void SetField(object target, System.Type type, string fieldName, object value)
        {
            if (value == null)
            {
                log($"⚠️ Value for '{fieldName}' is null. Skipping.");
                return;
            }

            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                log($"✅ Linked '{fieldName}'.");
            }
            else
            {
                log($"❌ Field '{fieldName}' not found in {type.Name}.");
            }
        }
    }
}
