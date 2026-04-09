# JUTPS 系统拆解 08：存档、场景管理与 UI

## 1. 模块范围

本篇把三个相邻模块放在一起看：

- `JU Save Load`
- `Scene Management`
- `UI`

原因是它们共同构成了“包自带的关卡型游戏外壳”。

## 2. 存档系统架构

### 底层：`JUSaveLoad`

路径：

- `Assets/Julhiecio TPS Controller/JU Save Load/Scripts/JUSaveLoad.cs`

职责：

- 维护整份存档数据
- 区分：
  - `GlobalData`
  - `SceneData`
- 用 JSON 序列化后再做 AES 加密
- 写入 `Saves/Save.bin`
- 支持备份文件

特点：

- 这是一个轻量但完整的本地单机存档方案
- 能满足 demo、单机原型、关卡进度保存

### 存档组件基类：`JUSaveLoadComponent`

路径：

- `Assets/Julhiecio TPS Controller/JU Save Load/Scripts/JUSaveLoadComponent.cs`

职责：

- 组件化地保存与读取数据
- 统一生成 Key
- 根据 `SaveLoadModes` 决定写到 Scene 还是 Global
- 在 `Awake` 时读档，在销毁时尝试存档

### 存档管理器：`JUSaveLoadManager`

职责：

- 注册需要存档的对象
- `SaveOnFile()` 时先让所有对象同步当前状态，再真正写盘

### 业务适配组件

包里已经提供了一系列适配器：

- `JUSaveLoadCharacter`
- `JUSaveLoadWeapon`
- `JUSaveLoadArmor`
- `JUSaveLoadThrowableItem`
- `JUSaveLoadWheeledVehicle`

说明：

- 包的存档扩展方式是“给具体系统写一个 SaveLoad Adapter 组件”

## 3. 场景管理

### `SceneController`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Scene Management/SceneController.cs`

职责：

- 记录玩家复活点
- 死亡后重生或重载场景
- 重置角色状态、布娃娃、碰撞体、动画、库存标志

### 其他场景脚本

- `LevelLoader`
- `SimpleLevelTransition`
- `QuitApplication`

整体定位很明确：

- 这是关卡型单机场景外壳
- 不是大型 Meta 游戏的流程框架

## 4. UI 系统

### 包内 UI 的特点

代表脚本：

- `JU_UIPause`
- `JU_UISettings`
- `InventoryUIManager`
- `UIInteractMessages`
- `Crosshair`

特点：

- 主要基于 UGUI prefab
- 通过组件引用和运行时 Instantiate/刷新条目工作
- 偏“一个系统一个 screen/controller”的传统写法

### `InventoryUIManager`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Inventory System/Inventory UI/InventoryUIManager.cs`

特征：

- 会根据库存动态实例化 Slot prefab
- 能做 Loot View
- 能在打开背包时控制鼠标与角色移动

它对原型很实用，但不适合直接承载你后续的复杂局外仓库。

## 5. 对 Project-XX 的主要判断

### 存档层

短期可以用来：

- 做局内原型档
- 保存角色位置、装备、场景状态

但不建议直接作为最终方案承载：

- 多地图 Meta 档案
- 商人关系
- 任务链
- 经济系统
- 技能树与 Buff 历史

原因：

- 它是组件 Key-Value 风格存档
- 更适合场景对象同步
- 不适合复杂账户档案与版本迁移

### 场景层

短期可以用来：

- 单个战斗场景死亡重开
- 样机地图切换

但你后续还需要自己建立：

- 局外 Hub 流程
- 进图/撤离结算流程
- 商人和仓库场景流转
- 地图解锁与入口规则

### UI 层

最关键的项目约束是：

- Project-XX 已经明确规定新的 runtime UI 必须遵守 `UiProductionStandard`

因此结论非常明确：

- JUTPS 自带 UI 可以参考和临时借用
- 但 Project-XX 后续正式 UI 不能继续照它的旧组织方式无节制扩展

## 6. 对 Project-XX 的 UI 接入建议

建议这样做：

1. 保留 JUTPS 的战斗底层事件与状态。
2. 在 Project-XX 内用自己的 `ViewBase / WindowBase / *Template` 重做正式 UI。
3. 用 Presenter/Bridge 把：
   - 角色生命
   - 弹药
   - 交互提示
   - 背包内容
   - 商店内容
   - 任务内容
   接到项目自己的 UGUI prefab。

不要做的事：

- 不要把 JUTPS 的 Pause/Inventory UI 直接当最终 UI 继续堆功能
- 不要再新建 runtime IMGUI
- 不要把复杂仓库布局继续写在旧 UI 控制器里

## 7. 总结

这个模块对你最有用的不是“直接拿来做最终游戏”，而是：

- 作为原型流程参考
- 作为角色状态如何同步到 UI 的参考
- 作为场景对象型存档适配的参考

最终正式项目里，建议：

- 场景内对象状态同步可以借鉴 JUSaveLoad 的组件化思想
- Meta 档案、商人、任务、经济请单独设计
- 新正式 UI 全部走 Project-XX 自己的 prefab 规范
