# FPS Framework 系统拆解 04：生命、重生、UI 与场景外壳

## 1. 模块范围

本篇把几个相邻模块放在一起看：

- `Damageable`
- `Actor`
- `SpawnManager`
- `DeathCamera`
- `UIManager`
- `PauseMenu / LoadingScreen / MainMenu`
- `SaveSystem / SettingsManager`

原因是它们共同构成了这套包的“轻量比赛型 / 单关卡型壳”。

## 2. 生命与受击系统

### `Damageable`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/Damageable.cs`

职责：

- 管理血量
- 管理自动回血
- 管理死亡、摧毁、布娃娃、受击反馈
- 记录 `DamageSource`
- 与 `Actor`、`Ragdoll`、`DeathCamera` 联动

说明：

- 这是一个标准 FPS 样机型生命组件
- 适合承载局内战斗
- 不适合直接承担复杂生存值、异常状态和 RPG 伤害结算

### `Actor`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/Actor.cs`

职责：

- 管理队伍、击杀、死亡、重生
- 接 UI 的 `PlayerCard / KillFeed / Hitmarker`
- 在死亡确认时统计击杀关系

说明：

- 它带有一些轻量多人对战式统计思路
- 但本质还是场景内 actor 壳

## 3. 重生与死亡镜头

### `SpawnManager`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/SpawnManager.cs`

职责：

- 根据 team 或 spawn side 管理出生点
- 负责 `Actor` 的重生位置与延时

### `DeathCamera`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/DeathCamera.cs`

职责：

- 在玩家死亡后接管镜头与音频

这说明它的场景壳更像：

- 小规模 FPS 对局或样机
- 而不是搜打撤式“死亡结算 -> 物品处理 -> 返回局外”的完整流程

## 4. UI 与菜单壳

### `UIManager`

路径：

- `Assets/Akila/FPS Framework/Scripts/UI/UIManager.cs`

职责：

- 组织 `PlayerCard / Hitmarker / KillFeed / DamageableEffectsVisualizer`

### 相关 UI 脚本

位于：

- `Assets/Akila/FPS Framework/Scripts/UI`

包括：

- `Crosshair`
- `FirearmHUD`
- `PlayerCard`
- `Hitmarker`
- `KillFeed`
- `PauseMenu`
- `MainMenu`
- `LoadingScreen`

说明：

- 包内 UI 已经足够完成一层完整 FPS 样机演示
- 但仍是传统 UGUI 原型写法

## 5. 存档与设置

### `SaveSystem`

路径：

- `Assets/Akila/FPS Framework/Scripts/Utilities/Scripting Utilities/SaveSystem.cs`

职责：

- 用 JSON 存对象
- 用 JSON 存基础偏好键值
- 写入 `Application.persistentDataPath/Akila Documents/FPSFramework`

说明：

- 它是一个轻量 JSON save helper
- 适合保存设置、样机级偏好项
- 不适合作为 Project-XX 正式角色档和元进程存档

### `SettingsManager`

路径：

- `Assets/Akila/FPS Framework/Scripts/Settings Managment System/SettingsManager.cs`

说明：

- 是本地设置系统
- 不是完整产品配置 / 存档 / 账号系统

## 6. 对 Project-XX 的主要判断

### 生命层

短期可以用来：

- 承接玩家与敌人的基础受击
- 驱动击杀提示和死亡镜头

但不建议直接作为最终方案承载：

- 饱食度 / 饮水度 / 疲劳值
- 异常状态与 DOT
- 复杂护甲穿透和部位减伤
- Boss 机制与精英词缀伤害协议

### UI 层

短期可以用来：

- 作为 FPS HUD 参考
- 临时显示血量、命中、击杀、交互提示

但 Project-XX 的正式 UI 仍然必须走：

- `Assets/Resources/UI/...`
- `ViewBase / WindowBase`
- `*Template`
- `PrototypeRuntimeUiManager`

### 场景壳与存档层

短期可以参考：

- `LoadingScreen`
- `PauseMenu`
- `SaveSystem`

但不应直接继续扩成：

- BaseHub 流程
- 进图 / 撤离结算
- 仓库和商人场景
- Profile 持久化

## 7. 总结

这个模块对你最有用的不是“直接拿来做最终游戏”，而是：

- 作为玩家局内生命与反馈层参考
- 作为 FPS HUD 参考
- 作为轻量 Pause / Loading / Settings 的参考

最终正式项目里，建议：

- 局内生命和反馈可以桥接使用
- 正式 UI 全部重做
- 正式 Save、BaseHub、任务与 Meta 流程全部由 Project-XX 自己承接

