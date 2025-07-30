using UnityEngine;

// リーダーのデータを管理するクラス
public class LeaderModel
{
    public string name;         // リーダー名
    public int maxHp;          // 最大HP
    public int currentHp;      // 現在のHP
    public Sprite icon;        // 画像（アイコン）
    public string ability;     // 特殊能力の説明
    public bool isPlayer;      // プレイヤーのリーダーかどうか

    // コンストラクタ（リーダーIDとプレイヤーかどうかを引数にしてデータを読み込む）
    public LeaderModel(int leaderID, bool isPlayerLeader)
    {
        // Resourcesフォルダからリーダーデータを取得
        LeaderEntity leaderEntity = Resources.Load<LeaderEntity>("LeaderEntityList/Leader" + leaderID);
        
        if (leaderEntity != null)
        {
            // 取得したデータをLeaderModelに反映
            name = leaderEntity.name;
            maxHp = leaderEntity.hp;
            currentHp = maxHp; // 初期HPは最大値
            icon = leaderEntity.icon;
            ability = leaderEntity.ability;
            isPlayer = isPlayerLeader;
        }
        else
        {
            // デフォルト値を設定
            name = "Unknown Leader";
            maxHp = 20;
            currentHp = maxHp;
            ability = "No special ability";
            isPlayer = isPlayerLeader;
        }
    }
}