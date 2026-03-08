# 局内战斗系统设计

## 1. 设计目标

当前局内系统的目标不是还原完整 AAA 战斗，而是提供一套足够稳定的单机搜打撤战斗底座，满足以下需求：

- 第一人称移动、开火、近战、医疗、搜刮能够同场工作
- AI 能够通过视觉、听觉、嗅觉发现玩家并交战
- 战斗结果能够影响局外带出
- 后续可以继续扩展武器、状态、敌人和关卡内容

---

## 2. 核心运行时对象

### 2.1 玩家控制

- `Assets/Res/Scripts/FPS/PrototypeFpsInput.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`

### 2.2 单位结算

- `Assets/Res/Scripts/FPS/PrototypeUnitDefinition.cs`
- `Assets/Res/Scripts/FPS/PrototypeUnitVitals.cs`
- `Assets/Res/Scripts/FPS/PrototypeUnitHitbox.cs`
- `Assets/Res/Scripts/FPS/PrototypeStatusEffectController.cs`

### 2.3 表现与反馈

- `Assets/Res/Scripts/FPS/PrototypeCombatTextController.cs`
- `Assets/Res/Scripts/FPS/PrototypeTargetHealthBar.cs`

### 2.4 战局与交互

- `Assets/Res/Scripts/Raid/RaidGameMode.cs`
- `Assets/Res/Scripts/Interaction/PlayerInteractor.cs`
- `Assets/Res/Scripts/Loot/LootContainerWindowController.cs`
- `Assets/Res/Scripts/Loot/PlayerInventoryWindowController.cs`

### 2.5 AI

- `Assets/Res/Scripts/AI/PrototypeBotController.cs`
- `Assets/Res/Scripts/AI/PrototypeEnemyRuntimeFactory.cs`
- `Assets/Res/Scripts/AI/PrototypeEnemySpawnProfile.cs`
- `Assets/Res/Scripts/AI/PrototypeEncounterDirector.cs`

---

## 3. 玩家操控设计

## 3.1 输入层

`PrototypeFpsInput` 负责把 Input System 和 fallback 输入统一成稳定接口。

当前重要输入包括：

- 移动、视角、射击、交互、背包
- 武器 1 / 2 / 3 切换
- 换弹、切换开火模式
- 快速治疗、止血、夹板、止痛
- `C` 切换站立 / 蹲下
- `Shift` 奔跑
- `LCtrl + 鼠标滚轮` 调整移动速度比例

## 3.2 角色移动

`PrototypeFpsController` 当前采用“接近竞技 FPS 手感 + Tarkov 风格姿态/速度管理”的折中方案。

当前特征：

- 地面摩擦与加速度存在，不是立刻停下
- 起跳保留速度，支持连跳
- 空中侧移需要侧键 + 鼠标同向移动才有效
- 落地默认有掉速，但连续跳跃可规避

## 3.3 姿态与速度比例

当前不是传统“走路 / 奔跑 / 蹲下”三段式，而是：

- `C` 切换蹲下
- 站立和蹲下都受 `movementSpeedRatio` 控制
- `LCtrl + 鼠标滚轮` 在 `10% - 100%` 之间调节速度比例
- `Shift` 奔跑时会自动：
  - 速度比例切到 100%
  - 姿态切回站立

这套设计更接近 Tarkov 的“精细移动速度控制”。

## 3.4 体力

体力逻辑位于 `PrototypeUnitVitals`，由 `PrototypeFpsController` 驱动消耗。

当前消耗来源：

- 奔跑
- 跳跃
- 近战攻击

当前恢复规则：

- 任意体力消耗后会进入恢复延迟
- 如果体力耗尽，会进入更长的虚脱恢复延迟
- 低于动作阈值时无法开始新的耗体动作
- 但已开始的奔跑可以继续消耗到 0

---

## 4. 武器与攻击

## 4.1 武器槽位

玩家当前有三个槽位：

- Primary
- Secondary
- Melee

运行时用 `WeaponRuntime` 保存：

- 当前定义
- 当前弹匣剩余子弹
- 开火模式
- Burst 待发子弹
- 冷却与换弹状态

## 4.2 枪械

当前已接入：

- 步枪
- 手枪
- 近战武器

枪械支持：

- Semi / Burst / Auto
- 独立射速
- 换弹时间
- 有效射程
- 散布
- 使用不同弹药定义

## 4.3 近战

近战武器与枪械共用武器槽逻辑，但命中路径不同。

当前特征：

- 有攻击冷却
- 有攻击距离和半径
- 消耗体力
- 可以直接触发伤害与冲击

---

## 5. 伤害、部位、护甲与状态效果

## 5.1 部位系统

当前部位系统使用 `PrototypeUnitDefinition` 做数据驱动。

默认人型定义包含：

- 头部
- 躯干
- 腿部

每个部位可配置：

- 最大生命
- 溢出倍率
- 是否计入总生命
- 是否接收溢出
- 零血后的致死规则

## 5.2 护甲系统

护甲在 `PrototypeUnitVitals` 中结算，支持：

- 覆盖指定部位
- 护甲等级
- 耐久
- 格挡后耐久损耗
- 被穿透后耐久损耗
- 护甲对流血 / 骨折的防护

当前护甲是“定义级装备”，还没有完整的实例耐久跨局保存。

## 5.3 状态效果

出血、骨折、止痛等效果已经从“部位状态”改成“单位级状态”，由 `PrototypeStatusEffectController` 统一管理。

这样做的好处：

- 后续加中毒、灼烧、减速、增伤时不用再改部位逻辑
- 方便把效果系统往 RPG 化方向扩展

当前内建效果：

- Light Bleed
- Heavy Bleed
- Fracture
- Painkiller

## 5.4 伤害来源记录

`PrototypeUnitVitals` 和 `PrototypeStatusEffectController` 会记录：

- 最后伤害来源单位
- 最后伤害来源快照
- Debuff 的施加者

因此玩家死亡时，结算界面可以显示：

- 直接击杀来源
- 因 Debuff 持续伤害导致死亡时的来源

---

## 6. 战斗反馈

当前局内战斗反馈包括：

- 受击飘字
- 护甲命中飘字
- 护甲损坏提示
- Target 头顶总血条
- HUD 生命 / 体力 / 护甲 / 状态显示
- 命中标记
- 结算界面死亡来源

其中飘字当前区分：

- 护甲受击：灰色
- 护甲损坏：提示文案
- 生命受击：红色

---

## 7. 局内 Loot 与战斗后的搜刮

## 7.1 地面拾取

`GroundLootItem` 负责地面物品交互。

## 7.2 容器搜刮

`LootContainer` 和 `LootContainerWindowController` 负责箱子、柜子等容器搜刮。

支持：

- 首次打开随机刷物
- 从 Loot 表滚动生成
- 可转移到玩家主背包

## 7.3 尸体搜刮

敌人死亡后，会在尸体上生成统一的可搜刮实体。

当前尸体窗口可以查看和拿取：

- 武器
- 护甲
- 敌人携带的额外物品

这里不再用“武器单独掉地”的老方案，而是统一走尸体窗口。

---

## 8. AI 战斗设计

## 8.1 当前敌人类型

当前已经固定为 4 类 archetype：

- 普通丧尸
- 警察丧尸
- 军人丧尸
- 丧尸犬

## 8.2 感知方式

AI 当前支持：

- 视觉
- 听觉
- 嗅觉（仅狗类 archetype）

听觉来源包括：

- 玩家移动噪声
- 跳跃
- 落地
- 近战
- 枪声

并且玩家当前速度比例与姿态会改变噪声半径。

## 8.3 攻击方式

近战 AI：

- 追近后使用近战冷却攻击

远程 AI：

- 不是持续完美锁定玩家
- 会锁定目标身体区域
- 在区域内随机取命中点
- 支持单发冷却或短点射后冷却

因此当前远程 AI 已不是“每帧锁中心点”的高压命中逻辑。

---

## 9. 战局与结算

`RaidGameMode` 负责：

- 开局运行
- 撤离成功
- 玩家死亡失败
- 超时失败

`PrototypeRaidProfileFlow` 负责：

- 进局时把局外配置应用到玩家
- 结算时把战局结果写回 Profile
- 非运行状态显示 `Return To Menu`

---

## 10. 当前限制

当前局内战斗系统仍有这些限制：

- 玩家与 AI 武器实例状态还不是真正实例化资产
- 护甲耐久没有跨局持久化
- AI 仍集中在 `PrototypeBotController`
- HUD 和搜刮 UI 仍是 IMGUI
- 更复杂的掩体战术、队友协同、门战术还没成型

---

## 11. 后续扩展建议

如果继续深化局内系统，优先建议：

1. 把武器、护甲、容器都做成实例级持久化
2. 给局内 UI 做真正的多容器拖拽转移
3. 拆分 `PrototypeFpsController`
4. 拆分 `PrototypeBotController`
5. 把原型 blockout 场景和正式内容场景分离
