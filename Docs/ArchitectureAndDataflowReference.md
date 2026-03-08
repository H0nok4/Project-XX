# 架构与数据流参考

## 1. 分层视角

当前工程建议按以下层次理解：

### 1.1 定义资产层

- `ItemDefinition`
- `AmmoDefinition`
- `ArmorDefinition`
- `MedicalItemDefinition`
- `PrototypeWeaponDefinition`
- `PrototypeUnitDefinition`
- `LootTableDefinition`
- `PrototypeEnemySpawnProfile`
- `PrototypeMerchantCatalog`
- `PrototypeItemCatalog`

职责：

- 提供静态数据
- 不直接负责运行时状态推进

### 1.2 运行时领域层

- `PrototypeFpsController`
- `PrototypeUnitVitals`
- `PrototypeStatusEffectController`
- `PrototypeBotController`
- `InventoryContainer`
- `LootContainer`
- `RaidGameMode`
- `PrototypeRaidProfileFlow`
- `PrototypeProfileService`

职责：

- 推进状态
- 做结算
- 管理局内外数据转移

### 1.3 交互与表现层

- `PlayerInteractor`
- `LootContainerWindowController`
- `PlayerInventoryWindowController`
- `PrototypeCombatTextController`
- `PrototypeTargetHealthBar`
- `PrototypeMainMenuController`

职责：

- 与玩家交互
- 显示当前状态
- 不应成为底层结算真相来源

### 1.4 编辑器工具层

- `PrototypeIndoorSceneBuilder`
- `PrototypeMainMenuSceneBuilder`
- `PrototypeRaidToolkitWindow`

职责：

- 生成和维护原型内容
- 不参与正式运行时结算

---

## 2. 场景级架构

## 2.1 MainMenu.unity

主要对象：

- `PrototypeMainMenuController`
- `PrototypeItemCatalog`
- `PrototypeMerchantCatalog`

主要能力：

- 加载 Profile
- 编辑仓库与战斗配置
- 购买 / 出售
- 进入战局

## 2.2 SampleScene.unity

主要对象：

- `RaidGameMode`
- `PrototypeRaidProfileFlow`
- 玩家对象：
  - `PrototypeFpsController`
  - `PrototypeFpsInput`
  - `PlayerInteractor`
  - `PrototypeUnitVitals`
  - `PrototypeStatusEffectController`
- AI 对象：
  - `PrototypeBotController`
  - `PrototypeUnitVitals`
- 场景对象：
  - `LootContainer`
  - `GroundLootItem`
  - `ExtractionZone`
  - `PrototypeDoor`
  - `PrototypeBreakable`
  - 刷怪点 / 区域

---

## 3. 关键数据流

## 3.1 主菜单到战局

流程：

1. `PrototypeMainMenuController` 加载 `ProfileData`
2. 玩家在局外调整：
   - `stashItems`
   - `stashWeaponIds`
   - `raidBackpackItems`
   - `secureContainerItems`
   - `specialEquipmentItems`
   - `equippedArmorItems`
   - `equippedPrimaryWeaponId`
   - `equippedSecondaryWeaponId`
   - `equippedMeleeWeaponId`
3. 点击 `Enter Battle`
4. 进入 `SampleScene`
5. `PrototypeRaidProfileFlow` 把这些配置应用到玩家运行时对象

## 3.2 战局结束回写 Profile

流程：

1. `RaidGameMode` 状态进入：
   - `Extracted`
   - `Failed`
   - `Expired`
2. `PrototypeRaidProfileFlow` 收集当前玩家状态
3. 写回：
   - 安全箱
   - 特殊装备
   - 近战槽
4. 如果撤离成功，再额外写回：
   - 战斗背包
   - 当前护甲
   - 当前主武器 / 副武器
5. 如果失败或超时：
   - 清空风险区
6. `PrototypeProfileService.SaveProfile`

## 3.3 玩家命中到伤害结算

流程：

1. `PrototypeFpsController` 执行射线检测
2. 命中 `PrototypeUnitHitbox`
3. `PrototypeUnitHitbox` 组装 `DamageInfo`
4. 调用 `PrototypeUnitVitals.ApplyDamage`
5. `PrototypeUnitVitals` 结算：
   - 护甲覆盖
   - 穿深
   - 耐久变化
   - 部位伤害
   - 溢出
   - 状态效果施加
   - 死亡
6. `PrototypeCombatTextController` 响应事件显示飘字

## 3.4 Debuff 导致死亡的数据流

流程：

1. 攻击命中后记录来源单位
2. `PrototypeStatusEffectController` 保存效果施加者
3. 后续 DOT 结算时把来源重新带回 `PrototypeUnitVitals`
4. `PrototypeUnitVitals` 更新最后伤害来源
5. `RaidGameMode` 结算界面显示来源

## 3.5 玩家移动噪声到 AI 听觉

流程：

1. `PrototypeFpsController` 根据：
   - 奔跑
   - 站立速度比例
   - 蹲姿
   - 跳跃 / 落地 / 枪声
   计算噪声半径
2. 调用 `PrototypeCombatNoiseSystem.ReportNoise`
3. `PrototypeBotController` 接收噪声事件
4. 根据：
   - 距离
   - archetype 感知参数
   - 当前状态
   决定是否进入警觉 / 搜索 / 追击

## 3.6 AI 死亡到尸体搜刮

流程：

1. `PrototypeBotController` 监听自身死亡
2. 创建尸体 Loot 载体
3. 把当前武器、护甲、额外物品写入尸体容器
4. 玩家通过 `PlayerInteractor` 对尸体交互
5. `LootContainerWindowController` 显示尸体武器与物资

---

## 4. 编辑器工具数据流

## 4.1 MainMenu Builder

`PrototypeMainMenuSceneBuilder` 负责：

- 维护 `PrototypeItemCatalog.asset`
- 维护 `PrototypeMerchantCatalog.asset`
- 重建 `MainMenu.unity`

## 4.2 SampleScene Builder

`PrototypeIndoorSceneBuilder` 负责：

- 维护默认物品、武器、护甲、敌人配置和 Loot 表
- 重建 `SampleScene.unity`

## 4.3 Raid Toolkit

`PrototypeRaidToolkitWindow` 负责：

- 原型关卡中的对象放置
- 不负责底层结算
- 更接近地图搭建辅助层

---

## 5. 当前架构结论

当前架构已经具备稳定的原型纵切面，但仍有 3 个明显特点：

- 运行时控制器较大，尤其是 `PrototypeFpsController` 与 `PrototypeBotController`
- UI 仍偏原型，尚未与逻辑层完全表现分离
- 存档层仍偏定义级，不是实例级

因此它适合继续快速做功能，但不适合直接扩成正式产品而不经过重构。
