# 状态真相与持久化约定

## 1. 文档目的

本文件用于明确 Project-XX 中后期开发里最容易失控的两件事：

- 哪个系统才是某类状态的唯一真相来源
- 这些状态应该在什么时机写回存档

这份文档不替代路线图，而是为路线图中的成长、任务链、世界状态、剧情、制作系统提供统一边界。

---

## 2. 单一真相来源

| 状态域 | 唯一真相来源 | 是否持久化 | 主要负责内容 | 明确不负责 |
| --- | --- | --- | --- | --- |
| 玩家资料 / 仓库 / 装备实例 | `PrototypeProfileService` | 是 | 局外容器、装备实例、成长数据、设施数据、制作队列 | 局内运行时瞬时状态 |
| 世界状态 | `WorldStateService` | 是 | 商人解锁、NPC显隐、一次性剧情标记、章节标记、任务链阶段结果 | 原子任务目标进度、剧情播放过程状态 |
| 原子任务状态 | `QuestManager` | 是 | 任务接取、目标进度、完成/失败、奖励发放资格 | 商人解锁显隐、剧情播放控制 |
| 任务链状态 | `QuestChainRuntime` + `WorldStateService` | 是 | 多任务串联、阶段推进、链路前后关系 | 单个任务目标计数 |
| 剧情播放状态 | `NarrativeDirector` | 否 | 当前演出步骤、播放中断、跳过、恢复、绑定 | 任何长期状态真相 |
| 战斗属性汇总 | `CharacterStatAggregator` | 否 | 成长、装备、词条、技能、Buff 的统一派生属性结果 | Profile 落盘、任务状态 |

---

## 3. 归属细则

### 3.1 Quest 与 QuestChain 的关系

- `Quest` 只关心单个任务的原子目标和提交状态。
- `QuestChain` 只关心“当前应该推进到哪一步”，并引用具体的 `Quest`。
- `QuestChain` 不重复保存 `Quest` 的目标进度。
- `QuestManager` 不直接保存剧情章节和商人解锁。

### 3.2 WorldState 与 Narrative 的关系

- `WorldStateService` 保存“剧情是否已经发生”的结果。
- `NarrativeDirector` 只负责“剧情正在怎么播放”。
- 剧情一旦播放完成，才允许通过 `WorldStateService` 写入永久标记。
- 跳过剧情不应跳过结果写回，但也不能让 `NarrativeDirector` 自己持久化。

### 3.3 Profile 与 Raid 回写的关系

- 局外配置修改应立即经由 `PrototypeProfileService` 写回。
- 局内结果应通过 `PrototypeRaidProfileFlow` 汇总后统一写回。
- 禁止任何局内运行时脚本直接手写 Profile 持久化。

---

## 4. 存档版本策略

### 4.1 最低要求

- 根存档必须包含 `profileSchemaVersion`
- 世界状态子结构必须包含 `worldStateVersion`
- 成长数据子结构必须包含 `progressionDataVersion`

### 4.2 迁移规则

- 每次新增持久化字段时，都要补对应迁移器
- 迁移必须支持从至少上一个正式版本升级
- 迁移失败时必须保留原始备份
- 迁移完成后要记录日志，便于排查玩家档问题

### 4.3 建议结构

```csharp
public class ProfileData
{
    public int profileSchemaVersion;
    public PlayerProgressionData progression;
    public WorldStateData worldState;
    public List<SavedItemInstanceDto> itemInstances;
    public List<CraftingQueueEntryDto> craftingQueue;
}
```

---

## 5. 写回时机约定

### 5.1 立即写回

- 局外商店购买 / 出售
- 基地成长点分配
- 技能树解锁
- 设施升级
- 制作队列开始 / 取消 / 领取

### 5.2 战局结算时写回

- 风险区物品带出 / 丢失
- 装备实例耐久、弹药
- 特定任务完成结果
- 局内获得但未结算的收益

### 5.3 事件提交时写回

- 地图商人解锁
- 地图任务NPC任务链阶段推进
- 一次性剧情标记
- 特殊区域发现状态

---

## 6. 禁止事项

- 禁止 UI 直接修改成长、任务、世界状态真相数据
- 禁止 `NarrativeDirector` 自己保存剧情长期状态
- 禁止 `QuestChainRuntime` 复制一份任务目标进度
- 禁止把关键解锁条件只写在编辑器工具或场景对象里

---

## 7. 与路线图的关系

建议把本文件作为下列阶段的硬约束：

- `阶段0.4` 存档版本化与迁移基线
- `阶段2.6` 局外入口迁移与兼容层
- `阶段5.1` 世界状态与解锁状态服务
- `阶段9.3` 存档稳定性与版本迁移
