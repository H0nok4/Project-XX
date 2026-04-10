# FPS Framework 2.1.3 导入与审计记录

## 1. 本次处理结果

- 已导入包文件：`FPS Framework 20 v2.1.3 (06 Nov 2025).unitypackage`
- Unity 编辑器版本：`6000.3.10f1`
- 导入后新增主目录：`Assets/Akila/FPS Framework/`
- 导入后 `Packages/manifest.json` 新增依赖：
  - `com.unity.render-pipelines.core@17.3.0`
  - `com.unity.shadergraph@17.3.0`
- 导入后检查结果：
  - Unity Console 未发现导入阶段报错
  - 导入完成后出现的是现有工程和旧包代码的兼容性 warning，没有新增致命编译错误
  - `Assets/Akila/FPS Framework` 已完整落盘，可见 `Scenes / Prefabs / Scripts / Data / UI / Art / Editor`
  - 导入同时改动了多份 `ProjectSettings/*.asset`，属于需要额外审计的全局配置污染

## 2. 额外注意事项

该包不是只靠一个主包就结束的安装结构。包内还自带了三份渲染管线转换包：

- `Assets/Akila/FPS Framework/Editor/Wizard Data/Packages/FPSF_BIRP.unitypackage`
- `Assets/Akila/FPS Framework/Editor/Wizard Data/Packages/FPSF_URP.unitypackage`
- `Assets/Akila/FPS Framework/Editor/Wizard Data/Packages/FPSF_HDRP.unitypackage`

同时还带有编辑器侧的 `RPConvertor` 与设置窗口，用于：

- 检测当前项目使用的 Render Pipeline
- 安装缺失的 RP 依赖
- 导入对应 RP 的替换资产
- 覆盖一部分框架相关 prefab、材质、场景和配置

出于避免误覆盖项目级渲染设置的考虑，本次只完成了主包导入，没有执行它自带的 `Setup` 向导。

这意味着当前状态是：

- `FPS Framework` 已进入项目，可做代码与资源审计
- 若后续你决定正式把它作为主 FPS 底座，还应在备份后审慎评估是否执行它的 `URP Setup`

## 3. 导入带来的项目设置污染

这次导入除了新增 `Assets/Akila/` 目录与 `Packages` 依赖外，还直接改动了多份全局项目设置文件，包括：

- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/QualitySettings.asset`
- `ProjectSettings/InputManager.asset`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/TagManager.asset`
- 以及部分 `Audio / Dynamics / Time / VFX / EditorSettings` 配置

从 diff 看，变化不只是“补充必要设置”，而是存在明显的“样板工程覆盖”特征，例如：

- Build Settings 被切到 FPS Framework 自带场景
- 还出现了 `Assets/Akila/FPS Framework Pro/Scenes/Multiplayer/Multiplayer Demo.unity` 这样的场景引用
- `GraphicsSettings` 的自定义 SRP 引用被清空
- `QualitySettings` 被重写成包自己的默认档位结构
- `InputManager` 的旧输入轴表被大幅替换
- `ProjectSettings.asset` 里的产品标识、分辨率、色彩空间、包名、输入处理方式等被覆盖
- `TagManager` 的标签和层配置被改成包自己的命名

结论：

- 这套包的导入会污染全局项目设置
- 这些改动不能视为“安全且必要的最小改动”
- 如果你准备正式接入它，建议把“保留哪些全局设置、回滚哪些样板设置”单独做一次清理

## 4. 包的总体结构

这套包不是“全游戏框架”，而是一套围绕第一人称玩家体验组织的场景型 FPS 原型框架。

资源主目录：

- `Assets/Akila/FPS Framework/Scenes`
- `Assets/Akila/FPS Framework/Prefabs`
- `Assets/Akila/FPS Framework/Data`
- `Assets/Akila/FPS Framework/UI`
- `Assets/Akila/FPS Framework/Art`
- `Assets/Akila/FPS Framework/Scripts`

粗略统计：

- 脚本：约 `190` 个 `.cs`
- 预制体：约 `84` 个 `.prefab`
- 数据资产：约 `88` 个 `.asset`
- 场景：`Demo.unity`、`Main Menu.unity`、`Loading.unity`

脚本目录粗略统计：

| 模块目录 | 脚本数 |
| --- | ---: |
| Character | 79 |
| UI | 26 |
| Utilities | 20 |
| Animation System | 15 |
| Editor | 15 |
| Internal | 11 |
| Audio System | 10 |
| Settings Managment System | 7 |
| Extras | 6 |
| Input | 1 |

## 5. 代表性场景与资产

### 场景

- `Assets/Akila/FPS Framework/Scenes/Main Menu.unity`
- `Assets/Akila/FPS Framework/Scenes/Loading.unity`
- `Assets/Akila/FPS Framework/Scenes/Demo.unity`

说明：

- `Main Menu` 与 `Loading` 证明它自带了一层轻量关卡外壳
- `Demo` 是最值得参考的装配场景
- 官方 `Scenes/Readme.txt` 也明确建议你不要把它的 Demo 场景当长期生产场景直接开发

### 角色 prefab

- `Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Characters/Bot.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Characters/FPS Hands Variant.prefab`

### 关键玩法 prefab

- `Assets/Akila/FPS Framework/Prefabs/World/Game Manager.prefab`
- `Assets/Akila/FPS Framework/Prefabs/World/Enviroment.prefab`
- `Assets/Akila/FPS Framework/Prefabs/HUD/HUD.prefab`
- `Assets/Akila/FPS Framework/Prefabs/HUD/Settings Menu.prefab`

### 武器 prefab

- `Assets/Akila/FPS Framework/Prefabs/Weapons/Pistol_1.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Weapons/Assault Rifle_1.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Weapons/Shotgun_1.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Weapons/Sniper_1.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Weapons/Grenade_1.prefab`

### 武器数据资产

- `Assets/Akila/FPS Framework/Data/Weapons/Pistol_1.asset`
- `Assets/Akila/FPS Framework/Data/Weapons/Assault Rifle_1.asset`
- `Assets/Akila/FPS Framework/Data/Weapons/Shotgun_1.asset`
- `Assets/Akila/FPS Framework/Data/Weapons/Sniper_1.asset`
- `Assets/Akila/FPS Framework/Data/Weapons/Spray Patterns/*`

## 6. 核心架构判断

这套包的关键设计不是大型数据驱动框架，而是：

- 以 `Player.prefab` 为核心宿主
- 用 `FirstPersonController + CharacterInput + CameraManager + CharacterManager`
  承担第一人称运动、观察与角色状态
- 用 `Inventory + InventoryItem + ItemInput + Firearm`
  承担手持物切换、武器输入与武器行为
- 用 `ProceduralAnimator + ProceduralAnimation + Modifiers`
  承担枪械摇摆、呼吸、后坐力、探身、壁障回避等“手感层”
- 用 `Damageable + Actor + SpawnManager + DeathCamera`
  承担生命、击杀反馈、重生与死亡镜头
- 用 `UIManager + Hitmarker + KillFeed + PlayerCard + PauseMenu`
  承担轻量 HUD 与菜单壳

这意味着它非常适合作为：

- 第一人称移动和视角底座
- 手持枪械的手感层
- 程序化 FPS 动画样机层
- 轻量交互与局内 HUD 参考实现

但它并不适合作为：

- 完整搜打撤元进程框架
- 复杂 AI、载具、任务、商人、仓库母体
- 正式项目的最终 UI、存档与规则中心

## 7. 和 JUTPS 相比最突出的特点

相较 JUTPS，这套包最有价值的差异点是：

- `FirstPersonController` 明显更偏第一人称手感，而不是 TPS/FPS 兼容底座
- `ProceduralAnimator` 体系更强调武器展示与镜头反馈
- `FirearmPreset` 把枪械参数集中成 `ScriptableObject`
- `CameraManager` 有主相机与武器相机双 FOV 思路
- `Controls.inputactions` 明确拆成 `Player / Firearm / Throwable / UI` 四个 action map

而它明显弱于 JUTPS 的地方是：

- 没有成体系的 AI 行为与感知系统
- 没有成熟的载具驾驶与上车桥接系统
- 没有像 JUTPS 那样完整的“敌人、车辆、世界玩法支线”

## 8. 面向 Project-XX 的总判断

如果目标是“单机 PVE 第一人称搜打撤 + RPG 成长”，建议把 FPS Framework 定位为：

- `玩家侧第一人称运动与手感底座`
- `玩家武器展示与开火反馈层`
- `第一人称输入 / 镜头 / Procedural Animation 参考实现`

不要把它定位为：

- 最终游戏的全量玩法框架
- 可直接承载 AI / 载具 / 商人 / 仓库 / Meta 的完整方案

对 Project-XX 最合理的用法是：

1. 用它替换玩家自身的第一人称控制与武器手感层。
2. 继续保留 Project-XX 自己的数据、UI、Meta、背包与规则系统。
3. 与 JUTPS 做桥接式分工，而不是让两个包同时争夺同一套玩家控制权。
