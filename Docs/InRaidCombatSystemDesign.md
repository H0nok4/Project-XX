# Project-XX 局内战斗系统设计

## 1. 文档范围

本文档只讨论局内系统，也就是玩家进入 `SampleScene` 之后的运行时战斗链路。

本文档覆盖：

- 玩家控制
- 武器与攻击
- 单位生命与护甲
- 状态效果
- AI 感知与攻击
- 战局状态
- 局内 UI 与反馈

本文档不重点讨论：

- 主菜单
- 局外仓库
- Profile 持久化

这些内容见：

- [MetaProfileAndWarehouseDesign.md](D:/UnityProject/Project-XX/Project-XX/Docs/MetaProfileAndWarehouseDesign.md)

---

## 2. 局内系统目标

当前局内原型的目标不是做完整产品，而是验证以下闭环：

1. 玩家进入战斗场景
2. 玩家能移动、射击、近战、搜刮、撤离
3. AI 能发现玩家并攻击
4. 命中能经过护甲、部位、状态效果结算
5. 战局能正常结束并显示结果

因此设计上优先：

- 可玩性
- 可调性
- 快速扩展

而不是：

- 最终性能
- 最终 UI 质量
- 最终资源组织

---

## 3. 局内模块分层

建议把当前局内系统理解为 6 层：

1. 输入层
2. 玩家控制层
3. 战斗结算层
4. AI 行为层
5. 战局状态层
6. 表现层

依赖方向：

`Input -> Controller -> Combat/Vitals -> Raid State -> UI Feedback`

其中 AI 是另一条平行控制链：

`Perception -> Bot Controller -> Combat/Vitals -> UI Feedback`

---

## 4. 输入与玩家控制

### 4.1 输入封装

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeFpsInput.cs`

职责：

- 封装 Input System 的动作读取
- 提供统一访问入口
- 隔离键位与控制逻辑

当前暴露的典型输入：

- 移动
- 视角
- 射击
- 跳跃
- 蹲下
- 冲刺
- 交互
- 背包
- 换弹
- 开火模式切换
- 快速医疗

设计意义：

- 让 `PrototypeFpsController` 不直接依赖底层按键
- 后续做自定义按键时，不需要重写控制器逻辑

### 4.2 玩家控制器

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs`

职责：

- 第一人称视角控制
- 地面移动
- 空中控制
- 跳跃与落地速度逻辑
- 体力消耗与恢复
- 蹲伏与站起
- 武器切换
- 射击与近战
- 医疗快捷键
- 局内 HUD

### 4.3 当前移动设计

当前移动手感参考 CS/Source 系：

- 地面有加速度与摩擦
- 空中横向控制受限
- 只有 `A/D + 鼠标同向移动` 才能有效空中转向
- 落地会丢速
- 连跳可避免正常落地掉速

### 4.4 当前蹲伏设计

蹲伏不是单纯降视角，而是同时调整：

- `CharacterController.height`
- `CharacterController.center`
- 相机高度
- `stepOffset`

并且有顶头检测：

- 只有头顶空间足够才允许站起

设计意义：

- 玩家可以通过低矮障碍
- 不会在低矮通道里强制站起穿模

### 4.5 体力系统

体力属于单位级资源，但当前主要由玩家控制器消费。

当前体力参与动作：

- 冲刺
- 跳跃
- 近战

恢复规则：

- 消耗后进入恢复延迟
- 耗尽后进入更长的虚脱恢复延迟
- 低于动作阈值时不能开始新的耗体力动作
- 但已经开始的动作可以继续消耗到 0

---

## 5. 武器与攻击链路

### 5.1 武器定义

核心脚本：

- `Assets/Res/Scripts/Items/Definitions/PrototypeWeaponDefinition.cs`
- `Assets/Res/Scripts/Items/Definitions/AmmoDefinition.cs`

当前武器定义包含：

- 是否近战
- 弹药定义
- 弹匣容量
- 射速
- 换弹时长
- 开火模式
- burst 次数
- 散布
- 有效距离
- 近战伤害与范围

### 5.2 玩家射击链路

玩家攻击流程：

1. `PrototypeFpsInput` 提供攻击输入
2. `PrototypeFpsController` 判断当前武器状态
3. 根据武器定义决定：
   - 单发
   - burst
   - 自动
   - 近战
4. 射线或近战检测命中碰撞体
5. 识别 `PrototypeUnitHitbox`
6. 构造 `PrototypeUnitVitals.DamageInfo`
7. 调用目标的 `ApplyDamage`

### 5.3 近战攻击

近战目前仍走武器定义，而不是独立系统。

优点：

- 近战与远程共享一套武器槽逻辑
- AI 和玩家都能复用同一套近战定义

代价：

- 近战动作系统、动画系统、命中窗口仍然较简单

### 5.4 噪声系统

核心脚本：

- `Assets/Res/Scripts/AI/PrototypeBotController.cs`
  - 内含 `PrototypeCombatNoiseSystem`

玩家这些行为会广播噪声：

- 开枪
- 近战
- 移动
- 冲刺
- 起跳
- 落地

设计意义：

- AI 不必只靠视线发现玩家
- 搜索和警觉能建立在统一事件源上

---

## 6. 单位生命、护甲与部位系统

### 6.1 核心设计

局内所有可受伤单位都围绕这三个对象工作：

- `PrototypeUnitDefinition`
- `PrototypeUnitVitals`
- `PrototypeUnitHitbox`

### 6.2 UnitDefinition

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeUnitDefinition.cs`

职责：

- 定义单位的部位结构
- 定义每个部位的属性
- 决定血条锚点

每个部位当前可配置：

- `partId`
- `displayName`
- `maxHealth`
- `overflowMultiplier`
- 是否计入总生命
- 是否接收溢出伤害
- 黑掉后的致死规则
- 溢出转发目标

这意味着当前部位系统已经不是写死 `Head/Torso/Legs` 的旧实现，而是数据驱动。

### 6.3 UnitHitbox

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeUnitHitbox.cs`

职责：

- 将物理命中映射到某个 `partId`
- 将命中转发到 `PrototypeUnitVitals`
- 支持“外层部位 -> 内层部位”的命中传递

适用场景：

- 头盔拦头
- 外层甲片保护躯干
- 特殊怪物外壳保护核心

### 6.4 UnitVitals

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeUnitVitals.cs`

职责：

- 维护部位生命状态
- 处理护甲结算
- 处理穿深
- 处理溢出伤害
- 触发死亡
- 记录最后伤害来源
- 发出战斗反馈事件

### 6.5 护甲覆盖区

当前护甲是按部位覆盖工作的：

- 护甲定义声明覆盖哪些 `partId`
- 命中某部位时，`Vitals` 会先检查是否有护甲覆盖
- 若覆盖，先计算：
  - 护甲是否拦截
  - 耐久损耗
  - 是否被穿透

当前目标：

- 允许不同目标拥有不同护甲组合
- 能直观看到护甲命中、护甲损坏、肉伤

### 6.6 溢出与黑部位逻辑

当前设计保留了原型级 Tarkov-like 思路：

- 部位打空后，超出部分会按规则分摊
- 某些部位可配置为黑后再受伤致死
- 某些部位可配置为不接收溢出

这个设计的价值在于：

- 不需要重写伤害系统，就能支持非人形目标
- 特殊怪物核心、外壳、护甲层都能通过配置落地

---

## 7. 状态效果系统

### 7.1 当前方向

状态效果已经从“部位级特殊逻辑”收敛为“单位级 Buff/Debuff 系统”。

核心脚本：

- `Assets/Res/Scripts/FPS/PrototypeStatusEffectController.cs`

### 7.2 当前效果

- Light Bleed
- Heavy Bleed
- Fracture
- Painkiller

### 7.3 为什么这么改

原因不是为了更简单，而是为了更可扩展。

如果继续把流血、骨折、止痛都绑在部位系统里，后面加入：

- 中毒
- 灼烧
- 迟缓
- 增伤
- 免伤
- RPG 类短时 Buff

会越来越乱。

现在改成统一状态系统后：

- `Vitals` 只关心伤害与生命
- `StatusEffectController` 只关心持续效果

边界更清楚。

### 7.4 当前与控制器的关系

玩家控制器读取状态系统输出的惩罚系数，例如：

- 移动减速
- 跳跃惩罚
- 止痛临时压制骨折影响

---

## 8. AI 战斗系统

### 8.1 核心脚本

- `Assets/Res/Scripts/AI/PrototypeBotController.cs`

### 8.2 当前 AI 设计方向

AI 已从“通用 Bot”收敛成“敌人 archetype 驱动”。

当前 archetype：

- 普通丧尸
- 警察丧尸
- 军人丧尸
- 丧尸犬

### 8.3 感知能力

当前 AI 感知来源：

- 视觉
- 听觉
- 嗅觉

其中：

- 视觉受视野、距离、遮挡影响
- 听觉来自 `PrototypeCombatNoiseSystem`
- 嗅觉当前主要给丧尸犬使用，可跨障碍范围索敌

### 8.4 攻击行为

近战敌人：

- 追到近距离后近战攻击

远程敌人：

- 有攻击间隔
- 警察丧尸倾向单发后冷却
- 军人丧尸倾向短 burst 后冷却
- 不是每发死锁玩家中心
- 会锁一个身体圆形区域并随机命中点

这样做的目的：

- 防止远程 AI 过于“外挂化”
- 保留可躲避空间
- 保证不同敌人类型有明显战斗差异

### 8.5 当前取舍

已移除或弱化的方向：

- 复杂掩体战术
- 高度泛化的战术 Bot 系统

原因：

- 当前项目更接近丧尸敌人原型
- 设计目标强调“敌人类型差异”，不是“战术行为深度”

---

## 9. 战局状态系统

### 9.1 核心脚本

- `Assets/Res/Scripts/Raid/RaidGameMode.cs`

### 9.2 当前职责

- 管理战局计时
- 跟踪玩家是否存活
- 处理撤离成功
- 处理超时失败
- 显示结算界面

### 9.3 与玩家 UI 焦点的关系

战局结算界面不是普通画面叠层，而是会实际占用：

- `PlayerInteractionState`

这样可以保证：

- 死亡/撤离后自动显示鼠标
- 玩家控制器不再锁回鼠标
- 可以直接点击 `Return To Menu`

这是近期修复的关键点之一。

---

## 10. 局内交互与搜打撤闭环

### 10.1 交互接口

核心接口：

- `Assets/Res/Scripts/Interaction/IInteractable.cs`

当前接入对象：

- 地面物品
- 箱子
- 撤离点

### 10.2 玩家交互器

核心脚本：

- `Assets/Res/Scripts/Interaction/PlayerInteractor.cs`

职责：

- 视线射线检测
- 找最近可交互目标
- 显示提示
- 调用交互对象

### 10.3 当前闭环

玩家进入场景后可以：

1. 与地面拾取物交互
2. 与箱子交互搜刮
3. 与撤离终端交互完成撤离

这三类交互已经构成原型期的“搜打撤”最小闭环。

---

## 11. 局内表现层

### 11.1 核心脚本

- `Assets/Res/Scripts/FPS/PrototypeTargetHealthBar.cs`
- `Assets/Res/Scripts/FPS/PrototypeCombatTextController.cs`
- `Assets/Res/Scripts/FPS/PrototypeFpsController.cs` 内 HUD
- `Assets/Res/Scripts/Raid/RaidGameMode.cs` 结算 UI

### 11.2 当前反馈目标

当前表现层不是追求美术完成度，而是追求“调试清晰”。

所以它优先反馈：

- 打到护甲还是肉体
- 护甲是否损坏
- 目标还有多少总血量
- 玩家当前武器/体力/状态
- 战局是否成功或失败

---

## 12. 当前局内系统的优点

### 12.1 优点

- 已形成完整单局闭环
- 部位、护甲、状态、武器已经基本数据化
- 玩家与 AI 都共享同一套伤害系统
- UI 焦点问题已基本被统一到 `PlayerInteractionState`
- Scene Builder 可快速重建测试环境

### 12.2 局内系统最有价值的部分

当前最值得保留和继续扩展的是：

- `PrototypeUnitDefinition + PrototypeUnitVitals`
- `InventoryContainer`
- `PrototypeFpsInput`
- `PlayerInteractionState`

这些已经具备继续演化成正式系统的价值。

---

## 13. 当前局内系统的短板

### 13.1 UI 还是 IMGUI

优点是快，缺点是：

- 不适合复杂战斗 HUD
- 不适合正式风格结算界面
- 不适合装备面板与复杂动画

### 13.2 玩家控制器职责仍偏多

`PrototypeFpsController` 当前承担了：

- 移动
- 战斗
- 医疗
- HUD
- AI 噪声广播

后续最好拆成：

- MovementController
- WeaponController
- MedicalController
- HudPresenter

### 13.3 AI 仍属于原型级

虽然已经足够测试，但还不适合长期复杂扩展：

- 没有明确行为树/状态机资产化
- 没有更细的动画/动作层
- 没有 squad/协同行为

---

## 14. 建议的下一步局内迭代

如果继续增强局内系统，建议优先级如下：

1. 更稳定的装备栏与护甲穿戴显示
2. 更正式的战斗 HUD
3. 更丰富的 LootContainer 类型
4. 敌人掉落与尸体搜刮
5. 更细的 AI 动画与动作反馈
6. 将 `PrototypeFpsController` 进一步拆模块

---

## 15. 结论

当前局内系统已经不只是“射击 Demo”，而是一个完整的单局原型：

- 有战斗
- 有搜刮
- 有撤离
- 有结算
- 有 AI
- 有护甲/部位/状态效果

下一阶段不一定要继续加更多局内功能，反而更值得把：

- 装备栏
- 仓库
- 敌人掉落
- 局外成长

和局内系统真正接起来。
