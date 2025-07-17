using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Diagnostics;

/// <summary>
/// Canvas picker window for selecting which Canvas to work with
/// </summary>
public class CanvasPickerWindow : EditorWindow
{
    private Canvas[] canvases;
    public Canvas selectedCanvas;
    private Vector2 scrollPosition;
    private int selectedIndex = -1;
    private bool isWaitingForSelection = true;

    public void SetCanvases(Canvas[] canvases)
    {
        this.canvases = canvases;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Select Canvas", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (canvases == null || canvases.Length == 0)
        {
            EditorGUILayout.LabelField("No canvases available");
            return;
        }

        EditorGUILayout.LabelField($"Found {canvases.Length} Canvas objects in scene:", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Canvas selection toggle
            bool isSelected = (selectedIndex == i);

            EditorGUILayout.BeginHorizontal();

            // Selection button
            if (isSelected) GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button(isSelected ? "✅ Selected" : "Select", GUILayout.Width(80)))
            {
                selectedIndex = i;
                selectedCanvas = canvas;
                Selection.activeGameObject = canvas.gameObject;
            }
            GUI.backgroundColor = Color.white;

            // Canvas name
            EditorGUILayout.LabelField($"🖼️ {canvas.name}", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();

            // Detailed info
            EditorGUILayout.LabelField($"Mode: {canvas.renderMode}");
            EditorGUILayout.LabelField($"Position: {canvas.transform.position}");
            EditorGUILayout.LabelField($"Scale: {canvas.transform.localScale}");

            var distance = Vector3.Distance(Vector3.zero, canvas.transform.position);
            EditorGUILayout.LabelField($"Distance from Origin: {distance:F2} units");

            // Issue indicators
            if (canvas.transform.localScale.x <= 0.01f)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("⚠️ Scale very small - VR interaction issue!", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            if (distance > 10f)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("⚠️ Too far from player - may be unreachable!", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("⚠️ Not WorldSpace - VR needs WorldSpace!", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = selectedCanvas != null;
        if (GUILayout.Button("✅ Use Selected Canvas", GUILayout.Height(30)))
        {
            isWaitingForSelection = false;
            Close();
        }
        GUI.enabled = true;

        if (GUILayout.Button("❌ Cancel", GUILayout.Height(30)))
        {
            selectedCanvas = null;
            isWaitingForSelection = false;
            Close();
        }

        EditorGUILayout.EndHorizontal();

        if (selectedCanvas != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Selected: {selectedCanvas.name}", EditorStyles.centeredGreyMiniLabel);
        }
    }

    void OnDestroy()
    {
        isWaitingForSelection = false;
    }
}