# Project-XX 执行任务清单

更新时间：`2026-04-14`

## 1. 使用说明

这份文档作为项目开发过程中的长期任务看板，状态与 [FpsExtractionRpg-DevelopmentRoadmap.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/FpsExtractionRpg-DevelopmentRoadmap.md) 保持同步。

状态约定：

- `[ ]` 待完成
- `[x]` 已完成

推荐使用方式：

1. 开始做某个任务时，在任务后追加 `（进行中）`
2. 完成后勾选为 `[x]`
3. 如果任务拆得还不够细，就在对应阶段下继续补子任务
4. 如果任务被阻塞，在文末 `阻塞记录` 中补充原因

## 2. 当前阶段判断

- `R0` 已完成
- `R1` 已完成
- 当前主线：`R2`
- 当前优先方向：`容器 -> 搜刮 -> 死亡丢失 -> 撤离回写`
- 当前已新增正式基础能力：`阵营与敌对关系框架`

## 3. 已完成项

### 3.1 规划与文档

- [x] T-DOC-001 导入 `JU TPS 3` 包并完成结构审计
- [x] T-DOC-002 完成 JUTPS 系统分析文档
- [x] T-DOC-003 完成项目初版策划案
- [x] T-DOC-004 完成项目初版技术设计
- [x] T-DOC-005 完成开发路线图
- [x] T-DOC-006 完成可执行任务清单初始化
- [x] T-DOC-007 完成基于第一人称、基地设施、生存值的文档二次修订
- [x] T-DOC-008 完成 `Akila + JUTPS` 双框架适配与裁剪方案
- [x] T-DOC-009 根据当前代码状态重写路线图、技术设计与开发者入口文档

### 3.2 工程接入

- [x] T-ENG-001 完成 JUTPS 包导入
- [x] T-ENG-002 完成导入后首轮粉材质修复
- [x] T-ENG-003 将项目渲染配置重新接回 URP 主配置

## 4. R0：项目基线恢复与接入稳定

### 4.1 项目设置与渲染基线

- [x] T-R0-001 梳理 `ProjectSettings.asset` 中被导包污染的字段并恢复项目基线
- [x] T-R0-002 核对 `applicationIdentifier`、公司名、产品 GUID、图形 API 等项目级配置
- [x] T-R0-003 核对 `GraphicsSettings` 与 `QualitySettings`，确认全部质量档使用项目 URP 资产
- [ ] T-R0-004 扫描 JUTPS 包内剩余异常材质，确认是否仍有少量残余粉材质
- [x] T-R0-005 验证主场景与关键 demo 场景在当前渲染管线下可打开

### 4.2 代码与数据目录骨架

- [x] T-R0-006 创建 `Assets/Res/Scripts/ProjectXX/Bootstrap`
- [x] T-R0-007 创建 `Assets/Res/Scripts/ProjectXX/Foundation`
- [x] T-R0-008 创建 `Assets/Res/Scripts/ProjectXX/Domain/Raid`
- [x] T-R0-009 创建 `Assets/Res/Scripts/ProjectXX/Domain/Meta`
- [x] T-R0-010 创建 `Assets/Res/Scripts/ProjectXX/Domain/Base`
- [x] T-R0-011 创建 `Assets/Res/Scripts/ProjectXX/Domain/Combat`
- [x] T-R0-012 创建 `Assets/Res/Scripts/ProjectXX/Services`
- [x] T-R0-013 创建 `Assets/Res/Scripts/ProjectXX/Infrastructure/Definitions`
- [x] T-R0-014 创建 `Assets/Res/Scripts/ProjectXX/Infrastructure/Save`
- [x] T-R0-015 创建 `Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework`
- [x] T-R0-016 创建 `Assets/Res/Scripts/ProjectXX/Bridges/JUTPS`
- [x] T-R0-017 创建 `Assets/Res/Scripts/ProjectXX/Presentation`
- [x] T-R0-018 创建 `Assets/Res/Data/Definitions/Items`
- [x] T-R0-019 创建 `Assets/Res/Data/Definitions/Equipment`
- [x] T-R0-020 创建 `Assets/Res/Data/Definitions/Buffs`
- [x] T-R0-021 创建 `Assets/Res/Data/Definitions/Skills`
- [x] T-R0-022 创建 `Assets/Res/Data/Definitions/Survival`
- [x] T-R0-023 创建 `Assets/Res/Data/Definitions/Facilities`
- [x] T-R0-024 创建 `Assets/Res/Data/Definitions/Recipes`
- [x] T-R0-025 创建 `Assets/Res/Data/Definitions/Enemies`
- [x] T-R0-026 创建 `Assets/Res/Data/Definitions/Bosses`
- [x] T-R0-027 创建 `Assets/Res/Data/Definitions/Maps`
- [x] T-R0-028 创建 `Assets/Res/Data/Definitions/Loot`
- [x] T-R0-029 创建 `Assets/Res/Data/Definitions/Merchants`
- [x] T-R0-030 创建 `Assets/Res/Data/Definitions/Quests`

### 4.3 Bootstrap 与开发入口

- [x] T-R0-031 创建 `Bootstrap` 场景或启动入口
- [x] T-R0-032 建立基础 `ProjectXXBootstrap` 脚本
- [x] T-R0-033 建立项目主命名空间约定
- [x] T-R0-034 建立基础日志与调试输出规范
- [x] T-R0-035 建立开发者入口文档并记录当前目录约定

### 4.4 R0 验收

- [x] T-R0-036 完成 `R0` 阶段回归检查
- [x] T-R0-037 在本清单中确认 `R0` 阶段完成

## 5. R1：第一人称战斗与最小可战斗切片

### 5.1 第一人称玩家与相机

- [x] T-R1-001 锁定正式玩法为第一人称
- [x] T-R1-002 锁定 `Akila FPS Framework` 为正式玩家底座
- [x] T-R1-003 创建 `ProjectXXFirstPersonViewBridge`
- [x] T-R1-004 创建第一人称武器与手臂展示层
- [x] T-R1-005 跑通第一人称交互射线与准星锚点
- [x] T-R1-006 在正式玩家 prefab 中隔离非正式第三人称展示逻辑
- [x] T-R1-007 在测试场景中停用 JUTPS 默认玩家链路与玩家 UI

### 5.2 玩家桥接与运行时

- [x] T-R1-008 创建 `ProjectXXPlayerFacade`
- [x] T-R1-009 创建 `ProjectXXAkilaPlayerBridge`
- [x] T-R1-010 创建 `ProjectXXCharacterStatBridge`
- [x] T-R1-011 创建 `ProjectXXCharacterBuffBridge`
- [x] T-R1-012 创建 `ProjectXXEquipmentBridge`
- [x] T-R1-013 创建 `ProjectXXDamageBridge`
- [x] T-R1-014 将桥接组件挂接到正式玩家 prefab
- [x] T-R1-015 创建 `RaidSessionRuntime`
- [x] T-R1-016 创建 `RaidPlayerRuntime`
- [x] T-R1-017 创建基础 `PlayerProfileRuntime`
- [x] T-R1-018 建立 raid 内玩家运行态初始化流程

### 5.3 武器、近战与伤害

- [x] T-R1-019 创建 `ProjectXXWeaponBridge`
- [x] T-R1-020 创建 `ProjectXXWeaponDurabilityBridge`
- [x] T-R1-021 创建 `ProjectXXAmmoResolver`
- [x] T-R1-022 创建 `ProjectXXMeleeBridge`
- [x] T-R1-023 创建 `MeleeHitResolver`
- [x] T-R1-024 创建 `MeleeStaggerService`
- [x] T-R1-025 跑通 `Akila -> JUTPS` 伤害链
- [x] T-R1-026 跑通 `JUTPS -> Akila` 伤害链

### 5.4 敌人与 JUTPS 桥接

- [x] T-R1-027 创建 1 个普通近战敌人原型定义
- [x] T-R1-028 创建 `JutpsHealthProxy`
- [x] T-R1-029 创建 `JutpsTargetAdapter`
- [x] T-R1-030 创建 `JutpsEnemyDamageableAdapter`
- [x] T-R1-031 创建 `JutpsEnemyBridge`
- [x] T-R1-032 配置敌人感知、追击、攻击与死亡回调

### 5.5 测试地图与最小闭环

- [x] T-R1-033 创建 `ProjectXX_RaidTestMap`
- [x] T-R1-034 放入玩家出生点
- [x] T-R1-035 放入敌人刷新点
- [x] T-R1-036 放入最小撤离点占位
- [x] T-R1-037 跑通第一人称移动、开火、近战、受伤、死亡闭环
- [x] T-R1-038 跑通 `JUTPS` 敌人发现并攻击玩家
- [x] T-R1-039 跑通玩家击杀 `JUTPS` 敌人
- [x] T-R1-040 接入 `ProjectXXRaidHudController`

### 5.6 R1 稳定化

- [x] T-R1-041 修复 Overlay Camera 覆盖 Main Camera 的 URP 相机栈问题
- [x] T-R1-042 修复 Akila 武器与手臂材质在 URP 下的兼容问题
- [x] T-R1-043 修复 JUTPS 敌人未贴地导致的悬空与慢速问题
- [x] T-R1-044 修复投射物命中层不同步导致的“开火但无法伤害敌人”问题
- [x] T-R1-045 修复敌人身上出现环境弹孔贴花的问题

### 5.7 阵营与敌对关系框架

- [x] T-R1-046 创建 `ProjectXXFaction`
- [x] T-R1-047 创建 `ProjectXXFactionMember`
- [x] T-R1-048 创建 `ProjectXXFactionUtility`
- [x] T-R1-049 创建 `ProjectXX.Domain.Combat.asmdef`
- [x] T-R1-050 把 Akila `Damageable` 接入统一阵营伤害许可
- [x] T-R1-051 把 JUTPS `JUHealth` 接入统一阵营伤害许可
- [x] T-R1-052 创建 `ProjectXXJutpsFactionTargetFilter`
- [x] T-R1-053 创建 `ProjectXXJutpsFactionBridge`
- [x] T-R1-054 把 JUTPS FOV 目标筛选接入 faction filter
- [x] T-R1-055 确认敌人之间不再互相伤害
- [x] T-R1-056 确认中立阵营会在受伤后对来源阵营进入敌对状态
- [x] T-R1-057 确认友方阵营默认可将 `Enemy` 作为有效敌对目标

### 5.8 R1 验收

- [x] T-R1-058 完成 `R1` 阶段回归检查
- [x] T-R1-059 在本清单中确认 `R1` 阶段完成

## 6. R2：容器、搜刮、死亡丢失与撤离回写

### 6.1 定义层

- [ ] T-R2-001 创建 `ItemDefinition`
- [ ] T-R2-002 创建 `WeaponDefinition`
- [ ] T-R2-003 创建 `MeleeWeaponDefinition`
- [ ] T-R2-004 创建 `ArmorDefinition`
- [ ] T-R2-005 创建 `ChestRigDefinition`
- [ ] T-R2-006 创建 `BackpackDefinition`
- [ ] T-R2-007 创建 `EquipmentSlotDefinition`
- [ ] T-R2-008 创建 `ContainerDefinition`

### 6.2 运行时容器系统

- [ ] T-R2-009 创建 `ItemInstanceRuntime`
- [ ] T-R2-010 创建 `GridSize`
- [ ] T-R2-011 创建 `GridCoord`
- [ ] T-R2-012 创建 `InventoryGridRuntime`
- [ ] T-R2-013 创建 `ContainerRuntime`
- [ ] T-R2-014 创建 `EquipmentRuntime`
- [ ] T-R2-015 创建 `LoadoutRuntime`

### 6.3 交互与搜刮

- [ ] T-R2-016 创建 `ProjectXXInteractableBridge`
- [ ] T-R2-017 创建 `InteractionPromptPresenter`
- [ ] T-R2-018 创建 `InteractionRequirementEvaluator`
- [ ] T-R2-019 在 `ProjectXX_RaidTestMap` 中放入第一个正式容器对象
- [ ] T-R2-020 跑通“发现 -> 打开 -> 转移 -> 关闭”的最小搜刮流程

### 6.4 容器规则与转移服务

- [ ] T-R2-021 实现物品尺寸占地校验
- [ ] T-R2-022 实现物品旋转校验
- [ ] T-R2-023 实现容器允许物品类型校验
- [ ] T-R2-024 实现胸甲/胸挂装备合法性校验
- [ ] T-R2-025 实现负重汇总计算
- [ ] T-R2-026 创建 `InventoryService`
- [ ] T-R2-027 创建 `ContainerTransferService`
- [ ] T-R2-028 实现容器内放置
- [ ] T-R2-029 实现容器间拖拽转移
- [ ] T-R2-030 实现容器间交换
- [ ] T-R2-031 实现失败回滚与错误提示

### 6.5 UI 第一版

- [ ] T-R2-032 创建背包窗口 prefab
- [ ] T-R2-033 创建容器窗口 prefab
- [ ] T-R2-034 创建物品格模板 prefab
- [ ] T-R2-035 创建 `InventoryWindowController`
- [ ] T-R2-036 创建 `InventoryWindowTemplate`
- [ ] T-R2-037 跑通拖拽、放置、交换的 UI 刷新

### 6.6 死亡与撤离

- [ ] T-R2-038 创建 `DeathLossService`
- [ ] T-R2-039 实现死亡丢失与保留分类
- [ ] T-R2-040 实现背包/胸挂转尸体容器
- [ ] T-R2-041 创建 `ExtractionService`
- [ ] T-R2-042 创建 `RaidResultService`
- [ ] T-R2-043 实现撤离成功回写局外

### 6.7 阵营样例内容补齐

- [ ] T-R2-044 在测试图中加入 1 个友方 NPC 样例
- [ ] T-R2-045 在测试图中加入 1 个中立 NPC 样例
- [ ] T-R2-046 跑通“友方主动攻击敌人”的场景级验证
- [ ] T-R2-047 跑通“中立受击后反击来源阵营”的场景级验证

### 6.8 R2 验收

- [ ] T-R2-048 完成 `R2` 阶段回归检查
- [ ] T-R2-049 在本清单中确认 `R2` 阶段完成

## 7. R3：BaseHub、仓库、设施与商人闭环

### 7.1 Profile 与基地运行时

- [ ] T-R3-001 创建 `ProfileService`
- [ ] T-R3-002 完善 `PlayerProfileRuntime`
- [ ] T-R3-003 创建 `BaseHubRuntimeState`
- [ ] T-R3-004 创建 `FacilityRuntimeState`
- [ ] T-R3-005 创建 `MerchantRuntimeState`
- [ ] T-R3-006 创建 `QuestRuntimeState`

### 7.2 BaseHub 场景与局外循环

- [ ] T-R3-007 创建 `BaseHub` 场景
- [ ] T-R3-008 创建基地态玩家出生点
- [ ] T-R3-009 创建 `ProjectXXBaseHubPlayerBridge`
- [ ] T-R3-010 跑通基地第一人称移动与交互
- [ ] T-R3-011 放入仓库区、建造区、商人区的基础交互点
- [ ] T-R3-012 创建局外仓库容器运行时
- [ ] T-R3-013 建立 raid 结果并入仓库流程
- [ ] T-R3-014 建立局外配装保存流程
- [ ] T-R3-015 创建仓库窗口
- [ ] T-R3-016 创建配装窗口

### 7.3 设施、商人与任务

- [ ] T-R3-017 创建 `FacilityDefinition`
- [ ] T-R3-018 创建 `FacilityLevelDefinition`
- [ ] T-R3-019 创建 `FacilityRequirementDefinition`
- [ ] T-R3-020 创建 `FacilityBuildRecipeDefinition`
- [ ] T-R3-021 创建 `FacilityService`
- [ ] T-R3-022 创建 `FacilityConstructionService`
- [ ] T-R3-023 跑通建造位交互与设施解锁流程
- [ ] T-R3-024 跑通设施升级后的状态刷新
- [ ] T-R3-025 创建 `MerchantDefinition`
- [ ] T-R3-026 创建 `MerchantMoveInRequirementDefinition`
- [ ] T-R3-027 创建 `MerchantService`
- [ ] T-R3-028 创建第一位商人的库存与价格规则
- [ ] T-R3-029 跑通商人入住条件判断
- [ ] T-R3-030 创建商人 UI 第一版
- [ ] T-R3-031 创建 `QuestDefinition`
- [ ] T-R3-032 创建 `QuestObjectiveDefinition`
- [ ] T-R3-033 创建 `QuestService`
- [ ] T-R3-034 跑通接取、推进、提交的最小闭环
- [ ] T-R3-035 创建任务日志窗口

### 7.4 R3 验收

- [ ] T-R3-036 完成 `R3` 阶段回归检查
- [ ] T-R3-037 在本清单中确认 `R3` 阶段完成

## 8. R4：敌人扩展、精英、Boss 与派系化遭遇

### 8.1 敌人与精英

- [ ] T-R4-001 创建 `EnemyArchetypeDefinition`
- [ ] T-R4-002 创建 `RaidEnemyRuntime`
- [ ] T-R4-003 创建 `EliteModifierDefinition`
- [ ] T-R4-004 创建 `EliteModifierRuntime`
- [ ] T-R4-005 创建 `EliteModifierService`
- [ ] T-R4-006 跑通普通怪转精英怪流程

### 8.2 Boss 与遭遇

- [ ] T-R4-007 创建 `BossDefinition`
- [ ] T-R4-008 创建 `BossPhaseDefinition`
- [ ] T-R4-009 创建 `RaidBossRuntime`
- [ ] T-R4-010 创建 `BossMechanicController`
- [ ] T-R4-011 跑通至少 2 阶段 Boss
- [ ] T-R4-012 配置 Boss 专属掉落
- [ ] T-R4-013 创建 `EnemySpawnService`
- [ ] T-R4-014 创建 `RaidEventService`
- [ ] T-R4-015 创建第一版遭遇点配置
- [ ] T-R4-016 创建第一版 Boss 房或 Boss 事件入口

### 8.3 派系化 NPC 遭遇

- [ ] T-R4-017 扩展 faction 框架以支持更复杂的 NPC 遭遇
- [ ] T-R4-018 增加派系营地、巡逻与援助响应
- [ ] T-R4-019 建立友方、中立、敌对 NPC 的内容生产规范

### 8.4 R4 验收

- [ ] T-R4-020 完成 `R4` 阶段回归检查
- [ ] T-R4-021 在本清单中确认 `R4` 阶段完成

## 9. R5：成长、生存、技能、Buff 与异常

### 9.1 成长与修正

- [ ] T-R5-001 创建 `AttributeDefinition`
- [ ] T-R5-002 创建 `SkillDefinition`
- [ ] T-R5-003 创建 `BuffDefinition`
- [ ] T-R5-004 创建 `AffixDefinition`
- [ ] T-R5-005 创建 `ModifierResolver`
- [ ] T-R5-006 跑通属性修正到局内角色表现

### 9.2 生存值系统

- [ ] T-R5-007 创建 `SurvivalStatDefinition`
- [ ] T-R5-008 创建 `SurvivalThresholdDefinition`
- [ ] T-R5-009 创建 `SurvivalRuntimeState`
- [ ] T-R5-010 创建 `SurvivalService`
- [ ] T-R5-011 实现 raid 中按时间衰减
- [ ] T-R5-012 实现基地中不衰减且不自动恢复
- [ ] T-R5-013 实现阈值 Debuff 判定
- [ ] T-R5-014 实现数值归零时的局内持续掉血

### 9.3 恢复、休息、制作与异常

- [ ] T-R5-015 创建 `ConsumableDefinition`
- [ ] T-R5-016 创建 `RecipeDefinition`
- [ ] T-R5-017 创建 `ConsumptionService`
- [ ] T-R5-018 创建 `RecipeCraftingService`
- [ ] T-R5-019 创建 `RestService`
- [ ] T-R5-020 实现吃饭恢复饱食度
- [ ] T-R5-021 实现喝水恢复饮水度
- [ ] T-R5-022 实现休息恢复疲劳值
- [ ] T-R5-023 实现休息间清理疲劳类 Debuff
- [ ] T-R5-024 实现“下一局 Buff”写入与生效
- [ ] T-R5-025 创建 `StatusEffectDefinition`
- [ ] T-R5-026 创建 `MutationDefinition`
- [ ] T-R5-027 创建 `BuffService`
- [ ] T-R5-028 跑通伤病、污染、恐惧等状态效果
- [ ] T-R5-029 创建 `RelicDefinition`
- [ ] T-R5-030 实现遗物槽位逻辑
- [ ] T-R5-031 实现装备词缀系统
- [ ] T-R5-032 让词缀和遗物影响 Build 选择

### 9.4 UI 与表现

- [ ] T-R5-033 创建生存值 HUD
- [ ] T-R5-034 创建角色成长窗口
- [ ] T-R5-035 创建配方制作窗口
- [ ] T-R5-036 刷新基地设施窗口中的恢复与制作反馈

### 9.5 R5 验收

- [ ] T-R5-037 完成 `R5` 阶段回归检查
- [ ] T-R5-038 在本清单中确认 `R5` 阶段完成

## 10. R6：内容扩展、平衡与产品化

### 10.1 内容扩展

- [ ] T-R6-001 扩展到第 2 张地图
- [ ] T-R6-002 扩展到第 3 张地图
- [ ] T-R6-003 扩展更多地图专属掉落
- [ ] T-R6-004 扩展第 2 名商人
- [ ] T-R6-005 扩展更多任务链
- [ ] T-R6-006 扩展更多基地设施
- [ ] T-R6-007 扩展更多精英词缀
- [ ] T-R6-008 扩展更多 Boss

### 10.2 平衡与产品化

- [ ] T-R6-009 调整掉率与经济平衡
- [ ] T-R6-010 调整耐久与修理成本
- [ ] T-R6-011 调整容器压力与负重曲线
- [ ] T-R6-012 调整生存值衰减与恢复节奏
- [ ] T-R6-013 优化 UI 易用性
- [ ] T-R6-014 优化存档稳定性与加载流程
- [ ] T-R6-015 建立固定回归清单

### 10.3 R6 验收

- [ ] T-R6-016 完成 `R6` 阶段回归检查
- [ ] T-R6-017 在本清单中确认 `R6` 阶段完成

## 11. 当前推荐下一任务

- [ ] T-NEXT-001 开始 `R2` 的 `ItemDefinition` / `ContainerDefinition` / `InventoryGridRuntime`
- [ ] T-NEXT-002 在 `ProjectXX_RaidTestMap` 中接入第一个正式容器与 Project-XX 交互提示
- [ ] T-NEXT-003 补 `DeathLossService` / `ExtractionService` / `RaidResultService`
- [ ] T-NEXT-004 在测试图中加入 1 个友方 NPC 与 1 个中立 NPC 样例，验证 faction 框架的内容层表现
- [ ] T-NEXT-005 为 `RaidTestMap` 增加 PlayMode 自动化回归

## 12. 阻塞记录

- 暂无

## 13. 变更记录

- 2026-04-09：初始化执行任务清单
- 2026-04-09：根据第一人称、BaseHub、设施和生存值设计重构清单
- 2026-04-11：根据 `Akila FPS Framework` 作为玩家底座的决策修订清单
- 2026-04-14：完成 R0 基线恢复、R1 战斗切片与 `Akila + JUTPS` 基础战斗闭环
- 2026-04-14：补充 R1 稳定化项与阵营/敌对关系框架任务线，并同步到最新路线图
