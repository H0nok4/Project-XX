# Project-XX 开发路线图

## 1. 路线图目标

这份路线图基于当前四份核心文档：

- `FpsExtractionRpg-InitialGDD.md`
- `FpsExtractionRpg-TechnicalDesign.md`
- `FpsExtractionRpg-ExecutionChecklist.md`
- `Docs/FPSFramework/FPSFramework-And-JUTPS-IntegrationGuide.md`

目标不是给出精确排期，而是定义：

- 正确的开发顺序
- 每个阶段的目标
- 每个阶段的交付物
- 每个阶段的通过标准
- 哪些系统必须先做出闭环，哪些系统可以后扩

## 2. 总体策略

开发顺序建议遵循五条原则。

### 原则 1：先锁定框架边界，再扩玩法

在当前方案里，必须先明确：

- `FPS Framework` 负责玩家侧第一人称控制、观察、枪械手感与武器展示
- `JUTPS` 负责敌人 AI、载具与部分世界能力原型
- `Project-XX` 负责规则、数据、UI、Meta、仓库、任务、商人和存档

如果不先把这条边界锁住，后续很容易出现双控制器、双输入、双 HUD 和双交互系统互相打架。

### 原则 2：先锁定第一人称底座，再扩玩法

本项目是严格第一人称游戏，所以相机、手感、交互射线、武器展示、近战反馈必须尽早定型。不要在 TPS/TPS-FPS 混合状态下长期开发。

### 原则 3：先打通闭环，再堆内容

先证明“基地准备、进图、搜刮、战斗、死亡/撤离、返回基地、恢复与再出击”成立，再去堆地图、敌人、商人和配方数量。

### 原则 4：先做规则底座，再做表现扩展

先把格子容器、死亡丢失、设施解锁、生存值、商人入住条件这些硬规则做稳，再上复杂 UI、大量 VFX 或大规模内容。

### 原则 5：Raid 与 BaseHub 要成对建设

这不是“局内做完后再补一点局外菜单”的项目。基地、设施、商人、生存恢复本身就是核心体验的一半，必须和 raid 系统同步推进。

## 3. 里程碑总览

建议按 7 个阶段推进。

- `R0` 项目基线恢复、双框架接入稳定与边界确认
- `R1` 联合测试场景与玩家战斗垂直切片
- `R2` 交互、载具与搜刮撤离闭环
- `R3` BaseHub、仓库、设施与商人闭环
- `R4` 敌人、精英与 Boss 系统
- `R5` 成长、生存、技能、Buff 与遗物
- `R6` 内容扩展、平衡与产品化

## 4. R0：项目基线恢复、双框架接入稳定与边界确认

## 4.1 目标

把当前项目从“已导入 JUTPS / FPS Framework 但边界、输入和运行时职责仍不稳定”的状态，收敛到一个可持续开发的稳定基线。

## 4.2 核心工作

- 清理 JUTPS 与 FPS Framework 导入带来的项目设置污染
- 稳定渲染管线与材质兼容
- 明确玩家使用 `FPS Framework`、敌人/车辆使用 `JUTPS`
- 禁用或移除玩家侧重复控制器、重复输入、重复 HUD 与重复交互接管
- 确定项目主代码目录与命名规范
- 搭建 `Bridges/FPSFramework` 与 `Bridges/JUTPS` 目录骨架
- 搭建 `ProjectXX` 基础代码目录
- 建立基础 Bootstrap 流程
- 建立 definitions 与 save 的基础落位

## 4.3 交付物

- 可稳定打开的工程
- 稳定的 URP 配置
- `Assets/Res/Scripts/ProjectXX/` 目录骨架
- `Assets/Res/Data/Definitions/` 目录骨架
- 玩家 / 敌人 / 车辆 / 交互的接入边界清单
- `Bootstrap` 场景或初始化入口

## 4.4 通过标准

- 工程可正常打开、编译
- 主要 demo 与项目自有场景不再出现成片粉材质
- 玩家根物体上不存在 `FPS Framework` 与 `JUTPS` 双控制器并存
- `FPS Framework`、`JUTPS` 与 `Project-XX` 的代码与运行时职责分层明确

## 5. R1：联合测试场景与玩家战斗垂直切片

## 5.1 目标

在单张联合测试地图中跑通最基础的“`FPSF Player` 进入场景、移动、瞄准、开火、受伤、死亡，并能稳定作用于 `JUTPS Enemy`”闭环。

## 5.2 核心工作

- 锁定正式 `FPS Framework` 玩家预制体、第一人称相机与输入模式
- 搭建 `RaidPlayerControllerFacade`、`RaidPlayerViewBridge`、`RaidInputContext`
- 搭建 `ProjectXXFirearmBridge`、`WeaponRuntimeToPresetMapper`、`WeaponModifierApplier`
- 搭建 `RaidHealthBridge` 与 `DamageProtocolBridge`
- 建立 `RaidSessionRuntime` 与 `RaidPlayerRuntime`
- 做一张最小测试地图
- 做 1 种 `JUTPS` 普通近战敌人
- 做 1 个最小 Project-XX 交互点

## 5.3 交付物

- 基于 `FPS Framework` 的基础玩家角色
- 第一人称相机、武器展示层与输入域
- `Project-XX HUD`
- 主武器 / 副武器基础流程与伤害桥
- 玩家生命、受击、死亡
- 普通敌人追击与攻击
- 最小可用交互点

## 5.4 通过标准

- 能正常第一人称移动、瞄准、开火
- 能被敌人击杀
- 能对 `JUTPS` 敌人稳定造成伤害
- 敌人基本行为稳定
- HUD 与交互射线不依赖任一旧包 UI
- 玩家输入、相机和武器展示不再被 `JUTPS` 抢占

## 6. R2：交互、载具与搜刮撤离闭环

## 6.1 目标

跑通“交互发现、搜刮容器、格子放置、死亡丢失、撤离带回，以及玩家上下 `JUTPS` 载具”的核心闭环。

## 6.2 核心工作

- 实现 `ProjectXXInteractableBridge`、`InteractionPromptPresenter`、`InteractionRequirementEvaluator`
- 实现 `JutpsVehicleBridge` 与玩家上下车输入域切换
- 实现 `ItemDefinition`、`ItemInstanceRuntime`
- 实现 `InventoryGridRuntime`、`ContainerRuntime`
- 实现背包、胸挂、防护胸挂容器规则
- 实现物品尺寸与旋转
- 实现容器 UI 第一版
- 实现 `DeathLossService`
- 实现 `ExtractionService` 与 `RaidResultService`

## 6.3 交付物

- 格子背包窗口
- 地图容器窗口
- 玩家装备栏位窗口
- 撤离点与最小权限校验交互
- 1 个 `JUTPS` 载具或世界对象桥接样例
- 死亡后装备丢失 / 保留规则
- 撤离带回结果写回局外

## 6.4 通过标准

- `FPS Framework` 玩家可通过 Project-XX 交互桥打开容器、撤离点与任务交互物
- 玩家可稳定上 / 下 `JUTPS` 载具，且输入域切换无冲突
- 物品必须占用格子
- 容器之间可转移物品
- 主武器、副武器、头部、胸部、胸挂、背包死亡丢失
- 近战、战术设备、遗物死亡保留
- 成功撤离后局内新增物品回到局外

## 7. R3：BaseHub、仓库、设施与商人闭环

## 7.1 目标

建立最基础的局外循环，让玩家以 `FPS Framework` 的第一人称基地态在 `BaseHub` 中移动、整理、建造、交易和准备下一次 raid。

## 7.2 核心工作

- 实现 `PlayerProfileRuntime`
- 实现 `BaseHubRuntimeState`
- 创建可自由移动的 `BaseHub` 场景
- 建立基地态玩家输入域，关闭不需要的战斗动作但保留观察与交互
- 实现局外仓库容器
- 实现 `LoadoutRuntime`
- 实现设施定义与设施运行时
- 跑通第一版设施建造与升级流程
- 实现 1 名商人和其入住条件
- 实现基础商店与买卖逻辑
- 实现基础任务系统第一版
- 落地休息间与厨房两个首期设施

## 7.3 交付物

- 可交互的 `BaseHub` 场景
- 局外仓库
- 配装界面
- 建造入口与设施界面
- 商人界面
- 基础任务日志
- 休息间与厨房的第一版交互

## 7.4 通过标准

- 玩家能在基地中第一人称移动和交互
- `BaseHub` 不再依赖 FPSF / JUTPS 自带 HUD 作为正式界面
- 玩家能整理仓库并给角色配装
- 玩家能建造至少 1 个设施并看到状态变化
- 玩家能满足条件后解锁第 1 位商人入住
- 玩家能使用休息间和厨房的基础功能

## 8. R4：敌人、精英与 Boss 系统

## 8.1 目标

把敌人系统从“会打人的普通怪”升级为“具备层次和记忆点的 PVE 遭遇系统”。

## 8.2 核心工作

- 实现 `EnemyArchetypeDefinition`
- 扩展 `JutpsEnemyFacade` 与敌人运行时桥
- 实现 `EliteModifierDefinition`
- 实现 `EliteModifierService`
- 实现 `BossDefinition`
- 实现 `BossPhaseDefinition`
- 实现 `BossMechanicController`
- 实现 1 个完整 Boss 遭遇

## 8.3 交付物

- 1 种普通近战怪
- 1 种远程怪
- 2 到 3 个精英词缀
- 1 个 Boss
- Boss 专属掉落与阶段机制

## 8.4 通过标准

- 精英怪属性和行为可明显区别于普通怪
- Boss 有至少两阶段
- Boss 掉落可进入局外闭环

## 9. R5：成长、生存、技能、Buff 与遗物

## 9.1 目标

让项目从“好玩的 PVE raid 原型”进化成“有长期成长、连续生存压力和 Build 变化的 RPG 搜打撤”。

## 9.2 核心工作

- 实现属性系统
- 实现技能系统
- 实现 Buff / Debuff 系统
- 实现遗物系统
- 实现装备词缀系统
- 建立 `ModifierResolver`
- 实现饱食度、饮水度、疲劳值
- 实现生存值阈值惩罚和归零掉血
- 实现食物、饮水、休息的恢复逻辑
- 实现休息间的“下一局 Buff”
- 扩展厨房配方与可消耗品系统

## 9.3 交付物

- 角色成长页
- 基础技能树或技能面板
- Buff 状态显示
- 生存值 HUD
- 遗物槽位逻辑
- 词缀装备系统
- 可用的食物、饮品、休息恢复闭环

## 9.4 通过标准

- 角色成长能直接影响局内表现
- 生存值会持续影响多局之间的决策
- 玩家必须通过吃饭、喝水、休息来主动恢复状态
- 至少存在 2 到 3 种 Build 倾向
- 遗物与词缀对 raid 决策产生显著影响

## 10. R6：内容扩展、平衡与产品化

## 10.1 目标

从单一垂直切片，扩展成更完整、可长期游玩的产品原型。

## 10.2 核心工作

- 扩展到 2 到 4 张地图
- 扩展到 3 到 5 名商人
- 扩展地图专属掉落
- 扩展基地设施与升级层级
- 扩展任务链
- 扩展精英词缀池与 Boss 池
- 调整经济、掉率、耐久、容器压力、生存值曲线和平衡
- 优化 UI、加载、存档与回归流程

## 10.3 交付物

- 完整首期内容包
- 多张地图
- 多个商人
- 多套任务线
- 多级基地设施
- 稳定的结算与存档系统

## 10.4 通过标准

- 玩家有明确的长期目标
- 进图理由足够丰富
- 各地图有独立身份与资源价值
- 基地成长和 raid 收益形成稳定循环
- 系统之间能稳定闭环

## 11. 并行工作流建议

为了减少互相阻塞，建议按五条主线并行推进。

### 主线 A：规则与结算

- 容器
- 掉落
- 撤离
- 死亡丢失
- 生存值
- 商人
- 任务
- 交互权限

### 主线 B：玩家执行层（FPS Framework）

- 玩家角色桥接
- 相机
- 武器桥接
- 近战
- 伤害协议
- 生存值对角色手感的影响

### 主线 C：世界能力桥接（JUTPS）

- 敌人桥接
- 载具桥接
- 世界对象桥接
- AI 调参
- 遭遇原型

### 主线 D：基地与 Meta

- BaseHub 场景
- 设施
- 商人入住
- 制作
- 局外仓库
- 配装

### 主线 E：表现与内容

- UI
- 地图
- 音频
- VFX
- 场景事件
- 叙事与世界观资产

## 12. 推荐的近期任务顺序

如果从现在开始继续往下做，推荐顺序如下：

1. 固化项目目录、Bootstrap 和双框架目录结构
2. 完成 FPS Framework / JUTPS 接入审计，确认玩家、敌人、车辆和交互边界
3. 搭一张 `FPSF Player + JUTPS Enemy + Project-XX HUD` 联合测试场景
4. 打通 `RaidPlayerControllerFacade`、`ProjectXXFirearmBridge` 和 `RaidHealthBridge`
5. 补 `ProjectXXInteractableBridge` 与 `JutpsVehicleBridge`
6. 做格子背包、容器、死亡丢失与撤离回写
7. 搭建使用基地态玩家输入域的 `BaseHub` 场景
8. 接入仓库、设施、商人和任务
9. 打通精英、远程怪与 Boss 的第一套遭遇切片
10. 再把生存值、技能、Buff 与遗物系统大规模铺开

## 13. 垂直切片验收定义

推荐把第一版真正“可玩”标准定义为：

- 1 个可自由移动的基地场景
- 1 套局外仓库
- 1 间休息间
- 1 套厨房基础配方
- 1 名商人
- 1 套基础任务
- 1 张地图
- 1 个普通怪
- 1 个精英怪
- 1 个 Boss
- 1 套 Project-XX 容器 / 撤离交互闭环
- 1 个 `JUTPS` 载具或世界对象桥接样例
- 格子背包
- 死亡丢失
- 撤离结算

只要这套切片稳定，且玩家执行层、世界能力层与 Project-XX 规则层没有再互相抢控制权，你后面的地图、设施、商人、技能和内容扩展就会快很多。
