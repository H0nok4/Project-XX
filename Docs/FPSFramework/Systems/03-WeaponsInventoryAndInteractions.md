# FPS Framework 系统拆解 03：武器、库存与交互

## 1. 模块范围

这一层主要覆盖：

- `Inventory`
- `InventoryItem`
- `ItemInput`
- `Firearm`
- `FirearmPreset`
- `FirearmAttachmentsManager`
- `InteractionsManager`
- `Pickable`

本质上它是一个“以玩家手持物为中心的轻量 FPS 武器与拾取系统”。

## 2. 数据建模方式

### `FirearmPreset`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/Firearm System/FirearmPreset.cs`

特点：

- 用 `ScriptableObject` 持有枪械主要参数
- 包含：
  - 射击机制
  - 射击方向
  - 命中层
  - 投射物 / 抛壳 / 贴花
  - 射速 / 伤害 / 射程 / 命中偏移
  - 喷散模式
  - 弹药类型 / 弹匣容量 / 备弹
  - 换弹方式与时间
  - 相机后坐力与镜头抖动
  - ADS FOV
  - 玩家移动速度修正
  - Fire / Reload 音频

说明：

- 它比 JUTPS 的武器参数组织更集中
- 很适合作为 Project-XX 武器定义到运行时武器表现之间的桥点

### `Inventory`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/Inventory System/Inventory.cs`

特点：

- 用 `startItems` 初始化手持物
- 通过 `items` 列表和当前 index 切换启用对象
- 支持默认物品
- 本质是“快捷栏式手持物切换”

这是一套：

- 线性插槽库存
- 手持物对象启停切换

而不是：

- 网格背包
- 多容器仓库
- 子弹分堆与复杂装填规则

## 3. 武器系统

### `Firearm`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/Firearm System/Firearm.cs`

职责：

- 读取 `ItemInput`
- 管理单发 / 连发 / 装填
- 管理喷散、备弹、弹匣、抛壳、投射物、音效
- 驱动 HUD 与 Crosshair
- 和 `FirearmAttachmentsManager`、`ProceduralAnimation` 联动

它的能力明显偏“FPS 手感层”，不是偏“数据库型武器系统”。

### `ItemInput`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/Inventory System/ItemInput.cs`

职责：

- 读取 `Firearm` 和 `Throwable` action map
- 输出 `Fire / Aim / Reload / Drop / FireModeSwitch / SightModeSwitch`

说明：

- 它把武器输入与角色移动输入拆开了
- 这很适合后续做更清楚的玩家输入桥

### 附件系统

代表脚本：

- `Attachment`
- `AttachmentMagazine`
- `AttachmentMuzzle`
- `AttachmentSight`
- `FirearmAttachmentsManager`

说明：

- 配件系统已有基础骨架
- 但更偏 FPS 原型层
- 不等于完整的搜打撤改枪数据库和改装规则系统

## 4. 交互系统

### `InteractionsManager`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/Interactions System/InteractionsManager.cs`

职责：

- 在范围内 `OverlapSphere` 查找 `IInteractable`
- 选取最近交互对象
- 显示交互 HUD
- 执行瞬时或长按交互
- 交互时取消武器换弹

这层的意义是：

- 它把交互发现与交互执行分开了
- 作为玩家局内搜刮和开门入口是够用的

### `Pickable`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/Pickable.cs`

说明：

- 包内掉落、拾取物更多是“世界对象 + 交互行为”
- 不是复杂容器系统

## 5. 这套武器与交互层的优点

优点：

- 武器手感和参数关系清楚
- `FirearmPreset` 很适合做桥接定义
- 枪械 procedural animation 已打通
- 交互发现逻辑轻量直接
- 足够支撑第一人称局内原型

## 6. 这套武器与交互层的限制

局限：

- `Inventory` 只是快捷栏，不是仓库系统
- 没有塔科夫式容器、格子、弹匣装填规则
- 交互条件系统较轻
- 缺少复杂权限 / 钥匙 / 任务阶段检查协议
- 配件系统还不是完整的经济与改枪规则框架

## 7. 对 Project-XX 的建议

建议保留：

- `Firearm`
- `FirearmPreset`
- `ItemInput`
- `FirearmAttachmentsManager`
- `InteractionsManager`

建议重建：

- `ItemDefinition`
- `AmmoDefinition`
- `MagazineDefinition`
- `InventoryGridRuntime`
- `ContainerRuntime`
- `InteractionRequirement / InteractionExecution`

推荐拆成两层：

### FPS Framework 层

- 手持武器实体
- 开火 / 后坐 / ADS / Crosshair
- 世界拾取与轻量交互

### Project-XX 层

- 物品数据库
- 物品实例数据
- 容器与仓库
- 掉落规则
- 商人经济
- 任务条件与钥匙权限

最终建议：

- `Firearm` 作为“局内执行武器”
- Project-XX 的定义与实例系统作为“局内外共享数据脑”

