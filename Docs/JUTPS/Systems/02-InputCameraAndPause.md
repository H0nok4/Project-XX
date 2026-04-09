# JUTPS 系统拆解 02：输入、相机与暂停

## 1. 模块范围

本模块包括：

- 玩家输入资产
- 载具输入资产
- 输入设备切换识别
- TPS/FPS/TopDown 相机控制
- 暂停与设置界面

这是整套包里“把玩家意图变成控制信号”的中枢层。

## 2. 输入系统架构

### 玩家输入资产：`JUPlayerCharacterInputAsset`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Inputs/JUPlayerCharacterInputAsset.cs`

特征：

- 基于 Unity Input System
- 用 `ScriptableObject` 承载一整套 `InputAction`
- 一个资产内同时包含：
  - 移动输入
  - 视角输入
  - 射击/近战/瞄准/换弹
  - 蹲伏/匍匐/翻滚
  - 切枪/背包/交互等

优势：

- 输入绑定集中
- 角色 prefab 只需要引用一个输入资产
- 同一角色逻辑可以较容易切换不同输入方案

局限：

- 输入资产承载范围很大，动作过多时会越来越臃肿
- 后续若要支持复杂键位重绑定，仍需要你自己再包一层配置保存与 UI

### 载具输入资产：`JUVehicleInputAsset`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Physics/Vehicle Physics/JUVehicleInputAsset.cs`

特征：

- 独立于角色输入
- 用单独 `ScriptableObject` 承载油门、转向、刹车、氮气
- 内置 Classic / Advanced 两套创建菜单和 demo 资产

结论：

- 载具输入与角色输入是分开的
- 后续如果你做“角色进车后切控制域”，这个架构是合理的

### 输入设备识别：`JUInputManager`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/JU Input/JUInputManager.cs`

职责：

- 检测 Keyboard/Mouse、Xbox、PlayStation、Nintendo、Touch
- 广播 `OnChangeInputType`
- 自动创建隐藏实例并跨场景驻留

项目价值：

- 非常适合做输入提示图标切换
- 也适合移动端/手柄 UI 适配

## 3. 相机系统架构

### 基类：`JUCameraController`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Libraries/JUCameraSystemLib.cs`

职责：

- 定义 `CameraState`
- 处理旋转输入、镜头位移、碰撞、FOV、光标锁定
- 提供不同镜头状态之间的平滑过渡

`CameraState` 结构里主要包含：

- 距离
- FOV
- Pivot 偏移
- Camera 偏移
- 旋转灵敏度
- 俯仰限制
- 碰撞层

这意味着包的相机不是写死的一组参数，而是“状态切换式相机”。

### TPS 相机：`TPSCameraController`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Camera Controllers/TPSCameraController.cs`

职责：

- 读取 `InputAsset.LookAxis`
- 根据角色状态切换：
  - Normal
  - FireMode
  - AimMode
  - Driving
  - Dead
- 在瞄准时把镜头推进到枪械的 `CameraAimingPosition`

架构意义：

- 相机和角色是松耦合的
- 角色通过状态影响相机
- 相机并不直接主导角色，只是观察并响应角色状态

### FPS / TopDown / SideScroller

从 prefab 和 demo 来看，JUTPS 不是为每种玩法重写一整套角色系统，而是：

- 共用角色/武器/输入底座
- 用不同相机 prefab 和少量角色装配差异实现不同视角玩法

## 4. 暂停与设置

### `JUPauseGame`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Utilities/JUPauseGame.cs`

职责：

- 单例化暂停控制
- 通过 `Time.timeScale` 冻结时间
- 提供 `OnPause` / `OnContinue`
- 允许外部控制是否可暂停

### `JU_UIPause`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/UI/JU_UIPause.cs`

职责：

- 驱动暂停界面、返回菜单、打开设置
- 处理鼠标锁定/显示

### `JU_UISettings`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/UI/JU_UISettings.cs`

职责：

- 把设置界面控件绑定到 `JUGameSettings`
- 包含 Controls / Graphics / Audio 三块

## 5. 运行流程

玩家输入与相机的典型流程：

1. `JUPlayerCharacterInputAsset` 持有动作映射。
2. `JUCharacterController` 在 `Update` 里读取输入资产状态。
3. `TPSCameraController` 在 `Update/FixedUpdate/LateUpdate` 中读取 LookAxis 并跟随角色。
4. 角色的 `FiringMode / IsAiming / IsDriving / IsDead` 改变相机状态。
5. `JUPauseGame` 统一控制暂停，UI 通过事件响应。

## 6. 对 Project-XX 的价值

这套输入和相机层很值得直接复用，原因是：

- 它已经把 TPS/FPS 的基本观察与控制闭环跑通
- 角色、载具、暂停的输入域切换已经有基础
- 相机状态切换对枪战原型足够成熟

## 7. 对 Project-XX 的限制

你后续一定会遇到这些扩展点：

- 自定义键位重绑定
- 不同地图/状态下的输入锁定策略
- 菜单、仓库、商店、对话、任务日志的 UI 输入焦点管理
- 更多瞄准模式、肩部切换、战术设备快捷键

因此建议：

- 保留 JUTPS 的输入资产与相机系统作为战斗输入底座
- 在其外层增加 Project-XX 自己的“输入上下文管理”
- 新 runtime UI 不继续扩展 JUTPS 自带暂停/背包 UI 模式，而是接入项目自己的 UGUI prefab 工作流
