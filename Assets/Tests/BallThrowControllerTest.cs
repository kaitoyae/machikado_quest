using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using NUnit.Framework;
using System.Collections;
using packt.FoodyGO.Controllers;

/// <summary>
/// BallThrowController用のテストクラス
/// テスト駆動開発（TDD）に従い、実装前にテストを作成
/// </summary>
public class BallThrowControllerTest
{
    private GameObject ballPrefab;
    private BallThrowController controller;
    private Camera testCamera;
    private MonsterController testMonster;

    [SetUp]
    public void Setup()
    {
        // テスト用オブジェクトの作成
        GameObject testGameObject = new GameObject("TestBallThrowController");
        controller = testGameObject.AddComponent<BallThrowController>();
        
        // テスト用カメラ
        GameObject cameraObject = new GameObject("TestCamera");
        testCamera = cameraObject.AddComponent<Camera>();
        
        // テスト用モンスター
        GameObject monsterObject = new GameObject("TestMonster");
        testMonster = monsterObject.AddComponent<MonsterController>();
        
        // テスト用ボールプレハブ（Sphereプレハブの代用）
        ballPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballPrefab.AddComponent<Rigidbody>();
        ballPrefab.GetComponent<Rigidbody>().useGravity = false;
        ballPrefab.AddComponent<SphereCollider>();
    }

    [TearDown]
    public void TearDown()
    {
        // テスト後のクリーンアップ
        Object.DestroyImmediate(controller.gameObject);
        Object.DestroyImmediate(testCamera.gameObject);
        Object.DestroyImmediate(testMonster.gameObject);
        Object.DestroyImmediate(ballPrefab);
    }

    /// <summary>
    /// ボール掴み機能のテスト
    /// 期待値: ボールをクリック/タップ時に掴み状態になる
    /// </summary>
    [Test]
    public void GrabBall_WhenClickOnBall_ShouldGrabBall()
    {
        // Arrange
        controller.ballPrefab = ballPrefab;
        controller.throwCamera = testCamera;
        
        // Act
        controller.OnGrabPerformed(Vector2.zero);
        
        // Assert
        Assert.IsTrue(controller.IsGrabbing, "ボールが掴まれていない");
        Assert.IsNotNull(controller.CurrentBall, "CurrentBallがnull");
    }

    /// <summary>
    /// ドラッグ中の軌道計算テスト
    /// 期待値: ドラッグ距離に応じて投射軌道が計算される
    /// </summary>
    [Test]
    public void CalculateTrajectory_WhenDragging_ShouldCalculateCorrectTrajectory()
    {
        // Arrange
        controller.ballPrefab = ballPrefab;
        controller.throwCamera = testCamera;
        controller.OnGrabPerformed(Vector2.zero);
        
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = new Vector2(100, 100);
        
        // Act
        Vector3 trajectory = controller.CalculateThrowVelocity(startPos, endPos);
        
        // Assert
        Assert.Greater(trajectory.magnitude, 0, "投射速度が0");
        Assert.IsTrue(trajectory.y > 0, "Y軸方向の速度が負またはゼロ");
    }

    /// <summary>
    /// リリース時の物理投射テスト
    /// 期待値: リリース時にボールが物理演算で投げられる
    /// </summary>
    [Test]
    public void ReleaseBall_WhenDragged_ShouldThrowBallWithPhysics()
    {
        // Arrange
        controller.ballPrefab = ballPrefab;
        controller.throwCamera = testCamera;
        controller.OnGrabPerformed(Vector2.zero);
        
        Vector2 releasePosition = new Vector2(50, 50);
        
        // Act
        controller.OnReleasePerformed(releasePosition);
        
        // Assert
        Assert.IsFalse(controller.IsGrabbing, "リリース後もGrabbing状態");
        Assert.IsNull(controller.CurrentBall, "リリース後にCurrentBallが残っている");
    }

    /// <summary>
    /// モンスター衝突検出テスト
    /// 期待値: ボールがモンスターに衝突時に適切に処理される
    /// </summary>
    [Test]
    public void BallHitMonster_WhenCollided_ShouldTriggerHitEvent()
    {
        // Arrange
        bool hitEventTriggered = false;
        controller.ballPrefab = ballPrefab;
        controller.throwCamera = testCamera;
        
        // ヒットイベントのテスト用リスナー
        controller.OnBallHitMonster += (ball, monster) => hitEventTriggered = true;
        
        // Act
        controller.OnGrabPerformed(Vector2.zero);
        controller.OnReleasePerformed(new Vector2(50, 50));
        
        // モンスターとの衝突をシミュレート
        controller.SimulateMonsterHit(testMonster);
        
        // Assert
        Assert.IsTrue(hitEventTriggered, "ヒットイベントが発生していない");
    }

    /// <summary>
    /// 連続投げ制限テスト
    /// 期待値: 一度に一つのボールのみ投げることができる
    /// </summary>
    [Test]
    public void MultipleThrows_ShouldBeRestrictedToOneAtATime()
    {
        // Arrange
        controller.ballPrefab = ballPrefab;
        controller.throwCamera = testCamera;
        
        // Act
        controller.OnGrabPerformed(Vector2.zero);
        bool firstGrabSuccess = controller.IsGrabbing;
        
        controller.OnGrabPerformed(Vector2.zero);
        bool secondGrabAttempt = controller.IsGrabbing;
        
        // Assert
        Assert.IsTrue(firstGrabSuccess, "最初のGrabが失敗");
        Assert.IsTrue(secondGrabAttempt, "2回目のGrab試行時にも同じ状態を維持すべき");
    }

    /// <summary>
    /// 力の調整テスト
    /// 期待値: ドラッグ距離に応じて投射力が適切に調整される
    /// </summary>
    [Test]
    public void ThrowForce_ShouldScaleWithDragDistance()
    {
        // Arrange
        controller.ballPrefab = ballPrefab;
        controller.throwCamera = testCamera;
        
        Vector2 shortDrag = new Vector2(10, 10);
        Vector2 longDrag = new Vector2(100, 100);
        
        // Act
        Vector3 shortThrow = controller.CalculateThrowVelocity(Vector2.zero, shortDrag);
        Vector3 longThrow = controller.CalculateThrowVelocity(Vector2.zero, longDrag);
        
        // Assert
        Assert.Greater(longThrow.magnitude, shortThrow.magnitude, 
            "長いドラッグの方が強い力で投げられるべき");
    }
}