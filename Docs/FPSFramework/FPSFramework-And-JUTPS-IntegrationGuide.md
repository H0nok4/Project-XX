# FPS Framework 与 JUTPS 的协同接入方案

## 1. 结论先行

对 Project-XX 最推荐的方案不是“二选一”，而是：

- `FPS Framework` 负责玩家侧第一人称控制、观察、枪械手感与武器展示
- `JUTPS` 继续负责它更成熟的 AI、载具、部分世界能力原型
- `Project-XX` 自己负责规则、数据、UI、Meta、仓库、任务、商人和最终存档

最重要的原则只有一句：

- 不要让 `FPS Framework` 和 `JUTPS` 同时争夺同一个玩家角色的控制权

## 2. 为什么推荐这种分工

结合：

- `Docs/JUTPS/JUTPS-UsageGuide.md`
- `Docs/JUTPS/Systems/01` 到 `08`
- `Docs/FPSFramework/Systems/01` 到 `04`

可以得到一个很明确的判断：

### FPS Framework 更强的地方

- 第一人称玩家控制手感
- 第一人称枪械展示层
- Procedural weapon animation
- 主相机 / 武器相机 FOV
- 玩家侧输入域拆分

### JUTPS 更强的地方

- AI 控制器、感知器、动作块
- 载具驾驶与上车桥接
- TPS/FPS 混合型角色与世界能力生态
- 更完整的敌人、场景对象、车辆原型支线

### 两者都不该当最终方案的地方

- 正式 UI
- 正式仓库 / 容器
- 正式 Meta 档
- 任务 / 商人 / 经济
- RPG 属性、技能、Buff、遗物

## 3. 功能域分工建议

| 功能域 | 推荐主方案 | 原因 |
| --- | --- | --- |
| 玩家移动与视角 | FPS Framework | 更偏纯第一人称，手感与武器展示更好 |
| 玩家枪械与手感 | FPS Framework | `FirearmPreset + ProceduralAnimator + CameraManager` 更适合做 FPS 主链路 |
| 玩家近战与战术设备 | Project-XX 桥接后择优 | 两边都不是最终成品，建议走项目自建桥层 |
| 敌人 AI 与感知 | JUTPS | JUTPS 已有 Patrol / FOV / Hear / Attack 等成熟骨架 |
| 载具与上车 | JUTPS | JUTPS 有 `DriveVehicles` 与完整车辆支线 |
| 交互发现 | 以玩家所用控制器为准 | 玩家若使用 FPSF，则优先用 FPSF 的 `InteractionsManager` |
| 背包 / 容器 / 仓库 | Project-XX 自建 | 两边都不适合最终搜打撤容器系统 |
| HUD / Pause / Settings | Project-XX 自建 | 项目已有明确 UI 规范 |
| Save / Meta / BaseHub | Project-XX 自建 | 两边都只适合原型，不适合正式产品 |

## 4. 推荐的集成姿势

### 方案 A：FPS Framework 管玩家，JUTPS 管敌人与车辆

这是最推荐的方案。

结构如下：

1. 玩家角色使用 `FPS Framework` 的 `Player.prefab` 体系。
2. 敌人与 NPC 继续使用 JUTPS 的角色、AI、感知、武器和车辆体系。
3. Project-XX 在上层统一：
   - 伤害协议
   - 任务协议
   - 交互协议
   - 掉落协议
   - UI 与结算协议

好处：

- 玩家手感改善最快
- 不需要把 JUTPS 的 AI 和载具迁移走
- 可以逐步桥接，而不是一次性重构整个项目

### 方案 B：完全迁移到 FPS Framework

不推荐。

原因：

- FPS Framework 明显缺少成熟的 AI 感知与行为层
- 几乎没有成套载具系统
- 场景世界能力生态比 JUTPS 弱很多

### 方案 C：继续用 JUTPS 玩家控制，只把 FPS Framework 某些动画或枪感拆过去

也不推荐作为首选。

原因：

- 你想解决的正是 JUTPS 玩家手感偏弱
- 如果仍保留 JUTPS 玩家控制主链路，改造成本高，收益反而分散

## 5. 绝对不要做的事

以下做法非常容易把工程拖进“双框架互相打架”的状态：

1. 不要在同一个玩家根物体上同时挂 `JUCharacterController` 和 `FirstPersonController`。
2. 不要让两套输入系统同时读取并驱动同一个玩家动作。
3. 不要同时启用两套库存系统来管理同一把玩家手持武器。
4. 不要同时启用两套交互发现器去处理同一批可交互物。
5. 不要让两套 Pause / HUD / Settings UI 一起接管同一个运行时界面。

## 6. 推荐的桥接边界

建议新增几层桥，而不是直接互调原包脚本。

### 玩家桥

- `RaidPlayerControllerFacade`
- `RaidPlayerViewBridge`
- `RaidInputContext`

职责：

- 把 FPS Framework 的玩家执行层包装成 Project-XX 可消费接口
- 向上只暴露移动、瞄准、开火、交互、死亡等统一能力

### 武器桥

- `ProjectXXFirearmBridge`
- `WeaponRuntimeToPresetMapper`
- `WeaponModifierApplier`

职责：

- 把 Project-XX 的武器定义和运行时修正，映射到 `FirearmPreset` 和 `Firearm`

### 生命与伤害桥

- `RaidHealthBridge`
- `DamageProtocolBridge`

职责：

- 统一 FPSF 玩家与 JUTPS 敌人之间的伤害事件和死亡事件
- 让 Project-XX 的护甲、Buff、异常、生存值仍有机会插入结算

### 交互桥

- `ProjectXXInteractableBridge`
- `InteractionPromptPresenter`
- `InteractionRequirementEvaluator`

职责：

- 让 FPSF 的 `InteractionsManager` 只负责“发现与触发”
- 让真正的权限、任务、钥匙、撤离条件由 Project-XX 决定

### JUTPS 世界能力桥

- `JutpsVehicleBridge`
- `JutpsEnemyFacade`
- `JutpsWorldActorBridge`

职责：

- 把 JUTPS 的车辆、AI、世界对象接入 Project-XX 的规则域

## 7. 具体功能怎么配合

### 玩家输入与相机

建议：

- 玩家只保留 FPSF 输入主链路
- JUTPS 输入资产不再驱动玩家本体
- Project-XX 统一管理 UI 焦点与暂停，再改 `FPSFrameworkCore.IsInputActive`

### 玩家武器

建议：

- 玩家枪械执行层用 FPSF
- JUTPS 武器不再作为玩家主武器体系
- 敌人仍可暂时保留 JUTPS 武器，后续由统一伤害桥收口

### 敌人与 AI

建议：

- 敌人继续用 JUTPS
- Project-XX 把任务、兴趣点、枪声、掉落价值转给 JUTPS AI 上层决策

### 载具

建议：

- 继续用 JUTPS `DriveVehicles`
- 玩家接近可驾驶物时，由 Project-XX 交互桥切换玩家控制域
- 上车时禁用 FPSF 玩家移动输入，下车时恢复

### 交互

建议：

- 玩家近身交互优先走 FPSF `InteractionsManager`
- 撤离点、容器、门、机关、商人终端都挂 Project-XX 业务桥
- 需要 JUTPS 车辆进入逻辑时，在桥内转发到 JUTPS

### HUD 和 UI

建议：

- 正式 HUD 全部由 Project-XX 重做
- FPSF 与 JUTPS 的 UI 只作为状态源和原型参考

## 8. 第一阶段最推荐的落地顺序

### R0：完成接入边界确认

- 主包已导入
- 明确玩家用 FPSF、敌人/车辆用 JUTPS
- 文档先落地

### R1：搭一张联合测试场景

- `FPSF Player`
- `JUTPS Enemy`
- `Project-XX HUD`
- 一把 FPSF 武器
- 一个 JUTPS 敌人
- 一个最小交互点

验证目标：

- 玩家第一人称手感稳定
- 能正常对 JUTPS 敌人造成伤害
- HUD 不依赖任一旧包 UI

### R2：补车辆与交互桥

- FPSF 玩家可触发 JUTPS 车辆
- 玩家上下车时输入域稳定切换
- 项目交互协议成型

### R3：再决定是否继续扩大 FPSF 占比

如果玩家侧结果满意，再继续：

- 扩更多武器
- 接近战 / 战术设备
- 接更完整的 Project-XX 规则层

## 9. 最终建议

对 Project-XX 来说，最稳的路线是：

1. 用 FPS Framework 解决“玩家手感”问题。
2. 用 JUTPS 保住“敌人、AI、车辆、世界能力”现成资产。
3. 用 Project-XX 自己的桥接层统一规则、数据、UI 和结算。

这条路的本质不是“双包叠加”，而是：

- `FPS Framework = 玩家执行层`
- `JUTPS = 世界与敌人原型层`
- `Project-XX = 正式规则与产品层`

