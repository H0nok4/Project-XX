# Project-XX 开发路线图

更新时间：`2026-04-25`

预制作框架整改建议请同时参考：

- [AGENT.md](/d:/UnityProject/Project-XX/Project-XX/AGENT.md)
- [ProjectXX-FrameworkHealthPlan.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-FrameworkHealthPlan.md)
- [ProjectXX-SystemOwnershipMatrix.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-SystemOwnershipMatrix.md)
- [ProjectXX-ThirdPartyPatchLog.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-ThirdPartyPatchLog.md)
- [ProjectXX-VendorOwnershipMatrix.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-VendorOwnershipMatrix.md)

## 1. 文档目的

这份路线图基于以下文档与当前仓库实现状态整理：

- `FpsExtractionRpg-InitialGDD.md`
- `FpsExtractionRpg-TechnicalDesign.md`
- `FpsExtractionRpg-FrameworkFitAndReductionPlan.md`
- `FpsExtractionRpg-ExecutionChecklist.md`

它的目标不再是描述“理想中的开发顺序”，而是明确：

- 当前代码已经走到哪一步
- `R0` 与 `R1` 实际交付了什么
- 现阶段哪些基础已经稳定，哪些仍然只是样例
- 接下来 `R2 -> R6` 应该如何推进

## 2. 当前实现快照

### 2.1 总体状态

- `R0` 已完成
- `R1` 已完成，并完成了一轮关键稳定化
- 当前主线切换为 `R2：交互、容器、搜刮、死亡丢失、撤离回写`
- 阵营与敌对关系框架已经落地，后续友方 NPC / 中立 NPC / 敌对 NPC 将统一复用这套规则

### 2.2 当前可运行切片

- 启动场景
  - `Assets/Scenes/ProjectXX/ProjectXX_Bootstrap.unity`
  - `Assets/Scenes/ProjectXX/ProjectXX_RaidTestMap.unity`
- 正式测试玩家预制体
  - `Assets/Res/Prefabs/ProjectXX/Gameplay/ProjectXXRaidPlayer.prefab`
- 最小敌人定义
  - `Assets/Res/Data/Definitions/Enemies/BasicZombieEnemy.asset`
- 核心安装入口
  - `Assets/Res/Scripts/ProjectXX/Bootstrap/ProjectXXBootstrap.cs`
  - `Assets/Res/Scripts/ProjectXX/Bootstrap/ProjectXXRaidSceneInstaller.cs`

### 2.3 当前已落地的核心模块

#### 玩家与 FPS Framework 桥接

- `ProjectXXPlayerFacade`
- `ProjectXXAkilaPlayerBridge`
- `ProjectXXCharacterStatBridge`
- `ProjectXXCharacterBuffBridge`
- `ProjectXXEquipmentBridge`
- `ProjectXXWeaponBridge`
- `ProjectXXWeaponDurabilityBridge`
- `ProjectXXAmmoResolver`
- `ProjectXXDamageBridge`
- `ProjectXXFirstPersonViewBridge`
- `ProjectXXMeleeBridge`

#### JUTPS 兼容与敌人桥接

- `JutpsTargetAdapter`
- `JutpsEnemyDamageableAdapter`
- `JutpsEnemyBridge`
- `ProjectXXJutpsFactionBridge`
- `ProjectXXJutpsFactionTargetFilter`

#### 规则与运行时

- `RaidSessionRuntime`
- `ProjectXXRaidRuntimeRegistry`
- `RaidPlayerRuntime`
- `PlayerProfileRuntime`
- `ProjectXXItemDefinition`
- `ProjectXXEquippableItemDefinition`
- `ProjectXXItemInstanceRuntime`
- `ProjectXXInventoryGridRuntime`
- `ProjectXXContainerDefinition`
- `ProjectXXContainerRuntime`
- `ProjectXXEquipmentRuntime`
- `ProjectXXLoadoutRuntime`
- `ProjectXXEnemyDefinition`
- `ProjectXXCompatibilitySettings`
- `ProjectXXCompatibilityValidator`
- `ProjectXXFactionMember`
- `ProjectXXFactionUtility`

#### 表现与测试场景

- `ProjectXXRaidHudController`
- `ProjectXXExtractionPoint`
- `ProjectXX_RaidTestMap.unity`

### 2.4 当前已经验证通过的闭环

- `Akila FPS Framework` 作为正式第一人称玩家执行层
- `JUTPS Zombie AI` 作为敌人原型执行层
- 玩家在测试图中移动、瞄准、开火、近战、受伤、死亡
- `Akila -> JUTPS` 伤害与击杀同步
- `JUTPS -> Akila` 伤害与死亡同步
- `RaidSessionRuntime` 与 `ProjectXXRaidHudController` 的基础状态同步
- `ProjectXXRaidRuntimeRegistry` 已开始承接 `Session / Player / HUD / Extraction / Enemy` 的显式运行时注册
- `ProjectXXCompatibilitySettings` 已开始承接 Tag / Layer 兼容名定义与集中校验
- 测试图中的最小撤离点占位交互
- 敌人不会再互相造成伤害
- 中立单位默认不会主动攻击敌对阵营，但在受伤后会记住伤害来源阵营并开始反击
- 友方单位默认会把 `Enemy` 视作有效敌对目标

### 2.5 本轮稳定化已解决问题

- Overlay Camera 覆盖 Main Camera 的 URP 相机栈问题已修复
- Akila 武器与手臂紫材质问题已修复，完成首轮 URP 材质替换
- JUTPS 敌人悬空、慢速、未贴地的问题已修复
- `Projectile` 命中层未同步导致“开火但不伤害敌人”的问题已修复
- 敌人受击后出现环境弹孔贴花的问题已修复
- 敌人与玩家的伤害链都已接入统一阵营判定
- JUTPS 的 FOV 目标搜索已加入阵营过滤，不再只靠 tag/layer 进行粗粒度判定

## 3. 里程碑总览

| 阶段 | 状态 | 说明 |
| --- | --- | --- |
| `R0` | 已完成 | 项目基线恢复、目录骨架、Bootstrap、双框架接入边界确立 |
| `R1` | 已完成 | 测试地图、第一人称玩家、JUTPS 敌人、基础 HUD、最小战斗闭环 |
| `R2` | 当前主线 | 容器、搜刮、交互、死亡丢失、撤离回写 |
| `R3` | 待开始 | BaseHub、局外仓库、设施、商人与基础局外循环 |
| `R4` | 待开始 | 敌人扩展、精英、Boss、派系化 NPC 遭遇 |
| `R5` | 待开始 | 成长、Buff、异常、生存值与装备/角色长期系统 |
| `R6` | 待开始 | 内容扩展、数值平衡、产品化打磨 |

## 4. R0 实际完成情况

### 4.1 目标回顾

把项目从“Akila 与 JUTPS 已导入，但边界混乱、渲染不稳、目录无主线”的状态，收束成一个可持续开发的基线。

### 4.2 实际交付

- 项目设置基线恢复
  - `ProjectSettings.asset`
  - `GraphicsSettings.asset`
  - `QualitySettings.asset`
  - `TagManager.asset`
  - `EditorBuildSettings.asset`
- Project-XX 主代码目录骨架建立
  - `Assets/Res/Scripts/ProjectXX/...`
- Definitions 目录骨架建立
  - `Assets/Res/Data/Definitions/...`
- 启动场景与启动入口建立
  - `ProjectXX_Bootstrap.unity`
  - `ProjectXXBootstrap.cs`
- 双框架边界已经落地为代码结构
  - `Bridges/FPSFramework`
  - `Bridges/JUTPS`
- 开发者入口文档建立
  - `Docs/ProjectXX/ProjectXX-DeveloperEntry.md`

### 4.3 R0 验收结论

`R0` 通过。当前工程已经能稳定打开、编译，并以：

- `Akila = 玩家执行层`
- `JUTPS = 敌人与世界原型执行层`
- `Project-XX = 规则、桥接、运行时与正式 UI 层`

的结构继续推进。

## 5. R1 实际完成情况

### 5.1 目标回顾

在一张联合测试地图中跑通：

`Akila 第一人称玩家 + JUTPS 近战敌人 + Project-XX HUD + 最小撤离点`

的基础战斗切片。

### 5.2 实际交付

#### 玩家执行层

- 正式测试玩家预制体
  - `ProjectXXRaidPlayer.prefab`
- 第一人称视图与玩家桥接
  - `ProjectXXFirstPersonViewBridge`
  - `ProjectXXPlayerFacade`
  - `ProjectXXAkilaPlayerBridge`
- 属性、Buff、装备、伤害桥
  - `ProjectXXCharacterStatBridge`
  - `ProjectXXCharacterBuffBridge`
  - `ProjectXXEquipmentBridge`
  - `ProjectXXDamageBridge`

#### 武器与近战

- 武器桥接与弹药/耐久代理
  - `ProjectXXWeaponBridge`
  - `ProjectXXAmmoResolver`
  - `ProjectXXWeaponDurabilityBridge`
- 近战桥接
  - `ProjectXXMeleeBridge`
  - `MeleeHitResolver`
  - `MeleeStaggerService`

#### 敌人与 JUTPS 桥接

- `JutpsHealthProxy`
- `JutpsTargetAdapter`
- `JutpsEnemyDamageableAdapter`
- `JutpsEnemyBridge`
- `BasicZombieEnemy.asset`

#### 战斗规则与阵营

- 共享阵营域模型
  - `ProjectXXFaction`
  - `ProjectXXFactionDisposition`
  - `ProjectXXFactionRetaliationMode`
- 阵营成员与敌对关系运行时
  - `ProjectXXFactionMember`
  - `ProjectXXFactionUtility`
- JUTPS 侧阵营桥接与目标过滤
  - `ProjectXXJutpsFactionBridge`
  - `ProjectXXJutpsFactionTargetFilter`

#### 场景与运行时

- `RaidSessionRuntime`
- `ProjectXXRaidRuntimeRegistry`
- `RaidPlayerRuntime`
- `PlayerProfileRuntime`
- `ProjectXX_RaidTestMap.unity`
- `ProjectXXRaidHudController`
- `ProjectXXExtractionPoint`
- `ProjectXXRaidSceneInstaller`

### 5.3 当前 R1 验收结果

以下标准已经达到：

- 玩家能正常第一人称移动、瞄准、开火
- 玩家会被敌人攻击并死亡
- 玩家可以稳定击杀 `JUTPS` 敌人
- 敌人可发现、追击并近战攻击玩家
- HUD 已由 Project-XX 自己接管最小展示
- 不再依赖 JUTPS 默认玩家链路与默认玩家 UI
- 阵营系统已经接入伤害链与 AI 目标链

### 5.4 当前 R1 的边界说明

`R1` 已完成，但它仍然只是“最小可战斗切片”，不是完整的遭遇系统。当前已经稳定的，是：

- 第一人称玩家基础战斗
- 敌人基础追击与近战
- 基础 HUD 与会话状态
- 阵营伤害过滤与动态敌对关系基础框架

当前尚未在内容层完成的，是：

- 正式友方 NPC 样例
- 正式中立 NPC 样例
- 更完整的敌人类型、派系营地、巡逻逻辑与遭遇编排

结论：`R1` 已经从“能跑”进入“能作为 R2/R4 的稳定底座”。

## 6. R2 当前路线图

### 6.1 阶段目标

把当前“能打”的测试切片，推进到：

`能搜、能装、能丢、能撤、能把收益带回局外`

的最小搜打撤闭环。

### 6.2 R2 工作包

#### `R2-A` 定义与数据底座

- `ProjectXXItemDefinition`（第一版已落地）
- `ProjectXXEquippableItemDefinition`（第一版已落地）
- `WeaponDefinition`
- `MeleeWeaponDefinition`
- `ArmorDefinition`
- `ChestRigDefinition`
- `BackpackDefinition`
- `EquipmentSlotDefinition`
- `ProjectXXContainerDefinition`（第一版已落地）

#### `R2-B` 运行时容器系统

- `ProjectXXItemInstanceRuntime`（第一版已落地）
- `ProjectXXGridSize`（第一版已落地）
- `ProjectXXGridCoord`（第一版已落地）
- `ProjectXXInventoryGridRuntime`（第一版已落地）
- `ProjectXXContainerRuntime`（第一版已落地）
- `ProjectXXEquipmentRuntime`（第一版槽位运行时已落地）
- `ProjectXXLoadoutRuntime`（第一版已落地）

#### `R2-C` 交互桥与最小搜刮

- `IProjectXXInteractable / ProjectXXInteractionResult`（第一版已落地）
- `ProjectXXContainerInteractable`（第一版已落地）
- `ProjectXXPlayerInteractionBridge`（第一版已落地：玩家相机射线发现目标，`E` 键触发交互）
- `InteractionPromptPresenter`（第一版由 `RaidSessionRuntime -> ProjectXXRaidHudController` 承接）
- `InteractionRequirementEvaluator`（待补；当前只有 `CanInteract(GameObject)` 的最小判断）
- `ProjectXXRaidSceneInstaller` 已能在缺省场景生成第一个最小测试容器，并在 `ProjectXX_RaidTestMap` 中显式绑定 `SmallLootCrate / FieldRations`
- 已跑通“发现 -> 打开提示 -> 打开搜刮窗口”；正式需求判断仍待补

#### `R2-D` UI 第一版

- `ProjectXXLootWindowController`（第一版已落地：显示容器/背包内容，`T` 拿取第一个可放物品，`G` 放回第一个背包物品，`Esc` 关闭）
- `ProjectXXInventoryTransferUtility`（第一版已落地：跨网格移动第一个可放置物品）
- 背包窗口（仍待正式化；当前只作为 LootWindow 的右侧清单）
- 容器窗口（第一版已落地为 prototype runtime window）
- 物品格模板（待补；当前仍是文字清单）
- 拖拽、放置、交换的最小可用交互（待补；当前只有键盘驱动的 first-fit 转移）

#### `R2-E` 死亡与撤离结算

- `DeathLossService`
- `ExtractionService`
- `RaidResultService`
- 跑通死亡后转尸体容器
- 跑通撤离成功回写局外

#### `R2-F` 阵营样例内容补齐

- 在测试图中加入 1 个友方 NPC 样例
- 在测试图中加入 1 个中立 NPC 样例
- 验证“友方主动攻击敌人、中立受击后反击来源阵营”的场景级表现

说明：

- `R2-E` 比 `R2-F` 更关键
- `R2-F` 的主要作用是把这次已完成的阵营框架从“底层规则可用”推进到“内容样例可测”

### 6.3 R2 通过标准

- 玩家可以通过 Project-XX 交互桥打开容器
- 物品有尺寸、占格、可旋转
- 背包与容器之间可稳定转移物品
- 死亡时按槽位规则丢失装备与战利品
- 成功撤离后，局内新增物品可回写到局外 Profile
- 测试图中可以看到至少一组友方/中立 NPC 的阵营行为样例

## 7. R3-R6 方向概览

### `R3` BaseHub、仓库、设施与商人闭环

- BaseHub 场景
- 局外仓库与配装
- 设施建造与升级
- 首位商人入驻
- 基础任务与局外恢复循环

### `R4` 敌人、精英、Boss 与 NPC 遭遇系统

- 敌人 archetype 扩展
- 精英修饰与 Boss 阶段
- 远程敌人与特殊机制敌人
- 基于 `ProjectXXFactionMember` 的 NPC 阵营遭遇扩展
- 营地、巡逻、救援、伏击等内容型遭遇

### `R5` 成长、生存、技能、Buff 与异常

- 属性与技能树
- Buff / Debuff / 异常
- 饥饿、饮水、疲劳、生存恢复
- 装备词缀与长期成长系统

### `R6` 内容扩展、平衡与产品化

- 地图扩展与掉落表扩展
- 装备池与敌池扩展
- 数值平衡
- 正式 UI 打磨
- 存档、版本迁移、性能与发版准备

## 8. 当前建议

当前最合理的推进顺序是：

1. 继续完成预制作框架健康化整改。当前 ProjectXX 正式 runtime 已基本完成 asmdef 拆分，`JUTPS.Runtime` 与 `JUTPS.Editor` 都已落地，且一批内嵌在 runtime 文件中的 JUTPS custom inspector、menu item、editor-only tool、资产保存逻辑、默认资源绑定逻辑与 `JU Save Load` editor playmode callback 都已抽离或替换；上一轮继续把 `JUFootstep` 默认脚步音频装配迁到 editor context menu，并把 `JUEditor` 中仅供 `JUBoxArea` 创建菜单使用的 `SceneView` helper 移回 editor 侧；随后又把 `JUFootPlacement / JUSlipCapsule` 的 `Handles/Gizmos` scene 可视化迁到 editor 侧，并把 `JUFieldOfViewSensor / Escape` 的 editor API 收敛为 runtime-safe 默认配置与通用 gizmo 绘制，同时移除 `JU_AI_PatrolCharacter / JU_AI_Zombie` 的 editor 调试浮字与 `JUCoverTrigger / GravityBox` 的 editor 箭头句柄；本轮继续把 `BodyLeanInert / VehicleAI / JUVehicleEngine / JUGizmoDrawer / AdvancedRagdollController / Weapon` 的 scene/authoring debug 收敛为通用 `Gizmos`，清掉 ProjectXX runtime 脚本中的 `UnityEditor` 直接引用，给 `JUSaveLoad` 增加可覆盖保存根目录，并落地第一版 Project-XX `Item / Equipment / Container / Loadout` 语义，把 Akila Inventory 降级为起始武器表现绑定；下一步优先做容器交互桥、最小搜刮 UI 与死亡/撤离结算，同时继续清理 `JUEditor / UnityEditorUtilities` 这类仍位于 runtime 路径的 editor helper
2. 在已有 `ProjectXXRaidRuntimeRegistry` 与 `ProjectXXCompatibilitySettings` 基础上，继续把剩余场景级查找和兼容字符串压缩到正式入口；`ProjectXXRaidSceneInstaller` 已支持显式 authoring 玩家、HUD、敌人与 Akila manager 引用
3. 再继续以 `R2` 为主线，在 Project-XX 物品/装备/容器 ownership 之上优先完成交互、搜刮、死亡丢失、撤离回写
4. 在 `R2` 末段补 1 个友方 NPC 与 1 个中立 NPC 样例，把阵营系统变成场景可见能力
