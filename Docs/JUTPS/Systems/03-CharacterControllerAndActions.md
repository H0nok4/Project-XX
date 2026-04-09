# JUTPS 系统拆解 03：角色控制与动作能力

## 1. 模块定位

这一层是 JUTPS 的真正核心。

关键关系：

- `JUCharacterBrain` 提供角色状态、动作接口、装备接口、IK 与动画桥接
- `JUCharacterController` 负责“玩家驱动版”的具体输入接线
- AI 也是通过同一套角色接口在开车，而不是另写一套怪物控制器

这正是本包最有价值的部分。

## 2. 关键类

### `JUCharacterBrain`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Libraries/Character Controller Libs/JUCharacterControllerCore.cs`

职责：

- 聚合角色最核心状态：
  - 移动
  - 跑步/冲刺
  - 蹲伏/匍匐/翻滚
  - 跳跃
  - FireMode / Aiming
  - 当前装备物
  - 受伤/死亡/布娃娃
- 持有关键依赖：
  - `Animator`
  - `Rigidbody`
  - `JUHealth`
  - `JUInventory`
  - `DriveVehicles`
  - 武器与近战缓存
- 提供统一动作接口：
  - `_Move`
  - `_Jump`
  - `_Crouch`
  - `_Prone`
  - `_Roll`
  - `DefaultUseOfAllItems`
  - `SwitchToItem`

最重要的架构含义：

- 角色“状态机”是隐式写在组件逻辑里的，不是单独 Scriptable FSM
- 角色是所有战斗行为的统一宿主

### `JUCharacterController`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/JUCharacterController.cs`

职责：

- 继承 `JUCharacterBrain`
- 在 `Update` / `FixedUpdate` 中执行：
  - GroundCheck
  - HealthCheck
  - Animator 参数同步
  - 玩家输入读取
  - 旋转、移动、瞄准、武器方向控制

关键逻辑：

- 使用 `JUPlayerCharacterInputAsset` 读取移动、跑、蹲、翻滚、切枪、射击、瞄准等输入
- 通过 `DefaultUseOfAllItems` 把“输入”转发到装备系统
- 通过 `SwitchToItem` 驱动角色装备切换

## 3. 动作能力是怎么拼上的

角色 prefab 上并不是只有一个控制器，而是“控制器 + 能力插件”的组合：

- `JUCharacterController`
- `JUInventory`
- `DriveVehicles`
- `JUInteractionSystem`
- `JUCoverController`
- `JUFootPlacement`
- `AdvancedRagdollController`
- `DamageableBody`

这意味着：

- 大部分“能力”并不写死在 `JUCharacterController` 里
- 控制器负责统一角色状态和调用时机
- 具体能力由额外组件承担

## 4. 输入到动作的主链路

典型链路如下：

1. `JUCharacterController.ControllerInputs()` 读取输入资产。
2. 输入被翻译为角色层状态：
   - `HorizontalX/VerticalY`
   - `IsRunning`
   - `IsCrouched`
   - `IsProne`
   - `IsAiming`
3. 然后调用：
   - `_Jump`
   - `_Roll`
   - `SwitchToItem`
   - `DefaultUseOfAllItems`
4. 角色控制器进一步驱动动画、IK、武器朝向、身体朝向。

## 5. 装备与动作的耦合方式

`JUCharacterBrain` 中有一个非常关键的方法：

- `DefaultUseOfAllItems(...)`

这个方法把“射击/瞄准/近战/换弹/拳击”统一分发给当前左右手物品。

说明：

- 角色层不直接关心“当前是手枪、步枪、刀还是拳头”
- 它只关心“当前左右手各自挂着什么”
- 具体行为由武器/物品组件自己实现

这是一个不错的扩展点：

- 你后面做技能、战术道具、消耗品时，可以沿着“可持有物”这条轴扩展

## 6. 角色控制层的优点

优点：

- TPS/FPS 基础动作完整
- 玩家与 AI 共用一套动作底座
- 与武器、载具、交互衔接自然
- 适合先做“可玩的战斗样机”

## 7. 角色控制层的局限

局限也很明显：

- 角色状态很多，逻辑偏集中，后续会越来越难维护
- 不是显式状态机，复杂动作互斥关系要靠代码分支控制
- 缺少高层“战斗能力系统”抽象
- 没有天然的 RPG 属性、Buff、技能标签层

## 8. 对 Project-XX 的改造建议

建议保留：

- 角色移动
- 角色动作状态
- 瞄准/FireMode 基础
- 跳跃/翻滚/蹲伏/匍匐
- 武器切换接口

建议新增一层“角色战斗域”而不是直接改烂原脚本：

- `CharacterStatsRuntime`
- `CharacterBuffRuntime`
- `CharacterSkillRuntime`
- `CombatModifierResolver`
- `EquipmentModifierResolver`

推荐接法：

1. 让 JUTPS 继续负责“动作执行”。
2. 让 Project-XX 负责“动作结果修正”。
3. 比如移动速度、后坐力、装填速度、耐力消耗、受伤抗性，都不要硬编码到 JUTPS 原字段里，而是通过额外 Runtime Modifier 层算出最终值。

## 9. Project-XX 里最适合的角色定位

这套角色控制器最适合承载：

- 局内玩家角色
- 局内人形 NPC
- 局内敌对武装单位

不建议直接承载：

- 局外 Meta 身份数据
- 商人/任务/账号档案
- 复杂背包仓库数据

最终建议：

- `JUCharacterController` 作为“局内躯体”
- Project-XX 自己的 Profile/Stats/Equipment/Skill/Buff 作为“局内外共享数据脑”
