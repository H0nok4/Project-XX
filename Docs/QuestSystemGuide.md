# 任务系统说明与扩展指南

本文档基于当前工程里的实际实现整理，目标是回答三个问题：

1. 当前任务系统是怎么跑起来的。
2. 后续要扩展任务内容时，应该改哪些层。
3. 要增加新的任务目标或新的探索点，推荐的制作流程是什么。

适用范围：

- 基地任务 NPC
- 商人委托
- 任务日志 / 追踪 HUD
- 基于事件推进的任务目标
- 与 `WorldStateData` 的持久化联动

## 1. 当前结构总览

当前任务系统的核心文件位于：

- `Assets/Res/Scripts/Quest/Quest.cs`
- `Assets/Res/Scripts/Quest/QuestObjective.cs`
- `Assets/Res/Scripts/Quest/QuestReward.cs`
- `Assets/Res/Scripts/Quest/QuestRuntimeState.cs`
- `Assets/Res/Scripts/Quest/QuestManager.cs`
- `Assets/Res/Scripts/Quest/PrototypeQuestCatalog.cs`
- `Assets/Res/Scripts/Quest/DialogueSystem.cs`
- `Assets/Res/Scripts/Base/QuestNPC.cs`
- `Assets/Res/Scripts/Base/BaseHubDirector.cs`
- `Assets/Res/Scripts/Base/BaseHubZoneMarker.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`

它们的职责可以分成 6 层：

### 1.1 定义层

- `Quest` 定义任务本体：ID、名称、描述、前置、任务类型、发布 NPC、提交 NPC、目标列表、奖励。
- `QuestObjective` 是任务目标基类。
- `QuestReward` 定义现金、经验、物品、故事标记、解锁内容等奖励。
- `PrototypeQuestCatalog` 负责在运行时构建当前默认任务列表。

### 1.2 运行时状态层

- `QuestRuntimeState` 保存单个任务的运行时状态。
- 核心字段包括：`status`、`rewardsClaimed`、`tracked`、`objectiveProgress`。
- 任务进度真相保存在 `WorldStateData.questStates`，不是保存在 UI 或 NPC 身上。

### 1.3 运行时管理层

- `QuestManager` 是任务系统唯一的运行时入口。
- 它负责初始化任务目录、读写状态、接任务、推进进度、完成检测、提交校验、奖励发放、保存回档。
- 其他系统如果要推进任务，应尽量通过 `QuestEventHub` 发事件，而不是直接改 `QuestRuntimeState`。

### 1.4 事件层

- `QuestEventHub` 是任务推进的统一事件入口。
- 当前支持的事件类型：`Talk`、`Kill`、`Collect`、`Explore`、`Extract`、`Custom`。
- `QuestManager` 监听 `QuestEventHub.EventRaised`，把事件分发给进行中的任务目标。

### 1.5 展示与交互层

- `QuestNPC` 和 `DialogueSystem` 负责基地任务接取 / 提交 / 汇报。
- `QuestTrackerHUD`、`QuestListUI`、`QuestDetailUI` 负责任务日志和追踪 HUD。
- `PrototypeMainMenuController` 提供基地仓库页、商人页等入口，并且会触发部分任务事件。

### 1.6 场景与触发层

- `BaseHubDirector` 负责基地场景里的任务运行时接入。
- `BaseHubZoneMarker` 描述基地中的功能分区。
- 当前已补上“进入分区时触发探索事件”的链路，仓库区现在会在玩家真正进入分区体积时触发 `Explore(base_warehouse)`。

## 2. 当前任务生命周期

### 2.1 初始化

1. `BaseHubDirector` 或其他入口调用 `QuestManager.GetOrCreate()`。
2. `QuestManager.TryInitialize()` 构建任务目录。
3. `QuestManager` 从 `WorldStateData.questStates` 恢复任务运行时状态。
4. `QuestTrackerHUD` 绑定到 `QuestManager`，开始展示任务。

### 2.2 接取任务

1. 玩家与 `QuestNPC` 交互。
2. `DialogueSystem` 从 `QuestManager` 查询该 NPC 可接 / 可交 / 相关任务。
3. 玩家点击“接取任务”时调用 `QuestManager.StartQuest(...)`。
4. `QuestManager` 重置任务进度、设置追踪、保存状态。

补充：

- `StartQuest` 会在传入 `starterNpcId` 时自动补一条 `Talk` 事件，因此“和发布 NPC 对话”这类第一步目标可以在接任务瞬间被推进。

### 2.3 推进任务

1. 其他系统通过 `QuestEventHub` 抛出任务事件。
2. `QuestManager` 遍历所有进行中的任务。
3. 每个 `QuestObjective` 根据事件类型和匹配规则决定是否增加进度。
4. `QuestManager.RefreshQuestCompletion(...)` 统一刷新目标进度与任务完成状态。
5. 任务日志 / HUD 通过 `QuestChanged` 事件刷新显示。

### 2.4 提交任务

1. 玩家在对应 NPC 或商人处选择提交。
2. `QuestManager.CanClaimQuest(...)` 校验是否全部完成，是否在正确的提交点。
3. `DeliverObjective` 这类目标会在提交时二次校验并消耗仓库物资。
4. `QuestManager.TryClaimQuest(...)` 发放奖励并写回存档。

### 2.5 持久化

- 任务状态保存在 `WorldStateData.questStates`。
- `QuestManager.SaveRuntimeState()` 最终通过 `PrototypeMainMenuController.SaveProfileFromContainers()` 或 `PrototypeProfileService.SaveProfile(...)` 写回。
- 这保证了基地、战斗、商人、设施和任务使用同一套存档链路。

## 3. 当前已实现的目标类型

当前目标类型都定义在 `Assets/Res/Scripts/Quest/QuestObjective.cs`。

### 3.1 事件驱动型

- `TalkObjective`
  - 监听 `QuestEventType.Talk`
  - 常用于与 NPC 对话
- `KillObjective`
  - 监听 `QuestEventType.Kill`
  - 支持按敌人 ID、敌人类型、Boss 条件筛选
- `CollectObjective`
  - 监听 `QuestEventType.Collect`
  - 常用于拾取或收集物品
- `ExploreObjective`
  - 监听 `QuestEventType.Explore`
  - 当前仓库区参观目标就使用这个类型
- `ExtractObjective`
  - 监听 `QuestEventType.Extract`
  - 常用于撤离类任务
- `CustomEventObjective`
  - 监听 `QuestEventType.Custom`
  - 用于临时或系统特定的目标

### 3.2 仓库校验型

- `DeliverObjective`
  - 不依赖事件累计
  - 当前进度由 `QuestManager.GetStorageItemCount(...)` 实时计算
  - 提交任务时通过 `ConsumeClaimRequirements(...)` 真正扣除物资

## 4. 当前事件源分布

下面这些地方已经会主动抛任务事件：

- `DialogueSystem`
  - NPC 对话时触发 `RaiseTalk(...)`
- `PlayerProgressionRuntime`
  - 击杀敌人时触发 `RaiseKill(...)`
- `GroundLootItem`
  - 拾取时触发 `RaiseCollect(...)`
- `LootContainer`
  - 开箱时触发 `RaiseCustom("loot_container_opened")`
  - 武器箱会额外触发 `RaiseCustom("weapon_crate_opened")`
- `RaidGameMode`
  - 开局时触发 `RaiseCustom("raid_started")`
  - 撤离时触发 `RaiseExtract(...)`
- `PrototypeMainMenuController`
  - 打开基地仓库页时触发 `RaiseExplore("base_warehouse")`
- `BaseHubDirector`
  - 玩家进入带有探索 ID 的基地分区时触发 `RaiseExplore(...)`
- `BaseHubZoneMarker`
  - 现在支持配置 `questExploreLocationId`
  - 若未配置，仓库区会默认回退到 `base_warehouse`

## 5. 本次修复说明

问题表现：

- 任务“基地熟悉”中的目标“参观仓库区”无法稳定完成。

根因：

- 之前真正推进该目标的逻辑只在 `PrototypeMainMenuController.ShowPage(MenuPage.Warehouse)` 里。
- 也就是说，目标实际含义更接近“打开仓库页”，而不是“进入仓库区”。
- 玩家只是走到基地仓库区时，并不会触发探索事件。

本次修复：

- `BaseHubDirector` 现在会检测玩家是否进入某个分区体积。
- 当玩家进入 `BaseHubZoneMarker` 覆盖的区域时，会读取该分区的 `QuestExploreLocationId` 并触发 `QuestEventHub.RaiseExplore(...)`。
- 事件只在“进入”时触发，不会停留在区域内每帧重复触发。
- 玩家离开后再次进入，会再次触发，行为与区域事件一致。
- `BaseHubZoneMarker` 增加了 `questExploreLocationId` 配置入口。
- 为了兼容现有场景，仓库区在未手动配置时仍会默认映射到 `base_warehouse`。

结论：

- “参观仓库区”现在既可以通过进入仓库分区完成，也保留了从仓库 UI 页面进入时的兜底触发。

## 6. 如何新增一条任务

如果只是新增任务内容，而不是新增目标类型，建议按下面流程走。

### 6.1 选择任务发布与提交入口

先明确：

- `giverNpcId`
- `turnInNpcId`
- `QuestType`
- 前置任务 `prerequisiteQuests`
- 前置剧情标记 `requiredStoryFlags`

### 6.2 在任务目录里定义任务

位置：

- `Assets/Res/Scripts/Quest/PrototypeQuestCatalog.cs`

做法：

1. 在 `CreateDefaultQuests(...)` 里新增一个 `Quest` 或 `MerchantQuest`。
2. 填写稳定的 `questId`，不要后续频繁改名。
3. 配置目标列表 `objectives`。
4. 配置奖励 `reward`。

建议：

- `questId` 使用稳定、可读的英文 ID，例如 `chapter1_visit_recovery`。
- 文案可以改，但 ID 一旦进档，不要轻易重命名。

### 6.3 选择目标推进方式

优先复用现有目标类型：

- 对话：`TalkObjective`
- 击杀：`KillObjective`
- 收集：`CollectObjective`
- 区域探索：`ExploreObjective`
- 撤离：`ExtractObjective`
- 物资提交：`DeliverObjective`
- 特殊一次性逻辑：`CustomEventObjective`

### 6.4 接通事件源

如果目标类型已经存在，只需要确保对应系统会抛事件。

示例：

- 去某个基地分区看看：确保对应分区的 `questExploreLocationId` 正确
- 与某个 NPC 交谈：确保 `QuestNPC` / `DialogueSystem` 使用的是同一个 NPC ID
- 开某类箱子：在对应交互逻辑里抛 `RaiseCustom(...)`

### 6.5 手动验证

新增任务后至少验证以下项目：

1. 能在正确的 NPC / 商人处接取。
2. HUD 和任务日志能显示目标。
3. 推进事件发生后，目标进度正常变化。
4. 完成后只能在正确的提交点提交。
5. 奖励、故事标记、解锁状态可以持久化。

## 7. 如何新增一个探索目标

这是当前最常见、也最容易继续扩展的一类。

### 7.1 基地分区探索

适用场景：

- 参观仓库区
- 到任务区报到
- 进入恢复区

制作流程：

1. 在任务里添加 `ExploreObjective`，例如 `locationId = "base_recovery"`。
2. 找到目标区域对应的 `BaseHubZoneMarker`。
3. 在该标记上设置 `questExploreLocationId = "base_recovery"`。
4. 运行后，玩家进入该分区体积时会自动抛 `Explore(base_recovery)`。

注意：

- 现在仓库区有内置兼容映射，不配置也能走 `base_warehouse`。
- 新增其他分区时，推荐明确配置 `questExploreLocationId`，不要再继续依赖硬编码回退。

### 7.2 非基地场景探索

如果探索点不属于基地分区，推荐做法是：

1. 在对应交互脚本、触发器或体积组件里调用 `QuestEventHub.RaiseExplore("your_location_id")`。
2. 在任务定义里使用同一个 `locationId`。

推荐的 ID 规则：

- `base_warehouse`
- `base_recovery`
- `raid_lab_archive`
- `raid_rooftop_signal`

## 8. 如何新增一个全新的目标类型

当现有目标类型不够用时，再新增新的 `QuestObjective` 子类。

### 8.1 先判断属于哪一类

优先先想清楚，它更接近下面哪种模式：

- 事件累计型
  - 例如“交互 3 次终端”“扫描 5 个样本”
- 实时计算型
  - 例如“仓库里至少有 20 发子弹”
- 提交消耗型
  - 例如“交付任务道具”
- 组合校验型
  - 例如“白天击杀 Boss 后撤离”

### 8.2 实现方式

#### 方案 A：事件累计型

适合大部分新目标。

做法：

1. 在 `QuestObjective.cs` 里新增一个继承自 `EventQuestObjective` 的类型。
2. 指定对应的 `QuestEventType`。
3. 实现 `Matches(QuestEventRecord record)`。
4. 如果增量不是固定 `record.Amount`，重写 `ResolveProgressDelta(...)`。
5. 在业务逻辑里调用对应 `QuestEventHub` 事件。

#### 方案 B：实时计算型

适合进度来自外部状态而不是事件累计。

做法：

1. 继承 `QuestObjective`。
2. 重写 `GetCurrentProgress(...)`。
3. 如果需要提交时校验或消耗，再重写 `CanClaim(...)` / `ConsumeClaimRequirements(...)`。

`DeliverObjective` 就是当前的参考实现。

### 8.3 新目标类型制作清单

1. 在 `QuestObjective.cs` 中实现新类。
2. 给新类增加必要字段，例如 `terminalId`、`factionId`、`distanceMeters`。
3. 在实际业务系统里发任务事件，或者提供可查询的运行时数据。
4. 在 `PrototypeQuestCatalog.cs` 中创建一个使用该新目标的任务样例。
5. 进游戏验证接取、推进、提交、存档恢复。

## 9. 后续扩展建议

当前这套系统已经能支撑一章主线、基地任务和商人委托，但如果任务量继续增加，建议优先做下面几项整理。

### 9.1 把目标类型拆文件

当前所有目标类型都堆在 `QuestObjective.cs` 中。

建议后续拆成：

- `TalkObjective.cs`
- `KillObjective.cs`
- `ExploreObjective.cs`
- `DeliverObjective.cs`

这样更利于维护和定位。

### 9.2 把任务定义数据化

当前任务定义都写在 `PrototypeQuestCatalog.cs` 中，适合原型阶段，但不适合大量内容制作。

后续可以考虑：

- ScriptableObject 任务定义
- JSON / 表驱动导入
- 编辑器工具批量校验任务 ID、前置任务、奖励引用

### 9.3 增加通用触发器组件

当前探索逻辑已经接入基地分区，但战斗地图仍然建议补一个通用触发器，例如：

- `QuestTriggerVolume`
- `QuestEventEmitter`

用于在地图里直接配置 `Talk / Explore / Custom` 触发，不必每次都手写脚本。

### 9.4 任务链与原子任务继续分离

当前原子任务状态由 `QuestManager` 负责，这是对的。

后续如果做章节任务链，建议保持：

- `QuestManager` 只记录单任务目标进度和提交状态
- `QuestChainRuntime` 只记录链路阶段推进
- `WorldStateData` 只记录全局结果和解锁状态

不要把剧情阶段直接塞回单个 `QuestRuntimeState`。

## 10. 制作时的注意事项

### 10.1 ID 稳定

- `questId`
- `npcId`
- `merchantId`
- `locationId`
- `eventId`

这些都应视为存档兼容字段，尽量一次定好。

### 10.2 不要让 UI 成为数据真相

UI 只能查询和展示，不要让 UI 自己维护任务状态。

正确入口应当始终是：

- `QuestManager`
- `QuestEventHub`
- `WorldStateData`

### 10.3 新目标先做一个最小闭环

每次新增目标类型，先做 1 条测试任务验证完整闭环：

1. 接取
2. 推进
3. 完成
4. 提交
5. 存档恢复

确认没问题后，再批量铺内容。

## 11. 当前推荐的新增任务工作流

如果后续继续让 AI 或同事接手，推荐直接按下面模板提任务：

1. 说明任务 ID、名称、发布 NPC、提交 NPC。
2. 说明目标类型和每一步需要的事件 ID / 位置 ID / 物品 ID。
3. 说明奖励内容。
4. 指定是否需要新触发器、是否需要新目标类型。
5. 要求同时补一条最小验证路径。

这样最容易在不破坏现有结构的前提下稳定扩展。
