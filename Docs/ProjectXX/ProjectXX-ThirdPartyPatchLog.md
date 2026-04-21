# Project-XX 第三方补丁台账

更新时间：`2026-04-22`

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
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/JUTPS.Editor.asmdef` | 为 vendor 正式 editor 脚本建立独立程序集边界，并显式引用 `JUTPS.Runtime` | 把 JUTPS 正式 editor 目录从 `Assembly-CSharp-Editor` 中收束出来，让 editor/runtime 边界成对落地 | Retain | 后续继续压缩 runtime 内残余 editor helper |
| `Assets/Julhiecio TPS Controller/Scripts/Libraries/Editor Libs/JUHeaderPropertyDecorator.cs` | 仅保留 `JUHeader` / `JUSubHeader` / `JUReadOnly` / `JUButton` 的 runtime attribute 定义 | 把运行时 attribute 与 editor drawer 职责拆开，避免继续把 editor drawer 混在 runtime vendor 文件里 | Retain | 如继续清理 editor helper，可再评估目录归位 |
| `Assets/Julhiecio TPS Controller/Editor/Editor Scripts/JUHeaderPropertyDrawers.cs` | 新增 editor-only property drawers，接管 `JUHeader` 系列 inspector 绘制 | 让 `JUHeader` 的 editor 行为落回 `JUTPS.Editor` 正式程序集，而不是继续混在 runtime vendor 文件里 | Retain | 与 `JUTPS.Editor.asmdef` 一起保留 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/Additionals/JUHealth.cs` | 伤害入口改为优先委托 `ProjectXXCombatant`，无 combatant 时再走旧逻辑，并补事件初始化保护 | 开始收敛双生命系统，让 JUHealth 退成执行入口/表现镜像 | Internalize | 长期应由 Project-XX combat state 取代 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Character Controllers/Additionals/DamageableBodyPart.cs` | body part 层级补同样的 faction gate | 防止部位伤害绕过规则 | Temporary | 跟随 combat state 收敛 |
| `Assets/Julhiecio TPS Controller/Scripts/Gameplay/Weapon Management/Damager.cs` | 前置拒绝同阵营命中，避免继续走命中特效链 | 解决“看起来被打中了但规则上不该生效” | Temporary | 长期若 JUTPS 攻击链被正式收编，可改为调用 Project-XX combat API |
| `Assets/Julhiecio TPS Controller/Scripts/AI/Sensors/JUFieldOfViewSensor.cs` | 改为依赖正式 combat target filter 契约 `IProjectXXTargetFilter`，不再直接绑定 bridge 组件类型 | 让 JUTPS 感知链服从 faction 规则，同时去掉 vendor 对 bridge 层的反向依赖 | Internalize | 长期应继续减少对 Tag/Layer 的依赖，并最终由 Project-XX 正式感知规则接管 |
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
2. 再决定 `JUHealth / Damageable / JutpsHealthProxy / JutpsEnemyDamageableAdapter` 的最终去留
3. 然后把本台账中的 `Temporary` 补丁逐步消解
