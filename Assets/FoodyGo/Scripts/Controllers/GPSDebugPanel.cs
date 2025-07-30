using UnityEngine;
using UnityEngine.UI;
using packt.FoodyGO.Services;
using System.Collections;

namespace packt.FoodyGO.Controllers
{
    public class GPSDebugPanel : MonoBehaviour
    {
        [Header("References")]
        public GPSLocationService gpsService;
        public CharacterGPSCompassController characterController;
        
        [Header("UI Elements")]
        public GameObject debugPanel;
        public Text latitudeText;
        public Text longitudeText;
        public Text statusText;
        public Text characterPositionText;
        public Text distanceToTargetText;
        public Text targetPositionText;
        
        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.Tab;
        public float updateInterval = 0.5f;
        public bool visibleByDefault = true;
        
        private float nextUpdateTime;
        private bool isVisible = false;
        private float lastToggleTime = 0;
        
        private void Start()
        {
            if (debugPanel == null)
            {
                Debug.LogWarning("Debug panel not assigned, disabling debug script");
                enabled = false;
                return;
            }
            
            // 最初から表示するかどうか
            isVisible = visibleByDefault;
            SetDebugPanelVisibility(isVisible);
            Debug.Log("GPS Debug Panel initialized, visible: " + isVisible);
        }
        
        private void Update()
        {
            // Toggle debug panel with Tab key
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleDebugPanel();
            }
            
            // マルチタッチ検出（3本指タップでデバッグパネルをトグル）
            if (Input.touchCount >= 3)
            {
                bool allTouchesPhaseEnded = true;
                for (int i = 0; i < 3; i++)
                {
                    if (Input.GetTouch(i).phase != TouchPhase.Ended)
                    {
                        allTouchesPhaseEnded = false;
                        break;
                    }
                }
                
                if (allTouchesPhaseEnded)
                {
                    ToggleDebugPanel();
                }
            }
            
            if (isVisible && Time.time > nextUpdateTime)
            {
                UpdateDebugInfo();
                nextUpdateTime = Time.time + updateInterval;
            }
        }
        
        // Public method to toggle the debug panel
        public void ToggleDebugPanel()
        {
            // Prevent rapid toggling
            if (Time.time - lastToggleTime < 0.5f)
            {
                return;
            }
            
            lastToggleTime = Time.time;
            isVisible = !isVisible;
            SetDebugPanelVisibility(isVisible);
            Debug.Log($"Debug panel visibility toggled: {isVisible}");
        }
        
        private void SetDebugPanelVisibility(bool visible)
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(visible);
            }
        }
        
        private void UpdateDebugInfo()
        {
            if (gpsService != null)
            {
                // Update GPS info
                if (latitudeText != null)
                    latitudeText.text = $"Latitude: {gpsService.Latitude:F6}";
                
                if (longitudeText != null)
                    longitudeText.text = $"Longitude: {gpsService.Longitude:F6}";
                
                string status = "GPS Status: ";
                if (gpsService.Simulating)
                    status += "SIMULATION";
                else if (gpsService.IsServiceStarted)
                    status += "ACTIVE";
                else
                    status += "INACTIVE";
                
                if (statusText != null)
                    statusText.text = status;
            }
            else
            {
                if (statusText != null)
                    statusText.text = "GPS Service: NOT FOUND";
            }
            
            if (characterController != null)
            {
                // Update character info
                if (characterPositionText != null)
                {
                    Vector3 pos = characterController.transform.position;
                    characterPositionText.text = $"Position: X:{pos.x:F2}, Y:{pos.y:F2}, Z:{pos.z:F2}";
                }
                
                if (targetPositionText != null)
                {
                    Vector3 target = characterController.Target;
                    targetPositionText.text = $"Target: X:{target.x:F2}, Y:{target.y:F2}, Z:{target.z:F2}";
                }
                
                if (distanceToTargetText != null)
                {
                    Vector3 target = characterController.Target;
                    float distance = Vector3.Distance(characterController.transform.position, target);
                    distanceToTargetText.text = $"Distance to target: {distance:F2} m";
                }
            }
            else
            {
                if (characterPositionText != null)
                    characterPositionText.text = "Character: NOT FOUND";
            }
        }
        
        // Methods to control simulation via UI buttons
        public void ToggleSimulation()
        {
            if (gpsService != null)
            {
                if (gpsService.Simulating)
                {
                    Debug.Log("Attempting to disable simulation");
                    gpsService.Simulating = false;
                    // Use SwitchToSimulation method instead of reflection
                    gpsService.SwitchToSimulation(false);
                }
                else
                {
                    Debug.Log("Enabling simulation mode");
                    gpsService.SwitchToSimulation(true);
                }
                
                UpdateDebugInfo();
            }
            else
            {
                Debug.LogError("GPS service not found, can't toggle simulation");
            }
        }
        
        public void ToggleRandomWalk()
        {
            if (gpsService != null)
            {
                gpsService.RandomSimulation = !gpsService.RandomSimulation;
                Debug.Log($"Random walk simulation: {gpsService.RandomSimulation}");
                UpdateDebugInfo();
            }
        }
        
        public void IncreaseSimulationRate()
        {
            if (gpsService != null)
            {
                gpsService.Rate = Mathf.Max(0.1f, gpsService.Rate * 0.5f);
                Debug.Log($"Simulation rate increased to: {gpsService.Rate}");
                UpdateDebugInfo();
            }
        }
        
        public void DecreaseSimulationRate()
        {
            if (gpsService != null)
            {
                gpsService.Rate = Mathf.Min(10f, gpsService.Rate * 2f);
                Debug.Log($"Simulation rate decreased to: {gpsService.Rate}");
                UpdateDebugInfo();
            }
        }
    }
} 