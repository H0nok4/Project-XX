# Project-XX 开发者入口

更新时间：`2026-04-22`

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
- [ProjectXXWeaponBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXWeaponBridge.cs)
- [ProjectXXDamageBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXDamageBridge.cs)
- [ProjectXXFirstPersonViewBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXFirstPersonViewBridge.cs)

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
- [JutpsHealthProxy.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/JutpsHealthProxy.cs)

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

- `JUTPS` 的正式 editor 脚本已落入 `JUTPS.Editor.asmdef`，但 runtime 内仍残留少量 editor helper / gizmo / handles 混装代码
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

当前还没有的是：

- 正式容器与背包
- 正式局外仓库
- 正式友方/中立 NPC 内容样例
- 精英/Boss 遭遇

## 下一步建议

最推荐的顺序是：

1. 继续收缩 `JUHealth / Damageable / JutpsHealthProxy / JutpsEnemyDamageableAdapter` 的迁移保留范围
2. 把剩余 `FindFirstObjectByType / FindObjectsByType` 压缩到 composition root 以内
3. 继续把兼容规则从组件局部字段收敛到 `ProjectXXCompatibilitySettings`
4. 继续清理 `JUTPS.Runtime` 内残余 editor helper 混装点，再规划 ProjectXX 自己的 editor/test 边界
