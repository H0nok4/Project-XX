# 用 FPS Framework 替换 JUTPS 玩家操作的详细步骤文档

## 1. 先说结论

如果你的目标是：

- 保留 JUTPS 的敌人、AI、载具、世界交互原型
- 但把玩家第一人称移动、视角、武器展示和手感换成 FPS Framework

最稳的做法不是：

- 在同一个玩家物体上把 `JUCharacterController` 换成 `FirstPersonController`

而是：

- 把 `JUTPS 玩家整条 prefab 链` 换成 `FPS Framework 玩家整条 prefab 链`
- 然后用桥接层把 JUTPS 世界系统继续接到这个新玩家上

一句话说清楚：

- 替换对象是“玩家链路”，不是“单个控制器组件”

## 2. 为什么不是简单换一个组件

从 JUTPS 当前代码和 prefab 关系看，玩家控制并不是 `JUCharacterController` 一个人在干活，而是一整条链在协作：

- `FPS Character.prefab`
- `FirstPerson Camera Controller.prefab`
- `Player Character Inputs.asset`
- `JUTPS User Interface`
- `JUCharacterController`
- `JUInteractionSystem`
- `JUInventory`
- `DriveVehicles`
- `JUHealth`

关键耦合点如下：

### `JUCharacterController`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/JUCharacterController.cs`

它直接读取：

- `JUPlayerCharacterInputAsset`

并直接控制：

- 角色移动
- 蹲伏 / 匍匐 / 跳跃 / 翻滚
- 物品切换
- FireMode / Aiming
- 武器朝向与 IK

### `TPSCameraController`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Camera Controllers/TPSCameraController.cs`

它也直接读取：

- `JUPlayerCharacterInputAsset`

同时又直接观察：

- `JUCharacterController`

来切相机状态：

- Normal
- FireMode
- Aiming
- Driving
- Dead

### `JUInteractionSystem`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Interaction System/JUInteractionSystem.cs`

默认也走：

- `JUPlayerCharacterInputAsset`

### `DriveVehicles`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Abilities/DriveVehicles.cs`

它不仅依赖：

- `JUInteractionSystem`

还深度依赖：

- `JUCharacterController`
- `JUTPSAction.TPSCharacter`
- 角色动画参数
- 角色移动启停
- 角色当前装备状态

这意味着：

- 车辆进入不是一个独立系统
- 它是绑定在 JUTPS 玩家角色身体上的能力

### 多个 JUTPS UI / Scene 脚本会直接找 `JUCharacterController`

例如：

- `JUGameManager.PlayerController`
- `UIInteractMessages`
- `Crosshair`
- `UIItemInformation`
- `JUScopeSystemUI`
- `MobileRig`
- `SceneController`

这些脚本大量假设：

- 场景中 tag 为 `Player` 的对象上一定有 `JUCharacterController`

所以一旦你把玩家换成 FPS Framework：

- 这些脚本要么会失效
- 要么需要停用
- 要么需要专门桥接

## 3. 推荐替换范围

推荐按三个范围理解这件事。

### 范围 A：只替换玩家移动 / 相机 / 枪感

保留：

- JUTPS 敌人
- JUTPS AI
- JUTPS 场景交互物

替换：

- 玩家角色
- 玩家相机
- 玩家武器展示与输入

这是最推荐的第一步。

### 范围 B：替换玩家，并保留部分 JUTPS 世界兼容

在范围 A 基础上，再补桥接：

- JUTPS 敌人能识别并攻击 FPSF 玩家
- FPSF 玩家能继续触发部分 JUTPS 世界交互

这是最适合 Project-XX 的正式接法。

### 范围 C：替换玩家，但继续无缝复用 JUTPS 载具进入链

这个范围不适合作为第一阶段目标。

原因：

- `DriveVehicles` 深度绑定 `JUCharacterController`
- 不能直接拿来接 FPSF 玩家

建议放到后续单独桥接，不要一开始就追求这一步。

## 4. 推荐的总体架构

最推荐的结构是：

- `FPS Framework` 负责玩家第一人称执行层
- `JUTPS` 负责敌人、AI、载具、世界原型层
- `Project-XX` 负责规则、UI、任务、仓库、成长和正式存档

玩家侧：

- `Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab`
- `Assets/Akila/FPS Framework/Prefabs/World/Game Manager.prefab`

敌人与世界侧：

- 继续使用 `Assets/Julhiecio TPS Controller/...`

## 5. 迁移前的准备

在真正开始之前，建议先做这几件事：

1. 不要在正式主场景上直接替换。
2. 先做一个集成测试场景。
3. 先接受“第一阶段不做车辆无缝接管”。
4. 默认停用 JUTPS 自带玩家 HUD / Pause / Scope UI。

建议新建一张测试场景，例如：

- `Assets/Scenes/Sandbox/FPSF_JUTPS_PlayerReplacement.unity`

如果你现在已经有一张玩法测试图，也可以复制一份只用于迁移验证。

## 6. 详细替换步骤

## 6.1 第一步：识别并下线 JUTPS 玩家链

在测试场景里，先找到并停用这些对象：

- `FPS Character`
- `FirstPerson Camera Controller`
- `JUTPS User Interface`

如果场景里有以下对象，也建议一并检查并优先停用：

- `SceneController`
- `MobileRig`
- 任何依赖 `JUGameManager.PlayerController` 的 UI

原因很简单：

- 这些对象默认都把“玩家 = JUCharacterController”写死了

不要做的事：

- 不要保留 `FPS Character` 再额外塞一个 FPSF 控制器
- 不要试图在同一个玩家物体上同时启用 JUTPS 和 FPSF 两套输入/相机/武器系统

## 6.2 第二步：把 FPS Framework 玩家链放进场景

拖入这些对象：

- `Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab`
- `Assets/Akila/FPS Framework/Prefabs/World/Game Manager.prefab`

可选临时拖入：

- `Assets/Akila/FPS Framework/Prefabs/HUD/HUD.prefab`

但如果你准备尽快接 Project-XX 自己的 HUD，建议只把它当临时验证 HUD。

## 6.3 第三步：先只验证 FPSF 玩家本身是否跑通

这一步先不管 JUTPS 兼容，只确认：

- 能移动
- 能转视角
- 能拿枪
- 能开火
- 能正常显示基础 HUD

推荐最小检查项：

1. `Player.prefab` 落点是否正确
2. `Inventory.startItems` 是否放入了一把 FPSF 武器
3. `Game Manager.prefab` 是否正常生成 `UIManager` 和 `DeathCamera`
4. 是否没有 JUTPS 相机和 FPSF 相机同时存在

## 6.4 第四步：给 FPSF 玩家设置 JUTPS 可见的 Tag / Layer

JUTPS 的 AI 感知不仅看组件，还会看：

- Layer
- Tag

相关逻辑主要在：

- `Assets/Julhiecio TPS Controller/Scripts/AI/Sensors/JUFieldOfViewSensor.cs`

默认它会关注的目标标签包括：

- `Player`
- `Players`
- `Character`
- `Characters`
- `Vehicle`
- `Vehicles`

所以建议你至少做两件事：

1. 把 FPSF 玩家根物体的 tag 设成 `Player`
2. 把 FPSF 玩家的主要碰撞体放到一个会被 JUTPS AI 感知到的目标 Layer 上

建议：

- 如果场景已有 `Player` 或 `Character` 层，就优先复用
- 如果没有，就在 AI 的 `TargetsLayer` 里显式加入 FPSF 玩家所在层

## 6.5 第五步：不要直接复用 JUTPS 的“角色层 9 / 骨骼层 15”伤害方案

这是迁移里最容易踩坑的地方。

JUTPS 的子弹逻辑在：

- `Assets/Julhiecio TPS Controller/Scripts/Physics/Bullet.cs`

它对伤害目标有明显分支：

- 如果碰到 layer `9` 或 `15`
  - 优先当成 `DamageableBodyPart`
  - 否则会尝试找 `JUCharacterBrain`
- 其他情况
  - 才会对命中的对象直接 `TryGetComponent<JUHealth>`

这意味着：

- 如果你把 FPSF 玩家碰撞体直接放到 JUTPS 的角色/骨骼伤害层
- 但又没有补齐 `JUCharacterController` 或 `DamageableBodyPart`

那么 JUTPS 子弹很可能打不到你的 FPSF 玩家。

第一阶段最稳的建议是：

- 不要直接沿用 JUTPS 的角色骨骼伤害层
- 先走 `JUHealth` 兼容代理路线

## 6.6 第六步：给 FPSF 玩家补一个 JUTPS 兼容健康代理

JUTPS 敌人和武器大量直接依赖：

- `JUHealth`
- `DamageableBodyPart`

相关逻辑出现在：

- `Attack.cs`
- `Bullet.cs`
- `Damager.cs`
- `JU_AI_PatrolCharacter.cs`

第一阶段推荐的最小兼容方式是：

1. 在 FPSF 玩家身上额外挂一个 `JUHealth` 代理组件。
2. 让它的生命值与 FPSF 的 `Damageable` 同步。
3. 由桥接脚本负责：
   - JUTPS 伤害进来时，同步扣到 FPSF `Damageable`
   - FPSF 玩家死亡时，把 `JUHealth` 标记到正确状态

建议后续自己实现一个桥，例如：

- `JutpsHealthProxy`

它至少需要负责：

- 监听 `JUHealth.OnDamaged`
- 转发到 FPSF `Damageable`
- 把 FPSF `Damageable.health` 回写到 `JUHealth.Health`
- 在死亡时保持两边状态一致

### 第一阶段建议的代理形态

建议把 `JUHealth` 挂在一个你明确控制的“兼容受击根”上：

- 它要有 Collider
- 它要能被 JUTPS AI 和武器命中
- 但不要强行放进 JUTPS 骨骼层方案

如果你后续确实需要：

- 头部伤害
- 四肢伤害
- 身体部位伤害倍率

再进入下一阶段，去补：

- `DamageableBody`
- `DamageableBodyPart`

不要在第一天就把这部分一起做完。

## 6.7 第七步：如果要继续复用普通 JUTPS 交互，可以只桥 `JUInteractionSystem`

普通 JUTPS 交互物的基类是：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Interaction System/JUInteractable.cs`

其中：

- `JUGeneralInteractable.CanInteract()` 基本只关心 `InteractionEnabled`
- 它并不强依赖 `JUCharacterController`

这意味着普通交互有一个可行方案：

1. 在 FPSF 玩家上额外挂一个 `JUInteractionSystem`
2. 把它设成：
   - `UseDefaultInputs = false`
3. 不让它自己读 JUTPS 输入资产
4. 当 FPSF 玩家按下交互键时，由桥接脚本主动调用：
   - `FindNearInteractables()`
   - `Interact(NearestInteractable)`

你可以后续实现一个桥，例如：

- `JutpsGeneralInteractionBridge`

它负责：

- 读取 FPSF 的交互输入
- 驱动 `JUInteractionSystem`
- 把交互提示翻译给 Project-XX HUD

这条路适合：

- 门
- 开关
- 简单机关
- 一次性交互点

## 6.8 第八步：不要把 `JUVehicleInteractable + DriveVehicles` 当成第一阶段可直接复用

车辆交互的判定在：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Interaction System/JUVehicleInteractable.cs`

它明确会检查：

- `DriveVehicles`
- `JUCharacterController`
- `Rigidbody`
- `character.IsRolling`
- `character.IsRagdolled`

而 `DriveVehicles` 本身又深度依赖：

- `JUTPSAction.TPSCharacter`
- `JUCharacterController`
- 角色动画参数
- 角色移动启停
- 当前装备状态

所以结论很明确：

- 普通 `JUInteractionSystem` 还能桥
- `JUVehicleInteractable + DriveVehicles` 不能当成即插即用功能

如果你现在的目标只是“替换玩家手感”，建议第一阶段先这样处理：

1. 保留场景里的 JUTPS 车辆 prefab
2. 但暂时不开放 FPSF 玩家上车
3. 等玩家侧稳定后，再单独做 `RaidVehicleBridge`

## 6.9 第九步：停用所有依赖 `JUGameManager.PlayerController` 的 JUTPS 玩家 UI

这一步很重要。

以下系统默认都把：

- `tag=Player`
- `JUCharacterController`

当成玩家前提：

- `JUGameManager`
- `UIInteractMessages`
- `Crosshair`
- `UIItemInformation`
- `JUScopeSystemUI`
- `MobileRig`
- `SceneController`

因此在 FPSF 玩家替换后，建议：

- 停用这些 JUTPS 玩家 UI 和场景壳
- 改用 Project-XX 自己的 HUD / Pause / Scope / Interaction Prompt

否则你会遇到：

- NullReference
- UI 读不到玩家
- Scope / Crosshair 不更新
- Respawn 逻辑错误

## 7. 推荐的执行顺序

为了让替换过程可控，建议按这个顺序推进。

### 阶段 1：只跑通 FPSF 玩家

目标：

- 玩家移动
- 视角
- 枪械
- 基础 HUD

不要急着做：

- 敌人伤害
- 车辆
- JUTPS 交互

### 阶段 2：补 JUTPS 敌人可攻击玩家

目标：

- JUTPS AI 能看见 FPSF 玩家
- JUTPS 武器能打到 FPSF 玩家
- 死亡状态正确同步

这一步的核心是：

- Tag / Layer
- `JUHealth` 代理

### 阶段 3：补普通世界交互桥

目标：

- FPSF 玩家能触发普通 `JUInteractable`

### 阶段 4：最后才考虑车辆桥接

目标：

- 明确是否真的需要 FPSF 玩家继续使用 JUTPS 车辆

这一步不要前置。

## 8. 替换时的系统去留建议

| 系统 | 建议 |
| --- | --- |
| `JUCharacterController` | 从玩家身上移除，不和 FPSF 混挂 |
| `TPSCameraController / FirstPerson Camera Controller` | 从玩家链移除 |
| `JUPlayerCharacterInputAsset` | 不再驱动玩家本体 |
| `JUInteractionSystem` | 可作为普通交互桥保留 |
| `DriveVehicles` | 第一阶段不要接 |
| `JUInventory` | 不作为 FPSF 玩家主库存 |
| `JUHealth` | 作为兼容代理可保留 |
| `DamageableBody / DamageableBodyPart` | 只有需要部位伤害时再补 |
| `JUTPS UI` | 玩家侧尽量停用 |
| `JUTPS AI / Enemies` | 继续保留 |
| `JUTPS Vehicles` | 继续保留，但先不接玩家 |

## 9. 最小验证清单

当你完成第一轮替换后，至少要验证这些：

1. 场景里不存在两个同时可控的玩家。
2. 场景里不存在两套同时驱动视角的相机。
3. FPSF 玩家 tag / layer 已正确设置给 JUTPS AI 使用。
4. JUTPS 玩家 UI 已关闭，不再尝试读取 `JUCharacterController`。
5. JUTPS 敌人已经能发现 FPSF 玩家。
6. JUTPS 伤害进入 FPSF 玩家时，血量和死亡状态能正确同步。
7. FPSF 玩家开火不会再驱动 JUTPS 武器链。

## 10. 最终建议

最推荐的落地方式是：

1. 先把 JUTPS 玩家整条链替掉，不做混挂。
2. 先跑通 FPSF 玩家自己的移动、镜头和枪感。
3. 用 `JUHealth` 代理解决 JUTPS 敌人与伤害兼容。
4. 用 `JUInteractionSystem` 仅桥普通世界交互。
5. 车辆桥接单独延期，不和第一阶段绑定。

如果你后续愿意继续推进，下一步最值得做的不是再写更多文档，而是：

- 先搭一个 `FPSF Player + JUTPS Enemy` 的联合测试场景
- 然后补一个最小的 `JutpsHealthProxy`

这会比继续停留在纯分析层更快验证路线是否正确。

