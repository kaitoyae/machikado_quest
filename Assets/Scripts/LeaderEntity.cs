using UnityEngine;

// リーダーデータ本体
[CreateAssetMenu(fileName = "LeaderEntity", menuName = "Create LeaderEntity")]
public class LeaderEntity : ScriptableObject
{
    public new string name;     // リーダー名
    public int hp;              // HP
    public Sprite icon;         // 画像（アイコン）
    public string ability;      // 特殊能力の説明
}