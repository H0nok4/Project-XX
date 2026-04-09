# JUTPS 系统拆解 01：核心启动与组合方式

## 1. 这个系统负责什么

这一层不是“玩法功能本身”，而是整包的装配底座，负责：

- 识别玩家角色与当前输入平台
- 给其他系统提供公共入口
- 约定角色 prefab、相机 prefab、UI prefab、场景对象之间的拼装关系

这层的关键特点是：

- 没有重型 IOC/Service Locator 框架
- 绝大多数能力靠 `MonoBehaviour` 之间直接拿组件引用协作
- 角色是整套玩法系统的核心宿主

## 2. 关键类

### `JUGameManager`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/JUGameManager.cs`

职责：

- 缓存 `PlayerController`
- 判断当前是否走移动端控制
- 提供全局静态入口 `Instance`

重要结论：

- 这是一个非常轻量的全局入口，不承担复杂状态管理
- 只适合作为基础环境判断，不适合作为你后续 Meta 系统的总管理器

### `JUGameSettings`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay Settings/JUGameSettings.cs`

职责：

- 用 `PlayerPrefs` 保存图像、控制、音量设置
- 直接应用分辨率、质量等级、音量
- 提供 `OnChangeSettings` 广播

重要结论：

- 这是“本地运行参数设置系统”，不是玩家进度系统
- 可保留作为选项设置层，但不要和游戏存档/账号进度混用

## 3. 包的组合模式

JUTPS 的真实工作方式是“组合式 prefab + 组件挂载”：

- 角色 prefab 挂载角色、库存、交互、受击、布娃娃、上车等组件
- 相机 prefab 独立存在，通过角色引用驱动
- UI prefab 独立存在，通过玩家、暂停、库存等系统间接驱动
- 场景中再放置 AI、载具、路径点、可拾取物、场景控制器

也就是说，核心关系是：

`Scene -> Character Prefab -> Character Subsystems -> Camera/UI/AI/Vehicle/Items`

而不是：

`Global Game Framework -> Register All Systems -> Resolve By Container`

## 4. 为什么这个结构适合原型

优点：

- 上手快
- 可视化强
- 通过 prefab 可以快速拼出 demo
- 组件边界比较清楚，替换单个系统相对容易

缺点：

- 数据不够集中
- 运行时依赖关系隐式，容易靠 Inspector 连接
- 一旦进入大型 RPG/Meta 项目，容易出现状态分散
- 不适合承载复杂跨场景业务

## 5. 运行时依赖的主轴

从代码和 demo 看，JUTPS 的主轴是：

1. 角色 prefab 是中心。
2. `JUCharacterController` 驱动大部分动作与状态。
3. 其他系统通过拿角色组件协作：
   - `JUInventory`
   - `JUInteractionSystem`
   - `DriveVehicles`
   - `JUHealth`
   - `JUCoverController`
4. 相机通过角色状态切换镜头状态。
5. AI 通过关闭默认玩家输入、直接调用角色控制接口来驱动同一套角色能力。

## 6. 对 Project-XX 的启示

适合直接继承的思路：

- 把角色战斗、交互、载具、AI 保持为“场景内能力组件”
- 把视角、移动、武器开火继续留在角色侧

不适合继续沿用的思路：

- 把整个游戏的元进程也塞到角色或场景组件里
- 让商人、任务、经济、地图掉落等系统直接依赖场景 prefab 状态

## 7. 建议你后续怎么接

建议采用“两层结构”：

### 场景内玩法层

继续复用 JUTPS：

- 角色控制
- 枪战
- AI
- 载具
- 简单交互

### 场景外 Meta 层

由 Project-XX 自己建立：

- 玩家档案
- 任务与商人
- 地图配置
- 掉落表
- 经济系统
- 属性/技能/Buff
- 装备与仓库

最终应该是：

- JUTPS 负责“怎么跑、怎么开枪、怎么打、怎么上车”
- Project-XX 负责“为什么进图、掉什么、带什么、赚什么、成长什么、解锁什么”
