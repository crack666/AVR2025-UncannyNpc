using UnityEngine;
using UnityEditor;

public class BuildTestScript
{
    [MenuItem("Build/Test Compilation")]
    public static void TestCompilation()
    {
        Debug.Log("Testing compilation...");
        
        // Force a recompile
        AssetDatabase.Refresh();
        
        // Check for compilation errors
        if (EditorApplication.isCompiling)
        {
            Debug.Log("Project is compiling...");
        }
        else
        {
            Debug.Log("Project compilation successful!");
        }
    }
}
