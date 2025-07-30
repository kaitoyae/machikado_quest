using UnityEngine;
using StarterAssets;

/// <summary>
/// 滑らかな移動と歩行アニメーションのためのコントローラー
/// ポケモンGOスタイルの動きを実現
/// </summary>
public class SmoothMovementController : MonoBehaviour
{
    [Header("コンポーネント参照")]
    [Tooltip("ThirdPersonControllerコンポーネントへの参照")]
    public StarterAssets.ThirdPersonController characterController;
    
    [Tooltip("Animatorコンポーネントへの参照")]
    public Animator animator;
    
    [Header("移動補間設定")]
    [Tooltip("位置の移動をより滑らかにするための補間係数")]
    [Range(0.1f, 10f)]
    public float positionSmoothFactor = 5f;
    
    [Tooltip("位置の移動の最大速度")]
    public float maxMoveSpeed = 10f;
    
    [Header("アニメーション設定")]
    [Tooltip("歩行アニメーションの最小閾値")]
    [Range(0.01f, 1f)]
    public float minWalkThreshold = 0.05f;
    
    [Tooltip("アニメーションの敏感さ（低いほど敏感）")]
    [Range(0.01f, 1f)]
    public float animationSensitivity = 0.1f;
    
    [Tooltip("アニメーションの滑らかさ")]
    [Range(1f, 20f)]
    public float animationSmoothness = 5f;
    
    // 内部処理用変数
    private Vector3 _targetPosition;
    private Vector3 _currentVelocity;
    
    private void Start()
    {
        // コンポーネントの取得
        if (characterController == null)
        {
            characterController = GetComponent<ThirdPersonController>();
        }
        
        if (animator == null && characterController != null)
        {
            animator = characterController.GetComponent<Animator>();
        }
        
        // ThirdPersonControllerのパラメータを設定
        if (characterController != null && animator != null)
        {
            // コンポーネントを取得できたら、パラメータを設定する
            if (characterController.AnimationSensitivity != 0)
            {
                // 既存のパラメータがあれば（ThirdPersonControllerが修正済みなら）直接設定
                characterController.AnimationSensitivity = animationSensitivity;
                characterController.AnimationSmoothness = animationSmoothness;
                characterController.MinimumWalkThreshold = minWalkThreshold;
            }
            
#if UNITY_EDITOR
            // エディタ専用：アニメーターのブレンドツリー調整を行う
            AttachAnimationAdjuster();
#endif
        }
        
        // 初期位置の設定
        _targetPosition = transform.position;
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// エディタ専用：アニメーション調整コンポーネントを取り付ける
    /// </summary>
    private void AttachAnimationAdjuster()
    {
        if (animator != null)
        {
            var adjuster = GetComponent<AnimationBlendTreeAdjuster>();
            if (adjuster == null)
            {
                adjuster = gameObject.AddComponent<AnimationBlendTreeAdjuster>();
                adjuster.targetAnimator = animator;
                adjuster.walkThreshold = 0.5f; // 歩行アニメーションの閾値を小さく設定
            }
        }
    }
#endif
    
    private void Update()
    {
        // 位置の更新処理
        UpdatePosition();
        
        // ThirdPersonControllerへのパラメータ同期
        UpdateControllerParameters();
    }
    
    /// <summary>
    /// ThirdPersonControllerのパラメータを動的に更新
    /// </summary>
    private void UpdateControllerParameters()
    {
        if (characterController != null)
        {
            // AnimationSensitivityプロパティが存在する場合のみ実行（互換性確保）
            if (characterController.AnimationSensitivity != 0)
            {
                characterController.AnimationSensitivity = animationSensitivity;
                characterController.AnimationSmoothness = animationSmoothness;
                characterController.MinimumWalkThreshold = minWalkThreshold;
            }
        }
    }
    
    /// <summary>
    /// 位置の滑らかな補間処理
    /// </summary>
    private void UpdatePosition()
    {
        // ここでの処理はThirdPersonControllerの動きに干渉しないよう注意
        // 将来的に独自の滑らかな位置補間ロジックを実装する場合に使用
    }
}