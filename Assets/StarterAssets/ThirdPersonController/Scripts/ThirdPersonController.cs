using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Tooltip("How sensitive the animation is to small movements (lower value = more sensitive)")]
        [Range(0.01f, 1.0f)]
        public float AnimationSensitivity = 0.1f;

        [Tooltip("How smooth the animation transition should be")]
        [Range(1.0f, 20.0f)]
        public float AnimationSmoothness = 5.0f;

        [Tooltip("The minimum speed to trigger walk animation")]
        [Range(0.01f, 1.0f)]
        public float MinimumWalkThreshold = 0.05f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        // 動きの検出用
        private Vector3 _lastPosition;
        private float _movementMagnitude;
        private float _lastMovementTime;
        private bool _wasMoving;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private Rigidbody _rigidbody;
        private CapsuleCollider _capsuleCollider;
        
        // Unity 6対応: CharacterController機能をRigidbodyで再現
        private Vector3 _velocity;
        private bool _isGroundedInternal;
        
        // ARCHITECTURAL SOLUTION: Lifecycle management
        private bool _isDestroying = false;
        private bool _isQuitting = false;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        
        // Unity 6対応: CharacterController互換性プロパティ
        public Vector3 velocity => _velocity;
        public bool isGrounded => _isGroundedInternal;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private void Awake()
        {
            Debug.Log($"[AWAKE] ===== Awake開始 on {gameObject.name} =====");
            Debug.Log($"[AWAKE] GameObject Active: {gameObject.activeInHierarchy}, Component Enabled: {enabled}");
            
            // ARCHITECTURAL SOLUTION: Subscribe to scene management events
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
                Debug.Log($"[AWAKE] MainCamera found: {_mainCamera?.name}");
            }
            
            // Unity 6対応: Rigidbody + CapsuleColliderの完璧な設定
            _rigidbody = GetComponent<Rigidbody>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
            
            // CharacterControllerが存在する場合は無効化
            try 
            {
                CharacterController oldController = GetComponent<CharacterController>();
                if (oldController != null)
                {
                    Debug.Log($"[AWAKE] CharacterControllerを無効化してRigidbodyベースに移行");
                    
                    // CharacterControllerの設定を保存
                    float height = oldController.height;
                    float radius = oldController.radius;
                    Vector3 center = oldController.center;
                    
                    // CharacterControllerを無効化
                    oldController.enabled = false;
                    
                    // CapsuleColliderを追加（存在しない場合）
                    if (_capsuleCollider == null)
                    {
                        _capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                        _capsuleCollider.height = height;
                        _capsuleCollider.radius = radius;
                        _capsuleCollider.center = center;
                        Debug.Log($"[AWAKE] CapsuleCollider追加: Height={height}, Radius={radius}, Center={center}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AWAKE] CharacterController処理エラー: {e.Message}");
            }
            
            // Rigidbodyが存在しない場合は追加
            if (_rigidbody == null)
            {
                Debug.Log($"[AWAKE] Rigidbodyが見つからないため追加します");
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            if (_rigidbody != null)
            {
                // Unity 6対応: 物理ベースの移動システム
                _rigidbody.isKinematic = false; // 物理エンジンを使用
                _rigidbody.useGravity = true; // Unity物理エンジンによる重力
                _rigidbody.freezeRotation = true; // 回転は手動制御
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // スムーズな動き
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 高速移動対応
                
                // 質量とドラッグの設定
                _rigidbody.mass = 1f;
                _rigidbody.linearDamping = 1f; // 移動の抵抗（適度な減速）
                _rigidbody.angularDamping = 0f; // 回転の抵抗は不要
                
                Debug.Log($"[AWAKE] Rigidbody設定完了: Non-Kinematic物理ベース移動");
                Debug.Log($"[AWAKE] Transform:{transform.position}, Rigidbody:{_rigidbody.position}");
            }
            
            if (_capsuleCollider == null)
            {
                Debug.LogError($"[AWAKE ERROR] CapsuleCollider not found on {gameObject.name}");
            }
            else
            {
                // CapsuleColliderの最適化
                _capsuleCollider.isTrigger = false; // 物理衝突を有効
                Debug.Log($"[AWAKE] CapsuleCollider設定完了: Height={_capsuleCollider.height}, Radius={_capsuleCollider.radius}");
            }
            
            Debug.Log($"[AWAKE] Unity 6対応完了 - Rigidbody+CapsuleCollider移動システム初期化完了 on {gameObject.name}");
        }

        private void Start()
        {
            Debug.Log($"[START] ===== Start開始 on {gameObject.name} =====");
            Debug.Log($"[START] GameObject Active: {gameObject.activeInHierarchy}, Component Enabled: {enabled}");
            
            // 重要: コンポーネントの初期化を確実に行う
            if (_rigidbody == null)
            {
                Debug.LogError($"[START ERROR] Rigidbodyが見つかりません！ Awake()が呼ばれていない可能性があります");
                return;
            }
            
            // ULTRA ANALYSIS: GroundLayers設定の詳細分析
            Debug.Log($"[ULTRA] GroundLayers詳細分析:");
            Debug.Log($"[ULTRA] GroundLayers.value: {GroundLayers.value}");
            for (int i = 0; i < 32; i++)
            {
                if ((GroundLayers.value & (1 << i)) != 0)
                {
                    Debug.Log($"[ULTRA] 有効レイヤー: {i} ({LayerMask.LayerToName(i)})");
                }
            }
            
            // 周辺の全オブジェクトとレイヤーを調査
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            Debug.Log($"[ULTRA] シーン内の全オブジェクト数: {allObjects.Length}");
            foreach (var obj in allObjects)
            {
                if (obj.GetComponent<Collider>() != null)
                {
                    Debug.Log($"[ULTRA] Collider発見: {obj.name}, Layer:{obj.layer}({LayerMask.LayerToName(obj.layer)}), Position:{obj.transform.position}, Active:{obj.activeInHierarchy}");
                }
            }
            
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            // ポケモンGOスタイルの俯瞰視点用にカメラ角度を初期化
            _cinemachineTargetPitch = CameraAngleOverride;
            
            _hasAnimator = TryGetComponent(out _animator);
            Debug.Log($"[START] HasAnimator: {_hasAnimator}, Animator: {_animator?.name}");
            
            _input = GetComponent<StarterAssetsInputs>();
            Debug.Log($"[START] Input component: {_input?.name}");
            
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
            Debug.Log($"[START] PlayerInput component: {_playerInput?.name}");
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            
            // 初期位置の記録（Rigidbody基準）
            if (_rigidbody != null)
            {
                // RigidbodyとTransformの位置を同期
                _rigidbody.position = transform.position;
                _lastPosition = _rigidbody.position;
                _lastMovementTime = Time.time;
                
                Debug.Log($"[START] 初期位置同期 - Transform:{transform.position}, Rigidbody:{_rigidbody.position}");
                
                // 初期位置を地面にスナップ
                SnapToGround();
            }
            
            Debug.Log($"[START] Unity 6移動システム準備完了:");
            Debug.Log($"[START] - Position: {transform.position}");
            Debug.Log($"[START] - Rigidbody: {_rigidbody != null} (Kinematic: {_rigidbody?.isKinematic})");
            Debug.Log($"[START] - CapsuleCollider: {_capsuleCollider != null} (Height: {_capsuleCollider?.height}, Radius: {_capsuleCollider?.radius})");
            Debug.Log($"[START] - 地面判定準備完了 on {gameObject.name}");
        }

        private void Update()
        {
            // 完全無効化 - SimpleMovementControllerとの競合回避
            return;
            
            Debug.Log($"[UPDATE] Update呼び出し - Frame:{Time.frameCount}, Destroying:{_isDestroying}, Quitting:{_isQuitting}, Active:{gameObject.activeInHierarchy}, Enabled:{enabled} on {gameObject.name}");
            
            // ULTIMATE DEFENSE: Multiple early exit conditions
            if (_isDestroying || _isQuitting)
            {
                Debug.Log($"[UPDATE BLOCKED] Destroying:{_isDestroying}, Quitting:{_isQuitting} on {gameObject.name}");
                return;
            }
            
            // Additional safety: Check gameobject state
            if (!gameObject.activeInHierarchy || !enabled)
            {
                Debug.Log($"[UPDATE BLOCKED] Active:{gameObject.activeInHierarchy}, Enabled:{enabled} on {gameObject.name}");
                return;
            }

            // Input system check
            if (_input == null)
            {
                Debug.LogError($"[UPDATE ERROR] _input is null on {gameObject.name}");
                return;
            }

            Debug.Log($"[UPDATE] 処理開始 - Input:{_input.move}, HasAnimator:{_hasAnimator} on {gameObject.name}");

            _hasAnimator = TryGetComponent(out _animator);

            GroundedCheck();
            HandleJump(); // 物理ベースのジャンプ処理
            
            // Updateでは入力処理とアニメーション更新のみ
            CalculateMovement();
            
            Debug.Log($"[UPDATE] 処理完了 - Position:{transform.position} on {gameObject.name}");
        }
        
        private void FixedUpdate()
        {
            // 🚨 完全無効化 - SimpleMovementControllerとの競合回避
            return;
            
            Debug.Log($"[FIXED_UPDATE] FixedUpdate呼び出し - Frame:{Time.fixedTime}, Destroying:{_isDestroying}, Quitting:{_isQuitting} on {gameObject.name}");
            
            // ULTIMATE DEFENSE: Multiple early exit conditions
            if (_isDestroying || _isQuitting)
            {
                Debug.Log($"[FIXED_UPDATE BLOCKED] Destroying:{_isDestroying}, Quitting:{_isQuitting} on {gameObject.name}");
                return;
            }
            
            // Additional safety: Check gameobject state
            if (!gameObject.activeInHierarchy || !enabled)
            {
                Debug.Log($"[FIXED_UPDATE BLOCKED] Active:{gameObject.activeInHierarchy}, Enabled:{enabled} on {gameObject.name}");
                return;
            }

            // Input system check
            if (_input == null)
            {
                Debug.LogError($"[FIXED_UPDATE ERROR] _input is null on {gameObject.name}");
                return;
            }

            // FixedUpdateで物理移動を実行
            Move();
            
            // Rigidbodyが自動的にTransformを更新するため、同期処理は不要
            
            Debug.Log($"[FIXED_UPDATE] 物理移動完了 - Position:{_rigidbody?.position} on {gameObject.name}");
        }
        

        private void LateUpdate()
        {
            // ARCHITECTURAL SOLUTION: Skip if destroying
            if (_isDestroying || _isQuitting)
                return;
                
            CameraRotation();
            
            // Unity物理システムがRigidbody→Transform同期を自動処理
            Debug.Log($"[LATE_UPDATE] 物理システム自動同期完了 - Transform:{transform.position}");
        }
        
        /// <summary>
        /// ARCHITECTURAL SOLUTION: Proper lifecycle management
        /// </summary>
        private void OnDestroy()
        {
            _isDestroying = true;
            
            // Unsubscribe from scene management events
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
            
            Debug.Log($"[LIFECYCLE] ThirdPersonController.OnDestroy called on {gameObject.name}");
        }
        
        /// <summary>
        /// ARCHITECTURAL SOLUTION: Scene management event handlers
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Debug.Log($"[SCENE MANAGEMENT] Scene loaded: {scene.name}, mode: {mode}, ThirdPersonController on {gameObject.name}");
        }
        
        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            Debug.Log($"[SCENE MANAGEMENT] Scene unloaded: {scene.name}, ThirdPersonController on {gameObject.name}");
            // Prepare for potential destruction
            _isDestroying = true;
        }
        
        private void OnDisable()
        {
            // ULTIMATE SOLUTION: Set destroying flag as early as possible
            _isDestroying = true;
            Debug.Log($"[LIFECYCLE] ThirdPersonController.OnDisable called on {gameObject.name}");
        }
        
        private void OnApplicationQuit()
        {
            _isQuitting = true;
            _isDestroying = true; // Double safety
            // Debug.Log($"[LIFECYCLE] ThirdPersonController.OnApplicationQuit called on {gameObject.name}");
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log($"[LIFECYCLE] ThirdPersonController.OnApplicationPause(true) called on {gameObject.name}");
            }
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // Unity 6対応: 完璧な地面判定システム
            if (_capsuleCollider == null || _rigidbody == null)
            {
                Grounded = false;
                Debug.LogWarning($"[GROUNDED] コンポーネント不足 - CapsuleCollider:{_capsuleCollider != null}, Rigidbody:{_rigidbody != null}");
                return;
            }
            
            // Rigidbody位置基準で地面検出（Transform操作なし）
            Vector3 rigidbodyPos = _rigidbody.position;
            Vector3 capsuleBottom = rigidbodyPos + Vector3.down * (_capsuleCollider.height * 0.5f - _capsuleCollider.radius);
            Vector3 spherePosition = capsuleBottom + Vector3.down * GroundedOffset;
            
            // 複数の検出方法を組み合わせて確実な判定
            float checkRadius = _capsuleCollider.radius + GroundedRadius;
            
            // Method 1: Sphere check (GroundLayers)
            bool sphereGrounded = Physics.CheckSphere(spherePosition, checkRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            
            // Method 2: Raycast check (GroundLayers)
            bool rayGrounded = Physics.Raycast(rigidbodyPos, Vector3.down, 
                _capsuleCollider.height * 0.5f + GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            
            // Method 3: 全レイヤーでのチェック（デバッグ用）
            bool sphereAllLayers = Physics.CheckSphere(spherePosition, checkRadius, ~0, QueryTriggerInteraction.Ignore);
            bool rayAllLayers = Physics.Raycast(rigidbodyPos, Vector3.down, 
                _capsuleCollider.height * 0.5f + GroundedRadius, ~0, QueryTriggerInteraction.Ignore);
            
            // 結果を統合
            Grounded = sphereGrounded || rayGrounded;
            _isGroundedInternal = Grounded;
            
            // デバッグログで詳細情報を出力
            Debug.Log($"[GROUNDED] Pos:{rigidbodyPos}, Grounded:{Grounded}");
            Debug.Log($"[GROUNDED] GroundLayers({GroundLayers.value}): Sphere={sphereGrounded}, Ray={rayGrounded}");  
            Debug.Log($"[GROUNDED] AllLayers: Sphere={sphereAllLayers}, Ray={rayAllLayers}");
            Debug.Log($"[GROUNDED] CheckPos: Sphere={spherePosition}, Radius={checkRadius}");

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void SnapToGround()
        {
            Vector3 currentPosition = _rigidbody != null ? _rigidbody.position : transform.position;
            Debug.Log($"[SNAP] 地面スナップ開始 - CurrentPosition:{currentPosition}, GroundLayers:{GroundLayers.value}");
            
            // ULTRA INVESTIGATION: 複数の方法でCollider存在を確認
            Vector3 checkPos = currentPosition;
            
            // 1. Physics.OverlapSphereでColliderの存在確認
            Collider[] nearbyColliders = Physics.OverlapSphere(checkPos, 5f);
            Debug.Log($"[ULTRA] 周辺Collider数: {nearbyColliders.Length}");
            foreach (var col in nearbyColliders)
            {
                Debug.Log($"[ULTRA] 発見Collider: {col.name}, Layer:{col.gameObject.layer}, LayerName:{LayerMask.LayerToName(col.gameObject.layer)}, Position:{col.transform.position}, Bounds:{col.bounds}");
            }
            
            // 2. Map_Tileを直接検索
            GameObject[] mapTiles = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (var tile in mapTiles)
            {
                if (tile.name.Contains("Map_Tile"))
                {
                    Debug.Log($"[ULTRA] Map_Tile発見: {tile.name}, Layer:{tile.layer}, Position:{tile.transform.position}, Active:{tile.activeInHierarchy}");
                    var collider = tile.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Debug.Log($"[ULTRA] Map_Tile Collider: Type:{collider.GetType()}, Enabled:{collider.enabled}, isTrigger:{collider.isTrigger}, Bounds:{collider.bounds}");
                        if (collider is MeshCollider meshCol)
                        {
                            Debug.Log($"[ULTRA] MeshCollider詳細: Convex:{meshCol.convex}, Mesh:{meshCol.sharedMesh?.name}");
                        }
                    }
                }
            }
            
            // 3. 従来のRaycast
            RaycastHit hit;
            Vector3 rayStart = currentPosition + Vector3.up * 2f;
            
            // まずGroundLayersでチェック
            bool groundFound = Physics.Raycast(rayStart, Vector3.down, out hit, 10f, GroundLayers);
            Debug.Log($"[SNAP] GroundLayers検索結果: {groundFound}");
            
            // GroundLayersで見つからない場合、全レイヤーで検索
            if (!groundFound)
            {
                groundFound = Physics.Raycast(rayStart, Vector3.down, out hit, 10f);
                Debug.Log($"[SNAP] 全レイヤー検索結果: {groundFound}");
                if (groundFound)
                {
                    Debug.Log($"[SNAP] 全レイヤーで発見 - Collider:{hit.collider.name}, Layer:{hit.collider.gameObject.layer}, LayerName:{LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                }
            }
            
            if (groundFound)
            {
                Vector3 groundPosition = new Vector3(currentPosition.x, hit.point.y, currentPosition.z);
                if (_rigidbody != null)
                {
                    _rigidbody.position = groundPosition;
                }
                transform.position = groundPosition;
                _verticalVelocity = -2f;
                Debug.Log($"[SNAP] 地面にスナップ完了 - GroundPosition:{groundPosition}, Collider:{hit.collider.name}, Distance:{hit.distance}");
            }
            else
            {
                Debug.Log($"[SNAP] 地面が見つかりません - RayStart:{rayStart}");
                
                // FALLBACK: 最も近いColliderを地面として使用
                if (nearbyColliders.Length > 0)
                {
                    var closestCollider = nearbyColliders[0];
                    Vector3 fallbackPos = new Vector3(currentPosition.x, closestCollider.bounds.max.y, currentPosition.z);
                    if (_rigidbody != null)
                    {
                        _rigidbody.position = fallbackPos;
                    }
                    transform.position = fallbackPos;
                    _verticalVelocity = -2f;
                    Debug.Log($"[SNAP] FALLBACK: 最寄りのColliderを使用 - Position:{fallbackPos}, Collider:{closestCollider.name}");
                }
                else
                {
                    // 地面が全く見つからない場合は、Y=0に設定
                    Vector3 defaultPos = new Vector3(currentPosition.x, 0f, currentPosition.z);
                    if (_rigidbody != null)
                    {
                        _rigidbody.position = defaultPos;
                    }
                    transform.position = defaultPos;
                    Debug.Log($"[SNAP] デフォルト位置に設定 - Position:{defaultPos}");
                }
            }
        }

        private void CalculateMovement()
        {
            // Rigidbody位置基準で移動量を計算（Transform操作なし）
            Vector3 currentRigidbodyPos = _rigidbody.position;
            Vector3 movement = currentRigidbodyPos - _lastPosition;
            movement.y = 0; // 高さ方向の移動は無視
            
            // 実際の移動量を計算
            _movementMagnitude = movement.magnitude / Time.deltaTime;
            
            // 移動状態の判定（MinimumWalkThreshold以上の移動があるかどうか）
            bool isMovingNow = _movementMagnitude >= MinimumWalkThreshold;
            
            // 移動状態が変わった場合の処理
            if (isMovingNow && !_wasMoving)
            {
                // 静止→移動の状態変化
                _wasMoving = true;
                _lastMovementTime = Time.time;
            }
            else if (!isMovingNow && _wasMoving)
            {
                // 移動→静止の状態変化
                _wasMoving = false;
            }
            
            // Rigidbody位置を記録
            _lastPosition = currentRigidbodyPos;
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            
            // ポケモンGOスタイルではBottomClampを調整して、常に見下ろす角度を維持
            float effectiveBottomClamp = CameraAngleOverride > 0 ? CameraAngleOverride * 0.75f : BottomClamp;
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, effectiveBottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        public void Move()
        {
            Debug.Log($"[MOVE] 開始 - Grounded:{Grounded}, VerticalVelocity:{_verticalVelocity}, Input:{_input.move}");
            
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // Unity 6対応: Rigidbodyベースの速度計算
            float currentHorizontalSpeed = new Vector3(_velocity.x, 0.0f, _velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // ポケモンGOスタイル: 実際の移動量に基づいたアニメーションブレンドを実装
            float targetAnimationSpeed = 0f;
            
            // 入力がある場合、または実際に動いている場合
            if (_input.move != Vector2.zero || _movementMagnitude > MinimumWalkThreshold)
            {
                // 実際の移動量を使用して、より敏感に歩行アニメーションを発生させる
                targetAnimationSpeed = Mathf.Max(_movementMagnitude, inputMagnitude * targetSpeed);
                
                // 小さな動きを増幅（ポケモンGOスタイル）
                if (targetAnimationSpeed < MoveSpeed)
                {
                    // 小さな動きを大げさに表現（AnimationSensitivityが小さいほど敏感になる）
                    targetAnimationSpeed = Mathf.Lerp(MoveSpeed * 0.8f, MoveSpeed, 
                        Mathf.Clamp01(targetAnimationSpeed / (MoveSpeed * AnimationSensitivity)));
                }
            }
            
            // アニメーションブレンド値をスムーズに変更
            _animationBlend = Mathf.Lerp(_animationBlend, targetAnimationSpeed, 
                Time.deltaTime * AnimationSmoothness);
                
            // 非常に小さい値の場合はゼロにする
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            // Unity 6対応: 物理ベースの移動システム
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            Vector3 horizontalMovement = targetDirection.normalized * _speed;
            
            Debug.Log($"[MOVE] 移動計算 - Speed:{_speed}, HorizontalMovement:{horizontalMovement}");
            
            // Non-Kinematic Rigidbodyでの物理ベース移動
            if (!_isDestroying && !_isQuitting && 
                gameObject.activeInHierarchy && enabled && isActiveAndEnabled && 
                _rigidbody != null)
            {
                // 水平移動力を適用
                Vector3 targetVelocity = new Vector3(horizontalMovement.x, _rigidbody.linearVelocity.y, horizontalMovement.z);
                Vector3 velocityChange = targetVelocity - _rigidbody.linearVelocity;
                velocityChange.y = 0; // Y軸の速度変更は重力に任せる
                
                // 力を使って移動（より自然な物理挙動）
                _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
                
                Debug.Log($"[MOVE] 物理移動完了 - Velocity:{_rigidbody.linearVelocity}, Position:{_rigidbody.position}");
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void HandleJump()
        {
            if (!_rigidbody) return;
            
            Debug.Log($"[JUMP] 開始 - Grounded:{Grounded}, Jump:{_input.jump}");
            
            if (Grounded)
            {
                // ジャンプタイムアウトの処理
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
                
                // ジャンプ処理
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // 物理ベースのジャンプ（上向きの力を加える）
                    float jumpVelocity = Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y);
                    _rigidbody.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
                    
                    // タイムアウトをリセット
                    _jumpTimeoutDelta = JumpTimeout;
                    
                    // アニメーション
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                    
                    Debug.Log($"[JUMP] ジャンプ実行 - JumpVelocity:{jumpVelocity}");
                }
            }
            else
            {
                // 空中にいる場合
                _jumpTimeoutDelta = JumpTimeout;
                _input.jump = false;
                
                // フリーフォールアニメーション
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }
            
            // 地面にいる場合のアニメーションリセット
            if (Grounded && _hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }
        }
        
        private void JumpAndGravity()
        {
            Debug.Log($"[JUMP] 開始 - Grounded:{Grounded}, VerticalVelocity:{_verticalVelocity}, Jump:{_input.jump}");
            
            // 異常な垂直速度をリセット
            if (_verticalVelocity < -100f)
            {
                _verticalVelocity = -2f;
                Debug.LogWarning($"[JUMP] 異常な垂直速度をリセット: {_verticalVelocity}");
            }
            
            if (Grounded)
            {
                Debug.Log($"[JUMP] 地面にいる状態の処理");
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity && !Grounded)
            {
                float oldVerticalVelocity = _verticalVelocity;
                _verticalVelocity += Gravity * Time.deltaTime;
                
                // 重力による加速度制限
                _verticalVelocity = Mathf.Max(_verticalVelocity, -50f);
                
                Debug.Log($"[JUMP] 重力適用 - Old:{oldVerticalVelocity}, New:{_verticalVelocity}, Gravity:{Gravity}, DeltaTime:{Time.deltaTime}");
            }
            
            Debug.Log($"[JUMP] 完了 - FinalVerticalVelocity:{_verticalVelocity}");
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position + Vector3.up, FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position + Vector3.up, FootstepAudioVolume);
            }
        }

        /// <summary>
        /// Unity 6対応: 衝突検出付きの安全な移動システム
        /// CharacterController.Moveの完全な代替実装
        /// </summary>
        private Vector3 PerformSafeMovement(Vector3 currentPosition, Vector3 targetPosition)
        {
            if (_capsuleCollider == null)
                return targetPosition;
            
            Vector3 movement = targetPosition - currentPosition;
            float distance = movement.magnitude;
            
            if (distance < 0.001f)
                return currentPosition;
            
            Vector3 direction = movement.normalized;
            
            // CapsuleCastで衝突検出
            Vector3 capsuleTop = currentPosition + Vector3.up * (_capsuleCollider.height * 0.5f - _capsuleCollider.radius);
            Vector3 capsuleBottom = currentPosition + Vector3.down * (_capsuleCollider.height * 0.5f - _capsuleCollider.radius);
            
            RaycastHit hit;
            bool hasHit = Physics.CapsuleCast(
                capsuleBottom, capsuleTop, _capsuleCollider.radius,
                direction, out hit, distance, ~0, QueryTriggerInteraction.Ignore);
            
            if (hasHit)
            {
                // 衝突した場合、衝突点の手前で停止
                float safeDistance = Mathf.Max(0, hit.distance - 0.01f);
                Vector3 safePosition = currentPosition + direction * safeDistance;
                
                // 壁に沿った移動を試行 (CharacterControllerのslide機能を再現)
                Vector3 slideDirection = Vector3.ProjectOnPlane(direction, hit.normal).normalized;
                float remainingDistance = distance - safeDistance;
                
                if (remainingDistance > 0.01f && slideDirection.magnitude > 0.1f)
                {
                    // 滑り移動の二次衝突チェック
                    bool slideHit = Physics.CapsuleCast(
                        safePosition + Vector3.down * (_capsuleCollider.height * 0.5f - _capsuleCollider.radius),
                        safePosition + Vector3.up * (_capsuleCollider.height * 0.5f - _capsuleCollider.radius),
                        _capsuleCollider.radius, slideDirection, out hit, remainingDistance,
                        ~0, QueryTriggerInteraction.Ignore);
                    
                    if (!slideHit)
                    {
                        safePosition += slideDirection * remainingDistance;
                    }
                    else
                    {
                        safePosition += slideDirection * Mathf.Max(0, hit.distance - 0.01f);
                    }
                }
                
                Debug.Log($"[SAFE_MOVE] 衝突検出 - Hit:{hit.collider.name}, SafePosition:{safePosition}");
                return safePosition;
            }
            
            return targetPosition;
        }

        // コンパス制御用のメソッド
        public void SetLookRotation(Quaternion targetRotation)
        {
            SetLookRotation(targetRotation, Time.deltaTime * 5.0f);
        }
        
        // 補間速度を指定できるオーバーロードメソッド
        public void SetLookRotation(Quaternion targetRotation, float smoothFactor)
        {
            if (CinemachineCameraTarget == null) return;
            
            Vector3 eulerAngles = targetRotation.eulerAngles;
            
            // ヨー角をスムーズに補間して設定
            float currentYaw = _cinemachineTargetYaw;
            float targetYaw = eulerAngles.y;
            
            // 角度の違いを計算（-180〜180度の範囲）
            float deltaYaw = Mathf.DeltaAngle(currentYaw, targetYaw);
            
            // 角度の変化が大きい場合は直接値を設定、そうでなければスムーズに補間
            if (Mathf.Abs(deltaYaw) > 90.0f)
            {
                _cinemachineTargetYaw = targetYaw;
            }
            else
            {
                // 滑らかに目標角度に近づける (パラメータ化されたスムージング係数を使用)
                _cinemachineTargetYaw = Mathf.LerpAngle(currentYaw, targetYaw, smoothFactor);
            }
            
            // ポケモンGOスタイルのカメラでは、ピッチ角も維持する
            _cinemachineTargetPitch = Mathf.Lerp(_cinemachineTargetPitch, CameraAngleOverride, smoothFactor);
            
            // カメラ回転を更新
            RefreshCameraRotation();
        }

        public void RefreshCameraRotation()
        {
            if (CinemachineCameraTarget == null) return;
            
            // 現在のピッチ角を維持しながらヨー角を更新
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 
                0.0f);
        }
    }
}