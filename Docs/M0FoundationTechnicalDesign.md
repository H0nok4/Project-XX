# Project-XX M0 技术设计

## 1. 文档目的

本设计用于细化 `DevelopmentRoadmap_Part3.md` 中的 `M0：前置治理与入口迁移`，把路线图条目下沉为可执行的技术方案。

本阶段只解决两个底层问题：

- `M0-A`：给存档建立稳定的 schema 版本化、迁移、备份与诊断入口
- `M0-B`：把 `MainMenu` 到 `BaseScene` 的局外入口迁移做成可兼容演进的路由层

本设计不负责：

- 完整基地玩法内容
- 成长系统、任务链、商人内容的正式实现
- UI 视觉重做

---

## 2. 当前状态与问题

结合现有代码与文档，当前有四个直接风险：

1. `PrototypeProfileService` 同时承担 DTO 定义、读写、Sanitize、数据转换，缺少独立迁移入口。
2. 现有存档只有 `version`，没有分层 schema 版本，也没有备份和诊断输出。
3. `PrototypeMainMenuController` 同时承担局外 UI、Profile 管理、场景进入逻辑，后续极易继续膨胀。
4. `PrototypeRaidProfileFlow` 仍把返回入口写死为 `MainMenu`，不适合后续基地 Hub。

现有锚点：

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`
- `Assets/Res/Scripts/UI/Editor/PrototypeMainMenuSceneBuilder.cs`

---

## 3. 设计目标

### 3.1 M0-A 目标

- 让根存档具备明确的 schema 版本号
- 支持从现有旧档平滑迁移
- 让后续 `WorldStateData`、`PlayerProgressionData`、制作队列等结构可继续演进
- 迁移失败时保留原档并输出可读诊断

### 3.2 M0-B 目标

- 明确 `MainMenu` 与 `BaseScene` 的长期职责
- 把场景跳转从业务控制器中抽离到统一路由层
- 保留调试入口，不强行一次性废掉原型主菜单
- 为后续基地正式化预留统一返回点和启动路径

---

## 4. M0-A 存档Schema版本化与迁移设计

### 4.1 设计原则

- `PrototypeProfileService` 继续作为外部入口 facade，但不再独占所有内部职责
- 读盘、迁移、Sanitize、诊断分层处理
- 一切新持久化字段必须经由迁移入口落盘
- 不允许任意系统自己新增一套 JSON 文件

### 4.2 目标数据结构

根存档结构建议演进为：

```csharp
[Serializable]
public sealed class ProfileData
{
    public int profileSchemaVersion = CurrentProfileSchemaVersion;
    public int legacyVersion = 2;
    public WorldStateData worldState = new WorldStateData();
    public PlayerProgressionData progression = new PlayerProgressionData();

    public List<ItemStackRecord> stashItems = new();
    public List<ItemStackRecord> loadoutItems = new();
    public List<ItemStackRecord> extractedItems = new();
    public List<ItemStackRecord> raidBackpackItems = new();
    public List<ItemStackRecord> secureContainerItems = new();
    public List<ItemStackRecord> specialEquipmentItems = new();
    public List<ItemStackRecord> equippedArmorItems = new();
    public List<string> stashWeaponIds = new();
    public string equippedPrimaryWeaponId = string.Empty;
    public string equippedSecondaryWeaponId = string.Empty;
    public string equippedMeleeWeaponId = string.Empty;
}
```

子结构最小壳体：

```csharp
[Serializable]
public sealed class WorldStateData
{
    public int worldStateVersion = 1;
    public List<string> unlockedRaidMerchantIds = new();
    public List<string> unlockedRaidNpcIds = new();
    public List<QuestChainStageRecord> questChainStages = new();
    public List<string> storyFlags = new();
}
```

```csharp
[Serializable]
public sealed class PlayerProgressionData
{
    public int progressionDataVersion = 1;
}
```

说明：

- `legacyVersion` 用于兼容当前 `version` 语义，不作为未来主版本号。
- `profileSchemaVersion` 才是后续正式迁移入口。
- `WorldStateData` 和 `PlayerProgressionData` 先提供最小合法结构，先解决“结构占位和迁移入口”，不要求 M0 就把业务做完。

### 4.3 建议新增类型

新增下列类或文件：

- `ProfileFileGateway`
- `ProfileMigrationRunner`
- `ProfileMigrationResult`
- `IProfileMigrationStep`
- `WorldStateData`
- `PlayerProgressionData`
- `ProfileDiagnostics`

职责划分：

| 类型 | 责任 | 不负责 |
| --- | --- | --- |
| `PrototypeProfileService` | 对外 facade、数据转换、外部 API 保持稳定 | 原始文件备份细节、迁移步骤调度 |
| `ProfileFileGateway` | 定位文件、读取原始 JSON、写入 JSON、创建备份 | DTO 业务清洗 |
| `ProfileMigrationRunner` | 识别版本、按顺序执行迁移步骤 | 场景流转 |
| `ProfileDiagnostics` | 输出迁移日志、校验问题、异常摘要 | 实际持久化 |
| `WorldStateData` | 世界状态数据容器 | 任务原子进度 |

### 4.4 读取流程

建议把读取流程调整为：

1. `PrototypeProfileService.LoadProfile(catalog)`
2. `ProfileFileGateway.TryReadRawJson(out rawJson, out backupPathHint)`
3. `ProfileMigrationRunner.ParseAndUpgrade(rawJson)`
4. 迁移成功后得到 `ProfileData`
5. `PrototypeProfileService.SanitizeProfile(profile, catalog)`
6. 如果发生升级，立即回写升级后的新结构
7. 返回内存中的 `ProfileData`

伪代码：

```csharp
public static ProfileData LoadProfile(PrototypeItemCatalog catalog)
{
    string rawJson = ProfileFileGateway.TryReadRawJson();
    ProfileMigrationResult migration = ProfileMigrationRunner.ParseAndUpgrade(rawJson, catalog);

    ProfileData profile = migration.Profile ?? CreateDefaultProfile(catalog);
    SanitizeProfile(profile, catalog);

    if (migration.Upgraded)
    {
        SaveProfile(profile, catalog);
    }

    return profile;
}
```

### 4.5 保存流程

建议把保存流程调整为：

1. `PrototypeProfileService.SaveProfile(profile, catalog)`
2. `SanitizeProfile`
3. `ProfileDiagnostics.ValidateBeforeSave(profile)`
4. `ProfileFileGateway.WriteJson(profileSchemaVersionedJson)`
5. 失败时输出错误并保留最近一次成功存档

保存时要求：

- 永远写 `profileSchemaVersion`
- 子结构为空时仍保留合法默认对象
- 不因为某个可选子结构为空而写出半残档

### 4.6 迁移策略

首批迁移只需要覆盖当前已存在的旧档。

建议定义：

- `LegacyVersion 2 -> ProfileSchemaVersion 1`

迁移动作：

1. 读取旧 `version`
2. 映射到 `profileSchemaVersion = 1`
3. 如果 `worldState` 不存在，补默认 `WorldStateData`
4. 如果 `progression` 不存在，补默认 `PlayerProgressionData`
5. 保留原有背包、武器、护甲和保护区数据
6. 输出 `migration.log`

后续规则：

- 每次 schema 升级只新增一个明确版本步进
- 迁移器按 `1 -> 2 -> 3` 顺序串行，不允许跨版本硬跳逻辑散落在 `SanitizeProfile`

### 4.7 备份与诊断策略

建议备份目录：

- `Application.persistentDataPath/ProfileBackups/`

建议文件：

- `prototype_profile.backup.{timestamp}.json`
- `prototype_profile.migration.{timestamp}.log`

建议诊断输出至少包含：

- 原始版本号
- 迁移目标版本号
- 是否补齐缺失子结构
- 是否发现未知字段或无效物品 ID
- 最终是否成功写回

### 4.8 对现有代码的影响

`PrototypeProfileService` 的外部静态调用不需要立即大改，但内部会有这些变化：

- `version` 不再是唯一入口
- `SanitizeProfile` 只做结构合法化，不做版本迁移
- 创建默认档时同时初始化 `worldState` 和 `progression`

不在 M0 解决的事情：

- 物品实例化 DTO
- 护甲跨局耐久实例
- 成长点与技能点业务逻辑

---

## 5. M0-B 局外入口迁移与兼容层设计

### 5.1 长期场景职责矩阵

| 场景 | 长期职责 | 临时职责 | 不再承担 |
| --- | --- | --- | --- |
| `MainMenu` | 启动壳、调试入口、快速跳转 | 兼容旧原型仓库/商店页 | 正式基地 Hub |
| `BaseScene` | 正式局外入口、基地 Hub、剧情/成长/商店/制作统一落点 | 早期可只放最小交互壳 | Builder 可重建原型页 |
| `SampleScene` | 原型战斗地图 / 测试战斗地图 | 结算后返回 Meta 路由 | 直接决定正式局外入口 |

### 5.2 目标路由

长期目标路由：

`Boot -> MetaEntryRouter -> BaseScene -> SampleScene -> BaseScene`

兼容期允许：

`Boot -> MainMenu(Debug) -> SampleScene -> MetaEntryRouter -> BaseScene or MainMenu`

说明：

- 调试时可保留直接进 `MainMenu`
- 正式局外循环必须默认回到 `BaseScene`
- `PrototypeRaidProfileFlow` 不再自己决定“回哪个场景”

### 5.3 建议新增类型

- `MetaEntryRouter`
- `MetaEntryRouteConfig`
- `MetaEntryTarget`
- `MetaSessionContext`
- `BaseHubDirector`

建议职责：

| 类型 | 责任 | 不负责 |
| --- | --- | --- |
| `MetaEntryRouter` | 决定进入哪个局外场景、统一处理返回路由 | 具体 UI 绘制 |
| `MetaEntryRouteConfig` | 配置默认入口、调试入口、战局返回点 | 保存 Profile |
| `MetaSessionContext` | 保存本次会话的返回目标、调试模式、来源场景 | 长期持久化 |
| `BaseHubDirector` | 基地内功能点分发与交互注册 | 场景间迁移决策 |

### 5.4 路由状态模型

建议最小状态：

```csharp
public enum MetaEntryTarget
{
    MainMenu,
    BaseScene
}

public sealed class MetaSessionContext
{
    public MetaEntryTarget defaultMetaTarget;
    public MetaEntryTarget returnFromRaidTarget;
    public bool debugEntryEnabled;
    public string lastRaidSceneName;
}
```

约束：

- `MetaSessionContext` 是运行时会话态，不写入 Profile
- Profile 只存长期状态，路由偏好属于运行时配置

### 5.5 入口决策规则

启动时：

1. 读取 `MetaEntryRouteConfig`
2. 若启用调试快速入口，则可进 `MainMenu`
3. 否则默认进 `BaseScene`

从战局返回时：

1. `PrototypeRaidProfileFlow` 完成战局结果回写
2. 调用 `MetaEntryRouter.ReturnFromRaid()`
3. 由路由器决定加载 `BaseScene` 或 `MainMenu`

禁止：

- `PrototypeRaidProfileFlow` 直接硬编码 `SceneManager.LoadScene("MainMenu")`
- `PrototypeMainMenuController` 自己决定正式局外目标

### 5.6 对现有控制器的改造方向

#### PrototypeMainMenuController

M0 阶段只做减责，不做重写。

保留：

- 现有原型仓库 / 商店页
- 调试进入战斗流程
- Profile 保存按钮

移出：

- 正式局外入口身份
- 战局返回落点控制
- 未来基地商人、设施、剧情入口

建议改法：

- 增加对 `MetaEntryRouter` 的依赖
- `StartRaid()` 只请求路由器进入战局
- 后续 `Visit Base` / `Open Formal Hub` 按钮也经由路由器

#### PrototypeRaidProfileFlow

保留：

- 局内应用 loadout
- 战局结束回写 Profile

调整：

- `mainMenuSceneName` 改为 `fallbackMetaSceneName`
- 正常返回优先调用路由器
- 只有路由器不存在时才 fallback 到旧场景名

#### BaseHubDirector

M0 阶段只要求最小骨架：

- 基地入口初始化
- 统一注册商人点位、工作台点位、任务点位的空接口
- 不要求 M0 就把基地系统做完整

### 5.7 与 Builder 的关系

现有 `PrototypeMainMenuSceneBuilder` 仍可继续维护原型 `MainMenu`，但要加一条硬约束：

- 它只能重建原型主菜单，不得覆盖 `BaseScene`

如果后续要有基地 Builder，应独立命名和独立资产链，不复用 `PrototypeMainMenuSceneBuilder`。

---

## 6. 分阶段落地策略

### Phase 1

- 建立 schema 版本与迁移 runner
- 加入 `WorldStateData` / `PlayerProgressionData` 空结构
- 建立 `MetaEntryRouter` 和最小 `MetaSessionContext`
- 让战局返回不再写死 `MainMenu`

### Phase 2

- 在 `BaseScene` 放入最小 `BaseHubDirector`
- 允许从启动或调试入口进入基地
- 让基地成为默认返回点

### Phase 3

- 后续 M1 / M2 / M5 在此前提下接入成长、世界状态、基地功能

---

## 7. 文件级影响清单

### 必改文件

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`

### 计划新增文件

- `Assets/Res/Scripts/Profile/ProfileFileGateway.cs`
- `Assets/Res/Scripts/Profile/ProfileMigrationRunner.cs`
- `Assets/Res/Scripts/Profile/ProfileDiagnostics.cs`
- `Assets/Res/Scripts/Profile/WorldStateData.cs`
- `Assets/Res/Scripts/Profile/PlayerProgressionData.cs`
- `Assets/Res/Scripts/Profile/MetaEntryRouter.cs`
- `Assets/Res/Scripts/Profile/MetaEntryRouteConfig.cs`
- `Assets/Res/Scripts/Profile/MetaSessionContext.cs`
- `Assets/Res/Scripts/Base/BaseHubDirector.cs`

### 可能需要更新

- `Assets/Res/Scripts/UI/Editor/PrototypeMainMenuSceneBuilder.cs`
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/BaseScene.unity`

---

## 8. 验收与手动验证

### 8.1 M0-A 验证

1. 删除存档后启动，确认生成带 `profileSchemaVersion` 的新档
2. 放入旧档，确认可自动迁移并生成备份
3. 故意写入无效 JSON，确认不会覆盖已有备份并输出诊断
4. 读取后检查 `worldState` / `progression` 永远存在合法默认值

### 8.2 M0-B 验证

1. 从默认启动路径进入，确认可落到 `BaseScene`
2. 从调试入口进入 `MainMenu`，确认仍可进入战斗
3. 战局结束后返回，确认优先回到路由器指定的局外入口
4. `MainMenu` 与 `BaseScene` 不再各自维护同一套正式业务入口

---

## 9. 明确不做

- 不在 M0 阶段做完整基地商人交互
- 不在 M0 阶段做世界状态业务规则
- 不在 M0 阶段把 `PrototypeMainMenuController` 全拆成 presenter
- 不在 M0 阶段重做 UI 技术栈

---

## 10. 与后续里程碑的关系

- `M1-A` 将直接依赖本设计中的 `ProfileData` 版本化入口
- `M2-A` 将直接依赖 `WorldStateData` 壳体与写回边界
- `M5-B` 将直接依赖 `MetaEntryRouter + BaseHubDirector` 作为基地正式化基础

因此，M0 不是“可选优化”，而是中后期路线能否稳定推进的分界线。
