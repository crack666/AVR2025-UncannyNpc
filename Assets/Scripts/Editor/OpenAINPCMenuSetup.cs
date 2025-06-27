using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

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

        // 7. UI Canvas + Panel + Buttons erstellen
        var canvas = Object.FindFirstObjectByType<Canvas>() ?? new GameObject("Canvas").AddComponent<Canvas>();
        if (canvas.GetComponent<CanvasScaler>() == null) canvas.gameObject.AddComponent<CanvasScaler>();
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var panel = GameObject.Find("NPC UI Panel") ?? new GameObject("NPC UI Panel");
        panel.transform.SetParent(canvas.transform, false);
        if (panel.GetComponent<Image>() == null) panel.AddComponent<Image>().color = new Color(0.1f,0.1f,0.1f,0.8f);
        var uiManager = AddOrGetComponent(panel, "Managers.NPCUIManager");
        // Buttons, Text, InputField etc. können hier analog wie im QuickSetup erzeugt und referenziert werden

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
}
