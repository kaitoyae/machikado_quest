using UnityEngine;

/// <summary>
/// Rigidbodyの重力機能が正しく動作するかを検証するための分離テスト用スクリプト。
/// このコンポーネントは、アタッチされたGameObject上の他の全てのMonoBehaviourを無効化し、
/// 純粋な物理環境でオブジェクトが落下するかどうかをテストします。
/// </summary>
public class GravityIntegrityTest : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        Debug.LogWarning("--- GRAVITY INTEGRITY TEST RUNNING ---");
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("TEST FAILED: Rigidbody component not found.");
            enabled = false; // Rigidbodyがなければこのスクリプトも停止
            return;
        }

        // --- テストのためにRigidbodyの設定を強制的に上書き ---
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None; // テスト中は全ての位置・回転の固定を解除
        rb.linearDamping = 0;
        rb.angularDamping = 0.05f;
        
        // Rigidbodyを強制的にスリープから復帰させる
        if (rb.IsSleeping())
        {
            rb.WakeUp();
        }

        Debug.Log($"[GRAVITY_TEST] Initial Test Settings Overwritten: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}, constraints={rb.constraints}");

        // --- このGameObject上の他のスクリプトを全て無効化 ---
        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts)
        {
            // このテストスクリプト自体は無効化しない
            if (script == this) continue;

            Debug.LogWarning($"[GRAVITY_TEST] Disabling component for isolation test: {script.GetType().Name}");
            script.enabled = false;
        }
        Debug.LogWarning("--- All other scripts on this GameObject have been disabled. Running pure physics test. ---");
    }

    void FixedUpdate()
    {
        // Y座標とY軸方向の速度を毎フレーム記録し、重力が作用しているかを確認
        Debug.Log($"[GRAVITY_TEST] Time: {Time.time:F2}s, Y Position: {transform.position.y:F3}, Y Velocity: {rb.linearVelocity.y:F3}");
    }
}
