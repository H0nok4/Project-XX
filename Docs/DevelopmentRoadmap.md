# Project-XX AI开发路线图

## 文档说明

本文档为AI辅助开发提供清晰的分阶段目标和任务分解。每个阶段都有明确的：
- 开发目标
- 具体任务列表
- 技术要点
- 验收标准
- 依赖关系

AI在开发时应该：
1. 按照阶段顺序进行开发
2. 完成当前阶段的验收标准后再进入下一阶段
3. 遵循开发规范文档的约定
4. 保持与现有原型代码的兼容性
5. 优先使用现有系统，避免重复造轮

---

## 开发阶段总览

```
阶段0：代码重构与架构优化（当前）
  ↓
阶段1：RPG装备系统核心
  ↓
阶段2：基地场景与商人系统
  ↓
阶段3：任务系统与剧情框架
  ↓
阶段4：角色成长系统
  ↓
阶段5：战斗地图商人与任务NPC
  ↓
阶段6：剧情内容制作
  ↓
阶段7：制作与经济系统
  ↓
阶段8：内容扩展
  ↓
阶段9：优化与完善
```

---

## 阶段0：代码重构与架构优化

### 目标
在添加新功能前，先优化现有代码架构，为后续开发打好基础。

### 任务列表

#### 0.1 实例级装备存档系统
**优先级**：最高
**预计时间**：1-2周

**任务描述**
- 创建装备实例数据结构（ItemInstance, WeaponInstance, ArmorInstance）
- 实现实例级序列化和反序列化
- 更新ProfileService支持实例级存档
- 迁移现有定义级存档到实例级

**技术要点**
- 使用DTO模式分离运行时实例和存档数据
- 保持向后兼容，支持旧存档迁移
- 实例ID生成和管理
- 实例属性（耐久、弹药、词条等）持久化

**验收标准**
- [x] 武器实例可以保存弹药数量和耐久
- [x] 护甲实例可以保存耐久
- [x] 撤离后装备状态正确保存
- [x] 旧存档可以正常加载并迁移

**相关文件**
- `Assets/Res/Scripts/Items/Runtime/ItemInstance.cs`
- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- 新增：`SavedItemInstanceDto.cs`, `SavedWeaponInstanceDto.cs`, `SavedArmorInstanceDto.cs`

#### 0.2 拆分主菜单控制器
**优先级**：高
**预计时间**：1周

**任务描述**
- 将PrototypeMainMenuController拆分为多个Presenter
- 创建MetaInventoryPresenter（仓库管理）
- 创建MetaMerchantPresenter（商店管理）
- 创建MetaLoadoutPresenter（装备配置）
- 保留MainMenuController作为页面路由

**技术要点**
- 使用MVP或MVVM模式
- 保持UI和逻辑分离
- 事件驱动的模块通信

**验收标准**
- [x] 主菜单控制器代码行数<500行
- [x] 各Presenter职责单一
- [x] 功能正常，无回归问题

**相关文件**
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- 新增：`MetaInventoryPresenter.cs`, `MetaMerchantPresenter.cs`, `MetaLoadoutPresenter.cs`

#### 0.3 优化玩家控制器
**优先级**：中
**预计时间**：1周

**任务描述**
- 拆分PrototypeFpsController的部分功能
- 创建PlayerWeaponController（武器管理）
- 创建PlayerMedicalController（医疗管理）
- 保留FpsController专注于移动和输入

**技术要点**
- 组件化设计
- 保持输入层统一
- 避免循环依赖

**验收标准**
- [x] FpsController代码行数<800行
- [x] 武器和医疗逻辑独立
- [x] 功能正常，手感不变

**相关文件**
- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`
- 新增：`PlayerWeaponController.cs`, `PlayerMedicalController.cs`

#### 0.4 存档Schema版本化与迁移基线
**优先级**：最高
**预计时间**：3-5天

**任务描述**
- 为 Profile、成长、世界状态建立明确的版本字段
- 建立统一的迁移入口与迁移日志
- 为旧档升级、新字段补全、损坏恢复提供基线支持
- 在中后期功能扩张前锁定状态真相与持久化边界

**技术要点**
- `ProfileData` 增加 `profileSchemaVersion`
- `WorldStateData`、`PlayerProgressionData` 采用独立子版本字段
- 每次新增持久化字段都必须附带迁移器
- 具体约束以 [StateOwnershipAndPersistenceRules.md](./StateOwnershipAndPersistenceRules.md) 为准

**验收标准**
- [x] 旧档可迁移到新 Profile 结构
- [x] 新增成长 / 世界状态字段不会导致旧档报错
- [x] 迁移失败时可保留原始备份
- [x] 有可追踪的迁移日志输出

**相关文件**
- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- 新增：`ProfileMigrationService.cs`, `ProfileSchemaVersion.cs`
- 新增：`PlayerProgressionData.cs`, `WorldStateData.cs`

#### 0.5 局外入口迁移规划与兼容层
**优先级**：高
**预计时间**：5-7天

**任务描述**
- 明确 `MainMenu.unity` 与 `BaseScene.unity` 的长期职责边界
- 为 `MainMenu -> BaseHub` 的迁移建立兼容层
- 避免仓库、商店、成长、任务、剧情入口在两个局外场景中双维护
- 保留开发期快速直达和调试入口

**技术要点**
- `MainMenu` 长期应退化为启动壳或调试入口，而不是正式局外主场景
- 创建 `MetaEntryRouter` 或等价协调层，统一局外入口流转
- 将业务逻辑从单一页面控制器迁移到可被 `BaseScene` 复用的 Presenter / Service

**验收标准**
- [x] 已定义 `MainMenu` 与 `BaseScene` 的最终职责
- [x] 局外核心功能不需要在两个场景各写一套
- [x] 保留开发调试入口且不影响正式流程

**相关文件**
- `Assets/Scenes/MainMenu.unity`
- 新增：`Assets/Scenes/BaseScene.unity`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- 新增：`MetaEntryRouter.cs`, `BaseHubDirector.cs`

---

## 阶段1：RPG装备系统核心

### 目标
实现完整的RPG装备系统，包括等级、品质、词条、技能。

### 前置条件
- 阶段0完成
- 实例级存档系统可用

### 任务列表

#### 1.1 装备等级系统
**优先级**：最高
**预计时间**：3-5天

**任务描述**
- 为ItemDefinition添加等级属性
- 实现等级影响基础属性的计算
- 更新UI显示装备等级
- 实现等级要求检查

**技术要点**
```csharp
// 装备定义添加等级
public class ItemDefinition : ScriptableObject
{
    public int itemLevel = 1; // 1-50
    public int requiredLevel = 1; // 使用要求
}

// 属性计算
public float GetScaledValue(float baseValue, int itemLevel)
{
    return baseValue * (1 + (itemLevel - 1) * 0.1f); // 每级+10%
}
```

**验收标准**
- [x] 装备有等级显示
- [x] 等级影响属性计算
- [x] 等级不足无法装备
- [x] 战利品掉落等级合理

**相关文件**
- `Assets/Res/Scripts/Items/Definitions/ItemDefinition.cs`
- `Assets/Res/Scripts/Items/Definitions/PrototypeWeaponDefinition.cs`
- `Assets/Res/Scripts/Items/Definitions/ArmorDefinition.cs`

#### 1.2 装备品质系统
**优先级**：最高
**预计时间**：3-5天

**任务描述**
- 创建品质枚举（Common, Uncommon, Rare, Epic, Legendary）
- 为装备实例添加品质属性
- 实现品质影响属性加成
- 实现品质颜色显示
- 更新掉落系统支持品质权重

**技术要点**
```csharp
public enum ItemRarity
{
    Common,    // 白色，基础属性
    Uncommon,  // 绿色，+10%，1词条
    Rare,      // 蓝色，+25%，2词条
    Epic,      // 紫色，+50%，3词条，可能有技能
    Legendary  // 橙色，+100%，4词条，必有技能
}

public class ItemInstance
{
    public ItemDefinition definition;
    public ItemRarity rarity;
    public int level;
    public List<ItemAffix> affixes; // 词条
    public ItemSkill skill; // 技能（可选）
}
```

**验收标准**
- [ ] 装备有品质显示和颜色
- [ ] 品质影响属性加成
- [ ] 掉落系统按权重生成品质
- [ ] 高品质装备更稀有

**相关文件**
- 新增：`ItemRarity.cs`, `ItemInstance.cs`（扩展）
- `Assets/Res/Scripts/Loot/LootTableDefinition.cs`

#### 1.3 随机词条系统
**优先级**：高
**预计时间**：5-7天

**任务描述**
- 创建词条数据结构
- 实现词条池和随机生成
- 实现词条效果应用
- 更新UI显示词条
- 实现词条数值范围

**技术要点**
```csharp
public enum AffixType
{
    // 攻击类
    DamageBonus,
    CritChance,
    CritDamage,
    ArmorPenetration,

    // 防御类
    ArmorBonus,
    DamageReduction,

    // 机动类
    MoveSpeed,
    ReloadSpeed,

    // 生存类
    HealthBonus,
    StaminaBonus,
    HealingBonus
}

public class ItemAffix
{
    public AffixType type;
    public float value;
    public int tier; // 词条品质（影响数值范围）
}

public class AffixPool : ScriptableObject
{
    public List<AffixDefinition> availableAffixes;

    public List<ItemAffix> GenerateAffixes(ItemRarity rarity, int itemLevel)
    {
        int affixCount = GetAffixCount(rarity);
        // 随机生成词条
    }
}
```

**验收标准**
- [ ] 装备可以有随机词条
- [ ] 词条数量根据品质决定
- [ ] 词条效果正确应用到战斗
- [ ] UI正确显示所有词条
- [ ] 词条数值在合理范围内

**相关文件**
- 新增：`ItemAffix.cs`, `AffixType.cs`, `AffixPool.cs`, `AffixDefinition.cs`
- `Assets/Res/Scripts/Items/Runtime/ItemInstance.cs`

#### 1.4 装备技能系统（被动）
**优先级**：中
**预计时间**：5-7天

**任务描述**
- 创建装备技能数据结构
- 实现被动技能系统
- 实现技能效果（击杀回血、弹药回收等）
- 更新UI显示技能
- 实现技能触发和效果

**技术要点**
```csharp
public enum ItemSkillType
{
    // 被动技能
    KillHeal,           // 击杀回血
    AmmoRecovery,       // 弹药回收
    IronBody,           // 钢铁之躯
    BattleFrenzy,       // 战斗狂热
    PerfectDodge,       // 完美闪避
    Bloodlust,          // 嗜血
    Unyielding          // 不屈意志
}

public class ItemSkill
{
    public ItemSkillType type;
    public float value;
    public string description;
}

// 技能管理器
public class PlayerSkillManager : MonoBehaviour
{
    private List<ItemSkill> activeSkills = new List<ItemSkill>();

    public void OnEquipmentChanged()
    {
        // 收集所有装备的技能
        activeSkills.Clear();
        // 从装备中收集技能
    }

    public void OnEnemyKilled()
    {
        // 触发击杀相关技能
    }
}
```

**验收标准**
- [ ] 史诗/传说装备可能有技能
- [ ] 技能效果正确触发
- [ ] UI正确显示技能描述
- [ ] 多个技能可以同时生效
- [ ] 技能效果平衡合理

**相关文件**
- 新增：`ItemSkill.cs`, `ItemSkillType.cs`, `PlayerSkillManager.cs`
- `Assets/Res/Scripts/Items/Runtime/ItemInstance.cs`

#### 1.5 更新战利品系统
**优先级**：高
**预计时间**：3-5天

**任务描述**
- 更新LootTableDefinition支持RPG装备生成
- 实现根据地图难度生成合适等级的装备
- 实现品质权重配置
- 更新尸体掉落支持RPG装备

**技术要点**
```csharp
public class LootTableDefinition : ScriptableObject
{
    public int minItemLevel = 1;
    public int maxItemLevel = 10;

    [Header("品质权重")]
    public float commonWeight = 60f;
    public float uncommonWeight = 25f;
    public float rareWeight = 10f;
    public float epicWeight = 4f;
    public float legendaryWeight = 1f;

    public ItemInstance GenerateItem()
    {
        // 随机等级
        int level = Random.Range(minItemLevel, maxItemLevel + 1);

        // 随机品质
        ItemRarity rarity = RollRarity();

        // 生成词条
        List<ItemAffix> affixes = GenerateAffixes(rarity, level);

        // 生成技能（如果适用）
        ItemSkill skill = GenerateSkill(rarity);

        return new ItemInstance(definition, level, rarity, affixes, skill);
    }
}
```

**验收标准**
- [ ] 战利品生成RPG装备
- [ ] 装备等级符合地图难度
- [ ] 品质分布合理
- [ ] Boss掉落更好的装备

**相关文件**
- `Assets/Res/Scripts/Loot/LootTableDefinition.cs`
- `Assets/Res/Scripts/AI/PrototypeBotController.cs`（尸体掉落）

#### 1.6 更新商店系统
**优先级**：中
**预计时间**：3-5天

**任务描述**
- 更新商店支持出售RPG装备
- 实现商人等级影响出售装备品质
- 实现装备价格根据等级、品质、词条计算
- 更新商店UI显示完整装备信息

**技术要点**
```csharp
public class MerchantInventory
{
    public List<ItemInstance> availableItems;

    public void GenerateInventory(int merchantLevel)
    {
        // 根据商人等级生成商品
        // 1级：普通
        // 2级：普通+优秀
        // 3级：优秀+稀有
        // 4级：稀有+史诗
        // 5级：史诗+传说
    }

    public int CalculatePrice(ItemInstance item)
    {
        int basePrice = item.definition.basePrice;
        float levelMultiplier = 1 + (item.level - 1) * 0.1f;
        float rarityMultiplier = GetRarityMultiplier(item.rarity);
        float affixMultiplier = 1 + item.affixes.Count * 0.2f;

        return (int)(basePrice * levelMultiplier * rarityMultiplier * affixMultiplier);
    }
}
```

- 商人等级 / 信誉 / 世界状态 / 刷新规则的职责边界以 [MerchantProgressionMatrix.md](./MerchantProgressionMatrix.md) 为准

**验收标准**
- [ ] 商店出售RPG装备
- [ ] 装备价格合理
- [ ] 商人等级影响商品品质
- [ ] UI显示完整装备信息

**相关文件**
- `Assets/Res/Scripts/Profile/PrototypeMerchantCatalog.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`

---

## 阶段2：基地场景与商人系统

### 目标
创建完整的基地场景，实现固定位置商人交互系统。

### 前置条件
- 阶段1完成
- RPG装备系统可用

### 任务列表

#### 2.1 基地场景设计与搭建
**优先级**：最高
**预计时间**：1-2周

**任务描述**
- 设计基地场景布局
- 划分功能区域（仓库区、商人区、准备区、任务区）
- 搭建基地场景（可以先用blockout）
- 放置商人NPC位置
- 添加导航和标识

**技术要点**
- 使用ProBuilder或其他工具快速搭建
- 合理的区域划分和动线设计
- 清晰的视觉引导
- 性能优化（合批、遮挡剔除）

**验收标准**
- [ ] 基地场景完整可导航
- [ ] 各功能区域清晰
- [ ] 商人位置合理
- [ ] 性能达标（60FPS）

**相关文件**
- 新增：`Assets/Scenes/BaseScene.unity`
- 新增：基地场景相关预制体

#### 2.2 商人NPC交互系统
**优先级**：最高
**预计时间**：5-7天

**任务描述**
- 创建商人NPC预制体
- 实现靠近显示交互提示
- 实现按E键打开商店
- 创建商人数据配置
- 实现商人对话（可选）

**技术要点**
```csharp
public class MerchantNPC : MonoBehaviour, IInteractable
{
    public string merchantName;
    public MerchantType merchantType;
    public int merchantLevel = 1;
    public float interactionRange = 2f;

    public string GetInteractionPrompt()
    {
        return $"按E与{merchantName}交易";
    }

    public void OnInteract(GameObject interactor)
    {
        // 打开商店UI
        MerchantUIManager.Instance.OpenMerchant(this);
    }
}

public enum MerchantType
{
    Weapon,
    Armor,
    Medical,
    General
}
```

**验收标准**
- [ ] 可以靠近商人看到提示
- [ ] 按E打开对应商店
- [ ] 商店显示商人信息
- [ ] 交互流畅自然

**相关文件**
- 新增：`MerchantNPC.cs`, `MerchantUIManager.cs`
- `Assets/Res/Scripts/Interaction/IInteractable.cs`
- `Assets/Res/Scripts/Interaction/PlayerInteractor.cs`

