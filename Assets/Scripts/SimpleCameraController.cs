using UnityEngine;
// Cinemachineサポートを一時的に無効化（エラー回避のため）
// Cinemachineパッケージインストール後に有効化してください
//#if CINEMACHINE_PRESENT
//using Cinemachine;
//#endif

/// <summary>
/// シンプルカメラ制御システム
/// プラットフォーム対応（PC: マウス、Mobile: タッチ）
/// 既存Cinemachine FreeLookとの連携
/// </summary>
public class SimpleCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("PC用マウス感度")]
    public float mouseSensitivity = 2f;
    [Tooltip("モバイル用タッチ感度")]
    public float touchSensitivity = 1f;

    [Header("Existing Compatibility")]
    [Tooltip("上方向クランプ角度")]
    public float TopClamp = 70.0f;
    [Tooltip("下方向クランプ角度")]
    public float BottomClamp = -30.0f;

    [Header("Touch Settings")]
    [Tooltip("タッチ感度調整")]
    public float touchSensitivityMultiplier = 0.5f;
    [Tooltip("慣性減衰")]
    public float touchDamping = 0.1f;

    // コンポーネント参照
    private InputCoordinator inputCoordinator;
    private Camera playerCamera;
//#if CINEMACHINE_PRESENT
//    private CinemachineFreeLook freeLookCamera;
//#endif
    
    // 内部状態
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;
    private Vector2 lastTouchPosition;
    private Vector2 touchVelocity;
    private bool isTouching = false;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        FindCinemachineCamera();
        SetInitialValues();
    }

    void LateUpdate()
    {
        HandleCameraRotation();
    }

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponents()
    {
        inputCoordinator = GetComponent<InputCoordinator>();
        if (inputCoordinator == null)
        {
            inputCoordinator = FindObjectOfType<InputCoordinator>();
        }

        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
    }

    /// <summary>
    /// Cinemachineカメラ検索
    /// </summary>
    private void FindCinemachineCamera()
    {
//#if CINEMACHINE_PRESENT
//        freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
//        
//        if (freeLookCamera != null)
//        {
//            Debug.Log("[SimpleCameraController] Cinemachine FreeLook発見、連携開始");
//        }
//        else
//        {
//            Debug.LogWarning("[SimpleCameraController] Cinemachine FreeLookが見つかりません");
//        }
//#else
        Debug.LogWarning("[SimpleCameraController] Cinemachineパッケージがインストールされていません");
//#endif
    }

    /// <summary>
    /// 初期値設定
    /// </summary>
    private void SetInitialValues()
    {
        cinemachineTargetYaw = transform.rotation.eulerAngles.y;
        cinemachineTargetPitch = transform.rotation.eulerAngles.x;
    }

    /// <summary>
    /// プラットフォーム別カメラ回転処理
    /// </summary>
    private void HandleCameraRotation()
    {
        if (inputCoordinator == null) return;

        Vector2 lookInput = inputCoordinator.look;

        if (inputCoordinator.IsMobileDevice)
        {
            HandleMobileCameraInput(lookInput);
        }
        else
        {
            HandlePCCameraInput(lookInput);
        }

        UpdateCinemachineCamera();
    }

    /// <summary>
    /// PC用カメラ入力処理
    /// </summary>
    private void HandlePCCameraInput(Vector2 lookInput)
    {
        if (lookInput.sqrMagnitude >= 0.01f)
        {
            cinemachineTargetYaw += lookInput.x * mouseSensitivity;
            cinemachineTargetPitch += lookInput.y * mouseSensitivity;
        }
    }

    /// <summary>
    /// モバイル用カメラ入力処理
    /// </summary>
    private void HandleMobileCameraInput(Vector2 lookInput)
    {
        // タッチ入力処理
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case UnityEngine.TouchPhase.Began:
                    isTouching = true;
                    lastTouchPosition = touch.position;
                    touchVelocity = Vector2.zero;
                    break;
                    
                case UnityEngine.TouchPhase.Moved:
                    if (isTouching)
                    {
                        Vector2 deltaPosition = touch.position - lastTouchPosition;
                        touchVelocity = deltaPosition * touchSensitivity * touchSensitivityMultiplier;
                        
                        cinemachineTargetYaw += touchVelocity.x;
                        cinemachineTargetPitch -= touchVelocity.y; // 反転
                        
                        lastTouchPosition = touch.position;
                    }
                    break;
                    
                case UnityEngine.TouchPhase.Ended:
                case UnityEngine.TouchPhase.Canceled:
                    isTouching = false;
                    break;
            }
        }
        else
        {
            // 慣性減衰
            touchVelocity = Vector2.Lerp(touchVelocity, Vector2.zero, touchDamping);
            
            cinemachineTargetYaw += touchVelocity.x;
            cinemachineTargetPitch -= touchVelocity.y;
        }
    }

    /// <summary>
    /// Cinemachineカメラ更新
    /// </summary>
    private void UpdateCinemachineCamera()
    {
        // ピッチクランプ
        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

//#if CINEMACHINE_PRESENT
//        // FreeLookカメラがある場合は連携
//        if (freeLookCamera != null)
//        {
//            // Cinemachine FreeLookの軸値を直接制御
//            freeLookCamera.m_XAxis.Value = cinemachineTargetYaw;
//            freeLookCamera.m_YAxis.Value = Mathf.InverseLerp(BottomClamp, TopClamp, cinemachineTargetPitch);
//        }
//        else
//#endif
        {
            // 直接カメラ回転
            transform.rotation = Quaternion.Euler(cinemachineTargetPitch, cinemachineTargetYaw, 0.0f);
        }
    }

    /// <summary>
    /// 角度クランプユーティリティ
    /// </summary>
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    // デバッグ情報
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(640, 10, 300, 150));
        GUILayout.Label("=== Camera Controller ===");
        GUILayout.Label($"Platform: {(inputCoordinator?.IsMobileDevice == true ? "Mobile" : "PC")}");
        GUILayout.Label($"Yaw: {cinemachineTargetYaw:F1}°");
        GUILayout.Label($"Pitch: {cinemachineTargetPitch:F1}°");
        GUILayout.Label($"Touch Active: {isTouching}");
//#if CINEMACHINE_PRESENT
//        GUILayout.Label($"FreeLook: {(freeLookCamera != null ? "Connected" : "None")}");
//#else
        GUILayout.Label("FreeLook: Not Available");
//#endif
        GUILayout.EndArea();
    }
}