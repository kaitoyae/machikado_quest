using UnityEngine;

public class TouchTester : MonoBehaviour
{
    void Update()
    {
        if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsTouching)
        {
            Debug.Log($"Touch Position: {MobileInputManager.Instance.TouchPosition}");
            Debug.Log($"Is touching UI: {MobileInputManager.Instance.IsTouchingUI()}");
        }
    }
}