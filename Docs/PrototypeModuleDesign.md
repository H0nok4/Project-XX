# Project-XX 原型模块设计

## 1. 文档目的

本文档用于说明当前 `Project-XX` 原型工程的模块划分、场景职责、核心数据流，以及后续继续扩展时应保持的边界。

这份文档对应的是 2026-03-09 之前已经落地的实现，不是早期计划稿。

配套文档：

- [InRaidCombatSystemDesign.md](./InRaidCombatSystemDesign.md)
- [MetaProfileAndWarehouseDesign.md](./MetaProfileAndWarehouseDesign.md)
- [ArchitectureAndDataflowReference.md](./ArchitectureAndDataflowReference.md)
- [ModuleInterfaceContracts.md](./ModuleInterfaceContracts.md)
- [RefactorRoadmap.md](./RefactorRoadmap.md)

---

## 2. 当前原型范围

当前工程已经不是单纯的 FPS 射击样板，而是一套可反复验证的单机搜打撤原型，已覆盖：

- 主菜单、仓库、商店、进局、撤离、死亡结算
- 风险装备与保护槽位
- 局内拾取、开箱、尸体搜刮、带出
- 部位伤害、护甲、耐久、状态效果、体力
- 多武器槽、近战、药品、弹药
- 基于噪声、视觉、嗅觉的敌人 AI
- 门、可破坏物、随机 Loot、敌人刷怪、关卡搭建工具

当前重点不是做完整产品，而是维持一套可快速迭代的 vertical slice。

---

## 3. 主要场景与构建器

### 3.1 场景

- `Assets/Scenes/MainMenu.unity`
  - 局外入口
  - 提供首页、仓库页、商店页
  - 由 `PrototypeMainMenuController` 驱动

- `Assets/Scenes/SampleScene.unity`
  - 局内样例战斗场景
  - 包含玩家、AI、门、破坏物、Loot、撤离点、战局状态机
  - 用于验证局内玩法闭环

### 3.2 构建器

- `Assets/Res/Scripts/UI/Editor/PrototypeMainMenuSceneBuilder.cs`
  - 生成 `MainMenu.unity`
  - 维护 `PrototypeItemCatalog.asset`
  - 维护 `PrototypeMerchantCatalog.asset`

- `Assets/Res/Scripts/FPS/Editor/PrototypeIndoorSceneBuilder.cs`
  - 生成 `SampleScene.unity`
  - 生成和维护部分默认物品、武器、护甲、敌人配置、Loot 表

结论：

- 当前两张核心场景都仍然是“可重建原型场景”
- 真正的长期正式关卡还没有从原型构建链中拆出来

---

## 4. 模块总览

当前工程可以按 9 个模块理解：

1. 局外资料、仓库与商店
2. 战局状态与场景流转
3. 玩家输入与角色操控
4. 单位生命、部位、护甲、状态效果
5. 物品、武器、弹药、药品定义
6. 交互、背包、拾取、搜刮、尸体 Loot
7. AI 感知、追击、攻击与生成
8. 关卡原型编辑工具
9. 表现层与调试辅助

依赖方向建议维持为：

`定义资产 -> 运行时系统 -> UI / 调试表现`

而不是 UI 反过来驱动底层状态。

---

## 5. 模块说明

### 5.1 局外资料、仓库与商店

核心脚本：

- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/PrototypeItemCatalog.cs`
- `Assets/Res/Scripts/Profile/PrototypeMerchantCatalog.cs`

职责：

- 加载与保存 Profile
- 管理仓库、武器柜、战斗背包、安全箱、特殊装备槽
- 管理主武器、副武器、近战武器与护甲配置
- 处理商店购买与出售
- 把局外配置带进局内

当前规则：

- `Warehouse Stash` 和 `Weapon Locker` 为安全区
- `Raid Backpack`、已装备主副武器、已装备护甲为风险区
- `Melee`、`Secure Container`、`Special Equipment` 为保护区
- 死亡时保留保护区，清空风险区
- 撤离时保留当前战斗背包与已装备物

### 5.2 战局状态与场景流转

核心脚本：

- `Assets/Res/Scripts/Raid/RaidGameMode.cs`
- `Assets/Res/Scripts/Raid/ExtractionZone.cs`
- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`

职责：

- 控制战局 `Idle / Running / Extracted / Failed / Expired`
- 监听玩家死亡与撤离
- 显示结算结果
- 返回主菜单前回写 Profile

### 5.3 玩家输入与角色操控

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeFpsInput.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`

职责：

- 封装输入
- 控制第一人称移动、视角、跳跃、空中控制
- 控制多武器槽、开火、换弹、近战、医疗快捷键
- 控制体力、奔跑、姿态与移动噪声

当前操控特征：

- `C` 切换站立 / 蹲下
- `LCtrl + 鼠标滚轮` 调整移动速度比例
- `Shift` 强制站立并满速冲刺
- 速度档位与蹲姿都会影响移动噪声，进而影响 AI 听觉反应

### 5.4 单位生命、部位、护甲、状态效果

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeUnitDefinition.cs`
- `Assets/Res/Scripts/FPS/PrototypeUnitVitals.cs`
- `Assets/Res/Scripts/FPS/PrototypeUnitHitbox.cs`
- `Assets/Res/Scripts/FPS/PrototypeStatusEffectController.cs`
- `Assets/Res/Scripts/FPS/PrototypeCombatTextController.cs`

职责：

- 处理部位血量与溢出伤害
- 处理护甲覆盖区、耐久、穿深与护甲伤
- 处理单位级状态效果
- 提供受击飘字、护甲损坏反馈、死亡来源记录

当前设计：

- 部位系统主要负责结构化伤害结算
- 出血、骨折、止痛等效果已抽成单位级 Buff / Debuff
- 这为后续扩展 RPG 化状态留出了统一入口

### 5.5 物品、武器、弹药、药品定义

核心脚本：

- `Assets/Res/Scripts/Items/Definitions/ItemDefinition.cs`
- `Assets/Res/Scripts/Items/Definitions/PrototypeWeaponDefinition.cs`
- `Assets/Res/Scripts/Items/Definitions/AmmoDefinition.cs`
- `Assets/Res/Scripts/Items/Definitions/ArmorDefinition.cs`
- `Assets/Res/Scripts/Items/Definitions/MedicalItemDefinition.cs`

职责：

- 提供静态定义资产
- 支持武器、弹药、护甲、药品等基础数据驱动

当前限制：

- 仍以定义级数据为主
- 武器实例状态、耐久实例、配件实例尚未完整持久化

### 5.6 交互、背包、拾取、搜刮、尸体 Loot

核心脚本：

- `Assets/Res/Scripts/Interaction/IInteractable.cs`
- `Assets/Res/Scripts/Interaction/PlayerInteractor.cs`
- `Assets/Res/Scripts/Inventory/InventoryContainer.cs`
- `Assets/Res/Scripts/Loot/GroundLootItem.cs`
- `Assets/Res/Scripts/Loot/LootContainer.cs`
- `Assets/Res/Scripts/Loot/LootContainerWindowController.cs`
- `Assets/Res/Scripts/Loot/PlayerInventoryWindowController.cs`
- `Assets/Res/Scripts/Loot/PrototypeCorpseLoot.cs`
- `Assets/Res/Scripts/Loot/PrototypeWeaponPickup.cs`

职责：

- 玩家对可交互物的统一射线查询
- 地面拾取与箱子搜刮
- 局内背包查看与丢弃
- 尸体统一搜刮：武器、护甲、额外物品进入同一窗口

### 5.7 AI 感知、追击、攻击与生成

核心脚本：

- `Assets/Res/Scripts/AI/PrototypeBotController.cs`
- `Assets/Res/Scripts/AI/PrototypeEnemySpawnProfile.cs`
- `Assets/Res/Scripts/AI/PrototypeEnemyRuntimeFactory.cs`
- `Assets/Res/Scripts/AI/PrototypeEncounterDirector.cs`

职责：

- 控制敌人 archetype 行为
- 生成敌人运行时角色
- 管理刷怪点 / 区域生成

当前 archetype：

- 普通丧尸
- 警察丧尸
- 军人丧尸
- 丧尸犬

当前感知：

- 视觉
- 听觉
- 丧尸犬的嗅觉

当前远程攻击特征：

- 支持单发冷却 / 短点射冷却
- 使用锁定区域内随机弹道，而不是完美锁头

### 5.8 关卡原型编辑工具

核心脚本：

- `Assets/Res/Scripts/LevelDesign/Editor/PrototypeRaidToolkitWindow.cs`
- `Assets/Res/Scripts/Interaction/PrototypeDoor.cs`
- `Assets/Res/Scripts/Interaction/PrototypeBreakable.cs`
- `Assets/Res/Scripts/Loot/LootTableDefinition.cs`
- `Assets/Res/Scripts/Loot/GroundLootSpawnPoint.cs`

职责：

- 快速搭 blockout
- 放置门、玻璃、木板等可交互物
- 放置随机 Loot 容器与地面刷新点
- 放置敌人刷怪点与刷怪区域

### 5.9 表现层与调试辅助

核心点：

- HUD
- 受击飘字
- 头顶血条
- 战局结算 IMGUI
- 主菜单 IMGUI

当前状态：

- 表现层仍大量使用 IMGUI
- 适合原型迭代，不适合长期复杂 UI

---

## 6. 当前已跑通的闭环

### 6.1 局外闭环

`主菜单 -> 仓库整理 -> 商店买卖 -> 配置战斗背包 / 武器 / 护甲 -> 进入战局`

### 6.2 局内闭环

`探索 -> 拾取 -> 开箱 -> 杀敌 -> 搜尸 -> 撤离或死亡 -> 结算`

### 6.3 带入带出闭环

- 进局时装填战斗背包、武器和护甲
- 撤离时保留风险区当前物品
- 死亡时清空风险区，只保留保护区

---

## 7. 当前关键数据资产

主要目录：

- `Assets/Res/Data/PrototypeFPS/Items`
- `Assets/Res/Data/PrototypeFPS/Weapons`
- `Assets/Res/Data/PrototypeFPS/UnitDefinitions`
- `Assets/Res/Data/PrototypeFPS/LootTables`
- `Assets/Res/Data/PrototypeFPS/EnemyProfiles`
- `Assets/Resources/PrototypeItemCatalog.asset`
- `Assets/Resources/PrototypeMerchantCatalog.asset`

这些资产承担“原型数据源”的角色，Builder 会对其中部分内容进行生成或维护。

---

## 8. 当前已知限制

当前仍然是原型，不是正式生产结构。主要限制：

- 主菜单、仓库、结算、搜刮 UI 仍是 IMGUI
- 物品与武器还没有完整实例化持久化
- 商店没有信誉、补货、限购与价格波动
- 场景仍以可重建样例为主，未拆成正式内容场景
- AI 还没有完整组件化，`PrototypeBotController` 仍承担较多职责

---

## 9. 建议阅读顺序

1. 先看本文，建立整体边界感
2. 再看 [InRaidCombatSystemDesign.md](./InRaidCombatSystemDesign.md)
3. 然后看 [MetaProfileAndWarehouseDesign.md](./MetaProfileAndWarehouseDesign.md)
4. 需要追数据流时看 [ArchitectureAndDataflowReference.md](./ArchitectureAndDataflowReference.md)
5. 需要确定边界时看 [ModuleInterfaceContracts.md](./ModuleInterfaceContracts.md)
6. 需要安排下一阶段开发时看 [RefactorRoadmap.md](./RefactorRoadmap.md)
