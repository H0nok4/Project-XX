# 模块接口与边界约定

## 1. 文档目的

本文档不是类图，而是用于规定：

- 哪个模块应该负责什么
- 哪个模块不应该负责什么
- 模块之间允许如何协作

这些约定的目的是避免“功能能跑，但职责失控”。

---

## 2. 全局约束

### 2.1 单一结算来源

- 生命与伤害结算只认 `PrototypeUnitVitals`
- 状态效果结算只认 `PrototypeStatusEffectController`
- Profile 读写只认 `PrototypeProfileService`

禁止：

- UI 自己改血量
- AI 直接绕过 `PrototypeUnitVitals` 扣血
- 任意模块直接手写 JSON 存档

### 2.2 输入集中入口

- 玩家输入只应通过 `PrototypeFpsInput` 暴露给运行时系统

禁止：

- 在多个运行时模块中直接硬读 `Keyboard.current`
- UI 逻辑直接改动玩家移动状态而不通过焦点系统

### 2.3 UI 焦点统一管理

- 所有会占用玩家控制权的界面都应通过 `PlayerInteractionState`

禁止：

- 某个界面只自己显示鼠标，但不登记 UI 焦点
- 结算、背包、搜刮各自搞一套输入冻结逻辑

### 2.4 原型构建器不是业务真相

- Builder 可以生成默认资产和样例场景
- Builder 不是运行时逻辑真相来源

禁止：

- 把关键运行时规则只写在 Builder 中
- 依赖“必须先重建场景才能工作”的隐藏规则

---

## 3. 模块边界

## 3.1 Profile 模块

核心：

- `PrototypeProfileService`
- `PrototypeItemCatalog`
- `PrototypeMerchantCatalog`

负责：

- Profile 的读取、清洗、保存
- 定义级目录查询
- 商店目录与价格定义

不负责：

- 局内运行时背包控制
- UI 表现
- 战斗结算

允许被调用者：

- `PrototypeMainMenuController`
- `PrototypeRaidProfileFlow`

## 3.2 Main Menu / Meta UI 模块

核心：

- `PrototypeMainMenuController`

负责：

- 组织局外页面
- 调用 Profile 服务
- 调用仓库 / 商店操作

不负责：

- 自己决定战斗死亡规则
- 自己计算护甲、血量、战斗逻辑

约定：

- 这里只能操作资料层与容器层
- 不应出现局内战斗细节结算

## 3.3 Raid 桥接模块

核心：

- `PrototypeRaidProfileFlow`
- `RaidGameMode`

负责：

- 把局外配置应用到局内
- 把局内结果回写局外
- 控制战局生命周期

不负责：

- 决定具体伤害数值
- 直接驱动背包 UI

约定：

- `PrototypeRaidProfileFlow` 是场景桥接层，不是库存系统本体

## 3.4 玩家控制模块

核心：

- `PrototypeFpsInput`
- `PrototypeFpsController`

负责：

- 玩家移动、武器、医疗、视角、噪声

不负责：

- Profile 落盘
- 商店逻辑
- 直接操作 AI 状态机

约定：

- 需要扣血时，必须走 `PrototypeUnitVitals`
- 需要产生噪声时，必须走噪声系统

## 3.5 单位生命与状态模块

核心：

- `PrototypeUnitDefinition`
- `PrototypeUnitVitals`
- `PrototypeUnitHitbox`
- `PrototypeStatusEffectController`

负责：

- 结构化伤害
- 护甲结算
- 状态效果
- 死亡来源

不负责：

- 玩家输入
- AI 决策
- UI 页面布局

约定：

- 所有命中最终都应归并到 `ApplyDamage`
- 所有单位级 Debuff 都应归并到 `PrototypeStatusEffectController`

## 3.6 AI 模块

核心：

- `PrototypeBotController`
- `PrototypeEnemySpawnProfile`
- `PrototypeEnemyRuntimeFactory`
- `PrototypeEncounterDirector`

负责：

- 敌人感知
- 敌人追击 / 搜索 / 攻击
- 敌人生成

不负责：

- 定义伤害公式
- 自己创建一套独立生命系统
- 自己实现独立背包容器

约定：

- AI 攻击命中玩家时必须走和玩家相同的伤害结算链
- AI 掉落与尸体搜刮必须复用 Loot / Inventory 体系

## 3.7 Loot / Inventory / Interaction 模块

核心：

- `InventoryContainer`
- `PlayerInteractor`
- `GroundLootItem`
- `LootContainer`
- `LootContainerWindowController`
- `PlayerInventoryWindowController`
- `PrototypeCorpseLoot`

负责：

- 容器存取
- 交互查询
- 地面拾取与搜刮 UI

不负责：

- 存档结构定义
- 战斗伤害
- AI 决策

约定：

- 所有容器都尽量复用 `InventoryContainer`
- 所有可交互对象都尽量实现 `IInteractable`

## 3.8 Level Design / Editor 模块

核心：

- `PrototypeIndoorSceneBuilder`
- `PrototypeMainMenuSceneBuilder`
- `PrototypeRaidToolkitWindow`

负责：

- 维护原型场景与默认资产
- 提供关卡原型放置工具

不负责：

- 运行时判定
- 正式生产内容唯一真相

---

## 4. 当前最重要的接口契约

### 契约 1：伤害入口唯一

任何直接攻击、爆炸、近战、DOT 最终都应归并到：

- `PrototypeUnitVitals.ApplyDamage(...)`

### 契约 2：状态效果入口唯一

任何持续伤害、减速、骨折、止痛等单位级状态都应归并到：

- `PrototypeStatusEffectController`

### 契约 3：存档入口唯一

任何局外持久化都应归并到：

- `PrototypeProfileService.LoadProfile(...)`
- `PrototypeProfileService.SaveProfile(...)`

### 契约 4：玩家 UI 焦点唯一

任何局内会抢占玩家操作的窗口都应登记：

- `PlayerInteractionState`

---

## 5. 目前最容易越界的地方

### 5.1 `PrototypeFpsController`

风险：

- 容易继续吸收 HUD、输入、医疗、武器、噪声、姿态等更多逻辑

要求：

- 新增逻辑前先判断是否属于移动、战斗还是表现

### 5.2 `PrototypeBotController`

风险：

- 感知、移动、攻击、掉落、尸体搜刮都在一个控制器中

要求：

- 新增敌人行为时，优先加配置，不优先继续堆控制分支

### 5.3 `PrototypeMainMenuController`

风险：

- 仓库、商店、首页、装备槽、自动存档仍集中在一个局外协调器及其运行时 UGUI 视图组合里

要求：

- 新加局外系统时优先扩展资料结构，不优先扩展这个类的 UI 细节

---

## 6. 后续重构时的执行原则

1. 先保留现有功能闭环，再拆结构
2. 先拆职责最重的控制器，再拆小模块
3. 先把数据入口统一，再升级 UI
4. 先让实例和定义分开，再做复杂经济与装备
