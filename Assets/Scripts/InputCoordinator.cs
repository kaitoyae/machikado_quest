using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 統合入力管理システム - プラットフォーム自動検出
/// PC: WASD + Mouse (Input System)
/// Mobile: Virtual Joystick + GPS + Touch Camera
/// 既存StarterAssetsInputsとの完全互換性
/// </summary>
public class InputCoordinator : MonoBehaviour
{
    [Header("Platform Auto-Detection")]
    [Tooltip("プラットフォームを自動検出する")]
    public bool autoDetectPlatform = true;

    [Header("Existing Components Integration")]
    [Tooltip("GPS移動制御コンポーネント")]
    public MonoBehaviour gpsController;  // CharacterGPSCompassController用
    [Tooltip("モバイル用バーチャルジョイスティック")]
    public UIVirtualJoystick virtualJoystick;

    [Header("Input Sensitivity")]
    [Tooltip("PC用マウス感度")]
    public float mouseSensitivity = 2f;
    [Tooltip("モバイル用タッチ感度")]
    public float touchSensitivity = 1f;

    // 既存StarterAssetsInputs互換性プロパティ
    [Header("StarterAssets Compatibility")]
    public Vector2 move;
    public Vector2 look;
    public bool sprint;

    // プラットフォーム状態
    public bool IsMobileDevice { get; private set; }
    public string CurrentInputSource { get; private set; } = "None";

    // 内部状態
    private PlayerInput playerInput;
    private bool isGPSMovementActive = false;
    private Vector2 gpsMovementInput = Vector2.zero;
    private Vector2 joystickInput = Vector2.zero;

    void Awake()
    {
        DetectPlatformAndSetup();
    }

    void Start()
    {
        InitializeComponents();
    }

    void Update()
    {
        if (IsMobileDevice)
        {
            UpdateMobileInputs();
        }
        else
        {
            UpdatePCInputs();
        }
    }

    /// <summary>
    /// プラットフォーム検出と初期設定
    /// </summary>
    private void DetectPlatformAndSetup()
    {
        if (!autoDetectPlatform) return;

        IsMobileDevice = Application.isMobilePlatform;
        
        Debug.Log($"[InputCoordinator] プラットフォーム検出: {(IsMobileDevice ? "Mobile" : "PC")}");
    }

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponents()
    {
        // PlayerInput取得
        playerInput = GetComponent<PlayerInput>();
        
        // GPS Controller検索（型名で検索）
        if (gpsController == null)
        {
            var gpsComponents = FindObjectsOfType<MonoBehaviour>();
            foreach (var component in gpsComponents)
            {
                if (component.GetType().Name == "CharacterGPSCompassController")
                {
                    gpsController = component;
                    break;
                }
            }
        }
            
        // Virtual Joystick検索
        if (virtualJoystick == null)
            virtualJoystick = FindObjectOfType<UIVirtualJoystick>();

        // モバイル専用初期化
        if (IsMobileDevice && virtualJoystick != null)
        {
            virtualJoystick.joystickOutputEvent.AddListener(OnVirtualJoystickInput);
        }

        Debug.Log($"[InputCoordinator] 初期化完了 - GPS:{gpsController != null}, Joystick:{virtualJoystick != null}");
    }

    /// <summary>
    /// PC入力更新
    /// </summary>
    private void UpdatePCInputs()
    {
        CurrentInputSource = "Keyboard+Mouse";
        // Input Systemイベントで自動更新されるため、ここでは特別な処理不要
    }

    /// <summary>
    /// モバイル入力更新（GPS優先）
    /// </summary>
    private void UpdateMobileInputs()
    {
        MonitorGPSMovement();
        
        // 優先度: GPS > Virtual Joystick
        if (isGPSMovementActive)
        {
            move = gpsMovementInput;
            CurrentInputSource = "GPS";
        }
        else
        {
            move = joystickInput;
            CurrentInputSource = "Virtual Joystick";
        }
    }

    /// <summary>
    /// GPS移動監視
    /// </summary>
    private void MonitorGPSMovement()
    {
        if (gpsController == null) return;

        // GPS移動が有効で実際に移動している場合
        if (gpsController != null)
        {
            // ReflectionでGPSコントローラーのプロパティをチェック
            var enableGPSField = gpsController.GetType().GetField("enableGPSMovement");
            var isMovingProperty = gpsController.GetType().GetProperty("IsMoving");
            
            bool enableGPS = enableGPSField != null ? (bool)enableGPSField.GetValue(gpsController) : false;
            bool isMoving = isMovingProperty != null ? (bool)isMovingProperty.GetValue(gpsController) : false;
            
            isGPSMovementActive = enableGPS && isMoving;
        }
        else
        {
            isGPSMovementActive = false;
        }
        
        if (isGPSMovementActive)
        {
            // GPS移動ベクトルを計算（簡易版）
            var targetProperty = gpsController.GetType().GetProperty("Target");
            if (targetProperty != null)
            {
                Vector3 targetPosition = (Vector3)targetProperty.GetValue(gpsController);
                Vector3 currentPosition = transform.position;
                Vector3 direction = (targetPosition - currentPosition).normalized;
                
                gpsMovementInput = new Vector2(direction.x, direction.z);
            }
        }
        else
        {
            gpsMovementInput = Vector2.zero;
        }
    }

    // Input System イベントハンドラー
    public void OnMove(InputValue value)
    {
        if (!IsMobileDevice)
        {
            move = value.Get<Vector2>();
        }
    }

    public void OnLook(InputValue value)
    {
        if (!IsMobileDevice)
        {
            look = value.Get<Vector2>() * mouseSensitivity;
        }
    }

    public void OnSprint(InputValue value)
    {
        sprint = value.isPressed;
    }

    // Virtual Joystick イベントハンドラー
    private void OnVirtualJoystickInput(Vector2 input)
    {
        joystickInput = input;
    }

    // デバッグ情報
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Platform: {(IsMobileDevice ? "Mobile" : "PC")}");
        GUILayout.Label($"Input Source: {CurrentInputSource}");
        GUILayout.Label($"Move: {move}");
        GUILayout.Label($"Look: {look}");
        GUILayout.Label($"Sprint: {sprint}");
        GUILayout.Label($"GPS Active: {isGPSMovementActive}");
        GUILayout.EndArea();
    }
}