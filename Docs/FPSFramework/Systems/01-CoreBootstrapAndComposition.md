# FPS Framework 系统拆解 01：核心启动与组合方式

## 1. 这个系统负责什么

这一层不是“复杂玩法本身”，而是整包的装配底座，负责：

- 约定玩家 prefab、HUD、菜单、场景壳之间的关系
- 提供框架级静态状态入口
- 提供轻量单例管理器
- 提供 Render Pipeline 和编辑器创建向导

这层的关键特点是：

- 没有重型 IOC / Service Locator
- 依然是典型的 `MonoBehaviour + Prefab` 组合模式
- 玩家 prefab 仍然是整套玩法系统的核心宿主

## 2. 关键类

### `FPSFrameworkCore`

路径：

- `Assets/Akila/FPS Framework/Scripts/Internal/FPSFrameworkCore.cs`

职责：

- 提供框架版本号与静态状态
- 持有 `IsActive / IsInputActive / IsPaused` 这类运行时开关
- 提供部分工具方法和编辑器辅助

重要结论：

- 它是一个轻量静态核心，不是完整游戏生命周期框架
- 很适合承载“玩家控制是否启用”这种包内开关
- 不适合作为 Project-XX 元进程总入口

### `GameManager`

路径：

- `Assets/Akila/FPS Framework/Scripts/Utilities/GameManager.cs`

职责：

- 作为非常薄的场景级单例
- 在场景里实例化 `DeathCamera` 和 `UIManager`

重要结论：

- 这是一个极轻量的壳
- 只适合做包内玩家局内辅助对象的生成
- 不适合接管场景流转、Meta 档、任务和经济

### `UIManager`

路径：

- `Assets/Akila/FPS Framework/Scripts/UI/UIManager.cs`

职责：

- 缓存 HUD 相关单例引用
- 暴露 `PlayerCard / Hitmarker / KillFeed / DamageableEffectsVisualizer`
- 轻量提供 `LoadGame` 和 `Quit` 入口

### `SettingsManager`

路径：

- `Assets/Akila/FPS Framework/Scripts/Settings Managment System/SettingsManager.cs`

职责：

- 管理多份 `SettingsPreset`
- 在 `Awake / Start / Update / OnApplicationQuit` 依次调用设定项
- 配合 `SaveSystem` 保存本地设置

说明：

- 这是本地设置层
- 不是正式游戏档案系统

### `RPConvertor` 与 `FPSFrameworkSettingsWindow`

路径：

- `Assets/Akila/FPS Framework/Scripts/Internal/RPConvertor.cs`
- `Assets/Akila/FPS Framework/Scripts/Internal/FPSFrameworkSettingsWindow.cs`

职责：

- 检查当前工程使用的 Render Pipeline
- 安装缺失 RP 包
- 导入 `FPSF_BIRP / FPSF_URP / FPSF_HDRP` 转换包
- 覆盖相关 prefab、材质和场景设置

这是一个重要结论：

- 这套包把 RP 适配看成“二次转换流程”
- 不应在正式项目里无备份直接执行

## 3. 包的组合模式

FPS Framework 的真实工作方式是“玩家 prefab + 场景辅助单例 + HUD/菜单 prefab”：

- 角色 prefab 挂载第一人称控制、输入、相机、库存、交互、生命等组件
- `Game Manager` 场景中生成 `DeathCamera` 与 `UIManager`
- `HUD / Settings / Pause / Main Menu / Loading` 作为独立 UI prefab 和场景壳存在
- 武器、拾取物、交互物都以场景对象形式出现

也就是说，核心关系更接近：

`Scene -> GameManager -> Player Prefab -> Character Subsystems -> HUD / Weapon / World`

而不是：

`Global Gameplay Framework -> Register Systems -> Resolve Everywhere`

## 4. 为什么这个结构适合原型

优点：

- 装配直观
- 第一人称主链路清晰
- 容易快速换武器、调手感、改 HUD
- 很适合单地图 FPS 样机

缺点：

- 全局状态较轻，难承载复杂跨场景业务
- 单例与静态开关偏多
- UI、菜单、场景流程都偏原型风格
- 不适合直接长成搜打撤 + RPG + BaseHub 完整母体

## 5. 对 Project-XX 的启示

适合直接继承的思路：

- 把 FPS Framework 仅当作局内玩家“执行层”
- 保留它的角色 prefab 组合思路
- 借鉴它对第一人称手感、菜单和 loading 的最小壳组织

不适合继续沿用的思路：

- 让 `GameManager` 管整个游戏流程
- 让 `FPSFrameworkCore` 成为项目总状态中心
- 让 `SettingsManager / SaveSystem` 混入玩家成长与 Meta 档案

## 6. 建议你后续怎么接

建议采用“两层结构”：

### 玩家执行层

继续复用 FPS Framework：

- 玩家第一人称运动
- 玩家局内输入
- 武器相机与 procedural animation
- 轻量局内交互

### 项目规则层

由 Project-XX 自己建立：

- `RaidSessionRuntime`
- `RaidPlayerRuntime`
- `PlayerProfileRuntime`
- `LoadoutRuntime`
- `ModifierResolver`
- `Meta Save / BaseHub / Traders / Tasks`

最终应该是：

- FPS Framework 负责“怎么走、怎么看、怎么开枪、枪怎么晃”
- Project-XX 负责“为什么进图、带什么、掉什么、成长什么、撤离结算什么”

