# FPS Framework 在 Project-XX 中的使用指南

## 1. 适合怎么用这套包

对 Project-XX 来说，最合理的定位是：

- 把 FPS Framework 当作“第一人称人物控制与枪械手感底座”
- 不把它当作最终完整搜打撤游戏框架

建议优先使用的现成能力：

- 第一人称角色移动
- 第一人称相机和双 FOV 武器展示
- 枪械后坐力、瞄准、喷散、换弹
- 程序化武器动画
- 轻量世界交互
- 基础受伤、击杀反馈与重生壳

建议谨慎使用或只做过渡原型的部分：

- 原生 Inventory
- 原生 HUD / Settings / Pause UI
- 原生 SaveSystem
- 原生 Main Menu / Loading 外壳

## 2. 先从哪些内容看起

建议按这个顺序理解包：

1. `Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab`
2. `Assets/Akila/FPS Framework/Scripts/Character/FirstPersonController.cs`
3. `Assets/Akila/FPS Framework/Scripts/Character/CharacterInput.cs`
4. `Assets/Akila/FPS Framework/Scripts/Character/Firearm System/Firearm.cs`
5. `Assets/Akila/FPS Framework/Scripts/Animation System/ProceduralAnimator.cs`
6. `Assets/Akila/FPS Framework/Scenes/Demo.unity`

这样可以先把“角色手感主链路”看通，再补场景和 UI。

## 3. 最常用的现成资产

### 角色 prefab

- `Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Characters/FPS Hands Variant.prefab`

### 世界与管理 prefab

- `Assets/Akila/FPS Framework/Prefabs/World/Game Manager.prefab`
- `Assets/Akila/FPS Framework/Prefabs/World/Enviroment.prefab`

### HUD prefab

- `Assets/Akila/FPS Framework/Prefabs/HUD/HUD.prefab`
- `Assets/Akila/FPS Framework/Prefabs/HUD/Firearm HUD.prefab`
- `Assets/Akila/FPS Framework/Prefabs/HUD/PickupHUD.prefab`
- `Assets/Akila/FPS Framework/Prefabs/HUD/Settings Menu.prefab`

### 武器 prefab

- `Assets/Akila/FPS Framework/Prefabs/Weapons/Pistol_1.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Weapons/Assault Rifle_1.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Weapons/Shotgun_1.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Weapons/Sniper_1.prefab`
- `Assets/Akila/FPS Framework/Prefabs/Weapons/Grenade_1.prefab`

### 武器数据

- `Assets/Akila/FPS Framework/Data/Weapons/*.asset`
- `Assets/Akila/FPS Framework/Data/Weapons/Spray Patterns/*.asset`

## 4. 搭一个最小可运行 FPS 样机的步骤

### 方案 A：用官方 Demo 看完整装配

1. 打开 `Assets/Akila/FPS Framework/Scenes/Demo.unity`
2. 先确认玩家、HUD、世界物件和武器装配关系
3. 再看 `Main Menu.unity` 与 `Loading.unity`，理解它自带的轻量流程壳

### 方案 B：在你自己的测试场景里最小接入

1. 新建一张你自己的 sandbox 场景，不直接长期改官方 Demo。
2. 放入：
   - `Player.prefab`
   - `Game Manager.prefab`
   - 基础地面和可交互物
3. 给角色确认挂好：
   - `FirstPersonController`
   - `CharacterInput`
   - `CharacterManager`
   - `CameraManager`
   - `Inventory`
   - `InteractionsManager`
   - `Damageable`
   - `Actor`
4. 给 `Inventory.startItems` 指向一把武器 prefab，先跑通手持武器和开火
5. 再把 HUD 逐步替换为 Project-XX 自己的运行时 UI

## 5. 如果你要开始做正式项目，建议按这个方式接入

### 第一步：只保留玩家侧第一人称“执行层”

保留：

- 第一人称角色移动
- 相机旋转与 FOV 过渡
- 武器 procedural animation
- 瞄准、射击、换弹、喷散、后坐力
- 局内轻量交互发现

### 第二步：把规则层和数据层自己重建

自己做：

- 角色成长与属性
- Build、Buff、遗物、技能
- 背包、容器、仓库
- 任务、商人、经济
- 正式 UI
- 正式存档

### 第三步：不要把 FPS Framework 的场景壳和旧 UI 继续扩成最终方案

Project-XX 正式 runtime UI 必须遵守：

- `Assets/Resources/UI/...`
- `ViewBase / WindowBase`
- `*Template`
- 挂载到 `PrototypeRuntimeUiManager`

不要直接把 FPS Framework 自带的：

- `HUD`
- `PauseMenu`
- `Settings Menu`
- `Main Menu`
- `LoadingScreen`

扩成最终产品方案。

## 6. 哪些地方最适合改，哪些地方尽量别硬改

### 适合扩展

- `FirearmPreset`
- `SprayPattern`
- `ProceduralAnimation` 连接关系
- `InteractionsManager` 外层的业务桥
- `Damageable` 与 Project-XX 生命规则之间的桥

### 不建议在原脚本里越改越深

- `Inventory` 直接改成塔科夫式仓库
- `SaveSystem` 直接扩成完整 Profile 存档
- `UIManager` 直接堆项目级 HUD、商店、仓库、任务 UI
- `GameManager` 直接接管 Project-XX 全局流程

## 7. 渲染管线接入提醒

这套包有自己的 RP Setup 流程，会：

- 检查目标 Render Pipeline
- 导入额外 RP 子包
- 覆盖部分 prefab、场景、材质和设置

所以建议你：

1. 先做资源审计和局部验证。
2. 真要正式用它时，再在备份后评估是否执行 `URP Setup`。
3. 不要把它的 RP 设置向导当成无风险按钮直接点。

## 8. 一句话结论

这套包非常适合做你的“第一人称玩家手感骨架”，尤其适合替代 JUTPS 在玩家侧的控制与武器反馈；但它不适合直接承担 Project-XX 的完整搜打撤 RPG 规则与元进程框架。

