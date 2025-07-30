using UnityEngine;
using System.Collections;
using packt.FoodyGO.Mapping;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace packt.FoodyGO.Controllers
{
    public class MonsterController : MonoBehaviour
    {
        public MapLocation location;
        private bool isCaptured = false;
        public float animationSpeed = 1.0f;
        private Animator animator;

        // Use this for initialization
        void Start()
        {
            Debug.Log($"MonsterController: Start() called on {gameObject.name}");
            
            // アニメーターの取得
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            
            // コライダーを追加（タップ検出用）
            if (GetComponent<Collider>() == null)
            {
                SphereCollider collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 1f;
                collider.isTrigger = true;
                Debug.Log($"MonsterController: Added SphereCollider to {gameObject.name}");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (isCaptured) return;

#if UNITY_EDITOR
            // エディタ専用: Cキーでテスト捕獲
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                Debug.Log($"MonsterController: C key pressed on {gameObject.name}");
                CaptureMonster();
            }
#else
            // 実機: タップ検出
            if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsTouching)
            {
                // タッチ開始時のみ処理
                if (!MobileInputManager.Instance.IsTouchingUI())
                {
                    CheckTap();
                }
            }
#endif
        }

#if UNITY_EDITOR
        private void CheckMouseClick()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    CaptureMonster();
                }
            }
        }
#endif

        private void CheckTap()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector2 touchPos = MobileInputManager.Instance.TouchPosition;
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(touchPos.x, touchPos.y, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    CaptureMonster();
                }
            }
        }

        private void CaptureMonster()
        {
            if (isCaptured) return;
            
            isCaptured = true;
#if UNITY_EDITOR
            Debug.Log($"Monster captured! (エディタテスト用: {gameObject.name})");
#else
            Debug.Log("Monster captured!");
#endif
            // Catchシーンに遷移
            SceneManager.LoadScene("Catch");
        }
    }
}
