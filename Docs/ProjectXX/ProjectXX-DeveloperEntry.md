# Project-XX 开发者入口

更新时间：`2026-04-25`

预制作框架整改建议请同时参考：

- [AGENT.md](/d:/UnityProject/Project-XX/Project-XX/AGENT.md)
- [ProjectXX-FrameworkHealthPlan.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-FrameworkHealthPlan.md)
- [ProjectXX-SystemOwnershipMatrix.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-SystemOwnershipMatrix.md)
- [ProjectXX-ThirdPartyPatchLog.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-ThirdPartyPatchLog.md)
- [ProjectXX-VendorOwnershipMatrix.md](/d:/UnityProject/Project-XX/Project-XX/Docs/ProjectXX/ProjectXX-VendorOwnershipMatrix.md)

## 当前可运行入口

- `Assets/Scenes/ProjectXX/ProjectXX_Bootstrap.unity`
- `Assets/Scenes/ProjectXX/ProjectXX_RaidTestMap.unity`

当前 Build Settings 已把以上两张场景放在前两位。正常启动流程是：

`Bootstrap -> RaidTestMap`

## 当前测试切片内容

当前可运行的是一套 `R1` 战斗切片：

- Akila 第一人称玩家
- JUTPS Zombie 敌人
- Project-XX HUD
- 最小撤离点
- 基础阵营与敌对关系框架

已验证的行为包括：

- 玩家移动、瞄准、开火、近战、受伤、死亡
- 敌人发现、追击、近战攻击
- 玩家击杀敌人
- 敌人不再互相伤害
- 中立单位受伤后会把来源阵营视为敌对

## 关键入口脚本

### 场景安装

- [ProjectXXRaidSceneInstaller.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bootstrap/ProjectXXRaidSceneInstaller.cs)
- [ProjectXXRaidRuntimeRegistry.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bootstrap/ProjectXXRaidRuntimeRegistry.cs)
- [ProjectXXCompatibilityValidator.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bootstrap/ProjectXXCompatibilityValidator.cs)

职责：

- 建立 Raid composition root
- 生成/配置玩家
- 生成/配置敌人
- 注册 `Session / Player / HUD / Extraction / Enemies`
- 把运行时对象关系从全局查找收敛为显式注入

### 玩家桥接

- [ProjectXXPlayerFacade.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXPlayerFacade.cs)
- [ProjectXXAkilaPlayerBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXAkilaPlayerBridge.cs)
- [ProjectXXEquipmentBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXEquipmentBridge.cs)
- [ProjectXXWeaponBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXWeaponBridge.cs)
- [ProjectXXDamageBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXDamageBridge.cs)
- [ProjectXXFirstPersonViewBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXFirstPersonViewBridge.cs)

### 物品与装备语义

- [ProjectXXItemDefinition.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXItemDefinition.cs)
- [ProjectXXEquippableItemDefinition.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXEquippableItemDefinition.cs)
- [ProjectXXItemInstanceRuntime.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXItemInstanceRuntime.cs)
- [ProjectXXInventoryGridRuntime.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXInventoryGridRuntime.cs)
- [ProjectXXInventoryTransferUtility.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXInventoryTransferUtility.cs)
- [ProjectXXContainerDefinition.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXContainerDefinition.cs)
- [ProjectXXContainerRuntime.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXContainerRuntime.cs)
- [ProjectXXEquipmentRuntime.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXEquipmentRuntime.cs)
- [ProjectXXLoadoutRuntime.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory/ProjectXXLoadoutRuntime.cs)
- [StarterAssaultRifle.asset](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Data/Definitions/Equipment/StarterAssaultRifle.asset)
- [SmallLootCrate.asset](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Data/Definitions/Loot/SmallLootCrate.asset)
- [FieldRations.asset](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Data/Definitions/Loot/FieldRations.asset)

职责：

- 让 Project-XX 拥有正式物品/装备定义
- 让 Project-XX 拥有第一版容器格子、物品实例、背包与配装运行时
- 让 Akila `InventoryItem` 只作为第一人称武器表现 prefab 绑定
- 为 R2 的搜刮、死亡丢失与撤离回写预留正式 ownership

### R2 交互与容器入口

- [IProjectXXInteractable.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Interaction/IProjectXXInteractable.cs)
- [ProjectXXInteractionResult.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Interaction/ProjectXXInteractionResult.cs)
- [ProjectXXContainerInteractable.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Meta/Interaction/ProjectXXContainerInteractable.cs)
- [ProjectXXPlayerInteractionBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Presentation/Raid/ProjectXXPlayerInteractionBridge.cs)
- [ProjectXXLootWindowController.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Presentation/Raid/ProjectXXLootWindowController.cs)

职责：

- 让 Project-XX 拥有正式交互契约，而不是继续外扩 Akila/JUTPS 的交互系统
- 让玩家通过相机射线发现 `IProjectXXInteractable`，并把提示写入 `RaidSessionRuntime`
- 让 HUD 通过 Session 统一显示交互提示，后续容器、门、商人、撤离终端可复用同一通道
- 让 `ProjectXXLootWindowController` 承接第一版容器窗口、背包清单、键盘 first-fit 拿取/放回与关闭流程
- 当前仍不是正式格子 UI；拖拽、交换、堆叠拆分与可视格模板仍属于后续 R2-D

### Combat State

- [ProjectXXCombatant.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXCombatant.cs)
- [ProjectXXCombatantSync.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/Combat/ProjectXXCombatantSync.cs)

### Compatibility

- [ProjectXXCompatibilitySettings.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Infrastructure/Definitions/ProjectXXCompatibilitySettings.cs)
- [ProjectXXCompatibilitySettingsProvider.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Foundation/ProjectXXCompatibilitySettingsProvider.cs)
- [ProjectXXCompatibilitySettings.asset](/d:/UnityProject/Project-XX/Project-XX/Assets/Resources/ProjectXXCompatibilitySettings.asset)

### JUTPS 敌人桥接

- [JutpsTargetAdapter.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/JutpsTargetAdapter.cs)
- [JutpsEnemyDamageableAdapter.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/JutpsEnemyDamageableAdapter.cs)
- [JutpsEnemyBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/JutpsEnemyBridge.cs)

### 战斗与阵营系统

- [ProjectXXFaction.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXFaction.cs)
- [ProjectXXFactionMember.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXFactionMember.cs)
- [ProjectXXCombatant.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXCombatant.cs)
- [ProjectXXFactionUtility.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXFactionUtility.cs)
- [ProjectXXJutpsFactionBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/ProjectXXJutpsFactionBridge.cs)
- [ProjectXXJutpsFactionTargetFilter.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/ProjectXXJutpsFactionTargetFilter.cs)

## 当前目录约定

代码目录：

- `Assets/Res/Scripts/ProjectXX/Bootstrap`
- `Assets/Res/Scripts/ProjectXX/Foundation`
- `Assets/Res/Scripts/ProjectXX/Domain/Combat`
- `Assets/Res/Scripts/ProjectXX/Domain/Raid`
- `Assets/Res/Scripts/ProjectXX/Domain/Meta`
- `Assets/Res/Scripts/ProjectXX/Domain/Meta/Inventory`
- `Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework`
- `Assets/Res/Scripts/ProjectXX/Bridges/JUTPS`
- `Assets/Res/Scripts/ProjectXX/Infrastructure/Definitions`
- `Assets/Res/Scripts/ProjectXX/Presentation`

数据目录：

- `Assets/Res/Data/Definitions/*`
- `Assets/Resources/ProjectXXCompatibilitySettings.asset`

## 当前程序集边界

已经拆出的 ProjectXX 关键程序集：

- `ProjectXX.Domain.Combat.asmdef`
- `ProjectXX.Domain.Meta.asmdef`
- `ProjectXX.Domain.Raid.asmdef`
- `ProjectXX.Infrastructure.Definitions.asmdef`
- `ProjectXX.Foundation.asmdef`
- `ProjectXX.Services.asmdef`
- `ProjectXX.UI.asmdef`
- `ProjectXX.UI.Editor.asmdef`
- `ProjectXX.Bridges.FPSFramework.asmdef`
- `ProjectXX.Bridges.Combat.asmdef`
- `ProjectXX.Bridges.JUTPS.asmdef`
- `ProjectXX.Presentation.Raid.asmdef`
- `ProjectXX.Bootstrap.asmdef`
- `JUTPS.Runtime.asmdef`
- `JUTPS.Editor.asmdef`

当前仍暂留 `Assembly-CSharp` 的 ProjectXX 模块：

- 无新增正式 runtime 模块

暂未继续硬拆的原因：

- `JUTPS` 的正式 editor 脚本已落入 `JUTPS.Editor.asmdef`，且 `DamageableBody / MobileRig / FractureTool / JUFootPlacement` 的内嵌 inspector、`JUBoxArea / JUVehicleInputAsset` 的菜单创建逻辑、`JUIconGenerator` 的整文件 editor-only 工具、`FractureTool` 的资产保存逻辑、`JUInteractionSystem / JUApplyAudioVolumeSettings` 的默认资源绑定逻辑，以及 `JU Save Load` 基类的 editor playmode callback 都已被抽离或替换；上一轮继续把 `JUFootstep` 默认脚步音频装配迁到 editor context menu，并把 `JUEditor` 中仅供 `JUBoxArea` 创建菜单使用的 `SceneView` helper 移回 editor 侧；随后又把 `JUFootPlacement / JUSlipCapsule` 的 scene 可视化迁到 `JUTPS.Editor`，并把 `JUFieldOfViewSensor / Escape` 的残余 editor API 收敛成 runtime-safe 配置与通用 gizmo 绘制，同时移除 `JU_AI_PatrolCharacter / JU_AI_Zombie` 的 editor 调试浮字与 `JUCoverTrigger / GravityBox` 的 editor 箭头句柄；本轮继续把 `BodyLeanInert / VehicleAI / JUVehicleEngine / JUGizmoDrawer / AdvancedRagdollController / Weapon` 的 scene/authoring debug 收敛为通用 `Gizmos`，把 ProjectXX runtime 脚本中的 `UnityEditor` 直接引用清掉，并给 `JUSaveLoad` 增加可覆盖的保存根目录；当前 JUTPS runtime 内直接 editor API 残留已主要收缩到 `JUEditor / UnityEditorUtilities`
- ProjectXX 当前缺的已经不是 runtime asmdef，而是后续 editor/test 层的长期边界治理
- `Assembly-CSharp` 里剩下的主要是非 ProjectXX 代码与 Unity 自动生成工程，不再是 ProjectXX 正式 runtime 主体

## 阵营系统使用约定

当前长期使用的阵营枚举为：

- `Player`
- `FriendlyNpc`
- `NeutralNpc`
- `Enemy`

当前规则：

- 同阵营不可互伤
- `Player` 与 `FriendlyNpc` 互为友方
- `Enemy` 默认敌对 `Player` 与 `FriendlyNpc`
- `NeutralNpc` 默认中立，但在受伤后会把伤害来源阵营加入运行时敌对列表

如果要新增一个可战斗 NPC，建议至少完成以下挂接：

1. 先挂 `ProjectXXCombatant`
2. 再挂 `ProjectXXFactionMember`
3. 设置 faction
4. 挂 `ProjectXXJutpsFactionBridge`
5. 如果是 JUTPS 角色，保持 `JutpsTargetAdapter` 可用
6. 如果需要兼容 Akila / JUTPS 伤害入口，保留 `JUHealth` 或 `IDamageable` 适配入口

## 当前已知的阶段边界

当前有的是：

- 战斗切片
- 阵营框架
- 会话状态与基础 HUD
- 第一版 Project-XX 物品/装备定义、容器格子、`EquipmentRuntime` 与 `LoadoutRuntime`
- 第一版 Project-XX 容器交互契约、玩家交互桥与 HUD 提示链
- 第一版搜刮窗口与键盘驱动的容器/背包 first-fit 转移

当前还没有的是：

- 正式格子化背包/容器 UI、拖拽、交换与堆叠拆分
- 正式局外仓库
- 正式友方/中立 NPC 内容样例
- 精英/Boss 遭遇

## 下一步建议

最推荐的顺序是：

1. 继续收缩 `JUHealth / Damageable / JutpsEnemyDamageableAdapter` 的迁移保留范围；`JutpsHealthProxy` 已从当前桥接入口中移除
2. 把剩余 `FindFirstObjectByType / FindObjectsByType` 继续压缩到 composition root 内，并优先使用 `ProjectXXRaidSceneInstaller` 上显式 authoring 的玩家、HUD、敌人和 Akila manager 引用
3. 基于已落地的 `ProjectXXLootWindowController / ProjectXXInventoryTransferUtility` 继续把文字清单升级成格子 UI、拖拽交换与死亡/撤离结算
4. 继续把兼容规则从组件局部字段收敛到 `ProjectXXCompatibilitySettings`
5. 继续清理 `JUEditor / UnityEditorUtilities` 这类仍位于 runtime 路径的 editor helper，并规划 ProjectXX 自己的 editor/test 边界；`JU Save Load` 已具备第一版可配置保存根目录
