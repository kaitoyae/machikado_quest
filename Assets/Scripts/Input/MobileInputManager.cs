using UnityEngine;
using UnityEngine.InputSystem;

public class MobileInputManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static MobileInputManager Instance { get; private set; }
    
    // 入力情報
    public bool IsTouching { get; private set; }
    public Vector2 TouchPosition { get; private set; }
    public Vector2 TouchDelta { get; private set; }
    
    private Vector2 lastTouchPosition;
    
    void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        bool wasTouching = IsTouching;
        Vector2 currentPosition = Vector2.zero;
        
#if UNITY_EDITOR || UNITY_STANDALONE
        // PCでの入力（マウス）
        IsTouching = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (Mouse.current != null)
        {
            currentPosition = Mouse.current.position.ReadValue();
        }
#else
        // モバイルでの入力（タッチ）
        var touchscreen = Touchscreen.current;
        IsTouching = touchscreen != null && touchscreen.primaryTouch.press.isPressed;
        if (IsTouching && touchscreen != null)
        {
            currentPosition = touchscreen.primaryTouch.position.ReadValue();
        }
#endif
        
        // タッチ位置の更新
        if (IsTouching)
        {
            TouchPosition = currentPosition;
            
            // タッチ開始時
            if (!wasTouching)
            {
                lastTouchPosition = currentPosition;
                TouchDelta = Vector2.zero;
            }
            else
            {
                // タッチ中のデルタ計算
                TouchDelta = currentPosition - lastTouchPosition;
                lastTouchPosition = currentPosition;
            }
        }
        else
        {
            TouchDelta = Vector2.zero;
        }
    }
    
    // ワールド座標でのタッチ位置を取得
    public Vector3 GetTouchWorldPosition(Camera camera)
    {
        if (camera == null) return Vector3.zero;
        
        Vector3 screenPos = new Vector3(TouchPosition.x, TouchPosition.y, camera.nearClipPlane);
        return camera.ScreenToWorldPoint(screenPos);
    }
    
    // UI上のタッチかどうかを判定
    public bool IsTouchingUI()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
#else
        var touchscreen = Touchscreen.current;
        return UnityEngine.EventSystems.EventSystem.current != null && 
               touchscreen != null && 
               touchscreen.primaryTouch.press.isPressed && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject((int)touchscreen.primaryTouch.touchId.ReadValue());
#endif
    }
}