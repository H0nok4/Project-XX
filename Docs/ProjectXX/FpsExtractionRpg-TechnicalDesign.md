# Project-XX 粗略技术设计文档

## 1. 目标

在 JUTPS 的角色控制、战斗、交互与基础 AI 底座之上，构建一套适合“单机 PVE 第一人称搜打撤 + 恐怖后末日 + RPG 成长 + 基地经营”的技术架构。

这套架构要满足：

- 单机 PVE 优先
- 正式玩法严格使用第一人称视角
- 支持枪械与近战双主线战斗
- 支持恐怖、怪异、异常、偏科幻或偏魔幻内容
- 支持塔科夫式容器格子与装备丢失逻辑
- 支持基地场景、设施建造、商人入住与局外交互
- 支持饱食度、饮水度、疲劳值的持续管理
- 支持精英怪、Boss、词缀、Buff、异常机制
- 支持局内外完整闭环
- 与 Project-XX 现有 runtime UI 规范兼容

## 2. 基本原则

### 原则 1：JUTPS 负责“动作与执行”，Project-XX 负责“规则与数据”

复用 JUTPS：

- 第一人称角色移动底座
- 基础相机与输入承接
- 枪械开火
- 基础近战
- 基础交互
- 载具
- AI 基础感知与控制

Project-XX 自建：

- 格子背包与容器
- 装备槽位规则
- 死亡丢失与保留逻辑
- 属性、技能、Buff、异常系统
- 生存值与恢复系统
- 基地场景、设施、商人入住、制造
- 精英怪/Boss 词缀与机制系统
- 商人、任务、成长、局外仓库
- 正式 UI
- 最终存档

### 原则 2：正式产品只交付第一人称玩法

- Raid 与 BaseHub 都以第一人称移动、观察、战斗、搜刮和交互
- 玩家可持枪，也可使用近战武器
- 第三人称如有保留，只能作为调试或非正式镜头，不得成为主玩法分支
- 输入、相机、HUD、交互射线、武器展示都必须按 FPS 需求设计

### 原则 3：Raid、BaseHub、Meta Profile 严格分层

- `Raid Runtime` 负责战斗、搜刮、死亡、撤离、掉落、怪物、场景事件
- `BaseHub Runtime` 负责基地第一人称移动、设施使用、商人交互、建造和局外恢复
- `Meta Profile` 负责持久化角色档案、仓库、成长、任务、商人状态和基地状态

### 原则 4：数据驱动优先

- 定义层尽量用 `ScriptableObject`
- 运行时状态尽量用纯 C# 对象
- MonoBehaviour 负责表现、桥接与场景生命周期

### 原则 5：正式 UI 统一走 Project-XX 规范

所有新 runtime UI 必须：

- 放在 `Assets/Resources/UI/...`
- 使用 `ViewBase` / `WindowBase`
- 使用 `*Template`
- 挂载到 `PrototypeRuntimeUiManager`

不要继续扩展 JUTPS 自带 UI 作为正式方案。

## 3. 高层架构

建议采用六层结构。

## 3.1 Foundation 层

来源：

- Unity
- JUTPS

职责：

- 第一人称玩家控制
- 武器和近战动作
- 相机
- 基础交互
- 载具
- AI 基础感知与控制
- 场景中的物理、动画和表现

## 3.2 Raid Domain 层

职责：

- 局内玩家运行态
- 局内容器与搜刮逻辑
- 局内战利品生成
- 局内敌人、精英、Boss 运行态
- 局内异常事件与场景机制
- 局内任务目标推进
- 局内生存值衰减
- 局内死亡与撤离结算

## 3.3 BaseHub Domain 层

职责：

- 基地场景中的第一人称玩家运行态
- 设施建造与升级
- 设施交互
- 商人驻留与可见性控制
- 休息、进食、饮水、制造等局外行为
- 下一局 Buff 预置

## 3.4 Meta Domain 层

职责：

- 玩家 Profile
- 仓库
- 配装
- 商人关系与解锁
- 任务
- 技能与成长
- 长期解锁
- 基地建设状态
- 图鉴和研究

## 3.5 Presentation 层

职责：

- Raid HUD
- 生存值 HUD
- 背包与容器界面
- 装备与角色页
- 商人界面
- 任务日志
- 基地建造界面
- 设施交互窗口
- 结算界面

## 3.6 Persistence 层

职责：

- 定义资产加载
- Profile 存档
- 基地状态存档
- Raid 快照
- 设置存档
- 版本迁移

## 4. 定义层设计

## 4.1 物品与装备定义

建议建立：

- `ItemDefinition`
- `WeaponDefinition`
- `MeleeWeaponDefinition`
- `TacticalDeviceDefinition`
- `ArmorDefinition`
- `ChestRigDefinition`
- `BackpackDefinition`
- `RelicDefinition`
- `ConsumableDefinition`
- `AmmoDefinition`

关键字段建议：

- `Id`
- `DisplayName`
- `Description`
- `Icon`
- `WorldPrefab`
- `InventorySize`
  - 例如 `1x1`、`1x2`、`2x2`
- `Weight`
- `Rarity`
- `MaxDurability`
- `AffixPool`
- `Tags`

## 4.2 装备槽位定义

建议建立：

- `EquipmentSlotDefinition`

固定槽位建议：

- `PrimaryWeapon`
- `SecondaryWeapon`
- `Melee`
- `Head`
- `ChestArmor`
- `ChestRig`
- `Backpack`
- `TacticalDevice`
- `Relic`

每个槽位建议定义：

- 可接受的物品类型
- 是否死亡丢失
- 是否可为空
- 是否影响容器系统
- 是否提供防护

## 4.3 容器定义

建议建立：

- `ContainerDefinition`

关键字段：

- `Width`
- `Height`
- `CanRotateItems`
- `AllowedItemTags`
- `ContainerType`

容器类型示例：

- 背包
- 胸挂
- 防护胸挂
- 战利品箱
- 尸体容器
- 特殊任务容器

## 4.4 基地与设施定义

建议建立：

- `FacilityDefinition`
- `FacilityLevelDefinition`
- `FacilityRequirementDefinition`
- `FacilityBuildRecipeDefinition`
- `FacilityEffectDefinition`
- `MerchantMoveInRequirementDefinition`

关键字段建议：

- `FacilityId`
- `DisplayName`
- `Category`
- `RequiredFacilities`
- `RequiredItems`
- `RequiredQuests`
- `RequiredPlayerLevel`
- `RequiredMerchantState`
- `InteractableType`
- `UnlockEffects`
- `LevelEffects`

## 4.5 商人、任务与制造定义

建议建立：

- `MerchantDefinition`
- `MerchantInventoryDefinition`
- `MerchantTierDefinition`
- `QuestDefinition`
- `QuestObjectiveDefinition`
- `RecipeDefinition`

关键字段建议：

- 商人初次出现条件
- 商人入住前置设施
- 商人声望条件
- 制造配方材料
- 制造配方耗时
- 产物列表
- 任务对设施或商人的解锁效果

## 4.6 成长、异常与生存定义

建议建立：

- `AttributeDefinition`
- `SkillDefinition`
- `BuffDefinition`
- `AffixDefinition`
- `StatusEffectDefinition`
- `MutationDefinition`
- `SurvivalStatDefinition`
- `SurvivalThresholdDefinition`
- `RestBuffDefinition`

作用：

- 统一角色成长
- 统一装备词缀
- 统一怪物精英修正
- 统一异常/污染/诅咒表现
- 统一饱食度、饮水度、疲劳值规则

## 4.7 敌人与遭遇定义

建议建立：

- `EnemyArchetypeDefinition`
- `EliteModifierDefinition`
- `BossPhaseDefinition`
- `EncounterDefinition`
- `SpawnGroupDefinition`
- `RaidEventDefinition`

关键字段：

- 基础生命/防御/速度
- 近战或远程类型
- 技能列表
- 精英词缀池
- 掉落表
- 行为参数
- 感知范围

## 4.8 地图与掉落定义

建议建立：

- `MapDefinition`
- `LootTableDefinition`
- `LootAreaDefinition`
- `ExtractionPointDefinition`
- `MapProgressionDefinition`

## 5. 运行时状态设计

## 5.1 Profile 层运行时

建议建立：

- `PlayerProfileRuntime`
- `MerchantRuntimeState`
- `QuestRuntimeState`
- `SkillTreeRuntime`
- `ResearchRuntimeState`
- `BaseHubRuntimeState`
- `FacilityRuntimeState`
- `SurvivalRuntimeState`

### `PlayerProfileRuntime`

至少包含：

- 等级
- 属性
- 技能
- 当前资金
- 仓库
- 商人状态
- 任务状态
- 已解锁地图/区域
- 已建造设施
- 已知配方
- 当前饱食度、饮水度、疲劳值
- 下一局预置 Buff

### `BaseHubRuntimeState`

职责：

- 记录当前基地可用设施
- 记录基地交互点状态
- 记录商人是否已入住
- 记录可见建造位和未解锁区域
- 输出当前可进入的局外功能

### `FacilityRuntimeState`

职责：

- 记录设施等级
- 记录建造完成状态
- 记录可交互状态
- 记录冷却、制作队列或恢复队列
- 记录设施提供的被动效果

### `SurvivalRuntimeState`

建议字段：

- `Satiety`
- `Hydration`
- `Fatigue`
- `ActiveThresholdIds`
- `LastUpdatedUtc`
- `RaidDecayPaused`
- `PendingNextRaidBuffId`

## 5.2 装备与库存运行时

建议建立：

- `ItemInstanceRuntime`
- `EquipmentRuntime`
- `InventoryGridRuntime`
- `ContainerRuntime`
- `LoadoutRuntime`

### `ItemInstanceRuntime`

建议字段：

- `DefinitionId`
- `UniqueId`
- `CurrentDurability`
- `Rarity`
- `Affixes`
- `StackCount`
- `Rotation`
- `BoundState`
- `CustomData`

### `InventoryGridRuntime`

职责：

- 维护二维格子占用
- 校验物品能否放入
- 处理旋转
- 处理移入/移出/交换

### `EquipmentRuntime`

职责：

- 管理角色槽位物品
- 处理死亡丢失与保留逻辑
- 输出当前负重、防护、可用容器等结果

## 5.3 Raid 层运行时

建议建立：

- `RaidSessionRuntime`
- `RaidPlayerRuntime`
- `RaidEnemyRuntime`
- `RaidBossRuntime`
- `RaidLootRuntime`
- `RaidExtractionRuntime`
- `RaidThreatRuntime`

### `RaidPlayerRuntime`

职责：

- 持有玩家局内属性快照
- 持有当前生命、异常、耐久、负重状态
- 持有本局实时生存值状态
- 记录局内拾取、任务推进、击杀与撤离状态

### `RaidEnemyRuntime`

职责：

- 记录敌人实例状态
- 挂接精英词缀和 Buff
- 驱动战斗结果修正

### `RaidBossRuntime`

职责：

- 记录阶段
- 记录机制状态
- 记录召唤、护盾、弱点等专属逻辑

## 6. 服务层设计

建议建立以下服务。

## 6.1 Profile 与存档

- `ProfileService`
- `SaveGameService`
- `SettingsService`

## 6.2 BaseHub 与设施

- `BaseHubService`
- `FacilityService`
- `FacilityConstructionService`
- `BaseInteractionService`
- `MerchantMoveInService`

## 6.3 生存与恢复

- `SurvivalService`
- `RestService`
- `ConsumptionService`
- `RecipeCraftingService`
- `RecoveryResolver`

## 6.4 物品与容器

- `ItemFactoryService`
- `InventoryService`
- `ContainerTransferService`
- `EquipmentService`
- `LootGenerationService`

## 6.5 成长与修正

- `AttributeService`
- `SkillService`
- `BuffService`
- `ModifierResolver`

## 6.6 Raid 流程

- `RaidSessionService`
- `ExtractionService`
- `RaidResultService`
- `DeathLossService`

## 6.7 商人和任务

- `MerchantService`
- `QuestService`
- `ProgressionService`

## 6.8 敌人与遭遇

- `EnemySpawnService`
- `EliteModifierService`
- `BossDirectorService`
- `RaidEventService`

## 7. 与 JUTPS 的桥接设计

## 7.1 第一人称角色与视角桥接

建议新增：

- `ProjectXXCharacterFacade`
- `ProjectXXFirstPersonViewBridge`
- `ProjectXXInputContextBridge`
- `ProjectXXBaseHubPlayerBridge`

职责：

- 把 JUTPS 角色控制锁定在正式第一人称玩法
- 统一 raid 与 BaseHub 的第一人称输入模式
- 切换战斗态与基地态允许的动作集合
- 同步相机、武器展示、交互射线和 HUD 锚点

策略建议：

- 角色底层继续复用 JUTPS 的移动和受击骨架
- 第一人称手臂、武器、镜头震动、视野限制由 Project-XX 上层控制
- BaseHub 中关闭不需要的战斗动作，但保留第一人称移动与交互

## 7.2 角色数值桥接

建议新增：

- `ProjectXXCharacterStatBridge`
- `ProjectXXCharacterBuffBridge`
- `ProjectXXEquipmentBridge`
- `ProjectXXDamageBridge`

职责：

- 把 Project-XX 的运行时数值同步到 `JUCharacterController`
- 把装备、Buff、异常、生存值对移动、近战、射击、交互的修正作用到 JUTPS

例子：

- 最终移动速度 = JUTPS 基础速度 x 负重修正 x Buff 修正 x 生存值修正 x 异常修正
- 最终近战伤害 = 基础近战伤害 x 力量修正 x 词缀修正 x 异常修正
- 最终稳定性 = 武器基础值 x 技能修正 x 恐惧修正 x 疲劳修正

## 7.3 武器桥接

建议新增：

- `ProjectXXWeaponBridge`
- `ProjectXXAmmoResolver`
- `ProjectXXWeaponDurabilityBridge`

策略：

- JUTPS `Weapon` 继续负责开火、射线、抛壳、枪口火焰、基础后坐
- Project-XX 负责真正的武器实例数据

运行时由桥接层把以下信息同步给 JUTPS：

- 当前弹药
- 当前耐久
- 稀有度修正
- 词缀效果
- 异常/技能修正

## 7.4 近战桥接

因为敌人大多为近战，建议单独加强这条链路：

- `ProjectXXMeleeBridge`
- `MeleeHitResolver`
- `MeleeStaggerService`

目标：

- 近战不是“保底动作”，而是重要战斗维度
- 支持第一人称近战打击反馈
- 支持敌人高压近战
- 支持玩家近战槽位作为死亡保留的稳定输出工具

## 7.5 容器与背包桥接

JUTPS `JUInventory` 不应承担最终仓库逻辑，只建议保留为：

- 当前角色手持切换
- 局内快捷使用代理

正式格子系统建议完全由 Project-XX 自己实现：

- `InventoryGridRuntime`
- `ContainerRuntime`
- `EquipmentRuntime`
- `RaidInventoryBridge`

桥接原则：

- Project-XX 决定物品是否存在于容器中
- JUTPS 只负责场景中实际装备和使用的那部分物体

## 7.6 交互桥接

建议新增：

- `ProjectXXInteractableAdapter`
- `InteractionRequirementEvaluator`
- `InteractionExecutionService`

继续复用 JUTPS：

- 查找最近交互物
- 基础交互输入

Project-XX 负责：

- 是否有钥匙/任务条件
- 是否有足够容器空间
- 是否允许撤离
- 是否允许开启 Boss 事件
- 是否允许局内商店交易
- 是否允许建造、使用或升级基地设施

## 7.7 AI 扩展

保留 JUTPS：

- `FieldOfView`
- `HearSensor`
- `Attack`
- `FollowWaypoint`

新增 Project-XX 上层：

- `EnemyIntentRuntime`
- `EliteModifierRuntime`
- `BossMechanicController`
- `RaidThreatEvaluator`
- `AmbushDirector`

重点不是让 AI 变成战术军队，而是让它更适合恐怖 PVE：

- 近战群体压迫
- 突袭与包围
- 异常技能
- 精英怪强化逻辑
- Boss 阶段机制

## 8. 装备与丢失规则技术设计

## 8.1 装备槽位规则

按策划要求，槽位规则如下。

### 死亡丢失

- 主武器
- 副武器
- 头部
- 胸部
- 胸挂
- 背包

### 死亡不丢失

- 近战槽位
- 战术设备
- 特殊遗物/护符

建议实现：

- 在 `EquipmentSlotDefinition` 中直接配置 `DropOnDeath`
- `DeathLossService` 根据槽位配置结算掉落和保留

## 8.2 胸部与胸挂规则

需要特殊处理的不是 UI，而是装备合法性。

规则建议：

- `ChestArmor` 可装备护甲
- `ChestRig` 可装备两类物品：
  - 普通胸挂：无防护，仅提供容器
  - 防护胸挂：有防护并提供容器

装备校验逻辑：

- 若 `ChestArmor` 已装备护甲，则 `ChestRig` 只能装备普通胸挂
- 若 `ChestArmor` 为空，则 `ChestRig` 可装备普通胸挂或防护胸挂

## 8.3 护甲结算

采用“整体护甲值先吃伤害”的设计，不走复杂部位穿透。

建议建立：

- `ArmorRuntime`
- `ArmorDamageResolver`

伤害流程：

1. 命中胸部/头部时先看对应防护是否存在。
2. 若有护甲值，则优先扣护甲值。
3. 护甲值归零后，后续伤害再进入生命值。

## 9. 格子背包与容器系统设计

## 9.1 核心结构

建议建立：

- `GridSize`
- `GridCoord`
- `GridOccupancyMap`
- `InventoryGridRuntime`
- `ContainerRuntime`

### `InventoryGridRuntime`

职责：

- 放置校验
- 拖拽交换
- 旋转校验
- 占地标记
- 清空与整理

## 9.2 物品尺寸

每个物品定义必须带尺寸：

- `1x1`
- `1x2`
- `2x1`
- `2x2`
- `2x3`

必要时支持旋转：

- `CanRotate`

## 9.3 容器种类

建议容器分为：

- 玩家主背包
- 胸挂
- 防护胸挂
- 尸体容器
- 地图箱体
- 商店/交付临时容器
- 特殊任务容器

## 9.4 技术约束

容器系统必须优先完成这些能力：

- 判定能否放入
- 从一个容器拖到另一个容器
- 死亡时从角色容器生成战利品容器
- 撤离时把局内新增物品写回局外仓库

## 10. BaseHub 与设施系统技术设计

## 10.1 BaseHub 场景职责

`BaseHub` 不是菜单，而是正式场景。

场景职责建议：

- 生成基地态第一人称角色
- 加载已建造设施和当前可交互点
- 控制商人驻留可见性
- 提供仓库、配装、任务、商人、建造、休息、制作等入口
- 接收 raid 结算后的状态回写

## 10.2 设施数据与运行时

建议采用“定义 + 运行时状态”的结构：

- `FacilityDefinition` 负责设施种类和功能
- `FacilityLevelDefinition` 负责各等级成本与效果
- `FacilityRuntimeState` 负责当前等级、可用状态、冷却、队列

设施效果建议分为：

- 被动效果
  - 例如降低疲劳累积、提高制作效率
- 主动交互
  - 例如休息、烹饪、提交材料
- 解锁效果
  - 例如商人入住、开放新 UI、开放新配方

## 10.3 建造与升级流程

建议建立：

- `FacilityConstructionService`
- `ConstructionRequirementEvaluator`

标准流程：

1. 玩家在基地交互点选择某个设施位。
2. 系统读取该建造位允许的设施类型。
3. `ConstructionRequirementEvaluator` 校验：
   - 材料是否足够
   - 前置任务是否完成
   - 前置设施是否存在
   - 玩家等级是否满足
4. 校验通过后扣除材料并更新 `FacilityRuntimeState`。
5. 刷新基地场景中的设施表现与可用功能。

## 10.4 商人入住条件

商人解锁不只依赖任务，还依赖基地设施。

建议实现：

- `MerchantDefinition` 中配置 `MoveInRequirements`
- `MerchantMoveInService` 在基地回写时统一评估

入住条件可以组合：

- 某设施存在
- 某设施达到指定等级
- 完成某条任务
- 提交某类道具
- 玩家达到指定等级或声望

## 10.5 可用设施示例

### 休息间

建议建立：

- `RestService`
- `RestBuffDefinition`

核心规则：

- 进入休息流程时，消耗时间和可能的资源
- 清除连续 raid 带来的疲劳类 Debuff
- 恢复部分或全部疲劳值
- 给 `SurvivalRuntimeState.PendingNextRaidBuffId` 写入下一局 Buff

### 厨房

建议建立：

- `RecipeCraftingService`
- `RecipeRuntimeQueue`

核心规则：

- 配方消耗材料，产出食品或饮品
- 成品通过 `ConsumptionService` 恢复饱食度或饮水度
- 高级厨房可开放更复杂配方和附加 Buff

## 11. 生存系统技术设计

## 11.1 三项核心数值

建议统一采用百分制：

- `Satiety`
- `Hydration`
- `Fatigue`

初版建议范围：

- `0 - 100`

## 11.2 衰减规则

建议建立：

- `SurvivalTickContext`
- `SurvivalDecayRule`
- `SurvivalService`

规则：

- 在 raid 中按时间持续衰减
- 在 BaseHub 中不自动衰减
- 在 BaseHub 中也不自动恢复
- 只能通过食物、饮水、休息等明确行为恢复
- 不同地图、异常区域、天气、Boss 机制可提供额外衰减修正

## 11.3 阈值与 Debuff 映射

建议建立：

- `SurvivalThresholdDefinition`
- `SurvivalPenaltyResolver`

初版阈值建议：

- `> 75`
  - 无惩罚
- `50 - 75`
  - 轻度惩罚
- `25 - 50`
  - 中度惩罚
- `1 - 25`
  - 重度惩罚
- `0`
  - 极限惩罚

惩罚映射建议：

- 饱食度过低：
  - 最大体力下降
  - 负重效率下降
  - 近战输出和恢复效率下降
- 饮水度过低：
  - 视野晃动增强
  - 耐力回复下降
  - 瞄准稳定性下降
- 疲劳值过低：
  - 交互速度下降
  - 移动和转向手感变差
  - 恐惧与异常抗性下降

## 11.4 数值归零时的处理

当任一生存值为 `0` 时：

- 应用该维度的最大 Debuff
- 在 raid 中允许触发持续生命流失
- 可通过 `SurvivalThresholdDefinition` 配置伤害频率和数值

建议初版默认规则：

- 三项生存值任何一项归零，均可在 raid 中造成周期性掉血
- 同时多个维度归零时，伤害或额外惩罚可叠加

## 11.5 恢复与下一局 Buff

恢复来源只允许来自：

- 直接食用食物
- 直接饮用饮品
- 使用基地设施休息
- 特殊药品或异常效果

休息间的实现重点：

- 清理疲劳型 Debuff
- 回补疲劳值
- 允许写入下一局临时 Buff

厨房的实现重点：

- 产出恢复饱食度和饮水度的可消耗品
- 允许某些配方提供下一局短期加成

## 12. 敌人、精英与 Boss 技术设计

## 12.1 敌人模型

建议用“原型 + 运行时修正”的方式：

- `EnemyArchetypeDefinition`
- `RaidEnemyRuntime`

基础类型：

- 普通近战怪
- 远程怪
- 精英怪
- Boss

## 12.2 精英词缀系统

建议建立：

- `EliteModifierDefinition`
- `EliteModifierRuntime`
- `EliteModifierService`

精英词缀可以作用于：

- 最大生命
- 移动速度
- 攻击方式
- Buff 附着
- 护盾
- 召唤机制
- 死亡效果

## 12.3 Boss 机制系统

建议建立：

- `BossDefinition`
- `BossPhaseDefinition`
- `BossMechanicController`

阶段驱动内容：

- 技能切换
- 召唤小怪
- 场景机关激活
- 特殊护盾
- 地图污染增强
- 撤离限制变化

## 13. UI 技术方案

## 13.1 正式 UI 范围

需要正式落地的界面包括：

- Raid HUD
- 生存值显示
- 格子背包/容器窗口
- 装备页
- 商人窗口
- 任务日志
- 基地设施窗口
- 建造窗口
- 配方制作窗口
- 进图准备页
- 结算页

## 13.2 UI 实现规则

全部新 UI 走：

- `Assets/Resources/UI/...`
- `ViewBase`
- `WindowBase`
- `*Template`
- `PrototypeRuntimeUiManager`

## 13.3 推荐控制器

建议建立：

- `RaidHudView`
- `SurvivalHudView`
- `InventoryWindowController`
- `EquipmentWindowController`
- `MerchantWindowController`
- `QuestJournalWindowController`
- `BaseFacilityWindowController`
- `RecipeCraftWindowController`
- `LoadoutWindowController`
- `RaidResultWindowController`

## 14. 存档设计

## 14.1 存档拆分

不建议把最终系统压到 JUTPS `JUSaveLoad` 上。

建议拆成：

- `ProfileSave.json`
  - 等级、属性、技能、仓库、商人、任务、地图解锁、生存值
- `BaseHubSave.json`
  - 设施建造、设施等级、商人入住、配方、基地交互状态
- `RaidSave.json`
  - 当前 raid 快照或调试中断恢复
- `Settings.json`
  - 画面、音量、输入、UI 设置

## 14.2 死亡与撤离写档

建议流程：

- 进入 raid 前冻结一份配装快照
- Raid 中使用 `RaidSessionRuntime` 追踪增量变化
- 死亡时由 `DeathLossService` 结算保留和丢失
- 撤离成功时由 `RaidResultService` 将增量并入 Profile
- 返回基地时同步刷新 `BaseHubRuntimeState`

## 15. 场景与流程建议

建议首期场景流：

1. `Bootstrap`
2. `MainMenu`
3. `BaseHub`
4. `Raid_<MapId>`
5. `RaidResult`

### Bootstrap

职责：

- 初始化服务
- 加载定义资产
- 加载 Profile 与 BaseHub 状态

### BaseHub

职责：

- 生成基地态第一人称玩家
- 仓库
- 配装
- 商人
- 任务
- 建造
- 休息和制作

### Raid Scene

职责：

- 生成玩家和装备映射
- 生成容器、敌人、Boss、事件、撤离点和任务目标
- 运行 JUTPS 战斗层
- 运行 Project-XX 规则层

## 16. 开发顺序建议

### M0：底座验证

- 稳定 JUTPS 接入
- 跑通第一人称视角、战斗、近战、敌人、撤离

### M1：局内闭环

- 格子背包
- 容器搜刮
- 死亡丢失结算
- 基础敌人、精英、Boss
- 基础掉落

### M2：局外基地闭环

- BaseHub 场景
- 仓库
- 商人
- 任务
- 设施建造

### M3：成长与持续状态

- 属性
- 技能
- Buff / Debuff
- 饱食度、饮水度、疲劳值
- 休息与制作

## 17. 当前主要技术风险

### 风险 1：JUTPS 原库存系统过于简化

结论：

- 不能承担最终格子仓库与容器方案

### 风险 2：JUTPS UI 路线与项目正式 UI 路线不一致

结论：

- 只能参考，不能继续扩建

### 风险 3：JUTPS 原始体验偏 TPS 根系，第一人称交付需要额外桥接

结论：

- 必须尽早锁定第一人称相机、武器展示、交互射线和动画策略

### 风险 4：基地与 Raid 共用角色状态，容易出现状态同步错误

结论：

- 必须明确 `PlayerProfileRuntime`、`BaseHubRuntimeState` 和 `RaidPlayerRuntime` 的数据边界

### 风险 5：生存值和 Buff 叠加后容易出现数值膨胀

结论：

- 必须通过 `ModifierResolver` 和统一阈值定义做集中结算

## 18. 最终建议

对这个项目来说，最合理的技术路线不是“完全重写”，也不是“完全继承 JUTPS”，而是：

- 让 `JUTPS` 负责第一人称角色、武器、移动、基础战斗执行
- 让 `Project-XX` 负责局内外规则、容器、成长、基地、商人、生存值和正式界面

一句话概括：

- `JUTPS = 身体、动作、武器、基础战斗`
- `Project-XX = 容器、规则、成长、基地、商人、任务、生存与产品化结构`

## 19. 推荐代码目录落位

建议以 `Assets/Res/Scripts/ProjectXX/` 为项目主代码根目录。

建议目录如下：

- `Assets/Res/Scripts/ProjectXX/Bootstrap`
- `Assets/Res/Scripts/ProjectXX/Foundation`
- `Assets/Res/Scripts/ProjectXX/Domain/Raid`
- `Assets/Res/Scripts/ProjectXX/Domain/Base`
- `Assets/Res/Scripts/ProjectXX/Domain/Meta`
- `Assets/Res/Scripts/ProjectXX/Domain/Common`
- `Assets/Res/Scripts/ProjectXX/Services`
- `Assets/Res/Scripts/ProjectXX/Infrastructure/Save`
- `Assets/Res/Scripts/ProjectXX/Infrastructure/Definitions`
- `Assets/Res/Scripts/ProjectXX/Bridges/JUTPS`
- `Assets/Res/Scripts/ProjectXX/Presentation/Hud`
- `Assets/Res/Scripts/ProjectXX/Presentation/Inventory`
- `Assets/Res/Scripts/ProjectXX/Presentation/Character`
- `Assets/Res/Scripts/ProjectXX/Presentation/Merchant`
- `Assets/Res/Scripts/ProjectXX/Presentation/Quest`
- `Assets/Res/Scripts/ProjectXX/Presentation/BaseHub`
- `Assets/Res/Scripts/ProjectXX/Presentation/RaidResult`

推荐资源目录如下：

- `Assets/Resources/UI/Hud/`
- `Assets/Resources/UI/Inventory/`
- `Assets/Resources/UI/Character/`
- `Assets/Resources/UI/Merchant/`
- `Assets/Resources/UI/Quest/`
- `Assets/Resources/UI/BaseHub/`
- `Assets/Resources/UI/Raid/`

推荐定义资产目录如下：

- `Assets/Res/Data/Definitions/Items/`
- `Assets/Res/Data/Definitions/Equipment/`
- `Assets/Res/Data/Definitions/Buffs/`
- `Assets/Res/Data/Definitions/Skills/`
- `Assets/Res/Data/Definitions/Survival/`
- `Assets/Res/Data/Definitions/Facilities/`
- `Assets/Res/Data/Definitions/Recipes/`
- `Assets/Res/Data/Definitions/Enemies/`
- `Assets/Res/Data/Definitions/Bosses/`
- `Assets/Res/Data/Definitions/Maps/`
- `Assets/Res/Data/Definitions/Loot/`
- `Assets/Res/Data/Definitions/Merchants/`
- `Assets/Res/Data/Definitions/Quests/`

## 20. 关键运行流程

这一节把最重要的游戏流程从“概念设计”落成“程序实现主线”。

## 20.1 返回基地流程

建议标准流程如下：

1. raid 结算完成后回写 `PlayerProfileRuntime`。
2. `BaseHubService` 根据最新 Profile 重建 `BaseHubRuntimeState`。
3. 载入 `BaseHub` 场景。
4. 生成基地态第一人称玩家。
5. 刷新已建造设施、商人驻留点和交互提示。
6. UI 根据基地上下文加载可用窗口入口。

## 20.2 设施建造流程

建议标准流程如下：

1. 玩家在基地中靠近建造位并发起交互。
2. UI 展示该建造位可建造设施及需求。
3. `FacilityConstructionService` 校验材料、任务、前置设施与等级。
4. 校验通过后扣除资源并更新 `FacilityRuntimeState`。
5. 场景中激活设施表现体。
6. 若该设施能解锁商人或配方，则同步刷新相关系统状态。

## 20.3 休息流程

建议标准流程如下：

1. 玩家在休息间交互。
2. `RestService` 校验休息条件和可能的消耗。
3. 清理连续 raid 累积的疲劳型 Debuff。
4. 恢复疲劳值，并按规则恢复其他状态。
5. 写入下一局临时 Buff。
6. BaseHub UI 刷新当前生存值和待生效 Buff。

## 20.4 饮食恢复流程

建议标准流程如下：

1. 玩家直接使用食物/饮品，或在厨房制作完成后选择消耗。
2. `ConsumptionService` 解析该物品的恢复效果。
3. 修改 `SurvivalRuntimeState` 中的饱食度或饮水度。
4. 刷新阈值状态并移除对应 Debuff。
5. 若食物/饮品带 Buff，则交由 `BuffService` 注册。

## 20.5 进图流程

建议标准流程如下：

1. `BaseHub` 中完成配装确认。
2. `LoadoutRuntime` 冻结一份进图快照。
3. `ProfileService` 扣除进图前消耗品和弹药装填变化。
4. `RaidSessionService` 创建新的 `RaidSessionRuntime`。
5. 切换到目标 `Raid_<MapId>` 场景。
6. `Bootstrap` 注入当前 Profile、Loadout、MapDefinition。
7. 场景生成玩家角色，并挂接 JUTPS 桥接组件。
8. 把当前生存值快照复制到 `RaidPlayerRuntime`。
9. 场景生成敌人、精英修正、Boss、容器、撤离点和任务目标。
10. HUD 初始化并开始监听 raid 运行时状态。

## 20.6 容器转移流程

建议标准流程如下：

1. 玩家打开某个容器窗口。
2. UI 从 `ContainerRuntime` 和 `InventoryGridRuntime` 拉取当前格子状态。
3. 玩家拖拽物品时，先在前端做局部合法性预判。
4. 实际放置时调用 `ContainerTransferService`。
5. `ContainerTransferService` 负责：
   - 尺寸校验
   - 旋转校验
   - 目标容器标签校验
   - 栈叠或交换校验
6. 校验通过后写回源容器和目标容器。
7. UI 收到容器变更事件后刷新。

## 20.7 玩家死亡结算流程

建议标准流程如下：

1. `RaidPlayerRuntime` 进入死亡状态。
2. `DeathLossService` 读取 `EquipmentRuntime` 的槽位规则。
3. 按 `DropOnDeath` 将装备分成：
   - 保留清单
   - 丢失清单
4. 背包、胸挂中的局内战利品生成战利品容器或尸体容器。
5. 角色身上保留物品回写到 `ProfileRuntime`。
6. 当前生存值、伤病和任务进度按失败规则写回。
7. raid 标记为失败。
8. 进入结算界面。

## 20.8 撤离成功结算流程

建议标准流程如下：

1. 玩家触发撤离点交互。
2. `ExtractionService` 校验撤离条件：
   - 是否满足任务或钥匙条件
   - 是否未处于禁止撤离状态
   - 是否已完成必要交互
3. 条件通过后进入撤离倒计时。
4. 成功撤离后由 `RaidResultService` 汇总：
   - 获得物品
   - 任务进度
   - 经验
   - 商人关系变化
   - Boss 首杀或区域发现
   - 生存值变化
5. 把结果并入 `PlayerProfileRuntime`。
6. 写档并返回 `RaidResult` 或 `BaseHub`。

## 20.9 精英生成流程

建议标准流程如下：

1. 地图刷怪点生成普通敌人原型。
2. `EnemySpawnService` 根据地图阶段、区域权重和事件权重决定是否转化为精英。
3. 若为精英，则由 `EliteModifierService` 选择词缀池。
4. 将词缀写入 `RaidEnemyRuntime`。
5. 通过桥接层把生命、速度、技能、Buff 修正同步到实际敌人。

## 20.10 Boss 阶段流程

建议标准流程如下：

1. `BossDirectorService` 创建 `RaidBossRuntime`。
2. 进入初始阶段并激活第一组技能/行为。
3. 当生命、时间或场景条件达到阈值后切换阶段。
4. `BossMechanicController` 更新：
   - 护盾状态
   - 场景危险
   - 召唤波次
   - Buff / Debuff
5. Boss 死亡后发放专属掉落和阶段完成事件。

## 21. 数据所有权与同步规则

这一节用于避免后续出现“到底谁才是权威数据”的混乱。

### 权威来源

- 定义数据权威来源：`ScriptableObject Definitions`
- 局外角色档案权威来源：`PlayerProfileRuntime`
- 基地设施状态权威来源：`BaseHubRuntimeState / FacilityRuntimeState`
- 局内角色状态权威来源：`RaidPlayerRuntime`
- 生存值持久状态权威来源：`SurvivalRuntimeState`
- 容器格子状态权威来源：`InventoryGridRuntime / ContainerRuntime`
- 角色动作执行权威来源：`JUCharacterController`
- 武器开火表现权威来源：`JUTPS Weapon`
- UI 只读展示权威来源：对应 Runtime State，不拥有最终状态

### 必须遵守的同步规则

- JUTPS 不直接保存局外仓库状态。
- UI 不直接修改 MonoBehaviour 上的“临时变量”作为最终状态。
- 存档只从 Runtime State 汇总，不直接从 UI 读。
- 场景内对象的可视表现，必须可由 Runtime State 重新恢复。
- BaseHub 与 Raid 对生存值的修改，都必须统一经过 `SurvivalService`。

## 22. 模块接口建议

建议优先抽出：

- `IProfileReadService`
- `IProfileWriteService`
- `IBaseHubService`
- `IFacilityService`
- `ISurvivalService`
- `IRestService`
- `ICraftingService`
- `IRaidSessionService`
- `IInventoryService`
- `IContainerTransferService`
- `IEquipmentService`
- `ILootGenerationService`
- `IBuffService`
- `IMerchantService`
- `IQuestService`
- `IExtractionService`

建议优先抽出的数据接口：

- `IItemInstanceView`
- `IContainerView`
- `IEquipmentView`
- `IFacilityView`
- `ISurvivalStateView`
- `IRaidResultView`
- `IEnemyModifierView`

这些接口的目标不是过度抽象，而是：

- 让 UI 不直接依赖具体 Runtime 实现
- 让 Service 层可替换
- 让测试更容易写

## 23. 测试与质量门槛

这个项目后续最容易出问题的不是战斗动作，而是“规则系统”和“状态同步”。所以测试重点应放在规则层。

## 23.1 EditMode Tests

建议优先写：

- 格子放置合法性测试
- 物品旋转与边界测试
- 死亡丢失规则测试
- 胸甲/胸挂装备合法性测试
- 护甲值结算测试
- 基地设施前置条件测试
- 商人入住条件测试
- 饱食度、饮水度、疲劳值阈值测试
- 生存值归零持续掉血测试
- 休息 Buff 写入测试
- 厨房配方消耗与产出测试
- 精英词缀应用测试
- Buff 叠加与移除测试
- 撤离结算合并测试

## 23.2 PlayMode Tests

建议优先写：

- BaseHub 进入与设施刷新流程
- 进图到撤离完整流程
- 死亡到结算流程
- 容器拖拽与回写流程
- 休息到下一局 Buff 生效流程
- 厨房制作到消耗恢复流程
- Boss 阶段切换流程
- 精英怪生成流程
- UI 与 Runtime State 同步流程

## 23.3 人工回归清单

每个重要里程碑至少回归：

- 进入基地
- 基地第一人称交互
- 进图
- 开火
- 近战
- 拾取
- 容器拖拽
- 食物/饮水消耗
- 休息恢复
- 死亡
- 撤离
- 结算
- 商人交易
- 任务推进

## 24. 首个垂直切片定义

为了避免系统铺太大，建议先把“第一套真正可玩的垂直切片”定义清楚。

### 垂直切片内容

- 1 个可自由移动的 `BaseHub`
- 1 间可交互的休息间
- 1 套基础厨房配方
- 1 张地图
- 1 名局外商人
- 1 套基础任务
- 1 套格子背包
- 1 种普通近战怪
- 1 种精英怪
- 1 个 Boss
- 1 套基础结算

### 垂直切片通过标准

- 可以在基地中第一人称移动并使用设施
- 可以通过休息或饮食调整生存值
- 可以从局外配装进入 raid
- 可以搜刮容器并把物品放入格子背包
- 可以遭遇普通怪、精英怪和 Boss
- 可以死亡并触发丢失逻辑
- 可以成功撤离并把结果写回局外
- 可以重新进入下一局，且局外数据、生存值和基地状态保持一致
