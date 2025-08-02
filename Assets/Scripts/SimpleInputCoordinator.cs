using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// シンプル統合入力管理システム
/// プラットフォーム自動検出とStarterAssets互換性
/// </summary>
public class SimpleInputCoordinator : MonoBehaviour
{
    [Header("Platform Auto-Detection")]
    public bool autoDetectPlatform = true;

    [Header("StarterAssets Compatibility")]
    public Vector2 move;
    public Vector2 look;
    public bool sprint;

    // プラットフォーム状態
    public bool IsMobileDevice { get; private set; }
    public string CurrentInputSource { get; private set; } = "None";

    void Awake()
    {
        DetectPlatform();
    }

    void Update()
    {
        UpdateInputSource();
    }

    private void DetectPlatform()
    {
        if (!autoDetectPlatform) return;
        
        IsMobileDevice = Application.isMobilePlatform;
        Debug.Log($"[SimpleInputCoordinator] Platform: {(IsMobileDevice ? "Mobile" : "PC")}");
    }

    private void UpdateInputSource()
    {
        CurrentInputSource = IsMobileDevice ? "Mobile" : "PC";
    }

    // Input System Events
    public void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        look = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        sprint = value.isPressed;
    }

    // Debug Display
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 120));
        GUILayout.Label("=== Input System ===");
        GUILayout.Label($"Platform: {(IsMobileDevice ? "Mobile" : "PC")}");
        GUILayout.Label($"Source: {CurrentInputSource}");
        GUILayout.Label($"Move: {move}");
        GUILayout.Label($"Look: {look}");
        GUILayout.Label($"Sprint: {sprint}");
        GUILayout.EndArea();
    }
}