using UnityEngine;
using UnityEditor;
using Setup;

public class OpenAINPCMenuSetup : EditorWindow
{
    [MenuItem("OpenAI NPC/Quick Setup", false, 0)]
    public static void ShowWindow()
    {
        OpenAINPCMenuSetup window = GetWindow<OpenAINPCMenuSetup>(true, "OpenAI NPC Quick Setup");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 180);
        window.InitAndCheckAvatar();
        window.Show();
    }

    private GameObject selectedAvatarPrefab;
    private bool showObjectPicker = false;
    private bool setupStarted = false;
    private string pickerControlName = "AvatarPrefabPicker";
    private GameObject foundAvatarInstance;

    private enum CanvasMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    private CanvasMode selectedCanvasMode = CanvasMode.ScreenSpaceOverlay;
    private Camera selectedCamera = null;
    private float worldCanvasScale = 0.01f;
    private string openAIApiKey = "";
    private ScriptableObject openAISettingsAsset;

    private void InitAndCheckAvatar()
    {
        foundAvatarInstance = FindReadyPlayerMeAvatar();
        openAISettingsAsset = Resources.Load<ScriptableObject>("OpenAISettings");
        if (foundAvatarInstance == null)
        {
            showObjectPicker = true;
            Debug.Log("[OpenAI NPC Setup] Kein Avatar in Szene gefunden. ObjectPicker wird angezeigt.");
        }
        else
        {
            Debug.Log($"[OpenAI NPC Setup] Avatar in Szene gefunden: {foundAvatarInstance.name}");
            showObjectPicker = true; // Zeige trotzdem die erweiterten Optionen
        }
    }

    void OnGUI()
    {
        GUILayout.Label("OpenAI NPC Quick Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("Wähle ein Avatar-Prefab, Canvas-Modus und (optional) OpenAI API Key:", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        // Avatar Prefab Auswahl
        GUILayout.Label("Avatar Prefab:");
        if (GUILayout.Button(selectedAvatarPrefab != null ? $"Ausgewählt: {selectedAvatarPrefab.name}" : "Avatar Prefab auswählen"))
        {
            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:Prefab", pickerControlName.GetHashCode());
            Debug.Log("[OpenAI NPC Setup] ObjectPicker geöffnet.");
        }
        string commandName = Event.current.commandName;
        if (commandName == "ObjectSelectorUpdated")
        {
            selectedAvatarPrefab = EditorGUIUtility.GetObjectPickerObject() as GameObject;
            Debug.Log($"[OpenAI NPC Setup] ObjectPicker Auswahl: {(selectedAvatarPrefab != null ? selectedAvatarPrefab.name : "null")}");
            Repaint();
        }

        GUILayout.Space(10);
        // Canvas Modus Auswahl
        GUILayout.Label("Canvas Modus:");
        selectedCanvasMode = (CanvasMode)EditorGUILayout.EnumPopup(selectedCanvasMode);
        if (selectedCanvasMode == CanvasMode.ScreenSpaceCamera)
        {
            selectedCamera = (Camera)EditorGUILayout.ObjectField("Kamera", selectedCamera, typeof(Camera), true);
        }
        if (selectedCanvasMode == CanvasMode.WorldSpace)
        {
            worldCanvasScale = EditorGUILayout.FloatField("World Canvas Scale", worldCanvasScale);
        }

        GUILayout.Space(10);
        // OpenAI API Key
        GUILayout.Label("OpenAI API Key (optional):");
        openAIApiKey = EditorGUILayout.TextField(openAIApiKey);
        if (openAISettingsAsset != null)
        {
            GUILayout.Label($"Gefundenes OpenAISettings Asset: {openAISettingsAsset.name}");
        }
        else
        {
            GUILayout.Label("Kein OpenAISettings Asset gefunden. Es wird ein neues erstellt.");
        }

        GUILayout.Space(10);
        // Setup starten
        GUI.enabled = selectedAvatarPrefab != null;
        if (GUILayout.Button("Setup starten"))
        {
            showObjectPicker = false;
            // Prefab in Szene instanziieren
            GameObject avatarInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedAvatarPrefab);
            avatarInstance.name = selectedAvatarPrefab.name;
            Undo.RegisterCreatedObjectUndo(avatarInstance, "Create Avatar from Picker");
            Debug.Log($"[OpenAI NPC Setup] Avatar-Prefab instanziiert: {avatarInstance.name}");
            // OpenAISettings ggf. API Key setzen
            if (!string.IsNullOrEmpty(openAIApiKey) && openAISettingsAsset != null)
            {
                var so = new SerializedObject(openAISettingsAsset);
                var keyProp = so.FindProperty("apiKey");
                if (keyProp != null)
                {
                    keyProp.stringValue = openAIApiKey;
                    so.ApplyModifiedProperties();
                    Debug.Log("[OpenAI NPC Setup] OpenAI API Key im Settings Asset gesetzt.");
                }
            }
            RunFullSetup(avatarInstance);
        }
        GUI.enabled = true;
        if (GUILayout.Button("Abbrechen"))
        {
            showObjectPicker = false;
            setupStarted = false;
            Debug.Log("[OpenAI NPC Setup] Setup abgebrochen durch Benutzer.");
            Close();
        }
    }

    public void RunFullSetup(GameObject avatar)
    {
        var openAISettings = Resources.Load<ScriptableObject>("OpenAISettings");
        var uiPanelSize = new Vector2(800, 400);
        var uiPanelPosition = new Vector2(0, 0);
        bool allValid = false;
        OpenAINPCSetupUtility.ExecuteFullSetup(
            openAISettings,
            avatar,
            uiPanelSize,
            uiPanelPosition,
            msg => Debug.Log($"[OpenAI NPC Setup] {msg}"),
            valid => allValid = valid,
            // Erweiterte Optionen an SetupUtility übergeben
            new { canvasMode = selectedCanvasMode.ToString(), camera = selectedCamera, worldCanvasScale = worldCanvasScale }
        );
        if (allValid)
            EditorUtility.DisplayDialog("OpenAI NPC Setup", "Setup erfolgreich abgeschlossen!", "OK");
        else
            EditorUtility.DisplayDialog("OpenAI NPC Setup", "Setup abgeschlossen, aber mit Warnungen. Siehe Konsole.", "OK");
        Debug.Log("[OpenAI NPC Setup] Quick Setup abgeschlossen. Siehe Konsole für Details.");
        Close();
    }

    private static GameObject FindReadyPlayerMeAvatar()
    {
        var renderers = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in renderers)
        {
            if (renderer.name.Contains("Wolf3D") ||
                renderer.name.ToLower().Contains("head") ||
                (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 10))
            {
                Transform current = renderer.transform;
                while (current.parent != null && !current.name.ToLower().Contains("avatar") && !current.name.ToLower().Contains("readyplayerme"))
                {
                    current = current.parent;
                }
                return current.gameObject;
            }
        }
        return null;
    }
}
