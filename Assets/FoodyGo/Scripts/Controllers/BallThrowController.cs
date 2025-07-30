using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace packt.FoodyGO.Controllers
{

/// <summary>
/// ボール投げ機能のコントローラー
/// ポケモンGOのモンスターボール投げを参考にした実装
/// </summary>
public class BallThrowController : MonoBehaviour
{
    [Header("ボール設定")]
    public GameObject ballPrefab;           // Sphereプレハブ
    public Transform ballSpawnPoint;        // ボール生成位置
    public float throwForceMultiplier = 10f; // 投射力の倍率
    public float maxThrowForce = 30f;       // 最大投射力
    
    [Header("クールダウン設定")]
    [SerializeField] private float ballCooldownTime = 1.0f;  // ボール生成のクールダウン時間
    
    [Header("カメラ設定")]
    public Camera throwCamera;             // 投げ用カメラ（通常はメインカメラ）
    
    [Header("軌道表示")]
    public LineRenderer trajectoryLine;    // 軌道表示用
    public int trajectoryPoints = 50;      // 軌道の点数
    public float trajectoryTimeStep = 0.1f; // 軌道計算の時間刻み
    
    [Header("入力設定")]
    // InputActionAssetは使用せず、直接InputActionを作成
    
    // 内部状態
    private InputAction grabAction;
    private InputAction aimAction;
    private bool isGrabbing = false;
    private Vector2 grabStartPosition;
    private Vector2 currentAimPosition;
    private GameObject currentBall;
    private float lastThrowTime = -999f;  // 最後に投げた時間
    
    // プロパティ
    public bool IsGrabbing => isGrabbing;
    public GameObject CurrentBall => currentBall;
    public bool CanThrow => Time.time - lastThrowTime >= ballCooldownTime;
    
    // イベント
    public event Action<GameObject, MonsterController> OnBallHitMonster;
    
    private void Awake()
    {
        InitializeInputSystem();
        InitializeComponents();
        
        // シーンに既存のボールがある場合は削除
        CleanupExistingBalls();
    }
    
    /// <summary>
    /// シーン内の既存のボールを削除
    /// </summary>
    private void CleanupExistingBalls()
    {
        // Sphereという名前のGameObjectを探す
        GameObject[] existingBalls = GameObject.FindObjectsOfType<GameObject>();
        foreach (var obj in existingBalls)
        {
            if (obj.name.Contains("Sphere") && obj != ballPrefab)
            {
                Debug.Log($"BallThrowController: Found existing ball '{obj.name}', removing it");
                Destroy(obj);
            }
        }
    }
    
    private void Start()
    {
        // 初回のボール生成をわずかに遅延させて、重複を防ぐ
        StartCoroutine(CreateInitialBallDelayed());
    }
    
    private System.Collections.IEnumerator CreateInitialBallDelayed()
    {
        // 0.1秒待機して、他の初期化処理が完了するのを待つ
        yield return new WaitForSeconds(0.1f);
        
        // ボールが存在しない場合のみ生成
        if (currentBall == null)
        {
            CreateBall();
            Debug.Log("BallThrowController: Initial ball created");
        }
        else
        {
            Debug.Log("BallThrowController: Ball already exists, skipping initial creation");
        }
    }
    
    private void Update()
    {
        // 自動ボール生成システム
        if (!isGrabbing && currentBall == null && CanThrow)
        {
            // クールダウンが終了し、ボールを持っていない場合は自動生成
            CreateBall();
            Debug.Log("BallThrowController: Ball auto-spawned after cooldown");
        }
        
        // クールダウン中の表示（オプション）
        if (!CanThrow && currentBall == null)
        {
            float remainingTime = ballCooldownTime - (Time.time - lastThrowTime);
            if (remainingTime > 0 && remainingTime < ballCooldownTime)
            {
                // 必要に応じてUIに残り時間を表示
                // Debug.Log($"Next ball in: {remainingTime:F1}s");
            }
        }
    }
    
    private void OnEnable()
    {
        EnableInputActions();
    }
    
    private void OnDisable()
    {
        DisableInputActions();
    }
    
    /// <summary>
    /// Input System の初期化
    /// </summary>
    private void InitializeInputSystem()
    {
        // 直接的なInput Systemの作成（InputActionAssetに依存しない）
        CreateDirectInputActions();
    }
    
    /// <summary>
    /// 直接的なInput Actionの作成
    /// </summary>
    private void CreateDirectInputActions()
    {
        var throwActionMap = new InputActionMap("ThrowBall");
        
        // Grab アクション（ボール掴み）
        grabAction = throwActionMap.AddAction("Grab", InputActionType.Button);
        grabAction.AddBinding("<Mouse>/leftButton");
        grabAction.AddBinding("<Touchscreen>/primaryTouch/press");
        
        // Aim アクション（照準）
        aimAction = throwActionMap.AddAction("Aim", InputActionType.Value);
        aimAction.expectedControlType = "Vector2";
        aimAction.AddBinding("<Mouse>/position");
        aimAction.AddBinding("<Touchscreen>/primaryTouch/position");
        
        throwActionMap.Enable();
        
        Debug.Log("BallThrowController: Input Actions initialized successfully");
    }
    
    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        if (throwCamera == null)
        {
            throwCamera = Camera.main;
            Debug.Log($"BallThrowController: Using main camera: {throwCamera.name}");
        }
            
        if (ballSpawnPoint == null)
        {
            ballSpawnPoint = transform;
            Debug.LogWarning("BallThrowController: BallSpawnPoint not assigned, using transform as fallback");
        }
        else
        {
            Debug.Log($"BallThrowController: BallSpawnPoint assigned: {ballSpawnPoint.name} at position {ballSpawnPoint.position}");
        }
            
        if (trajectoryLine == null)
        {
            trajectoryLine = gameObject.AddComponent<LineRenderer>();
            trajectoryLine.positionCount = trajectoryPoints;
            trajectoryLine.enabled = false;
            Debug.Log("BallThrowController: LineRenderer component added");
        }
    }
    
    /// <summary>
    /// 入力アクションの有効化
    /// </summary>
    private void EnableInputActions()
    {
        if (grabAction != null)
        {
            grabAction.performed += OnGrab;
            grabAction.canceled += OnRelease;
            grabAction.Enable();
        }
        
        if (aimAction != null)
        {
            aimAction.performed += OnAim;
            aimAction.Enable();
        }
    }
    
    /// <summary>
    /// 入力アクションの無効化
    /// </summary>
    private void DisableInputActions()
    {
        if (grabAction != null)
        {
            grabAction.performed -= OnGrab;
            grabAction.canceled -= OnRelease;
            grabAction.Disable();
        }
        
        if (aimAction != null)
        {
            aimAction.performed -= OnAim;
            aimAction.Disable();
        }
    }
    
    /// <summary>
    /// グラブアクション（ボール掴み開始）
    /// </summary>
    private void OnGrab(InputAction.CallbackContext context)
    {
        if (isGrabbing) return;
        if (currentBall == null) 
        {
            Debug.Log("BallThrowController: No ball available to grab");
            return;
        }
        
        Vector2 screenPosition = GetCurrentPointerPosition();
        OnGrabPerformed(screenPosition);
    }
    
    /// <summary>
    /// グラブ実行（テスト用パブリックメソッド）
    /// </summary>
    public void OnGrabPerformed(Vector2 screenPosition)
    {
        if (isGrabbing) return;
        if (currentBall == null) return;
        
        Debug.Log($"BallThrowController: Grab started at screen position {screenPosition}");
        
        isGrabbing = true;
        grabStartPosition = screenPosition;
        currentAimPosition = screenPosition;
        
        // ボールは既に存在するはずなので、軌道表示のみ
        ShowTrajectory(true);
        
        Debug.Log($"BallThrowController: Ball grabbed at {currentBall.transform.position}");
    }
    
    /// <summary>
    /// エイムアクション（照準移動）
    /// </summary>
    private void OnAim(InputAction.CallbackContext context)
    {
        if (!isGrabbing) return;
        
        currentAimPosition = context.ReadValue<Vector2>();
        UpdateTrajectoryDisplay();
    }
    
    /// <summary>
    /// リリースアクション（ボール投げ）
    /// </summary>
    private void OnRelease(InputAction.CallbackContext context)
    {
        if (!isGrabbing) return;
        
        Vector2 releasePosition = GetCurrentPointerPosition();
        Debug.Log($"BallThrowController: OnRelease called at position {releasePosition}");
        OnReleasePerformed(releasePosition);
    }
    
    /// <summary>
    /// リリース実行（テスト用パブリックメソッド）
    /// </summary>
    public void OnReleasePerformed(Vector2 releasePosition)
    {
        if (!isGrabbing) return;
        
        Debug.Log($"BallThrowController: OnReleasePerformed - Start: {grabStartPosition}, End: {releasePosition}");
        
        ThrowBall(grabStartPosition, releasePosition);
        
        isGrabbing = false;
        ShowTrajectory(false);
        
        // 投げた時刻を記録（currentBallはThrowBall内でnullになる）
        lastThrowTime = Time.time;
        Debug.Log($"BallThrowController: Ball thrown. Next ball will auto-spawn in {ballCooldownTime} seconds");
    }
    
    /// <summary>
    /// ボールの生成
    /// </summary>
    private void CreateBall()
    {
        Debug.Log($"BallThrowController: CreateBall called. BallPrefab={ballPrefab != null}, CurrentBall={currentBall != null}, SpawnPoint={ballSpawnPoint != null}");
        
        if (ballPrefab != null && currentBall == null)
        {
            Vector3 spawnPosition = ballSpawnPoint != null ? ballSpawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = ballSpawnPoint != null ? ballSpawnPoint.rotation : Quaternion.identity;
            
            Debug.Log($"BallThrowController: Creating ball at position {spawnPosition}");
            
            currentBall = Instantiate(ballPrefab, spawnPosition, spawnRotation);
            
            // Rigidbodyの設定
            Rigidbody rb = currentBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // 投げるまでは物理演算無効
                rb.useGravity = false;
                Debug.Log("BallThrowController: Ball Rigidbody configured as kinematic");
            }
            else
            {
                Debug.LogWarning("BallThrowController: Ball prefab does not have Rigidbody component!");
            }
        }
        else
        {
            if (ballPrefab == null) Debug.LogError("BallThrowController: Ball prefab is not assigned!");
            if (currentBall != null) Debug.Log("BallThrowController: Ball already exists, skipping creation");
        }
    }
    
    /// <summary>
    /// ボールを投げる
    /// </summary>
    private void ThrowBall(Vector2 startPos, Vector2 endPos)
    {
        if (currentBall == null) 
        {
            Debug.LogError("BallThrowController: ThrowBall called but currentBall is null!");
            return;
        }
        
        Vector3 throwVelocity = CalculateThrowVelocity(startPos, endPos);
        Debug.Log($"BallThrowController: Calculated velocity: {throwVelocity}");
        
        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = throwVelocity;
            Debug.Log($"BallThrowController: Ball thrown with velocity {throwVelocity.magnitude:F2}");
        }
        else
        {
            Debug.LogError("BallThrowController: Ball has no Rigidbody component!");
        }
        
        // 衝突検出のためのコンポーネント追加
        if (currentBall.GetComponent<BallCollisionDetector>() == null)
        {
            var detector = currentBall.AddComponent<BallCollisionDetector>();
            detector.Initialize(this);
        }
        
        // 投げた後、currentBallの参照をクリア（自動削除されるため）
        currentBall = null;
    }
    
    /// <summary>
    /// 投射速度の計算（改良版）
    /// 物理的により正確な弾道計算を実装
    /// </summary>
    public Vector3 CalculateThrowVelocity(Vector2 startPos, Vector2 endPos)
    {
        Vector2 dragVector = endPos - startPos;
        float dragDistance = dragVector.magnitude;
        
        // ドラッグ方向を正規化（Y軸は反転）
        Vector2 dragDirection = dragVector.normalized;
        dragDirection.y = -dragDirection.y; // 画面座標系を物理座標系に合わせる
        
        // 投射力の計算（非線形スケーリング）
        float normalizedDistance = Mathf.Clamp01(dragDistance / 200f); // 200pxを最大ドラッグとする
        float throwPower = Mathf.Pow(normalizedDistance, 1.5f) * maxThrowForce; // 非線形スケーリング
        throwPower = Mathf.Clamp(throwPower, 5f, maxThrowForce);
        
        // カメラの向きを考慮した3D方向計算
        Vector3 cameraForward = throwCamera.transform.forward;
        Vector3 cameraRight = throwCamera.transform.right;
        Vector3 cameraUp = throwCamera.transform.up;
        
        // 画面座標からワールド方向への変換
        Vector3 worldDirection = (cameraRight * dragDirection.x + cameraUp * dragDirection.y + cameraForward).normalized;
        
        // 放物線軌道の計算
        // 目標点を推定（カメラから一定距離の点）
        Vector3 targetPoint = throwCamera.transform.position + worldDirection * 10f;
        
        // 弾道計算: 角度を考慮した初速度
        float launchAngle = Mathf.Clamp(45f + dragDirection.y * 30f, 15f, 75f); // 15°から75°の範囲
        float launchAngleRad = launchAngle * Mathf.Deg2Rad;
        
        Vector3 horizontalDirection = new Vector3(worldDirection.x, 0, worldDirection.z).normalized;
        
        // 物理的に正確な初速度計算
        Vector3 velocity = horizontalDirection * throwPower * Mathf.Cos(launchAngleRad);
        velocity.y = throwPower * Mathf.Sin(launchAngleRad);
        
        return velocity;
    }
    
    /// <summary>
    /// 軌道表示の更新
    /// </summary>
    private void UpdateTrajectoryDisplay()
    {
        if (!isGrabbing || trajectoryLine == null) return;
        
        Vector3 throwVelocity = CalculateThrowVelocity(grabStartPosition, currentAimPosition);
        
        // ボールが存在する場合はボールの位置から、存在しない場合はBallSpawnPointから軌道を表示
        Vector3 startPosition = currentBall != null ? currentBall.transform.position : ballSpawnPoint.position;
        DrawTrajectory(startPosition, throwVelocity);
    }
    
    /// <summary>
    /// 軌道の描画（改良版）
    /// より正確な物理シミュレーションによる軌道予測
    /// </summary>
    private void DrawTrajectory(Vector3 startPos, Vector3 velocity)
    {
        Vector3 currentPos = startPos;
        Vector3 currentVel = velocity;
        int validPoints = 0;
        
        for (int i = 0; i < trajectoryPoints; i++)
        {
            trajectoryLine.SetPosition(i, currentPos);
            validPoints = i + 1;
            
            // 物理シミュレーション（Verlet積分法使用）
            Vector3 acceleration = Physics.gravity;
            
            // 空気抵抗を考慮（簡易版）
            Vector3 drag = -currentVel.normalized * (currentVel.sqrMagnitude * 0.01f);
            acceleration += drag;
            
            currentPos += currentVel * trajectoryTimeStep + 0.5f * acceleration * trajectoryTimeStep * trajectoryTimeStep;
            currentVel += acceleration * trajectoryTimeStep;
            
            // 地面に到達、または範囲外になったら停止
            if (currentPos.y < 0 || Vector3.Distance(startPos, currentPos) > 20f)
            {
                break;
            }
        }
        
        // 残りの点を無効にする
        for (int i = validPoints; i < trajectoryPoints; i++)
        {
            trajectoryLine.SetPosition(i, currentPos);
        }
        
        // 軌道線の視覚設定を改善
        if (trajectoryLine != null)
        {
            trajectoryLine.startWidth = 0.05f;
            trajectoryLine.endWidth = 0.02f;
            trajectoryLine.material = CreateTrajectoryMaterial();
        }
    }
    
    /// <summary>
    /// 軌道線用マテリアルの作成
    /// </summary>
    private Material CreateTrajectoryMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 1f, 0f, 0.7f); // 半透明の黄色
        return mat;
    }
    
    /// <summary>
    /// 軌道表示の切り替え
    /// </summary>
    private void ShowTrajectory(bool show)
    {
        if (trajectoryLine != null)
            trajectoryLine.enabled = show;
    }
    
    /// <summary>
    /// 現在のポインタ位置を取得
    /// </summary>
    private Vector2 GetCurrentPointerPosition()
    {
        if (aimAction != null)
            return aimAction.ReadValue<Vector2>();
        
        // フォールバック
        return Input.mousePosition;
    }
    
    /// <summary>
    /// モンスターヒットのシミュレート（テスト用）
    /// </summary>
    public void SimulateMonsterHit(MonsterController monster)
    {
        Debug.Log($"BallThrowController: SimulateMonsterHit called. Monster={monster != null}, CurrentBall={currentBall != null}");
        
        if (monster != null)
        {
            // currentBallがnullの場合、衝突したボール自体を使用
            GameObject hitBall = currentBall;
            if (hitBall == null)
            {
                // BallCollisionDetectorから呼ばれた場合、そのGameObjectを使用
                BallCollisionDetector detector = monster.GetComponent<BallCollisionDetector>();
                if (detector != null)
                {
                    hitBall = detector.gameObject;
                }
            }
            
            Debug.Log($"BallThrowController: Invoking OnBallHitMonster event with ball={hitBall != null}");
            OnBallHitMonster?.Invoke(hitBall, monster);
        }
    }
    
    /// <summary>
    /// モンスターヒット通知（BallCollisionDetectorから呼ばれる）
    /// </summary>
    public void NotifyMonsterHit(GameObject ball, MonsterController monster)
    {
        Debug.Log($"BallThrowController: NotifyMonsterHit called. Ball={ball != null}, Monster={monster != null}");
        
        if (ball != null && monster != null)
        {
            OnBallHitMonster?.Invoke(ball, monster);
        }
    }
}

/// <summary>
/// ボールの衝突検出用コンポーネント（改良版）
/// より詳細な衝突検出と効果音再生機能を追加
/// </summary>
public class BallCollisionDetector : MonoBehaviour
{
    private BallThrowController controller;
    private bool hasHitTarget = false; // 一度だけヒットするように制御
    private float minHitVelocity = 1f; // 最小ヒット速度
    
    [Header("効果音設定")]
    public AudioClip hitSoundEffect;
    public AudioClip missSound;
    private AudioSource audioSource;
    
    public void Initialize(BallThrowController throwController)
    {
        controller = throwController;
        SetupAudioSource();
    }
    
    private void SetupAudioSource()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHitTarget) return; // 既にヒット済みの場合は無視
        
        float impactVelocity = collision.relativeVelocity.magnitude;
        
        // モンスターとの衝突検出
        MonsterController monster = collision.gameObject.GetComponent<MonsterController>();
        if (monster != null && controller != null && impactVelocity >= minHitVelocity)
        {
            hasHitTarget = true;
            PlayHitEffect();
            
            // ボール自体からヒットを通知
            controller.NotifyMonsterHit(gameObject, monster);
            
            Debug.Log($"ボールがモンスターにヒット! 衝突速度: {impactVelocity:F2}");
        }
        else
        {
            // 地面や他のオブジェクトに衝突
            HandleMissHit(collision);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHitTarget) return;
        
        // トリガーコライダーによる衝突検出（より敏感な検出）
        MonsterController monster = other.GetComponent<MonsterController>();
        if (monster != null && controller != null)
        {
            hasHitTarget = true;
            PlayHitEffect();
            
            // ボール自体からヒットを通知
            controller.NotifyMonsterHit(gameObject, monster);
            
            Debug.Log("ボールがモンスターのトリガーにヒット!");
        }
    }
    
    private void HandleMissHit(Collision collision)
    {
        // 地面や障害物への衝突処理
        if (collision.gameObject.name.Contains("Plane") || collision.gameObject.layer == 0)
        {
            PlayMissEffect();
            Debug.Log("ボールが地面に落下");
            
            // 一定時間後にボールを削除
            Destroy(gameObject, 2f);
        }
    }
    
    private void PlayHitEffect()
    {
        if (audioSource != null && hitSoundEffect != null)
        {
            audioSource.PlayOneShot(hitSoundEffect);
        }
        
        // パーティクル効果の追加（オプション）
        CreateHitParticles();
    }
    
    private void PlayMissEffect()
    {
        if (audioSource != null && missSound != null)
        {
            audioSource.PlayOneShot(missSound);
        }
    }
    
    private void CreateHitParticles()
    {
        // 氷・雪のパーティクルエフェクト
        GameObject particles = new GameObject("IceHitParticles");
        particles.transform.position = transform.position;
        
        ParticleSystem ps = particles.AddComponent<ParticleSystem>();
        
        // メイン設定
        var main = ps.main;
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f); // ランダムな速度
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f); // ランダムなサイズ
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.8f, 0.9f, 1f, 1f),  // 薄い青白色
            new Color(1f, 1f, 1f, 1f)        // 純白
        );
        main.maxParticles = 50;
        main.gravityModifier = 0.5f; // 軽い重力影響
        
        // 形状設定（広がるように）
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        
        // エミッション設定（バースト）
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 30, 50) // 30〜50個のパーティクル
        });
        
        // 速度の減衰
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(-2f); // 内側に向かう力
        
        // サイズの変化（徐々に小さく）
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // 透明度の変化（フェードアウト）
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0.0f), 
                new GradientColorKey(Color.white, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // レンダラー設定（ソフトパーティクル）
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = Color.white;
        
        // パーティクルを自動削除
        Destroy(particles, 3f);
    }
    
    /// <summary>
    /// 衝突検出をリセット（テスト用）
    /// </summary>
    public void ResetHitDetection()
    {
        hasHitTarget = false;
    }
}

}