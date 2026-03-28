# FPS 主角控制与动画控制重构框架

## 1. 文档目标

本文档基于以下现有实现整理：

- `Assets/Res/Scripts/FPS/PrototypeFpsInput.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsMovementModule.cs`
- `Assets/Res/Scripts/FPS/PlayerAimController.cs`
- `Assets/Res/Scripts/FPS/PlayerWeaponController.cs`
- `Assets/Res/Scripts/FPS/PlayerMedicalController.cs`
- `Assets/Res/Scripts/FPS/PlayerThrowableController.cs`
- `Assets/Res/Prefabs/Player/FpsPlayer.prefab`
- `Docs/RefactorRoadmap.md`

目标不是推翻当前原型，而是在保留现有可玩闭环的前提下，为后续第一人称射击主角控制、第一人称表现、第三人称身体动画和武器动画建立一个可持续扩展的框架。

---

## 1.1 当前已落地边界

更新时间：`2026-03-28`

当前工程里已经完成了框架落地的前三步基础版，后续制作可以直接建立在这些边界之上：

1. `FpsPlayer.prefab` 已完成阶段 0 的预制体整理：
   - 命中盒 Rig 与可见角色 Rig 已分开命名。
   - 第一人称相机下已有显式的 `WeaponView_Primary / Secondary / Melee` 挂点。
   - 预制体已挂接 `PlayerAnimationRigRefs`，后续动画脚本不需要再靠运行时临时找节点。
2. `PrototypeFpsController` 已完成阶段 1 的主控制器瘦身：
   - 视角处理已抽离到 `PlayerLookController`。
   - HUD 刷新入口已抽离到 `PlayerHudPresenter`。
   - 主控制器当前更接近组合根 / 调度入口，而不是继续堆玩法细节。
3. 阶段 2 的统一状态边界已经落地：
   - `PlayerActionChannel` 负责显式动作优先级与上半身动作仲裁。
   - `PlayerStateHub` 负责每帧汇总快照。
   - `PlayerHudPresenter` 现已只依赖 `PlayerStateHub`，不再跨多个控制器直接拼装 HUD。
4. 阶段 3 的第三人称身体动画基础版已经落地：
   - `CharacterVisualRig` 已绑定 `Animator`、Generic Avatar 与 `FpsPlayerFullBody.controller`。
   - 已新增 `PlayerFullBodyAnimatorDriver`，把 `PlayerStateHub` 快照映射为 Animator 参数。
   - 已基于 `Assets/Res/Packages/素体.fbx` 生成可直接使用的基础 locomotion / jump / death / aim / fire / reload 占位动画资源。

因此，后续阶段 4 以后新增的第一人称手臂 Animator Driver、武器表现层、医疗/投掷动作表现层，都应默认建立在 `PlayerStateHub` 和 `PlayerActionChannel` 之上，而不是重新把状态判断塞回 `PrototypeFpsController`。

---

## 2. 当前实现现状

### 2.1 现有职责分布

| 区域 | 当前主要承担者 | 当前职责 | 现阶段评价 |
| --- | --- | --- | --- |
| 输入采集 | `PrototypeFpsInput` | 统一收集移动、视角、射击、瞄准、交互、换弹、治疗、投掷、姿态、速度档位等输入 | 作为统一输入入口是合理的，建议保留 |
| 主循环协调 | `PrototypeFpsController` | 组件装配、依赖注入、鼠标锁定、视角旋转、模块 Update 顺序、HUD 拼装、武器同步 | 仍然过重，是后续拆分的核心对象 |
| 角色移动 | `PrototypeFpsMovementModule` | `CharacterController` 驱动、跳跃、冲刺、蹲伏、速度档位、落地恢复、头部晃动、移动噪声 | 同时承担了运动学、表现层、AI 感知层三类职责 |
| 武器运行时 | `PlayerWeaponController` | 武器槽位、武器实例状态、射击、换弹、火模式、命中判定、第一人称武器模型实例化、ADS 姿态对齐 | 既是战斗域对象，又在做第一人称表现层 |
| 瞄准 | `PlayerAimController` | FOV、瞄准混合、散布倍率、把瞄准混合推给武器表现 | 当前是可用的过渡层 |
| 医疗 | `PlayerMedicalController` | 从背包选择医疗物品并执行治疗、止血、夹板、止痛逻辑 | 领域职责较清晰 |
| 投掷 | `PlayerThrowableController` | 选择投掷物、扣体力、生成投掷物、投掷反馈 | 领域职责较清晰 |
| 玩家预制体 | `FpsPlayer.prefab` | 角色根节点、`CharacterController`、相机、命中盒、全身模型、第一人称手臂模型实例 | 结构具备扩展潜力，但动画链路还未真正建立 |

### 2.2 从当前代码能确认的关键结论

1. `PrototypeFpsController` 已从早期巨型控制器拆出移动、武器、瞄准、医疗、投掷模块，但它自己仍负责：
   - 依赖装配
   - 视角旋转
   - 输入优先级编排
   - 光标 / UI 焦点切换
   - HUD 文本拼装
   - 武器定义同步

2. `PrototypeFpsMovementModule` 已经包含完整的基础 FPS 体感逻辑，这部分适合继续作为“权威运动层”，但不适合继续承载动画和镜头表现的全部职责。

3. `PlayerWeaponController` 当前有两条职责线：
   - 运行时武器状态与战斗判定
   - 第一人称武器模型加载、切枪显隐、ADS 姿态插值

4. `PlayerAimController` 已经形成了一个很好的过渡点：
   - 它不直接决定是否命中
   - 它只负责计算瞄准混合、FOV 和散布修正
   - 它可以自然演进为动画层的数据源之一

5. `FpsPlayer.prefab` 已经预埋了两套视觉层级：
   - 一个第三人称可见身体 Rig
   - 一个挂在相机下的第一人称手臂 / 武器 Rig

6. 预制体中的可见身体 `Animator` 目前没有挂 `AnimatorController`，说明当前还没有真正成型的角色动画状态机。

7. 项目搜索结果没有发现独立的 `.controller` / `.anim` 动画资产，说明这一块还处于空白或待接入状态。

### 2.3 当前主要结构问题

#### 问题 A：控制状态没有统一的中间层

当前模块之间通过直接调用与即时查询协作，例如：

- `PrototypeFpsController` 直接决定模块执行顺序
- `PlayerAimController` 直接读取 `PlayerWeaponController`
- HUD 直接从多个控制器拼状态

这会导致后续动画系统只能继续“到处拉状态”，难以稳定。

#### 问题 B：动画与表现状态没有独立域

目前虽然存在头部晃动、ADS 视图模型对齐、切枪显隐，但这些都分散在：

- `PrototypeFpsMovementModule`
- `PlayerAimController`
- `PlayerWeaponController`

缺少统一的“动画表现层”边界。

#### 问题 C：移动模块混合了运动学、镜头表现、噪声系统

`PrototypeFpsMovementModule` 目前至少承担了三类职责：

- 真正影响角色位置的运动学
- 只影响本地视觉的头部晃动与相机高度
- 只影响 AI 感知的噪声发射

这会阻碍后续动画、脚步声、镜头反馈的精细化制作。

#### 问题 D：武器控制器混合了战斗与第一人称表现

`PlayerWeaponController` 当前既负责：

- 开火、换弹、火模式、弹药与耐久

又负责：

- 武器视图模型创建
- 武器显隐
- ADS Pose 搜索与插值

继续往里面加入换弹动画、后坐力动画、左手 IK、武器晃动，会再次变成巨型类。

#### 问题 E：预制体命名与层级意图还不够清晰

`FpsPlayer.prefab` 中存在两个 `FullBodyRig` 命名，实际承担的职责并不相同：

- 一个更偏命中盒 / 逻辑定位
- 一个更偏可见角色模型

在动画系统接入前，这会增加沟通和维护成本。

---

## 3. 重构目标

本次主角控制与动画控制框架，建议满足以下目标：

1. 保留 `PrototypeFpsInput` 作为统一输入入口，不重做输入系统。
2. 保留 `CharacterController` 作为玩家位移权威来源，不引入 Root Motion 驱动主位移。
3. 让“玩法状态”和“动画表现状态”解耦。
4. 让“第三人称身体动画”和“第一人称手臂 / 武器动画”分层。
5. 保证武器、医疗、投掷等上半身行为有统一的动作仲裁层。
6. 让动画系统只消费稳定状态，不直接决定伤害、弹药和换弹完成时机。
7. 保证后续可以逐步加入：
   - 跑步、蹲伏、跳跃、落地动画
   - ADS 过渡
   - 开火 / 后坐力 / 换弹 / 切枪
   - 医疗与投掷动作
   - 受击、死亡、脚步、噪声表现

---

## 4. 目标架构

### 4.1 总体分层

建议把玩家系统稳定为 5 层：

1. 输入层
2. 玩法控制层
3. 状态汇聚层
4. 动画 / 表现层
5. HUD / UI 层

推荐的数据流如下：

```text
PrototypeFpsInput
    -> PlayerControlRoot
        -> Movement / Weapon / Medical / Throwable / Interaction
        -> PlayerStateHub
            -> PlayerAnimationController
                -> FullBodyAnimatorDriver
                -> FpArmsAnimatorDriver
                -> WeaponPresentationController
            -> PlayerHudPresenter
```

### 4.2 建议模块划分

| 目标模块 | 主要职责 | 现有来源 | 说明 |
| --- | --- | --- | --- |
| `PlayerControlRoot` | 只负责装配依赖、控制上下文切换、更新顺序 | `PrototypeFpsController` | 未来的主控制器应该更像 Composition Root |
| `PlayerLookController` | 只负责水平旋转、俯仰角、鼠标锁定策略 | 从 `PrototypeFpsController` 的 `HandleLook` 抽离 | 让相机控制从主控制器脱离 |
| `PlayerLocomotionMotor` | 角色移动、重力、冲刺、跳跃、蹲伏 | `PrototypeFpsMovementModule` 的运动学部分 | 保留 `CharacterController` 权威 |
| `PlayerLocomotionPresentation` | 头部晃动、镜头高度过渡、脚步相机反馈 | `PrototypeFpsMovementModule` 的表现部分 | 只影响本地第一人称表现 |
| `PlayerMovementNoiseEmitter` | 行走 / 跳跃 / 落地噪声发射 | `PrototypeFpsMovementModule` 的噪声部分 | 单独隔离 AI 感知逻辑 |
| `PlayerWeaponController` | 武器实例、弹药、射击、换弹、火模式、伤害判定 | 保留现有类 | 继续做玩法权威层 |
| `PlayerWeaponPresentationController` | 视图模型加载、切枪显隐、武器动画参数、武器晃动、后坐力表现 | 从 `PlayerWeaponController` 抽离 | 建议优先拆出 |
| `PlayerAimController` | 维持 ADS 状态、FOV 混合、扩散倍率 | 保留现有类 | 短期保留，中期可转为动画数据源 |
| `PlayerMedicalController` | 医疗玩法权威逻辑 | 保留现有类 | 不负责医疗动画 |
| `PlayerThrowableController` | 投掷玩法权威逻辑 | 保留现有类 | 不负责投掷动画 |
| `PlayerActionChannel` | 管理武器 / 医疗 / 投掷的互斥与打断规则 | 目前隐含在 `PrototypeFpsController` 中 | 推荐新增 |
| `PlayerStateHub` | 统一汇总稳定状态快照与一次性信号 | 新增 | 动画层和 HUD 层都应该从这里取状态 |
| `PlayerAnimationController` | 统一驱动全身动画与第一人称动画 | 新增 | 不直接参与伤害、弹药、位移 |
| `PlayerHudPresenter` | 拼装 HUD 展示数据 | 从 `PrototypeFpsController` 的 HUD 代码抽离 | 避免主控制器继续膨胀 |

### 4.3 关键边界原则

#### 原则 1：玩法权威永远在玩法层

以下行为必须由玩法层决定，而不是由动画事件反向决定：

- 是否能开火
- 是否扣弹
- 是否命中
- 是否造成伤害
- 是否完成换弹
- 是否消耗治疗物品
- 是否扣除投掷物
- 是否扣体力

动画只能表现这些结果，不能成为这些结果的唯一权威来源。

#### 原则 2：动画层只读稳定状态与一次性信号

动画层建议只消费两类数据：

- 连续状态：
  - 速度
  - 是否落地
  - 是否蹲伏
  - 是否瞄准
  - 当前武器槽位
  - 当前上半身动作
- 一次性信号：
  - 开火
  - 切枪
  - 换弹开始
  - 换弹结束
  - 跳跃
  - 落地
  - 受击
  - 死亡
  - 医疗开始
  - 投掷开始

#### 原则 3：第一人称与第三人称必须分层

建议明确区分：

- 第三人称可见身体：给他人看、给击中反馈看、给死亡 / 受击 / 观察者看
- 第一人称手臂与武器：给本机手感、瞄准、换弹、后坐力、晃动看

这两套动画不应该共享同一个 Animator Controller。

#### 原则 4：主位移不依赖 Root Motion

由于当前玩家使用 `CharacterController`，建议保持：

- 主位移、冲刺、跳跃、蹲伏仍由代码驱动
- Animator `Apply Root Motion` 保持关闭
- 动画只跟随位移结果

这样能保留当前原型的稳定手感和调参路径。

---

## 5. 状态汇聚层设计

### 5.1 建议新增的核心状态对象

建议引入一个统一的 `PlayerAnimationFrame` 或 `PlayerRuntimeStateSnapshot`，每帧由 `PlayerStateHub` 汇总。

建议包含以下字段：

| 分类 | 建议字段 |
| --- | --- |
| 上下文 | `ControlContext`、`IsUiFocused`、`IsAlive` |
| 朝向 | `YawDelta`、`Pitch`、`IsCursorLocked` |
| 移动 | `PlanarSpeed`、`MoveInputX`、`MoveInputY`、`VelocityY`、`IsGrounded` |
| 姿态 | `IsCrouching`、`IsSprinting`、`MovementSpeedRatio` |
| 瞄准 | `AimBlend`、`IsAiming`、`CanAim` |
| 武器 | `ActiveWeaponSlot`、`WeaponType`、`FireMode`、`MagazineAmmo`、`ReserveAmmo`、`IsReloading` |
| 动作 | `UpperBodyAction`、`CanFire`、`CanReload`、`CanUseMedical`、`CanThrow` |
| 一次性信号 | `JumpTriggered`、`LandTriggered`、`FireTriggered`、`ReloadTriggered`、`EquipTriggered`、`MedicalTriggered`、`ThrowTriggered`、`HurtTriggered`、`DeathTriggered` |
| 附加表现 | `RecoilImpulse`、`HitConfirmed`、`IsExhausted` |

### 5.2 上半身动作通道

当前 `PrototypeFpsController` 的优先级实际是：

1. 武器切换 / 换弹输入
2. 瞄准更新
3. 医疗
4. 投掷
5. 武器攻击

建议把这套隐式规则显式化，形成 `PlayerActionChannel`：

- `None`
- `Weapon`
- `Reload`
- `Medical`
- `Throwable`
- `Interact`
- `Dead`

这样后续动画层就不需要猜当前上半身应该播放什么动作。

---

## 6. 动画层框架

### 6.1 第三人称身体动画

用于驱动可见身体模型，服务于：

- 被其他角色观察
- 受击 / 死亡表现
- 阴影、轮廓、命中反馈
- 未来第三人称镜头或观战模式

建议建立 `PlayerFullBodyAnimatorDriver`，负责把 `PlayerAnimationFrame` 翻译为 Animator 参数。

建议参数集：

| 参数 | 类型 | 说明 |
| --- | --- | --- |
| `MoveX` | Float | 左右移动输入或局部速度 |
| `MoveY` | Float | 前后移动输入或局部速度 |
| `Speed` | Float | 水平速度归一化 |
| `VerticalSpeed` | Float | 垂直速度 |
| `IsGrounded` | Bool | 是否着地 |
| `IsCrouching` | Bool | 是否蹲伏 |
| `IsSprinting` | Bool | 是否冲刺 |
| `IsAiming` | Bool | 是否正在 ADS |
| `EquipSlot` | Int | 主武器 / 副武器 / 近战 |
| `UpperBodyAction` | Int | 武器 / 医疗 / 投掷等动作状态 |
| `FireTrigger` | Trigger | 开火瞬时信号 |
| `ReloadTrigger` | Trigger | 换弹开始 |
| `JumpTrigger` | Trigger | 起跳 |
| `LandTrigger` | Trigger | 落地 |
| `HurtTrigger` | Trigger | 受击 |
| `IsDead` | Bool | 死亡 |

建议状态机结构：

- Base Layer：Idle / Walk / Jog / Sprint / Crouch / Jump / Fall / Land
- Upper Body Layer：Aim / Fire / Reload / Medical / Throw / Melee
- Additive Layer：受击、呼吸、轻微偏移

### 6.2 第一人称手臂与武器动画

用于驱动本机第一人称表现，服务于：

- 开火体感
- ADS 过渡
- 后坐力
- 切枪
- 换弹
- 近战挥击
- 医疗与投掷的镜头内动作

建议建立 `PlayerFpArmsAnimatorDriver` 与 `PlayerWeaponPresentationController`，两者职责分开：

- `PlayerFpArmsAnimatorDriver`：
  - 负责第一人称手臂 Animator 参数
  - 负责把动作状态翻译为动画层切换
- `PlayerWeaponPresentationController`：
  - 负责武器视图模型挂点、显隐、位置姿态、枪口表现、后坐力位移 / 旋转、枪械 sway

建议第一人称参数集：

| 参数 | 类型 | 说明 |
| --- | --- | --- |
| `AimBlend` | Float | ADS 混合值 |
| `SprintBlend` | Float | 冲刺混合值 |
| `MoveSpeed` | Float | 移动强度 |
| `EquipSlot` | Int | 当前装备槽位 |
| `WeaponType` | Int | 枪械 / 近战 / 投掷 |
| `UpperBodyAction` | Int | 上半身动作状态 |
| `FireTrigger` | Trigger | 开火 |
| `ReloadTrigger` | Trigger | 换弹开始 |
| `MeleeTrigger` | Trigger | 近战挥击 |
| `MedicalTrigger` | Trigger | 医疗开始 |
| `ThrowTrigger` | Trigger | 投掷开始 |
| `HitConfirmTrigger` | Trigger | 可选，命中反馈 |

### 6.3 关于 ADS Pose 的演进建议

当前 `PlayerWeaponController` 已支持：

- 从武器 prefab 中查找 `ScopePose` / `AdsPose` / `AimPose` / `IronSightPose`
- 查不到时退回到 `PrototypeWeaponDefinition` 中的 ADS 偏移

这套机制短期可继续保留，但建议中期演进为 `WeaponPresentationProfile`：

- 武器视图模型 prefab
- ADS Pose 数据
- Recoil Curve
- Sway 参数
- Equip / Reload / Fire 动画配置
- 左手 IK 目标

这样武器玩法定义与武器表现定义就能分离。

### 6.4 动画事件使用原则

动画事件可以用于：

- 播放声音
- 播放壳体抛出 / 烟火 / 拉栓粒子
- 触发局部镜头抖动
- 触发弹匣插入音效

动画事件不应该作为唯一权威来驱动：

- 扣弹
- 命中
- 伤害
- 医疗完成
- 投掷物真正离手

如确实需要做“动作时机更像动画”的体验，也应采用：

- 玩法层先进入 Action State
- 动画层回调“表现时机”
- 真正结果仍由玩法层和计时器确认

---

## 7. 预制体与资产组织建议

### 7.1 预制体建议结构

建议把 `FpsPlayer.prefab` 最终整理为如下结构：

```text
FpsPlayer
├─ Systems
│  ├─ PrototypeFpsInput
│  ├─ PlayerControlRoot
│  ├─ PlayerStateHub
│  ├─ PlayerLocomotionMotor
│  ├─ PlayerLookController
│  ├─ PlayerWeaponController
│  ├─ PlayerMedicalController
│  ├─ PlayerThrowableController
│  ├─ PlayerAnimationController
│  └─ PlayerHudPresenter
├─ ViewRoot
│  └─ ViewCamera
│     ├─ Muzzle
│     ├─ WeaponView_Primary
│     ├─ WeaponView_Secondary
│     └─ WeaponView_Melee
├─ FPArmsRig
├─ CharacterVisualRig
├─ HitboxRig
└─ Interaction / Inventory / Other Runtime Components
```

### 7.2 预制体清理建议

优先建议做以下轻量整理：

1. 把两个 `FullBodyRig` 重命名为更明确的名称：
   - `HitboxRig`
   - `CharacterVisualRig`

2. 在 `ViewCamera` 下显式建立：
   - `WeaponView_Primary`
   - `WeaponView_Secondary`
   - `WeaponView_Melee`

3. 不再依赖“字段为空时运行时自动创建锚点”作为长期方案。

4. 给第一人称手臂和第三人称身体各自准备明确的 Animator 引用节点。

### 7.3 建议新增资产类型

建议后续按以下资产组织：

- `Assets/Res/Animation/Player/Controllers/PlayerFullBody.controller`
- `Assets/Res/Animation/Player/Controllers/PlayerFpArms.controller`
- `Assets/Res/Animation/Player/Masks/UpperBody.mask`
- `Assets/Res/Animation/Player/Profiles/WeaponPresentationProfile_*.asset`
- `Assets/Res/Animation/Player/Clips/...`

---

## 8. 实施原则

### 8.1 先加“桥接层”，再做“内部分拆”

建议优先新增：

- `PlayerStateHub`
- `PlayerAnimationController`
- `PlayerHudPresenter`
- `PlayerWeaponPresentationController`

不要一开始就重写：

- `PrototypeFpsMovementModule`
- `PlayerWeaponController`

原因是这两块已经承载当前可玩原型的核心玩法稳定性。

### 8.2 先把状态和边界稳定，再补动画资产

推荐顺序是：

1. 先定义状态汇聚接口
2. 再让全身动画接上
3. 再让第一人称动画接上
4. 最后再做后坐力、换弹细节、IK 和特效表现

### 8.3 所有动画接入都要接受“玩法无回归”约束

每次接入新动画层时，都必须确保以下功能不退化：

- 移动、冲刺、蹲伏、跳跃
- ADS
- 开火、换弹、切枪
- 医疗、投掷
- UI 打开时控制冻结
- 死亡 / 受击

---

## 9. 最终完成标准

当以下条件全部满足时，可以认为主角控制与动画控制框架基本成型：

1. 主控制器只做装配、上下文切换和更新顺序，不再直接拼接 HUD 和动画状态。
2. 移动、战斗、医疗、投掷都有稳定的玩法权威模块。
3. 存在统一的 `PlayerStateHub` 或等价状态汇聚层。
4. 第三人称身体动画拥有独立 Animator Controller。
5. 第一人称手臂 / 武器动画拥有独立 Animator 或表现驱动层。
6. 武器表现层从 `PlayerWeaponController` 中独立出来。
7. 动画只读状态和信号，不再反向掌控核心玩法结果。
8. 预制体结构和命名足够清晰，新成员可以快速定位：
   - 哪个是玩法层
   - 哪个是第一人称表现
   - 哪个是第三人称表现
   - 哪个是命中盒

---

## 10. 建议优先级结论

如果只看“后续制作收益 / 改动风险”比值，建议优先级如下：

1. 先建立 `PlayerStateHub` 与 `PlayerAnimationController` 的边界
2. 再把 `PlayerWeaponController` 的第一人称表现职责抽出
3. 再接第三人称身体 Animator
4. 再接第一人称手臂 / 武器 Animator
5. 最后再拆 `PrototypeFpsMovementModule` 中的头部晃动与噪声模块

这条顺序最不容易打断当前原型节奏，也最适合边做边验证。
