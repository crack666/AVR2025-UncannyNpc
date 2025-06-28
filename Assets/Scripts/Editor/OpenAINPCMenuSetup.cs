using UnityEngine;
using UnityEditor;
using Setup;

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
        // Suche Settings und Avatar automatisch
        var openAISettings = Resources.Load<ScriptableObject>("OpenAISettings");
        var avatar = FindReadyPlayerMeAvatar();
        var uiPanelSize = new Vector2(800, 400); // Breiteres Panel als Default
        var uiPanelPosition = new Vector2(0, 0);

        // Wenn kein Avatar gefunden, dynamisch nach Prefabs suchen
        if (avatar == null)
        {
            // 1. Suche nach Prefabs in Avatars-Ordner
            string[] avatarPrefabPaths = System.IO.Directory.GetFiles(
                "Assets/Ready Player Me/Avatars", "*.prefab", System.IO.SearchOption.AllDirectories);
            GameObject prefab = null;
            if (avatarPrefabPaths.Length == 1)
            {
                string assetPath = avatarPrefabPaths[0];
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
            else if (avatarPrefabPaths.Length > 1)
            {
                // Mehrere Prefabs gefunden: Auswahl-Dialog
                string[] prefabNames = new string[avatarPrefabPaths.Length];
                for (int i = 0; i < avatarPrefabPaths.Length; i++)
                    prefabNames[i] = System.IO.Path.GetFileNameWithoutExtension(avatarPrefabPaths[i]);
                int selected = EditorUtility.DisplayDialogComplex(
                    "Avatar Prefab Auswahl",
                    "Es wurden mehrere Avatar-Prefabs gefunden. Bitte wähle eines aus:",
                    prefabNames[0],
                    prefabNames.Length > 1 ? prefabNames[1] : "Abbrechen",
                    "Abbrechen");
                if (selected >= 0 && selected < prefabNames.Length)
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(avatarPrefabPaths[selected]);
            }
            // 2. Fallback: PreviewAvatar.prefab
            if (prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Ready Player Me/Core/Samples/QuickStart/PreviewAvatar/PreviewAvatar.prefab");
            }
            // 3. Wenn gefunden, instanziieren
            if (prefab != null)
            {
                avatar = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                avatar.name = "DefaultAvatar";
                Undo.RegisterCreatedObjectUndo(avatar, "Create Default Avatar");
                Debug.Log($"[OpenAI NPC Setup] Kein Avatar gefunden, Default-Prefab instanziiert: {prefab.name}");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Kein Avatar gefunden",
                    "Es wurde kein ReadyPlayerMe Avatar gefunden und kein Default-Prefab konnte geladen werden. Bitte installiere einen Avatar manuell oder starte den Avatar Loader.",
                    "OK");
                // Setup trotzdem fortsetzen, aber Warnung ausgeben
            }
        }

        // Setup synchron ausführen
        bool allValid = false;
        OpenAINPCSetupUtility.ExecuteFullSetup(
            openAISettings,
            avatar,
            uiPanelSize,
            uiPanelPosition,
            msg => Debug.Log($"[OpenAI NPC Setup] {msg}"),
            valid => allValid = valid
        );

        if (allValid)
            EditorUtility.DisplayDialog("OpenAI NPC Setup", "Setup erfolgreich abgeschlossen!", "OK");
        else
            EditorUtility.DisplayDialog("OpenAI NPC Setup", "Setup abgeschlossen, aber mit Warnungen. Siehe Konsole.", "OK");

        Debug.Log("[OpenAI NPC Setup] Quick Setup abgeschlossen. Siehe Konsole für Details.");
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
