using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Diagnostics;

/// <summary>
/// Canvas Tools for VR Canvas interaction diagnostics and fixes
/// </summary>
public static class CanvasTools
{
    // =============================================
    // Canvas Tools Section
    // =============================================

    [MenuItem("OpenAI NPC/Canvas Tools/Fix Canvas for VR", false, 200)]
    public static void RunCanvasFixer()
    {
        Debug.Log("[OpenAI NPC] Running Canvas VR Fix...");
            
        // Find all canvases in scene
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases.Length == 0)
        {
            EditorUtility.DisplayDialog("Canvas VR Fix", 
                "No Canvas found in current scene.\n\n" +
                "Please add a Canvas to the scene first.", 
                "OK");
            return;
        }

        Canvas selectedCanvas = null;

        // If multiple canvases, show object picker
        if (canvases.Length > 1)
        {
            selectedCanvas = ShowCanvasPickerDialog(canvases, "Select Canvas to Fix for VR");
            if (selectedCanvas == null) return; // User cancelled
        }
        else
        {
            // Single canvas - fix it directly
            selectedCanvas = canvases[0];
        }

        RunCanvasFixForSelected(selectedCanvas);
    }

    [MenuItem("OpenAI NPC/Canvas Tools/VR Canvas Diagnostics", false, 201)]
    public static void RunCanvasDiagnostics()
    {
        Debug.Log("[OpenAI NPC] Running VR Canvas Diagnostics...");
            
        // Find all canvases in scene
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases.Length == 0)
        {
            EditorUtility.DisplayDialog("VR Canvas Diagnostics", 
                "No Canvas found in current scene.\n\n" +
                "Please add a Canvas to the scene first.", 
                "OK");
            return;
        }

        Canvas selectedCanvas = null;

        // If multiple canvases, show object picker
        if (canvases.Length > 1)
        {
            selectedCanvas = ShowCanvasPickerDialog(canvases, "Select Canvas to Diagnose");
            if (selectedCanvas == null) return; // User cancelled
        }
        else
        {
            // Single canvas - diagnose it directly
            selectedCanvas = canvases[0];
        }

        RunCanvasDiagnosticsForSelected(selectedCanvas);
    }

    // =============================================
    // Helper Methods
    // =============================================

    /// <summary>
    /// Shows a Canvas picker dialog with detailed information
    /// </summary>
    private static Canvas ShowCanvasPickerDialog(Canvas[] canvases, string title)
    {
        if (canvases.Length == 1)
        {
            return canvases[0];
        }

        // For multiple canvases, show the picker window directly
        CanvasPickerWindow window = EditorWindow.GetWindow<CanvasPickerWindow>(true, title);
        window.SetCanvases(canvases);
        window.minSize = new Vector2(450, 350);
        window.maxSize = new Vector2(700, 600);
        window.position = new Rect(
            (Screen.currentResolution.width - 450) / 2,
            (Screen.currentResolution.height - 350) / 2,
            450, 350
        );
        window.ShowUtility();
            
        return window.selectedCanvas;
    }

    private static void RunCanvasFixForSelected(Canvas selectedCanvas)
    {
        // Create or find FixCanvasForVR component
        var canvasFixer = selectedCanvas.GetComponent<Diagnostics.FixCanvasForVR>();
        if (canvasFixer == null)
        {
            canvasFixer = Undo.AddComponent<Diagnostics.FixCanvasForVR>(selectedCanvas.gameObject);
            Debug.Log($"[Canvas Tools] Added FixCanvasForVR component to {selectedCanvas.name}");
        }

        // Run the fix
        canvasFixer.FixCanvasConfiguration();
            
        EditorUtility.DisplayDialog("Canvas VR Fix", 
            $"✅ Canvas VR Fix Complete for: {selectedCanvas.name}\n\n" +
            "• Canvas position and scale adjusted for VR\n" +
            "• XR Ray Interactors configured\n" +
            "• UI interaction optimized\n\n" +
            "Check Console for detailed results.", 
            "OK");
    }

    private static void RunCanvasDiagnosticsForSelected(Canvas selectedCanvas)
    {
        // Create or find VRCanvasDiagnostics component
        var canvasDiagnostics = selectedCanvas.GetComponent<Diagnostics.VRCanvasDiagnostics>();
        if (canvasDiagnostics == null)
        {
            canvasDiagnostics = Undo.AddComponent<Diagnostics.VRCanvasDiagnostics>(selectedCanvas.gameObject);
            Debug.Log($"[Canvas Tools] Added VRCanvasDiagnostics component to {selectedCanvas.name}");
        }

        // Run diagnostics
        canvasDiagnostics.RunFullDiagnostics();
            
        EditorUtility.DisplayDialog("VR Canvas Diagnostics", 
            $"🔍 VR Canvas Diagnostics Complete for: {selectedCanvas.name}\n\n" +
            "• Canvas configuration analyzed\n" +
            "• XR Ray Interactor setup checked\n" +
            "• UI interaction validated\n" +
            "• Common VR issues detected\n\n" +
            "See Console for detailed results.", 
            "OK");
    }
}