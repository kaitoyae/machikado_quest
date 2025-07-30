using UnityEngine;

public class DiagnoseController : MonoBehaviour
{
    void Awake()
    {
        Debug.Log($"[DIAGNOSE] ===== 診断開始 on {gameObject.name} =====");
        Debug.Log($"[DIAGNOSE] Position: {transform.position}");
        Debug.Log($"[DIAGNOSE] Active: {gameObject.activeInHierarchy}");
        
        // 全コンポーネントを調査
        Component[] components = GetComponents<Component>();
        Debug.Log($"[DIAGNOSE] コンポーネント数: {components.Length}");
        foreach (var comp in components)
        {
            Debug.Log($"[DIAGNOSE] コンポーネント: {comp.GetType().Name}, Enabled: {(comp as MonoBehaviour)?.enabled ?? true}");
        }
        
        // Rigidbodyの確認
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"[DIAGNOSE] Rigidbody: Position={rb.position}, Kinematic={rb.isKinematic}, UseGravity={rb.useGravity}");
        }
        else
        {
            Debug.Log($"[DIAGNOSE] Rigidbody: 見つかりません");
        }
        
        // CharacterControllerの確認
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            Debug.Log($"[DIAGNOSE] CharacterController: Enabled={cc.enabled}, Height={cc.height}");
        }
        else
        {
            Debug.Log($"[DIAGNOSE] CharacterController: 見つかりません");
        }
        
        // CapsuleColliderの確認
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            Debug.Log($"[DIAGNOSE] CapsuleCollider: Enabled={capsule.enabled}, Height={capsule.height}");
        }
        else
        {
            Debug.Log($"[DIAGNOSE] CapsuleCollider: 見つかりません");
        }
    }
    
    void Start()
    {
        Debug.Log($"[DIAGNOSE] ===== Start診断 on {gameObject.name} =====");
        
        // 周辺の地面オブジェクトを探索
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 10f);
        Debug.Log($"[DIAGNOSE] 周辺Collider数: {nearbyColliders.Length}");
        
        foreach (var col in nearbyColliders)
        {
            Debug.Log($"[DIAGNOSE] 発見Collider: {col.name}, Layer={col.gameObject.layer}({LayerMask.LayerToName(col.gameObject.layer)}), Position={col.transform.position}");
        }
        
        // 全レイヤーで下向きレイキャスト
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 2f;
        bool hitFound = Physics.Raycast(rayStart, Vector3.down, out hit, 10f);
        
        Debug.Log($"[DIAGNOSE] Raycast結果: Hit={hitFound}");
        if (hitFound)
        {
            Debug.Log($"[DIAGNOSE] Hit: {hit.collider.name}, Layer={hit.collider.gameObject.layer}, Distance={hit.distance}");
        }
    }
}