# Project-XX 粗略技术设计文档

更新时间：`2026-04-14`

## 1. 目标

在 `Akila FPS Framework` 的玩家第一人称执行层、`JUTPS` 的敌人/世界原型执行层，以及 `Project-XX` 自建规则层之上，构建一套适合：

- 单机 PVE 第一人称搜打撤
- 恐怖后末日题材
- RPG 成长
- 局内搜刮与局外基地经营

的技术架构。

这套架构要求：

- 玩家正式玩法严格采用第一人称
- 玩家、敌人、NPC、搜刮、撤离、局外成长能接成一个长期闭环
- 第三方框架只负责“执行层”，正式规则和正式 UI 由 Project-XX 接管

## 2. 基本原则

### 原则 1：Akila 负责玩家执行层，JUTPS 负责世界执行层，Project-XX 负责规则层

`Akila FPS Framework` 负责：

- 第一人称玩家控制
- 玩家相机、武器、开火、近战与基础交互发现

`JUTPS` 负责：

- 敌人 AI 感知、追击、攻击
- 车辆与部分世界原型能力
- 可复用的 NPC / 敌人行为骨架

`Project-XX` 负责：

- 规则定义
- 阵营、伤害许可与动态敌对关系
- 容器、装备、搜刮、撤离、局外回写
- 正式 HUD 与正式窗口系统
- 场景安装、桥接、运行时同步

### 原则 2：第三方框架不直接定义正式游戏规则

第三方框架的默认：

- Tag 逻辑
- 玩家 UI
- Demo 场景行为
- 原始伤害/目标筛选

都只能视为底层能力，不视为最终产品逻辑。正式规则必须由 `Project-XX` 在桥接层之上统一收口。

### 原则 3：运行时状态与定义资产分层

- 定义资产：尽量使用 `ScriptableObject`
- 运行时状态：尽量使用纯 C# runtime 对象
- MonoBehaviour：负责场景表现、生命周期与框架桥接

## 3. 当前高层架构

建议使用下列层次：

### 3.1 Foundation

来源：

- Unity
- Akila FPS Framework
- JUTPS

职责：

- 物理、动画、相机、输入、基础武器与 AI 执行

### 3.2 Domain

当前已存在的域：

- `Domain/Raid`
- `Domain/Meta`
- `Domain/Combat`

职责：

- Raid 会话状态
- 玩家与局外档案状态
- 阵营、敌对关系、伤害许可、动态仇恨

### 3.3 Bridges

目录：

- `Bridges/FPSFramework`
- `Bridges/JUTPS`

职责：

- 把第三方框架的能力接入 Project-XX 规则
- 负责跨框架同步，而不是定义核心玩法

### 3.4 Infrastructure

目录：

- `Infrastructure/Definitions`
- 后续的 `Infrastructure/Save`

职责：

- 定义资产
- 存档与资源装载

### 3.5 Presentation

当前已落地：

- `ProjectXXRaidHudController`
- `ProjectXXExtractionPoint`

后续扩展：

- 背包窗口
- 容器窗口
- 商人窗口
- BaseHub UI

## 4. 当前可运行切片的真实边界

当前仓库已经具备：

- Bootstrap 场景
- Raid 测试图
- 第一人称玩家预制体
- JUTPS 敌人原型
- Project-XX HUD 与会话状态同步
- 最小撤离点
- 阵营/敌对关系基础框架

当前尚未具备：

- 正式容器与搜刮
- 正式装备槽规则
- 正式局外仓库
- 正式友方/中立 NPC 内容样例
- 精英/Boss 遭遇系统

## 5. 当前战斗架构

## 5.1 玩家侧链路

关键文件：

- `ProjectXXPlayerFacade`
- `ProjectXXAkilaPlayerBridge`
- `ProjectXXWeaponBridge`
- `ProjectXXDamageBridge`
- `ProjectXXFirstPersonViewBridge`
- `ProjectXXMeleeBridge`

职责：

- 把 Akila 玩家预制体变成 Project-XX 的正式玩家入口
- 把武器、血量、HUD 与会话运行时接起来
- 修正第一人称相机栈与视图层

## 5.2 敌人侧链路

关键文件：

- `JutpsEnemyBridge`
- `JutpsEnemyDamageableAdapter`
- `JutpsHealthProxy`
- `JutpsTargetAdapter`

职责：

- 把 JUTPS 敌人接入 Project-XX 的伤害、定义与会话统计
- 把敌人的血量、死亡和命中表现收束成正式闭环

## 5.3 阵营与敌对关系架构

这是本轮新增并已落地的正式基础规则层。

### 核心文件

- `Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXFaction.cs`
- `Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXFactionMember.cs`
- `Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXFactionUtility.cs`
- `Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/ProjectXXJutpsFactionBridge.cs`
- `Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/ProjectXXJutpsFactionTargetFilter.cs`

### 当前阵营枚举

- `Player`
- `FriendlyNpc`
- `NeutralNpc`
- `Enemy`

### 当前基础关系

- `Player` 与 `FriendlyNpc` 互为友方
- `Enemy` 默认敌对 `Player` 与 `FriendlyNpc`
- `NeutralNpc` 默认不主动敌对任何阵营
- 同阵营单位之间不能互相伤害

### 当前动态规则

- 中立单位在受伤后，会把伤害来源阵营登记为运行时敌对阵营
- 友方单位默认会把 `Enemy` 作为有效敌对目标
- 敌方单位默认把 `Player` 与 `FriendlyNpc` 作为有效敌对目标

### 当前落地点

- `Akila Damageable.Damage(...)` 已接入阵营伤害许可
- `JUTPS JUHealth.DoDamage(...)` 已接入阵营伤害许可
- `JUTPS FieldOfView` 已接入 faction target filter

这意味着：

- 阵营判断不再是单独某个 AI 或某个武器脚本的局部行为
- 伤害入口与目标筛选入口都已经走同一套 Project-XX 规则

## 5.4 当前伤害链说明

### Akila -> JUTPS

路径：

- `Firearm` / `Projectile`
- `IDamageable`
- `JutpsEnemyDamageableAdapter`
- `JUHealth`
- `JutpsEnemyBridge`

说明：

- 命中敌人时，最终会进入 `JUHealth`
- 在真正扣血前，会经过 `ProjectXXFactionUtility.CanApplyDamage(...)`

### JUTPS -> Akila

路径：

- `Damager`
- `JUHealth`
- `JutpsHealthProxy`
- `Damageable`
- `ProjectXXDamageBridge`

说明：

- 接触伤害最终会同步到 Akila 的 `Damageable`
- 玩家血量与 HUD 更新由 Project-XX 运行时继续接管

## 5.5 当前 AI 目标链说明

JUTPS 原生 FOV 只认：

- layer
- tag

这不足以支撑：

- 敌人不互伤
- 友方 NPC 主动打敌人
- 中立 NPC 受击后才反击

因此当前的正式方案是：

1. 保留 JUTPS 的原始 `FieldOfView`
2. 用 `ProjectXXJutpsFactionBridge` 配置可扫描范围
3. 用 `ProjectXXJutpsFactionTargetFilter` 在真正选目标前做阵营过滤

最终结果是：

- 视觉感知仍复用 JUTPS
- 最终有效目标由 Project-XX 的阵营规则决定

## 6. 当前目录约定

### 6.1 代码目录

- `Assets/Res/Scripts/ProjectXX/Bootstrap`
- `Assets/Res/Scripts/ProjectXX/Foundation`
- `Assets/Res/Scripts/ProjectXX/Domain/Combat`
- `Assets/Res/Scripts/ProjectXX/Domain/Raid`
- `Assets/Res/Scripts/ProjectXX/Domain/Meta`
- `Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework`
- `Assets/Res/Scripts/ProjectXX/Bridges/JUTPS`
- `Assets/Res/Scripts/ProjectXX/Infrastructure/Definitions`
- `Assets/Res/Scripts/ProjectXX/Presentation`

### 6.2 共享程序集

新增：

- `ProjectXX.Domain.Combat.asmdef`

作用：

- 让阵营规则不再绑定到 `Assembly-CSharp`
- 允许 Akila 与未来其他程序集直接复用这套域模型

## 7. 下一阶段技术重点

## 7.1 R2 主线

优先实现：

- `ItemDefinition`
- `ContainerDefinition`
- `InventoryGridRuntime`
- `ContainerRuntime`
- `EquipmentRuntime`
- `DeathLossService`
- `ExtractionService`
- `RaidResultService`

## 7.2 阵营系统的下一步

当前阵营系统已经能支撑基础规则，但后续还需要内容化与产品化：

- 在测试图中加入友方 NPC 样例
- 在测试图中加入中立 NPC 样例
- 增加“受击后记仇时长、共享仇恨、阵营广播”等可选扩展
- 在 R4 中把精英/Boss/NPC 营地都接到同一套 faction 框架上

## 8. 当前结论

目前技术架构已经形成稳定边界：

- `Akila` 负责玩家执行
- `JUTPS` 负责敌人与世界执行
- `Project-XX` 负责规则、桥接、运行时与正式 UI

其中“阵营与敌对关系”已经从临时逻辑升级为正式基础模块。后续任何 NPC、敌人、派系化遭遇，都不应该再直接写死在单个武器脚本或 AI 脚本里，而应继续沿用 `ProjectXXFactionMember + ProjectXXFactionUtility + JUTPS Faction Bridge` 这一套结构。
