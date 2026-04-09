# JUTPS 系统拆解 04：武器、物品、库存与护甲

## 1. 模块范围

这一层由几个紧密关联的模块构成：

- `JUItem`
- `JUHoldableItem`
- `Weapon`
- `MeleeWeapon` / `ThrowableItem` / `GeneralHoldableObject`
- `JUInventory`
- `Armor`

本质上它是一个“以角色子物体为基础的装备与使用系统”。

## 2. 数据建模方式

### `JUItem`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Inventory System/JUItem.cs`

字段特点：

- `ItemName`
- `ItemIcon`
- `Unlocked`
- `ItemQuantity`
- `MaxItemQuantity`
- `ItemSwitchID`

说明：

- 这是非常轻量的物品基类
- 更像“带数量和图标的场景组件”
- 不是完整的数据库驱动物品定义

### `JUHoldableItem`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Libraries/Character Controller Libs/Item System Libs/ItemSystemLib.cs`

额外提供：

- 单次/持续使用控制
- 左右手标记
- 双持支持
- 持握 Pose
- 对侧手 IK 点
- 所属角色缓存

含义：

- 所有可持有物都是“角色手上/身上的实体对象”
- 不是纯数据 Item Instance

## 3. 武器系统

### `Weapon`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Weapon Management/Weapon.cs`

职责：

- 子弹数量与弹匣
- 射速控制
- 精度与每发扩散
- 射线判定与子弹 prefab 生成
- 枪口火焰、抛壳、音效
- Scope / CameraApproach 两种瞄准模式
- 程序化后坐力与枪机滑块动画

核心特点：

- 武器逻辑主要写在武器组件本身
- 角色层只管调用“使用物品”
- 武器可独立配置射速、命中层、弹药数、瞄准镜参数等

### 优点

- 很适合原型枪战
- 单把枪的参数都能在 prefab 上可视化调
- 和角色、相机、AI 联动已经打通

### 局限

- 没有成熟的弹药类型/穿甲/碎伤/配件系统抽象
- 没有塔科夫式弹种数据库与装弹规则
- 武器更多是场景 prefab 逻辑，不是数据库驱动武器实例

## 4. 库存系统

### `JUInventory`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Inventory System/JUInventory.cs`

关键特征：

- 通过角色子物体扫描收集：
  - `AllItems`
  - `AllHoldableItems`
  - 左手、右手持有物
- 使用 `ItemSwitchID` 和 `SequentialSlot` 做切换
- 支持拾取附近物品
- 支持装备/卸下物品
- 角色死亡后可变成 Loot 容器

这是一套“场景对象驱动”的库存系统，不是“仓库数据结构驱动”的库存系统。

### 它适合什么

- 线性武器栏
- 左右手装备
- 少量可拾取物
- 敌人死亡掉落
- 原型关卡中的拾取体验

### 它不适合什么

- 塔科夫式网格仓库
- 多层容器嵌套
- 子弹分堆/拆堆/装填到弹匣
- 背包容量、格子占用、旋转放置
- 局外仓库与角色身上负重之间的复杂转移

## 5. 护甲系统

### `Armor`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Armor System/Armor.cs`

能力：

- 可控制护甲外观部件显隐
- 可修改指定受击部位的 `DamageMultiplier`
- 可选护甲耐久/生命
- 与库存系统联动装备/卸下

这说明包的护甲是“视觉部件 + 部位减伤”的简化实现。

适合：

- 基础护甲槽位
- 简单减伤

不够：

- 护甲等级
- 耐久损耗材料
- 命中部位穿透计算
- 子弹与护甲材质互动

## 6. 运行时链路

典型链路如下：

1. 角色输入触发 `DefaultUseOfAllItems(...)`
2. 角色把“使用行为”分发给左右手物品
3. 当前物品如果是 `Weapon`，则执行射击、扩散、后坐力、音效
4. `JUInventory` 负责：
   - 持有物扫描
   - 物品启用/禁用
   - 左右手切换
   - 装备/卸下
   - 拾取附近物品
5. `Armor` 则在启用时修改受击部位与视觉部件

## 7. 对 Project-XX 的直接结论

这套系统可以作为：

- 局内武器使用层
- 局内基础拾取层
- 局内敌人掉落层
- 临时装备可视化层

但不能直接作为：

- 最终装备/仓库/经济系统
- 完整 RPG 物品体系

## 8. 推荐改造策略

建议保留：

- `Weapon` 的射击与基础瞄准/后坐力
- `JUHoldableItem` 的左右手持握模型
- `JUInventory` 的“局内快捷装备”职责
- `Armor` 的基础部位保护思路

建议重建：

- 物品数据库
- 物品实例数据
- 背包/仓库/容器结构
- 弹药与弹匣系统
- 词条、品质、稀有度、改装、任务物品

## 9. 面向搜打撤 RPG 的建议落点

推荐拆成两层：

### JUTPS 层

- 武器实体
- 手持物动作
- 基础射击
- 场景掉落交互

### Project-XX 层

- `ItemDefinition`
- `ItemInstance`
- `ContainerDefinition`
- `EquipmentSlotDefinition`
- `AmmoDefinition`
- `WeaponModDefinition`
- `ArmorDefinition`
- `LootTable`
- `VendorOffer`

然后通过桥接把数据库实例映射到 JUTPS 场景物体：

- 场景里仍然可以出现 `Weapon` prefab
- 但真正的数值、稀有度、耐久、改装、任务状态来自 Project-XX 的数据层
