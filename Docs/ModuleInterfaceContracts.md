# Project-XX 模块接口约定

## 1. 文档目的

本文档用于约束当前原型工程中各模块的“对外接口”和“允许负责的事情”。

它不是 C# 语言接口清单，而是工程边界约定。

核心目标：

- 防止职责继续堆到少数脚本里
- 明确哪些模块能直接互调
- 明确哪些模块只能通过中间层访问

---

## 2. 总体原则

### 2.1 单一职责

一个模块对外只应暴露一种主职责。

例子：

- `PrototypeProfileService` 负责 Profile 读写，不负责菜单布局
- `PrototypeUnitVitals` 负责生命结算，不负责输入
- `PlayerInteractor` 负责交互查询，不负责拾取后的 UI 布局

### 2.2 只向下依赖

模块应该优先依赖更稳定、更底层的对象：

- 控制器依赖领域对象
- UI 依赖控制器或领域对象
- 数据定义不依赖运行时控制器

### 2.3 通过状态，不通过场景假设

尽量不要写这种逻辑：

- “主菜单里一定有某个对象名”
- “SampleScene 一定只有一个某类对象”

能通过显式引用或服务获取的，就不要通过场景假设硬找。

---

## 3. Profile 模块接口约定

### 3.1 模块职责

模块：

- `PrototypeItemCatalog`
- `PrototypeProfileService`
- `PrototypeMainMenuController`
- `PrototypeRaidProfileFlow`

### 3.2 对外提供的能力

`PrototypeItemCatalog`

- 根据 `itemId` 查询 `ItemDefinition`
- 提供默认 Stash / Loadout 预设

`PrototypeProfileService`

- 读取 Profile
- 保存 Profile
- Inventory 与 Profile 记录互转
- 将战利品并回 Stash

`PrototypeMainMenuController`

- 操作局外 Stash / Loadout
- 进入战斗

`PrototypeRaidProfileFlow`

- 把局外 Loadout 装到局内背包
- 把局内结果回写到局外

### 3.3 不应负责的事情

Profile 模块不应：

- 直接进行战斗伤害结算
- 直接生成枪械命中
- 直接判断 AI 行为
- 直接依赖主菜单视觉资源细节

### 3.4 推荐调用方式

- 局外 UI 调 `PrototypeProfileService`
- 局内结算桥接调 `PrototypeProfileService`
- 其他运行时系统如果只需要物品定义，调 `PrototypeItemCatalog`

不要：

- 让 `RaidGameMode` 直接手改 Profile JSON
- 让 `PrototypeFpsController` 直接写存档

---

## 4. 战局模块接口约定

### 4.1 模块职责

模块：

- `RaidGameMode`
- `ExtractionZone`

### 4.2 对外提供的能力

`RaidGameMode`

- 当前战局状态
- 战局计时
- 战局结果
- 状态变化事件

`ExtractionZone`

- 撤离交互入口
- 请求战局尝试撤离

### 4.3 不应负责的事情

战局模块不应：

- 管玩家射击逻辑
- 管 Inventory 数据结构
- 管具体 AI 类型差异
- 直接生成 UI 焦点拥有者之外的复杂交互

### 4.4 当前允许的例外

`RaidGameMode` 当前会：

- 占用 `PlayerInteractionState`
- 直接管理结果 UI 光标状态

这是因为结算界面目前还在 IMGUI 原型阶段。  
后续若迁移到正式 UI，可把这层交给独立 `RaidResultPresenter`。

---

## 5. 玩家控制模块接口约定

### 5.1 模块职责

模块：

- `PrototypeFpsInput`
- `PrototypeFpsController`
- `PlayerInteractionState`

### 5.2 对外提供的能力

`PrototypeFpsInput`

- 提供统一输入状态读取

`PrototypeFpsController`

- 提供玩家移动、视角、战斗、医疗控制
- 读取 `PrototypeUnitVitals` 的惩罚状态

`PlayerInteractionState`

- 提供统一 UI 焦点占用接口

### 5.3 不应负责的事情

玩家控制模块不应：

- 直接写 Profile
- 直接决定战局胜负
- 直接持久化背包
- 直接知道主菜单如何布局

### 5.4 重点约束

任何需要占用鼠标和停止玩家控制的 UI，都应该通过：

- `PlayerInteractionState.SetUiFocused(owner, focused)`

不要新增一套平行的光标锁定状态。

这是当前必须严格保护的接口约定。

---

## 6. 单位与伤害模块接口约定

### 6.1 模块职责

模块：

- `PrototypeUnitDefinition`
- `PrototypeUnitVitals`
- `PrototypeUnitHitbox`
- `PrototypeStatusEffectController`

### 6.2 对外提供的能力

`PrototypeUnitDefinition`

- 描述部位拓扑与规则

`PrototypeUnitVitals`

- 接收伤害
- 返回生命/护甲/体力状态
- 发出死亡和战斗反馈事件

`PrototypeUnitHitbox`

- 把物理命中转成 `partId` 伤害

`PrototypeStatusEffectController`

- 应用、维护、清除状态效果

### 6.3 不应负责的事情

伤害模块不应：

- 读取原始输入
- 知道某个按钮映射
- 知道主菜单或场景切换
- 决定 AI 状态机

### 6.4 当前扩展规则

新增特殊怪物或特殊部位时：

- 优先扩 `PrototypeUnitDefinition`
- 其次扩 `PrototypeUnitHitbox`
- 最后才考虑改 `PrototypeUnitVitals`

不要一上来就在 `PrototypeUnitVitals` 写大量 `if (bossType)`。

---

## 7. 物品与背包模块接口约定

### 7.1 模块职责

模块：

- `ItemDefinition` 体系
- `ItemInstance`
- `InventoryContainer`

### 7.2 对外提供的能力

`ItemDefinition`

- 提供静态配置

`ItemInstance`

- 提供运行时栈信息

`InventoryContainer`

- 增删物品
- 转移物品
- 统计重量与格数

### 7.3 不应负责的事情

背包模块不应：

- 决定 UI 按钮怎么排
- 决定战利品是否算撤离成功
- 直接知道 AI、战局或菜单逻辑

### 7.4 当前允许的边界

允许：

- 主菜单复用 `InventoryContainer`
- 局内背包复用 `InventoryContainer`
- 箱子复用 `InventoryContainer`

不允许：

- 为每个场景单独做一套平行容器实现

---

## 8. 交互模块接口约定

### 8.1 模块职责

模块：

- `IInteractable`
- `PlayerInteractor`
- `GroundLootItem`
- `LootContainer`
- `ExtractionZone`

### 8.2 对外提供的能力

`PlayerInteractor`

- 查询当前视线目标
- 调用目标交互

`IInteractable`

- 为具体交互对象提供统一接口

### 8.3 约定

新增局内交互对象时，优先实现：

- `IInteractable`

不要在 `PlayerInteractor` 里继续堆：

- `if (isDoor)`
- `if (isCorpse)`
- `if (isWorkbench)`

否则交互器会持续膨胀。

---

## 9. AI 模块接口约定

### 9.1 模块职责

模块：

- `PrototypeBotController`
- `PrototypeCombatNoiseSystem`

### 9.2 对外提供的能力

`PrototypeBotController`

- AI 的感知、移动、攻击

`PrototypeCombatNoiseSystem`

- 噪声广播总线

### 9.3 不应负责的事情

AI 模块不应：

- 决定主菜单或仓库
- 直接写存档
- 直接管理战局 UI

### 9.4 扩展约定

新增敌人类型时优先顺序：

1. 扩 archetype 参数
2. 扩数据定义或配置入口
3. 最后才新增完全独立 AI 控制器

只有当行为模型明显不同到无法共存时，才新建第二类 AI 控制器。

---

## 10. 表现层接口约定

### 10.1 模块职责

模块：

- `PrototypeCombatTextController`
- `PrototypeTargetHealthBar`
- 各类 IMGUI 面板

### 10.2 对外能力

表现层应该：

- 读取状态
- 订阅事件
- 显示反馈

### 10.3 不应负责的事情

表现层不应：

- 改写核心生命值
- 重新做一套伤害计算
- 重新决定交互是否成立

### 10.4 典型正确方式

正确：

- `CombatTextController` 订阅 `Vitals` 反馈事件
- `HealthBar` 读取目标当前生命归一化

错误：

- `CombatTextController` 自己去决定“这次该算多少伤害”

---

## 11. Editor 模块接口约定

### 11.1 模块职责

模块：

- `PrototypeIndoorSceneBuilder`
- `PrototypeMainMenuSceneBuilder`

### 11.2 对外能力

Editor Builder 应负责：

- 搭建原型场景
- 创建默认原型资产
- 写入基础引用

### 11.3 不应负责的事情

Editor Builder 不应：

- 承担运行时逻辑
- 成为唯一数据源
- 写只有运行时才能读的临时状态

### 11.4 当前约定

Builder 生成的是：

- 默认场景
- 默认原型资产

不是：

- 玩家运行后的持久化状态

---

## 12. 当前最重要的边界

如果只保 4 条最重要的边界，建议记住这四条：

1. `PlayerInteractionState` 是唯一 UI 焦点入口
2. `PrototypeUnitVitals` 是唯一核心伤害结算入口
3. `PrototypeProfileService` 是唯一 Profile 读写入口
4. `InventoryContainer` 是局内外共享的唯一通用容器入口

只要这四条不被打破，系统继续扩展时仍然可控。
