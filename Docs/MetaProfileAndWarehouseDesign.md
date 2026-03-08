# Project-XX 局外仓库与资料流转设计

## 1. 文档范围

本文档只讨论局外系统，也就是玩家不在战斗场景时的资料、仓库、带入和结算回写逻辑。

本文档覆盖：

- 主菜单
- Profile 持久化
- Stash / Loadout
- 局外到局内的数据流转
- 局内结果回写到局外

本文档不重点讨论：

- 玩家战斗控制
- AI
- 局内伤害系统

这些内容见：

- [InRaidCombatSystemDesign.md](D:/UnityProject/Project-XX/Project-XX/Docs/InRaidCombatSystemDesign.md)

---

## 2. 局外系统目标

当前局外系统的目标不是做完整商人/任务/经济，而是先把单机搜打撤原型最小闭环跑通：

1. 玩家进入主菜单
2. 玩家整理仓库
3. 玩家配置本局带入
4. 玩家进入战斗
5. 战斗结果回写仓库
6. 玩家回到主菜单继续准备下一局

所以设计上优先保证：

- 流程完整
- 数据不丢
- 容易继续扩展

---

## 3. 局外模块结构

当前局外系统可以拆为 4 层：

1. 物品索引层
2. Profile 持久化层
3. 主菜单与仓库 UI 层
4. 战局桥接层

依赖方向：

`Item Catalog -> Profile Service -> Main Menu / Raid Flow`

---

## 4. 物品索引层

### 4.1 核心脚本

- `Assets/Res/Scripts/Profile/PrototypeItemCatalog.cs`

### 4.2 职责

这个模块不是背包，也不是存档，它的作用是：

- 提供 `itemId -> ItemDefinition` 的运行时查询
- 保存默认 Stash 预设
- 保存默认 Loadout 预设

### 4.3 资产

- `Assets/Resources/PrototypeItemCatalog.asset`

这个资产放在 `Resources` 下的原因：

- 主菜单场景和战斗场景都能直接加载
- 不需要额外挂引用也能找到

### 4.4 设计意义

局外 Profile 存的是轻量记录：

- `itemId`
- `quantity`

而不是直接序列化 `ItemDefinition` 资源引用。

因此必须有一个目录层把保存数据重新解析回运行时物品定义。

---

## 5. Profile 持久化层

### 5.1 核心脚本

- `Assets/Res/Scripts/Profile/PrototypeProfileService.cs`

### 5.2 当前 Profile 结构

当前保存结构非常轻量，主要包含两块：

- `stashItems`
- `loadoutItems`

每条记录都是：

- `itemId`
- `quantity`

### 5.3 为什么当前这样设计

因为这是原型阶段，当前还没有：

- 装备耐久差异
- 同类武器改装差异
- 独立实例属性
- 容器嵌套

所以先用“按物品类型堆叠”的数据结构最合适。

### 5.4 当前保存位置

Profile 保存到：

- `Application.persistentDataPath/prototype_profile.json`

### 5.5 当前服务职责

`PrototypeProfileService` 负责：

- 读取已有 Profile
- 若无存档则根据 `PrototypeItemCatalog` 创建默认 Profile
- 将 `InventoryContainer` 转成可保存记录
- 将保存记录恢复成运行时背包内容
- 将战利品合并回 `stashItems`

### 5.6 当前限制

当前 Profile 还不支持：

- 单个装备实例差异
- 改装枪械
- 独立护甲耐久跨局保存
- 装备位序列化
- 任务、经验、角色属性

这些都属于下一阶段扩展内容。

---

## 6. 主菜单与仓库模块

### 6.1 核心脚本

- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`

### 6.2 当前职责

该模块承担：

- 主菜单首页
- 仓库页
- `Stash` 容器
- `Raid Loadout` 容器
- 从仓库搬运到带入
- 从带入退回仓库
- 保存 Profile
- 重置默认 Profile
- 切换到战斗场景

### 6.3 当前 UI 结构

当前主菜单有两个主要页面：

- `Home`
  - 显示当前 Stash / Loadout 摘要
  - 提供进入战斗按钮

- `Stash`
  - 左侧：Stash
  - 右侧：Raid Loadout
  - 点击按钮在两边转移整组物品

### 6.4 为什么当前不用更复杂的仓库 UI

因为目前目标是先验证资料流转，而不是做完整 Tarkov 仓库。

所以暂时不做：

- 网格背包
- 拖拽
- 拆分栈
- 装备栏
- 子弹压弹匣 UI

先保持：

- 简单
- 稳定
- 能完整进出战局

### 6.5 容器实现

主菜单并没有新做一套仓库容器，而是继续复用：

- `InventoryContainer`

这意味着当前局外和局内在物品层使用同一套容器模型。

优点：

- 数据结构统一
- 转移逻辑一致
- 后续扩展成本更低

---

## 7. 局外到局内的流转

### 7.1 进入战斗前

流程如下：

1. 玩家在 `MainMenu` 中整理 `Stash`
2. 将部分物品搬到 `Raid Loadout`
3. 点击 `Enter Battle`
4. `PrototypeMainMenuController` 先保存 Profile
5. 场景切换到 `SampleScene`

### 7.2 进入战斗后

局内不会直接读主菜单内存状态，而是重新从 Profile 读取：

1. `SampleScene` 中的 `PrototypeRaidProfileFlow` 启动
2. 读取 `PrototypeItemCatalog`
3. 加载 Profile
4. 用 `loadoutItems` 填充玩家当前背包

设计意义：

- 场景切换后没有悬空状态依赖
- 主菜单和战斗场景之间的耦合更低
- 以后就算做存档加载或直接进战局，也能复用这条链路

---

## 8. 战局结果回写局外

### 8.1 核心脚本

- `Assets/Res/Scripts/Profile/PrototypeRaidProfileFlow.cs`

### 8.2 当前职责

它是局外与局内之间的桥：

- 开局：把 `loadoutItems` 装进玩家背包
- 结算：根据战局结果更新 Profile
- 非运行中状态下显示 `Return To Menu`

### 8.3 当前回写规则

#### 撤离成功

- 玩家当前背包中的全部物品合并回 `stashItems`
- 清空 `loadoutItems`
- 保存 Profile

#### 死亡 / 超时

- 不回收局内背包
- 直接清空 `loadoutItems`
- 保存 Profile

### 8.4 当前规则的含义

这套规则是一种原型期简化：

- 带入品进入战局后就脱离局外
- 撤离成功才会回到局外
- 失败则视为损失

虽然还很简化，但已经能验证最核心的风险/收益关系。

---

## 9. Build Settings 与场景入口

### 9.1 当前场景

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/SampleScene.unity`

### 9.2 Build Settings

由：

- `Assets/Res/Scripts/UI/Editor/PrototypeMainMenuSceneBuilder.cs`

自动维护。

当前会确保：

- `MainMenu` 在 Build Settings 中
- `SampleScene` 在 Build Settings 中

### 9.3 主菜单场景生成器

主菜单不是纯手工搭建，而是由 `PrototypeMainMenuSceneBuilder` 生成。

它负责：

- 创建基础场景结构
- 创建主菜单系统对象
- 创建/更新 `PrototypeItemCatalog.asset`
- 更新 Build Settings

设计意义：

- 主菜单结构可以重复生成
- 新增默认物品时，目录资产不会手工漏改

---

## 10. 当前局外系统的优点

### 10.1 已经具备最小 Meta 闭环

当前局外系统虽然简单，但已经形成：

- 准备
- 带入
- 结算
- 回写

这条完整链路。

### 10.2 Profile 与场景解耦

主菜单和战斗场景不是靠内存对象硬传状态，而是通过：

- `ProfileService`
- `ItemCatalog`

建立稳定边界。

这对后续扩展很重要。

### 10.3 局外与局内背包复用

同一套 `InventoryContainer` 能同时服务：

- 主菜单仓库
- 局内背包
- 箱子
- 地面掉落

这让系统结构非常统一。

---

## 11. 当前局外系统的短板

### 11.1 Profile 颗粒度太粗

目前只记录按类型堆叠后的数量，不记录：

- 独立物品实例
- 改装状态
- 当前耐久
- 装备栏位置

如果后面要做更像 Tarkov 的仓库，这一层必须升级。

### 11.2 UI 仍然是 IMGUI

这会限制：

- 复杂布局
- 拖拽体验
- 动画
- 局外角色页
- 仓库分页和筛选

### 11.3 没有独立装备系统

当前 `Loadout` 只是一个容器，不是“角色身上装备位”的概念。

所以当前还缺：

- 头部装备位
- 胸甲位
- 主武器位
- 副武器位
- 背包位
- 快捷栏位

### 11.4 经济系统尚未存在

当前没有：

- 商人
- 金钱结算
- 任务奖励
- 仓库扩容
- 角色成长

这些都还没开始。

---

## 12. 推荐的下一步局外扩展

### 12.1 第一阶段

建议先加：

- 装备栏
- 带入确认页
- 更明确的结算摘要
- 局外角色信息摘要

### 12.2 第二阶段

然后再加：

- 格子仓库
- 拖拽与拆分
- 子弹/弹匣管理
- 武器实例化保存

### 12.3 第三阶段

最后再考虑：

- 商人
- 任务
- 货币
- 角色成长
- 多存档槽位

---

## 13. 推荐的技术重构方向

### 13.1 DTO 与运行时容器分离

现在 `ProfileService` 已经是半 DTO 化，但后续建议更明确区分：

- 存档 DTO
- Runtime Inventory
- UI ViewModel

### 13.2 装备系统独立化

当前 `Loadout` 还是“一个背包”。

后续建议单独做：

- `EquipmentSlotType`
- `EquipmentController`
- `EquippedItemInstance`

### 13.3 从 IMGUI 迁移

当局外系统功能再多一点时，建议把：

- 主菜单
- 仓库
- 结算页

统一迁移到 uGUI 或 UIToolkit。

---

## 14. 结论

当前局外系统已经完成了最小可用版本：

- 有主菜单
- 有仓库
- 有带入
- 有结算回写
- 有返回主菜单

它现在最大的价值不是“做得多复杂”，而是已经建立了一个清楚的 Meta 流程骨架。

后续你要继续做单机搜打撤，局外系统最合理的扩展顺序是：

1. 装备栏
2. 更正式的仓库 UI
3. 结算摘要
4. 任务/经济

不要一开始就把局外系统做成全功能大仓库，否则原型迭代成本会急剧上升。
