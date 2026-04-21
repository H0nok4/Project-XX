# AGENT.md

更新时间：`2026-04-21`

本文件是 Project-XX 仓库级别的执行约束。  
后续所有实现、整改、重构、删改、文档更新都应默认遵守这里的原则。

## 1. 项目定位

Project-XX 是一个实验性新项目。  
当前目标不是“尽量温柔地把两个现成框架拼起来”，而是：

- 整合 `Akila FPS Framework` 与 `JUTPS`
- 把它们收编成一个健康、可长期演进、可承载正式内容制作的 Project-XX 框架

因此本仓库默认接受：

- 大改
- 删除代码
- 删除不再需要的 vendor 子系统
- 重写原框架逻辑
- 牺牲短期兼容性以换取长期健康性

## 2. 非妥协前提

### 2.1 不要因为“改动太多”而回避正确重构

如果某个问题的正确解法是：

- 删除旧链路
- 收敛双系统
- 改写 vendor 入口
- 废弃现有桥接

那么应优先做正确解法，而不是继续叠临时补丁。

### 2.2 Bridge 不是终局

桥接层只应该被视为：

- 迁移期手段
- vendor 能力接入手段
- 临时兼容层

如果一个 bridge 长期承担以下职责，就应该被视为待拆技术债：

- 双系统双活同步
- 正式规则落地
- 正式所有权转移
- 正式对象装配

### 2.3 系统必须选主

任何核心系统都必须有单一正式 owner。  
不允许长期存在“两个系统都算数”的正式架构。

至少以下系统必须显式选主：

- 玩家控制与相机
- 武器表现与开火执行
- 健康、伤害、死亡、阵营
- AI 感知、选敌、导航、攻击
- Inventory、Equipment、Container
- HUD、窗口、输入上下文
- Boot、场景装配、runtime registry
- Save/Profile/局外状态

### 2.4 Project-XX 拥有正式规则定义权

Project-XX 必须拥有以下正式规则的定义权：

- Combat state
- Faction / hostility
- Inventory / equipment / item definition
- Interaction / extraction / raid session
- HUD / window system
- Save / profile / meta state

Akila 和 JUTPS 可以继续提供执行能力，但不应继续定义这些正式规则。

## 3. 当前推荐系统所有权

当前默认正式方向如下：

- 玩家控制、第一人称相机、第一人称武器表现：`Akila`
- AI 导航、敌人基础感知、敌人动画攻击骨架：`JUTPS`
- 健康、伤害、死亡、阵营、交互、Inventory、Equipment、Session、HUD、Save：`Project-XX`

如果后续出现冲突：

- 优先维护 Project-XX 的正式所有权
- 不为保留 vendor 原貌而牺牲正式架构健康

## 4. 工程行为规范

### 4.1 禁止继续扩散的做法

从现在开始，默认禁止继续增加以下技术债：

- 在 ProjectXX 新逻辑里继续新增 `CompareTag(...)` 作为正式规则判断
- 在 ProjectXX 新逻辑里继续新增 `FindFirstObjectByType(...)` 作为正式依赖注入方式
- 在 gameplay 初始化里继续大规模 `AddComponent(...)` / `GetOrAdd(...)` 作为长期装配手段
- 让 `JUHealth <-> Damageable` 这种双系统镜像同步继续扩张
- 让 `Tag/Layer 回写` 从兼容层变成长期正式设计
- 继续把新的正式规则直接写进 vendor 文件但不登记
- 让场景安装器承担越来越多业务逻辑

### 4.2 Tag / Layer 只做兼容，不做正式规则源

Tag / Layer 可以存在，但只能承担：

- 第三方框架兼容
- 底层过滤
- 迁移期适配

正式规则必须优先依赖：

- 明确组件
- 明确 runtime state
- 明确定义资产

### 4.3 运行时装配优先级

长期目标优先级应为：

1. prefab authoring
2. composition root / scene context / registry
3. 显式引用
4. 运行时校验
5. 最后才是运行时自愈

### 4.4 删除无关系统是正向工作

如果某个 vendor 子系统：

- 不进入正式产品
- 会误导后续开发
- 会制造重复实现
- 会影响系统选主

那么删除、禁用、隔离它都是正向工作，而不是“破坏框架”。

## 5. Vendor 改动治理

### 5.1 任何 vendor 修改都必须可追踪

涉及 Akila / JUTPS 的正式修改，必须同步维护：

- `Docs/ProjectXX/ProjectXX-ThirdPartyPatchLog.md`
- `Docs/ProjectXX/ProjectXX-VendorOwnershipMatrix.md`

### 5.2 Vendor 模块必须被分类

每个 vendor 模块最终都应进入以下分类之一：

- `Retain`
- `Internalize`
- `Disable`
- `Delete`

不允许长期停留在“先留着以后再说”的模糊状态。

## 6. 框架整改优先顺序

在正式进入 R2 内容制作前，默认优先顺序为：

1. 先选主：完成系统所有权矩阵
2. 收敛双活：统一正式 combat state
3. 盘 vendor：完成 patch log 与 ownership matrix
4. 清路径：删除/禁用不会进入正式产品的 vendor 子系统
5. 拆边界：完成 ProjectXX asmdef 拆分
6. 固装配：建立 composition root / runtime registry
7. 再开始容器、搜刮、局外等内容型系统

## 7. 每次结构性改动后的必做项

发生以下任一情况时，必须同步更新文档：

- 系统所有权变化
- vendor patch 增减
- vendor 模块被收编 / 禁用 / 删除
- 重要 runtime 装配方式变化
- 新增或废弃正式框架约束

至少应检查这些文档：

- `AGENT.md`
- `Docs/ProjectXX/ProjectXX-FrameworkHealthPlan.md`
- `Docs/ProjectXX/ProjectXX-SystemOwnershipMatrix.md`
- `Docs/ProjectXX/ProjectXX-ThirdPartyPatchLog.md`
- `Docs/ProjectXX/ProjectXX-VendorOwnershipMatrix.md`

## 8. 当前默认判断规则

当出现以下冲突时，默认采用下面的取舍：

- “保留 vendor 原始结构” vs “Project-XX 长期健康”  
  选择：`Project-XX 长期健康`

- “先桥一下赶紧能跑” vs “一次性收敛双活系统”  
  选择：`一次性收敛双活系统`

- “也许以后用得上先别删” vs “减少长期噪音和误导”  
  选择：`减少长期噪音和误导`

- “短期改动面更小” vs “长期边界更清晰”  
  选择：`长期边界更清晰`
