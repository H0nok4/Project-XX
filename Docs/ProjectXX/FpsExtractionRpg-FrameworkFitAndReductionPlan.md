# Project-XX 框架适配与裁剪方案

## 1. 先说结论

你现在的真实技术前提不是：

- `JUTPS` 做玩家第一人称主底座

而应该是：

- `Akila FPS Framework` 做玩家第一人称操作、视角、枪械手感主底座
- `JUTPS` 做敌人、AI 感知、部分世界交互、部分人形执行层
- `Project-XX` 做规则、Meta、容器、成长、生存、正式 UI、正式存档

原因很直接：

- 你的项目明确是严格第一人称
- 你自己已经确认 `Akila` 的射击手感更符合目标
- `JUTPS` 的强项不在玩家 FPS 手感，而在现成的人形动作、AI、交互、载具和一整批原型世界模块

所以这次不是“删掉 JUTPS 还是保留 JUTPS”的二选一，而是：

- 把 `JUTPS` 从“玩家主框架”降级为“世界与敌人框架”

这一步做对了，后面很多冗余会自然消失。

## 2. 你的需求和两个框架分别冲突在哪

## 2.1 你的核心需求

Project-XX 当前最硬的约束有六个：

- 正式玩法严格第一人称
- 玩家枪械和镜头手感优先
- 局内外规则要自己控
- 正式 UI 不走框架自带 UI
- 存档不能绑死在第三方包内部结构上
- 要支持塔科夫式容器、死亡丢失、基地、商人、生存值、成长

## 2.2 JUTPS 的强项

- 人形角色动作底座完整
- 敌人 AI、FOV、听觉、攻击行为现成
- 世界交互链路干净
- 载具链路完整
- 近战怪、巡逻怪、简单持枪怪很快能跑起来

## 2.3 JUTPS 的冲突点

- 玩家链默认围绕 `JUCharacterController`
- 玩家 UI 默认围绕 `JUGameManager.PlayerController`
- 玩家库存默认围绕 `JUInventory`
- 玩家存档默认围绕 `JUSaveLoad`
- 玩家相机、输入、上车、切枪高度耦合
- 默认世界假设偏 TPS/TPS-FPS 混合，而不是纯 FPS 产品

一句话：

- `JUTPS` 最大的问题不是功能多，而是它默认认为“玩家就是 JUTPS 角色链”

这和你的目标直接冲突。

## 2.4 Akila FPS Framework 的强项

- `FirstPersonController` 的移动、视角、相机、姿态就是围绕 FPS 设计
- `CharacterInput` 和 `Firearm` 直接服务于第一人称武器操作
- 枪械、后坐、瞄准、视野变化、程序动画明显更贴近你要的手感
- 玩家交互链 `InteractionsManager` 足够轻
- 玩家伤害链 `Damageable` 足够直接

## 2.5 Akila FPS Framework 的弱项

- 库存系统是轻量快捷栏，不是搜打撤容器系统
- 存档是轻量 JSON，不适合最终 Meta 档案
- 世界 AI、怪物、巡逻、传感器、载具都不如 JUTPS 完整
- 商人、基地、任务、生存、词缀这些都没有成体系支持

一句话：

- `Akila` 适合当“玩家身体和枪”，不适合当“整个游戏框架”

## 3. 最合理的职责分工

建议直接锁成下面这个边界。

## 3.1 Akila 保留

- 玩家第一人称移动
- 玩家视角控制
- 玩家输入
- 玩家枪械开火与瞄准手感
- 玩家武器展示层
- 玩家近战执行层
- 玩家受击与死亡主状态
- 玩家交互输入入口

## 3.2 JUTPS 保留

- AI 感知
- AI 巡逻、追击、攻击
- 敌人用的人形动作执行层
- 世界交互物基类
- 门、机关、基础场景行为
- 载具系统
- 部分敌人近战命中链

## 3.3 Project-XX 自建

- ItemDefinition / ItemInstance
- 格子背包与容器
- 装备槽位与死亡丢失
- 生存值与恢复
- Buff / Debuff / 词缀 / 成长
- 基地、设施、商人、任务
- 局外仓库
- 正式 UI
- 正式存档

## 4. 哪些地方是硬冲突，必须改

## 4.1 玩家主控制器冲突

旧前提：

- 玩家使用 `JUCharacterController`

新前提：

- 玩家使用 `Akila.FirstPersonController`

这意味着：

- 不能继续把 JUTPS 玩家 prefab 当正式玩家
- 不能再把 R1 的桥接目标写成 “JUTPS 玩家角色 prefab”

## 4.2 玩家伤害链冲突

JUTPS 敌人和子弹默认主要打这些对象：

- `JUHealth`
- `DamageableBodyPart`
- `JUCharacterBrain`

Akila 玩家默认主要使用：

- `Damageable`

所以不加桥的话会出两个问题：

- JUTPS 敌人可能看得到玩家，但打不进玩家主血量
- 某些 AI 行为会判断不到玩家死亡态

第一阶段必须补：

- `JutpsHealthProxy`
- `JutpsTargetAdapter`

建议目标：

- JUTPS 世界侧继续看到一个 `JUHealth`
- Akila 玩家侧继续以 `Damageable` 为主权威

## 4.3 玩家 UI 冲突

JUTPS 很多 UI 和系统默认直接找：

- `JUGameManager.PlayerController`
- `JUCharacterController`
- `JUInventory`

这些对正式玩家都不再成立。

所以 JUTPS 这几类东西应直接退出正式产品链：

- `JUTPS Default User Interface`
- `JU_UIPause`
- `JU_UISettings`
- `UIInteractMessages`
- `UIItemInformation`
- `JUScopeSystemUI`
- `InventoryUIManager`
- `MobileRig`

结论：

- 不是“改一改继续用”，而是正式产品里整体下线

## 4.4 库存/容器冲突

Akila `Inventory`：

- 是快捷武器栏

JUTPS `JUInventory`：

- 是角色挂件式库存

你的目标：

- 是塔科夫式格子容器

所以这两个都不能承担最终仓库逻辑。

正确做法：

- `Project-XX` 维护真正的物品实例与容器状态
- `Akila Inventory` 只保留玩家当前已装备可用物
- `JUTPS JUInventory` 不进入正式玩家链

## 4.5 存档冲突

JUTPS `JUSaveLoad`：

- 太场景组件化

Akila `SaveSystem`：

- 太轻

结论：

- 两边都不适合作为最终存档主干

保留策略：

- 两者都只允许用于 demo 或临时调试
- 正式进度全部走 Project-XX 自己的 `ProfileSave / BaseHubSave / RaidSave / Settings`

## 4.6 载具冲突

JUTPS 载具进入链深度依赖：

- `DriveVehicles`
- `JUCharacterController`
- `JUInteractionSystem`

这和 Akila 玩家不兼容。

结论很简单：

- 第一阶段不要追求 Akila 玩家无缝复用 JUTPS 载具

这里真正该做的是延期，不是硬接。

## 5. 什么该保留，什么该冻结，什么该删除

## 5.1 JUTPS 中建议保留

- `Scripts/AI`
- `Scripts/Gameplay/Interaction System`
- `Scripts/Gameplay/Weapon Management/MeleeWeapon.cs`
- `Scripts/Gameplay/Character Controllers` 里给敌人用的人形执行层
- `Scripts/Armor System`
- `Scripts/Physics/Vehicle Physics`
- `Scripts/Gameplay/Abilities/DriveVehicles` 仅用于后续敌人或未来桥接研究

## 5.2 JUTPS 中建议冻结，不再扩展

- `Scripts/Inventory System`
- `Scripts/UI`
- `JU Save Load/Scripts`
- `Scripts/Gameplay Settings`
- `Scripts/Scene Management`
- `Scripts/Mobile`
- `Scripts/Cover System`

冻结的意思不是立刻删文件，而是：

- 不把它们纳入正式产品路线
- 不围绕它们继续写业务

## 5.3 JUTPS 中建议从正式场景剔除

- `JUTPS Default User Interface` 及同类 UI prefab
- `FPS Character.prefab`
- `TPS Character.prefab`
- `FirstPerson Camera Controller.prefab`
- `ThirdPerson Camera Controller.prefab`
- 所有 JUTPS 库存 UI
- 所有 JUTPS Save Load 组件

## 5.4 Akila 中建议保留

- `FirstPersonController`
- `CharacterInput`
- `CameraManager`
- `Firearm`
- `MeleeWeapon`
- `Damageable`
- `InteractionsManager`

## 5.5 Akila 中建议冻结，不作为正式方案

- `Inventory`
- `SaveSystem`
- 自带 UI 菜单和 HUD 中不符合项目规范的部分

`Akila` 也不是整包全吃。

## 6. 推荐的桥接方案

## 6.1 玩家桥

建议新增：

- `ProjectXXPlayerFacade`
- `ProjectXXAkilaPlayerBridge`
- `ProjectXXBaseHubPlayerBridge`
- `ProjectXXInputContextBridge`

职责：

- 统一玩家在 Raid / BaseHub 的动作可用域
- 统一玩家第一人称控制的开关
- 对外隐藏 Akila 具体组件细节

## 6.2 JUTPS 世界兼容桥

建议新增：

- `JutpsHealthProxy`
- `JutpsTargetAdapter`
- `JutpsGeneralInteractionBridge`

职责：

- 把 Akila 玩家暴露成 JUTPS 能理解的目标
- 把 JUTPS 伤害同步到 Akila `Damageable`
- 在不启用 JUTPS 输入的前提下，继续使用部分 JUTPS 世界交互

## 6.3 武器与装备桥

建议新增：

- `ProjectXXWeaponBridge`
- `ProjectXXAmmoResolver`
- `ProjectXXWeaponDurabilityBridge`
- `ProjectXXEquipmentBridge`

职责：

- `Akila Firearm` 负责开火表现和手感
- Project-XX 负责武器实例、耐久、稀有度、弹药和修正

## 6.4 敌人桥

建议新增：

- `ProjectXXEnemyAdapter`
- `ProjectXXEnemyHealthBridge`
- `ProjectXXEliteModifierBridge`

职责：

- 让 JUTPS 敌人继续跑动作和 AI
- 让 Project-XX 规则层继续控制精英词缀、Boss 机制、掉落结果

## 7. 第一阶段真正必须做的事

别一上来把所有桥都写完。第一阶段只做最短闭环。

## 7.1 R1 必做

- 用 Akila 玩家替换正式玩家链
- 关闭 JUTPS 玩家 UI 和玩家相机链
- 做 `JutpsHealthProxy`
- 做 `JutpsTargetAdapter`
- 做 `ProjectXXWeaponBridge`
- 跑通一个 JUTPS 近战怪攻击 Akila 玩家
- 跑通一个 Akila 玩家射击击杀 JUTPS 敌人

## 7.2 R1 不做

- 不做 JUTPS 载具接管
- 不做 JUTPS 库存接管
- 不做 JUTPS Pause / UI 接管
- 不做 JUTPS Save Load 接管
- 不做 JUTPS Cover 接管

## 8. 推荐删改顺序

按这个顺序最稳。

1. 在文档层明确玩家主底座改为 Akila。
2. 在测试场景中停用 JUTPS 玩家链和 JUTPS 玩家 UI。
3. 放入 Akila 玩家链并跑通纯玩家移动/开火。
4. 给 Akila 玩家补 `JUTPS` 兼容健康与目标适配。
5. 验证 JUTPS 敌人能发现并伤害 Akila 玩家。
6. 验证 Akila 玩家能击杀 JUTPS 敌人。
7. 再开始接 Project-XX 的规则层和 UI。
8. 最后才决定是否要做载具桥。

## 9. 对现有技术设计的修正

旧表述里最该改的三点是：

- 不再写 “JUTPS 负责第一人称玩家控制”
- 不再写 “JUTPS Weapon 是玩家武器权威”
- 不再写 “桥接组件挂到 JUTPS 玩家 prefab”

新的正确版本应该是：

- `Akila FPS Framework = 玩家第一人称身体、输入、相机、枪械手感`
- `JUTPS = 敌人、AI、世界交互、人形执行层、可延期的载具`
- `Project-XX = 规则、数据、Meta、UI、存档`

## 10. 最终判断

现在最冲突的不是 `JUTPS` 功能太多，而是你原来的文档把它放在了错误的中心位置。

真正该删掉的不是整包，而是三种错误依赖：

- 对 JUTPS 玩家链的依赖
- 对 JUTPS UI/库存/存档的依赖
- 对“玩家必须也是 JUTPS 角色”的依赖

更值钱的是把 `Akila` 定为玩家主链，把 `JUTPS` 压缩成世界与敌人框架，再让 `Project-XX` 自己掌控规则层。
