# Project-XX 第三方补丁台账

更新时间：`2026-04-23`

本文记录当前对 Akila / JUTPS 的实际代码补丁。  
目标不是“避免改 vendor”，而是确保每一处改动都可追踪、可评估、可决定去留。

状态约定：

- `Temporary`：迁移期补丁，未来应拆掉
- `Retain`：预计长期保留
- `Internalize`：应被收编为 Project-XX 正式实现
- `Delete`：应在收编后删除或替换

## 1. Akila FPS Framework

| 文件 | 变更内容 | 原因 | 当前状态 | 长期方向 |
| --- | --- | --- | --- | --- |
| `Assets/Akila/FPS Framework/Scripts/Akila.FPSFramework.asmdef` | 引入 `ProjectXX.Domain.Combat` 引用 | 让 Akila 入口可访问 faction 规则 | Temporary | 等 ProjectXX asmdef 体系稳定后重新评估依赖方向 |
| `Assets/Akila/FPS Framework/Scripts/Character/Damageable.cs` | 伤害入口改为优先委托 `ProjectXXCombatant`，无 combatant 时再走旧逻辑 | 开始收敛双生命系统，让 Akila `Damageable` 退成执行入口/表现镜像 | Internalize | 长期应只保留 Akila 执行层角色 |
| `Assets/Akila/FPS Framework/Scripts/Character/MeleeWeapon.cs` | 近战伤害传入真实 damage source | 避免近战无法正确识别来源阵营 | Retain | 若近战执行链被完全收编，可迁出 |
| `Assets/Akila/FPS Framework/Scripts/Character/Firearm System/Projectile.cs` | 同步 projectile 的命中层与 decal 配置 | 修复 projectile 使用旧 prefab 配置的问题 | Retain | 可保留，只要 Akila projectile 仍被采用 |

## 2. JUTPS

| 文件 | 变更内容 | 原因 | 当前状态 | 长期方向 |
| --- | --- | --- | --- | --- |
| `Assets/Julhiecio TPS Controller/JUTPS.Runtime.asmdef` | 为 vendor runtime 建立正式程序集边界，并显式引用 `ProjectXX.Domain.Combat` 与 Input System | 把 JUTPS 运行时代码从 `Assembly-CSharp` 中拉出，给桥接层和后续 editor 拆分建立健康边界 | Retain | 继续补齐 editor 侧程序集治理 |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/JUTPS.Editor.asmdef` | 为 vendor 正式 editor 脚本建立独立程序集边界，并显式引用 `JUTPS.Runtime` 与 `Unity.InputSystem` | 把 JUTPS 正式 editor 目录从 `Assembly-CSharp-Editor` 中收束出来，让 editor/runtime 边界成对落地，并承接输入资产相关 editor 逻辑 | Retain | 后续继续压缩 runtime 内残余 editor helper |
| `Assets/Julhiecio TPS Controller/Scripts/Libraries/Editor Libs/JUHeaderPropertyDecorator.cs` | 仅保留 `JUHeader` / `JUSubHeader` / `JUReadOnly` / `JUButton` 的 runtime attribute 定义 | 把运行时 attribute 与 editor drawer 职责拆开，避免继续把 editor drawer 混在 runtime vendor 文件里 | Retain | 如继续清理 editor helper，可再评估目录归位 |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/JUHeaderPropertyDrawers.cs` | 新增 editor-only property drawers，接管 `JUHeader` 系列 inspector 绘制 | 让 `JUHeader` 的 editor 行为落回 `JUTPS.Editor` 正式程序集，而不是继续混在 runtime vendor 文件里 | Retain | 与 `JUTPS.Editor.asmdef` 一起保留 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/Additionals/DamageableBody.cs`、`Assets/Julhiecio TPS Controller/Scripts/Mobile/Mobile Input/MobileRig.cs`、`Assets/Julhiecio TPS Controller/Scripts/Tools Components/FractureTool.cs`、`Assets/Julhiecio TPS Controller/JU Foot Placement/Scripts/Foot Placement/JUFootPlacement.cs` | 移除内嵌 `CustomEditor`，把 runtime 文件重新收敛回组件/工具本体；其中 `MobileRig.FindButtonsAndTouches()` 改为显式 editor 可调用入口；后续继续把 `JUFootPlacement` 的 `Handles/Gizmos` scene 可视化也抽离到 editor 侧 | 继续收紧 JUTPS runtime/editor 边界，避免 vendor runtime 文件长期混带 inspector 与 scene debug 逻辑 | Retain | 后续继续处理残余 `Handles / AssetDatabase / SceneView / scene debug` 混装点 |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Components Editor/DamageableBodyEditor.cs`、`Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Components Editor/MobileRigEditor.cs`、`Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Components Editor/FractureToolEditor.cs`、`Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Components Editor/JUFootPlacementEditor.cs` | 为上述 runtime 组件补独立 editor inspector 文件；其中 `JUFootPlacementEditor.cs` 继续接管 `JUFootPlacement` 与 `JUSlipCapsule` 的 scene gizmo/handles 可视化 | 让 inspector 与 scene 可视化职责明确落在 `JUTPS.Editor`，不再依赖 runtime 文件混装 | Retain | 与 `JUTPS.Editor.asmdef` 一起保留 |
| `Assets/Julhiecio TPS Controller/Scripts/JU/JUBoxArea.cs`、`Assets/Julhiecio TPS Controller/Scripts/Physics/Vehicle Physics/JUVehicleInputAsset.cs` | 移除内嵌 `MenuItem / AssetDatabase` 资产创建逻辑，把 runtime 文件重新收敛回数据/组件本体 | 继续削掉 runtime vendor 文件里的 editor 入口，避免正式运行时代码继续定义菜单与资产创建职责 | Retain | 后续继续处理残余 `Handles / AssetDatabase / SceneView / EditorApplication` 混装点 |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Create Functions/JUBoxAreaCreateMenu.cs`、`Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Create Functions/JUVehicleInputAssetCreateMenu.cs` | 为上述 runtime 类型补独立 editor 菜单与资产创建入口 | 让菜单创建与输入资产生成职责明确落在 `JUTPS.Editor` | Retain | 与 `JUTPS.Editor.asmdef` 一起保留 |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/JUIconGenerator.cs` | 把整文件 editor-only 的 `JUIconGenerator` 从 runtime tools 目录迁入 `JUTPS.Editor` | 该工具没有 runtime 引用，直接迁入 editor 程序集比继续保留在 runtime 路径更符合边界治理 | Retain | 后续继续筛查可整块迁出的 editor-only vendor 文件 |
| `Assets/Julhiecio TPS Controller/Scripts/Inputs/JUPlayerCharacterInputAsset.cs`、`Assets/Julhiecio TPS Controller/Scripts/Utilities/JUPauseGame.cs`、`Assets/Julhiecio TPS Controller/Scripts/Libraries/Vehicle System Libs/JUVehicleEngine.cs` | 移除对 `UnityEditor.EditorApplication.playModeStateChanged` 的依赖，改由对象生命周期清理输入/暂停/车辆运行时状态 | 让 runtime 文件不再通过 editor play mode 事件维持状态复位，继续削减 vendor runtime 中的 editor callback | Retain | 后续剩余 playmode callback 主要收敛到 `JU Save Load` 侧 |
| `Assets/Julhiecio TPS Controller/JU Save Load/Scripts/JUSaveLoadComponent.cs` | 移除对 `UnityEditor.EditorApplication.playModeStateChanged` 的依赖，改由 `OnApplicationQuit / OnDestroy` 退出清理缓存与保存注册 | 让 `JU Save Load` 基类不再依赖 editor play mode 事件维持退出清理，同时避免退出流程中重复保存 | Retain | 后续仍可继续清理 `JU Save Load` 侧剩余 editor-only 痕迹 |
| `Assets/Julhiecio TPS Controller/JU Save Load/Scripts/JUSaveLoad.cs` | 新增 `SaveRootDirectory` 与 `ResetSaveRootDirectory()`，默认仍使用 `Application.persistentDataPath` | 让 ProjectXX 后续可以接管测试/平台保存根目录，而不是继续把路径策略锁死在 vendor 内部 | Retain | 后续正式持久化系统落地后，可把 JU Save Load 降级为兼容入口 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay Settings/JUApplyAudioVolumeSettings.cs`、`Assets/Julhiecio TPS Controller/Scripts/Gameplay/Interaction System/JUInteractionSystem.cs` | 移除 runtime `Reset()` 内的 `AssetDatabase.LoadAssetAtPath(...)` 默认资源绑定 | 让 runtime 组件只保留对象默认值，不再直接访问 editor 资源库 | Retain | 默认资源绑定已迁到 editor hook |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Help Tab/ThankYouMessage.cs` | 在既有 editor 文件中新增 `JUTPSRuntimeComponentDefaults` 组件默认资源绑定入口，负责给 `JUApplyAudioVolumeSettings / JUInteractionSystem` 自动补默认资源 | 把 component authoring convenience 收敛到 `JUTPS.Editor`，避免继续把 `AssetDatabase` 塞回 runtime 文件 | Retain | 可继续扩展接管其他 runtime `Reset()` 里的 editor 资源绑定 |
| `Assets/Julhiecio TPS Controller/Scripts/Tools Components/FractureTool.cs` | 移除 runtime 文件中的 `AssetDatabase / FileUtil / MeshUtility` 资产保存逻辑，仅保留 fracture 工具本体与生成结果引用 | 继续把 editor 资产保存职责从 runtime vendor 文件中剥离 | Retain | 资产保存链已迁到 editor utility |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Components Editor/FractureToolEditor.cs` | 在既有 editor 文件中新增 `FractureToolAssetUtility`，接管生成 fracture mesh 资产的目录创建、优化与写盘 | 让 fracture 资产保存职责明确落在 `JUTPS.Editor`，并避免等待 Unity 刷新工程文件后再分裂出第二个 editor helper 文件 | Retain | 与 `FractureToolEditor.cs` 同文件保留 |
| `Assets/Julhiecio TPS Controller/Scripts/Effects/JUFootstep.cs` | 移除 runtime 文件中的 `ContextMenu / AssetDatabase` 默认脚步音频装配逻辑 | 让 `JUFootstep` 回到纯运行时表现，不再直接触达 editor 资产数据库 | Retain | 默认脚步资源装配已转交 `JUTPS.Editor` 的 context menu |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Components Editor/FractureToolEditor.cs` | 在既有 editor 文件中继续新增 `JUFootstepDefaultsMenu`，接管 `JUFootstep` 默认脚步音频的 context menu 资源装配 | 把脚步默认资源 authoring 职责收回 `JUTPS.Editor`，避免再把 `AssetDatabase` 塞回 runtime | Retain | 暂与 `FractureToolEditor.cs` 同文件保留，待后续统一整理 editor helper |
| `Assets/Julhiecio TPS Controller/Scripts/Editor Library/JUEditor.cs`、`Assets/Julhiecio TPS Controller/Editor/Editor Scripts/Create Functions/JUBoxAreaCreateMenu.cs` | 从 runtime `JUEditor` 移除仅供 editor 创建菜单使用的 `SceneView` 生成辅助，并把逻辑本地化到 `JUBoxAreaCreateMenu` | 让 `JUEditor` 只保留 `IsGameFocused` 这类运行时兼容入口，继续压缩 `JUTPS.Runtime` 中的 SceneView editor helper | Retain | 后续继续清理其余 `gizmo / handles / scene debug` 混装点 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/Additionals/JUSlipCapsule.cs` | 移除 runtime 文件中的 `UnityEditor / Handles` slip capsule 可视化逻辑 | 让 `JUSlipCapsule` 回到纯运行时碰撞职责，不再直接携带 editor scene 绘制代码 | Retain | scene 可视化已转交 `JUTPS.Editor` |
| `Assets/Julhiecio TPS Controller/Scripts/AI/Sensors/JUFieldOfViewSensor.cs` | 去掉 `InternalEditorUtility.tags` 与 `UnityEditor.Handles` 依赖：默认 target tag/layer 装配改为 runtime-safe 逻辑，视锥绘制改为通用 gizmo 线框扇形，并把 tag 过滤改为安全字符串比较 | 让 JUTPS FOV 默认配置与 scene debug 不再依赖 editor API，继续压缩 vendor runtime 中的 editor 混装点 | Internalize | 长期仍应继续减少对 Tag/Layer 的依赖，并最终由 Project-XX 正式感知规则接管 |
| `Assets/Julhiecio TPS Controller/Scripts/AI/Actions/Escape/Escape.cs` | 将 escape area 的 selected gizmo 从 `UnityEditor.Handles.DrawWireCube` 收敛为通用 `Gizmos.DrawWireCube` | 让 Escape 的场景调试绘制退出 editor API 依赖，同时保留现有 runtime debug 入口 | Retain | 后续仍可继续清理其余 AI action debug 混装点 |
| `Assets/Julhiecio TPS Controller/Scripts/AI/JU_AI_PatrolCharacter.cs`、`Assets/Julhiecio TPS Controller/Scripts/AI/JU_AI_Zombie.cs` | 移除 `SceneView.currentDrawingSceneView + Handles.Label` 选中调试浮字，仅保留 FOV 与当前 action 的 gizmo/debug 绘制 | 让 AI 角色选中调试退出 editor API 依赖，避免继续为纯调试标签保留 runtime 的 editor 混装 | Retain | 后续仍可继续收敛 AI debug 体系 |
| `Assets/Julhiecio TPS Controller/Scripts/Cover System/JUCoverTrigger.cs`、`Assets/Julhiecio TPS Controller/Scripts/Physics/Gravity Switch Physics/GravityBox.cs` | 将 `Handles.ArrowHandleCap` 方向提示收敛为通用 `Gizmos.DrawRay` | 让 cover / gravity 方向调试不再依赖 editor 箭头句柄，同时保留基础空间提示 | Retain | 属于 runtime scene helper 去 editor 化 |
| `Assets/Julhiecio TPS Controller/Scripts/Effects/BodyLeanInert.cs`、`Assets/Julhiecio TPS Controller/Scripts/AI/VehicleAI.cs`、`Assets/Julhiecio TPS Controller/Scripts/Libraries/Vehicle System Libs/JUVehicleEngine.cs` | 将 body lean / vehicle scene debug 中残余的 `Handles.Label / DrawWireArc / ArrowHandleCap` 收敛为通用 `Gizmos` 线段、线框球与目标点标记 | 让 body / vehicle runtime helper 继续退出 editor 句柄依赖，同时保留基础调试可视化 | Retain | 属于 runtime scene helper 去 editor 化 |
| `Assets/Julhiecio TPS Controller/Scripts/Tools Components/JUGizmoDrawer.cs`、`Assets/Julhiecio TPS Controller/Scripts/Physics/Character Physics/AdvancedRagdollController.cs`、`Assets/Julhiecio TPS Controller/Scripts/Gameplay/Weapon Management/Weapon.cs` | 将 gizmo mesh、ragdoll bone/body direction 与 weapon authoring crosshair 预览统一收敛为 `UnityEngine.Gizmos`，移除 runtime 对 `UnityEditor / Handles` 的直接依赖 | 继续压缩 weapon / body authoring 调试混装，让 runtime 文件只保留通用 gizmo 表达 | Retain | 下一步转向清理 runtime-path editor helper 与 `JU Save Load` 路径策略 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/Additionals/JUHealth.cs` | 伤害入口改为优先委托 `ProjectXXCombatant`，无 combatant 时再走旧逻辑，并补事件初始化保护 | 开始收敛双生命系统，让 JUHealth 退成执行入口/表现镜像 | Internalize | 长期应由 Project-XX combat state 取代 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/Additionals/DamageableBodyPart.cs` | body part 层级补同样的 faction gate | 防止部位伤害绕过规则 | Temporary | 跟随 combat state 收敛 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Weapon Management/Damager.cs` | 前置拒绝同阵营命中，避免继续走命中特效链 | 解决“看起来被打中了但规则上不该生效” | Temporary | 长期若 JUTPS 攻击链被正式收编，可改为调用 Project-XX combat API |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/JUCharacterController.cs` | 物理伤害忽略 tag 遍历增加防御性保护 | 避免缺失 tag 时运行时刷错 | Retain | 即使后续集中校验仍可保留为底层容错 |

## 3. 评估规则

每次新增 vendor 修改，都必须回答：

1. 这是不是底层执行入口，无法在 Project-XX 层优雅拦截？
2. 这是迁移期补丁，还是正式会长期保留的改动？
3. 这个改动未来更适合 `Retain`、`Internalize`、还是 `Delete`？
4. 这处补丁是否意味着某个系统的正式所有权还没选主？

## 4. 当前建议

当前最需要优先处理的，不是“继续新增补丁”，而是：

1. 已完成第一阶段：`ProjectXXCombatant` 已拆成独立正式 combat state，`ProjectXXCombatantSync` 已退成单向兼容镜像器
2. 再决定 `JUHealth / Damageable / JutpsEnemyDamageableAdapter` 的最终去留；`JutpsHealthProxy` 已从当前桥接路径删除
3. 然后把本台账中的 `Temporary` 补丁逐步消解
