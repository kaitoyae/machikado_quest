using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// UI操作統一管理システム
/// シーン切り替え、設定パネル管理、Input System連携
/// 移動システムから完全分離
/// </summary>
public class UIActionManager : MonoBehaviour
{
    [Header("Scene Management")]
    [Tooltip("ホーム画面シーン名")]
    public string homeSceneName = "HomeScreen";
    [Tooltip("マップシーン名")]
    public string mapSceneName = "Map";
    [Tooltip("カードバトルシーン名")]
    public string cardBattleSceneName = "CardBattleScene";

    [Header("UI References")]
    [Tooltip("ホームボタン")]
    public Button homeButton;
    [Tooltip("マップボタン")]
    public Button mapButton;
    [Tooltip("カードバトルボタン")]
    public Button cardBattleButton;
    [Tooltip("設定パネル")]
    public GameObject settingsPanel;
    [Tooltip("設定ボタン")]
    public Button settingsButton;

    [Header("Input System Integration")]
    [Tooltip("Input Systemとの連携を有効にする")]
    public bool enableInputSystemIntegration = true;

    [Header("Mobile Gesture Support")]
    [Tooltip("モバイルジェスチャー対応")]
    public bool enableMobileGestures = true;
    [Tooltip("設定パネル用スワイプしきい値")]
    public float swipeThreshold = 100f;

    // 内部状態
    private bool isSettingsPanelOpen = false;
    private PlayerInput playerInput;
    private Vector2 touchStartPosition;
    private bool isSwipeGesture = false;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        SetupUIButtons();
        SetupInputSystem();
    }

    void Update()
    {
        if (enableMobileGestures && Application.isMobilePlatform)
        {
            HandleMobileGestures();
        }
    }

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponents()
    {
        if (enableInputSystemIntegration)
        {
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                playerInput = FindObjectOfType<PlayerInput>();
            }
        }
    }

    /// <summary>
    /// UIボタン設定
    /// </summary>
    private void SetupUIButtons()
    {
        // ホームボタン
        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(() => LoadScene(homeSceneName));
        }

        // マップボタン
        if (mapButton != null)
        {
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(() => LoadScene(mapSceneName));
        }

        // カードバトルボタン
        if (cardBattleButton != null)
        {
            cardBattleButton.onClick.RemoveAllListeners();
            cardBattleButton.onClick.AddListener(() => LoadScene(cardBattleSceneName));
        }

        // 設定ボタン
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
        }

        // 設定パネル初期状態
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isSettingsPanelOpen = false;
        }
    }

    /// <summary>
    /// Input System設定
    /// </summary>
    private void SetupInputSystem()
    {
        if (!enableInputSystemIntegration || playerInput == null) return;

        Debug.Log("[UIActionManager] Input System連携開始");
    }

    /// <summary>
    /// シーン切り替え
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"[UIActionManager] 無効なシーン名: {sceneName}");
            return;
        }

        Debug.Log($"[UIActionManager] シーン切り替え: {sceneName}");
        
        // 設定保存
        SaveSettings();
        
        // シーン読み込み
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 設定パネル切り替え
    /// </summary>
    public void ToggleSettingsPanel()
    {
        if (settingsPanel == null) return;

        isSettingsPanelOpen = !isSettingsPanelOpen;
        settingsPanel.SetActive(isSettingsPanelOpen);

        Debug.Log($"[UIActionManager] 設定パネル: {(isSettingsPanelOpen ? "開く" : "閉じる")}");
    }

    /// <summary>
    /// 他スクリプト用UI操作API
    /// </summary>
    public void TriggerUIAction(string actionName)
    {
        switch (actionName.ToLower())
        {
            case "home":
                LoadScene(homeSceneName);
                break;
            case "map":
                LoadScene(mapSceneName);
                break;
            case "cardbattle":
                LoadScene(cardBattleSceneName);
                break;
            case "settings":
                ToggleSettingsPanel();
                break;
            default:
                Debug.LogWarning($"[UIActionManager] 不明なアクション: {actionName}");
                break;
        }
    }

    /// <summary>
    /// モバイルジェスチャー処理
    /// </summary>
    private void HandleMobileGestures()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case UnityEngine.TouchPhase.Began:
                    touchStartPosition = touch.position;
                    isSwipeGesture = false;
                    break;

                case UnityEngine.TouchPhase.Moved:
                    Vector2 swipeDelta = touch.position - touchStartPosition;
                    if (swipeDelta.magnitude > swipeThreshold && !isSwipeGesture)
                    {
                        isSwipeGesture = true;
                        HandleSwipeGesture(swipeDelta);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// スワイプジェスチャー処理
    /// </summary>
    private void HandleSwipeGesture(Vector2 swipeDelta)
    {
        // 上方向スワイプで設定パネル表示
        if (swipeDelta.y > swipeThreshold && Mathf.Abs(swipeDelta.x) < swipeDelta.y)
        {
            if (!isSettingsPanelOpen)
            {
                ToggleSettingsPanel();
            }
        }
        // 下方向スワイプで設定パネル非表示
        else if (swipeDelta.y < -swipeThreshold && Mathf.Abs(swipeDelta.x) < Mathf.Abs(swipeDelta.y))
        {
            if (isSettingsPanelOpen)
            {
                ToggleSettingsPanel();
            }
        }
    }

    /// <summary>
    /// 設定保存
    /// </summary>
    private void SaveSettings()
    {
        // PlayerPrefsで設定保存
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 設定読み込み
    /// </summary>
    private void LoadSettings()
    {
        // 必要に応じて設定復元
        string lastScene = PlayerPrefs.GetString("LastScene", "");
        Debug.Log($"[UIActionManager] 前回のシーン: {lastScene}");
    }

    // Input System イベントハンドラー
    public void OnUIHome(InputValue value)
    {
        if (value.isPressed)
        {
            LoadScene(homeSceneName);
        }
    }

    public void OnUIMap(InputValue value)
    {
        if (value.isPressed)
        {
            LoadScene(mapSceneName);
        }
    }

    public void OnUISettings(InputValue value)
    {
        if (value.isPressed)
        {
            ToggleSettingsPanel();
        }
    }

    // デバッグ情報
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(10, 220, 300, 150));
        GUILayout.Label("=== UI Action Manager ===");
        GUILayout.Label($"Current Scene: {SceneManager.GetActiveScene().name}");
        GUILayout.Label($"Settings Panel: {(isSettingsPanelOpen ? "Open" : "Closed")}");
        GUILayout.Label($"Mobile Gestures: {enableMobileGestures}");
        GUILayout.Label($"Input System: {enableInputSystemIntegration}");
        GUILayout.EndArea();
    }
}