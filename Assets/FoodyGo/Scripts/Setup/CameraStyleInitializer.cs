using UnityEngine;
using StarterAssets;
using packt.FoodyGO.Controllers;
using Cinemachine;

namespace packt.FoodyGO.Setup
{
    /// <summary>
    /// ランタイム時にポケモンGOスタイルのカメラ設定を適用するコンポーネント
    /// </summary>
    public class CameraStyleInitializer : MonoBehaviour
    {
        [Header("Camera Style")]
        [Tooltip("ポケモンGOスタイルのカメラ設定を使用")]
        public bool usePokemonGoStyle = true;
        
        [Header("Pokemon GO Style Camera Settings")]
        public float cameraHeight = 6.0f;
        public float cameraDistance = 12.0f;
        public float cameraFieldOfView = 50.0f;
        public float cameraAngle = 60.0f;
        
        [Header("Camera Clamp Settings")]
        public float topClamp = 89.0f;
        public float bottomClamp = 0.0f;
        
        [Header("3rd Person Follow Settings")]
        [Tooltip("TopRigの高さ")]
        public float topRigHeight = 6.0f;
        [Tooltip("TopRigの距離")]
        public float topRigRadius = 15.0f;
        
        [Tooltip("MiddleRigの高さ")]
        public float middleRigHeight = 5.0f;
        [Tooltip("MiddleRigの距離")]
        public float middleRigRadius = 10.0f;
        
        [Tooltip("BottomRigの高さ")]
        public float bottomRigHeight = 2.0f;
        [Tooltip("BottomRigの距離")]
        public float bottomRigRadius = 6.0f;
        
        [Tooltip("スプラインの曲率")]
        [Range(0f, 1f)]
        public float splineCurvature = 0.5f;
        
        private void Start()
        {
            if (usePokemonGoStyle)
            {
                Debug.Log("Initializing Pokemon GO style camera settings...");
                ApplyPokemonGoStyleCamera();
            }
        }
        
        public void ApplyPokemonGoStyleCamera()
        {
            // 1. ThirdPersonControllerを検索して設定を適用
            var controllers = FindObjectsOfType<ThirdPersonController>();
            foreach (var controller in controllers)
            {
                controller.TopClamp = topClamp;
                controller.BottomClamp = bottomClamp;
                controller.CameraAngleOverride = cameraAngle;
                
                Debug.Log($"Applied camera angle settings to {controller.name}");
                
                // CameraTargetの位置調整
                if (controller.CinemachineCameraTarget != null)
                {
                    var cameraRoot = controller.CinemachineCameraTarget.transform;
                    cameraRoot.localPosition = new Vector3(0, 2.5f, -1.0f);
                    Debug.Log($"Adjusted camera root position for {controller.name}");
                }
            }
            
            // 2. CharacterGPSCompassControllerを検索して設定を適用
            var gpsControllers = FindObjectsOfType<CharacterGPSCompassController>();
            foreach (var gpsController in gpsControllers)
            {
                gpsController.pokemonStyleCameraHeight = cameraHeight;
                gpsController.pokemonStyleCameraDistance = cameraDistance;
                gpsController.pokemonStyleCameraAngle = cameraAngle;
                
                Debug.Log($"Applied Pokemon GO style settings to {gpsController.name}");
            }
            
            // 3. CinemachineVirtualCameraを検索して設定を適用
            var virtualCameras = FindObjectsOfType<CinemachineVirtualCamera>();
            foreach (var virtualCamera in virtualCameras)
            {
                // カメラの位置を調整
                virtualCamera.transform.position = new Vector3(0.5f, cameraHeight, -cameraDistance);
                
                // レンズ設定を適用
                virtualCamera.m_Lens.FieldOfView = cameraFieldOfView;
                virtualCamera.m_Lens.FarClipPlane = 1000;
                
                // Transposer設定
                var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_FollowOffset = new Vector3(0.3f, cameraHeight, -cameraDistance);
                    Debug.Log($"Applied transposer settings to {virtualCamera.name}");
                }
                
                // OrbitalTransposerの設定
                var orbitalTransposer = virtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                if (orbitalTransposer != null)
                {
                    orbitalTransposer.m_FollowOffset = new Vector3(0, cameraHeight, -cameraDistance);
                    orbitalTransposer.m_XAxis.Value = 0; // 初期ヨー角
                    Debug.Log($"Applied orbital transposer settings to {virtualCamera.name}");
                }
                
                // 3rdPersonFollowの設定
                var thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if (thirdPersonFollow != null)
                {
                    // TopRig
                    thirdPersonFollow.ShoulderOffset = new Vector3(0.3f, 0, 0);
                    thirdPersonFollow.VerticalArmLength = 2.5f;
                    thirdPersonFollow.CameraDistance = cameraDistance;
                    
                    // インスペクタで見えるRig設定を反映
                    SetRigSettings(thirdPersonFollow);
                    
                    Debug.Log($"Applied 3rd person follow settings to {virtualCamera.name}");
                }
                
                // カメラの初期角度設定
                var aim = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
                if (aim != null)
                {
                    aim.m_VerticalAxis.Value = cameraAngle;
                    Debug.Log($"Applied POV settings to {virtualCamera.name}");
                }
            }
            
            Debug.Log("Pokemon GO style camera setup complete!");
        }
        
        // 3rdPersonFollowのRig設定を適用するヘルパーメソッド
        private void SetRigSettings(Cinemachine3rdPersonFollow thirdPersonFollow)
        {
            try
            {
                // リフレクションを使用してプライベートフィールドにアクセス
                var rigType = typeof(Cinemachine3rdPersonFollow);
                
                // TopRig
                var topRigField = rigType.GetField("m_TopRig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (topRigField != null)
                {
                    var topRig = topRigField.GetValue(thirdPersonFollow);
                    var heightField = topRig.GetType().GetField("m_Height");
                    var radiusField = topRig.GetType().GetField("m_Radius");
                    
                    if (heightField != null) heightField.SetValue(topRig, topRigHeight);
                    if (radiusField != null) radiusField.SetValue(topRig, topRigRadius);
                }
                
                // MiddleRig
                var middleRigField = rigType.GetField("m_MiddleRig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (middleRigField != null)
                {
                    var middleRig = middleRigField.GetValue(thirdPersonFollow);
                    var heightField = middleRig.GetType().GetField("m_Height");
                    var radiusField = middleRig.GetType().GetField("m_Radius");
                    
                    if (heightField != null) heightField.SetValue(middleRig, middleRigHeight);
                    if (radiusField != null) radiusField.SetValue(middleRig, middleRigRadius);
                }
                
                // BottomRig
                var bottomRigField = rigType.GetField("m_BottomRig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (bottomRigField != null)
                {
                    var bottomRig = bottomRigField.GetValue(thirdPersonFollow);
                    var heightField = bottomRig.GetType().GetField("m_Height");
                    var radiusField = bottomRig.GetType().GetField("m_Radius");
                    
                    if (heightField != null) heightField.SetValue(bottomRig, bottomRigHeight);
                    if (radiusField != null) radiusField.SetValue(bottomRig, bottomRigRadius);
                }
                
                // スプラインの曲率
                var splineCurvatureField = rigType.GetField("m_SplineCurvature", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (splineCurvatureField != null)
                {
                    splineCurvatureField.SetValue(thirdPersonFollow, splineCurvature);
                }
                
                Debug.Log("Successfully applied all 3rd person follow rig settings");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error applying 3rd person follow settings: {e.Message}");
            }
        }
    }
} 