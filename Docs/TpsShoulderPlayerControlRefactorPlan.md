# 第三人称越肩主角控制重构方案

## 1. 文档定位

本文档基于当前工程里的以下事实整理：

- 玩家控制主链仍由 `PrototypeFpsInput`、`PrototypeFpsController`、`PrototypeFpsMovementModule`、`PlayerWeaponController`、`PlayerMedicalController`、`PlayerThrowableController` 驱动。
- `PlayerLookController`、`PlayerStateHub`、`PlayerActionChannel`、`PlayerAnimationRigRefs`、`PlayerFullBodyAnimatorDriver` 已经落地，说明玩家控制链已经完成过一轮拆分。
- `FpsPlayer.prefab` 已具备 `HitboxRig`、`CharacterVisualRig`、`ViewRoot -> PitchPivot -> ViewCamera` 等基础结构，且第三人称可见身体已经挂接 Animator。
- 项目已安装 `com.unity.cinemachine 3.1.6`，现有工程可直接采用 Cinemachine 方案搭建越肩镜头。

本次目标不是在现有 FPS 链路上继续堆功能，而是把玩家控制重心切换为“第三人称越肩式主视角”，并为后续战斗、探索、医疗、投掷、交互和动画制作建立稳定边界。

---

## 1.1 当前进度快照（2026-03-31）

当前专项已经进入“阶段 1 已完成、阶段 1.5 基础能力已接入并持续调参、阶段 2 第一版已落地”的状态。

- 已完成：
  - 新增 `PlayerShoulderCameraController`
  - 新增 `PlayerOrientationController`
  - 新增 `PlayerAimPointResolver`
  - `FpsPlayer.prefab` 已接入 `ShoulderFollowTarget` 与 `ShoulderCameraRig`
  - `WorldCamera + CinemachineBrain` 已作为第三人称实际输出相机
  - `ViewCamera` 已降级为玩法兼容相机，继续供现有武器 / 投掷 / 交互链使用
  - 鼠标滚轮缩放、瞄准时相机收近、`SceneObject / Ground` 避障层已接入第一版
  - 角色探索态下已可按移动方向驱动可见身体 / HitboxRig 朝向，瞄准态下已回正到相机 yaw
  - `PlayerWeaponController` 已接入第一版 TPS 瞄准点与枪口修正
  - 投掷与交互方向已开始共享统一 aim point
  - `PlayerStateHub` 已开始汇总 `CameraYaw`、`CameraDistance`、`IsAimCamera`、`CharacterYawDeltaToCamera`、`AimWorldPoint`
  - `PlayerFullBodyAnimatorDriver` 已开始输出方向型 locomotion、武器槽位和 TPS 朝向差值参数
  - `FpsPlayerFullBody.controller` 已补齐 `Equip / Medical / Throw` 上半身占位状态入口
  - `PlayerWeaponPresentationController` 已接管第一人称视图模型实例化、切枪显隐与 ADS 姿态逻辑的第一版兼容层
  - `PlayerAnimationRigRefs` 已开始在运行时懒解析 `WeaponView_* / Muzzle`，`FpsPlayer.prefab` 也已补齐首批显式 weapon anchor 引用
- 已知待补：
  - 镜头默认构图、肩后偏移、跟随阻尼、瞄准收紧幅度还需要专门调手感
  - 当前“TPS 朝向”仍是桥接版本，玩家根节点与原玩法链仍保留兼容性的 FPS/Yaw 假设
  - 当前 TPS 瞄准点仍是桥接首版，贴墙压枪、近距离极限视差和第三人称武器实体 socket 仍需继续细化
  - 当前第三人称身体动画仍以占位资源为主，真正的瞄准移动、医疗、投掷表现仍需后续资源化
  - `PlayerWeaponController` 已完成第一轮旧 view-model / ADS pose 兼容实现清理，但 `ViewCamera` 兼容引用链仍未完全退出系统

本文档后续描述以“桥接迁移”作为默认策略，即在保留现有玩法链的前提下，逐阶段把主体验切换到第三人称越肩。

---

## 2. 从现有实现得出的基线结论

### 2.1 已经可以直接复用的部分

1. `PrototypeFpsInput` 仍适合作为统一输入入口。
2. `CharacterController` 仍适合作为玩家位移权威来源。
3. `PlayerStateHub` 与 `PlayerActionChannel` 已经提供了稳定的状态快照与动作仲裁边界。
4. `CharacterVisualRig + PlayerFullBodyAnimatorDriver` 已经为第三人称身体动画留出了入口。
5. `PlayerWeaponController`、`PlayerMedicalController`、`PlayerThrowableController` 已经具备玩法权威层雏形。

### 2.2 当前不适合直接沿用的 FPS 假设

1. `PlayerLookController` 仍采用“玩家根节点控制 yaw，相机子节点控制 pitch”的纯第一人称旋转方式。
2. `PrototypeFpsMovementModule` 直接修改 `ViewCamera` 本地位置，用于头部晃动、蹲伏降镜头和视角体感。
3. `PlayerAimController` 当前只处理第一人称 FOV 混合和 ADS 视图表现。
4. `PlayerWeaponController` 仍直接管理第一人称武器视图挂点、ADS Pose 与视图模型显隐。
5. `FpsPlayer.prefab` 里当前活跃的是贴近头部的 `ViewCamera`，并不适合直接扩展成越肩镜头。

不过在 2026-03-31 这一轮之后，第 1、2、5 条已经进入“桥接兼容态”：

- 主输出相机已不再是 `ViewCamera`，而是 `WorldCamera + CinemachineBrain`
- 角色可见身体朝向已开始通过 `PlayerOrientationController` 独立于探索态 / 瞄准态切换
- 但玩家根节点、部分武器逻辑与 `ViewCamera` 引用链仍保留旧假设，尚未彻底退出系统

### 2.3 预制体现状判断

结合 `FpsPlayer.prefab`，当前玩家预制体已经具备如下可转型基础：

- `CharacterVisualRig` 已经有独立 Animator，可承担第三人称主体表现。
- `HitboxRig` 与可见角色层已分开，适合后续继续做命中、受击和可见模型分层。
- `ViewRoot / PitchPivot / ViewCamera` 可以作为过渡期相机根链，但不应继续沿用为最终越肩镜头结构。
- 预制体里还存在一个禁用的 `WorldCamera`，说明当前相机链仍残留过渡资产，后续应统一收敛。

---

## 3. 目标体验定义

### 3.1 默认探索态

- 主视角为第三人称越肩镜头，默认右肩构图。
- 镜头跟随玩家，但不再贴头，不再依赖第一人称 head bob 作为主表现。
- 玩家移动方向以“相机平面方向”为基准，角色身体朝向与移动方向保持一致。
- 允许通过滚轮在限定范围内调整镜头距离，形成近距观察与稍远探索两档之间的连续过渡。

### 3.2 右键瞄准态

- 右键进入“越肩瞄准态”。
- 镜头距离收短，FOV 进一步拉近。
- 角色朝向与相机 yaw 对齐，不再只是“边跑边自由看”。
- 准星、武器散布、命中检测、枪口方向都以屏幕中心瞄准点为主，而不是纯相机正前方的第一人称假设。

### 3.3 交互与动作态

- 医疗、投掷、换弹、开火仍保持现有玩法权威逻辑，但表现切换到第三人称主体动作。
- 局内交互、搜刮和 UI 焦点切换时，镜头和角色输入都要稳定冻结。
- 后续仍可保留“近距离肩后压迫感”与“瞄准时更近”的恐怖游戏式构图，而不是回到传统远距离 TPS。

---

## 4. 核心设计原则

### 4.1 玩法权威不迁移

以下内容继续由玩法层决定：

- 位移是否生效
- 是否允许开火 / 换弹 / 治疗 / 投掷
- 弹药扣除、命中判定、伤害结算
- 医疗与投掷物消耗
- 体力扣除

动画与相机只表现结果，不反向成为唯一权威。

### 4.2 先改“视角与朝向”，后改“玩法模块内部”

本次重构的第一优先级是：

1. 把玩家的主视角从第一人称切换到第三人称越肩。
2. 把角色朝向逻辑从“固定相机头朝向”切换为“探索态按移动方向、瞄准态按镜头方向”。
3. 把武器瞄准判定从“第一人称射线”调整为“屏幕中心瞄准点 + 枪口修正”。

在此基础上，再逐步把武器表现、医疗表现和投掷表现迁移出去。

### 4.3 先保留类名与玩法逻辑，避免一次性大改名

短期内不建议一开始就把所有 `Fps` 命名全部推翻。

建议策略：

- 先让 `PrototypeFpsController` 继续作为玩家组合根使用。
- 新增更中性的模块名，例如 `PlayerShoulderCameraController`、`PlayerOrientationController`、`PlayerAimPointResolver`。
- 当第三人称链路跑稳后，再评估是否统一重命名为 `PrototypePlayerController` 或 `PlayerControlRoot`。

### 4.4 第一人称视图模型降级为兼容层

当前第一人称武器挂点和 ADS Pose 搜索逻辑仍可保留，但它们不再是主表现路径。

目标方向应是：

- 第三人称可见角色和武器挂点成为默认表现层。
- 第一人称视图模型仅作为过渡兼容、特殊演出或调试路径。

---

## 5. 目标架构

## 5.1 分层

建议把玩家系统收敛为 6 层：

1. 输入层
2. 相机层
3. 移动与朝向层
4. 玩法控制层
5. 状态汇聚层
6. 动画与 HUD 表现层

建议数据流如下：

```text
PrototypeFpsInput
    -> PlayerShoulderCameraController
    -> PlayerOrientationController
    -> PrototypeFpsMovementModule / Weapon / Medical / Throwable
    -> PlayerAimPointResolver
    -> PlayerActionChannel
    -> PlayerStateHub
    -> FullBody Animator / Weapon Presentation / HUD
```

## 5.2 建议新增或调整的模块

### `PlayerShoulderCameraController`

职责：

- 管理探索态与瞄准态镜头参数
- 管理滚轮缩放
- 管理越肩横向偏移
- 管理相机碰撞回缩
- 向其他系统提供当前相机 yaw、pitch、distance、isAimCamera 等状态

建议优先采用 Cinemachine 3 实现，而不是继续在 `PlayerLookController` 里手写全部镜头插值。

### `PlayerOrientationController`

职责：

- 探索态下按移动方向旋转玩家主体
- 瞄准态下按相机 yaw 旋转玩家主体
- 统一角色朝向切换速度与平滑策略

它不负责 pitch，不直接改 Camera，只负责玩家身体朝向。

### `PlayerAimPointResolver`

职责：

- 通过屏幕中心射线解析“瞄准点”
- 解决相机中心点、枪口点和命中点三者之间的对齐关系
- 为 `PlayerWeaponController`、投掷系统、交互系统提供统一的瞄准参考点

这是从 FPS 改到 TPS 时最重要的新桥接层之一。

### `PlayerAimController`

建议演进为“瞄准态管理器”，而不只是 FOV 混合器：

- 输入层面判断是否进入 Aim
- 管理 Hip / Aim 两组镜头参数
- 维持当前瞄准混合值
- 继续给武器系统提供散布修正

### `PlayerWeaponPresentationController`

职责：

- 接管第三人称武器可见模型挂点与表现
- 兼容当前第一人称 view model，直到 TPS 链路完成切换
- 提供枪口火光、后坐力表现、武器显隐和切枪表现

### `PlayerAnimationRigRefs`

建议扩展字段，而不是新建重复引用组件：

- 相机 follow target
- 相机 yaw root
- 相机 pitch root
- 第三人称武器挂点
- 左右手 IK 锚点
- 需要保留的第一人称兼容挂点

### `PlayerStateHub`

建议新增以下第三人称相关字段：

- `CameraYaw`
- `CameraPitch`
- `CameraDistance`
- `IsAimCamera`
- `IsShoulderRight`
- `IsFacingCameraYaw`
- `AimWorldPoint`
- `CharacterYawDeltaToCamera`

这些字段应成为动画、HUD、瞄准准星和调试信息的统一来源。

---

## 6. 输入方案调整

### 6.1 保留的输入

- `Move`
- `Look`
- `Attack`
- `Aim`
- `Interact`
- `Inventory`
- `Reload`
- `ToggleFireMode`
- `EquipPrimary / Secondary / Melee`
- 医疗与投掷输入
- `Sprint`
- `Jump`
- `ToggleCrouch`

### 6.2 新增或重定义的输入

- `CameraZoom`：使用鼠标滚轮缩放越肩距离
- `ShoulderSwap`：可选，后续如需左右肩切换再接入

### 6.3 与现有输入的关系

当前 `PrototypeFpsMovementModule` 使用 `Ctrl + Scroll` 调整移动速度比例。

建议：

- 第三人称默认使用“裸滚轮”控制镜头缩放。
- `Ctrl + Scroll` 是否继续保留给速度档位，由设计阶段明确取舍。
- 若项目后续不再强调 Tarkov 风格速度档位，可直接下线该功能，避免输入负担。

---

## 7. 相机方案

### 7.1 推荐方案

推荐使用 Cinemachine 3 的第三人称相机链，而不是继续让 `PlayerLookController` 直接持有 `ViewCamera` 完成全部控制。

推荐目标：

- 玩家身上保留 Follow / Aim 参考点
- 场景主相机挂 `CinemachineBrain`
- 玩家生成或启用时，绑定一个专属第三人称肩后 `CinemachineCamera`
- 探索态与瞄准态通过同一虚拟相机参数插值完成，不必一开始就分两套机位

### 7.2 必须满足的镜头行为

1. 基础跟随稳定，不抖动。
2. 镜头在墙边能回缩，不穿模。
3. 蹲伏、冲刺、瞄准切换时镜头位置平滑。
4. 右键瞄准时距离变近、FOV 变小。
5. 滚轮只在限定范围内改变镜头距离，不改变角色控制逻辑。
6. 镜头避障优先阻挡 `SceneObject` 与 `Ground` 等正式场景层，避免直接穿过环境主体。

### 7.3 与现有 `ViewCamera` 的关系

建议把当前 `ViewCamera` 从“贴头相机”改造成：

- 场景主 Camera 的持有者
- 被 CinemachineBrain 驱动的真实输出相机

不要再让 `PrototypeFpsMovementModule` 直接对它做第一人称 head bob 式位移写入。

---

## 8. 移动、朝向与射击规则

### 8.1 探索态移动

- 角色按相机平面的 Move 输入移动。
- 角色朝向与移动方向一致。
- 相机可绕角色自由观察，但角色不必始终面朝镜头前方。

### 8.2 瞄准态移动

- 角色朝向锁定到相机 yaw。
- 横移、后退保留 TPS 瞄准步态。
- 冲刺时强制退出瞄准。

### 8.2.1 当前阶段 2 的实际落地方式

为了避免在同一轮里同时推翻移动、命中盒和武器朝向链，本轮阶段 2 采用了更稳的桥接实现：

- 玩家根节点继续保留现有 yaw 驱动方式，确保输入、CharacterController 与旧玩法逻辑不被一次性打断。
- `PlayerOrientationController` 先接管 `CharacterVisualRig` 与 `HitboxRig` 的局部朝向：
  - 探索态下按当前平面速度方向旋转
  - 瞄准态下回正到相机 yaw
- 这意味着“可见身体 / 命中盒的 TPS 朝向规则”已经落地，但“玩法根节点完全脱离 FPS yaw 假设”仍留在后续深挖项。

这个取舍的目的不是回避问题，而是保证阶段 2 结束时，玩家已经能获得 TPS 式的运动阅读性，同时不给现有战斗链制造大面积回归风险。

### 8.3 射击与瞄准点

TPS 下不应继续采用“相机贴脸前向就是枪口方向”的第一人称简化假设。

建议采用：

1. 从屏幕中心发射 Aim Ray，得到目标点。
2. 枪口朝向目标点修正。
3. 命中检测以 Aim Ray 或枪口到目标点的二段式校正方案执行。

这样可以避免：

- 贴墙时枪口穿墙
- 第三人称越肩偏移导致的明显示差
- 准星对着目标却打偏的问题

### 8.4 投掷与交互

- 投掷抛射初速度建议改为参考相机瞄准点，而不是单纯 `viewCamera.forward`。
- 交互射线也建议逐步改用 `PlayerAimPointResolver` 提供的瞄准方向，减少 TPS 视角下的交互错位。

---

## 9. 动画迁移方向

### 9.1 第三人称全身动画成为主路径

现有 `PlayerFullBodyAnimatorDriver` 已可继续扩展。

后续应重点补齐：

- 探索态 locomotion
- 瞄准态 locomotion
- 右肩瞄准待机
- 横移 / 后退瞄准步态
- 开火、换弹、医疗、投掷动作

### 9.2 第一人称手臂动画降级

不建议继续把主要制作资源投入到第一人称 arms Animator。

建议优先级：

1. 第三人称身体动作完整
2. 第三人称武器挂点和开火表现稳定
3. 如仍有必要，再保留第一人称 arms 作为特殊场景兼容

### 9.3 `PlayerActionChannel` 继续沿用

医疗、投掷、换弹、开火的互斥关系不需要重做。

要做的是让第三人称动画系统消费这些动作状态，而不是继续主要服务于第一人称枪模。

---

## 10. 预制体调整建议

建议把 `FpsPlayer.prefab` 收敛为以下结构意图：

```text
FpsPlayer
├─ Systems
│  ├─ PrototypeFpsInput
│  ├─ PrototypeFpsController
│  ├─ PrototypeFpsMovementModule
│  ├─ PlayerShoulderCameraController
│  ├─ PlayerOrientationController
│  ├─ PlayerAimPointResolver
│  ├─ PlayerWeaponController
│  ├─ PlayerMedicalController
│  ├─ PlayerThrowableController
│  ├─ PlayerActionChannel
│  ├─ PlayerStateHub
│  └─ PlayerFullBodyAnimatorDriver
├─ CameraRigRoot
│  ├─ CameraYawRoot
│  ├─ CameraPitchRoot
│  ├─ CameraFollowTarget
│  └─ ViewCamera
├─ CharacterVisualRig
├─ HitboxRig
├─ WeaponSockets
└─ OptionalFirstPersonCompatibilityRig
```

重点不是立刻把层级完全改成上面这棵树，而是明确：

- 相机 rig 和角色可见模型要解耦
- 第三人称武器挂点不能继续只依赖 `ViewCamera` 子节点
- 第一人称视图挂点应降级为兼容资产

---

## 10.1 当前预制体落地状态（2026-03-31）

当前 `FpsPlayer.prefab` 已经不是纯 FPS 相机结构，而是进入了第三人称过渡态：

- `ViewRoot/PitchPivot/ViewCamera`
  - 继续保留为玩法兼容相机
  - 仍承担现阶段 `PlayerWeaponController`、`PlayerThrowableController`、交互与部分 FOV 驱动的引用来源
- `ViewRoot/PitchPivot/WorldCamera`
  - 挂接 `CinemachineBrain`
  - 作为第三人称实际渲染输出相机
- `ViewRoot/PitchPivot/ShoulderFollowTarget`
  - 作为肩后相机跟随目标
- `ShoulderCameraRig`
  - 挂接 `CinemachineCamera`
  - 挂接 `CinemachineThirdPersonFollow`

也就是说，当前的主风险已经从“没有 TPS 相机”变成了“相机链已桥接成功，但角色朝向、瞄准对齐和最终表现层还没有完全跟上”。

---

## 10.2 阶段 1.5 待调清单

阶段 1.5 不再新增大块架构，而是专门处理肩后镜头的手感与构图细节，当前先记录为后续持续调参项：

- 默认探索态肩后横向偏移、纵向抬升和相机距离
- 右键瞄准时的收近幅度、FOV 收紧幅度与过渡速度
- 滚轮缩放的默认步进、最小距离与最大距离
- `SceneObject` / `Ground` 避障层下的贴墙回缩稳定性
- 狭窄通道、门框、楼梯、低矮遮挡下的镜头表现
- 蹲伏、冲刺、起跳、落地时的镜头跟随阻尼
- 局内 UI 聚焦时镜头冻结与恢复的平滑性

阶段 1.5 会长期存在，允许在后续阶段继续做功能的同时穿插调手感。

---

## 11. 风险清单

1. `PrototypeFpsMovementModule` 当前直接写 `ViewCamera.localPosition`，若不先拆掉，会持续与 TPS 镜头系统打架。
2. `PlayerWeaponController.UpdateAimPresentation` 当前默认把 ADS 表现推给第一人称武器视图，需尽快抽离。
3. 交互、投掷、射击目前仍大量依赖 `viewCamera.forward`，TPS 化后会出现逻辑与视觉偏差。
4. 全身动画虽然已经挂上，但还缺少瞄准移动、转身和 TPS 专用状态。
5. 若不先明确“第一人称视图模型是否继续作为核心路径”，后续会重复投入制作成本。

---

## 12. 完成标准

当以下条件成立时，可以认为第三人称越肩主角控制完成基础成型：

1. 默认游戏镜头已切换为第三人称越肩视角。
2. 滚轮可在限定范围内缩放镜头距离。
3. 右键瞄准时镜头距离和 FOV 会收紧。
4. 探索态与瞄准态的角色朝向规则已分离。
5. 射击、投掷、交互都不再依赖纯第一人称射线假设。
6. `PlayerStateHub` 已能输出相机与 TPS 瞄准相关状态。
7. 第三人称身体动画已成为主表现路径。
8. 第一人称武器视图逻辑不再是战斗系统的硬依赖。

达到这一步后，玩家控制的后续工作就可以从“先完成视角切换”转入“补齐越肩战斗手感、动作质量和恐怖氛围表现”。
