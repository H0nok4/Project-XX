# 局外资料、仓库与经济系统设计

## 1. 设计目标

截至 2026-03-18，局外系统的目标已经从“仅支撑带入带出测试”升级为：

- 作为正式局外入口承接基地 Hub
- 持久化仓库、配装、商人、设施、任务和成长数据
- 为战局提供稳定的实例级装备桥接
- 为后续世界状态、地图商人和制作系统预留状态位

当前文档以代码真相为准，不再沿用“主菜单即正式局外入口”“仍以定义级存档为主”的旧描述。

---

## 2. 核心脚本与状态归属

### 2.1 Profile 持久化

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/ProfileFileGateway.cs`
- `Assets/Res/Scripts/Profile/ProfileMigrationRunner.cs`
- `Assets/Res/Scripts/Profile/ProfileSchemaVersion.cs`
- `Assets/Res/Scripts/Profile/ProfileDiagnostics.cs`

职责：

- 读写 Profile JSON
- 迁移旧档
- 维护实例级物品 / 武器 / 护甲 DTO
- 负责资金、世界状态和成长数据的总持久化入口

### 2.2 局外运行时

- `Assets/Res/Scripts/Profile/MetaEntryRouter.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuUguiView.cs`
- `Assets/Res/Scripts/Base/BaseHubDirector.cs`

职责：

- 决定默认进入基地还是调试壳
- 组织仓库、商人、成长和设施页面
- 在基地里把终端和 NPC 交互转成具体 UI 页面

### 2.3 商人、设施与任务状态

- `Assets/Res/Scripts/Profile/MerchantManager.cs`
- `Assets/Res/Scripts/Profile/MerchantData.cs`
- `Assets/Res/Scripts/Base/BaseFacilityManager.cs`
- `Assets/Res/Scripts/Base/FacilityData.cs`
- `Assets/Res/Scripts/Quest/QuestManager.cs`
- `Assets/Res/Scripts/Profile/WorldStateData.cs`

职责：

- 管理商人等级、信誉、委托交付
- 管理基地设施等级和效果
- 保存任务运行时状态、故事标记和后续世界状态扩展位

### 2.4 成长与构筑

- `Assets/Res/Scripts/Profile/PlayerProgressionData.cs`
- `Assets/Res/Scripts/Profile/PrototypePlayerProgressionUtility.cs`
- `Assets/Res/Scripts/Profile/CharacterStatAggregator.cs`
- `Assets/Res/Scripts/Profile/RespecService.cs`

职责：

- 保存等级、经验、属性和技能树
- 在局外展示角色成长
- 在进局时把成长修正带入战斗

---

## 3. 当前 Profile 结构

`PrototypeProfileService.ProfileData` 当前包含以下几组数据：

### 3.1 版本与共享状态

- `profileSchemaVersion`
- `legacyVersion`
- `version`
- `funds`
- `worldState`
- `progression`

说明：

- `funds` 已是主货币字段
- 旧的 `cash_bundle` 数据会在读取和清洗时被折算回 `funds`

### 3.2 实例级库存与装备字段

- `stashItemInstances`
- `raidBackpackItemInstances`
- `secureContainerItemInstances`
- `specialEquipmentItemInstances`
- `equippedArmorInstances`
- `stashWeaponInstances`
- `raidBackpackWeaponInstances`
- `equippedSecureContainerInstance`
- `equippedPrimaryWeaponInstance`
- `equippedSecondaryWeaponInstance`
- `equippedMeleeWeaponInstance`

说明：

- 这些字段已经是当前主路径
- 局内外桥接以实例级数据为准

### 3.3 兼容旧结构的保留字段

- `stashItems`
- `loadoutItems`
- `extractedItems`
- `raidBackpackItems`
- `secureContainerItems`
- `specialEquipmentItems`
- `equippedArmorItems`
- `stashWeaponIds`
- `equippedPrimaryWeaponId`
- `equippedSecondaryWeaponId`
- `equippedMeleeWeaponId`

说明：

- 这些字段仍然保留，用于兼容和迁移
- 新功能不应再把它们当作唯一真相来源

---

## 4. 当前局外场景职责

### 4.1 BaseScene

当前定位：

- 正式局外入口
- 基地 Hub
- 当前仓库、商人、设施、任务和成长入口的实际落点

### 4.2 MainMenu

当前定位：

- `DebugShell`
- 启动和跳转入口
- 用于保留开发期的快速测试通道

边界约定：

- 正式局外功能只继续增强 `BaseScene`
- `MainMenu` 不再重新长出第二套正式仓库 / 商人 / 成长业务

---

## 5. 容器与风险规则

## 5.1 安全区

### Warehouse Stash

对应：

- `stashItemInstances`
- `PrototypeMainMenuController.stashInventory`

特点：

- 长期安全仓库
- 不受战局失败影响
- 受仓库设施等级影响容量

### Weapon Locker

对应：

- `stashWeaponInstances`
- `PrototypeMainMenuController.weaponLocker`

特点：

- 安全存放未装备武器
- 受武器库设施等级影响容量

## 5.2 风险区

### Raid Backpack

对应：

- `raidBackpackItemInstances`
- `PlayerInteractor.PrimaryInventory`

特点：

- 进局时装填到主背包
- 搜刮所得默认进入这里
- 撤离时保留
- 失败或超时时清空

### Equipped Armor

对应：

- `equippedArmorInstances`
- `PrototypeUnitVitals.EquippedArmor`

特点：

- 进局时直接挂到角色身上
- 撤离时保留
- 失败或超时时丢失

### Equipped Primary / Secondary

对应：

- `equippedPrimaryWeaponInstance`
- `equippedSecondaryWeaponInstance`

特点：

- 进局时挂到武器槽
- 局内可被拾取武器替换
- 撤离时保存当前最终持有状态
- 失败或超时时清空

## 5.3 保护区

### Melee

对应：

- `equippedMeleeWeaponInstance`

特点：

- 视为保护槽
- 撤离与失败都会保留

### Secure Container

对应：

- `secureContainerItemInstances`
- `equippedSecureContainerInstance`

特点：

- 进局时会创建并绑定到玩家辅助容器
- 不受失败影响
- 当前已经接入局内多容器拖拽链

### Special Equipment

对应：

- `specialEquipmentItemInstances`

特点：

- 预留给任务道具、钥匙、特殊装置等
- 不受失败影响

---

## 6. 局内外桥接规则

## 6.1 进局

`PrototypeRaidProfileFlow` 在战局启动时会：

- 读取 `ProfileData`
- 把 `raidBackpackItemInstances` 装入主背包
- 创建并填充安全箱 / 特殊装备辅助容器
- 应用 `equippedArmorInstances`
- 应用主武器 / 副武器 / 近战
- 应用 `PlayerProgressionData`
- 初始化 `QuestManager` 的战局运行态

## 6.2 撤离

当 `RaidGameMode` 进入 `Extracted`：

- 写回安全箱
- 写回特殊装备
- 写回近战槽
- 写回战斗背包
- 写回当前护甲
- 写回当前主副武器
- 写回成长数据

## 6.3 失败或超时

当 `RaidGameMode` 进入 `Failed / Expired`：

- 保留安全箱
- 保留特殊装备
- 保留近战槽
- 清空战斗背包
- 清空当前护甲
- 清空当前主副武器
- 保留成长数据和世界状态

---

## 7. 经济、商人与设施

## 7.1 货币

当前主货币是真实字段：

- `ProfileData.funds`

说明：

- UI 和交易逻辑都直接读写 `funds`
- `cash_bundle` 只作为历史兼容和目录资产保留

## 7.2 商人系统

当前商人已经具备：

- 固定商人目录
- 运行时库存重建
- 商人等级
- 商人信誉折扣
- 供货委托

当前基地商人包括：

- 武器商人
- 护甲商人
- 医疗商人
- 杂货商人

## 7.3 设施系统

当前基地设施包括：

- 仓库
- 武器库
- 医疗站
- 工作台

当前效果包括：

- 仓库容量提升
- 武器柜容量提升
- 死亡返场补给
- 出售收益加成

---

## 8. 成长与世界状态

## 8.1 PlayerProgressionData

当前已接入：

- 玩家等级
- 当前经验 / 累计经验
- 击杀数
- 未分配属性点
- 未分配技能点
- 属性集
- 技能树

## 8.2 CharacterStatAggregator

当前会统一汇总：

- 等级成长
- 属性点加成
- 技能树节点修正
- 武器词条修正
- 护甲词条修正

这些结果会在进局后应用到生命、体力、伤害、装填、移速、暴击和交互范围等战斗属性。

## 8.3 WorldStateData

当前已包含：

- `questStates`
- `merchantProgress`
- `baseFacilities`
- `storyFlags`
- `unlockedRaidMerchantIds`
- `unlockedRaidNpcIds`
- `questChainStages`

其中真正已经进入主流程的是：

- 任务状态
- 商人进度
- 基地设施
- 部分故事标记位

---

## 9. 当前边界

当前局外系统已经能支撑完整原型闭环，但仍有这些边界：

- 地图商人 / 地图任务 NPC 的正式状态驱动尚未落地
- 制作、材料、订单刷新、装备强化等经济深度系统还未进入主流程
- `SchoolTestScene` / `HospitalTestScene` 还没有并入默认构建与回归验证
- 局外 UI 已可用，但仍偏运行时拼装式原型风格

---

## 10. 配套约束

当需要继续扩展局外系统时，以以下文档的约束为准：

- [StateOwnershipAndPersistenceRules.md](./StateOwnershipAndPersistenceRules.md)
- [MerchantProgressionMatrix.md](./MerchantProgressionMatrix.md)
- [DevelopmentRoadmap_Part2.md](./DevelopmentRoadmap_Part2.md)
