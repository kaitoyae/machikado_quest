using UnityEngine;
using UnityEditor;
using packt.FoodyGO.Controllers;

[CustomEditor(typeof(CharacterGPSCompassController))]
public class CharacterGPSControllerEditor : Editor
{
    private bool showPokemonGoPresets = false;
    private bool showDebugInfo = false;
    
    public override void OnInspectorGUI()
    {
        CharacterGPSCompassController controller = (CharacterGPSCompassController)target;
        
        EditorGUILayout.Space();
        
        // ポケモンGO風プリセットボタン
        EditorGUILayout.BeginVertical("box");
        showPokemonGoPresets = EditorGUILayout.Foldout(showPokemonGoPresets, "ポケモンGO風設定", true);
        
        if (showPokemonGoPresets)
        {
            EditorGUILayout.HelpBox(
                "ポケモンGO風の動作を実現するための推奨設定:\n" +
                "• GPS移動量を増幅（3倍）\n" +
                "• 最低保証速度を設定（2.0 m/s）\n" +
                "• 常に歩行アニメーションを再生\n" +
                "• 小さな移動でも反応する設定",
                MessageType.Info);
                
            if (GUILayout.Button("ポケモンGO風設定を適用"))
            {
                Undo.RecordObject(controller, "Apply Pokemon GO Settings");
                
                controller.movementAmplification = 3.0f;
                controller.guaranteedMovementSpeed = 2.0f;
                controller.alwaysAnimateWhenMoving = true;
                controller.minimumAnimationSpeed = 0.5f;
                controller.speedChangeRate = 5.0f;
                controller.minDistanceToMove = 0.0001f;
                controller.enableGPSMovement = true;
                
                EditorUtility.SetDirty(controller);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("GPS移動を無効にする（WASD操作のみ）"))
            {
                Undo.RecordObject(controller, "Disable GPS Movement");
                controller.enableGPSMovement = false;
                EditorUtility.SetDirty(controller);
            }
            
            if (GUILayout.Button("GPS移動を有効にする"))
            {
                Undo.RecordObject(controller, "Enable GPS Movement");
                controller.enableGPSMovement = true;
                EditorUtility.SetDirty(controller);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("デフォルト設定に戻す"))
            {
                Undo.RecordObject(controller, "Reset to Default Settings");
                
                controller.movementAmplification = 1.0f;
                controller.guaranteedMovementSpeed = 1.0f;
                controller.alwaysAnimateWhenMoving = false;
                controller.minimumAnimationSpeed = 0.3f;
                controller.speedChangeRate = 5.0f;
                controller.minDistanceToMove = 0.005f;
                controller.enableGPSMovement = true;
                
                EditorUtility.SetDirty(controller);
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // デバッグ情報
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical("box");
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "デバッグ情報", true);
            
            if (showDebugInfo)
            {
                EditorGUI.BeginDisabledGroup(true);
                
                EditorGUILayout.LabelField("現在位置", controller.transform.position.ToString());
                EditorGUILayout.LabelField("ターゲット位置", controller.Target.ToString());
                EditorGUILayout.LabelField("移動中", controller.IsMoving ? "はい" : "いいえ");
                
                if (controller.gpsLocationService != null)
                {
                    EditorGUILayout.LabelField("GPS緯度", controller.gpsLocationService.Latitude.ToString("F6"));
                    EditorGUILayout.LabelField("GPS経度", controller.gpsLocationService.Longitude.ToString("F6"));
                }
                
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // デフォルトのインスペクターを表示
        DrawDefaultInspector();
    }
}