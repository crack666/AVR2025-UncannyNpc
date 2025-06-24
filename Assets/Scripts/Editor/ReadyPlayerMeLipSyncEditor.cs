using UnityEngine;
using UnityEditor;
using Animation;

[CustomEditor(typeof(ReadyPlayerMeLipSync))]
public class ReadyPlayerMeLipSyncEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ReadyPlayerMeLipSync lipSync = (ReadyPlayerMeLipSync)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto-Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Discover Components"))
        {
            lipSync.AutoDiscoverComponents();
            EditorUtility.SetDirty(lipSync);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click 'Auto-Discover Components' to automatically find:\n" +
            "• Wolf3D_Head SkinnedMeshRenderer\n" +
            "• RealtimeAudioManager in scene\n" +
            "• AudioSource component (fallback)", 
            MessageType.Info
        );
        
        // Show current status
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Component Status", EditorStyles.boldLabel);
        
        // Head Mesh Renderer status
        var headRenderer = lipSync.GetType().GetField("headMeshRenderer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(lipSync) as SkinnedMeshRenderer;
        
        EditorGUILayout.LabelField("Head Mesh Renderer:", 
            headRenderer != null ? $"✅ {headRenderer.name}" : "❌ Not Found");
        
        // RealtimeAudioManager status
        var audioManager = lipSync.GetType().GetField("realtimeAudioManager", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(lipSync) as OpenAI.RealtimeAPI.RealtimeAudioManager;
        
        EditorGUILayout.LabelField("RealtimeAudioManager:", 
            audioManager != null ? $"✅ {audioManager.name}" : "❌ Not Found");
        
        // AudioSource status
        var audioSource = lipSync.GetType().GetField("audioSource", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(lipSync) as AudioSource;
        
        EditorGUILayout.LabelField("AudioSource (Fallback):", 
            audioSource != null ? $"✅ {audioSource.name}" : "⚠️ Not Found (Optional)");
    }
}
