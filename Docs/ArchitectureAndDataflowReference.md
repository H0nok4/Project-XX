# 架构与数据流参考

## 1. 分层视角

截至 2026-03-18，当前工程更适合按“配置资产 -> 持久化状态 -> Meta 运行时 -> Raid 运行时 -> 表现层 -> 编辑器工具”这条链路理解。

### 1.1 配置资产层

代表对象：

- `ItemDefinition`
- `PrototypeWeaponDefinition`
- `ArmorDefinition`
- `MedicalItemDefinition`
- `LootTableDefinition`
- `PrototypeEnemySpawnProfile`
- `PrototypeItemCatalog`
- `PrototypeMerchantCatalog`
- `MetaEntryRouteConfig`
- `AffixPool`

职责：

- 提供静态配置
- 不保存玩家运行时状态
- 允许 Builder 重建默认资产

### 1.2 持久化状态层

代表对象：

- `PrototypeProfileService.ProfileData`
- `WorldStateData`
- `PlayerProgressionData`
- `SavedItemInstanceDto`
- `SavedWeaponInstanceDto`
- `SavedArmorInstanceDto`

职责：

- 保存局外真相
- 保存任务、商人、设施和成长数据
- 通过迁移链兼容旧结构

### 1.3 Meta 运行时层

代表对象：

- `MetaEntryRouter`
- `PrototypeMainMenuController`
- `BaseHubDirector`
- `MerchantManager`
- `BaseFacilityManager`
- `QuestManager`

职责：

- 决定局外入口
- 组织基地 Hub 内的交互和页面打开
- 在局外更新商人、设施、任务和成长状态

### 1.4 Raid 运行时层

代表对象：

- `RaidGameMode`
- `PrototypeRaidProfileFlow`
- `PrototypeFpsController`
- `PlayerWeaponController`
- `PrototypeUnitVitals`
- `PrototypeStatusEffectController`
- `PrototypeBotController`
- `PrototypeEncounterDirector`

职责：

- 推进局内战斗与结算
- 承接局外配装和成长加成
- 产出局内结果并回写 Profile

### 1.5 表现与交互层

代表对象：

- `PrototypeMainMenuUguiView`
- `QuestTrackerHUD`
- `DialogueSystem`
- `LootContainerWindowController`
- `PlayerInventoryWindowController`
- `PrototypeCombatTextController`
- `PrototypeTargetHealthBar`
- `PlayerInteractor`

职责：

- 与玩家交互
- 显示当前状态
- 不应成为底层结算真相来源

### 1.6 编辑器工具层

代表对象：

- `PrototypeMainMenuSceneBuilder`
- `BaseHubSceneBuilder`
- `PrototypeIndoorSceneBuilder`
- `SchoolTestSceneBuilder`
- `PrototypeRaidToolkitWindow`

职责：

- 重建默认场景与资产
- 提供地图制作辅助
- 不参与运行时结算

---

## 2. 场景级架构

## 2.1 MainMenu.unity

当前定位：

- 调试启动壳
- 快速跳转入口
- 默认由 `PrototypeMainMenuSceneBuilder` 重建

主要对象：

- `PrototypeMainMenuController`
- `PrototypeMainMenuUguiView`
- `PrototypeItemCatalog`
- `PrototypeMerchantCatalog`

主要能力：

- 加载 Profile
- 快速进入基地或默认战区
- 用于调试成长、资金和基础配装状态

不再负责：

- 作为正式局外主场景长期承载仓库、商人、设施和任务业务

## 2.2 BaseScene.unity

当前定位：

- 正式局外入口
- 基地 Hub
- 当前 Meta 主循环承载场景

核心结构：

- `BH_BaseHubRoot`
  - `10_ReadyRoom`
  - `20_Warehouse`
  - `30_MerchantWing`
  - `40_MissionWing`
  - `50_RecoveryBay`
  - `60_Gameplay`
  - `70_Wayfinding`
- `BaseHubPlayer`
- `MetaUi`
- `BaseHubSystems`

主要对象：

- `BaseHubDirector`
- `PrototypeMainMenuController`
- `BaseFacilityManager`
- `MerchantUIManager`
- `QuestManager`
- 任务 NPC 锚点和商人锚点

主要能力：

- 打开仓库页和商人页
- 处理基地返场落点
- 承载设施升级、商人交互和任务入口

## 2.3 SampleScene.unity

当前定位：

- 默认战斗验证图
- 当前 Build Settings 中的默认 Raid 场景

主要对象：

- `RaidGameMode`
- `PrototypeRaidProfileFlow`
- 玩家对象：
  - `PrototypeFpsController`
  - `PrototypeFpsInput`
  - `PlayerWeaponController`
  - `PlayerMedicalController`
  - `PlayerThrowableController`
  - `PlayerProgressionRuntime`
  - `PrototypeUnitVitals`
- AI / 遭遇对象：
  - `PrototypeBotController`
  - `PrototypeEncounterDirector`
  - `PrototypeEnemySpawnPoint`
  - `PrototypeEnemySpawnArea`
- 场景对象：
  - `LootContainer`
  - `GroundLootItem`
  - `ExtractionZone`
  - `PrototypeDoor`
  - `PrototypeBreakable`

## 2.4 额外测试场景

当前工程中还存在：

- `SchoolTestScene.unity`
- `HospitalTestScene.unity`

它们用于战斗地图方向探索和白盒验证，但目前不在默认 Build Settings 流程中，也不属于正式入口链的一部分。

---

## 3. 关键数据流

## 3.1 默认启动到 Meta 入口

流程：

1. 读取 `MetaEntryRouteConfig`
2. `MetaEntryRouter.EnterDefaultMeta()`
3. 若开启调试入口，则进入 `MainMenu`
4. 否则默认进入 `BaseScene`

关键点：

- 路由真相在 `MetaEntryRouter`
- 不是由某个单独场景自己决定“下一个应该去哪”

## 3.2 BaseScene 中的局外交互打开链

流程：

1. 玩家靠近终端或 NPC
2. `PlayerInteractor` 命中 `IInteractable`
3. `BaseHubDirector` 接收交互
4. `BaseHubDirector` 调用 `PrototypeMainMenuController.ShowPage(...)` 或 `ShowMerchant(...)`
5. `PrototypeMainMenuUguiView` 重建对应页面

关键点：

- 基地负责入口分发
- 页面内容仍由 `PrototypeMainMenuController + Presenter/View` 统一维护

## 3.3 从基地出击到战局加载

流程：

1. 玩家在 `BaseScene` 完成配装和地图选择
2. `PrototypeMainMenuController` 触发 `MetaEntryRouter.EnterRaid(sceneName)`
3. 加载 `SampleScene` 或当前选定战区
4. `PrototypeRaidProfileFlow.Awake()` 读取 Profile
5. 将以下内容应用到玩家：
   - 背包
   - 安全箱
   - 特殊装备
   - 主副武器 / 近战
   - 护甲
   - 成长数据

## 3.4 战局结果回写与返回

流程：

1. `RaidGameMode` 进入 `Extracted / Failed / Expired`
2. `PrototypeRaidProfileFlow` 收集当前战局状态
3. 永久回写：
   - 安全箱
   - 特殊装备
   - 近战槽
   - 成长数据
4. 若成功撤离，额外回写：
   - 战斗背包
   - 当前护甲
   - 当前主副武器
5. 若失败或超时，清空风险区
6. `MetaEntryRouter.RecordRaidReturnArrival(...)`
7. 点击返回后走 `MetaEntryRouter.ReturnFromRaid(...)`

## 3.5 任务状态写回

流程：

1. `QuestManager` 初始化时读取 `WorldStateData.questStates`
2. 任务事件通过 `QuestEventHub` 推送
3. `QuestManager` 更新对应 `QuestRuntimeState`
4. 任务奖励、商人信誉和世界标记写回 `WorldStateData`
5. `PrototypeProfileService.SaveProfile(...)` 持久化

关键点：

- 任务真相在 `WorldStateData.questStates`
- 不是保存在某个临时 UI 控件里

## 3.6 成长汇总到战斗属性

流程：

1. `PlayerProgressionData` 保存等级、属性、技能树
2. `CharacterStatAggregator` 汇总：
   - 等级加成
   - 属性加成
   - 技能树节点修正
   - 装备词条修正
3. `PlayerProgressionRuntime` 把结果应用到：
   - `PrototypeUnitVitals`
   - `PlayerWeaponController`
   - `PlayerSkillManager`

关键点：

- 成长和装备词条已进入统一构筑结算链
- 后续新增数值项应继续接入 `CharacterStatAggregator`

## 3.7 玩家命中到尸体搜刮

流程：

1. `PrototypeFpsController` 发起命中检测
2. `PrototypeUnitHitbox` 组装伤害数据
3. `PrototypeUnitVitals.ApplyDamage(...)` 执行护甲、部位和状态效果结算
4. 目标死亡后 `PrototypeBotController` 创建尸体掉落
5. `PrototypeCorpseLoot` 承接武器、护甲和额外物品
6. 玩家通过 `PlayerInteractor` 打开 `LootContainerWindowController`
7. 窗口通过 `PrototypeRaidInventorySystem` 处理拖拽与装备交换

---

## 4. 编辑器工具数据流

## 4.1 MainMenu Builder

`PrototypeMainMenuSceneBuilder` 负责：

- 重建 `MainMenu.unity`
- 维护 `PrototypeItemCatalog.asset`
- 维护 `PrototypeMerchantCatalog.asset`
- 保证 `MainMenu` 维持调试壳职责

## 4.2 Base Hub Builder

`BaseHubSceneBuilder` 负责：

- 重建 `BaseScene.unity`
- 固化基地分区、商人柜台、任务区和恢复区
- 同步 `MetaEntryRouteConfig` 默认入口

## 4.3 Raid / Test Scene Builder

`PrototypeIndoorSceneBuilder` 负责：

- 重建 `SampleScene.unity`
- 维护默认战斗原型资产

`SchoolTestSceneBuilder` 负责：

- 重建 `SchoolTestScene.unity`
- 用于校园图方向验证

## 4.4 Raid Toolkit

`PrototypeRaidToolkitWindow` 负责：

- 提供原型战斗场景的摆放辅助
- 更接近地图制作工具层，而不是业务逻辑层

---

## 5. 当前架构结论

当前架构已经从早期样板推进到“可持续扩展的原型底座”，但仍有这些特点：

- 默认正式流程已经切到 `BaseScene`
- Profile、任务、成长、商人、设施都已进入同一条持久化链
- 运行时 UI 已以 UGUI/`PrototypeUiToolkit` 为主
- 默认 Build Settings 仍只覆盖一条主战区验证路径
- 复杂控制器仍然存在，后续扩展阶段要继续防止职责膨胀
