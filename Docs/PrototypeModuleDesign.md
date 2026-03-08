# Project-XX 原型模块设计文档

## 1. 文档目的

本文档用于整理当前 `Project-XX` 原型工程的模块划分、职责边界、关键数据流和后续扩展方向。

配套细分文档：

- [InRaidCombatSystemDesign.md](D:/UnityProject/Project-XX/Project-XX/Docs/InRaidCombatSystemDesign.md)
- [MetaProfileAndWarehouseDesign.md](D:/UnityProject/Project-XX/Project-XX/Docs/MetaProfileAndWarehouseDesign.md)
- [ArchitectureAndDataflowReference.md](D:/UnityProject/Project-XX/Project-XX/Docs/ArchitectureAndDataflowReference.md)
- [ModuleInterfaceContracts.md](D:/UnityProject/Project-XX/Project-XX/Docs/ModuleInterfaceContracts.md)
- [RefactorRoadmap.md](D:/UnityProject/Project-XX/Project-XX/Docs/RefactorRoadmap.md)

当前工程目标不是完整产品，而是一个可反复迭代的单机搜打撤原型。因此模块设计以：

- 单机场景闭环
- 快速验证 Gameplay
- 数据驱动的扩展能力
- 尽量少的系统耦合

为优先。

---

## 2. 当前场景结构

### 2.1 主场景

- `Assets/Scenes/MainMenu.unity`
  - 局外入口
  - 提供主菜单、仓库、带入配置
  - 由 `PrototypeMainMenuController` 驱动

- `Assets/Scenes/SampleScene.unity`
  - 局内战斗原型场景
  - 包含玩家、AI、拾取物、搜刮箱、撤离点、战局状态机
  - 由 `PrototypeIndoorSceneBuilder` 生成/重建

### 2.2 场景生成器

- `Assets/Res/Scripts/UI/Editor/PrototypeMainMenuSceneBuilder.cs`
  - 生成 `MainMenu.unity`
  - 生成/更新 `PrototypeItemCatalog.asset`
  - 更新 Build Settings

- `Assets/Res/Scripts/FPS/Editor/PrototypeIndoorSceneBuilder.cs`
  - 生成 `SampleScene.unity`
  - 搭建关卡、玩家、AI、拾取、箱子、撤离区
  - 挂载战局桥接组件

结论：

- 当前两张核心场景都不是手工长期维护，而是“脚本可重建”的原型场景。
- 场景本身更像“运行时验证结果”，不是唯一真相。

---

## 3. 模块总览

当前工程可以按 8 个模块理解：

1. 局外资料与主菜单
2. 战局状态与场景流转
3. 玩家控制与输入
4. 单位生命/护甲/状态系统
5. 武器与伤害数据定义
6. 背包、拾取、搜刮与交互
7. AI 感知与攻击行为
8. 表现层与调试辅助

依赖方向建议理解为：

`Data Definitions -> Runtime Systems -> Presentation/UI`

而不是反过来。

---

## 4. 局外资料与主菜单模块

### 4.1 核心职责

该模块负责：

- 管理局外仓库
- 管理 staged loadout（带入配置）
- 进入战斗前保存资料
- 局内结算后回写仓库

### 4.2 关键脚本

- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
  - 主菜单 UI 入口
  - 管理 `Home / Stash` 两个页签
  - 持有两个运行时 `InventoryContainer`
    - `Stash`
    - `Raid Loadout`
  - 负责从 Profile 加载、回写、进战斗

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
  - Profile 的 JSON 持久化服务
  - 保存内容：
    - `stashItems`
    - `loadoutItems`
  - 负责：
    - 读取磁盘
    - 生成默认 Profile
    - Inventory 与序列化记录之间互转
    - 撤离后将战利品并回 Stash

- `Assets/Res/Scripts/Profile/PrototypeItemCatalog.cs`
  - ItemDefinition 的运行时索引表
  - 提供：
    - `itemId -> ItemDefinition`
    - 默认 Stash
    - 默认 Loadout

### 4.3 资产

- `Assets/Resources/PrototypeItemCatalog.asset`
  - 局外和局内共用的物品目录入口

### 4.4 当前规则

- 进入战斗时，玩家背包由 `loadoutItems` 装填
- 结算时：
  - `Extracted`：当前背包物品并回 `stashItems`
  - `Failed / Expired`：清空 `loadoutItems`

这是一套偏原型化的“带入/带出”规则，重点是验证完整闭环，而不是模拟完整经济系统。

---

## 5. 战局状态与场景流转模块

### 5.1 核心职责

该模块负责：

- 战局开始、运行、失败、超时、撤离成功
- 撤离条件校验
- 局内结果显示
- 从战局返回主菜单

### 5.2 关键脚本

- `Assets/Res/Scripts/Raid/RaidGameMode.cs`
  - 战局状态机
  - 状态：
    - `Idle`
    - `Running`
    - `Extracted`
    - `Failed`
    - `Expired`
  - 负责计时、监听玩家死亡、展示结算面板
  - 结算状态下会自动接管 `PlayerInteractionState`，保证鼠标可用

- `Assets/Res/Scripts/Raid/ExtractionZone.cs`
  - 撤离区交互对象
  - 通过 `IInteractable` 接入玩家交互系统

- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`
  - 战局与局外 Profile 的桥接层
  - 负责：
    - 开局把 loadout 装进玩家背包
    - 结算时回写 Profile
    - 显示 `Return To Menu`

### 5.3 当前流转

`MainMenu -> SampleScene -> 结算 -> MainMenu`

其中：

- 从主菜单进入战斗由 `SceneManager.LoadScene("SampleScene")`
- 从战局返回菜单由 `SceneManager.LoadScene("MainMenu")`

---

## 6. 玩家控制与输入模块

### 6.1 核心职责

该模块负责：

- 第一人称移动与视角
- 近似 CS 风格的跳跃/空中控制
- 体力、冲刺、蹲伏
- 武器切换、射击、换弹、近战
- 医疗快捷键

### 6.2 关键脚本

- `Assets/Res/Scripts/FPS/PrototypeFpsInput.cs`
  - 输入封装层
  - 统一暴露：
    - 移动
    - 视角
    - 射击
    - 跳跃
    - 蹲下
    - 冲刺
    - 交互
    - 背包
    - 医疗快捷键

- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`
  - 玩家核心控制器
  - 负责：
    - 移动与空中控制
    - 蹲下/站起时 `CharacterController` 高度调整
    - 体力消耗与恢复
    - 武器逻辑
    - 近战
    - 射击命中
    - HUD 显示
    - 向 AI 广播噪声

### 6.3 输入/UI 焦点协作

该模块与 `PlayerInteractionState` 配合：

- 当 UI 焦点被占用时
  - 视角停止
  - 鼠标不锁定
  - 不处理战斗/移动输入

这套机制被：

- 背包界面
- 搜刮界面
- 战局结算界面

共同复用。

---

## 7. 单位生命、护甲与状态模块

### 7.1 核心职责

该模块负责：

- 部位血量
- 护甲覆盖区与耐久
- 穿深与护甲伤
- 溢出伤害
- 单位级 Buff / Debuff
- 死亡事件

### 7.2 关键脚本

- `Assets/Res/Scripts/FPS/PrototypeUnitDefinition.cs`
  - 单位部位拓扑定义
  - 定义：
    - 部位 ID
    - 最大血量
    - 溢出倍率
    - 是否计入总生命
    - 是否接收溢出
    - 黑掉后的致死规则

- `Assets/Res/Scripts/FPS/PrototypeUnitVitals.cs`
  - 单位运行时生命系统核心
  - 负责：
    - 直接命中结算
    - 护甲吸收/耐久损耗
    - 穿深与甲损
    - 溢出路由
    - 体力
    - 最后伤害来源记录
    - 战斗飘字反馈事件

- `Assets/Res/Scripts/FPS/PrototypeStatusEffectController.cs`
  - 单位级状态效果控制器
  - 当前效果包括：
    - 轻流血
    - 重流血
    - 骨折
    - 止痛

- `Assets/Res/Scripts/FPS/PrototypeUnitHitbox.cs`
  - 命中盒
  - 将碰撞命中映射到 `partId`
  - 可支持外层部位打空后转发到内部部位

### 7.3 设计原则

- “部位系统”只负责解剖结构和伤害分配
- “状态效果”改为单位级系统，不再硬绑某个部位
- 这是为了给后续 RPG 化效果留出统一入口

---

## 8. 武器与伤害数据模块

### 8.1 核心职责

该模块负责：

- 武器定义
- 子弹定义
- 护甲定义
- 医疗物定义

### 8.2 关键脚本

- `Assets/Res/Scripts/Items/Definitions/ItemDefinition.cs`
  - 所有物品定义的基础类

- `Assets/Res/Scripts/Items/Definitions/PrototypeWeaponDefinition.cs`
  - 枪械/近战武器数据
  - 包含：
    - 弹匣容量
    - 射速
    - 开火模式
    - 散布
    - 近战伤害与范围

- `Assets/Res/Scripts/Items/Definitions/AmmoDefinition.cs`
  - 子弹伤害、穿深、甲损、Debuff 概率

- `Assets/Res/Scripts/Items/Definitions/ArmorDefinition.cs`
  - 覆盖部位
  - 护甲等级
  - 耐久

- `Assets/Res/Scripts/Items/Definitions/MedicalItemDefinition.cs`
  - 回复量、止血、夹板、止痛等医疗效果

### 8.3 数据特征

- 当前战斗逻辑已经尽量从运行时代码转向 ScriptableObject 数据
- 枪、弹、甲、医疗物都属于可替换配置，而不是写死在控制器里

---

## 9. 背包、拾取、搜刮与交互模块

### 9.1 核心职责

该模块负责：

- 物品栈与背包容器
- 地面掉落
- 可搜刮箱子
- 统一交互接口

### 9.2 关键脚本

- `Assets/Res/Scripts/Inventory/InventoryContainer.cs`
  - 通用容器
  - 支持：
    - 加物品
    - 移除物品
    - 容器间转移
    - 重量限制
    - 栈数量限制

- `Assets/Res/Scripts/Items/Runtime/ItemInstance.cs`
  - 运行时物品栈

- `Assets/Res/Scripts/Interaction/IInteractable.cs`
  - 所有交互对象统一接口

- `Assets/Res/Scripts/Interaction/PlayerInteractor.cs`
  - 玩家视线交互检测
  - 负责提示与交互调用

- `Assets/Res/Scripts/Loot/GroundLootItem.cs`
  - 地面掉落物
  - 可直接拾取
  - 也承担“背包丢弃后生成地面物”的职责

- `Assets/Res/Scripts/Loot/LootContainer.cs`
  - 可搜刮容器

- `Assets/Res/Scripts/Loot/LootContainerWindowController.cs`
  - 箱子搜刮 UI

- `Assets/Res/Scripts/Loot/PlayerInventoryWindowController.cs`
  - 玩家背包 UI

- `Assets/Res/Scripts/Interaction/PlayerInteractionState`
  - 统一 UI 焦点状态

### 9.3 当前特点

- 局内和局外都复用 `InventoryContainer`
- 当前 UI 仍然是 IMGUI 原型级界面
- 还没有进入格子化仓库、拖拽拆分、装备栏等复杂阶段

---

## 10. AI 模块

### 10.1 核心职责

该模块负责：

- 感知玩家
- 根据敌人 archetype 决定行为
- 执行近战或远程攻击
- 基于噪声进行警觉/追击

### 10.2 关键脚本

- `Assets/Res/Scripts/AI/PrototypeBotController.cs`
  - 当前唯一 AI 核心控制器

- `Assets/Res/Scripts/AI/PrototypeCombatNoiseSystem`
  - 全局噪声广播系统
  - 玩家移动、射击、落地、近战等都会向这里上报噪声

### 10.3 当前敌人类型

- 普通丧尸
- 警察丧尸
- 军人丧尸
- 丧尸犬

### 10.4 当前 AI 行为特征

- 视觉 / 听觉 / 嗅觉感知
- archetype 驱动的移动速度、转身速度、攻击方式
- 远程 AI 支持：
  - 攻击节奏
  - 点射 / burst
  - 锁定圆形命中区域的随机射击

### 10.5 当前取舍

AI 已经从“通用战术 Bot”收敛为“几类明确敌人 archetype”，这是为了：

- 简化配置成本
- 更适合当前丧尸题材
- 让设计预期和实现一致

---

## 11. 表现层模块

### 11.1 核心职责

负责把运行时状态反馈给玩家。

### 11.2 关键脚本

- `Assets/Res/Scripts/FPS/PrototypeTargetHealthBar.cs`
  - Target/敌人头顶总血量条

- `Assets/Res/Scripts/FPS/PrototypeCombatTextController.cs`
  - 护甲伤害 / 生命伤害 / 护甲损坏飘字

### 11.3 当前反馈内容

- 护甲命中：灰色飘字
- 护甲损坏：专用提示
- 肉体命中：红色飘字
- 目标头顶血条
- 玩家 HUD：
  - 生命
  - 体力
  - 护甲状态
  - Buff/Debuff
  - 武器信息

---

## 12. 数据与资产分布

### 12.1 核心路径

- `Assets/Res/Scripts`
  - 运行时与编辑器脚本

- `Assets/Res/Data/PrototypeFPS`
  - 原型数据资产
  - 包括：
    - UnitDefinitions
    - Items
    - Weapons

- `Assets/Resources/PrototypeItemCatalog.asset`
  - 运行时 Profile 物品目录

- `Assets/Scenes`
  - `MainMenu`
  - `SampleScene`

### 12.2 数据驱动现状

已经数据化的部分：

- UnitDefinition
- WeaponDefinition
- AmmoDefinition
- ArmorDefinition
- MedicalItemDefinition
- ItemCatalog

仍然偏脚本硬编码的部分：

- 主菜单 UI 布局
- 战局结果 UI 布局
- 当前 MainMenu / SampleScene 的原型视觉
- 部分 AI archetype 默认参数

---

## 13. 关键运行时数据流

### 13.1 局外到局内

1. `MainMenu` 启动
2. `PrototypeMainMenuController` 读取 Profile
3. 玩家在 `Stash` 和 `Raid Loadout` 间搬运物品
4. 点击 `Enter Battle`
5. 保存 Profile
6. 切到 `SampleScene`
7. `PrototypeRaidProfileFlow` 读取 `loadoutItems`
8. 用 `InventoryContainer` 生成玩家当前背包

### 13.2 局内结算回写

1. 玩家撤离 / 死亡 / 超时
2. `RaidGameMode` 改状态并显示结果
3. `PrototypeRaidProfileFlow` 监听状态变化
4. `Extracted`：
   - 当前背包并回 `stashItems`
5. `Failed / Expired`：
   - 不回收局内背包
6. 清空 `loadoutItems`
7. 保存 Profile
8. 返回 `MainMenu`

### 13.3 伤害结算

1. 玩家或 AI 发起攻击
2. 命中 `PrototypeUnitHitbox`
3. Hitbox 将命中转发到 `PrototypeUnitVitals`
4. `Vitals` 根据：
   - 部位
   - 护甲
   - 穿深
   - 溢出规则
   进行结算
5. `StatusEffectController` 处理单位级 Debuff
6. `CombatTextController` / `HealthBar` 接收反馈
7. 若死亡，`RaidGameMode` 读取最后伤害来源

### 13.4 AI 感知

1. 玩家动作调用 `PrototypeCombatNoiseSystem.ReportNoise`
2. AI 接收噪声事件
3. AI 结合：
   - 视觉
   - 听觉
   - 嗅觉（丧尸犬）
   判断目标状态
4. 进入追击或攻击

---

## 14. 当前已知设计取舍

### 14.1 UI 仍是 IMGUI

优点：

- 快速
- 改动成本低
- 适合原型期

缺点：

- 不适合复杂仓库
- 不利于高质量布局和动画
- 交互扩展成本会越来越高

建议：

- 等仓库/角色面板稳定后，再迁移到 uGUI 或 UIToolkit

### 14.2 场景由 Builder 生成

优点：

- 可重复
- 不容易手工脏改
- 适合原型快速回滚

缺点：

- 手工微调内容容易被重建覆盖

建议：

- 继续把“关卡原型场景”和“正式内容场景”分开

### 14.3 无 Assembly Definition 拆分

当前所有运行时脚本基本仍在 `Assembly-CSharp`。

建议后续按模块拆：

- `ProjectXX.Core`
- `ProjectXX.Raid`
- `ProjectXX.Profile`
- `ProjectXX.AI`
- `ProjectXX.Editor`

这样能降低编译范围并明确依赖边界。

---

## 15. 后续扩展建议

### 15.1 局外系统

优先推荐继续扩展：

- 更正式的仓库 UI
- 装备栏
- 带入确认
- 任务/角色信息页

### 15.2 局内系统

推荐扩展：

- 更多 LootContainer 类型
- 更多敌人 archetype
- 更丰富的状态效果
- 更正式的任务目标

### 15.3 技术重构

推荐在原型稳定后做：

- 拆 Assembly Definition
- 把 IMGUI 迁到 uGUI / UIToolkit
- 给 Profile / Raid / Inventory 建更明确的 DTO 与服务边界

---

## 16. 模块结论

当前工程已经形成一个相对清晰的原型架构：

- 局外：`Profile + MainMenu + Stash/Loadout`
- 局内：`RaidGameMode + FPS + AI + Loot + Extraction`
- 共用：`Inventory + Item Definitions + UnitVitals`
- 表现：`HUD + HealthBar + CombatText`
- 生成：`Scene Builders`

如果后续继续迭代，建议尽量遵守一个原则：

**新增玩法优先落在“数据定义”或“独立控制器”里，不要继续把逻辑堆进 `PrototypeFpsController` 或 `PrototypeUnitVitals`。**

这样这套原型才能继续扩而不塌。
