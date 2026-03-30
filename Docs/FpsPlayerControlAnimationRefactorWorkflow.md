# FPS 主角控制与动画控制重构流程路线

## 1. 文档定位

本文档是 `Docs/RefactorRoadmap.md` 中“阶段 D：拆分玩家控制器”的 FPS 玩家专题落地路线，重点解决两件事：

1. 如何在不破坏当前原型可玩的前提下重构主角控制。
2. 如何逐步接入第三人称身体动画与第一人称手臂 / 武器动画。

它不是一次性大重写方案，而是一条可分阶段提交、可逐步验收、可随时停在中间版本继续做内容的路线。

---

## 1.1 当前进度与续接点

更新时间：`2026-03-28`

### 当前进度

| 阶段 | 状态 | 当前结果 |
| --- | --- | --- |
| 阶段 0 | 已完成 | `FpsPlayer.prefab` 已完成 Rig 命名清理、显式武器视图挂点整理，并新增 `PlayerAnimationRigRefs`。 |
| 阶段 1 | 已完成 | `PrototypeFpsController` 已抽离 `PlayerLookController` 与 `PlayerHudPresenter`，主控制器开始退回协调入口。 |
| 阶段 2 | 已完成 | 已新增 `PlayerStateHub`、`PlayerActionChannel`，并让 `PlayerHudPresenter` 改为只依赖状态快照。 |
| 阶段 3 | 基础版已完成 | 已接入第三人称 `Animator`、`PlayerFullBodyAnimatorDriver`、基础 locomotion / jump / death 状态机，以及上半身 aim / fire / reload 占位层。 |
| 阶段 4 | 未开始 | 下一步进入第一人称手臂与武器表现层拆分。 |

### 当前代码落点

- 预制体整理：`Assets/Res/Prefabs/Player/FpsPlayer.prefab`
- Rig 引用组件：`Assets/Res/Scripts/FPS/PlayerAnimationRigRefs.cs`
- 视角控制拆分：`Assets/Res/Scripts/FPS/PlayerLookController.cs`
- HUD 表现拆分：`Assets/Res/Scripts/FPS/PlayerHudPresenter.cs`
- 动作仲裁层：`Assets/Res/Scripts/FPS/PlayerActionChannel.cs`
- 状态汇聚层：`Assets/Res/Scripts/FPS/PlayerStateHub.cs`
- 主协调入口：`Assets/Res/Scripts/FPS/PrototypeFpsController.cs`
- 全身动画驱动：`Assets/Res/Scripts/FPS/PlayerFullBodyAnimatorDriver.cs`
- 第三人称 Animator Controller：`Assets/Res/Animations/FPS/FpsPlayerFullBody.controller`
- 动画占位资源：`Assets/Res/Animations/FPS/Clips/*.anim`

### 下次继续时的建议起点

1. 从阶段 4 开始，拆出第一人称武器表现控制器与手臂 Animator Driver。
2. 优先把 `ADS / Fire / Reload / Equip` 从 `PlayerWeaponController` 的表现职责中抽离。
3. 继续保持玩法权威不变，表现层只消费 `PlayerStateHub` 和动作事件，不反向驱动伤害、位移和弹药。

---

## 2. 基线判断

### 2.1 当前适合继续保留的部分

- `PrototypeFpsInput` 作为统一输入入口继续保留。
- `PrototypeFpsMovementModule` 作为当前位移权威逻辑继续保留。
- `PlayerWeaponController` 作为当前武器玩法权威逻辑继续保留。
- `PlayerMedicalController` / `PlayerThrowableController` 继续保留。
- `CharacterController` 仍作为玩家主位移方案。

### 2.2 当前必须开始收敛的部分

- `PrototypeFpsController` 的总控职责过重。
- 动画系统还没有真正资产化、状态机化。
- 第一人称与第三人称表现没有统一框架。
- 武器表现职责混在 `PlayerWeaponController` 中。
- HUD 文本拼装仍在主控制器里。

### 2.3 总体策略

整体策略建议是：

- 先加桥接层
- 再做状态汇聚
- 再挂动画
- 最后再做精细拆分

也就是说，优先建立新边界，而不是一开始就重写旧模块内部。

---

## 3. 执行约束

整个重构过程中建议遵守以下约束：

1. 不重做输入系统。
2. 不把位移切到 Root Motion。
3. 不要求第一阶段就把所有动画资产全部补齐。
4. 每一阶段结束时项目都应保持：
   - 可进入局内
   - 可移动
   - 可开火
   - 可换弹
   - 可治疗
   - 可投掷
   - 可打开局内 UI
5. 尽量先抽“表现层”，后动“玩法权威层”。
6. 没有明确收益前，不先拆 asmdef，不先大规模改目录。

---

## 4. 分阶段路线

## 阶段 0：现状固化与命名清理

### 目标

在不改玩法的前提下，把后续动画接入最容易踩坑的预制体问题先处理掉。

### 建议动作

1. 整理 `FpsPlayer.prefab` 的节点命名：
   - 把命中盒根节点与可见身体根节点区分命名
2. 在 `ViewCamera` 下显式建立并序列化：
   - `WeaponView_Primary`
   - `WeaponView_Secondary`
   - `WeaponView_Melee`
3. 新增一个专门持有动画引用的组件，例如：
   - `PlayerAnimationRigRefs`
4. 明确记录：
   - 第三人称模型节点
   - 第一人称手臂节点
   - 枪口节点
   - 视图模型挂点

### 完成标准

- 新成员看预制体层级能马上区分逻辑层、命中盒、第三人称可见层、第一人称表现层。
- 后续动画相关脚本不再依赖“字段为空时运行时自动找节点 / 自动创建节点”。

### 风险控制

- 本阶段不允许改玩法逻辑。
- 本阶段只接受预制体与序列化引用的清理。

---

## 阶段 1：主控制器瘦身，不改玩法权威

### 目标

把 `PrototypeFpsController` 从“功能承载者”降级为“协调入口”。

### 建议动作

1. 从 `PrototypeFpsController` 中优先抽出：
   - `PlayerLookController`
   - `PlayerHudPresenter`
2. 保留 `PrototypeFpsController` 或重命名为 `PlayerControlRoot`，只负责：
   - 组件装配
   - 模块 Update 顺序
   - UI / Gameplay 上下文切换
   - 鼠标锁定策略
3. 把 HUD 的文本拼接和显示逻辑整体迁移到 `PlayerHudPresenter`。
4. 把 `HandleLook` 迁移到独立控制器，避免后续动画和镜头系统继续耦合在主控制器里。

### 预期产出

- `PlayerControlRoot`
- `PlayerLookController`
- `PlayerHudPresenter`

### 完成标准

- 主控制器中不再直接拼装长字符串 HUD。
- 主控制器中不再直接操作相机俯仰。
- 新增玩法模块时，不需要继续往主控制器塞大量细节。

### 阶段验收

- 进入局内后，移动、开火、瞄准、治疗、投掷、UI 焦点切换行为完全保持一致。

---

## 阶段 2：建立状态汇聚层与动作仲裁层

### 目标

为动画和 HUD 建立统一数据源，避免后续多个系统到处拉状态。

### 建议动作

1. 新增 `PlayerStateHub`，每帧汇总：
   - 移动状态
   - 姿态状态
   - 武器状态
   - 瞄准状态
   - 医疗 / 投掷状态
   - 生命体征状态
2. 新增 `PlayerActionChannel`，把当前隐式动作优先级显式化。
3. 所有表现层统一从 `PlayerStateHub` 读状态，不直接跨模块拼装。
4. HUD 改为依赖 `PlayerStateHub`，不再直接串读多个控制器。

### 推荐的动作优先级

建议显式定义为：

1. `Dead`
2. `UiFocused`
3. `Medical`
4. `Throwable`
5. `Reload`
6. `Weapon`
7. `Idle`

### 预期产出

- `PlayerStateHub`
- `PlayerAnimationFrame`
- `PlayerActionChannel`

### 完成标准

- 动画系统与 HUD 都可以只依赖一个统一状态源。
- 医疗 / 投掷 / 武器行为的互斥逻辑不再散落在主控制器 Update 中。

### 阶段验收

- 同一帧内不会出现“治疗动画播放但实际还能开火”之类的状态冲突。
- 打开 UI 时，状态汇聚仍稳定，HUD 仍可读到冻结后的正确状态。

---

## 阶段 3：接入第三人称身体动画

### 目标

让 `FpsPlayer.prefab` 中的第三人称可见身体真正拥有可用 Animator。

### 建议动作

1. 给第三人称可见身体节点挂：
   - Avatar
   - `AnimatorController`
2. 新增 `PlayerFullBodyAnimatorDriver`，读取 `PlayerStateHub`，写 Animator 参数。
3. 第一版只做最低限度状态：
   - Idle
   - Walk
   - Sprint
   - Crouch
   - Jump / Fall / Land
   - Aim
   - Fire
   - Reload
   - Death
4. 保持 Root Motion 关闭。
5. 若当前模型资源不完整，先用占位动画或统一站姿 / 移动 clip 建立状态机结构。

### 当前落地情况（2026-03-28）

- 已新增 `PlayerFullBodyAnimatorDriver`，从 `PlayerStateHub` 读取状态并写入 Animator 参数。
- 已为 `CharacterVisualRig` 绑定 `FpsPlayerFullBody.controller`。
- 已为 `Assets/Res/Packages/素体.fbx` 生成 Generic Avatar，并绑定到第三人称 Animator。
- 已基于 `素体.fbx` 的双骨架动作资源生成一组合并后的基础 clip，放在 `Assets/Res/Animations/FPS/Clips/`。
- 已建立基础 locomotion / crouch / jump / land / death 状态，以及上半身 aim / fire / reload 占位层。

### 推荐范围

第一版不要一开始做：

- 复杂换弹分段
- 左手 IK
- 多武器专属动作树
- 高复杂 Additive Layer

先把基本状态机结构立住。

### 完成标准

- 其他角色观察玩家时，可以看到基础移动、蹲伏、瞄准、开火、换弹、死亡表现。
- 受击与死亡可以进入明确动画状态。

### 阶段验收

- 武器切换不会导致 Animator 卡死。
- 冲刺、蹲伏、跳跃切换时不会与 `CharacterController` 位移冲突。

---

## 阶段 4：拆出武器表现层并接入第一人称动画

### 目标

把第一人称武器与手臂表现从 `PlayerWeaponController` 中拆出来，建立独立动画控制链。

### 建议动作

1. 新增 `PlayerWeaponPresentationController`：
   - 负责视图模型实例化
   - 负责切枪显隐
   - 负责 ADS pose
   - 负责 recoil / sway / equip / reload / fire 动画输入
2. `PlayerWeaponController` 只保留：
   - 武器实例
   - 弹药与火模式
   - 射击与换弹权威逻辑
   - 命中判定
3. 新增 `PlayerFpArmsAnimatorDriver` 或等价控制器：
   - 负责第一人称手臂动画状态机
4. 把当前 `PlayerAimController -> PlayerWeaponController.UpdateAimPresentation` 的路径改为：

```text
PlayerAimController
    -> PlayerStateHub
        -> PlayerWeaponPresentationController
        -> PlayerFpArmsAnimatorDriver
```

5. 保留当前 ADS Pose 搜索逻辑作为兼容层，但开始引入 `WeaponPresentationProfile`。

### 第一版建议先接入的表现

- 切枪
- ADS 进出
- 开火后坐力
- 换弹整体动作
- 近战挥击

### 完成标准

- 第一人称表现不再由 `PlayerWeaponController` 直接驱动。
- 武器玩法逻辑与第一人称表现逻辑彻底分层。

### 阶段验收

- 半自动、连发、点射、近战都能触发正确第一人称动作。
- 瞄准时切枪、换弹、冲刺切换不会导致手臂或武器模型错位。

---

## 阶段 5：把医疗与投掷纳入统一上半身动作系统

### 目标

让医疗与投掷不再只是“玩法逻辑成功时弹一条反馈”，而是接入统一动作管线。

### 建议动作

1. 让 `PlayerMedicalController` 与 `PlayerThrowableController` 向 `PlayerActionChannel` 发布动作状态。
2. 第三人称身体动画接入：
   - 医疗使用
   - 投掷准备 / 投掷释放
3. 第一人称动画接入：
   - 用药
   - 缠绷带 / 止血
   - 投掷起手与释放
4. 明确打断规则：
   - 医疗期间是否允许切枪
   - 投掷期间是否允许 ADS
   - 冲刺是否强制退出医疗 / 投掷

### 完成标准

- 医疗、投掷不再是 UI 反馈级逻辑，而是完整的动作状态。
- 上半身动作互斥规则由统一系统管理。

### 阶段验收

- 医疗 / 投掷 / 开火不会再同时争抢同一套上半身表现。
- HUD、动画、玩法层对“当前动作”认知一致。

---

## 阶段 6：细节打磨与可扩展化

### 目标

在框架稳定后，逐步提升第一人称射击手感和动画完成度。

### 可继续推进的内容

1. 把 `PrototypeFpsMovementModule` 继续拆为：
   - `PlayerLocomotionMotor`
   - `PlayerLocomotionPresentation`
   - `PlayerMovementNoiseEmitter`
2. 引入更细的武器表现参数：
   - 不同枪械的 recoil curve
   - idle sway
   - movement sway
   - sprint pose
3. 增加更细的换弹阶段：
   - 开始换弹
   - 取弹匣
   - 装填
   - 拉机柄
4. 增加左手 IK / Weapon Socket 对齐。
5. 增加脚步动画事件与声音联动。
6. 增加受击方向动画、死亡方向动画、倒地表现。

### 完成标准

- 后续加入新枪、新动作、新人物技能时，主要是加状态和加资产，而不是回头修改主控制器大逻辑。

---

## 5. 每阶段建议交付物

| 阶段 | 建议代码交付物 | 建议资源交付物 | 必做验证 | 当前状态 |
| --- | --- | --- | --- | --- |
| 阶段 0 | Rig 引用组件、预制体命名整理 | 预制体层级整理 | 进入场景、武器显示正常 | 已完成 |
| 阶段 1 | `PlayerLookController`、`PlayerHudPresenter` | 无强制 | 原有玩法无回归 | 已完成 |
| 阶段 2 | `PlayerStateHub`、`PlayerActionChannel` | 无强制 | 状态一致性验证 | 已完成 |
| 阶段 3 | `PlayerFullBodyAnimatorDriver` | FullBody Animator Controller | 第三人称移动 / 开火 / 死亡 | 基础版已完成 |
| 阶段 4 | `PlayerWeaponPresentationController`、`PlayerFpArmsAnimatorDriver` | FP Arms Animator Controller、武器表现配置 | 第一人称 ADS / Fire / Reload | 未开始 |
| 阶段 5 | 医疗 / 投掷动作接入 | 医疗 / 投掷动画资源 | 上半身动作互斥验证 | 未开始 |
| 阶段 6 | 进一步模块拆分与表现参数化 | Recoil / Sway / IK / 细节动画资源 | 长线稳定性验证 | 未开始 |

---

## 6. 推荐目录规划

建议后续按以下目录组织，避免继续把新东西堆回 `FPS` 根目录：

```text
Assets/Res/Scripts/FPS/Core/
Assets/Res/Scripts/FPS/Control/
Assets/Res/Scripts/FPS/State/
Assets/Res/Scripts/FPS/Animation/
Assets/Res/Scripts/FPS/Presentation/
Assets/Res/Scripts/FPS/Hud/
Assets/Res/Animation/Player/
Assets/Res/Animation/Weapons/
```

建议放置思路：

- `Core/`：控制入口、公共枚举、动作状态定义
- `Control/`：移动、瞄准、武器、医疗、投掷等玩法控制
- `State/`：状态汇聚、快照、事件桥接
- `Animation/`：Animator Driver、动画桥接组件
- `Presentation/`：相机反馈、头部晃动、武器表现
- `Hud/`：局内 HUD 与状态显示

---

## 7. 核心测试清单

无论推进到哪个阶段，以下测试建议每轮都跑一遍：

1. 站立、前进、后退、横移、蹲伏、冲刺。
2. 起跳、空中、落地、落地后立即再移动。
3. ADS 进出，尤其是冲刺中禁止 ADS 的切换。
4. 主武器 / 副武器 / 近战切换。
5. 半自动、全自动、点射。
6. 换弹、换弹中切枪、换弹后开火。
7. 医疗、止血、夹板、止痛。
8. 投掷物使用与冷却。
9. 打开局内背包 / 搜刮窗口时输入冻结和光标释放。
10. 死亡、复活 / 重生、重新进入局内。

---

## 8. 当前最推荐的实施顺序

如果接下来只做一条最稳妥的执行线，推荐是：

1. 阶段 0：预制体命名与显式挂点整理
2. 阶段 1：主控制器瘦身
3. 阶段 2：状态汇聚层
4. 阶段 3：第三人称身体动画
5. 阶段 4：第一人称武器表现拆分
6. 阶段 5：医疗 / 投掷动作接入

原因是：

- 它最大限度复用现有可玩的玩法代码。
- 它把风险集中在“新加桥接层”，而不是“改动玩法底层”。
- 它能在任何阶段暂停，继续做内容，不会把项目卡死在半重构状态。

---

## 9. 最终里程碑判断

当以下条件成立时，可以认为 FPS 主角控制与动画重构进入稳定阶段：

1. `PrototypeFpsController` 已经退化为协调入口。
2. 存在统一的状态汇聚层与动作仲裁层。
3. 第三人称身体动画已接入。
4. 第一人称手臂 / 武器动画已接入。
5. `PlayerWeaponController` 不再承载视图模型与动画表现职责。
6. 医疗、投掷、开火、换弹都有一致的动作状态定义。
7. 新增一把枪或一个动作时，不再需要修改多个巨型类。

达到这一步后，后续就可以把重心从“结构止血”切回“武器手感、动画质量和内容制作”。
