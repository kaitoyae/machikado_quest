using UnityEngine;

/// <summary>
/// Rigidbodyへの全ての干渉を監視・記録するスクリプト
/// </summary>
public class RigidbodyMonitor : MonoBehaviour
{
    private Rigidbody targetRigidbody;
    private Vector3 lastVelocity;
    private Vector3 lastPosition;
    
    void Start()
    {
        targetRigidbody = GetComponent<Rigidbody>();
        if (targetRigidbody != null)
        {
            lastVelocity = targetRigidbody.linearVelocity;
            lastPosition = targetRigidbody.position;
            Debug.Log($"[RIGIDBODY_MONITOR] 監視開始 - Initial Velocity: {lastVelocity}, Position: {lastPosition}");
        }
    }
    
    void FixedUpdate()
    {
        if (targetRigidbody == null) return;
        
        Vector3 currentVelocity = targetRigidbody.linearVelocity;
        Vector3 currentPosition = targetRigidbody.position;
        
        // 速度が変更された場合
        if (currentVelocity != lastVelocity)
        {
            Debug.Log($"[RIGIDBODY_MONITOR] 速度変更検出！ {lastVelocity} → {currentVelocity}");
            
            // スタックトレースで変更元を特定
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            Debug.Log($"[VELOCITY_CHANGE_STACK]\n{stackTrace}");
            
            lastVelocity = currentVelocity;
        }
        
        // 位置が変更された場合  
        if (currentPosition != lastPosition)
        {
            Debug.Log($"[RIGIDBODY_MONITOR] 位置変更検出！ {lastPosition} → {currentPosition}");
            
            // スタックトレースで変更元を特定
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            Debug.Log($"[POSITION_CHANGE_STACK]\n{stackTrace}");
            
            lastPosition = currentPosition;
        }
    }
    
    void LateUpdate()
    {
        if (targetRigidbody == null) return;
        
        Vector3 currentVelocity = targetRigidbody.linearVelocity;
        Vector3 currentPosition = targetRigidbody.position;
        
        // LateUpdateでの変更もチェック
        if (currentVelocity != lastVelocity)
        {
            Debug.Log($"[RIGIDBODY_MONITOR] LateUpdate速度変更！ {lastVelocity} → {currentVelocity}");
            
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            Debug.Log($"[LATEUPDATE_VELOCITY_STACK]\n{stackTrace}");
            
            lastVelocity = currentVelocity;
        }
        
        if (currentPosition != lastPosition)
        {
            Debug.Log($"[RIGIDBODY_MONITOR] LateUpdate位置変更！ {lastPosition} → {currentPosition}");
            
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            Debug.Log($"[LATEUPDATE_POSITION_STACK]\n{stackTrace}");
            
            lastPosition = currentPosition;
        }
    }
}