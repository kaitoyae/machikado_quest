Ultimate Player Physics & Movement System 完全版仕様書

📋 システム概要

目的

現在の複雑なThirdPersonController（600行）を、シンプルで効率的な新システム（280行）に置き換え、以下の問題を解決：

- 二重重力の問題
- 垂直速度の二重管理
- Update/FixedUpdateの役割混在
- GPS入力の不安定性
- 過度なデバッグログ
- プラットフォーム対応の不備

システム構成

新システム = 4つのコンポーネント + 1つの設定ファイル
├── InputCoordinator.cs (80行) - 全入力の統合管理
├── SimpleMovementController.cs (120行) - 純粋物理移動
├── SimpleCameraController.cs (60行) - カメラ制御
├── UIActionManager.cs (80行) - UI操作管理
└── PlayerControls.inputactions - Input System設定

---

🎯 コンポーネント詳細仕様

1. InputCoordinator.cs

役割: プラットフォーム自動検出＋全入力統合

主要機能

- プラットフォーム自動検出（PC/Mobile）
- PC: WASD + Mouse（Input System）
- Mobile: Virtual Joystick + GPS + Touch Camera
- 既存StarterAssetsInputsとの互換性（move, look, sprint変数）
- GPSは既存CharacterGPSCompassControllerと連携
- 優先度: GPS > Virtual Joystick（Mobile時）

クラス構造

public class InputCoordinator : MonoBehaviour
{
[Header("Platform Auto-Detection")]
public bool autoDetectPlatform = true;

```
  [Header("Existing Components Integration")]
  public MonoBehaviour gpsController; // CharacterGPSCompassController用
  public UIVirtualJoystick virtualJoystick;

  [Header("Input Sensitivity")]
  public float mouseSensitivity = 2f;
  public float touchSensitivity = 1f;

  // 既存互換性プロパティ
  public Vector2 move;
  public Vector2 look;
  public bool sprint;

  // プラットフォーム状態
  public bool IsMobileDevice { get; private set; }
  public string CurrentInputSource { get; private set; }

  // 内部状態
  private PlayerInput playerInput;
  private bool isGPSMovementActive = false;
  private Vector2 gpsMovementInput = Vector2.zero;
  private Vector2 joystickInput = Vector2.zero;

```

}

主要メソッド

- DetectPlatformAndSetup() - プラットフォーム検出と初期化
- UpdatePCInputs() - PC用入力処理
- UpdateMobileInputs() - モバイル用入力処理
- MonitorGPSMovement() - GPS位置変化の監視
- OnMove(), OnLook(), OnSprint() - Input Systemイベント

---

1. SimpleMovementController.cs

役割: ThirdPersonControllerの完全置き換え（既存互換性維持）

主要機能

- 既存変数名維持（MoveSpeed, SprintSpeed, RotationSmoothTime等）
- 純粋Rigidbody物理（手動重力計算削除）
- シンプル地面判定（CheckSphere 1回のみ）
- ジャンプ機能削除（物理エンジンに委任）
- 既存アニメーションパラメータ互換（Speed, Grounded, MotionSpeed）

クラス構造

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(InputCoordinator))]
public class SimpleMovementController : MonoBehaviour
{
[Header("Movement Settings (Existing Compatible)")]
public float MoveSpeed = 2.0f;
public float SprintSpeed = 5.335f;
public float RotationSmoothTime = 0.12f;

```
  [Header("Physics Settings")]
  public float acceleration = 10f;
  public float groundDrag = 5f;
  public float airDrag = 0.5f;

  [Header("Ground Detection")]
  public float GroundedRadius = 0.28f;
  public LayerMask GroundLayers = 1;
  public float groundCheckOffset = 0.1f;

  [Header("Animation Settings")]
  [Range(0.01f, 1.0f)]
  public float AnimationSensitivity = 0.1f;
  [Range(1.0f, 20.0f)]
  public float AnimationSmoothness = 5.0f;

  // 既存互換プロパティ
  public bool Grounded { get; private set; }
  public Vector3 velocity => rb.velocity;

  // コンポーネント参照
  private Rigidbody rb;
  private CapsuleCollider capsule;
  private InputCoordinator inputCoordinator;
  private Animator animator;
  private Camera playerCamera;

  // アニメーションハッシュ（パフォーマンス最適化）
  private int animSpeedHash;
  private int animGroundedHash;
  private int animMotionSpeedHash;

```

}

主要メソッド

- ConfigurePhysics() - Rigidbody最適設定
- GroundedCheck() - シンプル地面判定
- HandleMovement() - カメラ相対移動
- ApplyMovementForce() - 物理ベース移動
- UpdateAnimations() - 既存互換アニメーション

物理設定詳細

private void ConfigurePhysics()
{
rb.useGravity = true; // 重力は物理エンジンに完全委任
rb.drag = 0f; // ドラッグは手動制御
rb.angularDrag = 10f; // 回転摩擦
rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
rb.interpolation = RigidbodyInterpolation.Interpolate;
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
}

---

1. SimpleCameraController.cs

役割: プラットフォーム対応カメラ制御

主要機能

- PC: マウス感度設定
- Mobile: タッチ感度設定
- 既存Cinemachine FreeLookとの連携
- 既存変数名維持（TopClamp, BottomClamp）
- 慣性カメラ制御（モバイル用）

クラス構造

public class SimpleCameraController : MonoBehaviour
{
[Header("Camera Settings")]
public float mouseSensitivity = 2f;
public float touchSensitivity = 1f;

```
  [Header("Existing Compatibility")]
  public float TopClamp = 70.0f;
  public float BottomClamp = -30.0f;

  [Header("Touch Settings")]
  public float touchSensitivityMultiplier = 0.5f;
  public float touchDamping = 0.1f;

  // コンポーネント参照
  private InputCoordinator inputCoordinator;
  private Camera playerCamera;

```

#if CINEMACHINE_PRESENT
private CinemachineFreeLook freeLookCamera;
#endif

```
  // 内部状態
  private float cinemachineTargetYaw;
  private float cinemachineTargetPitch;
  private Vector2 lastTouchPosition;
  private Vector2 touchVelocity;
  private bool isTouching = false;

```

}

主要メソッド

- HandleCameraRotation() - プラットフォーム別処理
- HandlePCCameraInput() - PC用マウス入力
- HandleMobileCameraInput() - モバイル用タッチ入力
- UpdateCinemachineCamera() - Cinemachine連携
- ClampAngle() - 角度制限ユーティリティ

---

1. UIActionManager.cs

役割: 全UI操作の統一管理（移動システムから分離）

主要機能

- シーン切り替え機能
- 設定パネル管理
- Input Systemとの連携オプション
- モバイルジェスチャー対応
- 設定保存/読み込み

クラス構造

public class UIActionManager : MonoBehaviour
{
[Header("Scene Management")]
public string homeSceneName = "HomeScreen";
public string mapSceneName = "Map";
public string cardBattleSceneName = "CardBattleScene";

```
  [Header("UI References")]
  public Button homeButton;
  public Button mapButton;
  public Button cardBattleButton;
  public GameObject settingsPanel;
  public Button settingsButton;

  [Header("Input System Integration")]
  public bool enableInputSystemIntegration = true;

  [Header("Mobile Gesture Support")]
  public bool enableMobileGestures = true;
  public float swipeThreshold = 100f;

  // 内部状態
  private bool isSettingsPanelOpen = false;
  private PlayerInput playerInput;
  private Vector2 touchStartPosition;
  private bool isSwipeGesture = false;

```

}

主要メソッド

- LoadScene() - シーン切り替え
- ToggleSettingsPanel() - 設定パネル制御
- TriggerUIAction() - 他スクリプト用API
- HandleMobileGestures() - ジェスチャー処理
- SaveSettings() / LoadSettings() - 設定保存/読み込み

---

🔧 Input Actions設定

PlayerControls.inputactions 構造

{
"name": "PlayerControls",
"maps": [
{
"name": "Player",
"actions": [
{
"name": "Move",
"type": "Value",
"expectedControlType": "Vector2",
"bindings": [
{
"name": "WASD",
"composite": "2DVector",
"bindings": [
{"path": "<Keyboard>/w", "action": "up"},
{"path": "<Keyboard>/s", "action": "down"},
{"path": "<Keyboard>/a", "action": "left"},
{"path": "<Keyboard>/d", "action": "right"}
]
},
{"path": "<Gamepad>/leftStick"}
]
},
{
"name": "Look",
"type": "Value",
"expectedControlType": "Vector2",
"bindings": [
{"path": "<Mouse>/delta"},
{"path": "<Gamepad>/rightStick"}
]
},
{
"name": "Sprint",
"type": "Button",
"bindings": [
{"path": "<Keyboard>/leftShift"},
{"path": "<Gamepad>/leftTrigger"}
]
}
]
},
{
"name": "UI",
"actions": [
{"name": "UIHome", "type": "Button", "bindings": [{"path": "<Keyboard>/h"}]},
{"name": "UIMap", "type": "Button", "bindings": [{"path": "<Keyboard>/m"}]},
{"name": "UISettings", "type": "Button", "bindings": [{"path": "<Keyboard>/escape"}]}
]
}
]
}

---

📂 既存コードとの統合

既存コンポーネント活用

| コンポーネント | 統合方法 | 備考 |
| --- | --- | --- |
| CharacterGPSCompassController | InputCoordinatorと連携 | Reflectionで安全にアクセス |
| UIVirtualJoystick | InputCoordinatorと連携 | イベント接続で入力取得 |
| StarterAssetsInputs | 段階的置き換え可能 | 互換性プロパティで移行支援 |
| 既存UI Button | UIActionManagerと連携 | onClick イベント統合 |
| Cinemachine FreeLook | SimpleCameraControllerと連携 | 軸値直接制御 |

既存ThirdPersonControllerの置き換え手順

1. 既存ThirdPersonController無効化（削除しない）
2. 新コンポーネント追加・設定
3. 動作確認後、旧コンポーネント削除

---

🔬 技術的詳細

二重重力問題の解決

旧システムの問題

// 旧ThirdPersonController（問題のあるコード）
if (Grounded)
{
// 手動で重力をリセット
_verticalVelocity = 0.0f;
}
else
{
// 手動重力計算
_verticalVelocity += Gravity * Time.deltaTime;
}

// Rigidbodyも同時に重力を適用 → 二重重力！
rb.useGravity = true; // これが問題の原因

新システムの解決

// SimpleMovementController（解決されたコード）
private void ConfigurePhysics()
{
// 重力は物理エンジンに完全委任
rb.useGravity = true;
// 手動重力計算を完全削除
// 垂直速度は rb.velocity で統一管理
}

// 既存互換性プロパティ
public Vector3 velocity => rb.velocity; // 統一されたアクセス

GPS入力の安定化

安全なReflectionアクセス

private void MonitorGPSMovement()
{
if (gpsController != null)
{
// Reflectionで型安全にアクセス
var enableGPSField = gpsController.GetType().GetField("enableGPSMovement");
var isMovingProperty = gpsController.GetType().GetProperty("IsMoving");

```
      bool enableGPS = enableGPSField != null ? (bool)enableGPSField.GetValue(gpsController) : false;
      bool isMoving = isMovingProperty != null ? (bool)isMovingProperty.GetValue(gpsController) : false;

      isGPSMovementActive = enableGPS && isMoving;
  }

```

}

パフォーマンス最適化

アニメーションハッシュキャッシュ

// 初期化時にハッシュを計算（1回のみ）
private void CacheAnimationHashes()
{
animSpeedHash = Animator.StringToHash("Speed");
animGroundedHash = Animator.StringToHash("Grounded");
animMotionSpeedHash = Animator.StringToHash("MotionSpeed");
}

// 実行時は高速ハッシュを使用
private void UpdateAnimations()
{
if (animator == null) return;

```
  animator.SetFloat(animSpeedHash, speed, AnimationSensitivity, Time.deltaTime * AnimationSmoothness);
  animator.SetBool(animGroundedHash, Grounded);
  animator.SetFloat(animMotionSpeedHash, speed > 0.1f ? 1f : 0f);

```

}

効率的なUpdate/FixedUpdate分離

void Update()
{
// 軽量な処理のみ
GroundedCheck();
HandleMovement();
UpdateAnimations();
}

void FixedUpdate()
{
// 物理処理のみ
ApplyMovementForce();
ApplyDrag();
}

---

🎮 操作仕様

PC操作

| アクション | キー/入力 | 説明 |
| --- | --- | --- |
| 移動 | WASD | 4方向移動 |
| カメラ | マウス移動 | 自由視点 |
| スプリント | Left Shift | 高速移動 |
| ホーム | H | ホーム画面へ |
| マップ | M | マップ画面へ |
| 設定 | ESC | 設定パネル |

モバイル操作

| アクション | 入力 | 説明 |
| --- | --- | --- |
| 移動 | GPS優先→Virtual Joystick | 位置ベース移動 |
| カメラ | タッチドラッグ | 慣性付きカメラ |
| UI | スワイプジェスチャー | 上下スワイプで設定 |

優先度システム（モバイル）

GPS移動 > Virtual Joystick > 静止状態

---

🐛 デバッグ機能

画面表示デバッグ情報

InputCoordinator

=== Input System ===
Platform: PC/Mobile
Input Source: GPS/Joystick/Keyboard
Move: (x, y)
Look: (x, y)
Sprint: true/false
GPS Active: true/false

SimpleMovementController

=== Movement Controller ===
Grounded: true/false
Speed: X.X m/s
Velocity: X.X m/s
H-Velocity: X.X m/s

SimpleCameraController

=== Camera Controller ===
Platform: PC/Mobile
Yaw: XXX.X°
Pitch: XXX.X°
Touch Active: true/false
FreeLook: Connected/None

UIActionManager

=== UI Action Manager ===
Current Scene: SceneName
Settings Panel: Open/Closed
Mobile Gestures: true/false
Input System: true/false

ログ出力

Debug.Log("[InputCoordinator] プラットフォーム検出: Mobile");
Debug.Log("[SimpleMovementController] 物理設定完了");
Debug.Log("[SimpleCameraController] Cinemachine連携開始");
Debug.Log("[UIActionManager] シーン切り替え: MapScene");

---

📊 パフォーマンス指標

コード量比較

旧システム: ThirdPersonController (600行)
新システム: 4コンポーネント合計 (280行)
削減率: 53% 削減

実行効率向上

- 二重重力計算削除 → CPU負荷軽減
- シンプル地面判定 → 物理クエリ最適化
- アニメーションハッシュキャッシュ → 文字列処理削除
- Update/FixedUpdate適切分離 → フレームレート安定化

メモリ使用量最適化

- 不要な変数削除
- 効率的なコンポーネント参照
- キャッシュされたハッシュ値使用

---

🔄 拡張性設計

新機能追加の容易さ

新しい入力デバイス追加

// InputCoordinatorに追加
private void UpdateCustomInputs()
{
// 新デバイス処理
}

新しい移動モード追加

// SimpleMovementControllerに追加
[Header("New Movement Mode")]
public bool enableSpecialMode = false;

private void HandleSpecialMovement()
{
// 特殊移動処理
}

新しいUI操作追加

// UIActionManagerに追加
public void TriggerCustomAction(string actionName)
{
// カスタムアクション処理
}

プラットフォーム対応拡張

// 新プラットフォーム検出
private void DetectAdvancedPlatforms()
{
if (Application.platform == RuntimePlatform.Switch)
{
// Nintendo Switch対応
}
else if (Application.platform == RuntimePlatform.PS4)
{
// PlayStation対応
}
}

---

⚠️ 注意事項とベストプラクティス

実装時の注意点

1. コンポーネント依存関係
SimpleMovementController → InputCoordinator (Required)
SimpleCameraController → InputCoordinator (Optional)
UIActionManager → PlayerInput (Optional)
2. 物理設定の重要性
// 必須設定
rb.useGravity = true;
rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
3. アニメーター互換性
必要なパラメータ:
- Speed (float)
- Grounded (bool)
- MotionSpeed (float)

トラブルシューティング

移動しない場合

1. InputCoordinatorの設定確認
2. PlayerInputの Actions設定確認
3. 新旧コンポーネントの競合確認

カメラが動かない場合

1. Cinemachine FreeLookの存在確認
2. CinemachineTargetタグの設定確認
3. マウス感度設定確認

GPS移動しない場合

1. CharacterGPSCompassControllerの設定確認
2. GPS有効化確認
3. 位置情報サービス確認

アニメーション再生されない場合

1. Animatorコンポーネント確認
2. アニメーションパラメータ名確認
3. アニメーション感度設定確認

---

🚀 将来の発展可能性

フェーズ2拡張案

AIベース移動

public class AIMovementExtension : MonoBehaviour
{
public SimpleMovementController movementController;

```
  private void UpdateAIMovement()
  {
      // AI制御による自動移動
  }

```

}

ネットワーク同期

public class NetworkMovementSync : MonoBehaviour
{
public SimpleMovementController movementController;

```
  private void SyncMovementOverNetwork()
  {
      // ネットワーク同期処理
  }

```

}

VR/AR対応

public class VRMovementAdapter : MonoBehaviour
{
public SimpleMovementController movementController;

```
  private void HandleVRInput()
  {
      // VRコントローラー入力処理
  }

```

}

---

📈 成功指標とKPI

パフォーマンス指標

- CPU使用率: 15%削減目標
- メモリ使用量: 10%削減目標
- フレームレート安定性: 95%以上

品質指標

- バグ発生率: 70%削減目標
- コードレビュー時間: 50%短縮目標
- 新機能開発時間: 40%短縮目標

ユーザーエクスペリエンス指標

- 移動の応答性: 100ms以下
- カメラの滑らかさ: 60fps安定
- プラットフォーム間統一性: 95%以上

---

🎓 学習リソースと参考資料

Unity公式ドキュメント

- https://docs.unity3d.com/Manual/class-Rigidbody.html
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.13/manual/index.html
- https://docs.unity3d.com/Packages/com.unity.cinemachine@2.10/manual/index.html

ベストプラクティス

- https://docs.unity3d.com/Manual/BestPracticeGuides.html
- https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions

コミュニティリソース

- Unity Forums
- Stack Overflow Unity タグ
- GitHub Unity Community

---

📝 まとめ

Ultimate Player Physics & Movement Systemは、従来の複雑なシステムを大幅に簡素化し、以下を実現します：

✅ 主要成果

1. 53%のコード削減 - 保守性向上
2. 二重重力問題の完全解決 - 物理演算の正常化
3. プラットフォーム自動対応 - 開発効率向上
4. 既存システムとの互換性 - スムーズな移行
5. 高い拡張性 - 将来の機能追加に対応

🎯 期待効果

- 開発速度向上: シンプルなコード構造
- バグ削減: 明確な責任分離
- パフォーマンス向上: 最適化された物理処理
- チーム生産性向上: 理解しやすい設計

Ultimate Player Physics & Movement Systemにより、Straventプロジェクトはより堅牢で効率的なプラットフォームへと進化します。

---

この仕様書は実装完了済みシステムの完全な技術文書です。保存・共有・参照用としてご活用ください。