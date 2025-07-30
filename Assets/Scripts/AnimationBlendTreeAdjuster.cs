using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

/// <summary>
/// アニメーターコントローラーのBlendTree閾値を調整するためのスクリプト
/// エディタモードでのみ完全に機能します
/// </summary>
#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public class AnimationBlendTreeAdjuster : MonoBehaviour
{
    [Tooltip("アニメーターコントローラーが含まれるGameObject")]
    public Animator targetAnimator;

    [Header("BlendTree設定")]
    [Tooltip("歩行アニメーションの閾値（元の値：2.0）")]
    [Range(0.1f, 2.0f)]
    public float walkThreshold = 0.5f;
    
    [Tooltip("走行アニメーションの閾値（元の値：6.0）")]
    [Range(2.0f, 10.0f)]
    public float runThreshold = 6.0f;

    [Header("デバッグ")]
    [Tooltip("現在のアニメーションスピード")]
    public float currentAnimationSpeed;

    private void Awake()
    {
        // アニメーターが設定されていない場合は自身から取得
        if (targetAnimator == null)
        {
            targetAnimator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
#if UNITY_EDITOR
        // エディタ時のみ閾値を適用
        ApplyThresholds();
#endif
    }

    private void Update()
    {
        // 現在のアニメーションスピードを表示
        if (targetAnimator != null)
        {
            currentAnimationSpeed = targetAnimator.GetFloat("Speed");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// アニメーターコントローラーのBlendTree閾値を調整する
    /// </summary>
    public void ApplyThresholds()
    {
        if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null) return;

        // アニメーターコントローラーを取得
        RuntimeAnimatorController controller = targetAnimator.runtimeAnimatorController;
        AnimatorController animatorController = controller as AnimatorController;

        if (animatorController == null)
        {
            Debug.LogWarning("AnimatorControllerを取得できませんでした");
            return;
        }

        // レイヤーからステートマシンを取得
        AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;

        // すべてのステートを確認
        foreach (var state in stateMachine.states)
        {
            // "Idle Walk Run Blend" を探す
            if (state.state.name.Contains("Idle Walk Run Blend"))
            {
                // BlendTreeを取得
                BlendTree blendTree = state.state.motion as BlendTree;
                if (blendTree != null)
                {
                    // 子モーションを確認
                    for (int i = 0; i < blendTree.children.Length; i++)
                    {
                        var child = blendTree.children[i];
                        
                        // Walkモーションの閾値を調整（通常2.0に設定されている）
                        if (i == 1) // 歩行アニメーション（1番目の子）
                        {
                            child.threshold = walkThreshold;
                            blendTree.children[i] = child;
                        }
                        // Runモーションの閾値を調整（通常6.0に設定されている）
                        else if (i == 2) // 走行アニメーション（2番目の子）
                        {
                            child.threshold = runThreshold;
                            blendTree.children[i] = child;
                        }
                    }
                    
                    Debug.Log("BlendTreeの閾値を調整しました: Walk=" + walkThreshold + ", Run=" + runThreshold);
                }
                break;
            }
        }
    }
#endif
}

#if UNITY_EDITOR
// ReadOnly属性（インスペクターでの表示用）
public class ReadOnlyAttribute : PropertyAttribute { }

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif