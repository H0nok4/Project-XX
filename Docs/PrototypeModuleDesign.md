# Project-XX 原型模块设计

## 1. 文档目的

本文档用于说明截至 2026-03-18 的 `Project-XX` 工程真实结构、模块职责、场景边界和已跑通闭环。

这份文档不再描述 2026-03-09 左右的早期样板状态，而是对齐当前仓库中的代码、场景和 Builder。

配套文档：

- [ArchitectureAndDataflowReference.md](./ArchitectureAndDataflowReference.md)
- [MetaProfileAndWarehouseDesign.md](./MetaProfileAndWarehouseDesign.md)
- [InRaidCombatSystemDesign.md](./InRaidCombatSystemDesign.md)
- [ModuleInterfaceContracts.md](./ModuleInterfaceContracts.md)
- [DevelopmentRoadmap_Part2.md](./DevelopmentRoadmap_Part2.md)

---

## 2. 当前项目快照

当前工程已经不是“主菜单 + 单张战斗样例图”的早期搜打撤样板，而是一套带有正式局外入口、成长壳体和任务框架的单机原型纵切面。

当前版本的结构真相：

- `BaseScene.unity` 已是正式局外入口，承载基地 Hub、仓库、商人、设施和任务 NPC。
- `MainMenu.unity` 已退化为 `DebugShell`，用于启动、跳转和调试，不再作为正式局外业务主场景。
- `SampleScene.unity` 仍是默认纳入 Build Settings 的战斗验证图。
- 工程中还保留 `SchoolTestScene.unity`、`HospitalTestScene.unity` 等测试战斗场景资产，但它们目前不在默认构建流程中。
- Profile 已接入实例级物品/武器/护甲存档，同时保留兼容旧字段的迁移链路。
- 商人等级、信誉、基地设施、任务系统、对话系统、角色成长系统都已经接入运行时主流程。
- 主菜单、任务日志、局内背包/搜刮窗口、基地提示层都已转为运行时 UGUI/`PrototypeUiToolkit` 路线，不再以 IMGUI 作为主实现。

---

## 3. 场景与构建链

### 3.1 默认流程场景

- `Assets/Scenes/MainMenu.unity`
  - 调试启动壳
  - 保留快速进入基地和战斗的入口
  - 由 `PrototypeMainMenuSceneBuilder` 重建

- `Assets/Scenes/BaseScene.unity`
  - 正式局外入口
  - 包含准备区、仓库区、商人区、任务区、恢复区
  - 由 `BaseHubSceneBuilder` 重建

- `Assets/Scenes/SampleScene.unity`
  - 默认战斗验证图
  - 验证战局状态、AI、Loot、撤离、回写流程
  - 由 `PrototypeIndoorSceneBuilder` 重建

### 3.2 测试场景资产

- `Assets/Scenes/SchoolTestScene.unity`
  - 校园白盒测试图
  - 当前已有 `SchoolTestSceneBuilder` / `SchoolInteriorBuilder` 辅助维护

- `Assets/Scenes/HospitalTestScene.unity`
  - 医院主题测试图
  - 当前以场景资产和变体资源存在，尚未接入默认构建与入口链路

### 3.3 编辑器工具

- `Assets/Res/Scripts/UI/Editor/PrototypeMainMenuSceneBuilder.cs`
  - 生成 `MainMenu.unity`
  - 维护 `PrototypeItemCatalog.asset`
  - 维护 `PrototypeMerchantCatalog.asset`

- `Assets/Res/Scripts/Base/Editor/BaseHubSceneBuilder.cs`
  - 生成 `BaseScene.unity`
  - 维护基地 Hub 布局、商人点位、任务锚点和入口路由配置

- `Assets/Res/Scripts/FPS/Editor/PrototypeIndoorSceneBuilder.cs`
  - 生成 `SampleScene.unity`
  - 维护默认物品、武器、敌人、Loot 表等原型资产

- `Assets/Res/Scripts/FPS/Editor/SchoolTestSceneBuilder.cs`
  - 重建 `SchoolTestScene.unity` 的校园白盒版本

- `Assets/Res/Scripts/LevelDesign/Editor/PrototypeRaidToolkitWindow.cs`
  - 提供战斗地图摆放辅助
  - 当前仍主要服务原型关卡制作

结论：

- 当前已经形成“正式局外入口 + 默认战斗验证图 + 额外测试战斗场景资产”的结构。
- 默认构建链仍以 `MainMenu / BaseScene / SampleScene` 为主，学校和医院测试图尚未进入正式入口流程。

---

## 4. 模块总览

当前工程更适合按 10 个模块理解：

1. 局外入口与场景路由
2. Profile 持久化与迁移
3. 基地 Hub 与设施系统
4. 商人系统与经济壳体
5. 任务、对话与世界状态
6. 角色成长与构筑汇总
7. 战局桥接与结算
8. 玩家战斗运行时
9. Loot / 装备 / 交互系统
10. AI 与战斗地图制作工具

---

## 5. 模块说明

### 5.1 局外入口与场景路由

核心脚本：

- `Assets/Res/Scripts/Profile/MetaEntryRouter.cs`
- `Assets/Res/Scripts/Profile/MetaEntryRouteConfig.cs`
- `Assets/Res/Scripts/Profile/MetaSessionContext.cs`

职责：

- 统一决定默认进入 `BaseScene` 还是 `MainMenu`
- 记录战局返回后的基地落点类型
- 屏蔽“战局结束直接硬编码回主菜单”的旧逻辑

### 5.2 Profile 持久化与迁移

核心脚本：

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/ProfileFileGateway.cs`
- `Assets/Res/Scripts/Profile/ProfileMigrationRunner.cs`
- `Assets/Res/Scripts/Profile/ProfileSchemaVersion.cs`

职责：

- 读写 Profile
- 兼容旧结构并做迁移
- 维护实例级物品、武器、护甲和局外/局内桥接字段
- 承载 `WorldStateData` 与 `PlayerProgressionData`

### 5.3 基地 Hub 与设施系统

核心脚本：

- `Assets/Res/Scripts/Base/BaseHubDirector.cs`
- `Assets/Res/Scripts/Base/BaseFacilityManager.cs`
- `Assets/Res/Scripts/Base/BaseHubZoneMarker.cs`
- `Assets/Res/Scripts/Base/BaseHubTerminalInteractable.cs`

职责：

- 管理基地内交互入口与 UI 焦点
- 处理撤离返场 / 死亡返场的落点与补给
- 驱动仓库、武器库、医疗站、工作台的运行时效果

### 5.4 商人系统与经济壳体

核心脚本：

- `Assets/Res/Scripts/Profile/PrototypeMerchantCatalog.cs`
- `Assets/Res/Scripts/Profile/MerchantManager.cs`
- `Assets/Res/Scripts/Profile/MerchantData.cs`
- `Assets/Res/Scripts/Base/MerchantNPC.cs`
- `Assets/Res/Scripts/Base/MerchantUIManager.cs`

职责：

- 维护商人目录与运行时库存
- 记录交易额、商人等级、信誉和供货委托
- 在基地里通过固定商人点位打开对应商店

### 5.5 任务、对话与世界状态

核心脚本：

- `Assets/Res/Scripts/Quest/QuestManager.cs`
- `Assets/Res/Scripts/Quest/Quest.cs`
- `Assets/Res/Scripts/Quest/QuestObjective.cs`
- `Assets/Res/Scripts/Quest/DialogueSystem.cs`
- `Assets/Res/Scripts/Base/QuestNPC.cs`
- `Assets/Res/Scripts/Profile/WorldStateData.cs`

职责：

- 维护任务接取、推进、提交与奖励发放
- 将任务状态写入 `WorldStateData.questStates`
- 在基地中通过任务 NPC 和任务日志驱动流程

### 5.6 角色成长与构筑汇总

核心脚本：

- `Assets/Res/Scripts/Profile/PlayerProgressionData.cs`
- `Assets/Res/Scripts/Profile/PrototypePlayerProgressionUtility.cs`
- `Assets/Res/Scripts/Profile/PlayerSkillTreeCatalog.cs`
- `Assets/Res/Scripts/Profile/CharacterStatAggregator.cs`
- `Assets/Res/Scripts/FPS/PlayerProgressionRuntime.cs`

职责：

- 维护等级、经验、属性、技能树和重置服务
- 将成长修正与装备词条汇总为统一战斗加成
- 同步到 `PrototypeUnitVitals`、`PlayerWeaponController` 等运行时系统

### 5.7 战局桥接与结算

核心脚本：

- `Assets/Res/Scripts/Raid/RaidGameMode.cs`
- `Assets/Res/Scripts/Raid/ExtractionZone.cs`
- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`

职责：

- 将局外配装应用到战局
- 监听撤离、失败、超时并回写 Profile
- 通过 `MetaEntryRouter` 返回基地或调试入口

### 5.8 玩家战斗运行时

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeFpsInput.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsMovementModule.cs`
- `Assets/Res/Scripts/FPS/PlayerWeaponController.cs`
- `Assets/Res/Scripts/FPS/PlayerMedicalController.cs`
- `Assets/Res/Scripts/FPS/PlayerThrowableController.cs`

职责：

- 控制第一人称移动、武器、医疗、投掷物和交互输入
- 把成长修正和装备状态落实到玩家战斗行为

### 5.9 Loot / 装备 / 交互系统

核心脚本：

- `Assets/Res/Scripts/Interaction/PlayerInteractor.cs`
- `Assets/Res/Scripts/Loot/PrototypeRaidInventorySystem.cs`
- `Assets/Res/Scripts/Loot/LootContainerWindowController.cs`
- `Assets/Res/Scripts/Loot/PlayerInventoryWindowController.cs`
- `Assets/Res/Scripts/Loot/PrototypeCorpseLoot.cs`
- `Assets/Res/Scripts/Items/Runtime/ItemInstance.cs`
- `Assets/Res/Scripts/Items/Runtime/WeaponInstance.cs`
- `Assets/Res/Scripts/Items/Runtime/ArmorInstance.cs`

职责：

- 维护多容器、多槽位的实例级装备/物品逻辑
- 支持地面拾取、开箱、搜尸和局内拖拽交互
- 区分风险区与保护区物资

### 5.10 AI 与战斗地图制作工具

核心脚本：

- `Assets/Res/Scripts/AI/PrototypeBotController.cs`
- `Assets/Res/Scripts/AI/PrototypeEncounterDirector.cs`
- `Assets/Res/Scripts/AI/PrototypeEnemyRuntimeFactory.cs`
- `Assets/Res/Scripts/AI/PrototypeEnemySpawnProfile.cs`
- `Assets/Res/Scripts/LevelDesign/Editor/PrototypeRaidToolkitWindow.cs`

职责：

- 控制 AI 感知、追击、攻击、死亡掉落
- 维护刷怪点 / 区域和敌人运行时生成
- 为战斗地图内容制作提供辅助工具

---

## 6. 当前已跑通的闭环

### 6.1 启动闭环

`Boot -> MetaEntryRouter -> BaseScene（正式） / MainMenu（调试）`

### 6.2 局外闭环

`基地进入 -> 仓库整理 -> 商人交易 -> 委托交付 -> 设施升级 -> 选择战区`

### 6.3 局内闭环

`进入 SampleScene -> 探索 / 战斗 / 拾取 / 搜刮 -> 撤离或失败 -> 回写 Profile -> 返回 BaseScene`

### 6.4 成长闭环

`击杀 / 任务奖励 -> 获得经验 -> 升级 -> 分配属性 / 解锁专精 -> CharacterStatAggregator 汇总 -> 反馈到战斗系统`

---

## 7. 当前关键资源与资产目录

主要目录：

- `Assets/Res/Data/PrototypeFPS/Items`
- `Assets/Res/Data/PrototypeFPS/Weapons`
- `Assets/Res/Data/PrototypeFPS/LootTables`
- `Assets/Res/Data/PrototypeFPS/EnemyProfiles`
- `Assets/Res/Data/PrototypeFPS/Variants`
- `Assets/Resources/PrototypeItemCatalog.asset`
- `Assets/Resources/PrototypeMerchantCatalog.asset`
- `Assets/Resources/MetaEntryRouteConfig.asset`
- `Assets/Resources/AffixPool.asset`

这些资产共同构成当前原型的数据源，Builder 会维护其中一部分默认内容。

---

## 8. 当前边界与风险

当前已经不是纯样板，但也还没有进入正式内容生产结构。主要边界如下：

- 默认 Build Settings 仍只覆盖 `MainMenu`、`BaseScene`、`SampleScene`
- `SchoolTestScene` / `HospitalTestScene` 还没有接入正式入口和回归验证流程
- 阶段 5 之后的“战斗地图商人 / 地图任务 NPC / 世界状态驱动显隐”尚未正式落地
- 剧情演出、CG、视频、Timeline 仍未进入当前主流程
- `PrototypeFpsController`、`PlayerWeaponController`、`PrototypeBotController` 依然是复杂控制器
- 运行时 UI 已切到 UGUI，但视觉和内容生产仍偏原型风格

---

## 9. 建议阅读顺序

1. 先看本文，建立当前工程的整体结构感
2. 再看 [ArchitectureAndDataflowReference.md](./ArchitectureAndDataflowReference.md)
3. 需要理解局外与存档时看 [MetaProfileAndWarehouseDesign.md](./MetaProfileAndWarehouseDesign.md)
4. 需要理解局内战斗时看 [InRaidCombatSystemDesign.md](./InRaidCombatSystemDesign.md)
5. 需要安排后续开发时看 [DevelopmentRoadmap_Part2.md](./DevelopmentRoadmap_Part2.md)
