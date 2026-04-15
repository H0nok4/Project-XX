# Project-XX 开发者入口

更新时间：`2026-04-14`

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

职责：

- 生成/配置玩家
- 生成/配置敌人
- 连接会话运行时
- 连接 HUD 与撤离点

### 玩家桥接

- [ProjectXXPlayerFacade.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXPlayerFacade.cs)
- [ProjectXXAkilaPlayerBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXAkilaPlayerBridge.cs)
- [ProjectXXWeaponBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXWeaponBridge.cs)
- [ProjectXXDamageBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXDamageBridge.cs)
- [ProjectXXFirstPersonViewBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/FPSFramework/ProjectXXFirstPersonViewBridge.cs)

### JUTPS 敌人桥接

- [JutpsHealthProxy.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/JutpsHealthProxy.cs)
- [JutpsTargetAdapter.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/JutpsTargetAdapter.cs)
- [JutpsEnemyDamageableAdapter.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/JutpsEnemyDamageableAdapter.cs)
- [JutpsEnemyBridge.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Bridges/JUTPS/JutpsEnemyBridge.cs)

### 阵营系统

- [ProjectXXFaction.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXFaction.cs)
- [ProjectXXFactionMember.cs](/d:/UnityProject/Project-XX/Project-XX/Assets/Res/Scripts/ProjectXX/Domain/Combat/ProjectXXFactionMember.cs)
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

1. 挂 `ProjectXXFactionMember`
2. 设置 faction
3. 挂 `ProjectXXJutpsFactionBridge`
4. 如果是 JUTPS 角色，保持 `JutpsTargetAdapter` 可用
5. 如果需要受到 Akila 武器伤害，确保有 `JUHealth` 或 `IDamageable` 适配入口

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

1. 优先完成 `R2` 的容器、搜刮、死亡丢失、撤离回写
2. 然后在测试图中补一个友方 NPC 和一个中立 NPC，验证 faction 框架的内容层表现
3. 最后再把更复杂的 NPC 遭遇、营地和精英/Boss 行为放到 `R4`
