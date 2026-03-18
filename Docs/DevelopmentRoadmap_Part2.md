#### 2.3 商人等级系统
**优先级**：高
**预计时间**：3-5天
**当前状态**：已完成（2026-03-18）

**任务描述**
- 实现商人等级数据结构
- 实现通过交易额提升商人等级
- 实现商人等级影响商品品质
- 更新UI显示商人等级和进度
- 已落地 `MerchantData + MerchantManager`，交易额会写回 Profile 并驱动商人库存等级
- 商人运行时库存由 `PrototypeMerchantCatalog` 按商人等级重建，等级提升后会刷新装备品质池
- 商人面板已显示等级、交易进度、起始等级和当前库存等级

**技术要点**
```csharp
public class MerchantData
{
    public string merchantId;
    public int level = 1;
    public int totalTradeAmount = 0;
    public int reputation = 0; // 信誉值

    public int GetLevelUpRequirement()
    {
        return level * 10000; // 每级需要10000货币交易额
    }

    public void AddTradeAmount(int amount)
    {
        totalTradeAmount += amount;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (totalTradeAmount >= GetLevelUpRequirement() && level < 5)
        {
            level++;
            // 触发升级事件
        }
    }
}
```

- 商人等级、信誉、解锁状态、库存刷新的职责边界以 [MerchantProgressionMatrix.md](./MerchantProgressionMatrix.md) 为准

**验收标准**
- [x] 商人有等级显示
- [x] 交易可以提升商人等级
- [x] 商人等级影响商品品质
- [x] UI显示升级进度

**相关文件**
- 新增：`MerchantData.cs`, `MerchantManager.cs`
- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/PrototypeMerchantCatalog.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/MetaMerchantPresenter.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuUguiView.cs`

#### 2.4 商人信誉系统
**优先级**：中
**预计时间**：3-5天
**当前状态**：已完成（2026-03-18）

**任务描述**
- 实现信誉等级（中立、友好、尊敬、崇拜）
- 实现完成商人任务提升信誉
- 实现信誉影响价格折扣
- 更新UI显示信誉状态
- 已为四位基地商人接入重复委托交付，交付指定物资即可累计信誉点
- 商人信誉只改价格倍率，不改库存 tier；信誉提升后会直接重算当前商店价格
- 商人面板已显示信誉等级、信誉进度和当前折扣

**技术要点**
```csharp
public enum ReputationLevel
{
    Neutral,    // 中立：无折扣
    Friendly,   // 友好：95折
    Honored,    // 尊敬：90折
    Revered     // 崇拜：85折
}

public class MerchantData
{
    public ReputationLevel reputation = ReputationLevel.Neutral;
    public int reputationPoints = 0;

    public float GetPriceMultiplier()
    {
        switch (reputation)
        {
            case ReputationLevel.Friendly: return 0.95f;
            case ReputationLevel.Honored: return 0.90f;
            case ReputationLevel.Revered: return 0.85f;
            default: return 1.0f;
        }
    }

    public void AddReputation(int points)
    {
        reputationPoints += points;
        UpdateReputationLevel();
    }
}
```

- 商人信誉只负责价格和部分权限，不负责决定商人是否出现或库存池 tier，具体以 [MerchantProgressionMatrix.md](./MerchantProgressionMatrix.md) 为准

**验收标准**
- [x] 商人有信誉等级显示
- [x] 完成任务可以提升信誉
- [x] 信誉影响购买价格
- [x] UI显示信誉进度

**相关文件**
- `MerchantData.cs`
- 新增：`ReputationLevel.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/PrototypeMerchantCatalog.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuUguiView.cs`

#### 2.5 基地设施系统（基础）
**优先级**：中
**预计时间**：5-7天
**当前状态**：已完成（2026-03-18）

**任务描述**
- 创建设施数据结构
- 实现仓库、武器库、医疗站、工作台
- 实现设施等级系统
- 实现设施升级功能
- 已新增 `BaseFacilityManager`，统一管理仓库、武器库、医疗站、工作台四类设施
- 仓库升级会提升仓库格数，武器库升级会提升武器柜容量
- 医疗站会在基地重生返场时自动补给恢复物资，工作台会提高出售收益
- 基地主页已显示设施等级、效果说明和升级入口

**技术要点**
```csharp
public enum FacilityType
{
    Warehouse,      // 仓库
    Armory,         // 武器库
    MedicalStation, // 医疗站
    Workbench       // 工作台
}

public class FacilityData
{
    public FacilityType type;
    public int level = 1;
    public int maxLevel = 5;

    public int GetUpgradeCost()
    {
        return level * 5000;
    }

    public bool CanUpgrade()
    {
        return level < maxLevel;
    }
}

public class BaseFacilityManager : MonoBehaviour
{
    private Dictionary<FacilityType, FacilityData> facilities;

    public void UpgradeFacility(FacilityType type)
    {
        // 升级设施
    }

    public int GetWarehouseCapacity()
    {
        int level = facilities[FacilityType.Warehouse].level;
        return 100 * level; // 每级100格
    }
}
```

**验收标准**
- [x] 基地有设施系统
- [x] 可以升级设施
- [x] 设施等级影响功能
- [x] UI显示设施状态

**相关文件**
- 新增：`FacilityType.cs`, `FacilityData.cs`, `BaseFacilityManager.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuUguiView.cs`
- `Assets/Res/Scripts/Base/BaseHubDirector.cs`
- `Assets/Res/Scripts/Base/Editor/BaseHubSceneBuilder.cs`

#### 2.6 局外入口迁移与兼容层
**优先级**：高
**预计时间**：5-7天
**当前状态**：已完成（2026-03-18）

**任务描述**
- 明确 `MainMenu` 与 `BaseScene` 的最终职责边界，避免正式局外功能长期双维护
- 为原型入口增加兼容层，保证旧调试流程和新正式流程可以并存一段时间
- 让 `MainMenu` 逐步退化为启动壳、调试入口与跳转页，而不是继续承载正式商店、成长、任务等业务
- 为后续基地正式化预留统一入口路由、返回逻辑与快速调试通道
- `BaseScene` 已成为正式局外入口，保留完整仓库 / 商人 / 设施能力
- `MainMenu` 已切换为 `DebugShell` 模式，只保留启动、跳转与调试入口
- 两个场景均通过 scene builder 固化职责边界，避免手工布线再次回流

**技术要点**
- 新增 `MetaEntryRouter`，统一处理从启动到基地、从基地到战斗、从结算回基地的入口流转
- `PrototypeMainMenuController` 不再同时承担“正式Hub + 调试入口”两类职责
- `BaseScene` 从这一阶段开始被视为正式局外入口，后续阶段只允许增强它，不再回写主菜单专属业务逻辑
- 入口流转、Profile读写、世界状态切换规则遵循 [StateOwnershipAndPersistenceRules.md](./StateOwnershipAndPersistenceRules.md)

**验收标准**
- [x] `MainMenu` 与 `BaseScene` 的职责边界清晰并已文档化
- [x] 正式局外流程可从 `BaseScene` 进入，不依赖旧主菜单业务面板
- [x] 调试入口仍可快速进入基地、战斗、测试流程
- [x] 不再出现同一套局外功能在两个入口各维护一份

**相关文件**
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/BaseScene.unity`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- 新增：`MetaEntryRouter.cs`, `BaseHubDirector.cs`
- `Assets/Res/Scripts/UI/Editor/PrototypeMainMenuSceneBuilder.cs`
- `Assets/Res/Scripts/Base/Editor/BaseHubSceneBuilder.cs`

---

## 阶段3：任务系统与剧情框架

### 目标
实现完整的任务系统框架和基础剧情系统。

### 前置条件
- 阶段2完成
- 基地场景可用

### 任务列表

#### 3.1 任务系统框架
**优先级**：最高
**预计时间**：1-2周

**任务描述**
- 创建任务数据结构
- 实现任务状态管理
- 实现任务目标追踪
- 实现任务完成检测
- 创建任务管理器

**技术要点**
```csharp
public enum QuestType
{
    Main,       // 主线
    Side,       // 支线
    Daily,      // 日常
    Hidden      // 隐藏
}

public enum QuestStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

public class Quest
{
    public string questId;
    public string questName;
    public string description;
    public QuestType type;
    public QuestStatus status;
    public List<QuestObjective> objectives;
    public QuestReward reward;
    public List<string> prerequisiteQuests; // 前置任务

    public bool CanStart()
    {
        // 检查前置任务是否完成
    }

    public void CheckCompletion()
    {
        if (objectives.All(o => o.IsCompleted()))
        {
            status = QuestStatus.Completed;
        }
    }
}

public abstract class QuestObjective
{
    public string description;
    public int currentProgress;
    public int requiredProgress;

    public abstract bool IsCompleted();
    public abstract void UpdateProgress(object data);
}

// 击杀目标
public class KillObjective : QuestObjective
{
    public string enemyType;

    public override bool IsCompleted()
    {
        return currentProgress >= requiredProgress;
    }
}

// 收集目标
public class CollectObjective : QuestObjective
{
    public string itemId;

    public override bool IsCompleted()
    {
        return currentProgress >= requiredProgress;
    }
}

// 探索目标
public class ExploreObjective : QuestObjective
{
    public string locationId;

    public override bool IsCompleted()
    {
        return currentProgress >= requiredProgress;
    }
}

public class QuestManager : MonoBehaviour
{
    private List<Quest> activeQuests = new List<Quest>();
    private List<Quest> completedQuests = new List<Quest>();

    public void StartQuest(string questId)
    {
        Quest quest = GetQuestDefinition(questId);
        if (quest.CanStart())
        {
            quest.status = QuestStatus.InProgress;
            activeQuests.Add(quest);
        }
    }

    public void UpdateQuestProgress(string questId, object data)
    {
        Quest quest = activeQuests.Find(q => q.questId == questId);
        if (quest != null)
        {
            foreach (var objective in quest.objectives)
            {
                objective.UpdateProgress(data);
            }
            quest.CheckCompletion();
        }
    }

    public void CompleteQuest(string questId)
    {
        Quest quest = activeQuests.Find(q => q.questId == questId);
        if (quest != null && quest.status == QuestStatus.Completed)
        {
            // 发放奖励
            GiveReward(quest.reward);
            activeQuests.Remove(quest);
            completedQuests.Add(quest);
        }
    }
}
```

**验收标准**
- [ ] 可以接取任务
- [ ] 任务目标正确追踪
- [ ] 完成目标后任务状态更新
- [ ] 可以提交任务获得奖励

**相关文件**
- 新增：`Quest.cs`, `QuestType.cs`, `QuestStatus.cs`, `QuestObjective.cs`, `QuestManager.cs`

#### 3.2 任务UI系统
**优先级**：高
**预计时间**：5-7天

**任务描述**
- 创建任务列表UI
- 创建任务详情UI
- 创建任务追踪HUD
- 实现任务接取和提交界面

**技术要点**
- 使用Unity UI或UI Toolkit
- 清晰的信息层级
- 实时更新任务进度
- 任务完成提示

**验收标准**
- [ ] 可以查看可接取任务
- [ ] 可以查看进行中任务
- [ ] HUD显示当前任务目标
- [ ] 任务完成有提示

**相关文件**
- 新增：`QuestListUI.cs`, `QuestDetailUI.cs`, `QuestTrackerHUD.cs`

#### 3.3 对话系统
**优先级**：高
**预计时间**：1周

**任务描述**
- 创建对话数据结构
- 实现对话显示系统
- 实现对话分支选择
- 实现对话触发任务

**技术要点**
```csharp
public class DialogueNode
{
    public string nodeId;
    public string speakerName;
    public string dialogueText;
    public List<DialogueOption> options;
    public string nextNodeId; // 线性对话的下一节点

    public bool HasOptions()
    {
        return options != null && options.Count > 0;
    }
}

public class DialogueOption
{
    public string optionText;
    public string targetNodeId;
    public System.Action onSelected; // 选择后的回调

    // 条件显示
    public bool IsAvailable()
    {
        // 检查条件
        return true;
    }
}

public class DialogueSystem : MonoBehaviour
{
    private DialogueNode currentNode;

    public void StartDialogue(string dialogueId)
    {
        // 加载对话数据
        // 显示对话UI
    }

    public void SelectOption(int optionIndex)
    {
        DialogueOption option = currentNode.options[optionIndex];
        option.onSelected?.Invoke();

        if (!string.IsNullOrEmpty(option.targetNodeId))
        {
            ShowNode(option.targetNodeId);
        }
        else
        {
            EndDialogue();
        }
    }

    public void ShowNextNode()
    {
        if (!string.IsNullOrEmpty(currentNode.nextNodeId))
        {
            ShowNode(currentNode.nextNodeId);
        }
        else
        {
            EndDialogue();
        }
    }
}
```

**验收标准**
- [ ] 可以与NPC对话
- [ ] 对话文本正确显示
- [ ] 可以选择对话选项
- [ ] 对话可以触发任务

**相关文件**
- 新增：`DialogueNode.cs`, `DialogueOption.cs`, `DialogueSystem.cs`, `DialogueUI.cs`

#### 3.4 基地任务NPC
**优先级**：高
**预计时间**：3-5天

**任务描述**
- 创建任务NPC预制体
- 实现任务接取交互
- 实现任务提交交互
- 放置基地任务NPC

**技术要点**
```csharp
public class QuestNPC : MonoBehaviour, IInteractable
{
    public string npcName;
    public List<string> availableQuests; // 可提供的任务ID

    public string GetInteractionPrompt()
    {
        if (HasAvailableQuests())
        {
            return $"按E与{npcName}对话（有新任务）";
        }
        else if (HasCompletableQuests())
        {
            return $"按E与{npcName}对话（可提交任务）";
        }
        else
        {
            return $"按E与{npcName}对话";
        }
    }

    public void OnInteract(GameObject interactor)
    {
        // 打开对话/任务界面
        QuestUIManager.Instance.OpenQuestNPC(this);
    }

    private bool HasAvailableQuests()
    {
        // 检查是否有可接取的任务
    }

    private bool HasCompletableQuests()
    {
        // 检查是否有可提交的任务
    }
}
```

**验收标准**
- [ ] 基地有任务NPC
- [ ] 可以从NPC接取任务
- [ ] 可以向NPC提交任务
- [ ] NPC有任务状态提示

**相关文件**
- 新增：`QuestNPC.cs`
- `Assets/Res/Scripts/Interaction/IInteractable.cs`

#### 3.5 商人委托任务
**优先级**：中
**预计时间**：3-5天

**任务描述**
- 为每个商人创建委托任务
- 实现完成任务提升信誉
- 实现任务奖励系统
- 创建商人任务数据

**技术要点**
```csharp
public class MerchantQuest : Quest
{
    public string merchantId;
    public int reputationReward; // 信誉奖励

    public override void OnComplete()
    {
        base.OnComplete();

        // 提升商人信誉
        MerchantManager.Instance.AddReputation(merchantId, reputationReward);
    }
}

// 示例：武器商人任务
public static class WeaponDealerQuests
{
    public static Quest CreateWeaponTestQuest()
    {
        return new MerchantQuest
        {
            questId = "weapon_dealer_test",
            questName = "武器测试",
            description = "使用指定武器击杀50个敌人",
            merchantId = "weapon_dealer",
            reputationReward = 100,
            objectives = new List<QuestObjective>
            {
                new KillObjective
                {
                    description = "使用突击步枪击杀敌人",
                    requiredProgress = 50
                }
            }
        };
    }
}
```

**验收标准**
- [ ] 每个商人有委托任务
- [ ] 完成任务提升信誉
- [ ] 任务奖励正确发放
- [ ] 任务难度合理

**相关文件**
- `MerchantQuest.cs`
- `MerchantData.cs`

#### 3.6 主线任务（第一章）
**优先级**：中
**预计时间**：1-2周

**任务描述**
- 设计第一章主线任务流程
- 创建主线任务数据
- 实现主线任务逻辑
- 制作任务相关对话

**任务流程示例**
```
第一章：觉醒

任务1：基地熟悉
- 目标：与指挥官对话
- 目标：参观仓库
- 目标：与武器商人对话
- 奖励：500货币，基础武器

任务2：首次出击
- 目标：进入战斗地图
- 目标：击杀5个敌人
- 目标：搜刮3个容器
- 目标：成功撤离
- 奖励：1000货币，经验值

任务3：物资补给
- 目标：收集10个医疗用品
- 目标：收集20个弹药
- 奖励：1500货币，护甲

任务4：危险区域
- 目标：探索废弃工厂
- 目标：击杀工厂Boss
- 目标：获得关键物品
- 奖励：3000货币，稀有武器，解锁新区域
```

**验收标准**
- [ ] 第一章任务可完整游玩
- [ ] 任务引导清晰
- [ ] 任务难度合理
- [ ] 任务奖励合适

**相关文件**
- 新增：主线任务数据文件

---

## 阶段4：角色成长系统

### 目标
实现完整的角色成长系统，包括等级、属性、技能树。

### 前置条件
- 阶段3完成
- 任务系统可用

### 任务列表

#### 4.1 等级系统
**优先级**：最高
**预计时间**：3-5天

**任务描述**
- 实现玩家等级系统
- 实现经验值获取
- 实现升级奖励
- 更新UI显示等级和经验

**技术要点**
```csharp
public class PlayerLevel : MonoBehaviour
{
    public int currentLevel = 1;
    public int currentExp = 0;
    public int maxLevel = 50;

    public int GetExpForNextLevel()
    {
        // 指数增长
        return 100 * currentLevel * currentLevel;
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (currentExp >= GetExpForNextLevel() && currentLevel < maxLevel)
        {
            currentExp -= GetExpForNextLevel();
            currentLevel++;
            OnLevelUp();
        }
    }

    private void OnLevelUp()
    {
        // 发放升级奖励
        PlayerAttributes.Instance.AddAttributePoints(3);
        PlayerSkillTree.Instance.AddSkillPoints(1);

        // 提升负重
        InventoryContainer.Instance.maxWeight += 2;

        // 触发升级事件
        OnLevelUpEvent?.Invoke(currentLevel);
    }

    public void GainExpFromKill(int enemyLevel)
    {
        int baseExp = 50;
        float levelDiff = enemyLevel - currentLevel;
        float multiplier = 1 + (levelDiff * 0.1f);
        multiplier = Mathf.Clamp(multiplier, 0.5f, 2.0f);

        int exp = (int)(baseExp * multiplier);
        AddExp(exp);
    }
}
```

**验收标准**
- [ ] 玩家有等级显示
- [ ] 击杀敌人获得经验
- [ ] 完成任务获得经验
- [ ] 升级获得奖励
- [ ] UI显示经验进度

**相关文件**
- 新增：`PlayerLevel.cs`
- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`

#### 4.2 玩家属性系统
**优先级**：高
**预计时间**：5-7天

**任务描述**
- 实现基础属性系统（力量、体质、敏捷、感知、技术）
- 实现升级获得属性点并手动分配
- 实现基础属性对派生战斗属性的影响
- 更新角色面板显示基础属性与派生属性

**技术要点**
```csharp
public enum PlayerAttributeType
{
    Strength,   // 负重、近战、后坐控制
    Endurance,  // 生命、体力、恢复
    Agility,    // 移速、换弹、操作速度
    Perception, // 探测、弱点识别、任务交互范围
    Tech        // 制作、维修、词条重铸
}

public class PlayerAttributeSet
{
    public int unspentPoints;
    public Dictionary<PlayerAttributeType, int> values;
}

public class PlayerDerivedStats
{
    public float maxHealth;
    public float maxStamina;
    public float carryWeight;
    public float reloadSpeedMultiplier;
    public float recoilControlMultiplier;
}
```

**验收标准**
- [ ] 玩家升级后获得属性点
- [ ] 不同属性会影响不同派生能力
- [ ] 属性分配结果可被存档并正确加载
- [ ] UI可清晰显示属性变化前后数值

**相关文件**
- 新增：`PlayerAttributeType.cs`, `PlayerAttributeSet.cs`, `PlayerDerivedStats.cs`
- 新增：`PlayerProgressionService.cs`, `PlayerProgressionPanel.cs`
- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`

#### 4.3 技能树与专精系统
**优先级**：高
**预计时间**：1-2周

**任务描述**
- 设计战斗、生存、工程三条基础专精分支
- 创建技能节点定义与解锁条件
- 实现被动增益与局内触发型效果
- 实现技能点分配、重置与存档

**技术要点**
```csharp
public enum SkillBranch
{
    Combat,
    Survival,
    Engineering
}

public class SkillNodeDefinition : ScriptableObject
{
    public string nodeId;
    public SkillBranch branch;
    public List<string> prerequisiteNodeIds;
    public int requiredPlayerLevel;
    public List<ModifierDefinition> modifiers;
}

public class PlayerSkillTree
{
    public int unspentSkillPoints;
    public HashSet<string> unlockedNodeIds;
}
```

**验收标准**
- [ ] 玩家可在技能树中解锁节点
- [ ] 技能节点前置条件正确生效
- [ ] 被动技能效果可正确影响战斗与生存系统
- [ ] 技能点与解锁状态可持久化

**相关文件**
- 新增：`SkillBranch.cs`, `SkillNodeDefinition.cs`, `PlayerSkillTree.cs`
- 新增：`SkillTreePresenter.cs`, `SkillTreePanel.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`
- `Assets/Res/Scripts/FPS/PrototypeUnitVitals.cs`

#### 4.4 属性汇总与构筑结算统一
**优先级**：最高
**预计时间**：5-7天

**任务描述**
- 统一等级、属性、技能、装备词条、状态效果的结算入口
- 创建角色构筑汇总服务，避免多个系统重复计算
- 定义派生属性刷新时机（装备变化、升级、技能解锁、状态变化）
- 为后续RPG装备词条和技能联动提供稳定底座

**技术要点**
- 新增 `CharacterStatAggregator` 作为单一构筑结算来源
- 约定所有数值修正统一输出为 `ModifierDefinition` / `ModifierRuntime`
- 禁止 UI、武器系统、状态系统各自维护私有属性缓存
- 与 `PrototypeUnitVitals`、`PrototypeFpsController`、`PlayerSkillManager` 做单向依赖整合

**验收标准**
- [ ] 装备变化后角色派生属性立即刷新
- [ ] 技能树与属性点分配结果可影响局内战斗表现
- [ ] 不同系统读取同一份派生属性结果
- [ ] 排查不存在“面板显示”和“实际战斗”不一致问题

**相关文件**
- 新增：`CharacterStatAggregator.cs`, `ModifierDefinition.cs`, `ModifierRuntime.cs`
- `Assets/Res/Scripts/FPS/PrototypeUnitVitals.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`
- `Assets/Res/Scripts/Items/Runtime/ItemInstance.cs`

#### 4.5 成长面板与角色构筑回显
**优先级**：中
**预计时间**：5-7天

**任务描述**
- 在基地中新增角色成长页或角色终端
- 展示等级、经验、属性、技能树、构筑摘要
- 展示装备词条和技能节点对当前构筑的贡献
- 支持技能重置、属性重置的基础入口（可受资源或剧情限制）

**技术要点**
- 保持成长面板只读写成长数据，不直接控制战斗逻辑
- UI支持清晰区分基础值、装备加成、技能加成、临时Buff
- 为后续剧情CG、任务奖励、基地终端共用UI样式预留布局规则

**验收标准**
- [ ] 基地中可查看完整成长信息
- [ ] 玩家可理解当前构筑的核心强项
- [ ] 重置操作有明确提示与约束
- [ ] 面板刷新不依赖重新进入场景

**相关文件**
- 新增：`PlayerProgressionPanel.cs`, `BuildSummaryPanel.cs`, `RespecService.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`

---

## 阶段5：战斗地图商人与任务NPC

### 目标
在战斗地图中建立中立交互层，让地图商人、任务NPC、任务链与撤离玩法形成完整闭环。

### 前置条件
- 阶段4完成
- 任务系统、对话系统、成长系统可用
- 至少有一张支持扩展的正式战斗地图或升级后的样例地图

### 状态真相约定
- `QuestManager` 负责原子任务目标、提交条件、完成状态
- `QuestChainRuntime` 负责任务链阶段推进，不负责记录原子目标计数
- `WorldStateService` 负责商人解锁、NPC显隐、剧情标记、章节性世界结果
- `NarrativeDirector` 只负责剧情播放和调度，不直接持有长期存档真相
- 更细的读写边界、回写时机、禁止模式以 [StateOwnershipAndPersistenceRules.md](./StateOwnershipAndPersistenceRules.md) 为准

### 任务列表

#### 5.1 世界状态与解锁状态服务
**优先级**：最高
**预计时间**：1周

**任务描述**
- 创建世界状态数据结构，用于记录商人解锁、任务链阶段、剧情标记、地图交互状态
- 实现世界状态的存档与加载
- 实现世界状态对基地商人、地图商人、任务NPC可见性的驱动
- 定义撤离成功、死亡失败、任务中断时的状态回写规则

**技术要点**
```csharp
public class WorldStateData
{
    public HashSet<string> unlockedRaidMerchantIds = new();
    public HashSet<string> unlockedRaidNpcIds = new();
    public Dictionary<string, int> questChainStages = new();
    public HashSet<string> storyFlags = new();
}

public interface IWorldStateService
{
    bool HasFlag(string flagId);
    void SetFlag(string flagId);
    int GetQuestChainStage(string chainId);
    void AdvanceQuestChain(string chainId);
}
```

- `WorldStateService` 不保存原子任务目标计数，只保存解锁和全局结果状态，具体约束见 [StateOwnershipAndPersistenceRules.md](./StateOwnershipAndPersistenceRules.md)

**验收标准**
- [ ] 地图商人与任务NPC可根据世界状态解锁/隐藏
- [ ] 剧情标记与任务链阶段可跨场景、跨存档保存
- [ ] 回档或重新进入战斗地图时状态不丢失
- [ ] 撤离与死亡的状态回写规则明确且可验证

**相关文件**
- 新增：`WorldStateData.cs`, `WorldStateService.cs`, `StoryFlagId.cs`
- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`

#### 5.2 战斗地图中立交互区框架
**优先级**：最高
**预计时间**：5-7天

**任务描述**
- 为战斗地图中的商人和任务NPC创建中立交互区
- 实现玩家靠近、提示、交互、对话、交易的完整流程
- 处理交互时玩家输入冻结、镜头锁定、AI威胁检测暂停等行为
- 预留安全区、隐藏营地、临时商点等不同交互形态

**技术要点**
- 复用 `IInteractable` 与 `PlayerInteractor`，不要独立做第二套交互入口
- 使用 `RaidNpcZone` 处理中立区域规则，而不是把规则写死在NPC脚本里
- 对话与交易期间统一通过 `PlayerInteractionState` 管理输入焦点
- 明确“可交互不代表安全”，避免系统隐含无敌区逻辑

**验收标准**
- [ ] 玩家可在战斗地图中与中立NPC稳定交互
- [ ] 交互提示、距离、输入冻结行为一致
- [ ] 退出对话或交易后玩家控制正确恢复
- [ ] 中立交互逻辑不破坏现有战斗与AI系统

**相关文件**
- 新增：`RaidNpcZone.cs`, `RaidNpcInteractor.cs`, `RaidNpcPromptView.cs`
- `Assets/Res/Scripts/Interaction/IInteractable.cs`
- `Assets/Res/Scripts/Interaction/PlayerInteractor.cs`

#### 5.3 战斗地图商人解锁系统
**优先级**：高
**预计时间**：1周

**任务描述**
- 为战斗地图商人创建数据定义、解锁条件和库存规则
- 支持“先完成特定任务/任务链，后在地图中出现并可交互”
- 支持地图商人提供特殊服务，如补给、临时交易、任务交付、维修
- 实现地图商人与基地商人的差异化定位

**技术要点**
```csharp
public class RaidMerchantDefinition : ScriptableObject
{
    public string merchantId;
    public string displayName;
    public string mapId;
    public List<string> unlockFlags;
    public List<string> questChainRequirements;
    public MerchantServiceType[] services;
}

public enum MerchantServiceType
{
    Trade,
    Repair,
    QuestTurnIn,
    EmergencySupply
}
```

**验收标准**
- [ ] 未解锁前地图商人不可见或不可交互
- [ ] 完成对应任务后地图商人正确解锁
- [ ] 地图商人可提供至少一种基地商人没有的特殊服务
- [ ] 商人解锁状态写回世界状态并驱动后续内容

**相关文件**
- 新增：`RaidMerchantDefinition.cs`, `RaidMerchantNpc.cs`, `MerchantServiceType.cs`
- 新增：`RaidMerchantService.cs`, `MerchantUnlockCondition.cs`
- `Assets/Res/Scripts/Profile/PrototypeMerchantCatalog.cs`

#### 5.4 战斗地图任务NPC与任务链
**优先级**：最高
**预计时间**：1-2周

**任务描述**
- 创建战斗地图任务NPC框架
- 支持任务链的接取、阶段推进、局内提交、跨局延续
- 支持“在地图中遇见NPC -> 接取任务 -> 回基地推进剧情 -> 再次进图继续”的结构
- 支持任务链对商人解锁、剧情标记、地图内容变化的联动

**技术要点**
- 任务链数据与单个任务分离，避免复杂剧情逻辑堆在单任务对象上
- 任务链阶段推进统一走 `WorldStateService`，不要把阶段状态只存进 `Quest`
- 允许任务链包含对话目标、交互目标、护送目标、提交目标等不同节点
- 为后续章节剧情和CG触发保留“阶段完成事件”

**验收标准**
- [ ] 战斗地图中可从NPC接取任务链
- [ ] 任务链阶段可跨多次出击继续推进
- [ ] 任务链可解锁地图商人或后续NPC
- [ ] 任务链失败/中断时有清晰回退或重试规则

**相关文件**
- 新增：`RaidQuestNpc.cs`, `QuestChainDefinition.cs`, `QuestChainRuntime.cs`
- 新增：`QuestChainStep.cs`, `RaidQuestNpcPresenter.cs`
- `QuestManager.cs`, `DialogueSystem.cs`

#### 5.5 局内任务目标与事件触发器
**优先级**：高
**预计时间**：1周

**任务描述**
- 扩展任务目标类型，支持交互、提交、扫描、区域停留、剧情触发、特殊撤离
- 创建战斗地图任务触发器组件
- 支持基于地图对象、容器、门禁、Boss、特定商人/NPC 的任务事件
- 支持任务引导标记与地图内反馈

**技术要点**
- 新增 `InteractObjective`、`TurnInObjective`、`StayInZoneObjective`、`TriggerStoryObjective`
- 地图触发器应只发事件，不直接改任务状态
- `QuestManager` 作为任务状态唯一真相来源
- 与 `RaidGameMode` 联动支持“特殊撤离成功才算完成”的任务

**验收标准**
- [ ] 新目标类型可在战斗地图中稳定触发
- [ ] 触发器不会因重复进入场景而重复结算
- [ ] 任务HUD可正确显示局内目标进度
- [ ] 特殊撤离任务可被正确检测

**相关文件**
- 新增：`InteractObjective.cs`, `TurnInObjective.cs`, `StayInZoneObjective.cs`
- 新增：`QuestTriggerVolume.cs`, `QuestEventEmitter.cs`
- `QuestManager.cs`, `RaidGameMode.cs`

#### 5.6 战斗地图NPC与任务摆放工具
**优先级**：中
**预计时间**：5-7天

**任务描述**
- 为地图商人、任务NPC、触发器、剧情点创建摆放工具
- 支持在编辑器中可视化查看解锁条件、任务链阶段与触发器关联
- 统一管理战斗地图中的中立内容点位
- 降低后续地图扩张时的内容制作成本

**技术要点**
- 复用现有 `PrototypeRaidToolkitWindow`，不要平行创建第二套地图工具链
- 用配置资产驱动摆放，不把任务逻辑硬编码进场景对象
- 对地图内容点使用唯一ID，便于世界状态回写和问题排查

**验收标准**
- [ ] 可通过工具快速放置和配置战斗地图NPC
- [ ] 可视化查看NPC与任务链/商人配置的关系
- [ ] 点位ID稳定，不因场景重排导致状态错乱
- [ ] 新地图中复制一套NPC系统成本可控

**相关文件**
- 新增：`RaidNpcPlacementDefinition.cs`, `RaidQuestPointDefinition.cs`
- `Assets/Res/Scripts/LevelDesign/Editor/PrototypeRaidToolkitWindow.cs`

---

## 阶段6：剧情内容制作

### 目标
搭建完整的剧情表现层，支持对话、CG、动态视频与过场动画，并与任务和世界状态联动。

### 前置条件
- 阶段5完成
- 世界状态服务可用
- 任务链与对话系统可稳定驱动内容

### 任务列表

#### 6.1 章节叙事数据与剧情编排
**优先级**：最高
**预计时间**：1-2周

**任务描述**
- 创建章节、段落、剧情事件、演出步骤的数据结构
- 为主线、支线、地图任务链提供统一叙事编排方式
- 支持基于世界状态决定剧情分支和演出顺序
- 支持剧情完成后写入标记并解锁后续内容

**技术要点**
```csharp
public class NarrativeSequenceDefinition : ScriptableObject
{
    public string sequenceId;
    public List<string> requiredFlags;
    public List<NarrativeStep> steps;
    public List<string> rewardFlags;
}

public abstract class NarrativeStep
{
    public string stepId;
    public abstract IEnumerator Play(NarrativeContext context);
}
```

**验收标准**
- [ ] 可用一套数据结构编排主线与支线剧情
- [ ] 剧情分支可被世界状态驱动
- [ ] 剧情演出完成后能推进任务链或解锁新内容
- [ ] 不同表现形式可在同一剧情序列中混用

**相关文件**
- 新增：`NarrativeSequenceDefinition.cs`, `NarrativeStep.cs`, `NarrativeContext.cs`
- 新增：`NarrativeDirector.cs`, `NarrativeEventBus.cs`

#### 6.2 CG展示系统
**优先级**：高
**预计时间**：5-7天

**任务描述**
- 支持静态CG、插画、章节封面展示
- 支持字幕、旁白、淡入淡出、镜头平移缩放
- 支持在基地、任务提交、关键战斗后播放CG
- 支持CG回看或图鉴解锁

**技术要点**
- 使用统一 `PresentationStep` 包装图片、文本、音频、时间轴控制
- 区分“仅展示CG”和“带字幕/选择分支的剧情CG”
- 支持跳过、快进与已看内容回放

**验收标准**
- [ ] 可稳定播放至少一段完整CG剧情
- [ ] 字幕、音频与CG切换同步
- [ ] 支持跳过与回放
- [ ] 播放完成后可正确恢复到游戏流程

**相关文件**
- 新增：`CgPresentationStep.cs`, `CgViewerController.cs`, `CgGalleryService.cs`
- 新增：`Assets/Res/Data/Narrative/CG`

#### 6.3 动态视频播放支持
**优先级**：中
**预计时间**：3-5天

**任务描述**
- 集成Unity `VideoPlayer` 播放预渲染剧情片段
- 支持视频前后接续任务、对话、CG、Timeline
- 支持资源缺失时回退到静态图或文字描述
- 统一视频音频混音与跳过逻辑

**技术要点**
- 视频播放不应阻塞主线程中的剧情状态机
- 为每个视频定义可跳过点、回退表现、完成回调
- 预留低配平台禁用视频、使用静态演出的降级方案

**验收标准**
- [ ] 可播放预渲染剧情视频
- [ ] 视频结束后能继续后续剧情步骤
- [ ] 视频可跳过且状态不丢失
- [ ] 缺少视频资源时有安全回退方案

**相关文件**
- 新增：`VideoPresentationStep.cs`, `NarrativeVideoPlayer.cs`
- 新增：`Assets/Res/Data/Narrative/Videos`

#### 6.4 过场动画与Timeline演出
**优先级**：高
**预计时间**：1周

**任务描述**
- 为关键剧情、Boss出场、基地事件搭建Timeline演出流程
- 支持相机切换、角色站位、动画触发、字幕同步
- 支持与局内地图NPC、基地商人、任务NPC共享演出接口
- 实现演出开始与结束时的输入、HUD、AI状态切换

**技术要点**
- 用 `PlayableDirector` 和可复用的绑定层管理演员引用
- 场景中的剧情角色绑定不要写死到Timeline资源里
- 演出期间由 `NarrativeDirector` 统一管理游戏暂停、UI隐藏与控制权恢复

**验收标准**
- [ ] 至少一段基地过场和一段局内过场可稳定播放
- [ ] 相机、角色动画、字幕能同步工作
- [ ] 过场结束后游戏状态可正确恢复
- [ ] Timeline资源可复用于不同剧情事件

**相关文件**
- 新增：`TimelinePresentationStep.cs`, `CutsceneBindingResolver.cs`
- 新增：`CutsceneStateController.cs`

#### 6.5 剧情触发与任务联动
**优先级**：最高
**预计时间**：1周

**任务描述**
- 实现任务完成、商人解锁、进入特定区域等触发剧情的逻辑
- 支持剧情结束后发奖励、写标记、推进章节
- 统一处理剧情失败、跳过、重复播放、回放逻辑
- 支持基地内固定商人、地图内商人、任务NPC共用触发规则

**技术要点**
- 任何剧情触发都先判断世界状态，避免重复播放
- 任务系统与剧情系统通过事件通信，不直接互相依赖内部状态
- 建立剧情调试模式，允许快速跳转特定剧情节点

**验收标准**
- [ ] 任务推进可触发对应剧情
- [ ] 剧情结束后能正确推进任务或世界状态
- [ ] 重复进入场景不会重复播放一次性剧情
- [ ] 跳过剧情不影响任务链与解锁逻辑

**相关文件**
- 新增：`NarrativeTriggerDefinition.cs`, `NarrativeConditionChecker.cs`
- `QuestManager.cs`, `WorldStateService.cs`, `DialogueSystem.cs`

#### 6.6 剧情回顾与调试工具
**优先级**：中
**预计时间**：3-5天

**任务描述**
- 提供剧情回顾、章节摘要、已解锁CG/视频列表
- 提供剧情调试面板，便于快速定位状态问题
- 提供跳转章节、设置Flag、模拟任务完成等开发功能

**技术要点**
- 开发工具与正式UI分离
- 调试入口只在编辑器或开发构建中启用
- 调试工具写世界状态时要有回滚和日志记录

**验收标准**
- [ ] 开发阶段可快速验证剧情分支
- [ ] 玩家可回看已解锁剧情表现
- [ ] 调试工具不会污染正式玩家存档

**相关文件**
- 新增：`NarrativeDebugWindow.cs`, `StoryArchivePanel.cs`

---

## 阶段7：制作与经济系统

### 目标
把基地从“功能菜单”升级为完整的长期成长中枢，形成材料、制作、升级、交易与装备养成闭环。

### 前置条件
- 阶段6完成
- 基地场景、商人系统、成长系统、世界状态服务可用

### 任务列表

#### 7.1 配方与制作系统
**优先级**：最高
**预计时间**：1-2周

**任务描述**
- 创建材料、配方、制作站、制作队列系统
- 支持基础弹药、医疗物资、任务道具、装备部件制作
- 支持基地设施等级影响制作内容与速度
- 支持制作取消、领取、失败回退规则

**技术要点**
```csharp
public class CraftRecipeDefinition : ScriptableObject
{
    public string recipeId;
    public FacilityType requiredFacility;
    public int requiredFacilityLevel;
    public List<ItemCost> costs;
    public List<ItemReward> rewards;
    public float craftDuration;
}

public class CraftingQueueEntry
{
    public string recipeId;
    public float endTimestamp;
    public CraftingState state;
}
```

**验收标准**
- [ ] 玩家可在基地工作台执行制作
- [ ] 制作内容受设施等级影响
- [ ] 领取与取消逻辑正确
- [ ] 制作产出可进入对应容器或仓库

**相关文件**
- 新增：`CraftRecipeDefinition.cs`, `CraftingService.cs`, `CraftingQueueEntry.cs`
- 新增：`CraftingPanel.cs`, `FacilityWorkbenchPresenter.cs`

#### 7.2 基地正式化与设施深度化
**优先级**：高
**预计时间**：1-2周

**任务描述**
- 把基地场景从基础交互场景升级为正式Hub
- 固化商人、工作台、任务终端、剧情触发点的空间布局
- 实现基地设施升级、可视变化、功能解锁与剧情联动
- 支持从基地场景直接进入准备、交易、成长、剧情与出击

**技术要点**
- 保持“固定位置商人 + 场景内交互打开对应功能”的核心体验
- 设施升级不只改数值，还要驱动场景中的表现变化
- 入口流转与快速传送仅作为体验优化，不能替代基础场景交互
- 基地场景应成为剧情、成长、经济系统的统一落点
- `MainMenu` 在该阶段后应只保留启动壳 / 调试用途，正式局外入口以 `BaseScene` 为准

**验收标准**
- [ ] 玩家可从基地场景完成核心局外循环
- [ ] 商人、工作台、任务点位布局清晰
- [ ] 设施升级后场景与功能同步变化
- [ ] 基地场景可承载剧情和任务推进

**相关文件**
- `Assets/Scenes/BaseScene.unity`
- 新增：`BaseHubDirector.cs`, `BaseFacilityVisualController.cs`
- `BaseFacilityManager.cs`, `MerchantNPC.cs`, `QuestNPC.cs`

#### 7.3 商人库存刷新与特殊订单
**优先级**：高
**预计时间**：1周

**任务描述**
- 实现商人库存刷新、限量商品、特殊订单、阶段性上新
- 支持基地商人与地图商人共用库存规则框架
- 支持世界状态、信誉、任务章节影响库存内容
- 支持高品质装备、稀有材料、任务关键道具的受控投放

**技术要点**
- 商人库存生成应区分固定商品、随机商品、阶段商品、限量商品
- 刷新规则应可按天、按章节、按任务阶段配置
- 避免把剧情关键道具完全交给随机刷新
- 商人等级、信誉、世界状态、刷新时钟的职责边界以 [MerchantProgressionMatrix.md](./MerchantProgressionMatrix.md) 为准

**验收标准**
- [ ] 商人库存可按规则刷新
- [ ] 特殊订单可通过任务或剧情解锁
- [ ] 高价值商品的投放节奏可控
- [ ] 基地商人与地图商人可共用部分库存生成逻辑

**相关文件**
- 新增：`MerchantStockRule.cs`, `MerchantOrderDefinition.cs`, `MerchantRefreshService.cs`
- `Assets/Res/Scripts/Profile/PrototypeMerchantCatalog.cs`

#### 7.4 装备强化、词条重铸与维修
**优先级**：中
**预计时间**：1周

**任务描述**
- 实现装备维修、耐久恢复、强化和词条重铸
- 支持消耗材料或货币改变装备成长结果
- 控制品质、词条、技能出现和提升的边界
- 让RPG装备成长形成长期目标，而不直接破坏搜打撤风险收益

**技术要点**
- 词条重铸要保留足够成本与风险，避免无限刷最优词条
- 装备强化应与品质、等级、词条数量分开设计
- 维修系统应与装备实例耐久直接关联

**验收标准**
- [ ] 玩家可维修和强化装备
- [ ] 词条重铸逻辑正确且成本合理
- [ ] 维修/强化结果可持久化
- [ ] 不同品质装备的养成上限存在区分

**相关文件**
- 新增：`EquipmentUpgradeService.cs`, `AffixRerollService.cs`, `EquipmentRepairService.cs`
- `Assets/Res/Scripts/Items/Runtime/ItemInstance.cs`

#### 7.5 经济平衡与资源闭环
**优先级**：中
**预计时间**：1周

**任务描述**
- 梳理货币、材料、药品、弹药、任务物资、稀有资源的获取与消耗路径
- 建立收益、支出、损耗、成长成本之间的平衡关系
- 设计单机模式下的长期经济目标，避免中后期通货膨胀
- 建立调试统计面板辅助平衡

**技术要点**
- 为局内收益、商店收益、制作收益、任务收益建立统一统计
- 重点监控稀有词条装备和高阶材料的产出速率
- 保留单机游戏的爽感，但避免资源完全失去意义

**验收标准**
- [ ] 有可用的经济平衡调试数据
- [ ] 中期和后期仍存在资源决策压力
- [ ] 装备养成、任务推进、商店交易不会互相冲垮

**相关文件**
- 新增：`EconomyBalanceService.cs`, `EconomyDebugPanel.cs`
- `CraftingService.cs`, `PrototypeMerchantCatalog.cs`, `QuestManager.cs`

---

## 阶段8：内容扩展

### 目标
从系统验证阶段进入内容生产阶段，扩展地图、章节、敌人、装备与任务体量，形成完整的单机可游玩内容。

### 前置条件
- 阶段7完成
- 剧情、成长、经济、战斗地图NPC体系可稳定复用

### 任务列表

#### 8.1 正式战斗地图扩张
**优先级**：最高
**预计时间**：2-4周

**任务描述**
- 设计并制作多张正式战斗地图
- 为每张地图定义主题、敌人分布、任务点、商人/NPC点、撤离规则
- 支持地图难度分层与推荐等级区间
- 形成地图间的剧情和经济差异

**验收标准**
- [ ] 至少有2-3张可重复游玩的正式地图
- [ ] 每张地图有独立的任务与资源定位
- [ ] 地图间难度和收益梯度清晰

**相关文件**
- 新增：正式地图场景与配置资产
- `PrototypeRaidToolkitWindow.cs`

#### 8.2 敌人派系、Boss与特殊遭遇
**优先级**：高
**预计时间**：2-3周

**任务描述**
- 扩展敌人派系、行为模式、装备池
- 制作地图Boss、精英敌人、特殊遭遇事件
- 让任务链、剧情和高价值Loot与遭遇内容形成绑定

**验收标准**
- [ ] 不同地图拥有差异化敌人内容
- [ ] 存在可服务任务和剧情的Boss遭遇
- [ ] 特殊遭遇可成为重复游玩的驱动力

**相关文件**
- 新增：敌人派系配置、Boss定义、遭遇事件配置
- `Assets/Res/Scripts/AI/PrototypeBotController.cs`

#### 8.3 商人网络与任务线扩展
**优先级**：高
**预计时间**：2-4周

**任务描述**
- 扩展基地商人和地图商人的数量与定位
- 为不同商人配置长期任务线、解锁链、剧情分支
- 让地图商人和基地商人之间形成叙事与经济联动

**验收标准**
- [ ] 每个主要商人都有独立功能定位
- [ ] 商人任务线可以驱动剧情和解锁
- [ ] 地图商人与基地商人不再只是UI入口，而是内容节点

**相关文件**
- 新增：商人任务线数据、商人剧情配置
- `RaidMerchantDefinition.cs`, `MerchantData.cs`

#### 8.4 装备库、词条池与技能池扩展
**优先级**：高
**预计时间**：2-3周

**任务描述**
- 扩展武器、护甲、词条、技能、套装或主题构筑
- 形成中期和后期构筑目标
- 支持不同地图掉落不同风格装备

**验收标准**
- [ ] 装备构筑有明显差异化方向
- [ ] 不同地图/商人/任务线可导向不同Build
- [ ] 装备内容量足以支撑长期游玩

**相关文件**
- 新增：装备定义、词条池、技能池配置资产
- `ItemInstance.cs`, `AffixPool.cs`, `ItemSkill.cs`

#### 8.5 章节与支线内容量产
**优先级**：中
**预计时间**：持续迭代

**任务描述**
- 基于已有任务链与剧情系统扩展主线章节
- 扩展支线、地图事件、隐藏任务、NPC故事线
- 为CG、视频、过场制作稳定的内容产线

**验收标准**
- [ ] 主线章节可持续扩展
- [ ] 支线和隐藏内容能提升探索价值
- [ ] 叙事内容生产不再严重依赖程序改代码

**相关文件**
- 新增：章节剧情数据、支线任务数据、演出资源

---

## 阶段9：优化与完善

### 目标
完成单机正式版本所需的性能、体验、稳定性与内容质量打磨。

### 前置条件
- 阶段8完成
- 全量核心内容已经可完整游玩

### 任务列表

#### 9.1 性能优化与加载流程
**优先级**：最高
**预计时间**：持续迭代

**任务描述**
- 优化基地场景、战斗地图、UI、AI、Loot系统的性能开销
- 优化加载、切图、资源预热、对象池与场景切换
- 为剧情演出、视频、CG引入资源分级加载策略

**验收标准**
- [ ] 核心场景帧率稳定
- [ ] 场景切换与剧情播放卡顿可控
- [ ] 高密度战斗与大量Loot情况下性能仍可接受

**相关文件**
- 涉及战斗、UI、演出、资源管理相关系统

#### 9.2 战斗、AI与RPG数值平衡
**优先级**：最高
**预计时间**：持续迭代

**任务描述**
- 统一校准武器、护甲、词条、技能、成长系统的数值关系
- 调整敌人AI强度、地图收益、任务难度和商人库存
- 让“搜打撤风险收益”与“RPG成长爽感”取得平衡

**验收标准**
- [ ] 不存在单一Build完全破坏平衡的问题
- [ ] 不同阶段的成长收益合理
- [ ] 战斗难度与奖励梯度匹配

**相关文件**
- 涉及战斗、成长、商店、任务、Loot相关配置资产

#### 9.3 存档稳定性与版本迁移
**优先级**：高
**预计时间**：1周

**任务描述**
- 梳理成长、世界状态、任务链、剧情、装备实例的存档结构
- 实现版本升级迁移、损坏恢复、调试备份
- 覆盖长流程游玩后的存档一致性问题

**技术要点**
- 存档迁移规则、字段归属、回写边界以 [StateOwnershipAndPersistenceRules.md](./StateOwnershipAndPersistenceRules.md) 为准
- 重点验证 `ProfileData`、`WorldStateData`、`QuestChainRuntime`、制作队列、装备实例数据的向后兼容
- 迁移失败时保留备份、输出诊断信息，并禁止直接覆盖原始存档
- 长流程测试需覆盖基地、战斗地图、任务链、剧情播放、制作完成后的连续读写

**验收标准**
- [ ] 长流程游玩后存档稳定
- [ ] 新版本可迁移旧版本存档
- [ ] 关键世界状态与任务链不会错乱

**相关文件**
- `PrototypeProfileService.cs`
- 新增：存档迁移与校验相关工具

#### 9.4 UI、音频与视觉表现打磨
**优先级**：中
**预计时间**：持续迭代

**任务描述**
- 把基地、任务、成长、商店、制作、剧情相关UI从原型体验打磨到正式体验
- 完善提示、音效、环境反馈、任务指引、剧情节奏
- 提升基地与战斗地图的视觉识别与氛围差异

**验收标准**
- [ ] UI层级清晰、交互顺畅
- [ ] 音频和视觉反馈能支持核心玩法体验
- [ ] 剧情表现与系统交互风格统一

**相关文件**
- UI、音频、视觉表现相关资源与脚本

#### 9.5 QA、工具链与发布准备
**优先级**：中
**预计时间**：1-2周

**任务描述**
- 补充关键流程检查清单与开发测试工具
- 整理内容导入、配置校验、任务状态检查、商人解锁检查工具
- 准备设置、按键、难度、存档槽位等正式版本基础功能

**验收标准**
- [ ] 核心流程有稳定的QA检查路径
- [ ] 常见内容配置错误可被工具提前发现
- [ ] 已具备面向正式单机版本的基础发布条件

**相关文件**
- 新增：内容校验工具、QA调试工具、发布准备相关配置

---

## 补充说明

本文件对应中后期阶段路线图本体，更细的执行拆分、里程碑批次和 AI 任务包见：

- [DevelopmentRoadmap_Part3.md](./DevelopmentRoadmap_Part3.md)
