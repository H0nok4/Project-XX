# Project-XX 重构路线图

## 1. 文档目标

本文档不是“马上要做的功能清单”，而是“在继续迭代原型时，哪些结构需要按阶段重构”的路线图。

目标是避免两种情况：

- 功能越来越多，但结构越来越乱
- 为了重构而重构，打断当前原型迭代节奏

因此这份路线图按阶段拆，并明确每阶段的触发条件。

---

## 2. 当前结构风险判断

当前工程已经能跑完整闭环，但也出现了几个典型风险：

- `PrototypeFpsController` 职责过多
- `PrototypeBotController` 已经是重控制器
- UI 全部还是 IMGUI
- 运行时代码都堆在 `Assembly-CSharp`
- Profile 仍然是轻量堆叠存档

这些不是“现在就必须修”，但如果继续加功能而不处理，后面会迅速恶化。

---

## 3. 阶段 0：继续原型，但守住边界

### 3.1 目标

在短期内继续做功能，但不急着大拆。

### 3.2 必须遵守

- 所有 UI 焦点统一走 `PlayerInteractionState`
- 所有 Profile 读写统一走 `PrototypeProfileService`
- 所有伤害结算统一走 `PrototypeUnitVitals`
- 新交互对象统一实现 `IInteractable`

### 3.3 通过标准

如果后续新增 3 到 5 个功能，核心脚本仍然没有明显失控，就可以保持这一阶段。

---

## 4. 阶段 1：拆玩家控制器

### 4.1 触发条件

出现以下任一情况时进入阶段 1：

- `PrototypeFpsController` 再增加明显新系统
- 需要多人协作同时改玩家控制
- HUD、医疗、战斗频繁互相影响

### 4.2 建议拆分目标

把 `PrototypeFpsController` 拆成：

- `PrototypePlayerMovementController`
- `PrototypePlayerWeaponController`
- `PrototypePlayerMedicalController`
- `PrototypePlayerHudPresenter`

### 4.3 拆分原则

- Movement 只负责位移、跳跃、蹲伏、体力移动消耗
- Weapon 只负责武器槽、射击、换弹、近战
- Medical 只负责快捷医疗与消耗品
- HUD 只负责显示

### 4.4 验收标准

- 任一模块不再需要理解另外三个模块的全部实现
- `PrototypeFpsInput` 仍然保持唯一输入入口

---

## 5. 阶段 2：拆 AI 控制器

### 5.1 触发条件

出现以下任一情况时进入阶段 2：

- 需要更多敌人 archetype
- 需要远近程外的特殊行为
- AI 需要动画、扑击、投掷、召唤等特殊动作

### 5.2 建议拆分目标

把 `PrototypeBotController` 拆成：

- `BotPerception`
- `BotLocomotion`
- `BotCombat`
- `BotArchetypeProfile`

### 5.3 拆分原则

- 感知只负责发现目标
- 移动只负责追击/转身/导航
- 战斗只负责攻击节奏与命中
- archetype profile 只负责参数

### 5.4 验收标准

- 修改某类敌人的攻击节奏时，不必改感知实现
- 修改嗅觉规则时，不必碰射击逻辑

---

## 6. 阶段 3：将局外系统升级为实例化数据

### 6.1 触发条件

当你准备做以下任一内容时，必须进入阶段 3：

- 武器改装
- 单独耐久跨局保存
- 装备栏
- 物品实例属性
- 格子仓库

### 6.2 当前问题

现在 Profile 只保存：

- `itemId`
- `quantity`

这不够表达：

- 同类武器不同改装
- 同类护甲不同耐久
- 独立任务物品标记

### 6.3 建议目标

新增 DTO：

- `SavedItemStackDto`
- `SavedItemInstanceDto`
- `SavedEquipmentSlotDto`
- `SavedProfileDto`

并明确区分：

- 静态定义：`ItemDefinition`
- 运行时实例：`ItemInstance`
- 存档实例：`SavedItemInstanceDto`

### 6.4 验收标准

- 同类物品可带不同实例属性
- 存档不再只靠“类型+数量”

---

## 7. 阶段 4：UI 从 IMGUI 迁移

### 7.1 触发条件

以下任一情况成立时，应开始迁移：

- 仓库需要拖拽
- 结算页需要更复杂信息
- 局外角色页、任务页、装备页变多
- 需要更正式的视觉表现

### 7.2 迁移建议顺序

1. 主菜单
2. 仓库/Loadout
3. 局内结算
4. 局内 HUD

### 7.3 不建议的做法

不要一次性把所有 IMGUI 全删掉再重做。

建议逐页迁移，保留旧逻辑，先替换表现层。

### 7.4 验收标准

- 运行时状态仍由现有控制器/领域对象提供
- 只更换表现层，不重写核心逻辑

---

## 8. 阶段 5：Assembly Definition 拆分

### 8.1 触发条件

以下任一情况成立时建议拆 asmdef：

- 编译时间明显变长
- 多人并行改动冲突多
- 编辑器工具越来越多
- 运行时与编辑器引用关系变复杂

### 8.2 建议目标

拆为：

- `ProjectXX.Core`
- `ProjectXX.Gameplay`
- `ProjectXX.Meta`
- `ProjectXX.Presentation`
- `ProjectXX.Editor`

### 8.3 建议原则

- `Editor` 只能依赖运行时模块，不能反向
- `Core` 不依赖 `Presentation`
- `Meta` 不依赖 `Gameplay` 的具体实现细节

### 8.4 验收标准

- 改 UI 不应重新编译全部 Editor builder
- 改 Profile 不应影响 AI 编译范围

---

## 9. 阶段 6：正式内容与原型场景分离

### 9.1 触发条件

当你开始做正式关卡内容、美术资源和长期摆放时，应进入这一阶段。

### 9.2 当前问题

现在 `MainMenu` 和 `SampleScene` 都可由 Builder 重建。

这对原型很好，但对正式内容有风险：

- 手工改动可能被覆盖
- 内容场景难以长期维护

### 9.3 建议目标

区分：

- `Prototype` 场景
- `Production` 场景

例如：

- `MainMenu_Prototype`
- `SampleScene_Prototype`
- `MainMenu_Production`
- `RaidBlockout_Production`

### 9.4 验收标准

- Builder 只负责原型场景
- 正式内容不再被 Builder 覆盖

---

## 10. 近期最合理的重构顺序

按当前项目状态，我建议优先级如下：

1. 拆 `PrototypeFpsController`
2. 引入装备栏数据结构
3. 升级 Profile 到实例化保存
4. 将主菜单/仓库迁移到正式 UI
5. 拆 AI 控制器
6. 拆 asmdef

这个顺序的原因：

- 玩家控制器和局外装备系统最容易继续膨胀
- 这两个地方如果不先收，会拖慢后面所有内容开发

---

## 11. 近期不建议优先做的重构

当前不建议优先做：

- 全量重写 AI
- 全量重写 Vitals
- 先拆 asmdef 再做功能
- 先做最终 UI 美术再补数据结构

原因：

- 这些会打断当前可玩的原型节奏
- 回报不如先梳理高耦合控制器和局外数据

---

## 12. 每阶段的完成标志

### 阶段 1 完成

- `PrototypeFpsController` 不再是超级脚本

### 阶段 2 完成

- AI 感知、移动、攻击可分别改动

### 阶段 3 完成

- Profile 可保存实例级数据

### 阶段 4 完成

- 仓库与结算界面脱离 IMGUI

### 阶段 5 完成

- 项目编译边界清晰

### 阶段 6 完成

- 原型场景与正式内容场景彻底分离

---

## 13. 结论

当前最应该做的不是“全面重构”，而是“按压力点重构”。

当前最大的两个压力点是：

- 玩家控制器过重
- 局外资料结构仍偏原型级

只要按顺序把这两块先收好，后面无论是继续做单机搜打撤，还是偏 RPG 化扩展，都会顺很多。
