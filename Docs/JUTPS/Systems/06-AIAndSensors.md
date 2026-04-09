# JUTPS 系统拆解 06：AI、感知与行为动作

## 1. 模块定位

JUTPS 的 AI 不是行为树，也不是 GOAP，而是一套：

- `AI 控制器`
- `感知器`
- `动作对象`

的组合模型。

这套设计足够支撑：

- 巡逻
- 发现目标
- 听声警觉
- 追击
- 攻击
- 失去目标后搜索
- 从危险区域逃离

## 2. 关键类

### `JUCharacterAIBase`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/AI/JUCharacterAIBase.cs`

职责：

- 持有 `JUCharacterController`
- 关闭角色默认玩家输入
- 用 `AIControlData` 描述当前控制意图：
  - `IsRunning`
  - `IsAttackPose`
  - `IsAttacking`
  - `MoveToDirection`
  - `LookToDirection`
- 每帧把 AI 意图转成角色动作

这个类非常关键，因为它证明：

- AI 和玩家共用同一套角色驱动底层
- AI 只是更高层的“控制器”

### `JU_AIActionBase`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/AI/Actions/JUAIActionBase.cs`

职责：

- 作为各类动作的公共基类
- 提供简单导航 / NavMesh 导航兼容
- 管理路径刷新频率
- 管理当前目的地与路径点

这层相当于：

- AI 行为块的基础运动工具箱

## 3. 感知系统

### 视野：`FieldOfView`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/AI/Sensors/JUFieldOfViewSensor.cs`

能力：

- 距离检测
- 角度过滤
- 目标层与目标标签过滤
- 遮挡检测
- 记录最近看见的对象和最后可见位置

### 听觉：`HearSensor`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/AI/Sensors/JUHearSensor.cs`

能力：

- 包内有一个全局 `JU AI Hear Manager`
- 所有 AI 听觉传感器注册到该管理器
- 声音事件通过 `AddSoundSource` 广播
- 管理器按组轮询，避免一次更新太多传感器

这一点很实用，因为它已经有“听声惊动”的基础骨架。

## 4. 具体 AI 控制器

### `JU_AI_PatrolCharacter`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/AI/JU_AI_PatrolCharacter.cs`

状态包括：

- `Patrol`
- `MovingToPossibleTargetPosition`
- `SearchingForLostTarget`
- `Attacking`

它组合了多种 Action：

- `MoveRandomAroundPoint`
- `FollowWaypoint`
- `MoveRandomInsideArea`
- `FollowPoint`
- `SearchLosedTarget`
- `Attack`
- `Escape`
- `DamageDetector`

说明：

- AI 不是巨型 if-else 单文件，而是“控制器 + 多个动作块”组合
- 虽然不是行为树，但已经具备模块化思路

### 其他 AI

包内还有：

- `JU_AI_Zombie`
- `VehicleAI`
- 多个示例动作和感知例子

## 5. 运行时逻辑

典型流程：

1. AI 控制器拿到角色组件。
2. 每帧更新感知器：
   - 看见目标
   - 听见声音
   - 收到伤害来源
3. 根据当前状态选用某个 Action。
4. Action 输出移动目的地、朝向、是否攻击。
5. `JUCharacterAIBase` 把这些意图转换为角色控制。

## 6. 这套 AI 的优点

优点：

- 结构比“所有逻辑堆一个脚本”更清楚
- 感知器是独立概念
- 玩家和 AI 共用角色、武器、攻击逻辑
- 足够快地支撑射击游戏样机

## 7. 这套 AI 的限制

它依然不是面向复杂搜打撤敌人的最终方案：

- 缺少战术小队协同
- 缺少掩体决策与火力压制层
- 缺少搜刮、撤离、巡逻区域切换等长程行为目标
- 缺少威胁评估、装备评估、资源决策
- 缺少商人、任务 NPC、友军、平民等多角色 AI 类型框架

## 8. 对 Project-XX 的建议

可直接复用：

- 基础敌人巡逻
- 视野和听觉感知
- 简单追击与攻击
- 载具 AI 示例

建议扩展：

- `ThreatModel`
- `SquadBlackboard`
- `RaidInterestPoint`
- `ExtractionBehavior`
- `LootInterestBehavior`
- `SuppressionAndCoverBehavior`

推荐策略：

1. 保留 JUTPS 感知和基础动作。
2. 在其上层增加“战术决策层”。
3. 把地图兴趣点、枪声等级、掉落价值、撤离时机、任务目标都转为 AI 可消费的数据。

## 9. 对搜打撤玩法最有用的现成点

最有价值的现成能力是：

- `FieldOfView`
- `HearSensor`
- `Attack`
- `FollowWaypoint`
- `Escape`

这些已经足够做出：

- 会巡逻的武装敌人
- 听见枪声后警觉的敌人
- 看到玩家后交火与追击的敌人

你后续要做的，是把“为什么去某处、为何撤、为何抢资源、何时呼叫增援”这一层加上去。
