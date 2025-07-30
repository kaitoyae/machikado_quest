using UnityEngine;
using UnityEngine.UI;

namespace packt.FoodyGO.UI
{
    /// <summary>
    /// Helper script to set up the GPS Debug UI
    /// This script is used to create a canvas and all necessary UI elements for debugging GPS in the game
    /// Usage: Attach to an empty GameObject and call Setup() method
    /// </summary>
    public class GPSDebugUI : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject panel;
        
        [Header("UI References")]
        public Text latitudeText;
        public Text longitudeText;
        public Text statusText;
        public Text characterPositionText;
        public Text targetPositionText;
        public Text distanceToTargetText;
        
        public void Setup()
        {
            // Create Canvas
            GameObject canvasGO = new GameObject("GPSDebugCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Add Canvas Scaler
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add Graphics Raycaster
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create Panel
            panel = CreatePanel(canvasGO.transform);
            
            // Create Text Elements
            latitudeText = CreateText(panel.transform, "Latitude", 0);
            longitudeText = CreateText(panel.transform, "Longitude", 1);
            statusText = CreateText(panel.transform, "Status", 2);
            characterPositionText = CreateText(panel.transform, "Character Position", 3);
            targetPositionText = CreateText(panel.transform, "Target Position", 4);
            distanceToTargetText = CreateText(panel.transform, "Distance", 5);
            
            // Create Buttons
            CreateButton(panel.transform, "Toggle Simulation", 6, "ToggleSimulation");
            CreateButton(panel.transform, "Toggle Random Walk", 7, "ToggleRandomWalk");
            CreateButton(panel.transform, "Speed Up", 8, "IncreaseSimulationRate");
            CreateButton(panel.transform, "Slow Down", 9, "DecreaseSimulationRate");
        }
        
        private GameObject CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject("DebugPanel");
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.3f, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.7f);
            
            return panel;
        }
        
        private Text CreateText(Transform parent, string label, int position)
        {
            GameObject textGO = new GameObject(label + "Text");
            textGO.transform.SetParent(parent, false);
            
            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -50 - position * 40);
            rect.sizeDelta = new Vector2(0, 30);
            
            Text text = textGO.AddComponent<Text>();
            text.text = label + ": ...";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            
            // Add shadow
            Shadow shadow = textGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(1, -1);
            
            return text;
        }
        
        private Button CreateButton(Transform parent, string label, int position, string functionName)
        {
            GameObject buttonGO = new GameObject(label + "Button");
            buttonGO.transform.SetParent(parent, false);
            
            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 1);
            rect.anchorMax = new Vector2(0.9f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -50 - position * 40);
            rect.sizeDelta = new Vector2(0, 30);
            
            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1);
            
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1);
            button.colors = colors;
            
            // Create button text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            
            Text text = textGO.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            
            return button;
        }
    }
} 