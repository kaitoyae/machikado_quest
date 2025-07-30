using UnityEngine;
using packt.FoodyGO.Controllers;
using packt.FoodyGO.Services;

namespace packt.FoodyGO.Setup
{
    /// <summary>
    /// Main game manager script, responsible for initializing and coordinating game systems
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("GPS Settings")]
        public bool enableGpsDebug = true;
        
        [Header("Camera Settings")]
        public bool usePokemonGoStyleCamera = true;
        
        // Reference to main scene components
        private GPSLocationService gpsService;
        private CharacterGPSCompassController characterController;
        private GPSDebugSetup debugSetup;
        private CameraStyleInitializer cameraInitializer;
        
        void Awake()
        {
            Debug.Log("GameManager initializing...");
            
            // Find required components
            gpsService = FindObjectOfType<GPSLocationService>();
            characterController = FindObjectOfType<CharacterGPSCompassController>();
            
            if (gpsService == null)
            {
                Debug.LogError("GPSLocationService not found in scene!");
            }
            
            if (characterController == null)
            {
                Debug.LogError("CharacterGPSCompassController not found in scene!");
            }
            
            // Make sure they're connected
            if (gpsService != null && characterController != null)
            {
                characterController.gpsLocationService = gpsService;
            }
            
            // Set up debug UI if needed
            if (enableGpsDebug)
            {
                SetupDebugUI();
            }
            
            // Set up camera style if needed
            if (usePokemonGoStyleCamera)
            {
                SetupCameraStyle();
            }
            
            Debug.Log("GameManager initialization complete");
        }
        
        private void SetupDebugUI()
        {
            // Check if debug setup already exists
            debugSetup = FindObjectOfType<GPSDebugSetup>();
            
            if (debugSetup == null)
            {
                GameObject debugObj = new GameObject("GPS Debug Setup");
                debugSetup = debugObj.AddComponent<GPSDebugSetup>();
                debugSetup.gpsService = gpsService;
                debugSetup.characterController = characterController;
                debugSetup.enableDebugMode = true;
                
                Debug.Log("Debug UI automatically created");
            }
            else
            {
                // Make sure references are assigned
                debugSetup.gpsService = gpsService;
                debugSetup.characterController = characterController;
                debugSetup.enableDebugMode = true;
                
                Debug.Log("Using existing debug UI");
            }
        }
        
        private void SetupCameraStyle()
        {
            // カメラスタイル設定コンポーネントを探す
            cameraInitializer = FindObjectOfType<CameraStyleInitializer>();
            
            if (cameraInitializer == null)
            {
                // コンポーネントがなければ作成
                GameObject cameraStyleObj = new GameObject("Camera Style Initializer");
                cameraInitializer = cameraStyleObj.AddComponent<CameraStyleInitializer>();
                cameraInitializer.usePokemonGoStyle = true;
                
                Debug.Log("Camera style initializer created");
            }
            else
            {
                // 既存のコンポーネントを使用
                cameraInitializer.usePokemonGoStyle = true;
                Debug.Log("Using existing camera style initializer");
            }
            
            // 初期化処理を実行
            cameraInitializer.ApplyPokemonGoStyleCamera();
        }
        
        // Helper method to toggle simulation mode (can be called from UI)
        public void ToggleSimulationMode()
        {
            if (gpsService != null)
            {
                gpsService.SwitchToSimulation(!gpsService.Simulating);
            }
        }
    }
} 