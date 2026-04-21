# Project-XX 预制作框架健康整改单

更新时间：`2026-04-22`

配套文档：

- [AGENT.md](/d:/UnityProject/Project-XX/Project-XX/AGENT.md)
- [ProjectXX-SystemOwnershipMatrix.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-SystemOwnershipMatrix.md)
- [ProjectXX-ThirdPartyPatchLog.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-ThirdPartyPatchLog.md)
- [ProjectXX-VendorOwnershipMatrix.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-VendorOwnershipMatrix.md)

## 1. 结论

当前工程已经是一个可运行、可验证、可继续探索的原型框架，但还不是适合“正式开始堆内容”的健康框架。

当前状态更准确的描述是：

- 原型验证已经够用
- 战斗闭环已经跑通
- 阵营框架已经有了雏形
- 但安装方式、依赖边界、第三方包耦合方式、运行时查找方式仍然偏“原型期”

如果在这个状态下直接进入大规模制作，后面最容易爆炸的不是战斗逻辑，而是：

- 组件挂接越来越隐式
- 场景与 prefab 越来越难追踪
- 第三方包改动越来越难升级
- Tag / Layer / 名字字符串越来越多
- 运行时对象来源越来越不确定

所以在真正进入制作前，建议先完成一轮“框架健康化整改”。

## 1.1 战略前提调整

在当前项目里，整改策略不再采用“尽量少动原框架、主要靠桥接先跑起来”的保守方式，而改为：

- 允许删除不再需要的原框架代码
- 允许重写原框架的关键逻辑
- 允许把 Akila / JUTPS 的局部能力收编为 Project-XX 自己的正式底座
- 允许为了长期健康性牺牲短期兼容性

这意味着：

- `Bridge` 只应该被视为迁移期手段，不应天然被视为最终架构
- 如果某条桥接链本质上是在维持“双系统双活”，那它就不是健康架构，而是待拆技术债
- 最终目标不该是“Akila + JUTPS + 一层厚桥接”，而应该是“Project-XX 拥有自己的正式游戏框架，Akila/JUTPS 只是被收编的能力来源”

## 1.2 现在应该明确接受的整改动作

在这个新前提下，以下动作都应视为合理范围，而不是“改太大了先别碰”：

- 删除 JUTPS 玩家链、UI 链、Inventory 链中不会进入正式产品的部分
- 删除 Akila 中不会进入正式产品的局外 UI、菜单、加载、设置等无关链路
- 重写 vendor 中必须被正式规则接管的入口
- 直接收编 vendor 某些模块到 Project-XX 的正式命名空间与架构边界里
- 把当前桥接脚本中的临时兼容逻辑拆掉，改成正式所有权关系

## 2. 当前主要风险概览

当前不合理点，按风险从高到低分为三档：

### `P0` 必须先改

这些问题如果不先处理，后面做容器、NPC、BaseHub、Boss、任务时会不断返工。

1. 核心系统所有权矩阵尚未真正选主
2. 战斗与生命值系统仍然双活
3. 运行时安装过重，组件来源不透明
4. 第三方包代码被直接修改，但缺少补丁治理与收编策略
5. Assembly 边界过粗，ProjectXX 仍大量依赖 `Assembly-CSharp`
6. Tag / Layer 仍然承担了过多兼容职责，缺少统一注册与校验入口
7. 运行时对象发现方式过于依赖 `FindFirstObjectByType` / `FindObjectsByType`
8. 不需要进入正式产品的 vendor 子系统还没有系统性删除/隔离

### `P1` 应在内容扩张前改

1. 阵营系统还是第一版，表达力够原型但不够长期内容生产
2. Inventory / Equipment / Weapon ownership 还没有彻底收归 Project-XX
3. UI 仍然是原型化生成方式，不适合作为正式产品 UI 底座
4. 定义层还过薄，正式内容制作所需的数据结构尚未规范化
5. Bootstrap 与场景切换仍然偏字符串驱动和样例驱动

### `P2` 可以延后，但必须进入中期计划

1. 持久化边界还没真正落地
2. 自动化回归为空白
3. 部分运行时查询与组件解析存在性能/正确性隐患

## 3. P0：必须先改的框架问题

## 3.1 必须先选主：系统所有权矩阵

### 当前问题

当前项目虽然在概念上写着：

- Akila = 玩家执行层
- JUTPS = 世界/敌人执行层
- Project-XX = 规则层

但从代码上看，很多系统还没有真正“选主”，而是处于“双活 + 桥接”状态。

例如：

- 玩家身上同时存在 `Damageable` 与 `JUHealth`
- 敌人同时暴露 `JUHealth` 与 `IDamageable`
- JUTPS AI 仍然需要 Tag/Layer 目标数据
- 后续 R2 又计划让 Project-XX 自己拥有正式的 Inventory / Equipment / Container 系统

如果不先选主，后面所有内容系统都会建立在摇摆边界之上。

### 健康目标

进入正式制作前，至少要把以下系统的所有权明确下来：

- 玩家控制 / 相机 / 武器表现：谁是最终 owner
- 健康 / 伤害 / 死亡 / faction：谁是最终 owner
- AI 感知 / 选敌 / 导航 / 攻击：谁是最终 owner
- Inventory / Equipment / Container：谁是最终 owner
- HUD / 窗口 / 输入上下文：谁是最终 owner
- Boot / 场景装配 / runtime registry：谁是最终 owner

### 建议结论

当前最合理的正式所有权应该是：

- 玩家控制、相机、第一人称武器表现：`Akila`
- AI 导航、基础感知、敌人动画攻击骨架：`JUTPS`
- 健康、伤害、死亡、阵营、交互、Inventory、Equipment、Session、HUD：`Project-XX`

这意味着：

- 战斗规则最终应该收归 Project-XX
- Inventory 绝不能继续以 Akila 的 item/runtime 作为长期正式基础
- JUTPS 不应继续拥有对玩家正式状态的定义权

## 3.2 战斗/生命值系统双活，必须收敛成单一正式模型

### 当前问题

当前代码里曾存在明确的“双生命系统双活”。  
截至 `2026-04-21`，第一阶段收敛已经落地为：

- 正式状态源：`ProjectXXCombatant`
- 玩家兼容端点：`Damageable` + `JUHealth` + `ProjectXXCombatantSync`
- 敌人兼容端点：`JUHealth` + `JutpsEnemyDamageableAdapter` + `ProjectXXCombatantSync`

这比之前健康了一步，但还没有彻底收口。

实际问题包括：

- 两套生命值状态需要同步
- 谁是 authoritative source 不够明确
- 很容易出现视觉表现、死亡事件、命中反馈、HUD 同步不同步的问题
- 新增异常、护甲、DOT、局内 Buff 时会成倍增加复杂度

### 之前没有充分强调的问题

之前因为担心改动面太大，这个问题只被当成“桥接成本高”，没有被上升到“必须整改的结构性问题”。  
现在在新前提下，应该明确把它列为 `P0`。

### 健康目标

正式框架里只能有一个 authoritative combat state。

建议做法：

- `Project-XX` 定义正式健康/伤害/死亡模型
- Akila 与 JUTPS 只消费这个模型，或者只作为执行入口把事件汇入这个模型
- `ProjectXXCombatantSync` 只能作为从正式 combat state 指向 vendor 的单向兼容镜像器
- `JutpsHealthProxy` 这类旧双向同步组件应视为迁移期残留，而不是长期保留组件

### 建议整改

- 已落地：新建独立的 `ProjectXXCombatant` 正式战斗状态组件
- 已落地：把 `ProjectXXCombatantSync` 收敛为单向兼容镜像器
- 明确谁负责生命值、护甲、死亡、阵营、异常
- 逐步消除 `JUHealth <-> Damageable` 的镜像同步
- 不再让“生命系统桥接”成为后续功能默认扩展点

## 3.3 运行时安装过重

### 当前问题

当前多个关键对象仍通过运行时 `GetOrAdd / AddComponent / FindFirstObjectByType` 自行拼装。例如：

- `ProjectXXRaidSceneInstaller`
- `ProjectXXAkilaPlayerBridge`
- `JutpsEnemyBridge`

这在原型期很高效，但正式制作阶段会带来几个问题：

- prefab 和场景上的“真实依赖”不清楚
- 同一个功能可能被场景安装器和对象自身重复安装
- 测试图能跑，不代表正式关卡也一定能跑
- 后面一旦拆场景、拆加载流程、做 BaseHub / Raid 双入口，安装链会越来越难维护

截至 `2026-04-21`，这一项已经完成第一步：

- `ProjectXXRaidSceneInstaller` 开始承担明确的 composition root 角色
- `ProjectXXRaidRuntimeRegistry` 已建立第一版运行时注册中心
- `ProjectXXRaidHudController`、`ProjectXXExtractionPoint`、`ProjectXXDamageBridge`、`JutpsEnemyBridge` 已开始改成显式注入优先

但这还没有彻底完成，因为 scene installer 内部仍保留少量场景扫描职责。

### 健康目标

进入正式制作前，需要把“对象身上应该有什么组件”从运行时自愈，改成：

- prefab 预先挂好
- composition root 明确装配
- 运行时只做验证，不再大规模动态补组件

### 建议整改

- 已落地：建立 `ProjectXXRaidRuntimeRegistry`，让 `Session / Player / HUD / Extraction / Enemies` 开始显式注册
- 已落地：让 `ProjectXXRaidSceneInstaller` 向 `RaidCompositionRoot` 方向收敛
- 把玩家 prefab 所需桥接尽量前移到 prefab authoring
- 把敌人 prefab 所需桥接尽量前移到 prefab authoring
- 把剩余场景级查找继续压缩在 composition root 内
- 把当前的 `GetOrAdd` 逻辑逐步改成“校验 + 报错”，而不是“发现缺少就补上”

## 3.4 第三方包代码直接修改，但没有补丁治理与收编策略

### 当前问题

当前我们已经修改了第三方包内的若干脚本，例如：

- Akila `Damageable`
- Akila `MeleeWeapon`
- Akila `Projectile`
- JUTPS `JUHealth`
- JUTPS `Damager`
- JUTPS `JUFieldOfViewSensor`
- JUTPS `JUCharacterController`

这些改动本身未必错，但在新的战略前提下，问题已经不只是“补丁没登记”，而是：

- 我们还没有明确哪些 vendor 代码准备长期持有
- 我们还没有明确哪些 vendor 代码准备彻底删除或禁用
- 我们还没有明确哪些 vendor 代码应该被正式收编到 Project-XX 自己的框架里

如果没有这层治理，后续会有几个问题：

- 一旦升级第三方包，很难知道哪些地方被我们改过
- 很多正式规则会不知不觉继续写进 vendor 代码，最后变成无法维护的 fork
- 项目会长期停留在“两个外部框架 + 大量补丁”的尴尬态

### 健康目标

后续需要建立明确规则：

- 哪些第三方改动是“允许保留的最小补丁”
- 哪些逻辑必须移回 ProjectXX 自己的正式层
- 哪些第三方模块准备被正式收编
- 哪些第三方模块准备被彻底删除
- 哪些 vendor 修改必须登记在补丁文档中

### 建议整改

- 新建一份 `ThirdPartyPatchLog` 文档，登记所有现存 vendor patch
- 建立 `VendorOwnershipMatrix`
- 明确把 vendor 模块分为：
  - `Retain`
  - `Internalize`
  - `Disable`
  - `Delete`
- 对已修改 vendor 文件做一次“补丁合理性盘点”
- 不再默认假设“尽量保留原框架完整性”比“框架长期健康”更重要

## 3.5 Assembly 边界过粗

### 当前问题

截至 `2026-04-22`，已经落地当前阶段拆分：

- `Akila.FPSFramework.asmdef`
- `Akila.FPSFramework.Editor.asmdef`
- `JUTPS.Runtime.asmdef`
- `JUTPS.Editor.asmdef`
- `ProjectXX.Domain.Combat.asmdef`
- `ProjectXX.Domain.Meta.asmdef`
- `ProjectXX.Domain.Raid.asmdef`
- `ProjectXX.Infrastructure.Definitions.asmdef`
- `ProjectXX.Foundation.asmdef`
- `ProjectXX.Services.asmdef`
- `ProjectXX.UI.asmdef`
- `ProjectXX.UI.Editor.asmdef`
- `ProjectXX.Bridges.FPSFramework.asmdef`
- `ProjectXX.Bridges.Combat.asmdef`
- `ProjectXX.Bridges.JUTPS.asmdef`
- `ProjectXX.Presentation.Raid.asmdef`
- `ProjectXX.Bootstrap.asmdef`

到这一步，ProjectXX 当前正式 runtime 代码已经基本退出 `Assembly-CSharp`。

当前剩下的主要 blocker 已经从“runtime 边界没拆”变成：

- `JUTPS` 正式 editor 脚本已落入 `JUTPS.Editor.asmdef`，但 runtime 内仍残留 editor helper / gizmo / handles 混装文件
- ProjectXX 自己的 editor/test 层还没有进入明确 asmdef 规划

这会导致：

- 编译边界过大
- 依赖方向容易混乱
- 后续模块化重构成本高
- 测试程序集和正式程序集很难清晰拆分

### 健康目标

ProjectXX 至少应该拆成几个明确程序集：

- `ProjectXX.Foundation`
- `ProjectXX.Infrastructure.Definitions`
- `ProjectXX.Domain.Raid`
- `ProjectXX.Domain.Meta`
- `ProjectXX.Bridges.FPSFramework`
- `ProjectXX.Bridges.JUTPS`
- `ProjectXX.Presentation`
- 后续 `ProjectXX.Bootstrap`

### 建议整改

- 已落地：把 `Foundation / Infrastructure.Definitions / Domain.Meta / Domain.Raid` 从 `Assembly-CSharp` 中拆出
- 已落地：把 ProjectXX 原型 UI 从 `Assembly-CSharp` 中拆出为 `ProjectXX.UI / ProjectXX.UI.Editor`
- 已落地：给 JUTPS vendor runtime 建立 `JUTPS.Runtime`，把 vendor 运行时代码正式拉出 `Assembly-CSharp`
- 已落地：把 `ProjectXX.Bridges.FPSFramework / ProjectXX.Bridges.Combat / ProjectXX.Bridges.JUTPS` 从 `Assembly-CSharp` 中拆出
- 已落地：把 `ProjectXX.Presentation.Raid / ProjectXX.Bootstrap / ProjectXX.Services` 从 `Assembly-CSharp` 中拆出
- 已落地：把 JUTPS `JUFieldOfViewSensor` 对 `ProjectXX` 的依赖从 bridge 组件类型收敛为正式 combat target filter 契约
- 下一步：继续清理 `JUTPS.Runtime` 内残余 editor helper 混装点，再规划 ProjectXX editor/test 程序集
- 严格限制依赖方向
- Domain 不依赖 Presentation
- Bridges 不反向定义 Domain 规则
- Presentation 只消费 runtime 状态

## 3.6 Tag / Layer 缺少统一注册与校验入口

### 当前问题

虽然正式阵营判断已经开始从 Tag 迁出，但当前兼容层仍大量依赖：

- `Player`
- `Enemy`
- `Bullet`
- `FPS Object`
- `Enviroment`

这些字符串和 layer 名称现在分散在：

- Project Settings
- 第三方包
- 适配桥接
- 场景/预制体

这会导致：

- 少一个 Tag 就会报运行时错
- 改名时很难知道全局影响
- Layer/Tag 兼容逻辑散落
- 一些本不该长期存在的适配层，例如 `JutpsTargetAdapter` 和 `ProjectXXJutpsFactionBridge`，仍在持续把正式状态回写成 Tag/Layer

截至 `2026-04-22`，这一项已经完成第一阶段收敛：

- `ProjectXXCompatibilitySettings` 已建立，开始承接兼容 Tag / Layer 名称
- `ProjectXXCompatibilityValidator` 已接入 raid composition root，启动时集中校验必要兼容项
- `JutpsTargetAdapter`、`ProjectXXJutpsFactionBridge`、`ProjectXXFirstPersonViewBridge`、`JutpsEnemyBridge` 已开始统一读这份配置

### 健康目标

需要把 Tag / Layer 降级为：

- 兼容第三方框架的底层配置
- 由一个统一入口声明和验证
- 不再作为 ProjectXX 正式规则的主数据源

### 建议整改

- 已落地：建立 `ProjectXXCompatibilitySettings` 资产
- 已落地：启动时对必须存在的 Tag / Layer 做集中校验
- ProjectXX 自己的新规则禁止继续直接写死 `CompareTag(...)`
- 中长期目标是把 JUTPS 的选敌逻辑直接改到读取 ProjectXX 正式目标信息，而不是继续依赖 Tag 回写
- 继续把剩余散落在组件字段中的兼容名收敛进正式配置资产

## 3.7 运行时对象发现方式太松

### 当前问题

当前很多对象通过：

- `FindFirstObjectByType`
- `FindObjectsByType`

去寻找会话、HUD、玩家、撤离点、敌人等。

原型期没问题，但正式制作时会带来：

- 多场景加载时来源不明确
- 重复对象难发现
- 调试时“到底连到了哪个对象”不透明

截至 `2026-04-21`，这一项也已完成第一阶段收敛：

- `ProjectXXRaidRuntimeRegistry` 已开始承接核心 Raid 运行时对象
- `ProjectXXRaidHudController` 不再自行查找玩家/武器桥，只消费 `RaidSessionRuntime`
- `ProjectXXExtractionPoint`、`ProjectXXDamageBridge`、`JutpsEnemyBridge` 不再以全局查找作为第一优先级

### 健康目标

运行时对象关系应尽量变成：

- composition root 注入
- prefab authoring 明确引用
- registry 显式注册

### 建议整改

- 已落地：建立 `ProjectXXRaidRuntimeRegistry`
- 已落地：`RaidSessionRuntime`、玩家 facade、HUD、撤离点开始通过显式注册连接
- 禁止继续在 ProjectXX 代码中新增新的全局查找依赖
- 继续把剩余的 `FindFirstObjectByType / FindObjectsByType` 约束在 composition root 内

## 3.8 没必要保留的 vendor 子系统应该直接清理

### 当前问题

之前出于“尽量少动原框架”的思路，这一项没有被明确提出。  
但在你现在允许大改的前提下，它其实是非常重要的整改项。

当前两个框架里有不少不会进入正式产品、但仍然在工程中制造噪音和误导的子系统，例如：

- JUTPS 玩家链
- JUTPS 默认玩家 UI
- JUTPS 与正式设计不相干的 demo 场景和 demo 资产入口
- Akila 中不会进入正式产品的菜单/加载/设置/演示链路
- 与 Project-XX 未来 Inventory / Equipment / UI 正式方案冲突的现成子系统

### 健康目标

正式制作前，工程里不应再保留大量“看起来可用、实际上不会采用”的并行实现。

### 建议整改

- 为两个框架建立 `Retain / Disable / Delete` 清单
- 明确把不进入正式产品的子系统从主路径上移除
- 必要时直接删除 demo-only 代码和资源，降低认知噪音
- 不要让“也许以后用得上”成为长期保留理由

## 4. P1：应在内容扩张前改的框架问题

## 4.1 阵营系统需要升级到正式内容版本

### 当前问题

当前阵营系统能支撑：

- 玩家
- 友方
- 中立
- 敌人
- 中立受击后敌对来源阵营

但它还是“第一版”：

- 敌对关系基于 faction，而不是更细粒度的 actor/group
- 没有敌对时长、仇恨衰减、援助广播
- `ResolveMember` 仍然依赖层级搜索
- faction 还是 enum，不是正式定义资产

### 健康目标

正式内容阶段需要升级为：

- `FactionDefinition`
- 可配置态度矩阵
- 可选 actor 级仇恨
- 可配置仇恨保持时长
- 可广播的支援/警戒事件

## 4.2 Inventory / Equipment / Weapon ownership 需要彻底收归 Project-XX

### 当前问题

这是之前也存在、但因为改动面太大没有被充分强调的问题。

当前项目未来路线明确要求：

- Project-XX 自己拥有 `ItemDefinition`
- Project-XX 自己拥有 `ContainerRuntime`
- Project-XX 自己拥有 `EquipmentRuntime`

这意味着：

- Akila 现成的 item/inventory 体系不能继续自然外扩成正式背包系统
- 现在的 `ProjectXXWeaponBridge / EquipmentBridge` 只能视为过渡期整合，不应默认成为长期底层

### 健康目标

正式路线应该是：

- Akila 负责武器持有、射击执行、第一人称表现
- Project-XX 负责物品定义、背包、装备槽、耐久、弹药归属、搜刮与掉落规则

### 建议整改

- 在进入 R2 前，先明确“Akila Inventory 在正式架构中的降级方式”
- 避免让 R2 容器系统建立在 Akila Inventory 抽象之上
- 提前规划“Project-XX Item -> Weapon Presentation” 的正式绑定方式

## 4.3 UI 仍然是原型化实现

### 当前问题

当前 `ProjectXXRaidHudController` 直接程序化生成 UI，适合作为 prototype HUD，不适合作为正式 UI 底座。

### 健康目标

在真正开始做背包、商人、BaseHub UI 前，需要先明确：

- 正式 UI prefab 结构
- Presenter / View 关系
- HUD 与窗口系统的统一生命周期

## 4.4 定义层还不够正式

### 当前问题

现在真正落地的定义只有一个最小敌人定义，后续制作所需的：

- Item
- Container
- Equipment Slot
- Backpack
- Armor
- Merchant
- Facility

都还没有正式定义。

### 健康目标

在开始大规模内容制作前，定义层必须先稳定，否则后面会一边做内容一边改数据结构。

## 4.5 Bootstrap 仍然偏样例化

### 当前问题

当前 `ProjectXXBootstrap` 仍然依赖场景名字符串和最小样例流程。

### 健康目标

后续需要：

- 明确 Boot 配置资产
- 区分开发入口与正式入口
- 区分 Raid / BaseHub / TestMap 的启动策略

## 5. P2：可以延后，但必须进入计划

## 5.1 持久化边界

当前 `PlayerProfileRuntime` 仍然是最小原型对象，还没有真正进入：

- 存档模型
- 版本迁移
- 局外仓库落盘
- Raid 结果回写

## 5.2 自动化回归

当前关键能力几乎都靠手工进 Play Mode 验证。  
在开始堆容器、商人、任务、遭遇之前，至少要补最小自动化回归。

## 5.3 运行时解析与性能

一些当前还能接受的做法，后续会变成性能或正确性隐患，例如：

- 频繁层级搜索 faction member
- 频繁场景级对象查找
- 场景安装器反复扫描对象

## 6. 建议的整改顺序

在真正开始 `R2` 内容制作前，建议按下面顺序推进：

1. 先明确系统所有权矩阵
2. 立刻决定战斗/生命值正式模型，开始消灭双活
3. 建立第三方补丁台账与 `VendorOwnershipMatrix`
4. 清理不进入正式产品的 vendor 子系统
5. 拆 ProjectXX asmdef 边界
6. 建立 `FrameworkConventions` 配置与启动校验
7. 建立 `RaidCompositionRoot / RuntimeRegistry`
8. 把玩家 prefab 与敌人 prefab 的桥接尽量前移到 authoring，逐步拆除运行时自愈安装
9. 把 HUD / Session / Extraction 的连接从全局查找改成显式引用或注册
10. 设计 faction v2 的目标形态
11. 明确 Inventory / Equipment 的正式所有权落点
12. 然后再进入 `R2` 的容器与搜刮开发

## 7. 进入正式制作前的完成标准

如果下面这些标准达成，我会认为框架已经进入“健康可制作”状态：

- ProjectXX 自己的核心模块已拆出明确 asmdef
- 双生命值/双伤害系统不再双活
- ProjectXX 不再继续新增对 Tag 的正式规则依赖
- 关键运行时对象不再依赖全局查找进行隐式连接
- 玩家与敌人 prefab 的正式组件结构基本固化
- 第三方补丁来源清楚、边界清楚，且已有保留/收编/删除决策
- R2 所需定义层已经稳定
- 至少有最小的 PlayMode / EditMode 自动化回归

## 8. 不应该再继续增加的技术债

从现在开始，建议明确禁止以下做法继续扩散：

- 在 ProjectXX 新逻辑里继续直接新增 `CompareTag(...)`
- 在 ProjectXX 新逻辑里继续新增 `FindFirstObjectByType(...)` 作为正式依赖注入方式
- 在 gameplay 初始化中继续大规模 `AddComponent(...)` 作为默认装配方式
- 让 `JUHealth <-> Damageable` 这类双系统桥接继续扩张
- 让 `Tag/Layer 回写适配` 从迁移手段变成长期设计
- 把新的正式规则继续直接写进 Akila / JUTPS vendor 文件但不登记治理
- 让场景安装器继续承担越来越多业务逻辑

## 9. 当前建议

不要直接跳进 `R2` 内容制作。  
在你现在允许大改和删改原框架逻辑的前提下，更合理的顺序是：

`系统选主 -> 删除无关链路 -> 收敛双活系统 -> 固化正式边界 -> 再开始容器/搜刮/局外系统制作`

这样做的好处不是“更优雅”，而是可以避免最昂贵的返工：内容做了一半，才发现底层一直建立在临时桥接之上。
