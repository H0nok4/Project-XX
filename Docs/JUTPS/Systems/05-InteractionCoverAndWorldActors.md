# JUTPS 系统拆解 05：交互、掩体与场景对象

## 1. 模块范围

这一层主要覆盖：

- `JUInteractionSystem`
- `JUInteractable` 及其子类
- `DriveVehicles` 的交互桥接部分
- `JUCoverController`
- 可交互门、场景机关等世界对象

它解决的问题是：

- 玩家在世界里“靠近什么、能做什么、做了之后怎么通知能力系统”

## 2. 交互系统架构

### `JUInteractionSystem`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Interaction System/JUInteractionSystem.cs`

职责：

- 在一定范围内查找最近的 `JUInteractable`
- 可选地进行遮挡检测
- 监听交互输入
- 调用 `Interactable.Interact()`
- 广播 `OnInteract`

特点：

- 它本身不处理具体交互逻辑
- 它只是“交互发现器 + 交互分发器”

这是一个比较干净的设计点，适合扩展。

### `JUInteractable`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Interaction System/JUInteractable.cs`

职责：

- 提供 `CanInteract(JUInteractionSystem)`
- 提供 `Interact()`

意义：

- 所有交互对象都统一挂在这条抽象链上
- 后续你要做门、撤离点、宝箱、商人终端、任务交付点时，都可以沿这条线扩展

## 3. 载具交互是怎么接进来的

### `DriveVehicles`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Abilities/DriveVehicles.cs`

它虽然是“能力系统”的一部分，但和交互系统强耦合：

- 自己拿 `JUInteractionSystem`
- 监听 `OnInteract`
- 如果交互对象是可进入载具，就切到上车流程

说明：

- JUTPS 的交互不是纯 UI 按钮交互
- 而是角色靠近场景物后，通过交互系统把行为桥接到具体能力组件

## 4. 掩体系统架构

### `JUCoverController`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Cover System/JUCoverController.cs`

职责：

- 检测掩体触发器
- 管理进入/退出掩体状态
- 处理沿墙移动
- 切换左右探身
- 在掩体中调整角色位置、旋转、相机偏移、持枪姿态

### `JUCoverTrigger`

职责：

- 作为掩体边界与几何信息提供者
- 给角色提供墙面左右端点、移动线段等数据

### 掩体系统的特点

- 是比较强耦合的动作玩法能力
- 直接操作角色位置和相机偏移
- 更适合 TPS
- 不太适合塔科夫式偏真实 FPS 主玩法作为核心机制

## 5. 世界交互主链路

典型链路：

1. `JUInteractionSystem` 周期性 `OverlapSphere` 查找最近交互物。
2. 交互输入触发时调用 `Interact(nearest)`.
3. `JUInteractable.Interact()` 执行具体逻辑。
4. 同时 `OnInteract` 广播给订阅组件。
5. `DriveVehicles` 这类能力系统借此开始自己的状态切换。

## 6. 对 Project-XX 的价值

这一层对你后续项目很有用，因为它天然适合承载：

- 地图内搜刮点
- 开门
- 撤离点交互
- 开启机关
- 局内商人终端
- 任务物件提交点
- 局内保险箱/电子门/钥匙系统

## 7. 对 Project-XX 的限制

当前交互层仍然偏轻量：

- 没有标准化交互条件系统
- 没有权限/钥匙/任务进度检查框架
- 没有长交互条、多人占用、服务器权威等复杂流程
- 没有统一交互反馈协议

## 8. 建议的二次封装方式

建议在 Project-XX 中保留 JUTPS 的“发现最近交互物”逻辑，但新增一层业务协议：

- `IInteractRequirement`
- `IInteractExecution`
- `InteractionPromptData`
- `InteractionResult`

推荐做法：

1. 世界对象继续继承或组合 `JUInteractable`。
2. 在 `CanInteract` 内调用 Project-XX 的任务/权限/物品检查。
3. 在 `Interact` 内执行你自己的业务命令，而不是把全部业务硬写进 JUTPS 子类。

## 9. 掩体系统对本项目的建议

如果你的目标更偏塔科夫式 FPS：

- 掩体系统不建议作为主打卖点
- 可以保留为少量 NPC 或特定职业技能的扩展能力

如果你想保持 TPS/FPS 混合玩法：

- 可把掩体作为 TPS 特化分支
- 但不要让它污染主线 FPS 搜打撤体验
