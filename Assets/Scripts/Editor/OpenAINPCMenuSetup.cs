using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using Setup.Steps;

public class OpenAINPCMenuSetup : EditorWindow
{
    [MenuItem("OpenAI NPC/Quick Setup", false, 0)]
    public static void ShowWindow()
    {
        if (EditorUtility.DisplayDialog("OpenAI NPC Quick Setup", "Dies wird die komplette OpenAI NPC Infrastruktur automatisch in der aktuellen Szene erstellen. Fortfahren?", "Ja", "Abbrechen"))
        {
            RunFullSetup();
        }
    }

    public static void RunFullSetup()
    {
        // --- Sicherstellen, dass ein EventSystem existiert ---
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // 1. Settings Asset erstellen oder laden (nur OpenAISettings)
        var openAISettingsType = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == "OpenAISettings");
        ScriptableObject settings = null;
        if (openAISettingsType != null)
            settings = EnsureSettingsAsset(openAISettingsType, "OpenAISettings", "Assets/Resources/OpenAISettings.asset");

        // 2. OpenAI NPC System GameObject erstellen
        var npcSystem = GameObject.Find("OpenAI NPC System") ?? new GameObject("OpenAI NPC System");
        Undo.RegisterCreatedObjectUndo(npcSystem, "Create OpenAI NPC System");

        // 3. Komponenten hinzufügen
        var realtimeClient = AddOrGetComponent(npcSystem, "OpenAI.RealtimeAPI.RealtimeClient");
        var audioManager = AddOrGetComponent(npcSystem, "OpenAI.RealtimeAPI.RealtimeAudioManager");
        var npcController = AddOrGetComponent(npcSystem, "NPC.NPCController");

        // 4. PlaybackAudioSource erstellen
        var playbackObj = GameObject.Find("PlaybackAudioSource") ?? new GameObject("PlaybackAudioSource");
        playbackObj.transform.SetParent(npcSystem.transform);
        var playbackAudio = playbackObj.GetComponent<AudioSource>();
        if (playbackAudio == null)
            playbackAudio = playbackObj.AddComponent<AudioSource>();
        playbackAudio.playOnAwake = false;
        playbackAudio.loop = false;
        playbackAudio.volume = 1.0f;
        playbackAudio.spatialBlend = 0.0f;

        // 5. Komponenten referenzieren
        SetField(audioManager, "realtimeClient", realtimeClient);
        SetField(audioManager, "playbackAudioSource", playbackAudio);
        SetField(audioManager, "settings", settings);
        SetField(realtimeClient, "settings", settings); // Nur noch OpenAISettings
        SetField(npcController, "realtimeClient", realtimeClient);
        SetField(npcController, "audioManager", audioManager);

        // 6. ReadyPlayerMe Avatar suchen und LipSync-Komponente hinzufügen
        var avatar = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r => r.name.Contains("Wolf3D") || r.name.ToLower().Contains("head"));
        if (avatar != null)
        {
            var avatarGO = avatar.transform.root.gameObject;
            var lipSync = AddOrGetComponent(avatarGO, "Animation.ReadyPlayerMeLipSync");
            SetField(lipSync, "headMeshRenderer", avatar);
            SetField(lipSync, "audioSource", playbackAudio);
            SetField(npcController, "lipSyncController", lipSync);
        }

        // 7. UI Canvas + Panel + Buttons erstellen (ausgelagert)
        var canvasStep = new SetupCanvasStep();
        canvasStep.Execute();
        var canvas = canvasStep.Canvas;

        // Panel als UI-Element mit RectTransform anlegen
        var panelGO = GameObject.Find("NPC UI Panel") ?? new GameObject("NPC UI Panel", typeof(RectTransform));
        panelGO.transform.SetParent(canvas.transform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(600, 300);
        panelRect.anchoredPosition = new Vector2(0, 40);
        var panelImage = panelGO.GetComponent<Image>() ?? panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.1f,0.1f,0.1f,0.8f);

        // 1. Connect Button
        var connectBtnGO = new GameObject("ConnectButton", typeof(RectTransform));
        connectBtnGO.transform.SetParent(panelGO.transform, false);
        var connectBtnRect = connectBtnGO.GetComponent<RectTransform>();
        connectBtnRect.anchorMin = new Vector2(0f, 1f);
        connectBtnRect.anchorMax = new Vector2(0f, 1f);
        connectBtnRect.pivot = new Vector2(0f, 1f);
        connectBtnRect.sizeDelta = new Vector2(120, 40);
        connectBtnRect.anchoredPosition = new Vector2(20, -20);
        var connectBtn = connectBtnGO.AddComponent<Button>();
        var connectBtnImg = connectBtnGO.AddComponent<Image>();
        connectBtnImg.color = new Color(0.2f,0.5f,0.2f,1f);
        var connectBtnTextGO = new GameObject("Text", typeof(RectTransform));
        connectBtnTextGO.transform.SetParent(connectBtnGO.transform, false);
        var connectBtnText = connectBtnTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        connectBtnText.text = "Connect";
        connectBtnText.fontSize = 20;
        connectBtnText.alignment = TMPro.TextAlignmentOptions.Center;
        connectBtnText.rectTransform.anchorMin = Vector2.zero;
        connectBtnText.rectTransform.anchorMax = Vector2.one;
        connectBtnText.rectTransform.offsetMin = Vector2.zero;
        connectBtnText.rectTransform.offsetMax = Vector2.zero;

        // 1b. Disconnect Button
        var disconnectBtnGO = new GameObject("DisconnectButton", typeof(RectTransform));
        disconnectBtnGO.transform.SetParent(panelGO.transform, false);
        var disconnectBtnRect = disconnectBtnGO.GetComponent<RectTransform>();
        disconnectBtnRect.anchorMin = new Vector2(0f, 1f);
        disconnectBtnRect.anchorMax = new Vector2(0f, 1f);
        disconnectBtnRect.pivot = new Vector2(0f, 1f);
        disconnectBtnRect.sizeDelta = new Vector2(120, 40);
        disconnectBtnRect.anchoredPosition = new Vector2(150, -20);
        var disconnectBtn = disconnectBtnGO.AddComponent<Button>();
        var disconnectBtnImg = disconnectBtnGO.AddComponent<Image>();
        disconnectBtnImg.color = new Color(0.5f,0.2f,0.2f,1f);
        var disconnectBtnTextGO = new GameObject("Text", typeof(RectTransform));
        disconnectBtnTextGO.transform.SetParent(disconnectBtnGO.transform, false);
        var disconnectBtnText = disconnectBtnTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        disconnectBtnText.text = "Disconnect";
        disconnectBtnText.fontSize = 20;
        disconnectBtnText.alignment = TMPro.TextAlignmentOptions.Center;
        disconnectBtnText.rectTransform.anchorMin = Vector2.zero;
        disconnectBtnText.rectTransform.anchorMax = Vector2.one;
        disconnectBtnText.rectTransform.offsetMin = Vector2.zero;
        disconnectBtnText.rectTransform.offsetMax = Vector2.zero;

        // 2. Start Conversation Button
        var startConvBtnGO = new GameObject("StartConversationButton", typeof(RectTransform));
        startConvBtnGO.transform.SetParent(panelGO.transform, false);
        var startConvBtnRect = startConvBtnGO.GetComponent<RectTransform>();
        startConvBtnRect.anchorMin = new Vector2(0f, 1f);
        startConvBtnRect.anchorMax = new Vector2(0f, 1f);
        startConvBtnRect.pivot = new Vector2(0f, 1f);
        startConvBtnRect.sizeDelta = new Vector2(180, 40);
        startConvBtnRect.anchoredPosition = new Vector2(290, -20);
        var startConvBtn = startConvBtnGO.AddComponent<Button>();
        var startConvBtnImg = startConvBtnGO.AddComponent<Image>();
        startConvBtnImg.color = new Color(0.2f,0.4f,0.6f,1f);
        var startConvBtnTextGO = new GameObject("Text", typeof(RectTransform));
        startConvBtnTextGO.transform.SetParent(startConvBtnGO.transform, false);
        var startConvBtnText = startConvBtnTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        startConvBtnText.text = "Start Conversation";
        startConvBtnText.fontSize = 18;
        startConvBtnText.alignment = TMPro.TextAlignmentOptions.Center;
        startConvBtnText.rectTransform.anchorMin = Vector2.zero;
        startConvBtnText.rectTransform.anchorMax = Vector2.one;
        startConvBtnText.rectTransform.offsetMin = Vector2.zero;
        startConvBtnText.rectTransform.offsetMax = Vector2.zero;

        // 2b. Stop Conversation Button
        var stopConvBtnGO = new GameObject("StopConversationButton", typeof(RectTransform));
        stopConvBtnGO.transform.SetParent(panelGO.transform, false);
        var stopConvBtnRect = stopConvBtnGO.GetComponent<RectTransform>();
        stopConvBtnRect.anchorMin = new Vector2(0f, 1f);
        stopConvBtnRect.anchorMax = new Vector2(0f, 1f);
        stopConvBtnRect.pivot = new Vector2(0f, 1f);
        stopConvBtnRect.sizeDelta = new Vector2(180, 40);
        stopConvBtnRect.anchoredPosition = new Vector2(480, -20);
        var stopConvBtn = stopConvBtnGO.AddComponent<Button>();
        var stopConvBtnImg = stopConvBtnGO.AddComponent<Image>();
        stopConvBtnImg.color = new Color(0.4f,0.2f,0.6f,1f);
        var stopConvBtnTextGO = new GameObject("Text", typeof(RectTransform));
        stopConvBtnTextGO.transform.SetParent(stopConvBtnGO.transform, false);
        var stopConvBtnText = stopConvBtnTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        stopConvBtnText.text = "Stop Conversation";
        stopConvBtnText.fontSize = 18;
        stopConvBtnText.alignment = TMPro.TextAlignmentOptions.Center;
        stopConvBtnText.rectTransform.anchorMin = Vector2.zero;
        stopConvBtnText.rectTransform.anchorMax = Vector2.one;
        stopConvBtnText.rectTransform.offsetMin = Vector2.zero;
        stopConvBtnText.rectTransform.offsetMax = Vector2.zero;

        // 3. InputField
        var inputGO = new GameObject("MessageInputField", typeof(RectTransform));
        inputGO.transform.SetParent(panelGO.transform, false);
        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(1f, 0f);
        inputRect.pivot = new Vector2(0.5f, 0f);
        inputRect.sizeDelta = new Vector2(-180, 40);
        inputRect.anchoredPosition = new Vector2(0, 20);
        var inputField = inputGO.AddComponent<TMPro.TMP_InputField>();
        var inputFieldImg = inputGO.AddComponent<Image>();
        inputFieldImg.color = new Color(0.2f,0.2f,0.2f,1f);
        var inputTextGO = new GameObject("Text", typeof(RectTransform));
        inputTextGO.transform.SetParent(inputGO.transform, false);
        var inputText = inputTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        inputText.text = "";
        inputText.fontSize = 18;
        inputText.alignment = TMPro.TextAlignmentOptions.Left;
        inputText.rectTransform.anchorMin = Vector2.zero;
        inputText.rectTransform.anchorMax = Vector2.one;
        inputText.rectTransform.offsetMin = Vector2.zero;
        inputText.rectTransform.offsetMax = Vector2.zero;
        inputField.textComponent = inputText;
        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
        placeholderGO.transform.SetParent(inputGO.transform, false);
        var placeholderText = placeholderGO.AddComponent<TMPro.TextMeshProUGUI>();
        placeholderText.text = "Type your message here...";
        placeholderText.fontSize = 18;
        placeholderText.alignment = TMPro.TextAlignmentOptions.Left;
        placeholderText.color = new Color(0.7f,0.7f,0.7f,0.7f);
        placeholderText.rectTransform.anchorMin = Vector2.zero;
        placeholderText.rectTransform.anchorMax = Vector2.one;
        placeholderText.rectTransform.offsetMin = Vector2.zero;
        placeholderText.rectTransform.offsetMax = Vector2.zero;
        inputField.placeholder = placeholderText;

        // 4. Conversation Display (Text)
        var convGO = new GameObject("ConversationDisplay", typeof(RectTransform));
        convGO.transform.SetParent(panelGO.transform, false);
        var convRect = convGO.GetComponent<RectTransform>();
        convRect.anchorMin = new Vector2(0f, 0.2f);
        convRect.anchorMax = new Vector2(1f, 0.8f);
        convRect.pivot = new Vector2(0.5f, 0.5f);
        convRect.sizeDelta = new Vector2(-40, -40);
        convRect.anchoredPosition = new Vector2(0, 0);
        var convText = convGO.AddComponent<TMPro.TextMeshProUGUI>();
        convText.text = "OpenAI Realtime NPC Chat\n\nClick 'Connect' to begin...";
        convText.fontSize = 18;
        convText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        convText.color = Color.white;

        // 5. Send Button
        var sendBtnGO = new GameObject("SendMessageButton", typeof(RectTransform));
        sendBtnGO.transform.SetParent(panelGO.transform, false);
        var sendBtnRect = sendBtnGO.GetComponent<RectTransform>();
        sendBtnRect.anchorMin = new Vector2(1f, 0f);
        sendBtnRect.anchorMax = new Vector2(1f, 0f);
        sendBtnRect.pivot = new Vector2(1f, 0f);
        sendBtnRect.sizeDelta = new Vector2(120, 40);
        sendBtnRect.anchoredPosition = new Vector2(-20, 20);
        var sendBtn = sendBtnGO.AddComponent<Button>();
        var sendBtnImg = sendBtnGO.AddComponent<Image>();
        sendBtnImg.color = new Color(0.2f,0.2f,0.5f,1f);
        var sendBtnTextGO = new GameObject("Text", typeof(RectTransform));
        sendBtnTextGO.transform.SetParent(sendBtnGO.transform, false);
        var sendBtnText = sendBtnTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        sendBtnText.text = "Send";
        sendBtnText.fontSize = 20;
        sendBtnText.alignment = TMPro.TextAlignmentOptions.Center;
        sendBtnText.rectTransform.anchorMin = Vector2.zero;
        sendBtnText.rectTransform.anchorMax = Vector2.one;
        sendBtnText.rectTransform.offsetMin = Vector2.zero;
        sendBtnText.rectTransform.offsetMax = Vector2.zero;

        // 6. Status Display
        var statusGO = new GameObject("StatusDisplay", typeof(RectTransform));
        statusGO.transform.SetParent(panelGO.transform, false);
        var statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0.85f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.sizeDelta = new Vector2(-40, 30);
        statusRect.anchoredPosition = new Vector2(0, -150);
        var statusText = statusGO.AddComponent<TMPro.TextMeshProUGUI>();
        statusText.text = "Status: Ready";
        statusText.fontSize = 16;
        statusText.alignment = TMPro.TextAlignmentOptions.Left;
        statusText.color = Color.yellow;
        statusText.raycastTarget = false; // <-- Blockiert keine Klicks mehr!

        // 7. Volume Slider
        var volumeSliderGO = new GameObject("VolumeSlider", typeof(RectTransform));
        volumeSliderGO.transform.SetParent(panelGO.transform, false);
        var volumeSliderRect = volumeSliderGO.GetComponent<RectTransform>();
        volumeSliderRect.anchorMin = new Vector2(1f, 1f);
        volumeSliderRect.anchorMax = new Vector2(1f, 1f);
        volumeSliderRect.pivot = new Vector2(1f, 1f);
        volumeSliderRect.sizeDelta = new Vector2(150, 30);
        volumeSliderRect.anchoredPosition = new Vector2(-20, -20);
        var volumeSlider = volumeSliderGO.AddComponent<Slider>();
        // (Slider visual setup omitted for brevity)

        // 8. VAD Toggle
        var vadToggleGO = new GameObject("EnableVADToggle", typeof(RectTransform));
        vadToggleGO.transform.SetParent(panelGO.transform, false);
        var vadToggleRect = vadToggleGO.GetComponent<RectTransform>();
        vadToggleRect.anchorMin = new Vector2(1f, 1f);
        vadToggleRect.anchorMax = new Vector2(1f, 1f);
        vadToggleRect.pivot = new Vector2(1f, 1f);
        vadToggleRect.sizeDelta = new Vector2(120, 30);
        vadToggleRect.anchoredPosition = new Vector2(-180, -20);
        var vadToggle = vadToggleGO.AddComponent<Toggle>();
        // (Toggle visual setup omitted for brevity)

        // 9. Verlinkung im NpcUiManager
        var uiManager = AddOrGetComponent(panelGO, "Managers.NpcUiManager");
        if (uiManager != null)
        {
            SetField(uiManager, "connectButton", connectBtn);
            SetField(uiManager, "disconnectButton", disconnectBtn);
            SetField(uiManager, "startConversationButton", startConvBtn);
            SetField(uiManager, "stopConversationButton", stopConvBtn);
            SetField(uiManager, "sendMessageButton", sendBtn);
            SetField(uiManager, "messageInputField", inputField);
            SetField(uiManager, "conversationDisplay", convText);
            SetField(uiManager, "statusDisplay", statusText);
            SetField(uiManager, "volumeSlider", volumeSlider);
            SetField(uiManager, "enableVADToggle", vadToggle);

            // Button-Events explizit binden
            BindButtonEvent(connectBtn, uiManager, "OnConnectClicked");
            BindButtonEvent(disconnectBtn, uiManager, "OnDisconnectClicked");
            BindButtonEvent(startConvBtn, uiManager, "OnStartConversationClicked");
            BindButtonEvent(stopConvBtn, uiManager, "OnStopConversationClicked");
            BindButtonEvent(sendBtn, uiManager, "OnSendMessageClicked");
        }

        // --- Hinweis und Direkt-Link zu den Settings ---
        string msg = "Setup abgeschlossen!\n\n" +
            "Bitte prüfe und trage deinen OpenAI API Key ein:\n" +
            $"- OpenAISettings: {AssetDatabase.GetAssetPath(settings)}\n\n" +
            "Du kannst das Settings-Asset jetzt direkt im Inspector öffnen.";

        if (EditorUtility.DisplayDialog("OpenAI NPC Setup", msg, "OpenAISettings anzeigen", "Fertig"))
        {
            Selection.activeObject = settings;
        }
    }

    static Component AddOrGetComponent(GameObject go, string typeName)
    {
        var type = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);
        if (type == null) return null;
        var comp = go.GetComponent(type) ?? go.AddComponent(type);
        return comp;
    }

    static void SetField(Component comp, string field, object value)
    {
        if (comp == null) return;
        var f = comp.GetType().GetField(field, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(comp, value);
    }

    static T EnsureSettingsAsset<T>(string assetName, string path) where T : ScriptableObject
    {
        var asset = Resources.Load<T>(assetName);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }
        return asset;
    }

    static ScriptableObject EnsureSettingsAsset(System.Type type, string assetName, string path)
    {
        var asset = Resources.Load(assetName, type) as ScriptableObject;
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance(type);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }
        return asset;
    }

    // Hilfsmethode für Button-Event-Bindung
    static void BindButtonEvent(Button button, Component target, string methodName)
    {
        if (button == null || target == null) return;
        var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (method != null)
        {
            UnityEngine.Events.UnityAction action = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, method) as UnityEngine.Events.UnityAction;
            if (action != null)
                button.onClick.AddListener(action);
        }
    }
}
