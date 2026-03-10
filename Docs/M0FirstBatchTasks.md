# Project-XX M0 第一批任务拆分

## 1. 文档目的

本文件基于 [M0FoundationTechnicalDesign.md](./M0FoundationTechnicalDesign.md) 拆分第一批可直接执行的开发任务。

拆分原则：

- 每个任务只解决一个核心问题
- 每个任务涉及 3-6 个核心文件
- 每个任务都能独立手动验证
- 不把基地正式内容、成长业务、任务业务提前塞进来

---

## 2. 第一批总目标

第一批不追求“基地玩法完成”，只追求两件事：

1. 存档底座从“静态 JSON + Sanitize”升级为“有 schema、可迁移、可备份”
2. 局外入口从“主菜单写死”升级为“有统一路由、可切换正式入口”

完成后应满足：

- 新旧档都能稳定读取
- 战局返回不再硬编码 `MainMenu`
- `BaseScene` 可以作为最小正式局外入口落点

---

## 3. 推荐执行顺序

1. `B1-01` 存档文件网关与 schema 根字段
2. `B1-02` 迁移 runner、备份与诊断
3. `B1-03` 世界状态与成长壳体接入
4. `B1-04` Meta 路由器与会话上下文
5. `B1-05` 主菜单接入路由器
6. `B1-06` 战局返回与 BaseScene 最小落点

---

## 4. 任务卡

### B1-01 存档文件网关与 schema 根字段

**目标**

- 给现有 Profile 存档建立明确的根 schema 字段和独立文件读写入口

**涉及文件**

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- 新增 `Assets/Res/Scripts/Profile/ProfileFileGateway.cs`

**主要改动**

- 在 `ProfileData` 中引入 `profileSchemaVersion`
- 保留旧 `version` 兼容语义，但不再把它当作未来主版本
- 把文件路径、原始 JSON 读写、目录创建从 `PrototypeProfileService` 抽到 `ProfileFileGateway`
- `PrototypeProfileService` 继续作为 facade，不直接散落文件 IO

**完成标准**

- [ ] 新生成存档包含 `profileSchemaVersion`
- [ ] 现有读写行为不退化
- [ ] `PrototypeProfileService` 内部不再直接处理大部分文件 IO 细节

**手动验证**

1. 删除本地档后启动游戏，确认生成新档
2. 检查 JSON 中包含 `profileSchemaVersion`
3. 购买 / 保存 / 再启动，确认原有档案读写功能仍正常

**非目标**

- 不在本任务实现迁移步骤链
- 不在本任务引入世界状态业务字段

---

### B1-02 迁移 runner、备份与诊断

**目标**

- 让旧档进入统一迁移入口，并具备备份和诊断能力

**涉及文件**

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- `Assets/Res/Scripts/Profile/ProfileFileGateway.cs`
- 新增 `Assets/Res/Scripts/Profile/ProfileMigrationRunner.cs`
- 新增 `Assets/Res/Scripts/Profile/ProfileDiagnostics.cs`

**主要改动**

- 新增 `LegacyVersion -> ProfileSchemaVersion` 的首个迁移步骤
- 读取旧档时先走迁移 runner
- 迁移成功后可自动回写新结构
- 迁移前生成备份文件
- 失败时输出诊断并保留旧档

**完成标准**

- [ ] 旧档可被识别并迁移
- [ ] 迁移前会生成备份
- [ ] 非法 JSON 不会直接覆盖已有存档
- [ ] 控制台有清晰迁移日志

**手动验证**

1. 放入旧版 profile，确认迁移成功
2. 查看备份目录，确认生成备份
3. 故意写坏 JSON，确认程序回退到默认档或安全路径，并输出诊断

**非目标**

- 不在本任务实现实例级物品迁移
- 不在本任务增加成长业务逻辑

---

### B1-03 世界状态与成长壳体接入

**目标**

- 在不做完整业务实现的前提下，把 `WorldStateData` 和 `PlayerProgressionData` 接到根存档结构

**涉及文件**

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`
- 新增 `Assets/Res/Scripts/Profile/WorldStateData.cs`
- 新增 `Assets/Res/Scripts/Profile/PlayerProgressionData.cs`

**主要改动**

- 给 `ProfileData` 增加 `worldState`、`progression`
- 默认建档时初始化两个子结构
- `SanitizeProfile` 补齐空子结构
- 不添加具体任务链或成长业务，只保证结构稳定

**完成标准**

- [ ] 新档永远带有 `worldState` 和 `progression`
- [ ] 旧档迁移后也带有默认子结构
- [ ] 不影响原有仓库、商店、战局回写

**手动验证**

1. 生成新档，检查两个子结构存在
2. 使用旧档启动，确认迁移后子结构自动补齐
3. 进入战斗并结算，确认 Profile 其他字段未被破坏

**非目标**

- 不实现 `WorldStateService`
- 不实现成长点、技能树、任务链逻辑

---

### B1-04 Meta 路由器与会话上下文

**目标**

- 把“进哪个局外入口、战斗后回哪里”从业务控制器中抽出来

**涉及文件**

- 新增 `Assets/Res/Scripts/Profile/MetaEntryRouter.cs`
- 新增 `Assets/Res/Scripts/Profile/MetaEntryRouteConfig.cs`
- 新增 `Assets/Res/Scripts/Profile/MetaSessionContext.cs`

**主要改动**

- 定义 `MetaEntryTarget`
- 定义默认局外入口与战局返回入口
- 封装 `EnterRaid`、`EnterBaseHub`、`ReturnFromRaid`
- 支持没有完整 `BaseScene` 时的 fallback

**完成标准**

- [ ] 代码中存在单一 Meta 路由入口
- [ ] 路由器能决定默认局外场景和战局返回场景
- [ ] 没有路由器时仍有安全 fallback，不会直接失效

**手动验证**

1. 使用调试配置启动，确认仍能进入 `MainMenu`
2. 切换默认目标为 `BaseScene`，确认能正确导航
3. 关闭或缺失某个目标场景时，确认 fallback 生效

**非目标**

- 不在本任务实现基地功能点
- 不在本任务迁移 MainMenu 内部 UI

---

### B1-05 主菜单接入路由器

**目标**

- 让 `PrototypeMainMenuController` 不再直接承担正式入口决策

**涉及文件**

- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
- `Assets/Res/Scripts/Profile/MetaEntryRouter.cs`
- 可能更新 `Assets/Scenes/MainMenu.unity`

**主要改动**

- `StartRaid()` 改为经由 `MetaEntryRouter`
- 预留“进入正式基地”按钮或调试按钮
- 主菜单继续保留仓库 / 商店 / 原型入口，但不再声明自己是唯一正式 Hub

**完成标准**

- [ ] 主菜单进入战斗通过统一路由器
- [ ] 主菜单不再写死正式局外身份
- [ ] 现有原型仓库 / 商店流程保持可用

**手动验证**

1. 从主菜单进入战斗，确认流程仍可用
2. 调试打开正式基地入口，确认可进入 `BaseScene`
3. 返回后确认没有出现双重入口逻辑分叉

**非目标**

- 不拆 presenter
- 不重做主菜单 UI

---

### B1-06 战局返回与 BaseScene 最小落点

**目标**

- 让战局结算后优先回到路由器指定的局外入口，并给 `BaseScene` 一个最小可落点

**涉及文件**

- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`
- `Assets/Res/Scripts/Profile/MetaEntryRouter.cs`
- 新增 `Assets/Res/Scripts/Base/BaseHubDirector.cs`
- 可能新增或更新 `Assets/Scenes/BaseScene.unity`

**主要改动**

- `PrototypeRaidProfileFlow` 的返回逻辑优先走 `MetaEntryRouter.ReturnFromRaid()`
- `mainMenuSceneName` 改为 fallback 语义
- 在 `BaseScene` 提供最小 `BaseHubDirector`
- 基地最小落点只需要展示“已进入正式局外入口”，不要求完成具体功能

**完成标准**

- [ ] 战局结算后默认可返回 `BaseScene`
- [ ] 路由器缺失时仍可 fallback 到旧主菜单
- [ ] `BaseScene` 至少可稳定承载最小入口骨架

**手动验证**

1. 进入战斗后撤离成功，确认返回目标正确
2. 进入战斗后死亡失败，确认返回目标仍正确
3. 删除或禁用路由器配置，确认 fallback 到旧场景名

**非目标**

- 不实现基地商人、设施、成长、剧情交互
- 不做基地美术布局

---

## 5. 批次交付边界

第一批完成后，应暂时停止继续扩功能，先确认以下三点：

- 存档升级链可重复执行，没有把迁移逻辑继续塞回 `SanitizeProfile`
- `MainMenu` 和 `BaseScene` 的职责已经技术上分开
- 战局返回点已从“控制器硬编码”升级为“统一路由决策”

只要这三点还没稳定，就不要进入 `M1-A` 或 `M2-A` 的正式实现。

---

## 6. 第一批之后的自然衔接

- 下一批最适合接 `M1-A`：成长存档结构与资料入口
- 并行可接 `M2-A`：`WorldStateService` 正式实现
- 基地内容相关开发应等 `B1-06` 稳定后，再进入 `M5-B` 的正式化扩展
