# 局外资料、仓库与经济系统设计

## 1. 设计目标

局外系统当前的目标不是做完整商业化 Meta，而是支撑局内原型的带入带出闭环，并提供最基础的经济测试入口。

当前应解决的问题：

- 玩家如何在局外整理仓库
- 玩家如何配置进局装备
- 玩家如何在局外购买与出售物资
- 玩家死亡和撤离后，哪些物资保留，哪些丢失

---

## 2. 核心脚本

### 2.1 资料与目录

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/PrototypeItemCatalog.cs`
- `Assets/Res/Scripts/Profile/PrototypeMerchantCatalog.cs`

### 2.2 局外界面

- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`

### 2.3 局内桥接

- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`

### 2.4 编辑器生成

- `Assets/Res/Scripts/UI/Editor/PrototypeMainMenuSceneBuilder.cs`

---

## 3. 当前 Profile 数据结构

`PrototypeProfileService.ProfileData` 当前已包含：

- `stashItems`
- `raidBackpackItems`
- `secureContainerItems`
- `specialEquipmentItems`
- `equippedArmorItems`
- `stashWeaponIds`
- `equippedPrimaryWeaponId`
- `equippedSecondaryWeaponId`
- `equippedMeleeWeaponId`

说明：

- 这些字段仍然是“定义级 + 数量 / ID”的轻量存档结构
- 还不是实例级存档
- `loadoutItems` 和 `extractedItems` 仍保留在结构里，但当前逻辑已不依赖它们作为主路径

---

## 4. 局外容器与槽位设计

## 4.1 安全区

### Warehouse Stash

安全仓库，用于长期存放普通物资。

特点：

- 不受战局死亡影响
- 商店购买的普通物资直接进入这里
- 出售也默认从这里扣除物品、返还现金

### Weapon Locker

安全武器柜，用于长期存放未装备武器。

特点：

- 商店购买的武器直接进入这里
- 不受战局死亡影响

## 4.2 风险区

### Raid Backpack

战斗背包，是局内主拾取容器。

特点：

- 进局时会被装填到玩家主背包
- 局内搜刮默认进入这里
- 死亡时清空
- 撤离时保留当前内容

### Equipped Armor

已装备护甲属于风险区。

特点：

- 进局时直接挂到玩家身上
- 死亡时丢失
- 撤离时保留

### Equipped Primary / Secondary

已装备主武器和副武器属于风险区。

特点：

- 进局时挂到玩家武器槽
- 局内可被拾枪替换
- 死亡时丢失
- 撤离时保存当前最后持有的武器

## 4.3 保护区

### Melee

近战武器槽是保护区。

特点：

- 死亡时不会消失
- 撤离时照常保留

### Secure Container

安全箱容器是保护区。

特点：

- 不受死亡影响
- 进局时会被加载到玩家
- 当前还没有完整的局内多容器拖拽 UI，但基础容器已存在

### Special Equipment

特殊装备容器是保护区。

特点：

- 不受死亡影响
- 设计上为后续任务道具、钥匙、特殊装置等预留

---

## 5. 主菜单结构

当前主菜单有三个主要页面：

- Home
- Warehouse
- Merchants

## 5.1 Home

展示当前概要：

- 资金
- 仓库与背包概况
- 当前装备情况

并提供：

- Enter Battle
- Open Warehouse
- Visit Merchants

## 5.2 Warehouse

当前仓库页包含四块：

- Warehouse Stash
- Raid Backpack
- Weapon Locker
- Protected Gear

支持的操作包括：

- 物资从仓库装进战斗背包
- 物资放入安全箱 / 特殊装备槽
- 护甲装备与卸下
- 武器装备与回存
- 出售仓库物资、武器、已装备护甲、已装备武器

## 5.3 Merchants

商店页当前是基础经济测试入口。

默认三个商人：

- 武器商人：武器、弹药
- 药品商人：医疗物资
- 护甲商人：护甲

当前购买规则：

- 普通物资买入后进入 `Warehouse Stash`
- 武器买入后进入 `Weapon Locker`

当前出售规则：

- 现金以 `Cash Bundle` 形式存放在仓库
- 出售时会把现金加入仓库

---

## 6. 经济系统

## 6.1 货币

当前货币不是单独的数值字段，而是物品：

- `cash_bundle`

好处：

- 不需要单独维护一套货币存档字段
- 可以直接复用背包与仓库逻辑

代价：

- 货币表现仍然偏原型
- 后续如果做更复杂经济，可能要切回专门数值字段

## 6.2 商店目录

`PrototypeMerchantCatalog` 当前提供：

- 商人列表
- 物资报价
- 武器报价
- 物资回收倍率
- 武器回收倍率

当前也支持运行时兜底：

- 如果 `PrototypeMerchantCatalog.asset` 缺失
- 主菜单会根据当前物品目录自动创建一套默认商店配置

## 6.3 当前经济规则

当前规则很简单：

- 没有商人等级
- 没有刷新时间
- 没有限购
- 没有差价浮动
- 没有任务、信誉与折扣

这足够支撑“买进 / 卖出 / 配装 / 进局”的闭环测试。

---

## 7. 局内外桥接规则

## 7.1 进局

`PrototypeRaidProfileFlow` 会在战局开始时：

- 把 `raidBackpackItems` 装入玩家主背包
- 把 `secureContainerItems` 和 `specialEquipmentItems` 装入辅助容器
- 把 `equippedArmorItems` 应用到玩家 `PrototypeUnitVitals`
- 把 `equippedPrimaryWeaponId / equippedSecondaryWeaponId / equippedMeleeWeaponId` 应用到玩家武器槽

## 7.2 撤离

当战局状态为 `Extracted`：

- 当前主背包内容写回 `raidBackpackItems`
- 当前护甲写回 `equippedArmorItems`
- 当前主武器 / 副武器 / 近战写回对应武器槽
- `Secure Container` 与 `Special Equipment` 始终保留

## 7.3 死亡或超时

当战局状态不是 `Extracted`：

- `raidBackpackItems` 清空
- `equippedArmorItems` 清空
- 主武器 / 副武器清空
- 近战武器保留
- `Secure Container` 保留
- `Special Equipment` 保留

---

## 8. 当前已经支撑的 Tarkov-like 规则

当前已经具备：

- 安全仓库
- 风险战斗背包
- 保护近战槽
- 安全箱
- 特殊装备槽
- 局内搜刮进入风险背包
- 撤离后保留风险区当前结果
- 死亡后只保留保护区
- 商店购买和出售

这已经足以支撑单机版 Tarkov-like 原型的核心带入带出循环。

---

## 9. 当前缺失项

当前还没有的内容包括：

- 物品实例级持久化
- 武器实例状态持久化
- 护甲耐久跨局保存
- 安全箱局内拖拽转移
- 更复杂的商店关系和信誉
- 商人刷新、限购与任务需求
- 保险、邮件、黑市等系统

---

## 10. 后续扩展建议

下一步优先级建议：

1. 先做多容器拖拽 UI
2. 再做实例级武器 / 护甲 / 物品存档
3. 然后扩展商店成长与刷新逻辑
4. 最后再补任务、信誉、局外成长
