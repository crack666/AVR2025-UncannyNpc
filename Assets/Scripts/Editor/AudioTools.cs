using UnityEngine;
using UnityEditor;
using Setup.Steps;
using Diagnostics;
using NPC;


namespace Setup.Tools
{
    /// <summary>
    /// Simple test to verify avatar prefab loading
    /// </summary>
    public class AudioTools : EditorWindow
    {
       
        // Separator
        [MenuItem("OpenAI NPC/Audio Tools/Audio Quick Fix", false, 100)]
        public static void RunAudioQuickFix()
        {
            Debug.Log("[OpenAI NPC] Running Audio Quick Fix...");
            
            // Find RealtimeAudioManager in current scene
            var audioManager = Object.FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            if (audioManager == null)
            {
                EditorUtility.DisplayDialog("Audio Quick Fix", 
                    "No RealtimeAudioManager found in current scene.\n\nPlease open a scene with an OpenAI NPC setup.", 
                    "OK");
                return;
            }

            try
            {
                // Run AudioQuickFixStep
                var quickFixStep = new AudioQuickFixStep(msg => Debug.Log($"[Audio Quick Fix] {msg}"));
                quickFixStep.ExecuteSync(null, audioManager.gameObject);

                EditorUtility.DisplayDialog("Audio Quick Fix", 
                    "✅ Audio settings optimized!\n\n" +
                    "• Unity audio settings configured\n" +
                    "• Buffer size optimized for stability\n" +
                    "• Diagnostic components added\n\n" +
                    "Check Console for detailed log.", 
                    "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Audio Quick Fix] Error: {ex.Message}");
                EditorUtility.DisplayDialog("Audio Quick Fix", 
                    "❌ Audio Quick Fix encountered issues.\n\nCheck Console for details.", 
                    "OK");
            }
        }

        [MenuItem("OpenAI NPC/Audio Tools/Audio Diagnostics", false, 101)]
        public static void RunAudioDiagnostics()
        {
            Debug.Log("[OpenAI NPC] Running Audio Diagnostics...");
            
            // Find AudioDiagnostics in current scene
            var audioDiagnostics = Object.FindFirstObjectByType<AudioDiagnostics>();
            if (audioDiagnostics == null)
            {
                // Try to find RealtimeAudioManager and add component
                var audioManager = Object.FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
                if (audioManager != null)
                {
                    audioDiagnostics = Undo.AddComponent<AudioDiagnostics>(audioManager.gameObject);
                    Debug.Log($"[Audio Tools] Added AudioDiagnostics component to {audioManager.name}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Audio Diagnostics", 
                        "No AudioDiagnostics component found and no RealtimeAudioManager found in current scene.\n\n" +
                        "Please open a scene with an OpenAI NPC setup first.", 
                        "OK");
                    return;
                }
            }

            // Run diagnostics - the component logs results to Console
            audioDiagnostics.RunFullDiagnostics();
            
            EditorUtility.DisplayDialog("Audio Diagnostics", 
                "🔍 Audio Diagnostics Complete!\n\n" +
                "• System audio capabilities checked\n" +
                "• Microphone devices analyzed\n" +
                "• Unity audio settings validated\n" +
                "• Audio drivers tested\n\n" +
                "See Console for detailed results.", 
                "OK");
        }

        [MenuItem("OpenAI NPC/Audio Tools/Audio Troubleshooting Guide", false, 102)]
        public static void OpenAudioTroubleshootingGuide()
        {
            string troubleshootingPath = "Assets/AUDIO_TROUBLESHOOTING.md";
            if (System.IO.File.Exists(troubleshootingPath))
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(troubleshootingPath));
            }
            else
            {
                EditorUtility.DisplayDialog("Audio Troubleshooting Guide", 
                    "AUDIO_TROUBLESHOOTING.md not found.\n\n" +
                    "Expected location: Assets/AUDIO_TROUBLESHOOTING.md", 
                    "OK");
            }
        }

        [MenuItem("OpenAI NPC/Audio Tools/Add Audio Diagnostics to Scene", false, 103)]
        public static void AddAudioDiagnosticsToScene()
        {
            Debug.Log("[OpenAI NPC] Adding AudioDiagnostics to current scene...");
            
            // Check if already exists
            var existing = Object.FindFirstObjectByType<AudioDiagnostics>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Add Audio Diagnostics", 
                    $"AudioDiagnostics already exists on: {existing.gameObject.name}\n\n" +
                    "Use 'Audio Diagnostics' menu item to run diagnostics.", 
                    "OK");
                return;
            }

            // Find suitable GameObject to attach to
            var audioManager = Object.FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            var npcController = Object.FindFirstObjectByType<NPCController>();
            
            GameObject targetObject = null;
            if (audioManager != null)
            {
                targetObject = audioManager.gameObject;
            }
            else if (npcController != null)
            {
                targetObject = npcController.gameObject;
            }
            else
            {
                // Create new GameObject
                targetObject = new GameObject("Audio Diagnostics");
                Undo.RegisterCreatedObjectUndo(targetObject, "Create Audio Diagnostics GameObject");
            }

            // Add AudioDiagnostics component
            var diagnostics = Undo.AddComponent<AudioDiagnostics>(targetObject);
            
            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            
            // Select the object
            Selection.activeGameObject = targetObject;
            
            EditorUtility.DisplayDialog("Add Audio Diagnostics", 
                $"✅ AudioDiagnostics component added to: {targetObject.name}\n\n" +
                "You can now use 'Audio Diagnostics' menu item to run diagnostics.", 
                "OK");
        }
    }
}
