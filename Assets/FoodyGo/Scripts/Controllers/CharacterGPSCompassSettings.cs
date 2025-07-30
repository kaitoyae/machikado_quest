using UnityEngine;

namespace packt.FoodyGO.Controllers
{
    [CreateAssetMenu(fileName = "GPSSettings", menuName = "FoodyGO/GPS Controller Settings")]
    public class CharacterGPSCompassSettings : ScriptableObject
    {
        [Header("ポケモンGO風設定プリセット")]
        [Tooltip("推奨設定をすべて適用")]
        public bool applyPokemonGoPreset = false;
        
        [Header("推奨値")]
        [Space(10)]
        [TextArea(10, 20)]
        public string recommendedSettings = @"
ポケモンGO風の動作を実現するための推奨設定:

[Movement Settings]
- Movement Amplification: 3.0
  GPS移動量を3倍に増幅して、小さな移動でも大きく動くように見せる
  
- Guaranteed Movement Speed: 2.0
  最低保証速度。これ以下にはならない
  
- Always Animate When Moving: ON
  移動中は常に歩行アニメーションを再生

[Animation Settings]
- Minimum Animation Speed: 0.5
  アニメーションの最低速度
  
- Speed Change Rate: 5.0
  アニメーションのブレンド速度

[その他の設定]
- Min Distance To Move: 0.001
  より小さい値にすることで、わずかな移動でも反応する";
        
        public void ApplyToController(CharacterGPSCompassController controller)
        {
            if (applyPokemonGoPreset)
            {
                controller.movementAmplification = 3.0f;
                controller.guaranteedMovementSpeed = 2.0f;
                controller.alwaysAnimateWhenMoving = true;
                controller.minimumAnimationSpeed = 0.5f;
                controller.speedChangeRate = 5.0f;
                controller.minDistanceToMove = 0.001f;
                
                Debug.Log("ポケモンGO風の設定を適用しました");
            }
        }
    }
}