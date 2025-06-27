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
        var npcController = AddOrGetComponent(npcSystem, "NPCController");
        var playbackAudio = AddOrGetComponent(npcSystem, "AudioSource");
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

        // 7. UI Canvas + Panel + Buttons erstellen (NEU: Steps-Logik)
        var uiStep = new Setup.Steps.CreateUISystemStep();
        uiStep.Execute(new Vector2(600, 300), new Vector2(0, 40));

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
