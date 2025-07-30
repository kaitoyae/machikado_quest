using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace packt.FoodyGO.Controllers
{
    public class CatchSceneController : MonoBehaviour
    {
        [Header("冷凍エフェクト")]
        public Transform frozenParticlePrefab;
        public MonsterController monster;
        public GameObject[] frozenDisableList;
        public GameObject[] frozenEnableList;
        
        [Header("ボール投げ設定")]
        public BallThrowController ballThrowController;
        public GameObject spherePrefab;
        public Transform ballSpawnPoint;
        
        private void Start()
        {
            InitializeBallThrowing();
        }
        
        /// <summary>
        /// ボール投げ機能の初期化
        /// </summary>
        private void InitializeBallThrowing()
        {
            if (ballThrowController == null)
            {
                ballThrowController = gameObject.AddComponent<BallThrowController>();
            }
            
            // ボール投げコントローラーの設定
            if (spherePrefab != null)
                ballThrowController.ballPrefab = spherePrefab;
                
            if (ballSpawnPoint != null)
                ballThrowController.ballSpawnPoint = ballSpawnPoint;
            else
                ballThrowController.ballSpawnPoint = transform;
                
            ballThrowController.throwCamera = Camera.main;
            
            // ボールヒットイベントの設定
            ballThrowController.OnBallHitMonster += OnBallHitMonster;
        }
        
        /// <summary>
        /// ボールがモンスターに当たった時の処理
        /// </summary>
        private void OnBallHitMonster(GameObject ball, MonsterController hitMonster)
        {
            Debug.Log($"CatchSceneController: OnBallHitMonster called. Ball={ball != null}, Monster={hitMonster != null}");
            
            if (hitMonster != null)
            {
                monster = hitMonster;
                
                // 衝突の擬似データを作成（実際の衝突データの代用）
                float ballSpeed = 0f;
                if (ball != null)
                {
                    Rigidbody ballRb = ball.GetComponent<Rigidbody>();
                    if (ballRb != null)
                    {
                        ballSpeed = ballRb.linearVelocity.magnitude;
                        Debug.Log($"CatchSceneController: Ball speed = {ballSpeed}");
                    }
                }
                
                ProcessMonsterHit(ballSpeed);
                
                // ボールを削除
                if (ball != null)
                {
                    Destroy(ball, 1f);
                }
            }
        }
        
        /// <summary>
        /// モンスターヒットの処理
        /// </summary>
        private void ProcessMonsterHit(float impactSpeed)
        {
            print("Monster hit with speed: " + impactSpeed);
            
            // 3回で凍結するように調整（1回あたり約0.33の減少）
            float animSpeedReduction = 0.35f; // 固定値で3回ヒットで凍結
            
            // 衝突速度が低い場合は効果を減らす
            if (impactSpeed < 5f)
            {
                animSpeedReduction *= (impactSpeed / 5f);
                print($"Weak hit! Reduction: {animSpeedReduction:F2}");
            }
            
            monster.animationSpeed = Mathf.Clamp01(monster.animationSpeed - animSpeedReduction);
            
            // 現在の状態を表示
            int hitCount = Mathf.CeilToInt((1f - monster.animationSpeed) / 0.33f);
            print($"Hit #{hitCount} - Animation Speed: {monster.animationSpeed:F2}");
            
            if (monster.animationSpeed <= 0.01f) // ほぼ0になったら完全に凍結
            {
                monster.animationSpeed = 0;
                print("Monster FROZEN!");

                // 凍結エフェクト
                if (frozenParticlePrefab != null)
                {
                    Instantiate(frozenParticlePrefab, monster.transform.position, monster.transform.rotation);
                }

                // UI切り替え（Caught_UIなどを表示）
                foreach(var g in frozenDisableList)
                {
                    if (g != null) g.SetActive(false);
                }
                foreach(var g in frozenEnableList)
                {
                    if (g != null) g.SetActive(true);
                }
                
                // アニメーターを完全に停止
                Animator animator = monster.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.speed = 0;
                }
            }
            else
            {
                // まだ凍結していない場合、アニメーション速度を更新
                Animator animator = monster.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.speed = monster.animationSpeed;
                    print($"Animator speed updated to: {animator.speed:F2}");
                }
            }
        }
        
        /// <summary>
        /// 従来の衝突処理（後方互換性のため残す）
        /// </summary>
        public void OnMonsterHit(GameObject go, Collision collision)
        {
            monster = go.GetComponent<MonsterController>();
            if (monster != null)
            {
                ProcessMonsterHit(collision.relativeVelocity.magnitude);
            }
        }
    }
}
