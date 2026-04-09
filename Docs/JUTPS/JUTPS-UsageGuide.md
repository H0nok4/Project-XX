# JU TPS 3 在 Project-XX 中的使用指南

## 1. 适合怎么用这套包

对 Project-XX 来说，最合理的定位是：

- 把 JUTPS 当作“战斗与角色控制底座”
- 不把它当作最终完整游戏框架

建议优先使用的现成能力：

- TPS/FPS 角色移动
- 相机系统
- 武器开火与基础受击
- AI 感知与基础巡逻/攻击
- 载具驾驶
- 基础交互

建议谨慎使用或只做过渡原型的部分：

- 原生库存 UI
- 原生存档
- 原生装备/护甲
- 原生设置/菜单外壳

## 2. 先从哪些 Demo 看起

建议按这个顺序理解包：

1. `ThirdPerson Shooter Demo`
2. `FPS Sample`
3. `Interaction System Demo`
4. `Save Load Demo`
5. `Vehicle AI Demo`
6. `AI` 目录下的各类小样例场景

这样可以先看主链路，再看分支能力。

## 3. 最常用的现成资产

### 角色 prefab

- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Character Prefabs/TPS Character.prefab`
- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Character Prefabs/FPS Character.prefab`

### 相机 prefab

- `Assets/Julhiecio TPS Controller/Prefabs/Game/Camera Prefabs/ThirdPerson Camera Controller.prefab`
- `Assets/Julhiecio TPS Controller/Prefabs/Game/Camera Prefabs/FirstPerson Camera Controller.prefab`

### 载具 prefab

- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Vehicles/Car.prefab`
- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Vehicles/Motorcycle.prefab`
- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Vehicles/Bike.prefab`

### 武器与物品 prefab

- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Items/Weapons/Guns/*`
- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Items/Melee Weapons/*`
- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Items/Throwable/*`
- `Assets/Julhiecio TPS Controller/Demos/Demo Prefabs/Items/Armor/*`

### 输入资产

- `Assets/Julhiecio TPS Controller/Input Controls/Player Character Inputs.asset`
- `Assets/Julhiecio TPS Controller/Input Controls/Classic Vehicle Input.asset`
- `Assets/Julhiecio TPS Controller/Input Controls/Advanced Vehicle Input.asset`

## 4. 搭一个最小可运行玩法原型的步骤

### 方案 A：快速起一个 FPS 样机

1. 打开 `FPS Sample` 看官方装配。
2. 新场景里放入：
   - `FPS Character.prefab`
   - `FirstPerson Camera Controller.prefab`
   - `JUTPS User Interface` 或你自己的 HUD
3. 给角色确认挂好：
   - `JUCharacterController`
   - `JUInventory`
   - `JUHealth`
   - `JUInteractionSystem`
4. 给 `JUCharacterController` 指向 `Player Character Inputs.asset`
5. 把一个武器 prefab 放到角色对应手骨下，或沿用官方角色 prefab 里的默认武器
6. 准备地面、NavMesh、敌人和可拾取物

### 方案 B：快速起一个 TPS 样机

1. 打开 `ThirdPerson Shooter Demo` 看官方装配。
2. 新场景里放入：
   - `TPS Character.prefab`
   - `ThirdPerson Camera Controller.prefab`
   - UI
3. 保留默认交互、背包、载具组件
4. 按需加入 AI、车辆、掩体触发器

## 5. 如果你要开始做正式项目，建议按这个方式接入

### 第一步：只保留“战斗底座”

保留：

- 角色控制
- 枪械与近战基础
- 交互系统
- AI 感知与攻击
- 载具

### 第二步：把业务层自己重建

自己做：

- 局外 Profile
- 任务系统
- 商人系统
- 地图配置
- 掉落表
- 仓库与装备系统
- 技能与 Buff 系统
- 结算与撤离流程

### 第三步：UI 不沿用 JUTPS 旧方案

Project-XX 新 UI 必须遵守：

- `Assets/Resources/UI/...`
- `ViewBase / WindowBase`
- `*Template`
- 挂载到 `PrototypeRuntimeUiManager`

不要直接把 JUTPS 的原生库存/设置/暂停页面继续扩展成最终方案。

## 6. 哪些地方最适合改，哪些地方尽量别硬改

### 适合扩展

- 新 AI Action / Sensor
- 新 Interactable 类型
- 新 Weapon 参数和 prefab
- 新 Vehicle 能力组件
- 新角色状态修正器

### 不建议在原脚本里越改越深

- `JUInventory` 直接改成塔科夫仓库
- `JUSaveLoad` 直接硬塞 Meta 档
- `JU_UI*` 直接扩成正式商店/任务/仓库 UI
- `JUCharacterControllerCore` 里持续塞 RPG 业务

## 7. 面向你这个项目的推荐落地姿势

我建议你后续采用“桥接式集成”：

1. JUTPS 继续负责局内动作与战斗。
2. Project-XX 数据层驱动真正的角色成长、掉落、任务、经济。
3. 场景中出现的枪、护甲、容器、商人交互点，都只是数据实例的表现载体。

这样做的好处：

- 能快速做出可玩的战斗版本
- 又不会被原包的简化库存和存档拖死

## 8. 一句话结论

这套包非常适合做你的“局内战斗骨架”，但不适合直接当“搜打撤 + RPG + 商人经济”的完整母体。最优方案是保留它的动作战斗层，自己建设项目真正的 Meta 与数据系统。
