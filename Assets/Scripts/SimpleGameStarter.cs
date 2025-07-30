using UnityEngine;

namespace MQ
{
    /// <summary>
    /// シンプルなゲーム開始テスト用スクリプト
    /// </summary>
    public class SimpleGameStarter : MonoBehaviour
    {
        [Header("ゲーム開始設定")]
        public bool autoStartOnAwake = false;
        public float startDelay = 1f;
        
        [Header("デバッグ情報")]
        public bool showDebugInfo = true;
        
        private bool gameStarted = false;
        
        private void Start()
        {
            Debug.Log("🎮 SimpleGameStarter - Start() called");
            
            if (autoStartOnAwake)
            {
                Invoke(nameof(StartGame), startDelay);
            }
        }
        
        [ContextMenu("ゲーム開始")]
        public void StartGame()
        {
            if (gameStarted)
            {
                Debug.Log("⚠️ ゲームは既に開始済みです");
                return;
            }
            
            Debug.Log("🎮 まちかどクエスト - ゲーム開始");
            gameStarted = true;
        }
        
        private void Update()
        {
            // Hキーは常に有効（デバッグパネル切替）
            if (IsKeyPressed(UnityEngine.KeyCode.H))
            {
                showDebugInfo = !showDebugInfo;
                Debug.Log($"🎛️ Debug panel: {(showDebugInfo ? "表示" : "非表示")}");
                return;
            }
            
            // ゲーム開始済みの場合は操作を制限
            if (gameStarted)
            {
                if (IsKeyPressed(UnityEngine.KeyCode.R))
                {
                    Debug.Log("🔄 シーンリロード");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                    );
                }
                return;
            }
            
            // ゲーム開始前のみ有効
            if (IsKeyPressed(UnityEngine.KeyCode.Space))
            {
                Debug.Log("🎮 Spaceキーが押されました - ゲーム開始");
                StartGame();
            }
            
            if (IsKeyPressed(UnityEngine.KeyCode.R))
            {
                Debug.Log("🔄 シーンリロード");
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
        }
        
        /// <summary>
        /// Input System対応のキー入力チェック
        /// </summary>
        private bool IsKeyPressed(UnityEngine.KeyCode keyCode)
        {
            #if ENABLE_INPUT_SYSTEM
            // Input System使用時
            switch (keyCode)
            {
                case UnityEngine.KeyCode.Space:
                    return UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame == true;
                case UnityEngine.KeyCode.R:
                    return UnityEngine.InputSystem.Keyboard.current?.rKey.wasPressedThisFrame == true;
                case UnityEngine.KeyCode.H:
                    return UnityEngine.InputSystem.Keyboard.current?.hKey.wasPressedThisFrame == true;
                default:
                    return false;
            }
            #else
            // 従来のInput Manager使用時
            return Input.GetKeyDown(keyCode);
            #endif
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            GUILayout.Label("デバッグパネル");
            GUILayout.Label($"ゲーム状態: {(gameStarted ? "開始済み" : "未開始")}");
            GUILayout.Label("操作方法:");
            GUILayout.Label("Space: ゲーム開始");
            GUILayout.Label("R: シーンリロード");
            GUILayout.Label("H: デバッグパネル切替");
            GUILayout.EndArea();
        }
    }
}