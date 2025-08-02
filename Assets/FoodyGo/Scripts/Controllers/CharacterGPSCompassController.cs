using UnityEngine;
using packt.FoodyGO.Mapping;
using packt.FoodyGO.Services;

namespace packt.FoodyGO.Controllers
{
    public class CharacterGPSCompassController : MonoBehaviour
    {
        public GPSLocationService gpsLocationService;
        private double lastTimestamp;        
        private InputCoordinator inputCoordinator;
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
            // 🔥 PC環境対応：モバイルデバイスでのみコンパスを有効化
            if (Application.isMobilePlatform)
            {
                Input.compass.enabled = true;
                Debug.Log($"[GPS_CONTROLLER] モバイル環境でコンパス有効化");
            }
            else
            {
                Debug.Log($"[GPS_CONTROLLER] PC環境のためコンパス無効化");
            }
            
            inputCoordinator = GetComponent<InputCoordinator>();
            
            Debug.Log($"[GPS_CONTROLLER] 初期化完了 - InputCoordinator:{inputCoordinator != null}");
            
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
            // GPS移動が無効の場合は、入力システムとコンパス回転に一切干渉しない
            if (!enableGPSMovement)
            {
                // 🔥 修正：GPS無効時はコンパス回転も無効にして、プレイヤー入力による回転に任せる
                Debug.Log($"[GPS_CONTROLLER] GPS移動無効 - 入力システムに完全に任せる");
                return; // 重要：GPS無効時は一切の処理をしない
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
                // InputCoordinatorを使用した新物理システム対応
                if (inputCoordinator != null)
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
                        
                        // InputCoordinatorに直接入力を設定
                        inputCoordinator.move = new Vector2(rightAmount, forwardAmount) * speedMultiplier;
                    }
                    
                    Debug.Log($"[GPS_CONTROLLER] GPS移動入力設定 - Target:{target}, Move:{inputCoordinator.move}");
                }
            }
            else
            {
                // GPS移動が有効な場合のみ入力をクリア（重要な変更）
                if (inputCoordinator != null)
                {
                    inputCoordinator.move = Vector2.zero;
                }
                
                // 🔥 モバイル環境でのみコンパス回転を実行
                if (Application.isMobilePlatform)
                {
                    var heading = 180 + Input.compass.magneticHeading;
                    var rotation = Quaternion.AngleAxis(heading, Vector3.up);
                    
                    // 🔥 完全Rigidbodyベース：物理エンジン経由での回転
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Rigidbody.MoveRotation使用（物理的に正しい）
                        Quaternion targetRotation = Quaternion.Slerp(rb.rotation, rotation, Time.deltaTime * 0.5f);
                        rb.MoveRotation(targetRotation);
                        Debug.Log($"[GPS_PHYSICS] Rigidbody回転使用（完全物理準拠） - Heading:{heading}");
                    }
                    else
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 0.5f);
                    }
                }
                else
                {
                    Debug.Log($"[GPS_CONTROLLER] PC環境のためコンパス回転をスキップ");
                }
            }
        }
    }
}
