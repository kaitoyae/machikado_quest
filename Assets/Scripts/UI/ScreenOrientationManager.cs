using UnityEngine;
using System.Collections;

public class ScreenOrientationManager : MonoBehaviour
{
    [SerializeField] private ScreenOrientation desiredOrientation = ScreenOrientation.Portrait;
    
    private void Awake()
    {
        Debug.Log($"ScreenOrientationManager Awake - Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        StartCoroutine(SetOrientationDelayed());
    }
    
    private void Start()
    {
        Debug.Log($"ScreenOrientationManager Start - Desired: {desiredOrientation}");
        SetOrientation();
    }
    
    private IEnumerator SetOrientationDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        SetOrientation();
        yield return new WaitForSeconds(0.5f);
        SetOrientation();
    }
    
    private void SetOrientation()
    {
        Debug.Log($"Setting orientation to: {desiredOrientation}, Current: {Screen.orientation}");
        
#if UNITY_EDITOR
        // Editorでの動作はリフレクションで対応
        UnityEditor.EditorApplication.delayCall += () =>
        {
            SetGameViewSize();
        };
#else
        // 実機での動作
        // まず自動回転を無効化
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        
        // 画面の向きを設定
        Screen.orientation = desiredOrientation;
#endif
        
        // デバッグ情報
        Debug.Log($"After setting - Current orientation: {Screen.orientation}");
        Debug.Log($"AutoRotate settings - Portrait: {Screen.autorotateToPortrait}, Landscape: {Screen.autorotateToLandscapeLeft}");
    }
    
#if UNITY_EDITOR
    private void SetGameViewSize()
    {
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Vector2 currentSize = (Vector2)GetSizeOfMainGameView.Invoke(null, null);
        
        bool isPortrait = desiredOrientation == ScreenOrientation.Portrait || desiredOrientation == ScreenOrientation.PortraitUpsideDown;
        bool needsSwap = (isPortrait && currentSize.x > currentSize.y) || (!isPortrait && currentSize.x < currentSize.y);
        
        if (needsSwap)
        {
            Debug.Log($"Simulating orientation change in Editor - Portrait: {isPortrait}");
        }
    }
#endif
}