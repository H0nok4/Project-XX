# Project-XX 粗略技术设计文档

## 1. 目标

在 JUTPS 的战斗与角色能力底座上，构建一套适合“FPS 搜打撤 + RPG”项目的可扩展技术架构。

本设计优先考虑：

- 单机或离线优先原型
- 后续可扩展为更复杂的 Meta 系统
- 与现有 Project-XX runtime UI 标准兼容

## 2. 基本原则

### 原则 1：复用 JUTPS 的战斗底层，不复用它的全部游戏外壳

复用：

- 角色控制
- 相机
- 武器开火
- AI 感知和基础战斗
- 交互
- 载具

替换或桥接：

- 库存
- 装备数据
- 商人
- 任务
- Meta 存档
- Buff / 技能 / 属性
- 正式 UI

### 原则 2：局内与局外分层

- `局内玩法层` 负责战斗、搜刮、撤离
- `局外 Meta 层` 负责档案、经济、商人、任务、成长

### 原则 3：数据优先，表现其次

- 定义先用 `ScriptableObject`
- 运行时实例用纯 C# Runtime State
- JUTPS prefab 只做表现与执行

### 原则 4：正式 UI 必须遵守 Project-XX 规范

新 runtime UI 必须：

- 放在 `Assets/Resources/UI/...`
- 使用 `ViewBase / WindowBase`
- 使用 `*Template` 脚本收口引用
- 挂到 `PrototypeRuntimeUiManager`

不要继续扩展 JUTPS 自带 UI 作为正式方案。

## 3. 高层架构分层

建议采用五层结构。

## 3.1 Foundation 层

来源：

- JUTPS 包
- Unity 原生系统

职责：

- 角色移动
- 枪械
- 近战/投掷
- AI 基础
- 交互
- 载具
- 场景内表现

## 3.2 Raid Domain 层

职责：

- 局内角色数值运行态
- 局内装备实例与耐久
- 掉落生成
- 容器交互
- 撤离规则
- 局内商店/临时商人
- 局内任务目标推进

## 3.3 Meta Domain 层

职责：

- 玩家档案
- 仓库
- 商人关系
- 任务进度
- 技能与天赋
- 经济
- 地图解锁
- 配装与进图准备

## 3.4 Presentation 层

职责：

- 正式 UGUI prefab 界面
- HUD
- 背包
- 商店
- 任务日志
- 角色页
- 结算页

## 3.5 Persistence 层

职责：

- 定义数据加载
- Profile 存档
- Raid 快照
- 版本迁移

## 4. 建议的模块划分

## 4.1 定义层（ScriptableObject）

建议建立这些定义资产：

- `ItemDefinition`
- `WeaponDefinition`
- `AmmoDefinition`
- `ArmorDefinition`
- `EquipmentSlotDefinition`
- `ContainerDefinition`
- `BuffDefinition`
- `SkillDefinition`
- `AttributeDefinition`
- `MerchantDefinition`
- `QuestDefinition`
- `QuestObjectiveDefinition`
- `MapDefinition`
- `LootTableDefinition`
- `ExtractionPointDefinition`
- `EnemyArchetypeDefinition`
- `VendorOfferDefinition`

作用：

- 统一所有内容数据
- 避免继续依赖散落在场景 prefab 上的直接数值

## 4.2 Runtime State 层

建议建立纯 C# 运行时对象：

- `MetaProfileRuntime`
- `RaidSessionRuntime`
- `PlayerLoadoutRuntime`
- `InventoryRuntime`
- `EquipmentRuntime`
- `MerchantRuntimeState`
- `QuestRuntimeState`
- `CharacterStatRuntime`
- `CharacterBuffRuntime`
- `LootContainerRuntime`
- `MapRuntimeState`

原则：

- 运行态对象不依赖 MonoBehaviour
- MonoBehaviour 只是表现和驱动桥

## 4.3 Service 层

建议建立这些服务：

- `ProfileService`
- `RaidSessionService`
- `ItemFactoryService`
- `LootGenerationService`
- `MerchantService`
- `QuestService`
- `SkillService`
- `BuffService`
- `EconomyService`
- `MapTravelService`
- `SaveGameService`

职责：

- 管理流程与规则
- 统一访问点
- 减少 UI 直接依赖场景对象

## 5. 与 JUTPS 的桥接设计

## 5.1 角色桥接

新增：

- `ProjectXXCharacterFacade`
- `ProjectXXCharacterStatBridge`
- `ProjectXXCharacterBuffBridge`
- `ProjectXXDamageBridge`

思路：

- `JUCharacterController` 继续做移动和动作
- Bridge 层负责把 RPG 属性、Buff、装备修正同步进去

例子：

- 最终移动速度 = JUTPS 基础速度 x 属性修正 x Buff 修正 x 负重修正
- 最终后坐力 = 武器基础值 x 技能修正 x 装备修正 x 伤病修正

## 5.2 武器桥接

新增：

- `WeaponInstanceRuntime`
- `ProjectXXWeaponPresenter`
- `ProjectXXAmmoResolver`

思路：

- JUTPS `Weapon` 保留射击行为
- 武器当前弹匣、弹种、耐久、改件、品质来自 Runtime 数据

## 5.3 库存桥接

JUTPS `JUInventory` 不应作为最终仓库系统，只建议保留为：

- 局内快捷栏/当前手持切换代理

新增：

- `InventoryRuntime`
- `ContainerRuntime`
- `EquipmentRuntime`
- `RaidInventoryBridge`

推荐逻辑：

- 局外仓库和局内容器全走 Project-XX 自己的数据结构
- JUTPS 只映射当前正在装备/使用的物体

## 5.4 交互桥接

新增：

- `ProjectXXInteractableAdapter`
- `InteractionRequirementEvaluator`
- `InteractionExecutionService`

思路：

- 继续使用 JUTPS 的“查找最近交互物”
- 交互业务本身由 Project-XX 自己的服务判断

这样可支持：

- 任务门槛
- 钥匙卡
- 商人权限
- 地图事件
- 局内商店

## 5.5 AI 扩展

保留：

- `FieldOfView`
- `HearSensor`
- `Attack`
- `FollowWaypoint`

新增上层：

- `RaidThreatModel`
- `SquadCoordinator`
- `InterestPointEvaluator`
- `LootInterestEvaluator`
- `ExtractionDecisionEvaluator`

让 AI 在基础战斗之上获得：

- 抢资源
- 巡逻热点
- 调查枪声
- 守撤离点
- 撤退/呼叫增援

## 6. UI 技术方案

## 6.1 正式 UI 统一方案

全部新 UI 走：

- `Assets/Resources/UI/...`
- `ViewBase`
- `WindowBase`
- `*Template`
- `PrototypeRuntimeUiManager`

建议模块：

- `Hud`
- `Inventory`
- `Character`
- `Merchant`
- `Quest`
- `RaidResult`
- `MapSelect`
- `Loadout`
- `Medical`
- `Workshop`

## 6.2 与 JUTPS UI 的关系

可参考：

- HUD 元素拆分方式
- 暂停与设置的基础行为

不直接沿用：

- 背包正式实现
- 商店正式实现
- 任务日志正式实现

## 6.3 UI Presenter 层

建议每个复杂界面都用 Presenter/Controller 层连接 Runtime State 与 Template。

例如：

- `MerchantWindowController`
- `MerchantWindowTemplate`
- `MerchantOfferItemView`
- `QuestJournalWindowController`
- `LoadoutWindowController`

## 7. 场景结构建议

建议首期场景流如下：

1. `Bootstrap`
2. `MainMenu`
3. `BaseHub`
4. `Raid_<MapId>`
5. `RaidResult`

### Bootstrap

职责：

- 初始化配置
- 加载 Profile
- 注册核心服务

### BaseHub

职责：

- 商人
- 仓库
- 配装
- 技能
- 任务
- 制作/维修

### Raid Scene

职责：

- 生成局内角色
- 挂接 JUTPS 角色/AI/载具系统
- 装载掉落、敌人、商人点、撤离点

## 8. 存档方案建议

不建议把最终 Meta 进度直接压到 JUTPS `JUSaveLoad` 上。

建议拆成：

- `ProfileSave.json`
  - 玩家等级、属性、技能、商人、仓库、任务
- `RaidCheckpoint.json`
  - 中断恢复或开发调试用
- `Settings.json`
  - 图像、音量、输入等

再配合：

- 版本号
- 迁移器
- 备份文件

JUTPS 的 `JUSaveLoad` 可以留给：

- Demo 场景验证
- 临时对象状态保存
- 少量局内原型

## 9. 掉落与地图配置方案

每张地图至少要有：

- `MapDefinition`
- `GlobalLootTable`
- `UniqueLootTable`
- `SpawnPointSet`
- `ExtractionRuleSet`
- `InRaidVendorDefinition`

掉落生成建议分三层：

1. 地图基础掉落
2. 区域专属掉落
3. 容器/事件/Boss 掉落

## 10. 商人与任务系统建议

### MerchantRuntimeState

至少包含：

- 是否已认识
- 是否可交易
- 当前等级
- 当前声望
- 已解锁页签
- 动态库存刷新时间

### QuestRuntimeState

至少包含：

- 接取状态
- 目标进度
- 失败状态
- 奖励领取状态
- 关联商人关系变化

### InRaidVendor

建议做成 `JUInteractable` 适配器：

- 允许局内交易
- 用代币、现金或临时关系结算
- 可售卖弹药、医疗、情报

## 11. 属性、技能、Buff 设计建议

建议统一采用 Modifier 管线。

### Modifier 来源

- 基础属性
- 技能
- Buff
- 装备
- 伤病
- 地图环境

### 统一求值器

建立：

- `Modifier`
- `ModifierSource`
- `StatResolver`

应用场景：

- 移动速度
- 负重上限
- 后坐力
- 装填速度
- 治疗速度
- 听觉距离
- 搜刮速度
- 交易价格

## 12. 首期开发顺序建议

### M0：底座验证

- 导入并稳定 JUTPS
- 选定 FPS 主视角方案
- 跑通角色、武器、AI、拾取、撤离

### M1：局内闭环

- 自定义掉落容器
- 自定义结算
- 基础任务目标
- 基础撤离点

### M2：局外闭环

- Profile
- 仓库
- 商人
- 基础任务日志
- 配装界面

### M3：RPG 纵深

- 属性
- 技能
- Buff / 伤病
- 装备修正
- 高阶商人解锁

## 13. 当前已知技术风险

### 风险 1：JUTPS 库存过于简化

结论：

- 不能作为最终仓库方案

### 风险 2：JUTPS UI 不符合项目正式 UI 路线

结论：

- 只能参考，不能继续直接扩建

### 风险 3：JUTPS 存档适合场景对象，不适合大型 Meta

结论：

- 必须自建 Profile 存档

### 风险 4：角色核心脚本过重

结论：

- 不宜持续把 RPG 业务塞进 `JUCharacterControllerCore`
- 应通过 Bridge 和 Modifier 层扩展

## 14. 最终建议

对这个项目来说，最合理的技术方向不是“完全重写”，也不是“完全继承 JUTPS”，而是：

- 把 JUTPS 作为局内动作战斗执行层
- 把 Project-XX 做成局内外规则、数据和界面的真正主框架

换句话说：

- `JUTPS = 身体与武器`
- `Project-XX = 规则、成长、经济、内容和产品化结构`
