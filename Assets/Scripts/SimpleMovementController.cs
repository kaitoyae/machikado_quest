using UnityEngine;

/// <summary>
/// シンプル物理ベース移動制御システム
/// ThirdPersonControllerの完全置き換え（既存互換性維持）
/// 二重重力問題を解決し、純粋Rigidbody物理を使用
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(InputCoordinator))]
public class SimpleMovementController : MonoBehaviour
{
    [Header("Movement Settings (Existing Compatible)")]
    [Tooltip("通常移動速度 m/s")]
    public float MoveSpeed = 2.0f;
    [Tooltip("スプリント速度 m/s")]
    public float SprintSpeed = 5.335f;
    [Tooltip("回転スムージング時間")]
    public float RotationSmoothTime = 0.12f;

    [Header("Physics Settings")]
    [Tooltip("加速度")]
    public float acceleration = 10f;
    [Tooltip("地面摩擦")]
    public float groundDrag = 5f;
    [Tooltip("空中摩擦")]
    public float airDrag = 0.5f;

    [Header("Ground Detection")]
    [Tooltip("地面判定半径")]
    public float GroundedRadius = 0.28f;
    [Tooltip("地面レイヤー")]
    public LayerMask GroundLayers = 1;
    [Tooltip("地面判定オフセット")]
    public float groundCheckOffset = 0.1f;

    [Header("Animation Settings")]
    [Tooltip("アニメーション感度")]
    [Range(0.01f, 1.0f)]
    public float AnimationSensitivity = 0.1f;
    [Tooltip("アニメーションスムーズネス")]
    [Range(1.0f, 20.0f)]
    public float AnimationSmoothness = 5.0f;

    // 既存互換プロパティ
    public bool Grounded { get; private set; }
    public Vector3 velocity => rb.linearVelocity;

    // コンポーネント参照
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private InputCoordinator inputCoordinator;
    private Animator animator;
    private Camera playerCamera;

    // 内部状態
    private float targetRotation;
    private float rotationVelocity;
    private Vector3 lastMovementDirection;
    private float currentSpeed;
    
    // アニメーションパラメータハッシュ（パフォーマンス最適化）
    private int animSpeedHash;
    private int animGroundedHash;
    private int animMotionSpeedHash;

    void Awake()
    {
        InitializeComponents();
        ConfigurePhysics();
        CacheAnimationHashes();
    }

    void Start()
    {
        FindPlayerCamera();
        lastKnownPosition = transform.position;
        Debug.Log($"[INIT] Start - Initial position: {lastKnownPosition}");
        
        // 🚨 強制的にログを出すテスト
        Debug.Log($"[TEST] Start実行完了");
    }

    void Update()
    {
        GroundedCheck();
        HandleMovement();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        // 🚨 FixedUpdate開始時の状態確認
        Vector3 velocityAtStart = rb.linearVelocity;
        
        ApplyMovementForce();
        ApplyDrag();
        
        // 🚨 力適用直後の確認
        Vector3 velocityAfterForce = rb.linearVelocity;
        if (velocityAfterForce != velocityAtStart)
        {
            Debug.Log($"[PHYSICS_CHANGE] Force適用後: {velocityAtStart} → {velocityAfterForce}");
        }
        
        // 🚨 1フレーム後の確認（コルーチンで）
        StartCoroutine(CheckVelocityAfterPhysics(velocityAfterForce));
    }
    
    private System.Collections.IEnumerator CheckVelocityAfterPhysics(Vector3 expectedVelocity)
    {
        yield return new WaitForFixedUpdate();
        
        if (rb.linearVelocity != expectedVelocity)
        {
            Debug.Log($"[RIGIDBODY_INTERFERENCE] 誰かがRigidbodyをリセット！ 期待値:{expectedVelocity} → 実際:{rb.linearVelocity}");
            
            // スタックトレースで犯人特定
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            Debug.Log($"[INTERFERENCE_STACK] Rigidbody干渉の呼び出し元:\n{stackTrace}");
        }
    }
    
    private Vector3 lastKnownPosition;
    
    void LateUpdate()
    {
        // 🚨 毎回ログを出して確認
        Debug.Log($"[LATEUPDATE_TEST] Current position: {transform.position}, Last: {lastKnownPosition}");
        
        // 🚨 位置変更の詳細追跡
        if (transform.position != lastKnownPosition)
        {
            Debug.Log($"[TRANSFORM_CHANGE] Position changed from {lastKnownPosition} to {transform.position}");
            
            // スタックトレースで犯人を特定
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            Debug.Log($"[STACK_TRACE] 位置変更の呼び出し元:\n{stackTrace}");
            
            lastKnownPosition = transform.position;
        }
        
        if (transform.hasChanged)
        {
            Debug.Log($"[TRANSFORM_HASCHANGED] Transform has changed!");
            transform.hasChanged = false;
        }
    }

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        inputCoordinator = GetComponent<InputCoordinator>();
        animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Rigidbody物理設定
    /// </summary>
    private void ConfigurePhysics()
    {
        // 重力は物理エンジンに完全委任（手動重力計算削除）
        rb.useGravity = true;
        rb.linearDamping = 0f; // ドラッグは手動制御
        rb.angularDamping = 10f; // 回転摩擦
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    /// <summary>
    /// アニメーションハッシュキャッシュ
    /// </summary>
    private void CacheAnimationHashes()
    {
        animSpeedHash = Animator.StringToHash("Speed");
        animGroundedHash = Animator.StringToHash("Grounded");
        animMotionSpeedHash = Animator.StringToHash("MotionSpeed");
    }

    /// <summary>
    /// プレイヤーカメラ検索
    /// </summary>
    private void FindPlayerCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    /// <summary>
    /// シンプル地面判定（CheckSphere 1回のみ）
    /// </summary>
    private void GroundedCheck()
    {
        // コライダーのセンターを考慮した地面判定位置
        Vector3 spherePosition = transform.position + capsule.center - Vector3.up * (capsule.height * 0.5f - groundCheckOffset);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers);
        
        // 🔥 追加デバッグ：Collider設定確認
        Debug.Log($"[COLLIDER_DEBUG] GameObject Position: {transform.position}");
        Debug.Log($"[COLLIDER_DEBUG] Collider Center: {capsule.center}");
        Debug.Log($"[COLLIDER_DEBUG] Collider Size: Height={capsule.height}, Radius={capsule.radius}");
        Debug.Log($"[COLLIDER_DEBUG] Ground Check Position: {spherePosition}");
        Debug.Log($"[COLLIDER_DEBUG] Ground Check Radius: {GroundedRadius}");
        Debug.Log($"[COLLIDER_DEBUG] Ground Layers: {GroundLayers.value}");
        Debug.Log($"[COLLIDER_DEBUG] Grounded Result: {Grounded}");
        
        // Debug.Log($"[SimpleMovementController] Ground check - Position: {spherePosition}, Radius: {GroundedRadius}, Layers: {GroundLayers.value}, Result: {Grounded}"); // クリーンアップ済み
    }

    /// <summary>
    /// 移動処理（カメラ相対）
    /// </summary>
    private void HandleMovement()
    {
        Debug.Log($"[HANDLEMOVEMENT_DEBUG] HandleMovement開始");
        
        Vector2 inputMove = inputCoordinator.move;
        bool isSprinting = inputCoordinator.sprint;

        // 🔥 緊急デバッグ：入力チェーン全体トレース
        Debug.Log($"[INPUT_TRACE] InputCoordinator.move: {inputMove}, magnitude: {inputMove.magnitude}");
        
        // 🚨 新Input System経由での入力確認
        if (inputMove.magnitude > 0.01f)
        {
            Debug.Log($"[DIRECT_INPUT] 新Input System経由で入力検出！ Move:{inputMove}");
        }

        // 入力が無い場合は早期リターン
        if (inputMove.magnitude < 0.01f)
        {
            lastMovementDirection = Vector3.zero;
            currentSpeed = 0f;
            Debug.Log($"[MOVEMENT_DEBUG] 入力なし - 早期リターン magnitude:{inputMove.magnitude}");
            return;
        }
        
        Debug.Log($"[MOVEMENT_DEBUG] 入力あり - 処理続行 magnitude:{inputMove.magnitude}");

        // カメラ相対方向計算
        Vector3 inputDirection = Vector3.zero;
        if (playerCamera != null)
        {
            Vector3 forward = playerCamera.transform.forward;
            Vector3 right = playerCamera.transform.right;
            
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            inputDirection = forward * inputMove.y + right * inputMove.x;
        }
        else
        {
            inputDirection = new Vector3(inputMove.x, 0f, inputMove.y);
        }

        // 目標速度設定
        float targetSpeed = isSprinting ? SprintSpeed : MoveSpeed;
        currentSpeed = targetSpeed;

        // 移動方向記録
        if (inputDirection.magnitude > 0.01f)
        {
            lastMovementDirection = inputDirection.normalized;
            
            // 回転処理
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, 
                ref rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
    }

    /// <summary>
    /// 物理ベース移動力適用
    /// </summary>
    private void ApplyMovementForce()
    {
        Debug.Log($"[APPLYFORCE_DEBUG] ApplyMovementForce開始 - lastMovementDirection: {lastMovementDirection}");
        
        // 🔥 ROOT CAUSE FIX: 入力がない時は速度をリセットしない
        if (lastMovementDirection.magnitude < 0.01f)
        {
            Debug.Log($"[APPLYFORCE_DEBUG] 移動方向なし - 早期リターン");
            return; // 重要：velocityを設定せずに終了
        }
        
        Debug.Log($"[APPLYFORCE_DEBUG] 移動方向あり - 力適用処理続行");

        Vector3 targetVelocity = lastMovementDirection * currentSpeed;
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 velocityDifference = targetVelocity - currentHorizontalVelocity;

        // 地面判定（一時的に無効化して動作確認）
        if (true) // 後で Grounded に変更
        {
            // 【重要】GPS修正効果確認用：力適用前後の速度追跡
            Vector3 velocityBefore = rb.linearVelocity;
            
            // 🔥 緊急診断：Rigidbody状態確認
            Debug.Log($"[RIGIDBODY_DIAGNOSIS] Mass:{rb.mass}, Drag:{rb.linearDamping}, IsKinematic:{rb.isKinematic}, UseGravity:{rb.useGravity}");
            Debug.Log($"[RIGIDBODY_DIAGNOSIS] Constraints:{rb.constraints}");
            Debug.Log($"[RIGIDBODY_DIAGNOSIS] 詳細 - FreezePositionX:{(rb.constraints & RigidbodyConstraints.FreezePositionX) != 0}");
            Debug.Log($"[RIGIDBODY_DIAGNOSIS] 詳細 - FreezePositionY:{(rb.constraints & RigidbodyConstraints.FreezePositionY) != 0}");
            Debug.Log($"[RIGIDBODY_DIAGNOSIS] 詳細 - FreezePositionZ:{(rb.constraints & RigidbodyConstraints.FreezePositionZ) != 0}");
            
            // 🚨 最終手段：実行時強制修正
            if ((rb.constraints & RigidbodyConstraints.FreezePositionX) != 0 || 
                (rb.constraints & RigidbodyConstraints.FreezePositionZ) != 0)
            {
                Debug.Log($"[CONSTRAINT_FIX] X,Z位置制約を強制解除！");
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            
            // 🔥 EMERGENCY: Rigidbody完全再作成 - 削除済み（これが原因だった！）
            // Debug.Log($"[EMERGENCY] Rigidbody再作成開始...");
            // 
            // // 現在のRigidbodyを破棄
            // DestroyImmediate(rb);
            // 
            // // 新しいRigidbodyを作成
            // rb = gameObject.AddComponent<Rigidbody>();
            // rb.mass = 1f;
            // rb.useGravity = true;
            // rb.isKinematic = false;
            // rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            // rb.linearDamping = 0.5f;
            // 
            // Debug.Log($"[EMERGENCY] 新Rigidbody作成完了 - ID:{rb.GetInstanceID()}");
            
            // 直接位置変更テスト - 削除済み
            // Vector3 posBefore = rb.position;
            // rb.position = new Vector3(1f, rb.position.y, 0f);
            // Debug.Log($"[POSITION_TEST] 前:{posBefore}, 後:{rb.position}");
            
            // 🔥 追加デバッグ：物理演算の詳細調査
            Debug.Log($"[PHYSICS_DETAIL] Time.timeScale: {Time.timeScale}");
            Debug.Log($"[PHYSICS_DETAIL] Time.fixedDeltaTime: {Time.fixedDeltaTime}");
            Debug.Log($"[PHYSICS_DETAIL] rb.interpolation: {rb.interpolation}");
            Debug.Log($"[PHYSICS_DETAIL] rb.collisionDetectionMode: {rb.collisionDetectionMode}");
            
            // 🚨 重力問題の診断
            Debug.Log($"[GRAVITY_DIAGNOSIS] Physics.gravity: {Physics.gravity}");
            Debug.Log($"[GRAVITY_DIAGNOSIS] rb.useGravity: {rb.useGravity}");
            Debug.Log($"[GRAVITY_DIAGNOSIS] rb.isKinematic: {rb.isKinematic}");
            Debug.Log($"[GRAVITY_DIAGNOSIS] rb.detectCollisions: {rb.detectCollisions}");
            Debug.Log($"[GRAVITY_DIAGNOSIS] rb.IsSleeping: {rb.IsSleeping()}");
            
            // 🔥 追加デバッグ：Collider設定確認
            var collider = GetComponent<CapsuleCollider>();
            if (collider != null)
            {
                Debug.Log($"[COLLIDER_DETAIL] IsTrigger: {collider.isTrigger}");
                Debug.Log($"[COLLIDER_DETAIL] ProvidesContacts: {collider.providesContacts}");
                Debug.Log($"[COLLIDER_DETAIL] Radius: {collider.radius}, Height: {collider.height}");
                Debug.Log($"[COLLIDER_DETAIL] Center: {collider.center}");
            }
            
            // 🔥 追加デバッグ：直接速度設定テスト（無効化）
            // Debug.Log($"[DIRECT_TEST] 直接速度設定前: {rb.linearVelocity}");
            // rb.linearVelocity = new Vector3(5f, 0f, 0f);
            // Debug.Log($"[DIRECT_TEST] 直接速度設定後: {rb.linearVelocity}");
            
            // 力ベースの移動（仕様書通り）
            rb.AddForce(velocityDifference * acceleration, ForceMode.Acceleration);
            
            Vector3 velocityAfter = rb.linearVelocity;
            Debug.Log($"[FORCE_DEBUG] 力適用 - Before:{velocityBefore.magnitude:F3}, After:{velocityAfter.magnitude:F3}, Force:{(velocityDifference * acceleration).magnitude:F3}");
        }
    }

    /// <summary>
    /// ドラッグ適用
    /// </summary>
    private void ApplyDrag()
    {
        float currentDrag = Grounded ? groundDrag : airDrag;
        rb.linearDamping = currentDrag;
    }

    /// <summary>
    /// 既存互換アニメーション更新
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null) return;

        // 速度計算
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;
        
        // アニメーションパラメータ設定
        animator.SetFloat(animSpeedHash, speed, AnimationSensitivity, Time.deltaTime * AnimationSmoothness);
        animator.SetBool(animGroundedHash, Grounded);
        animator.SetFloat(animMotionSpeedHash, speed > 0.1f ? 1f : 0f);
    }

    // デバッグ表示
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 地面判定可視化
        Vector3 spherePosition = transform.position - Vector3.up * (capsule.height * 0.5f - groundCheckOffset);
        Gizmos.color = Grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, GroundedRadius);

        // 移動方向可視化
        if (lastMovementDirection.magnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, lastMovementDirection * 2f);
        }
    }

    // デバッグ情報
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(320, 10, 300, 200));
        GUILayout.Label("=== Movement Controller ===");
        GUILayout.Label($"Grounded: {Grounded}");
        GUILayout.Label($"Speed: {currentSpeed:F1} m/s");
        GUILayout.Label($"Velocity: {rb.linearVelocity.magnitude:F1} m/s");
        GUILayout.Label($"H-Velocity: {new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude:F1} m/s");
        GUILayout.EndArea();
    }

    // 診断用コルーチン（削除済み）
}