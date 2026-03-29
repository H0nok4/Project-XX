# FPS 第一人称武器对位与瞄准适配规范

## 1. 文档目标

本文档用于统一当前项目中第一人称武器的：

- 持有位置制作规范
- ADS 对位规范
- 武器 prefab 节点规范
- `WeaponPresentationProfile` 配置规范
- 校准流程与验收标准

它的目标不是规定某一把枪的最终美术效果，而是建立一套可复用、可迭代、可批量扩展的标准，让后续新武器接入时主要是“补资产与调参数”，而不是反复修改控制器逻辑。

---

## 2. 核心原则

### 2.1 命中以相机为准，武器负责把结果演出来

第一人称射击里，最稳定的方案通常是：

- 准星 / 相机中心决定真正命中方向
- 武器模型负责把“这次命中”演出来
- 枪口、火花、后坐力、壳体抛出属于表现层

这样做的好处是：

- 不会因为模型轻微偏一点就导致“准星对着目标但打歪”
- 每把枪都可以有不同的视觉姿态，但射击逻辑仍稳定
- ADS 对位的目标变成“让瞄具视觉上对上相机中心”，而不是把武器本身变成玩法权威

### 2.2 持有姿态、瞄准姿态、动作偏移必须分层

每把第一人称武器至少应拆成三层概念：

- `Hip Pose`
说明：默认持枪位置，武器 prefab 在视图锚点下的初始姿态
- `Aim Pose`
说明：进入 ADS 时，把瞄具中心对到相机中心的姿态
- `Action Offset`
说明：recoil、equip、reload、melee、sway 等叠加偏移

不要把这三件事揉成一个固定位置，否则后续：

- 瞄准难调
- 不同武器难复用
- 切枪 / 换弹 / 后坐力很容易互相打架

### 2.3 武器玩法定义与武器表现定义分离

当前项目中建议保持：

- `PrototypeWeaponDefinition`
负责武器玩法定义、视图模型 prefab 引用、表现 profile 引用
- `WeaponPresentationProfile`
负责 ADS fallback、recoil、equip / reload / melee motion、locomotion sway 参数
- `PlayerWeaponPresentationController`
负责读取武器 prefab 节点和 profile，并在运行时混合表现

---

## 3. 当前项目中的标准边界

### 3.1 当前代码落点

- 武器玩法定义：[PrototypeWeaponDefinition.cs](d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/Items/Definitions/PrototypeWeaponDefinition.cs#L1)
- 武器表现配置：[WeaponPresentationProfile.cs](d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/FPS/WeaponPresentationProfile.cs#L1)
- 第一人称武器表现控制器：[PlayerWeaponPresentationController.cs](d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/FPS/PlayerWeaponPresentationController.cs#L1)
- 第一人称左手 IK 控制器：[PlayerFpArmsLeftHandIkController.cs](d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/FPS/PlayerFpArmsLeftHandIkController.cs#L1)
- 第一人称右手姿态修正：[PlayerFpArmsRightHandPoseCorrector.cs](d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/FPS/PlayerFpArmsRightHandPoseCorrector.cs#L1)
- 第一人称右手握把 IK：[PlayerFpArmsRightHandGripIkController.cs](d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/FPS/PlayerFpArmsRightHandGripIkController.cs#L1)
- 第一人称视图锚点：[PlayerAnimationRigRefs.cs](d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/FPS/PlayerAnimationRigRefs.cs#L1)
- 玩家预制体：[FpsPlayer.prefab](d:/UnityProject/Project-XX/Project-XX/Assets/Res/Prefabs/Player/FpsPlayer.prefab)

### 3.2 当前系统已支持的 ADS Pose 搜索名称

`PlayerWeaponPresentationController` 当前会按以下名称查找瞄准节点：

- `ScopePose`
- `AdsPose`
- `AimPose`
- `IronSightPose`

建议后续统一优先使用：

- `AimPose`

其它名称只保留兼容意义，不作为长期标准。

### 3.3 当前系统的姿态解算方式

当前控制器会把武器 prefab 中的 `AimPose` 视为“需要对到相机原点和朝向的目标点”，然后反解出武器 root 在 ADS 时应移动到哪里。

因此在制作上要理解为：

- 武器 prefab 的默认 root 姿态就是 `Hip Pose`
- `AimPose` 不是“另一个要直接挂的武器位置”
- `AimPose` 是一个标记点，表示“这把枪的瞄具中心应该在哪里、朝哪里”

---

## 4. 第一人称武器 prefab 标准结构

建议所有第一人称武器 prefab 按以下结构组织：

```text
WeaponView_<WeaponName>
├─ MeshRoot
├─ Muzzle
├─ AimPose
├─ LeftHandIK
├─ RightHandGrip
├─ ShellEject
├─ MagazineAnchor
├─ SprintPose
└─ InspectPose
```

不是所有节点都必须存在，但以下节点建议按优先级执行：

### 4.1 必须有

- `Muzzle`
说明：枪口、火花、曳光、子弹出膛特效参考点
- `AimPose`
说明：ADS 对位的标准节点

### 4.2 强烈建议有

- `LeftHandIK`
说明：左手握持目标点
- `RightHandGrip`
说明：右手主握把目标点，优先用于第一人称主手 IK 锁定
- `ShellEject`
说明：抛壳参考点

### 4.3 可选

- `MagazineAnchor`
说明：换弹时弹匣分离、挂点或特效对齐
- `SprintPose`
说明：冲刺姿态参考点
- `InspectPose`
说明：检视姿态参考点

---

## 5. 命名与坐标规范

### 5.1 Root 规范

武器 prefab 的根节点应满足：

- 默认挂到 `WeaponView_Primary / Secondary / Melee` 下时就是 `Hip Pose`
- root 本地缩放尽量保持 `1,1,1`
- 武器前方尽量保持 `+Z`
- 上方尽量保持 `+Y`

如果导入模型时根节点缩放不是 `1`，优先处理模型导入或中间空节点，不要长期把异常缩放留到运行时再补偿。

### 5.2 AimPose 规范

`AimPose` 应满足：

- 节点位置放在瞄具中心或机械瞄准中线中心
- 节点朝向与相机 ADS 朝向一致
- 推荐 `+Z` 指向武器射击前方
- 推荐 `+Y` 指向屏幕上方

如果 `AimPose` 的轴向不对，会出现这些典型问题：

- ADS 进来后枪会歪
- 镜头对上准心，但武器有奇怪的 roll
- 不同瞄具下偏转方向不一致

### 5.3 Muzzle 规范

`Muzzle` 应放在：

- 枪口真实出弹位置
- 刀具或近战武器可不强制使用
- 投掷物武器若有握持模型，可单独定义 `ThrowRelease`，不要复用普通 `Muzzle`

### 5.4 LeftHandIK 规范

`LeftHandIK` 应放在：

- 左手自然握持的位置
- 尽量避免放在会被动画大幅摆动的次级零件上
- 如果武器有可切换前握把或伸缩配件，建议后续拆成多个 IK 目标而不是只用一个

### 5.5 当前工程的 IK 接法

当前工程已经正式接入：

- `com.unity.animation.rigging@1.4.1`
- `PlayerFpArmsLeftHandIkController`
- `FPLeftHandIKRig / FPLeftHandIKConstraint / FPLeftHandIKTarget / FPLeftHandIKHint`

当前约定如下：

- 只对第一人称主武器 / 副武器启用左手 IK
- 当前工程对第一人称主武器 / 副武器启用 `RightHandGrip` 主手握把 IK
- `PlayerFpArmsRightHandPoseCorrector` 现在是兜底层，只在武器未提供 `RightHandGrip` 时参与右手补形
- 运行时会从当前活动武器 prefab 里递归查找 `LeftHandIK`
- 运行时会从当前活动武器 prefab 里递归查找 `RightHandGrip`
- 若武器没有 `LeftHandIK`，左手 IK 权重会自动回落为 `0`
- 若武器没有 `RightHandGrip`，右手会回退到基础持枪动画与右手补形层

这意味着后续接新枪时，只要补好 `LeftHandIK` 节点，就能直接接入现有左手约束链，而右手则可以先复用项目里的 hip / ADS 修正层，再根据需要回收到更明确的基础持枪动画
这意味着后续接新枪时，标准流程已经变成：

- 左手补 `LeftHandIK`
- 右手补 `RightHandGrip`
- 其余问题再由 profile 与少量动画兜底

---

## 6. WeaponPresentationProfile 规范

### 6.1 资产职责

`WeaponPresentationProfile` 用来定义每把武器的表现参数，当前已支持：

- `ADS Fallback`
- `Recoil`
- `Action Motion`
- `Locomotion Sway`

它的作用是：

- 同类逻辑复用同一套控制器
- 不同枪只换 profile 和 prefab 节点
- 后续调手感时优先调资产，不改代码

### 6.2 推荐字段分工

- ADS fallback
说明：当 prefab 没有正确 `AimPose` 或临时需要快速调姿态时使用
- Recoil
说明：枪击后退、枪口上扬、回正速度
- Action Motion
说明：切枪、换弹、近战挥击的整体位移和角度幅度
- Locomotion Sway
说明：移动时 bob、roll、pitch、瞄准时 sway 抑制倍率

### 6.3 建议初始参数范围

以下是适合作为初始调参区间的经验值：

- 步枪 recoil
说明：`fireKickBack 0.035~0.055`，`fireKickPitch 4~7`
- 手枪 recoil
说明：`fireKickBack 0.025~0.04`，`fireKickPitch 2.5~5`
- 重武器 recoil
说明：`fireKickBack 0.05~0.08`，`fireKickPitch 5.5~9`
- recoil 回正速度
说明：`14~22`
- equipDuration
说明：`0.18~0.32`
- reloadDuration
说明：先与实际动画长度一致，再微调
- meleeDuration
说明：与近战动画挥击主节奏一致，通常 `0.12~0.25`
- aimSwayMultiplier
说明：瞄准时建议 `0.25~0.55`

这些数值只是起点，不是最终标准。

---

## 7. 标准制作流程

建议每把枪都按以下流程制作。

### 7.1 第一步：确认导入与缩放

先确认：

- 模型导入后尺寸是否合理
- root 缩放是否接近 `1,1,1`
- 枪口是否朝 `+Z`

如果这一步就不稳定，后面所有 ADS 与 IK 都会越来越难调。

### 7.2 第二步：确定 Hip Pose

把武器 prefab 挂到第一人称锚点下后，先只调默认 root 姿态，让它在腰射状态下看起来自然。

这一步只做：

- 持枪高度
- 左右偏移
- 前后距离
- 默认 roll / yaw / pitch

不要在这一步强行把瞄具对到屏幕中心，因为那是 `AimPose` 的职责。

### 7.3 第三步：制作 AimPose

在武器 prefab 内创建 `AimPose`：

- 把它放在瞄具中心
- 把它的局部朝向调成与 ADS 期望朝向一致
- 如果是机瞄，就对准机械瞄具中心线
- 如果是镜瞄，就对准镜片中心

推荐做法是：

- 先把 Scene 相机放到你觉得“准”的视角
- 再把 `AimPose` 对到这个视角
- 最后由控制器反解武器 root 的 ADS 姿态

### 7.4 第四步：制作 Muzzle

在枪口实际出弹位置放 `Muzzle`：

- 视觉特效从这里出
- 如果枪口有制退器、消音器或多枪管，要以实际出弹点为准

### 7.5 第五步：制作 LeftHandIK

在武器前部放 `LeftHandIK`：

- 位置以左手自然扶持点为准
- 不要为了暂时贴合某一套动画而放得极端
- 优先保证“站立、ADS、移动”三种常用状态都合理

当前项目里建议把 `LeftHandIK` 当成“支撑手默认贴合点”，第一轮先保证：

- 腰射静止时握持自然
- ADS 时左手不会脱离护木 / 枪身
- 移动时手臂不会被目标点拉到夸张位置

如果三者不能同时满足，优先顺序建议是：

1. 先保证 ADS
2. 再保证腰射静止
3. 最后再微调移动中的贴合

不要在第一轮就为了移动 pose 过度牺牲 ADS 对位

### 7.6 第六步：配置 WeaponPresentationProfile

为这把枪创建或指定 profile：

- 如果 `AimPose` 已正确，ADS fallback 只作兜底
- recoil 根据枪型调
- equip / reload / melee 根据武器重量感调
- locomotion sway 根据武器大小和持枪稳定性调

### 7.7 第七步：运行时校准

进游戏后优先检查：

- 腰射持有位置是否自然
- ADS 是否能稳定对中
- 瞄准进出时是否有奇怪的 roll
- 开火后 recoil 是否破坏瞄具中心感
- 移动摆动是否过大或过小

---

## 8. 不同武器类别的适配建议

### 8.1 枪械

枪械至少应具备：

- `Muzzle`
- `AimPose`
- `LeftHandIK`
- `WeaponPresentationProfile`

这是第一优先级。

### 8.2 近战

近战武器一般不要求标准 ADS，但建议仍保留：

- 默认持有位置
- 挥击 profile
- 如有需要可保留 `AimPose` 作为“观察姿态”或特殊攻击姿态参考

如果近战完全不走 ADS，可让 profile 的 ADS fallback 保持零偏移。

### 8.3 投掷物

投掷物通常不需要传统 `Muzzle` / `AimPose` 逻辑，但建议预留：

- 默认手持位置
- 投掷起手 pose
- 释放参考点

如果后续要做更真实的抛掷动画，建议不要继续复用普通枪械的 ADS 语义，而是单独定义 `ThrowRelease` 节点。

---

## 9. 常见问题与修正方法

### 9.1 ADS 进来后准心还是偏

优先检查：

- `AimPose` 的位置是否在真实瞄具中心
- `AimPose` 的局部轴向是否正确
- prefab root 是否带异常缩放
- 是否错误依赖了 fallback，而不是实际 `AimPose`

### 9.2 武器在某些动作里看起来会歪

优先检查：

- `AimPose` 的 roll 是否干净
- recoil 角度是否过大
- locomotion sway 的 roll / pitch 是否叠太多
- equip / reload offset 是否把 ADS 姿态拉坏

### 9.3 火花或曳光从奇怪的位置出来

优先检查：

- `Muzzle` 是否在真正枪口
- 枪口下是否还有异常缩放节点
- prefab 内特效系统是否还在用旧节点

### 9.4 左手抓不住武器

优先检查：

- `LeftHandIK` 是否缺失
- 左手目标是否放在动态配件上
- 动画本体与 IK 约束是否冲突

### 9.5 同一把枪在镜子里和第一人称里看起来不一致

优先区分：

- 第一人称是 `ViewModel`
- 镜子里看到的是 `LocalFullBody` 的第三人称可见武器

这两者不应该强求完全同姿态，但应该在握持逻辑和动作时机上保持一致。

---

## 10. 验收清单

每把新武器接入后，建议至少走完以下验收：

1. 腰射静止时，武器持有位置自然，不挡住过多屏幕中心。
2. ADS 进入后，瞄具中心稳定对准相机中心，没有明显 roll。
3. 开火后 recoil 有反馈，但不会把 ADS 中心感完全打散。
4. 移动、横移、冲刺、蹲伏时，摆动幅度符合枪型预期。
5. 切枪、换弹、近战、医疗、投掷期间，武器不会跳位或穿模。
6. 枪口火花、曳光、抛壳位置正确。
7. 第三人称本地可见武器与第一人称动作时机一致。

---

## 11. 当前项目推荐执行顺序

对这个项目，推荐按下面顺序继续推进：

1. 先统一所有第一人称武器 prefab 的节点命名。
2. 为当前主武器 / 副武器 / 近战各补一个 `WeaponPresentationProfile`。
3. 先把 `AimPose`、`Muzzle`、`LeftHandIK` 补齐，再调 recoil 和 sway。
4. 等 Unity Editor 工具恢复后，把 profile 与新组件显式挂回 prefab，并做 Play Mode 回归。
5. 最后再推进左手 IK、换弹分段、检视动作、冲刺姿态等高阶表现。

### 11.1 当前项目已落地的首批 profile

当前工程里已经为默认三类武器和其同模变体补上首批 `WeaponPresentationProfile`：

- `Assets/Res/Data/PrototypeFPS/Weapons/Profiles/WeaponPresentationProfile_Carbine.asset`
- `Assets/Res/Data/PrototypeFPS/Weapons/Profiles/WeaponPresentationProfile_Sidearm.asset`
- `Assets/Res/Data/PrototypeFPS/Weapons/Profiles/WeaponPresentationProfile_CombatKnife.asset`

当前绑定范围：

- `Weapon_Carbine`、`Weapon_SchoolCarbine`、`Weapon_HospitalCarbine`
- `Weapon_Sidearm`、`Weapon_SchoolSidearm`、`Weapon_HospitalSidearm`
- `Weapon_CombatKnife`、`Weapon_SchoolKnife`、`Weapon_HospitalKnife`

这意味着后续如果只是在这些枪之间微调第一人称手感，优先直接调 profile，而不是继续改 `PlayerWeaponPresentationController`。

---

## 12. 结论

可复用的第一人称武器适配标准，本质上不是“每把枪都手调到看着差不多”，而是：

- prefab 节点统一
- profile 参数统一
- 玩法与表现边界统一
- 校准流程统一

只要这四件事稳定下来，后续新增武器的主要工作就会变成：

- 做 prefab
- 摆 `AimPose`
- 摆 `Muzzle`
- 摆 `LeftHandIK`
- 配 `WeaponPresentationProfile`

而不是继续改控制器。
