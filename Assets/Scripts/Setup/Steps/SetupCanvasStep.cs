using UnityEngine;
using UnityEngine.UI;

namespace Setup.Steps
{
    /// <summary>
    /// Step for creating and configuring the main UI Canvas
    /// </summary>
    public class SetupCanvasStep
    {
        public Canvas Canvas { get; private set; }

        public void Execute()
        {
            GameObject canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("Canvas");
            }
            // Sicherstellen, dass die Canvas-Komponente existiert
            var canvas = canvasGO.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasGO.AddComponent<Canvas>();
            }
            Canvas = canvas;
            if (canvasGO.GetComponent<CanvasScaler>() == null) canvasGO.AddComponent<CanvasScaler>();
            if (canvasGO.GetComponent<GraphicRaycaster>() == null) canvasGO.AddComponent<GraphicRaycaster>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Set RectTransform for Canvas
            var canvasRect = canvasGO.GetComponent<RectTransform>() ?? canvasGO.AddComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
        }
    }
}
