using UnityEngine;
using StarterAssets;
using packt.FoodyGO.Mapping;
using packt.FoodyGO.Services;

namespace packt.FoodyGO.Controllers
{
    public class CharacterGPSCompassController : MonoBehaviour
    {
        public GPSLocationService gpsLocationService;
        private double lastTimestamp;        
        private StarterAssets.ThirdPersonController thirdPersonController;
        private StarterAssetsInputs starterAssetsInputs;
        private Vector3 target;
        
        [Header("Camera Settings")]
        [Tooltip("Pokémon GO style camera height")]
        public float pokemonStyleCameraHeight = 12f;
        [Tooltip("Pokémon GO style camera distance")]
        public float pokemonStyleCameraDistance = 12f;
        [Tooltip("Pokémon GO style camera angle")]
        public float pokemonStyleCameraAngle = 60f;
        
        [Header("Movement Settings")]
        [Tooltip("Enable GPS-based movement")]
        public bool enableGPSMovement = false;
        [Tooltip("Amplify GPS movement by this factor")]
        public float movementAmplification = 3.0f;
        [Tooltip("Minimum movement speed in m/s")]
        public float guaranteedMovementSpeed = 0.5f;
        [Tooltip("Always animate when moving")]
        public bool alwaysAnimateWhenMoving = true;
        [Tooltip("Minimum animation speed")]
        public float minimumAnimationSpeed = 0.1f;
        [Tooltip("Speed change rate")]
        public float speedChangeRate = 10.0f;
        [Tooltip("Minimum distance to move")]
        public float minDistanceToMove = 0.025f;
        
        // Public property for target position
        public Vector3 Target => target;
        
        // Public property to check if character is moving
        public bool IsMoving => Vector3.Distance(target, transform.position) > minDistanceToMove;
        
        // Use this for initialization
        void Start()
        {
            Input.compass.enabled = true;
            thirdPersonController = GetComponent<ThirdPersonController>();
            starterAssetsInputs = GetComponent<StarterAssetsInputs>();
            
            Debug.Log($"[GPS_CONTROLLER] 初期化完了 - ThirdPersonController:{thirdPersonController != null}, Input:{starterAssetsInputs != null}");
            
            if (gpsLocationService != null)
            {
                gpsLocationService.OnMapRedraw += GpsLocationService_OnMapRedraw;
            }
        }

        private void GpsLocationService_OnMapRedraw(GameObject g)
        {
            // Only reset position if GPS movement is enabled AND we're using GPS positioning
            // This prevents sudden warping during map redraws
            if (enableGPSMovement && gpsLocationService != null && gpsLocationService.IsServiceStarted)
            {
                // Smooth transition instead of immediate reset
                var currentGPSPosition = ConvertGPSToWorldSpace(gpsLocationService.Longitude, gpsLocationService.Latitude);
                target = currentGPSPosition;
                // Don't immediately reset transform.position - let Update() handle smooth movement
            }
        }
        
        private Vector3 ConvertGPSToWorldSpace(double longitude, double latitude)
        {
            if (gpsLocationService == null) return transform.position;
            
            var x = ((GoogleMapUtils.LonToX((float)longitude)
                - gpsLocationService.mapWorldCenter.x) * gpsLocationService.mapScale.x);
            var y = (GoogleMapUtils.LatToY((float)latitude)
                - gpsLocationService.mapWorldCenter.y) * gpsLocationService.mapScale.y;
            return new Vector3(-x, 0, y);
        }

        // Update is called once per frame
        void Update()
        {
            // GPS移動が無効の場合は、GPS処理を完全にスキップ
            if (!enableGPSMovement)
            {
                Debug.Log($"[GPS_CONTROLLER] GPS移動無効 - コンパス処理のみ実行");
                
                // Orient an object to point to magnetic north and adjust for map reversal
                var heading = 180 + Input.compass.magneticHeading;
                var rotation = Quaternion.AngleAxis(heading, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.fixedTime * .001f);
                return;
            }
            
            // GPS移動が有効な場合のみGPS処理を実行
            if (gpsLocationService != null &&
                gpsLocationService.IsServiceStarted &&
                gpsLocationService.PlayerTimestamp > lastTimestamp)
            {
                //convert GPS lat/long to world x/y 
                var x = ((GoogleMapUtils.LonToX(gpsLocationService.Longitude)
                    - gpsLocationService.mapWorldCenter.x) * gpsLocationService.mapScale.x);
                var y = (GoogleMapUtils.LatToY(gpsLocationService.Latitude)
                    - gpsLocationService.mapWorldCenter.y) * gpsLocationService.mapScale.y;
                target = new Vector3(-x, 0, y);                
            }

            //check if the character has reached the new point
            if (Vector3.Distance(target, transform.position) > minDistanceToMove)
            {
                var move = target - transform.position;
                // ThirdPersonControllerの入力システムを使用
                if (starterAssetsInputs != null && thirdPersonController != null)
                {
                    move.y = 0; // 水平方向のみ
                    Vector3 moveDirection = move.normalized;
                    
                    // カメラの向きに対する相対的な移動方向を計算
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        Vector3 cameraForward = mainCamera.transform.forward;
                        Vector3 cameraRight = mainCamera.transform.right;
                        cameraForward.y = 0;
                        cameraRight.y = 0;
                        cameraForward.Normalize();
                        cameraRight.Normalize();
                        
                        // ワールド座標での移動方向をカメラ相対座標に変換
                        float forwardAmount = Vector3.Dot(moveDirection, cameraForward);
                        float rightAmount = Vector3.Dot(moveDirection, cameraRight);
                        
                        // 移動速度を調整
                        float speedMultiplier = Mathf.Min(movementAmplification, move.magnitude / minDistanceToMove);
                        
                        // StarterAssetsInputsに入力を設定
                        starterAssetsInputs.move = new Vector2(rightAmount, forwardAmount) * speedMultiplier;
                    }
                    
                    Debug.Log($"[GPS_CONTROLLER] GPS移動入力設定 - Target:{target}, Move:{starterAssetsInputs.move}");
                }
            }
            else
            {
                // 目標地点に到達したら入力をクリア
                if (starterAssetsInputs != null)
                {
                    starterAssetsInputs.move = Vector2.zero;
                }
                
                // Orient an object to point to magnetic north and adjust for map reversal
                var heading = 180 + Input.compass.magneticHeading;
                var rotation = Quaternion.AngleAxis(heading, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.fixedTime * .001f);
            }
        }
    }
}
