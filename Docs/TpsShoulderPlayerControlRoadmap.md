# 第三人称越肩主角控制实施路线图

## 1. 路线图定位

本文档是《`Docs/TpsShoulderPlayerControlRefactorPlan.md`》的执行拆解版，用来回答两件事：

1. 先做什么，才能最快看到越肩视角结果。
2. 哪些阶段可以停下来继续做内容，而不会把项目卡死在半重构状态。

原则是：

- 先切主视角
- 再修角色朝向和瞄准
- 再迁移武器、医疗、投掷表现
- 最后收尾 FPS 兼容层

---

## 1.1 当前进度记录（2026-04-01，已追加到本轮 TPS 交互修正）

为便于后续回查，本路线图从现在开始记录阶段进度。

- 阶段 0：已完成
  - 主视角切换目标、Cinemachine 方案和第三人称桥接迁移路线已经定稿
- 阶段 1：已完成第一版
  - `PlayerShoulderCameraController` 已落地
  - `FpsPlayer.prefab` 已接入 `ShoulderFollowTarget`、`ShoulderCameraRig`、`WorldCamera + CinemachineBrain`
  - `ViewCamera` 已降级为兼容玩法相机，`WorldCamera` 已成为第三人称输出相机
  - 滚轮缩放、瞄准收近、基础肩后构图已接上
- 阶段 1.5：基础能力已接入，继续调参
  - 滚轮距离上下限已明确
  - `Default / SceneObject / Ground` 避障层已写入 `CinemachineThirdPersonFollow`
  - 已补上输出相机的手动 `SphereCast` 防穿墙钳制，镜头遇到遮挡时会向玩家方向回缩
  - 仍需继续调构图、阻尼、狭窄空间表现与瞄准收近手感
- 阶段 2：已完成第一版
  - `PlayerOrientationController` 已落地
  - 探索态下可见身体 / HitboxRig 已按移动方向转向
  - 左键 hip-fire 开火时可见身体 / HitboxRig 会瞬时对齐到相机 yaw，并在短时间内保持射击朝向
  - 右键精确瞄准时可见身体 / HitboxRig 已回正到相机 yaw，且移动速度会下降
  - `PlayerStateHub` 已开始汇总相机与朝向相关状态
- 阶段 3：已完成第一版桥接
  - `PlayerAimPointResolver` 已落地
  - 屏幕中心 aim point 已成为武器、投掷、交互共用的方向来源
  - `PlayerWeaponController` 已接入“屏幕中心瞄准点 + 枪口修正”首版
  - `PlayerStateHub` 已开始输出 `AimWorldPoint`
- 阶段 4：已开始第一版
  - `PlayerFullBodyAnimatorDriver` 已开始输出方向型 locomotion 与 TPS 朝向相关参数
  - `FpsPlayerFullBody.controller` 已补上 `Equip / Medical / Throw` 上半身占位状态
  - 持枪默认待机已切到 ready / aim 上半身姿态，右键精瞄继续通过 `AimBlend`、机位和速度差异体现“更精确”
  - 第三人称身体动画已开始从“基础能播”往“可承接完整动作路径”推进
- 阶段 5：已完成第一版兼容抽取
  - `PlayerWeaponPresentationController` 已落地
  - `PlayerWeaponController.RefreshWeaponViewModels / UpdateAimPresentation` 已降级为兼容转发入口
  - 第一人称视图模型实例化与 ADS 姿态逻辑已迁往独立表现层
  - `PlayerWeaponController` 内部旧的第一人称 view-model / ADS pose 实现已清理
  - 第三人称武器已接入右手 socket、骨骼缩放补偿与运行时生成的 world muzzle 兜底
- 阶段 6：已完成“交互”子项第一版
  - `PlayerInteractor` 已从纯 FPS 中心射线升级为 TPS 友好的三段式查询：
    - 相机中心射线
    - 相机小半径容错 SphereCast
    - 玩家交互原点朝准星方向的 reach SphereCast
  - 相机查询距离已按“相机到玩家交互原点的后撤距离”补偿，不再因为肩后镜头在玩家身后而打不到玩家眼前目标
  - 非触发器遮挡现在会正确阻挡更远交互物，避免隔墙 / 穿柜交互
  - 交互目标会按屏幕中心偏差、相机距离和玩家可触达距离综合评分，提升第三人称下的准星容错

后续每推进一段，都在这里更新状态，避免只剩抽象目标而看不出实际做到哪里。

---

## 2. 当前判断

结合现有代码与预制体，当前项目最适合走的不是“推翻重做”，而是“桥接迁移”路线：

- 继续保留 `PrototypeFpsInput`
- 继续保留 `CharacterController`
- 继续保留 `PlayerWeaponController / PlayerMedicalController / PlayerThrowableController`
- 直接复用 `PlayerStateHub / PlayerActionChannel / PlayerFullBodyAnimatorDriver`
- 新增 TPS 专用相机、朝向和瞄准桥接层

这条路线风险最低，且每一阶段都能回到可玩状态。

---

## 3. 分阶段路线

## 阶段 0：基线冻结与视角切换决策

### 目标

在真正改相机前，先把关键技术路线定死，避免边做边改方向。

### 要做的事

1. 明确主视角正式切换为“第三人称越肩”，不再以第一人称为默认游玩模式。
2. 明确采用 Cinemachine 3 做肩后镜头。
3. 明确滚轮用于镜头距离缩放。
4. 明确右键进入瞄准态时同时收短相机距离与 FOV。
5. 明确第一人称 arms / weapon view 仅保留为兼容层，不再作为核心制作路径。

### 交付物

- 本方案文档
- 本路线图
- `Docs/RefactorRoadmap.md` 中的引用入口

### 验收

- 团队对目标体验、相机方案、第一人称兼容定位没有歧义。

---

## 阶段 1：搭起第三人称越肩相机原型

### 目标

先让玩家“站在场景里就已经是肩后视角”。

### 要做的事

1. 新增 `PlayerShoulderCameraController`。
2. 给 `FpsPlayer.prefab` 增加相机 follow / aim 参考点。
3. 让 `ViewCamera` 改为被 CinemachineBrain 驱动的真实输出相机。
4. 建立基础镜头参数：
   - 默认肩后距离
   - 最小缩放距离
   - 最大缩放距离
   - 默认肩后横向偏移
   - 瞄准态肩后偏移
   - 默认 FOV
   - 瞄准态 FOV
5. 加入基础镜头碰撞回缩。

### 这一阶段先不做

- 开火对齐
- 医疗与投掷动作
- 左右肩切换
- 高级镜头噪声和镜头动画

### 完成标准

- 进入局内后，默认已经是第三人称越肩视角。
- 鼠标可正常转镜头。
- 镜头不会轻易穿墙。
- 玩家可在不瞄准时正常移动。

### 可暂停点

做到这里，就已经可以进入“第三人称探索态原型可玩”。

---

## 阶段 1.5：肩后镜头打磨与避障调参

### 目标

不扩大战斗链路，先把肩后镜头做得更稳定、更像正式游戏机位。

### 当前待做

1. 把滚轮缩放的上下限、步进与默认距离整理成明确可调参数。
2. 明确镜头避障层优先覆盖 `SceneObject` 与 `Ground`。
3. 检查贴墙、穿门、靠近大型遮挡时的回缩稳定性。
4. 记录需要后续慢慢调的构图项：
   - 探索态肩后偏移
   - 瞄准态收近幅度
   - FOV 收紧幅度
   - 跟随阻尼
   - 蹲伏 / 冲刺 / 跳跃切换时机位表现

### 定位

阶段 1.5 是“持续存在的调参层”，不是一次性做完后就永久关闭的阶段。后续即使进入阶段 2 / 3，也允许回头继续调。

### 当前状态

- 状态：基础能力已接入，后续持续调参
- 说明：本轮已补上滚轮范围约束、`Default / SceneObject / Ground` 避障层写入与手动防穿墙钳制，后续重点转为机位和手感微调

---

## 阶段 2：改造角色朝向与第三人称移动逻辑

### 目标

让角色控制方式从 FPS 变成 TPS。

### 要做的事

1. 新增 `PlayerOrientationController`。
2. 探索态下改为“角色朝向跟随移动方向”。
3. 瞄准态下改为“角色朝向跟随相机 yaw”。
4. 让 `PrototypeFpsMovementModule` 不再直接承担主相机体感位移。
5. 将蹲伏、冲刺、跳跃后的朝向和镜头状态统一起来。

### 重点验证

- 前进、后退、横移相对相机方向是否正确。
- 冲刺时角色是否稳定向前，不发生朝向抽搐。
- 蹲伏与跳跃时镜头和角色朝向是否同步。

### 完成标准

- 探索态已经具备标准 TPS 控制感。
- 瞄准态切换时角色朝向没有明显跳变。

### 当前状态（2026-03-31）

- 状态：已完成第一版
- 已落地：
  - `PlayerOrientationController`
  - `CharacterVisualRig / HitboxRig` 在探索态下按平面速度方向转向
  - `CharacterVisualRig / HitboxRig` 在右键精瞄态下回正到相机 yaw
  - hip-fire 开火时会瞬时吸附到相机 yaw，并在射击窗口内维持 TPS 射击朝向
  - `PrototypeFpsMovementModule` 已接入右键精瞄减速倍率
  - `PlayerStateHub` 输出 `CameraYaw`、`CameraDistance`、`IsAimCamera`、`IsFacingCameraYaw`、`CharacterYawDeltaToCamera`
- 本阶段保留的兼容项：
  - 玩家根节点 yaw 与旧玩法链仍保持兼容
  - `PrototypeFpsMovementModule` 里的 `ViewCamera` 头部体感位移仍存在，但已经不再驱动实际输出相机

### 可暂停点

做到这里，就已经可以进入“第三人称探索 + 基础瞄准姿态”阶段。

---

## 阶段 3：建立 TPS 瞄准点与开火对齐

### 目标

解决 TPS 改造中最核心的“看哪打哪”问题。

### 要做的事

1. 新增 `PlayerAimPointResolver`。
2. 把屏幕中心射线变成统一的 aim point 来源。
3. 改造 `PlayerAimController`，让其输出：
   - 是否正在瞄准
   - 瞄准混合值
   - 当前镜头参数
   - 当前瞄准世界点
4. 改造 `PlayerWeaponController` 的命中判定入口，使其适配：
   - 相机中心瞄准点
   - 枪口发射点
   - 屏幕准星
5. 同步修正投掷和交互方向来源。

### 必须解决的体验问题

- 准星对着敌人但子弹打偏
- 贴墙时枪口穿墙
- 越肩偏移导致近距离交互错位

### 完成标准

- 右键瞄准时射击结果与屏幕中心基本一致。
- 投掷和交互不再出现明显的视差错误。

### 当前状态（2026-03-31）

- 状态：已完成第一版桥接
- 已落地：
  - `PlayerAimPointResolver`
  - 基于第三人称实际输出相机的屏幕中心瞄准点解析
  - `PlayerWeaponController` 射击方向改为优先朝向 aim point，并把命中冲击方向同步到真实弹道方向
  - `PlayerWeaponController` 开火 / 近战起点已优先改为第三人称 world muzzle
  - `PlayerThrowableController` 投掷方向改为参考统一 aim point
  - `PlayerInteractor` 交互查询已改用统一 aim ray
  - `PlayerAimController / PlayerStateHub` 已可对外输出当前 `AimWorldPoint`
- 本阶段保留的兼容项：
  - 仍沿用现有 `ViewCamera` / 枪口 / 第一人称武器挂点作为过渡期发射与表现基础
  - 贴墙、极近距离目标与第三人称武器实体 socket 的最终精修仍留在后续打磨阶段

### 本轮实测结论补记

- 持枪默认上半身已进入 `FpsPlayer_Aim`，不再继续沿用空手待机。
- 第三人称武器已不再受角色骨骼 `lossyScale=100` 放大影响，运行时世界武器保持正确尺度。
- 当世界武器 prefab 不提供显式 `Muzzle` 时，系统会自动生成 `__GeneratedWorldMuzzle`，确保第三人称开火起点可用。
- 通过临时测试障碍验证，肩后相机在目标距离约 `2.8m` 时可被压缩到约 `1.06m`，说明最终输出镜头的防穿墙钳制已生效。

### 可暂停点

做到这里，TPS 战斗闭环即可开始进入持续验证。

---

## 阶段 4：让第三人称身体动画成为主表现路径

### 目标

把当前已经接上的全身 Animator 真正变成核心表现系统。

### 要做的事

1. 扩展 `PlayerFullBodyAnimatorDriver` 消费更多 TPS 状态。
2. 补齐第三人称动作资产优先级：
   - 探索待机
   - 探索移动
   - 瞄准待机
   - 瞄准移动
   - 开火
   - 换弹
   - 医疗
   - 投掷
3. 让 `PlayerActionChannel` 成为第三人称上半身动作切换的唯一动作来源。
4. 让角色身体与武器挂点都服务于第三人称表现，而不是只服务于第一人称 gun view。

### 完成标准

- 第三人称动作已经足以支撑主游玩体验。
- 第一人称视图模型即使暂时保留，也不再是必须依赖项。

### 当前状态（2026-03-31）

- 状态：已开始第一版
- 已落地：
  - `PlayerFullBodyAnimatorDriver` 现已输出 `MoveX`、`MoveY`、`AimBlend`、`WeaponSlot`、`WeaponCategory`、`IsFacingCameraYaw`、`CharacterYawDeltaToCamera`
  - `PlayerStateHub` 已开始为第三人称动画汇总按身体朝向计算的局部移动分量
  - `FpsPlayerFullBody.controller` 已接入 `Equip / Medical / Throw` 占位状态，`PlayerActionChannel` 发出的对应触发已不再被全身 Animator 忽略
- 本阶段仍待补：
  - 正式的第三人称瞄准移动、医疗、投掷动作资产
  - 基于 `MoveX / MoveY` 的更完整 TPS 方向性 locomotion 资源与 BlendTree
  - 第三人称武器 socket、挂点和动画联动的进一步收敛

--- 

## 阶段 5：拆出武器表现层，收缩 FPS 兼容路径

### 目标

把 `PlayerWeaponController` 里剩余的第一人称表现职责抽离掉。

### 要做的事

1. 新增 `PlayerWeaponPresentationController`。
2. 迁移：
   - 武器可见模型控制
   - 切枪显隐
   - 枪口特效
   - 后坐力表现
   - 瞄准表现参数
3. 将当前 `UpdateAimPresentation` 从“第一人称挂点核心逻辑”降级为兼容逻辑。
4. 明确第三人称武器 socket、枪口和动画联动方式。

### 完成标准

- `PlayerWeaponController` 只保留玩法权威职责。
- TPS 武器表现不再依赖 `ViewCamera` 子节点。

### 当前状态（2026-03-31）

- 状态：已完成第一版兼容抽取
- 已落地：
  - `PlayerWeaponPresentationController`
  - 第一人称视图模型实例化、切枪显隐、ADS pose 计算与应用已从 `PlayerWeaponController` 迁往独立表现层
  - `PlayerWeaponController.UpdateAimPresentation / ResetAimPresentationImmediate / RefreshWeaponViewModels` 已降级为对表现层的兼容转发
  - `PlayerAnimationRigRefs` 已开始补齐 `Muzzle / WeaponView_*` 显式引用解析
  - `PlayerWeaponController` 已删除旧的第一人称视图模型实例缓存、锚点创建与 ADS pose 搜索/应用实现
  - `PlayerAnimationRigRefs` 与 `FpsPlayer.prefab` 已开始补齐运行时/序列化 weapon anchor 引用，减少对 `ViewCamera.Find(...)` 的回退
- 本阶段仍待补：
  - 枪口火光、壳体抛出、后坐力与 sway 的进一步迁移
  - 第三人称武器 socket、枪口和动画联动方式的正式落地
  - 继续把 `ViewCamera` 子节点作为武器表现硬依赖的残余路径收缩为可选兼容层

---

## 阶段 6：纳入医疗、投掷、交互与 UI 焦点

### 目标

让局内所有常用动作都稳定适配越肩视角。

### 要做的事

1. 医疗动作接入第三人称动画与动作仲裁。
2. 投掷动作接入第三人称动画与瞄准点系统。
3. 交互射线改用统一瞄准方向。
4. UI 打开时统一冻结：
   - 相机转动
   - 玩家位移
   - 瞄准状态
   - 动作输入

### 完成标准

- 越肩视角下，搜刮、交互、医疗、投掷都不再像“FPS 兼容功能”。

### 当前状态（2026-04-01）

- 状态：交互子项已完成第一版，医疗 / 投掷 / UI 焦点收口仍待继续
- 已落地：
  - `PlayerInteractor` 已改用统一 aim ray + TPS 容错查询
  - 交互查询已从“相机前方固定 3 米”改为“相机补偿查询 + 玩家可触达过滤”
  - 交互遮挡已不再默认忽略实心环境碰撞体
- 实测补记：
  - 隔离合成用例下，旧逻辑在 `3.00m` 相机直射线范围内无法选中玩家眼前目标，而新逻辑可命中
  - 同一用例下，旧逻辑会穿过阻挡物继续选中后方交互体，而新逻辑会正确返回空目标

---

## 阶段 7：收尾与打磨

### 目标

从“能玩”进入“可持续制作”。

### 要做的事

1. 清理 `WorldCamera`、过渡挂点和不再使用的第一人称默认路径。
2. 视需要下线“速度档位滚轮”或改绑。
3. 评估是否引入左右肩切换。
4. 补镜头细节：
   - 贴墙压镜
   - 近距离角色遮挡
   - 镜头切边
   - 受击镜头反馈
5. 评估是否把 `PrototypeFpsController` 重命名为更中性的玩家控制入口。

### 完成标准

- 新功能不再默认以 FPS 思路接入。
- 玩家主角控制链已经被团队默认理解为 TPS 越肩架构。

---

## 4. 每阶段验收清单

每推进一阶段，建议都回归以下测试：

1. 默认探索态移动、冲刺、蹲伏、跳跃。
2. 右键瞄准切换是否平滑。
3. 滚轮缩放是否稳定，并限制在范围内。
4. 贴墙、过门、靠近大模型时镜头是否穿模。
5. 开火、换弹、切枪是否还能正常工作。
6. 投掷、医疗、交互是否仍可用。
7. 打开局内 UI 后输入与镜头是否冻结。
8. 角色死亡、重生和重新进局后镜头是否恢复正常。

---

## 5. 当前最推荐的实施顺序

如果接下来只走一条最稳妥的线，建议顺序如下：

1. 阶段 1：先出肩后相机
2. 阶段 2：再改朝向和 TPS 移动
3. 阶段 3：再做 TPS 瞄准点和开火对齐
4. 阶段 4：再让第三人称动画成为主路径
5. 阶段 5：最后抽掉第一人称武器表现硬依赖

原因：

- 这条路线最先把“玩家实际看到的体验”改对。
- 它最大程度复用现有玩法层代码。
- 它不会把项目卡死在“动画先行但体验仍然是 FPS”这种尴尬状态。

---

## 6. 里程碑判断

当以下条件成立时，可以认为第三人称越肩主角控制进入稳定阶段：

1. 玩家默认主视角已经是越肩相机。
2. 右键瞄准可稳定切到更近的越肩瞄准构图。
3. 滚轮可在安全范围内缩放镜头距离。
4. 角色探索态与瞄准态朝向规则稳定。
5. 射击、投掷、交互都适配 TPS 瞄准点。
6. 第三人称身体动作已成为主表现层。
7. 第一人称视图模型已降级为兼容或已可安全移除。

达到这里后，后续就可以把重点从“视角重构”转入“战斗手感、恐怖氛围和角色动作质量”的持续制作。
