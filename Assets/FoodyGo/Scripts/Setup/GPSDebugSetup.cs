using UnityEngine;
using packt.FoodyGO.Controllers;
using packt.FoodyGO.Services;
using packt.FoodyGO.UI;

namespace packt.FoodyGO.Setup
{
    /// <summary>
    /// Automatically sets up the GPS debug UI and connects the relevant components
    /// </summary>
    public class GPSDebugSetup : MonoBehaviour
    {
        [Header("GPS Debug Settings")]
        public bool enableDebugMode = true;
        public bool visibleByDefault = true;
        
        [Header("References")]
        public GPSLocationService gpsService;
        public CharacterGPSCompassController characterController;
        
        private GPSDebugPanel debugPanel;
        private GPSDebugUI debugUI;
        
        void Awake()
        {
            // On mobile, always enable debug mode in development builds
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && DEBUG
            enableDebugMode = true;
#endif

            if (!enableDebugMode)
                return;
            
            Debug.Log("GPS Debug Setup initializing...");
                
            // Find GPS service if not assigned
            if (gpsService == null)
            {
                gpsService = FindObjectOfType<GPSLocationService>();
                Debug.Log("Found GPS service: " + (gpsService != null ? "Yes" : "No"));
            }
            
            // Find character controller if not assigned
            if (characterController == null)
            {
                characterController = FindObjectOfType<CharacterGPSCompassController>();
                Debug.Log("Found character controller: " + (characterController != null ? "Yes" : "No"));
            }
            
            // Create debug UI
            GameObject uiObject = new GameObject("GPSDebugUIController");
            uiObject.transform.SetParent(transform);
            
            debugUI = uiObject.AddComponent<GPSDebugUI>();
            debugUI.Setup();
            
            // Create debug panel controller
            debugPanel = uiObject.AddComponent<GPSDebugPanel>();
            debugPanel.gpsService = gpsService;
            debugPanel.characterController = characterController;
            debugPanel.visibleByDefault = visibleByDefault;
            
            // Connect UI elements to debug panel
            debugPanel.debugPanel = debugUI.transform.GetChild(0).gameObject;
            debugPanel.latitudeText = debugUI.latitudeText;
            debugPanel.longitudeText = debugUI.longitudeText;
            debugPanel.statusText = debugUI.statusText;
            debugPanel.characterPositionText = debugUI.characterPositionText;
            debugPanel.targetPositionText = debugUI.targetPositionText;
            debugPanel.distanceToTargetText = debugUI.distanceToTargetText;
            
            // Connect button events
            ConnectButtonEvents();
            
            Debug.Log("GPS Debug UI setup complete");
            
            // Add a message to show how to toggle the debug panel
            CreateInitialInstructions();
        }
        
        private void CreateInitialInstructions()
        {
            GameObject initialInstructionsObj = new GameObject("InitialInstructions");
            initialInstructionsObj.transform.SetParent(debugUI.transform);
            
            RectTransform rectTransform = initialInstructionsObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.anchoredPosition = new Vector2(0, 100);
            rectTransform.sizeDelta = new Vector2(800, 100);
            
            UnityEngine.UI.Text instructions = initialInstructionsObj.AddComponent<UnityEngine.UI.Text>();
            instructions.text = "タブキーまたは3本指タップでデバッグパネルを表示/非表示";
            instructions.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            instructions.fontSize = 24;
            instructions.alignment = TextAnchor.MiddleCenter;
            instructions.color = Color.white;
            
            // Auto-hide after 5 seconds if debug panel is not visible
            if (!visibleByDefault)
            {
                Destroy(initialInstructionsObj, 5f);
            }
        }
        
        private void ConnectButtonEvents()
        {
            if (debugUI == null)
            {
                Debug.LogError("Debug UI is null, can't connect button events");
                return;
            }
            
            // Find all buttons in the debug panel and connect their events
            Transform panelTransform = debugUI.transform.GetChild(0);
            
            for (int i = 0; i < panelTransform.childCount; i++)
            {
                Transform child = panelTransform.GetChild(i);
                if (child.name.Contains("Button"))
                {
                    UnityEngine.UI.Button button = child.GetComponent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        if (child.name.Contains("Toggle Simulation"))
                        {
                            button.onClick.AddListener(debugPanel.ToggleSimulation);
                            Debug.Log("Connected Toggle Simulation button");
                        }
                        else if (child.name.Contains("Toggle Random"))
                        {
                            button.onClick.AddListener(debugPanel.ToggleRandomWalk);
                            Debug.Log("Connected Toggle Random Walk button");
                        }
                        else if (child.name.Contains("Speed Up"))
                        {
                            button.onClick.AddListener(debugPanel.IncreaseSimulationRate);
                            Debug.Log("Connected Speed Up button");
                        }
                        else if (child.name.Contains("Slow Down"))
                        {
                            button.onClick.AddListener(debugPanel.DecreaseSimulationRate);
                            Debug.Log("Connected Slow Down button");
                        }
                    }
                }
            }
        }
    }
} 