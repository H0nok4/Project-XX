# Project-XX Vendor Ownership Matrix

更新时间：`2026-04-22`

本文用于明确 Akila / JUTPS 各子系统在 Project-XX 中的命运：

- `Retain`
- `Internalize`
- `Disable`
- `Delete`

## 1. 状态说明

- `Retain`：保留为长期依赖能力
- `Internalize`：保留能力，但逐步收编为 Project-XX 正式实现
- `Disable`：不作为正式产品链路，但暂不急于物理删除
- `Delete`：建议从主工程路径移除或清理

## 2. Akila FPS Framework

| 模块 | 状态 | 理由 | 备注 |
| --- | --- | --- | --- |
| 第一人称玩家控制 | Retain | 当前是最合适的玩家执行底座 | 正式 owner 仍是 Akila 执行层 |
| 第一人称相机 / 视图动画 / 武器表现 | Retain | 当前价值高，替换成本大 | 需把修正规则固化进正式 prefab |
| Firearm / Projectile 执行链 | Retain | 当前战斗切片依赖它 | 规则约束应由 Project-XX 提供 |
| Melee 执行链 | Internalize | 当前实现偏原型，不够正式 | 可保留表现，但规则和状态应迁出 |
| Damageable / Health 语义 | Internalize | 不能长期和 JUHealth 双活，当前已降级为 `ProjectXXCombatant` 兼容端点 | 最终应由 Project-XX combat state 取代 authoritative source |
| Inventory / InventoryItem / ItemInput 作为正式物品系统 | Disable | 与 Project-XX 未来容器/装备系统冲突 | 只能保留为执行层临时绑定，不得成为正式 owner |
| UIManager / PlayerCard / KillFeed / 默认运行时 UI | Disable | 不进入正式产品路径 | Project-XX UI 将接管 |
| Menu / Loading / Settings / 通用演示菜单链 | Disable | 与正式产品路径不一致 | 可后续集中清理 |
| Demo 场景与演示资源入口 | Delete | 只制造认知噪音 | 保留原包备份即可 |

## 3. JUTPS

| 模块 | 状态 | 理由 | 备注 |
| --- | --- | --- | --- |
| AI 导航 / locomotion / 基础攻击动画骨架 | Retain | 当前是最有价值的敌人执行能力 | 可继续作为敌人执行底座 |
| FieldOfView / 目标选择 | Internalize | 当前仍强依赖 Tag/Layer，但已改为通过 `IProjectXXTargetFilter` 消费正式目标过滤契约，而不是反向依赖 bridge 类型 | 应逐步收编为 Project-XX 正式目标规则驱动 |
| JUHealth / Damage / BodyPart | Internalize | 不适合长期与 Project-XX 正式战斗状态双活，当前已降级为 `ProjectXXCombatant` 兼容端点 | 过渡期可保留入口，长期应降级 |
| JUTPS editor tooling / custom inspectors | Retain | 当前仍需要保留 JUTPS 自身 editor 工具，但已通过 `JUTPS.Editor.asmdef` 与 runtime 分边界 | 后续继续清理 runtime 内残余 editor helper 混装 |
| JUTPS 玩家控制链 | Delete | 已不符合项目第一人称正式方向 | 不应继续保留在主路径 |
| JUTPS 默认玩家 UI | Delete | 不进入正式产品 | 可直接视为废弃路径 |
| JUTPS Inventory / Weapon / 玩家相关扩展链 | Delete | 与正式架构冲突 | 不应继续参与产品路径 |
| Vehicle 系统 | Retain | 后续仍可能需要 | 暂不作为当前主线 |
| Ragdoll / Character Physics | Retain | 有明确执行价值 | 可继续保留 |
| Demo 场景 / demo-only 入口 | Delete | 只增加噪音 | 建议逐步移除主工程依赖感 |

当前边界补充：

- `JUTPS.Runtime.asmdef` 已落地，vendor 运行时代码已正式退出 `Assembly-CSharp`
- `JUTPS.Editor.asmdef` 已落地，vendor 正式 editor 脚本已从 `Assembly-CSharp-Editor` 中收束出来
- 剩余问题主要是 runtime 内仍混有少量 editor helper / gizmo / handles 代码，属于下一阶段整改项

## 4. 当前最重要的治理结论

### 4.1 不应该再继续保留的幻觉

以下想法在当前项目里都不应该继续保留：

- “也许以后 JUTPS 玩家链还能用”
- “Akila Inventory 也许可以顺手扩成正式背包”
- “vendor 默认 UI 也许之后能改一改继续用”

这些想法会直接拖慢系统选主。

### 4.2 当前最值得先动刀的地方

优先级建议：

1. 继续裁撤 `Damageable / JUHealth / JutpsHealthProxy / JutpsEnemyDamageableAdapter` 的过渡保留范围
2. 把 JUTPS 玩家链与默认玩家 UI 从正式路径剔除
3. 把 Akila Inventory 从“潜在正式方案”降级为“迁移期执行依赖”
4. 逐步压缩 Tag/Layer 回写适配层
