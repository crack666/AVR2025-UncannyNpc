using UnityEngine;
using UnityEditor;

/// <summary>
/// Tutorial script to help set up the OpenAI NPC Test scene manually in Unity Editor.
/// This avoids crashes from complex scene references.
/// </summary>
public class OpenAINPCSetupTutorial : MonoBehaviour
{
    [Header("SETUP TUTORIAL - READ THIS")]
    [TextArea(10, 20)]
    public string instructions = @"
OPENAI NPC SETUP TUTORIAL:

1. FIND API KEY SETTINGS:
   - Go to Project Window → Assets/Settings/OpenAIRealtimeSettings.asset
   - Click on it and enter your OpenAI API Key in the Inspector

2. ADD READYPLAYERME AVATAR:
   - Open SampleScene first
   - Find GameObject named '6806c3522a9c5c70a48cf28e'
   - Copy it (Ctrl+C)
   - Switch back to OpenAI_NPC_Test scene
   - Paste it (Ctrl+V)
   - Position it at (0, 0, 5)

3. CREATE OPENAI SYSTEM:
   - Create Empty GameObject, name it 'OpenAI NPC System'
   - Add Scripts: RealtimeClient, RealtimeAudioManager, NPCController, NpcUiManager, OpenAINPCDebug
   - Link the OpenAIRealtimeSettings asset to each script

4. CREATE UI:
   - Create Canvas
   - Add Text for Debug output
   - Add Buttons: 'Connect' and 'Start Conversation'
   - Wire Connect button to RealtimeClient.ConnectAsync()
   - Wire Start button to NPCController.StartConversation()

5. ADD AUDIO:
   - Add AudioSource to NPC for voice output
   - Link it to NPCController and RealtimeAudioManager

6. TEST:
   - Press Play
   - Click Connect (check console for connection status)
   - Click Start Conversation
   - Speak to the NPC

TROUBLESHOOTING:
- If Unity crashes, delete Library folder and restart
- Check Console for error messages
- Ensure all script references are properly linked
- Verify OpenAI API key is valid

Need help? Check the broken scene file: OpenAI_NPC_Test_BROKEN.unity
";

#if UNITY_EDITOR
    [MenuItem("OpenAI NPC/Show Setup Tutorial")]
    public static void ShowTutorial()
    {
        EditorUtility.DisplayDialog("OpenAI NPC Setup", 
            "Tutorial script added to scene. Select the 'Setup Tutorial' GameObject in the scene hierarchy to see detailed instructions in the Inspector.", 
            "OK");
    }
#endif
}
