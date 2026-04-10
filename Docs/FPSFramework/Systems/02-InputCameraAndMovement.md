# FPS Framework 系统拆解 02：输入、相机与第一人称移动

## 1. 模块范围

本模块包括：

- `Controls.inputactions`
- `CharacterInput`
- `FirstPersonController`
- `CameraManager`
- `ProceduralAnimator`
- 动画修正器 `Modifiers`

这是整套包里最有价值的部分，也是它最适合替换 JUTPS 玩家手感层的原因。

## 2. 输入系统架构

### 输入资产：`Controls.inputactions`

路径：

- `Assets/Akila/FPS Framework/Scripts/Input/Controls.inputactions`

Action Map 拆分为：

- `Player`
- `Firearm`
- `Throwable`
- `UI`

`Player` 域包含：

- `Move`
- `Look`
- `Jump`
- `Sprint`
- `Tactical Sprint`
- `Crouch`
- `Interact`
- `Pickup`
- `Switch Item`
- `Item 1 ~ Item 9`
- `Next Item / Previous Item / DefaultItem`
- `Lean Right / Lean Left`

`Firearm` 域包含：

- `Fire`
- `Aim`
- `Reload`
- `Drop`
- `Fire Mode Swich`
- `Sight Mode Switch`

结论：

- 这套输入拆法比 JUTPS 的“一个大角色输入资产”更清晰
- 很适合做“玩家移动输入”和“武器输入”分层

### `CharacterInput`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/CharacterInput.cs`

职责：

- 读取 `Player` action map
- 管理 `toggleAim / toggleCrouch / toggleLean`
- 处理 `Sprint / Tactical Sprint`
- 统一输出 `MoveInput / LookInput / JumpInput / CrouchInput`
- 基于 `FPSFrameworkCore.IsActive / IsInputActive / IsPaused` 管控输入

它的结构含义是：

- 输入上下文已经有基本分域
- 但项目级复杂输入焦点管理仍然需要你自己再包一层

## 3. 相机系统架构

### `CameraManager`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/CameraManager.cs`

职责：

- 同时持有 `mainCamera` 与 `overlayCamera`
- 平滑控制主视角 FOV 与武器视角 FOV
- 提供相机抖动入口
- 与 `ProceduralAnimator` 中的 `Camera Kick` 动画相连

关键意义：

- 它天然支持武器视野和主视野分离
- 这对正式 FPS 的瞄准与武器展示层很有帮助

## 4. 第一人称控制器

### `FirstPersonController`

路径：

- `Assets/Akila/FPS Framework/Scripts/Character/FirstPersonController.cs`

职责：

- 基于 `CharacterController` 实现移动
- 支持 `walk / sprint / tactical sprint / crouch / jump`
- 支持坡面滑移、贴地、最大下落速度
- 支持移动平台自动跟随
- 支持保留跳跃动量
- 支持探身和脚步音效

比较有代表性的能力字段：

- `tacticalSprintSpeed`
- `autoDetectMovingPlatforms`
- `preserveMomentum`
- `slideDownSlopes`
- `stickToGroundForce`
- `stepInterval`

这说明它不是一个“最基础的 CharacterController 示例”，而是明显围绕 FPS 手感调过的一层。

## 5. Procedural Animation 系统

### `ProceduralAnimator`

路径：

- `Assets/Akila/FPS Framework/Scripts/Animation System/ProceduralAnimator.cs`

职责：

- 聚合多个 `ProceduralAnimation`
- 以 position / rotation 输出最终动画结果
- 控制全局动画权重、帧率和启停

### 常见 Modifier

位于：

- `Assets/Akila/FPS Framework/Scripts/Animation System/Modifiers`

包括：

- `KickAnimationModifier`
- `MoveAnimationModifier`
- `OffsetAnimationModifier`
- `SpringAnimationModifier`
- `SwayAnimationModifier`
- `WallAvoidanceAnimationModifier`
- `WaveAnimationModifier`

这部分正是这套包“手感更像 FPS 包”的关键原因：

- 枪械晃动
- 呼吸
- 探身
- 后坐位移
- 贴墙避让

都被组织成了更明确的 procedural animation 体系。

## 6. 这套输入与移动层的优点

优点：

- 强烈偏向第一人称体验
- 输入域拆分比 JUTPS 更清晰
- 主相机 / 武器相机双 FOV 结构合理
- Procedural animation 对武器手感帮助很大
- 已有战术冲刺、探身、贴墙、坡面、移动平台等细节

## 7. 这套输入与移动层的限制

局限也很明显：

- 没有项目级输入上下文管理
- 没有完整重绑定产品化框架
- 暂停、菜单、仓库、商店等复杂 UI 焦点流还不够
- 只偏玩家第一人称，不适合直接拿去驱动复杂敌人 AI

## 8. 对 Project-XX 的建议

建议保留：

- `Controls.inputactions` 的分域思路
- `CharacterInput`
- `FirstPersonController`
- `CameraManager`
- `ProceduralAnimator`

建议新增一层“项目输入与镜头域”而不是直接改烂原脚本：

- `RaidInputContext`
- `RuntimeUiInputGate`
- `CameraStateBridge`
- `LookSensitivityService`
- `ProjectXXPauseCoordinator`

推荐接法：

1. 让 FPS Framework 继续负责玩家局内第一人称执行。
2. 让 Project-XX 负责菜单、仓库、交易、任务等 UI 输入焦点。
3. 所有复杂状态切换通过 Project-XX 上层桥来改 `IsInputActive / IsPaused`，而不是把业务逻辑全塞进 FPSF 原脚本。

