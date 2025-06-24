using UnityEngine;
using UnityEditor;
using Animation;

public class LipSyncTestTool : EditorWindow
{
    private ReadyPlayerMeLipSync lipSyncComponent;
    private float mouthOpenValue = 0f;
    private float mouthSmileValue = 0.1f;
    private bool autoTest = false;
    private float autoTestTime = 0f;
    
    [MenuItem("Tools/ReadyPlayerMe/LipSync Test Tool")]
    public static void ShowWindow()
    {
        GetWindow<LipSyncTestTool>("LipSync Test Tool");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("ReadyPlayerMe LipSync Test Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Component selection
        lipSyncComponent = EditorGUILayout.ObjectField("LipSync Component", lipSyncComponent, typeof(ReadyPlayerMeLipSync), true) as ReadyPlayerMeLipSync;
        
        if (lipSyncComponent == null)
        {
            EditorGUILayout.HelpBox("Please assign a ReadyPlayerMeLipSync component to test.", MessageType.Warning);
            
            if (GUILayout.Button("Find LipSync Component in Scene"))
            {
                lipSyncComponent = FindObjectOfType<ReadyPlayerMeLipSync>();
            }
            return;
        }
        
        EditorGUILayout.Space();
        
        // Auto-discovery button
        if (GUILayout.Button("Auto-Discover Components"))
        {
            lipSyncComponent.AutoDiscoverComponents();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Manual BlendShape Testing", EditorStyles.boldLabel);
        
        // Manual controls
        mouthOpenValue = EditorGUILayout.Slider("Mouth Open", mouthOpenValue, 0f, 1f);
        mouthSmileValue = EditorGUILayout.Slider("Mouth Smile", mouthSmileValue, 0f, 1f);
        
        if (GUILayout.Button("Apply Values"))
        {
            lipSyncComponent.SetMouthOpen(mouthOpenValue);
            lipSyncComponent.SetBaseSmile(mouthSmileValue);
        }
        
        if (GUILayout.Button("Reset Expression"))
        {
            lipSyncComponent.ResetExpression();
            mouthOpenValue = 0f;
            mouthSmileValue = 0.1f;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto Test", EditorStyles.boldLabel);
        
        autoTest = EditorGUILayout.Toggle("Enable Auto Test", autoTest);
        
        if (autoTest)
        {
            EditorGUILayout.HelpBox("Auto test will animate the mouth automatically for testing.", MessageType.Info);
            
            if (Application.isPlaying)
            {
                // Auto test animation
                autoTestTime += Time.deltaTime;
                float animatedMouthOpen = Mathf.Sin(autoTestTime * 2f) * 0.5f + 0.5f; // 0-1 range
                float animatedMouthSmile = 0.1f + Mathf.Sin(autoTestTime * 1.5f) * 0.1f; // 0.0-0.2 range
                
                lipSyncComponent.SetMouthOpen(animatedMouthOpen);
                lipSyncComponent.SetBaseSmile(animatedMouthSmile);
                
                mouthOpenValue = animatedMouthOpen;
                mouthSmileValue = animatedMouthSmile;
                
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Auto test only works in Play Mode.", MessageType.Warning);
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Component Status", EditorStyles.boldLabel);
        
        // Show component status
        if (lipSyncComponent != null)
        {
            bool isActive = lipSyncComponent.IsLipSyncActive();
            EditorGUILayout.LabelField("LipSync Active:", isActive ? "✅ Yes" : "❌ No");
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Refresh"))
        {
            Repaint();
        }
    }
    
    private void Update()
    {
        if (autoTest && Application.isPlaying)
        {
            Repaint();
        }
    }
}
