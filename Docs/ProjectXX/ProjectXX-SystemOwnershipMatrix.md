# Project-XX 系统所有权矩阵

更新时间：`2026-04-21`

本文用于明确：

- Project-XX
- Akila FPS Framework
- JUTPS

在正式架构中的系统所有权边界。

## 1. 总原则

系统只有一个正式 owner。  
非 owner 可以提供执行能力，但不能继续定义正式规则。

## 2. 当前目标矩阵

| 系统 | 正式 Owner | Vendor 角色 | 当前状态 | 目标状态 | 优先级 |
| --- | --- | --- | --- | --- | --- |
| 玩家输入与第一人称移动 | Akila | JUTPS 不参与 | 基本清晰 | 保留 Akila 主导 | P0 |
| 第一人称相机与视图栈 | Akila | Project-XX 负责修正规则 | 依赖桥接修正 | 固化到正式玩家 prefab | P0 |
| 第一人称武器表现与开火执行 | Akila | Project-XX 提供规则约束 | 基本可用 | 保留 Akila 执行，Project-XX 绑定正式物品规则 | P1 |
| 近战执行 | Akila / Project-XX | JUTPS 不参与 | 过渡态 | 收敛为 Project-XX 规则 + Akila 表现 | P1 |
| 健康/伤害/死亡/护甲/异常 | Project-XX | Akila/JUTPS 只提供执行入口 | 明显双活 | 收敛到单一 Project-XX combat state | P0 |
| 阵营/敌对关系 | Project-XX | JUTPS 只消费结果 | 已有第一版 | 升级为正式 faction 系统 | P1 |
| AI 导航与基础 locomotion | JUTPS | Project-XX 提供规则约束 | 可用 | 保留 JUTPS | P0 |
| AI 感知与目标选择 | Project-XX | JUTPS 负责底层感知执行 | 仍依赖 Tag/Layer 兼容 | 改为读取 Project-XX 正式目标信息 | P0 |
| 敌人攻击动画与命中骨架 | JUTPS | Project-XX 提供规则层 | 可用 | 保留 JUTPS | P1 |
| Inventory / Item / Container | Project-XX | Akila item/inventory 不再是正式 owner | 尚未正式建立 | 完全由 Project-XX 拥有 | P0 |
| Equipment / Loadout | Project-XX | Akila 只负责武器持有表现 | 仍偏桥接态 | 完全由 Project-XX 拥有 | P0 |
| Interaction / Prompt / Extraction | Project-XX | Akila/JUTPS 只提供底层输入/碰撞能力 | 原型期 | 完全由 Project-XX 拥有 | P0 |
| HUD / Window / UI 生命周期 | Project-XX | vendor UI 不进入正式产品 | 仍为 prototype | 完全由 Project-XX UI 框架接管 | P1 |
| Raid Session / Runtime Registry | Project-XX | vendor 不参与 | 原型期 | 完全由 Project-XX composition root 接管 | P0 |
| BaseHub / Meta / Save | Project-XX | vendor 不参与 | 尚未开始 | 完全由 Project-XX 拥有 | P1 |
| Vehicle | 待定，暂以 JUTPS 为执行层 | Project-XX 未来提供规则接入 | 未正式开始 | 先保留 JUTPS 执行能力 | P2 |

## 3. 必须马上停止摇摆的边界

以下边界不能继续保持“双系统双活”：

### 3.1 生命值与伤害

当前表现：

- 正式状态源：`ProjectXXCombatant`
- 玩家兼容端点：`Damageable + JUHealth + ProjectXXCombatantSync`
- 敌人兼容端点：`JUHealth + JutpsEnemyDamageableAdapter + ProjectXXCombatantSync`

正式方向：

- 只保留一个 authoritative combat state
- vendor 只作为执行入口或表现层
- `ProjectXXCombatantSync` 只允许单向把正式状态镜像给 vendor

### 3.2 Inventory / Equipment

当前表现：

- R2 计划由 Project-XX 拥有正式容器与装备系统
- 但玩家执行仍然依赖 Akila item / inventory 结构

正式方向：

- Item / Container / Equipment / Ammo ownership 完全归 Project-XX
- Akila 只保留“武器表现与执行载体”角色

### 3.3 AI 选敌

当前表现：

- Project-XX 已拥有 faction 规则
- JUTPS 仍需要 Tag / Layer 作为兼容层

正式方向：

- JUTPS FOV/选敌链只做执行
- 正式敌我判断归 Project-XX

## 4. 当前执行顺序

建议按下面顺序把矩阵变成真实代码边界：

1. 先收敛生命值/伤害系统
2. 再收敛 Inventory / Equipment ownership
3. 再重做 AI 目标与兼容层
4. 最后拆掉大量只为“双活”存在的桥接脚本
