# Project-XX 执行任务清单

## 1. 使用说明

这份文档用于作为项目开发过程中的长期任务看板。

状态约定：

- `[ ]` 待完成
- `[x]` 已完成

推荐使用方式：

1. 开始做某个任务时，在任务后面追加 `（进行中）`
2. 完成后勾选为 `[x]`
3. 如果任务拆得还不够细，就在对应小节下继续补子任务
4. 如果某个任务被阻塞，在文末 `阻塞记录` 中补充原因

任务命名规则：

- `T-R0-001` 表示第 0 阶段的第 1 个任务
- `T-R3-015` 表示第 3 阶段的第 15 个任务

## 2. 当前建议起步顺序

如果现在开始正式进入开发，建议按这个顺序推进：

1. `R0` 项目基线恢复与目录骨架
2. `R1` 第一人称角色、战斗、普通怪垂直切片
3. `R2` 格子背包、容器、死亡丢失、撤离回写
4. `R3` BaseHub、仓库、设施与首位商人闭环
5. `R4` 精英怪、远程怪与 Boss 基础遭遇
6. `R5` 生存值、恢复、Buff 与成长系统

## 3. 已完成项

### 规划与分析

- [x] T-DOC-001 导入 `JU TPS 3` 包并完成结构审计
- [x] T-DOC-002 完成 JUTPS 系统分析文档
- [x] T-DOC-003 完成项目初版策划案
- [x] T-DOC-004 完成项目初版技术设计
- [x] T-DOC-005 完成开发路线图
- [x] T-DOC-006 完成可执行任务清单初始化
- [x] T-DOC-007 完成基于第一人称、基地设施、生存值的文档二次修订
- [x] T-DOC-008 完成 `Akila + JUTPS` 双框架适配与裁剪方案

### 工程接入

- [x] T-ENG-001 完成 JUTPS 包导入
- [x] T-ENG-002 完成导入后首轮粉材质修复
- [x] T-ENG-003 将项目渲染配置重新接回 URP 主配置

## 4. R0：项目基线恢复与接入稳定

### 4.1 项目设置清理

- [ ] T-R0-001 梳理 `ProjectSettings.asset` 中被 JUTPS 导包污染的字段，并恢复项目基线
- [ ] T-R0-002 核对 `applicationIdentifier`、公司名、产品 GUID、图形 API 等项目级配置
- [ ] T-R0-003 核对 `GraphicsSettings` 与 `QualitySettings`，确认全部质量档使用项目自己的 URP 资产
- [ ] T-R0-004 扫描 JUTPS 包内剩余异常材质，确认是否还有少量残余粉材质
- [ ] T-R0-005 验证主场景与关键 demo 场景在当前渲染管线下都可打开

### 4.2 代码目录骨架

- [ ] T-R0-006 创建 `Assets/Res/Scripts/ProjectXX/Bootstrap`
- [ ] T-R0-007 创建 `Assets/Res/Scripts/ProjectXX/Foundation`
- [ ] T-R0-008 创建 `Assets/Res/Scripts/ProjectXX/Domain/Common`
- [ ] T-R0-009 创建 `Assets/Res/Scripts/ProjectXX/Domain/Raid`
- [ ] T-R0-010 创建 `Assets/Res/Scripts/ProjectXX/Domain/Base`
- [ ] T-R0-011 创建 `Assets/Res/Scripts/ProjectXX/Domain/Meta`
- [ ] T-R0-012 创建 `Assets/Res/Scripts/ProjectXX/Services`
- [ ] T-R0-013 创建 `Assets/Res/Scripts/ProjectXX/Infrastructure/Definitions`
- [ ] T-R0-014 创建 `Assets/Res/Scripts/ProjectXX/Infrastructure/Save`
- [ ] T-R0-015 创建 `Assets/Res/Scripts/ProjectXX/Bridges/JUTPS`
- [ ] T-R0-016 创建 `Assets/Res/Scripts/ProjectXX/Presentation`
- [ ] T-R0-017 创建 `Assets/Res/Scripts/ProjectXX/Presentation/BaseHub`

完成标准：

- 目录结构与技术设计文档一致
- 新代码不再散落在临时目录中

### 4.3 数据目录骨架

- [ ] T-R0-018 创建 `Assets/Res/Data/Definitions/Items`
- [ ] T-R0-019 创建 `Assets/Res/Data/Definitions/Equipment`
- [ ] T-R0-020 创建 `Assets/Res/Data/Definitions/Buffs`
- [ ] T-R0-021 创建 `Assets/Res/Data/Definitions/Skills`
- [ ] T-R0-022 创建 `Assets/Res/Data/Definitions/Survival`
- [ ] T-R0-023 创建 `Assets/Res/Data/Definitions/Facilities`
- [ ] T-R0-024 创建 `Assets/Res/Data/Definitions/Recipes`
- [ ] T-R0-025 创建 `Assets/Res/Data/Definitions/Enemies`
- [ ] T-R0-026 创建 `Assets/Res/Data/Definitions/Bosses`
- [ ] T-R0-027 创建 `Assets/Res/Data/Definitions/Maps`
- [ ] T-R0-028 创建 `Assets/Res/Data/Definitions/Loot`
- [ ] T-R0-029 创建 `Assets/Res/Data/Definitions/Merchants`
- [ ] T-R0-030 创建 `Assets/Res/Data/Definitions/Quests`

### 4.4 Bootstrap 与基础入口

- [ ] T-R0-031 创建 `Bootstrap` 场景或启动入口
- [ ] T-R0-032 建立基础 `ProjectXXBootstrap` 脚本
- [ ] T-R0-033 建立项目主命名空间约定
- [ ] T-R0-034 建立基础日志与调试输出规范
- [ ] T-R0-035 建立开发者入口说明，记录当前目录约定

### 4.5 R0 验收

- [ ] T-R0-036 完成 `R0` 阶段回归检查
- [ ] T-R0-037 在本清单中勾选 `R0` 阶段完成

## 5. R1：第一人称战斗与角色垂直切片

### 5.1 第一人称控制与相机

- [ ] T-R1-001 锁定正式玩法为第一人称，不再保留 TPS 作为主玩法分支
- [ ] T-R1-001A 锁定 `Akila FPS Framework` 为正式玩家操作底座
- [ ] T-R1-002 创建 `ProjectXXFirstPersonViewBridge`
- [ ] T-R1-003 创建第一人称武器与手臂展示层
- [ ] T-R1-004 跑通第一人称交互射线与准星锚点
- [ ] T-R1-005 在正式玩家 prefab 中关闭或隔离非正式第三人称展示逻辑
- [ ] T-R1-005A 在测试场景中停用 `JUTPS` 玩家链与玩家 UI

### 5.2 玩家角色桥接

- [ ] T-R1-006 创建 `ProjectXXPlayerFacade`
- [ ] T-R1-006A 创建 `ProjectXXAkilaPlayerBridge`
- [ ] T-R1-007 创建 `ProjectXXCharacterStatBridge`
- [ ] T-R1-008 创建 `ProjectXXCharacterBuffBridge`
- [ ] T-R1-009 创建 `ProjectXXEquipmentBridge`
- [ ] T-R1-010 创建 `ProjectXXDamageBridge`
- [ ] T-R1-011 将桥接组件挂接到正式 `Akila` 玩家 prefab
- [ ] T-R1-011A 创建 `JutpsHealthProxy`
- [ ] T-R1-011B 创建 `JutpsTargetAdapter`

### 5.3 基础运行时对象

- [ ] T-R1-012 创建 `RaidSessionRuntime`
- [ ] T-R1-013 创建 `RaidPlayerRuntime`
- [ ] T-R1-014 创建基础 `PlayerProfileRuntime`
- [ ] T-R1-015 建立 raid 内玩家运行态初始化流程

### 5.4 武器与近战

- [ ] T-R1-016 创建 `ProjectXXWeaponBridge`
- [ ] T-R1-017 创建 `ProjectXXWeaponDurabilityBridge`
- [ ] T-R1-018 创建 `ProjectXXAmmoResolver`
- [ ] T-R1-019 创建 `ProjectXXMeleeBridge`
- [ ] T-R1-020 创建 `MeleeHitResolver`
- [ ] T-R1-021 创建 `MeleeStaggerService`

### 5.5 普通敌人垂直切片

- [ ] T-R1-022 创建 1 个普通近战敌人原型定义
- [ ] T-R1-023 创建普通敌人的最小运行时桥接
- [ ] T-R1-024 配置其感知、追击、攻击流程
- [ ] T-R1-025 建立简单的敌人死亡与掉落回调

### 5.6 最小测试场景

- [ ] T-R1-026 创建 1 张最小测试 raid 场景
- [ ] T-R1-027 场景中放入玩家出生点
- [ ] T-R1-028 场景中放入普通敌人刷新点
- [ ] T-R1-029 场景中放入简单撤离点占位
- [ ] T-R1-030 跑通第一人称移动、开火、近战、受伤、死亡闭环
- [ ] T-R1-030A 跑通 `JUTPS` 敌人发现并攻击 `Akila` 玩家
- [ ] T-R1-030B 跑通 `Akila` 玩家击杀 `JUTPS` 敌人

### 5.7 R1 验收

- [ ] T-R1-031 完成 `R1` 阶段回归检查
- [ ] T-R1-032 在本清单中勾选 `R1` 阶段完成

## 6. R2：格子背包与搜刮撤离闭环

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

### 6.3 容器规则

- [ ] T-R2-016 实现物品尺寸占地校验
- [ ] T-R2-017 实现物品旋转校验
- [ ] T-R2-018 实现容器允许物品类型校验
- [ ] T-R2-019 实现胸甲/胸挂装备合法性校验
- [ ] T-R2-020 实现负重汇总计算
- [ ] T-R2-021 实现整体护甲值扣减逻辑

### 6.4 容器转移服务

- [ ] T-R2-022 创建 `InventoryService`
- [ ] T-R2-023 创建 `ContainerTransferService`
- [ ] T-R2-024 实现容器内放置
- [ ] T-R2-025 实现容器间拖拽转移
- [ ] T-R2-026 实现容器间交换
- [ ] T-R2-027 实现失败回滚与错误提示

### 6.5 UI 第一版

- [ ] T-R2-028 创建背包窗口 prefab
- [ ] T-R2-029 创建容器窗口 prefab
- [ ] T-R2-030 创建物品格子模板 prefab
- [ ] T-R2-031 创建 `InventoryWindowController`
- [ ] T-R2-032 创建 `InventoryWindowTemplate`
- [ ] T-R2-033 跑通拖拽、放置、交换的 UI 刷新

### 6.6 死亡与撤离

- [ ] T-R2-034 创建 `DeathLossService`
- [ ] T-R2-035 实现死亡丢失与保留分类
- [ ] T-R2-036 实现背包/胸挂转尸体容器
- [ ] T-R2-037 创建 `ExtractionService`
- [ ] T-R2-038 创建 `RaidResultService`
- [ ] T-R2-039 实现撤离成功回写局外

### 6.7 R2 验收

- [ ] T-R2-040 完成 `R2` 阶段回归检查
- [ ] T-R2-041 在本清单中勾选 `R2` 阶段完成

## 7. R3：BaseHub、仓库、设施与商人闭环

### 7.1 Profile 与基地运行时

- [ ] T-R3-001 创建 `ProfileService`
- [ ] T-R3-002 完善 `PlayerProfileRuntime`
- [ ] T-R3-003 创建 `BaseHubRuntimeState`
- [ ] T-R3-004 创建 `FacilityRuntimeState`
- [ ] T-R3-005 创建 `MerchantRuntimeState`
- [ ] T-R3-006 创建 `QuestRuntimeState`

### 7.2 BaseHub 场景与基地角色

- [ ] T-R3-007 创建 `BaseHub` 场景
- [ ] T-R3-008 创建基地态玩家出生点
- [ ] T-R3-009 创建 `ProjectXXBaseHubPlayerBridge`
- [ ] T-R3-010 跑通基地第一人称移动与交互
- [ ] T-R3-011 放入仓库区、建造区、商人区的基础交互点

### 7.3 局外仓库与配装

- [ ] T-R3-012 创建局外仓库容器运行时
- [ ] T-R3-013 建立 raid 结果并入仓库流程
- [ ] T-R3-014 建立局外配装保存流程
- [ ] T-R3-015 创建仓库窗口
- [ ] T-R3-016 创建配装窗口

### 7.4 设施定义与建造

- [ ] T-R3-017 创建 `FacilityDefinition`
- [ ] T-R3-018 创建 `FacilityLevelDefinition`
- [ ] T-R3-019 创建 `FacilityRequirementDefinition`
- [ ] T-R3-020 创建 `FacilityBuildRecipeDefinition`
- [ ] T-R3-021 创建 `FacilityService`
- [ ] T-R3-022 创建 `FacilityConstructionService`
- [ ] T-R3-023 跑通建造位交互与设施解锁流程
- [ ] T-R3-024 跑通设施升级后的状态刷新

### 7.5 休息间与厨房第一版

- [ ] T-R3-025 创建休息间设施定义与场景占位
- [ ] T-R3-026 创建厨房设施定义与场景占位
- [ ] T-R3-027 打通休息间基础交互入口
- [ ] T-R3-028 打通厨房基础交互入口
- [ ] T-R3-029 创建设施窗口 prefab 与模板

### 7.6 商人与任务系统

- [ ] T-R3-030 创建 `MerchantDefinition`
- [ ] T-R3-031 创建 `MerchantMoveInRequirementDefinition`
- [ ] T-R3-032 创建 `MerchantService`
- [ ] T-R3-033 创建第一位商人的库存与价格规则
- [ ] T-R3-034 跑通商人入住条件判断
- [ ] T-R3-035 创建商人 UI 第一版
- [ ] T-R3-036 创建 `QuestDefinition`
- [ ] T-R3-037 创建 `QuestObjectiveDefinition`
- [ ] T-R3-038 创建 `QuestService`
- [ ] T-R3-039 跑通接取、推进、提交的最小闭环
- [ ] T-R3-040 创建任务日志窗口

### 7.7 R3 验收

- [ ] T-R3-041 完成 `R3` 阶段回归检查
- [ ] T-R3-042 在本清单中勾选 `R3` 阶段完成

## 8. R4：敌人、精英与 Boss 系统

### 8.1 敌人与精英

- [ ] T-R4-001 创建 `EnemyArchetypeDefinition`
- [ ] T-R4-002 创建 `RaidEnemyRuntime`
- [ ] T-R4-003 创建 `EliteModifierDefinition`
- [ ] T-R4-004 创建 `EliteModifierRuntime`
- [ ] T-R4-005 创建 `EliteModifierService`
- [ ] T-R4-006 跑通普通怪转精英怪流程

### 8.2 Boss

- [ ] T-R4-007 创建 `BossDefinition`
- [ ] T-R4-008 创建 `BossPhaseDefinition`
- [ ] T-R4-009 创建 `RaidBossRuntime`
- [ ] T-R4-010 创建 `BossMechanicController`
- [ ] T-R4-011 跑通至少 2 阶段 Boss
- [ ] T-R4-012 配置 Boss 专属掉落

### 8.3 遭遇与刷怪

- [ ] T-R4-013 创建 `EnemySpawnService`
- [ ] T-R4-014 创建 `RaidEventService`
- [ ] T-R4-015 创建第一版遭遇点配置
- [ ] T-R4-016 创建第一版 Boss 房或 Boss 事件入口

### 8.4 R4 验收

- [ ] T-R4-017 完成 `R4` 阶段回归检查
- [ ] T-R4-018 在本清单中勾选 `R4` 阶段完成

## 9. R5：成长、生存、技能、Buff 与遗物

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

### 9.3 恢复、休息与制作

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

### 9.4 Buff / Debuff / 异常

- [ ] T-R5-025 创建 `StatusEffectDefinition`
- [ ] T-R5-026 创建 `MutationDefinition`
- [ ] T-R5-027 创建 `BuffService`
- [ ] T-R5-028 跑通伤病、污染、恐惧等状态效果

### 9.5 遗物与词缀

- [ ] T-R5-029 创建 `RelicDefinition`
- [ ] T-R5-030 实现遗物槽位逻辑
- [ ] T-R5-031 实现装备词缀系统
- [ ] T-R5-032 让词缀和遗物影响 Build 选择

### 9.6 UI 与表现

- [ ] T-R5-033 创建生存值 HUD
- [ ] T-R5-034 创建角色成长窗口
- [ ] T-R5-035 创建配方制作窗口
- [ ] T-R5-036 刷新基地设施窗口中的恢复与制作反馈

### 9.7 R5 验收

- [ ] T-R5-037 完成 `R5` 阶段回归检查
- [ ] T-R5-038 在本清单中勾选 `R5` 阶段完成

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
- [ ] T-R6-010 调整耐久和修理成本
- [ ] T-R6-011 调整容器压力与负重曲线
- [ ] T-R6-012 调整生存值衰减与恢复节奏
- [ ] T-R6-013 优化 UI 易用性
- [ ] T-R6-014 优化存档稳定性与加载流程
- [ ] T-R6-015 建立固定回归清单

### 10.3 R6 验收

- [ ] T-R6-016 完成 `R6` 阶段回归检查
- [ ] T-R6-017 在本清单中勾选 `R6` 阶段完成

## 11. 当前推荐下一任务

建议我们从这里开始：

- [ ] T-NEXT-001 处理 `R0` 项目设置污染并固化 URP 基线
- [ ] T-NEXT-002 建立 `Assets/Res/Scripts/ProjectXX/` 与 `Assets/Res/Data/Definitions/` 目录骨架
- [ ] T-NEXT-003 创建最小 `Bootstrap` 入口
- [ ] T-NEXT-004 开始 `R1` 的 `Akila` 玩家桥与 `JUTPS` 世界兼容桥
- [ ] T-NEXT-005 创建最小 raid 测试场景

## 12. 阻塞记录

- 暂无

## 13. 变更记录

- 2026-04-09：初始化执行任务清单
- 2026-04-09：根据第一人称、BaseHub、设施和生存值设计重构清单
- 2026-04-11：根据 `Akila FPS Framework` 作为玩家底座的决策修订清单
