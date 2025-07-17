using UnityEngine;
using UnityEditor;
using Setup;
using System.IO;
using Diagnostics;
using Setup.Steps;
using NPC;

public class OpenAINPCMenuSetup : EditorWindow
{
    [MenuItem("OpenAI NPC/Quick Setup", false, 0)]
    public static void ShowWindow()
    {
        OpenAINPCMenuSetup window = GetWindow<OpenAINPCMenuSetup>(true, "OpenAI NPC Quick Setup");
        window.position = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 300, 500, 800); // Erweiterte Größe
        window.InitAndCheckAvatar();
        window.CheckLipSyncStatus();
        window.Show();
    }

    private GameObject selectedAvatarPrefab;
    private bool showObjectPicker = false;
    private bool setupStarted = false;
    private string pickerControlName = "AvatarPrefabPicker";
    private GameObject foundAvatarInstance;

    private enum CanvasMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    private Camera selectedCamera = null;
    private float worldCanvasScale = 0.01f;
    private string openAIApiKey = "";
    private ScriptableObject openAISettingsAsset;

    // LipSync Status
    private LipSyncStatus lipSyncStatus;
    private bool lipSyncStatusChecked = false;
    private bool showLipSyncDetails = false;
    private Vector2 scrollPosition;

    private enum LipSyncStatus
    {
        ULipSyncInstalled,
        ULipSyncPartial,
        ULipSyncMissing,
        UnknownError
    }

    private void InitAndCheckAvatar()
    {
        foundAvatarInstance = FindReadyPlayerMeAvatar();
        openAISettingsAsset = Resources.Load<ScriptableObject>("OpenAISettings");

        // Check existing API key
        if (openAISettingsAsset != null)
        {
            var so = new SerializedObject(openAISettingsAsset);
            var keyProp = so.FindProperty("apiKey");
            if (keyProp != null && !string.IsNullOrEmpty(keyProp.stringValue))
            {
                openAIApiKey = "[EXISTING KEY FOUND]"; // Don't show actual key for security
                Debug.Log("[OpenAI NPC Setup] Existing API key found in OpenAISettings.");
            }
        }

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

    private void CheckLipSyncStatus()
    {
        lipSyncStatus = DetectLipSyncSystem();
        lipSyncStatusChecked = true;
    }

    private LipSyncStatus DetectLipSyncSystem()
    {
        // Enhanced detection method (same as SetupLipSyncSystemStep)
        System.Type uLipSyncType = null;
        System.Type blendShapeType = null;

        // Search all loaded assemblies for uLipSync types
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;
            try
            {
                var t1 = assembly.GetType("uLipSync.uLipSync", false);
                var t2 = assembly.GetType("uLipSync.uLipSyncBlendShape", false);

                if (uLipSyncType == null && t1 != null) uLipSyncType = t1;
                if (blendShapeType == null && t2 != null) blendShapeType = t2;

                if (uLipSyncType != null && blendShapeType != null) break;
            }
            catch { /* ignore assembly load/type errors */ }
        }

        // Both types found = fully installed
        if (uLipSyncType != null && blendShapeType != null)
        {
            return LipSyncStatus.ULipSyncInstalled;
        }

        // Only one type found = partial (shouldn't happen normally)
        if (uLipSyncType != null || blendShapeType != null)
        {
            return LipSyncStatus.ULipSyncPartial;
        }

        // Check for uLipSync assemblies (legacy fallback)
        string[] assemblyNames = { "uLipSync.Runtime", "uLipSync" };
        foreach (string assemblyName in assemblyNames)
        {
            try
            {
                var assembly = System.Reflection.Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    return LipSyncStatus.ULipSyncPartial;
                }
            }
            catch { /* Assembly not found */ }
        }

        // Check for uLipSync directories
        string[] searchPaths = {
            "Assets/uLipSync",
            "Assets/Plugins/uLipSync",
            "Packages/com.hecomi.ulipsync"
        };

        foreach (string path in searchPaths)
        {
            if (Directory.Exists(path))
            {
                return LipSyncStatus.ULipSyncPartial;
            }
        }

        return LipSyncStatus.ULipSyncMissing;
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        DrawHeader();

        GUILayout.Space(10);

        // LipSync Status Section
        DrawLipSyncStatusSection();

        GUILayout.Space(15);

        // Main Setup Section
        DrawMainSetupSection();

        GUILayout.Space(15);

        // Action Buttons
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("OpenAI NPC Quick Setup", EditorStyles.largeLabel);
        EditorGUILayout.LabelField("Complete setup for AI-powered NPCs with facial animation", EditorStyles.helpBox);
    }

    private void DrawLipSyncStatusSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Header mit Status
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🎭 Facial Animation System", EditorStyles.boldLabel);

        if (lipSyncStatusChecked)
        {
            switch (lipSyncStatus)
            {
                case LipSyncStatus.ULipSyncInstalled:
                    EditorGUILayout.LabelField("✅ Professional", GetStatusStyle(Color.green));
                    break;
                case LipSyncStatus.ULipSyncPartial:
                    EditorGUILayout.LabelField("⚠️ Incomplete", GetStatusStyle(Color.yellow));
                    break;
                case LipSyncStatus.ULipSyncMissing:
                    EditorGUILayout.LabelField("🔄 Basic", GetStatusStyle(Color.cyan));
                    break;
                default:
                    EditorGUILayout.LabelField("❓ Unknown", GetStatusStyle(Color.gray));
                    break;
            }
        }
        else
        {
            EditorGUILayout.LabelField("Checking...", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();

        // Status Details
        if (lipSyncStatusChecked)
        {
            DrawLipSyncStatusDetails();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawLipSyncStatusDetails()
    {
        GUILayout.Space(5);

        switch (lipSyncStatus)
        {
            case LipSyncStatus.ULipSyncInstalled:
                DrawULipSyncInstalledUI();
                break;

            case LipSyncStatus.ULipSyncPartial:
                DrawULipSyncPartialUI();
                break;

            case LipSyncStatus.ULipSyncMissing:
                DrawULipSyncMissingUI();
                break;

            default:
                DrawULipSyncErrorUI();
                break;
        }
    }

    private void DrawULipSyncInstalledUI()
    {
        EditorGUILayout.HelpBox(
            "🎉 uLipSync Professional System Detected!\n" +
            "• MFCC-based phoneme analysis\n" +
            "• Advanced BlendShape mapping\n" +
            "• Calibration support for optimal results\n" +
            "• Zero-latency real-time processing",
            MessageType.Info
        );

        EditorGUILayout.LabelField("Your setup will use:", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  → Professional-grade facial animation", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  → Accurate phoneme detection", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  → Optimized for OpenAI voices", EditorStyles.miniLabel);
    }

    private void DrawULipSyncPartialUI()
    {
        EditorGUILayout.HelpBox(
            "⚠️ uLipSync Partially Detected\n" +
            "Installation appears incomplete. Some components may be missing.",
            MessageType.Warning
        );

        if (GUILayout.Button("🔄 Re-check uLipSync Status"))
        {
            CheckLipSyncStatus();
        }

        showLipSyncDetails = EditorGUILayout.Foldout(showLipSyncDetails, "Show Installation Guide");
        if (showLipSyncDetails)
        {
            DrawULipSyncInstallationGuide();
        }
    }

    private void DrawULipSyncMissingUI()
    {
        EditorGUILayout.HelpBox(
            "🔄 Basic LipSync Active\n" +
            "Your NPC will have basic mouth movement based on audio levels.\n" +
            "For professional facial animation, install uLipSync (optional).",
            MessageType.None
        );

        showLipSyncDetails = EditorGUILayout.Foldout(showLipSyncDetails, "💎 Upgrade to Professional Animation");
        if (showLipSyncDetails)
        {
            DrawULipSyncUpgradeUI();
        }
    }

    private void DrawULipSyncErrorUI()
    {
        EditorGUILayout.HelpBox(
            "❓ Cannot determine LipSync status.\n" +
            "Setup will proceed with fallback system.",
            MessageType.Warning
        );

        if (GUILayout.Button("🔄 Re-check Status"))
        {
            CheckLipSyncStatus();
        }
    }

    private void DrawULipSyncUpgradeUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("🌟 Professional Upgrade Benefits:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  • Realistic phoneme-based lip movement", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  • Accurate mouth shapes for speech sounds", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  • Calibration for different voices", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  • Production-ready quality", EditorStyles.miniLabel);

        GUILayout.Space(10);
        DrawULipSyncInstallationGuide();

        EditorGUILayout.EndVertical();
    }

    private void DrawULipSyncInstallationGuide()
    {
        EditorGUILayout.LabelField("📦 Installation Options:", EditorStyles.boldLabel);

        // Method 1: Package Manager (Recommended)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🚀 Method 1: Package Manager (Recommended)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Window → Package Manager", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("2. Click '+' → Add package from git URL", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("3. Enter: https://github.com/hecomi/uLipSync.git#upm", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("4. Click 'Add'", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Copy Git URL"))
        {
            EditorGUIUtility.systemCopyBuffer = "https://github.com/hecomi/uLipSync.git#upm";
            Debug.Log("[Setup] uLipSync Git URL copied to clipboard!");
        }
        if (GUILayout.Button("📦 Open Package Manager"))
        {
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Method 2: Unity Package
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📁 Method 2: Unity Package", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Download .unitypackage from GitHub releases", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("2. Import into Unity", EditorStyles.miniLabel);

        if (GUILayout.Button("🌐 Open uLipSync Releases"))
        {
            Application.OpenURL("https://github.com/hecomi/uLipSync/releases");
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "💡 After installation: Re-run this setup for automatic integration!",
            MessageType.Info
        );
    }

    private void DrawMainSetupSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚙️ Setup Configuration", EditorStyles.boldLabel);

        GUILayout.Space(5);

        // Avatar Prefab Auswahl
        EditorGUILayout.LabelField("👤 Avatar Prefab:", EditorStyles.boldLabel);
        if (GUILayout.Button(selectedAvatarPrefab != null ?
            $"Selected: {selectedAvatarPrefab.name}" :
            "Choose Avatar Prefab",
            GUILayout.Height(30)))
        {
            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:Prefab", pickerControlName.GetHashCode());
            Debug.Log("[OpenAI NPC Setup] ObjectPicker opened.");
        }

        string commandName = Event.current.commandName;
        if (commandName == "ObjectSelectorUpdated")
        {
            selectedAvatarPrefab = EditorGUIUtility.GetObjectPickerObject() as GameObject;
            Debug.Log($"[OpenAI NPC Setup] ObjectPicker selection: {(selectedAvatarPrefab != null ? selectedAvatarPrefab.name : "null")}");
            Repaint();
        }

        GUILayout.Space(10);

        // XR Canvas Information (WorldSpace forced for VR)
        EditorGUILayout.LabelField("🥽 XR Canvas Mode:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("For VR/XR applications, Canvas mode is automatically set to World Space. This is required for proper XR interaction.", MessageType.Info);
        
        // Always show world canvas scale since we're always in WorldSpace
        worldCanvasScale = EditorGUILayout.FloatField("World Canvas Scale", worldCanvasScale);
        EditorGUILayout.LabelField("(Recommended: 0.01 for VR)", EditorStyles.miniLabel);

        GUILayout.Space(10);

        // OpenAI API Key
        EditorGUILayout.LabelField("🔑 OpenAI Configuration:", EditorStyles.boldLabel);

        if (openAIApiKey == "[EXISTING KEY FOUND]")
        {
            EditorGUILayout.HelpBox("✅ API Key already configured in OpenAISettings", MessageType.Info);
            EditorGUILayout.LabelField("API Key (optional override):", EditorStyles.miniLabel);
            string newKey = EditorGUILayout.PasswordField("", "");
            if (!string.IsNullOrEmpty(newKey))
            {
                openAIApiKey = newKey;
            }
        }
        else
        {
            openAIApiKey = EditorGUILayout.PasswordField("API Key (optional):", openAIApiKey);
        }

        if (openAISettingsAsset != null)
        {
            EditorGUILayout.HelpBox($"✅ Found OpenAISettings: {openAISettingsAsset.name}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("ℹ️ No OpenAISettings found. A new one will be created.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Setup Status Preview
        DrawSetupPreview();

        GUILayout.Space(10);

        // Main Action Buttons
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = selectedAvatarPrefab != null;
        if (GUILayout.Button("🚀 Start Complete Setup", GUILayout.Height(40)))
        {
            showObjectPicker = false;
            // Prefab in Szene instanziieren
            GameObject avatarInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedAvatarPrefab);
            avatarInstance.name = selectedAvatarPrefab.name;
            // Register as 'CustomAvatar' for UI logic
            Setup.AvatarManager.Instance.RegisterAvatar("CustomAvatar", avatarInstance);
            Undo.RegisterCreatedObjectUndo(avatarInstance, "Create Avatar from Picker");
            Debug.Log($"[OpenAI NPC Setup] Avatar Prefab instantiated: {avatarInstance.name}");

            // Animation Controller hinzufügen
            AddAnimationControllerToAvatar(avatarInstance);

            // OpenAISettings API Key setzen falls angegeben
            if (!string.IsNullOrEmpty(openAIApiKey) && openAIApiKey != "[EXISTING KEY FOUND]" && openAISettingsAsset != null)
            {
                var so = new SerializedObject(openAISettingsAsset);
                var keyProp = so.FindProperty("apiKey");
                if (keyProp != null)
                {
                    keyProp.stringValue = openAIApiKey;
                    so.ApplyModifiedProperties();
                    Debug.Log("[OpenAI NPC Setup] OpenAI API Key updated in Settings asset.");
                }
            }
            else if (openAIApiKey == "[EXISTING KEY FOUND]")
            {
                Debug.Log("[OpenAI NPC Setup] Using existing API Key from OpenAISettings.");
            }

            RunFullSetup(avatarInstance);
        }
        GUI.enabled = true;

        if (GUILayout.Button("❌ Cancel", GUILayout.Height(40)))
        {
            showObjectPicker = false;
            setupStarted = false;
            Debug.Log("[OpenAI NPC Setup] Setup cancelled by user.");
            Close();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        
        // Note: Audio Tools are available in the menu: "OpenAI NPC/Audio Tools/"
        EditorGUILayout.HelpBox("💡 Audio troubleshooting tools are available in the menu:\n" +
                               "• OpenAI NPC/Audio Tools/Audio Quick Fix\n" +
                               "• OpenAI NPC/Audio Tools/Audio Diagnostics", 
                               MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    private void DrawSetupPreview()
    {
        EditorGUILayout.LabelField("📋 Setup Summary:", EditorStyles.boldLabel);

        string avatarStatus = selectedAvatarPrefab != null ?
            $"✅ {selectedAvatarPrefab.name}" :
            "❌ No avatar selected";
        EditorGUILayout.LabelField($"  Avatar: {avatarStatus}", EditorStyles.miniLabel);

        string lipSyncDescription = GetLipSyncDescription();
        EditorGUILayout.LabelField($"  LipSync: {lipSyncDescription}", EditorStyles.miniLabel);

        string canvasDescription = "✅ World Space (XR Mode)";
        EditorGUILayout.LabelField($"  UI Mode: {canvasDescription}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Canvas Scale: {worldCanvasScale}", EditorStyles.miniLabel);

        string apiKeyStatus;
        if (openAIApiKey == "[EXISTING KEY FOUND]")
        {
            apiKeyStatus = "✅ Already configured";
        }
        else if (!string.IsNullOrEmpty(openAIApiKey))
        {
            apiKeyStatus = "✅ Provided (will override)";
        }
        else
        {
            apiKeyStatus = "⚠️ Not set (can be added later)";
        }
        EditorGUILayout.LabelField($"  API Key: {apiKeyStatus}", EditorStyles.miniLabel);
    }

    private string GetLipSyncDescription()
    {
        switch (lipSyncStatus)
        {
            case LipSyncStatus.ULipSyncInstalled:
                return "✅ uLipSync Professional";
            case LipSyncStatus.ULipSyncPartial:
                return "⚠️ uLipSync Incomplete";
            case LipSyncStatus.ULipSyncMissing:
                return "🔄 Basic (Upgrade Available)";
            default:
                return "❓ Unknown";
        }
    }

    private GUIStyle GetStatusStyle(Color color)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = color;
        return style;
    }

    public void RunFullSetup(GameObject avatar)
    {
        // Initialize UnityMainThreadDispatcher early to prevent threading issues
        OpenAI.Threading.UnityMainThreadDispatcher.Initialize();
        Debug.Log("[OpenAI NPC Setup] UnityMainThreadDispatcher initialized successfully.");
        
        var openAISettings = Resources.Load<ScriptableObject>("OpenAISettings");
        var uiPanelSize = new Vector2(600, 350); // Matching MainDemo 15.unity
        var uiPanelPosition = new Vector2(0, 0);
        bool allValid = false;

        Debug.Log($"[OpenAI NPC Setup] Starting full setup with LipSync status: {lipSyncStatus}");

        OpenAINPCSetupUtility.ExecuteFullSetup(
            openAISettings,
            avatar,
            uiPanelSize,
            uiPanelPosition,
            msg => Debug.Log($"[OpenAI NPC Setup] {msg}"),
            valid => allValid = valid,
            // Erweiterte Optionen an SetupUtility übergeben
            new
            {
                canvasMode = "WorldSpace", // Always WorldSpace for XR
                camera = selectedCamera,
                worldCanvasScale = worldCanvasScale,
                lipSyncStatus = lipSyncStatus.ToString(),
                canvasSize = new Vector2(1920, 1080), // MainDemo 15.unity canvas size
                canvasPosition = new Vector3(0, 5.95f, 3), // MainDemo 15.unity canvas position
                canvasRotation = Vector3.zero,
                canvasScale = new Vector3(0.01f, 0.01f, 0.01f) // MainDemo 15.unity canvas scale
            }
        );

        if (allValid)
        {
            string message = lipSyncStatus == LipSyncStatus.ULipSyncInstalled ?
                "🎉 XR Setup complete with Professional LipSync!\n\n" +
                "💡 Next steps:\n" +
                "• Configure your OpenAI API key\n" +
                "• Test the NPC conversation in VR\n" +
                "• Calibrate uLipSync for optimal results\n" +
                "• Add XR Ray Interactors to your hands/controllers" :
                "✅ XR Setup complete with Basic LipSync!\n\n" +
                "💡 Next steps:\n" +
                "• Configure your OpenAI API key\n" +
                "• Test the NPC conversation in VR\n" +
                "• Add XR Ray Interactors to your hands/controllers\n" +
                "• Optional: Install uLipSync for enhanced facial animation";

            EditorUtility.DisplayDialog("OpenAI NPC Setup Complete", message, "Got it!");
        }
        else
        {
            EditorUtility.DisplayDialog("OpenAI NPC Setup", "Setup completed with warnings. Check Console for details.", "OK");
        }

        Debug.Log("[OpenAI NPC Setup] Quick Setup completed. See Console for details.");
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

    private static void AddAnimationControllerToAvatar(GameObject avatar)
    {
        // Check if avatar already has an Animator component
        Animator animator = avatar.GetComponent<Animator>();
        if (animator == null)
        {
            animator = avatar.AddComponent<Animator>();
            Debug.Log($"[OpenAI NPC Setup] Added Animator component to {avatar.name}");
        }

        // Load RPM Animation Controller
        RuntimeAnimatorController controller = LoadRPMAnimationController();

        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
            Debug.Log($"[OpenAI NPC Setup] Added Animation Controller to {avatar.name}: {controller.name}");
        }
        else
        {
            Debug.LogWarning($"[OpenAI NPC Setup] No Animation Controller found for {avatar.name}");
        }
    }

    private static RuntimeAnimatorController LoadRPMAnimationController()
    {
        string[] possiblePaths = {
            "Assets/Ready Player Me/Core/Samples/AvatarCreatorSamples/AvatarCreatorElements/Animation/AnimationController.controller",
            "Assets/Ready Player Me/Core/Samples/QuickStart/Animations/RpmPlayer.controller",
            "Assets/Plugins/ReadyPlayerMe/Resources/Animations/RpmPlayer.controller"
        };

        foreach (string path in possiblePaths)
        {
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            if (controller != null)
            {
                Debug.Log($"[OpenAI NPC Setup] Found Animation Controller: {path}");
                return controller;
            }
        }

        Debug.LogWarning("[OpenAI NPC Setup] No RPM Animation Controller found in project");
        return null;
    }

}
