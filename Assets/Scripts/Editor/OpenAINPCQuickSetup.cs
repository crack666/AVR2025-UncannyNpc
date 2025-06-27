using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

#if UNITY_EDITOR
public class OpenAINPCQuickSetup : EditorWindow
{
    [MenuItem("Tools/OpenAI NPC Quick Setup")]
    public static void ShowWindow()
    {
        GetWindow<OpenAINPCQuickSetup>("OpenAI NPC Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("OpenAI NPC Quick Setup", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create all necessary GameObjects for the OpenAI NPC system.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("1. Create OpenAI System GameObject", GUILayout.Height(30)))
        {
            CreateOpenAISystemManual();
        }
        
        if (GUILayout.Button("2. Create UI Canvas", GUILayout.Height(30)))
        {
            CreateUICanvas();
        }
        
        if (GUILayout.Button("3. Add ReadyPlayerMe Avatar (Manual)", GUILayout.Height(30)))
        {
            EditorUtility.DisplayDialog("Manual Step", 
                "1. Open SampleScene\n2. Find GameObject '6806c3522a9c5c70a48cf28e'\n3. Copy it (Ctrl+C)\n4. Return to this scene\n5. Paste it (Ctrl+V)\n6. Set position to (0, 0, 5)", 
                "OK");
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("After creating all objects, you need to manually add the scripts and link references.", MessageType.Warning);
    }

    private void CreateOpenAISystemManual()
    {
        GameObject npcSystem = new GameObject("OpenAI NPC System");
        
        Debug.Log("OpenAI NPC System GameObject created! Now manually add these scripts:\n" +
                 "- RealtimeClient\n" +
                 "- RealtimeAudioManager\n" +
                 "- NPCController\n" +
                 "- NpcUiManager\n" +
                 "- OpenAINPCDebug\n" +
                 "Then assign OpenAIRealtimeSettings.asset to each script.");
        
        Selection.activeGameObject = npcSystem;
    }

    private void CreateUICanvas()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create Debug Text
        GameObject debugTextGO = new GameObject("Debug Text");
        debugTextGO.transform.SetParent(canvasGO.transform);
        Text debugText = debugTextGO.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        debugText.text = "OpenAI NPC System\nStatus: Ready";
        debugText.fontSize = 16;
        debugText.color = Color.white;
        
        RectTransform debugRect = debugText.GetComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0, 0.5f);
        debugRect.anchorMax = new Vector2(0.5f, 1f);
        debugRect.offsetMin = Vector2.zero;
        debugRect.offsetMax = Vector2.zero;
        
        // Create Connect Button
        CreateButton(canvasGO.transform, "Connect Button", new Vector2(0.8f, 0.8f), new Vector2(0.95f, 0.95f), "Connect");
        
        // Create Start Button  
        CreateButton(canvasGO.transform, "Start Conversation Button", new Vector2(0.8f, 0.6f), new Vector2(0.95f, 0.75f), "Start Chat");
        
        Debug.Log("UI Canvas created with Debug Text and Buttons!");
        Selection.activeGameObject = canvasGO;
    }
    
    private void CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string text)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent);
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        
        Button button = buttonGO.AddComponent<Button>();
        
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        // Add text child
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform);
        
        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.text = text;
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}
#endif
