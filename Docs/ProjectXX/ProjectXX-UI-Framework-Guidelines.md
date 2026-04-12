# Project-XX UI Framework Guidelines

## 1. 目的

这份文档用于统一 Project-XX 在开发期与后续产品化阶段的 UI 制作方式，目标是：

- 让现有 `ViewBase / WindowBase / UiWidgetBase` 框架继续可扩展
- 避免后期 UI 代码、Prefab、输入、层级和状态管理失控
- 让新同学接手时可以快速判断“这个 UI 该怎么做”
- 让项目逐步朝“可发布游戏”的 UI 工程方式靠拢，而不是停留在原型阶段

适用范围：

- `Assets/Res/Scripts/UI`
- `Assets/Resources/UI`
- 所有运行时 HUD、界面页、弹窗、列表项、交互提示、通用控件

---

## 2. 基于当前框架的结论

## 2.1 当前框架已经做对的部分

现有框架的基础方向是对的，已经有一套能继续往产品化方向推进的骨架：

- `PrototypeRuntimeUiManager` 统一创建 Runtime Canvas、Layer 和 EventSystem
- `ViewBase` 负责界面类 UI 的根节点和生命周期
- `WindowBase` 负责窗口类 UI 的根节点、WindowChrome 和生命周期
- `PrefabViewBase<TTemplate>`、`PrefabWindowBase<TTemplate>` 已经开始收口 Prefab 加载与 Template 绑定流程
- `UiWindowService` 已经开始统一管理顶层 UI 的显示顺序与输入分发
- `UiInputService` 已经开始统一处理 Submit / Cancel 的全局输入入口
- `UiRouter` 已经开始提供统一的获取、打开、关闭与查询入口
- `UiPrefabRegistry` 已经开始统一收口 Runtime UI prefab 的注册信息
- `UiWidgetBase` 负责可复用控件的初始化、显示隐藏、交互状态和布局刷新
- `UiReusableRendererBase / UiReusableRenderer<T>` 负责可复用列表项绑定
- `UiVirtualList` 已经提供了发布级项目常见的长列表虚拟化基础能力
- `UiTransitionPlayer`、`UiAnimatedViewBase`、`UiAnimatedWindowBase`、`UiAnimatedButton` 已经具备基础动画入口
- `*Template` 组件已经开始承担“Prefab 引用收口”的职责

结论：

- 这套框架不需要推倒重来
- 但要从“能做 UI”升级到“能稳定维护大量 UI”，还需要补几层生产级规范和服务

## 2.2 当前框架最明显的风险点

结合现有代码，当前最容易在后期失控的点有这些：

- `UiWindowService` / `UiRouter` / `UiInputService` 已经落地，但返回链、窗口互斥、默认焦点和关闭后焦点恢复还没完全收敛
- 仍有一部分 UI 代码保留了旧的直接 `Resources.Load("字符串路径")` 思路，需要逐步迁到 `UiPrefabRegistry`
- 示例里已经出现“同一份业务 UI 同时有 `ViewBase` 版本和 `WindowBase` 版本”的情况
- 虽然已经有基础校验工具，但还没有形成团队层面的强制使用流程
- 还没有 Safe Area、主题、Localization、Loading Blocker、公共确认框等发布级公共能力
- 还没有明确“谁能写业务逻辑，谁只能做引用绑定”的硬规则

特别注意：

- 同一份 UI Prefab 不应该长期同时存在 `XxxView` 和 `XxxWindowController` 两套正式入口
- 当前 `StudentIdCardNameEntryView` 和 `StudentIdCardNameEntryWindowController` 共用一份 Prefab，这种模式在原型阶段可以接受，但正式项目里应收敛成一个唯一入口

---

## 3. 可发布游戏方向还需要补充什么

下面按优先级分成三层。

## 3.1 P0：必须尽快补齐

### 1. `UiWindowService` / `UiRouter`

当前状态：

- `UiWindowService` 已落地
- `UiRouter` 已落地

用途：

- 统一打开、关闭、置顶、返回、互斥窗口
- 统一 Modal 栈和 Back 栈
- 避免每个窗口自己做 singleton 和 `FindFirstObjectByType`

建议职责：

- `OpenWindow<TWindow>(...)`
- `CloseWindow<TWindow>()`
- `IsOpen<TWindow>()`
- `CloseTopmost()`
- 管理 `Window` / `Modal` / `Overlay` 的显示顺序和遮罩关系
- 统一 `GetOrCreate<T>()`，避免业务层到处写 `FindFirstObjectByType`

### 2. `UiInputService`

当前状态：

- `UiInputService` 已落地
- 目前已统一 Submit / Cancel 入口
- 后续还需要继续补输入上下文、默认焦点、关闭后焦点恢复与更完整的导航规则

用途：

- 统一 UI 层面的 Enter / Escape / Back / Cancel / Submit
- 支持键鼠、手柄、未来输入重绑定
- 避免每个 View / Window 自己在 `Update()` 里轮询键盘

建议职责：

- 提供 UI 级输入 Action
- 管理输入上下文切换
- 管理当前焦点对象、默认焦点、关闭后的焦点恢复

### 3. `PrefabViewBase<TTemplate>` / `PrefabWindowBase<TTemplate>`

这两个基类已经落地，后续新业务 UI 应优先基于它们继续做。

它们解决的问题是：

- `Resources.Load`
- `Instantiate`
- 拿 `Template`
- 判空
- 绑按钮
- 应用字体

建议补一个泛型中间层，统一这些步骤。

目标：

- 让大多数业务 UI 只写“绑定模板”和“刷新数据”
- 减少重复模板加载代码
- 强制所有正式 UI 走“Prefab + Template”模式

### 4. `UiPrefabRegistry`

当前状态：

- `UiPrefabRegistry` 脚本已落地
- `PrefabViewBase / PrefabWindowBase` 已支持优先从 registry 加载 prefab
- 编辑器下已补 `Rebuild Prefab Registry` 与 `Validate Prefab Registry`

不要让每个 UI 自己保存字符串资源路径。

建议：

- 用一个 `ScriptableObject` 或常量注册表统一维护 UI Id -> Prefab 路径 / Address
- UI 代码只认 `UiId`
- 路径和资源替换集中管理

### 5. `UiValidation` 编辑器校验

当前状态：

- 已补基础运行时 UI prefab 校验菜单
- 当前已覆盖 registry 重复项、路径不一致、RectTransform 根节点、缺失脚本、Button / ScrollRect / TMP_InputField / UiVirtualList 常见漏绑问题

至少要能检查：

- `Template` 是否漏绑引用
- `ViewBase / WindowBase` 对应 Prefab 是否缺 `Template`
- 列表项 Prefab 是否缺 `LayoutElement` 或 `UiReusableRendererBase`
- Layer / Raycast / CanvasGroup 是否明显异常

这类问题越早在编辑器里报错，后期越省事。

## 3.2 P1：建议在第一轮系统化 UI 时补齐

### 1. `UiSafeAreaAdapter`

用于：

- 刘海屏 / 异形屏适配
- 移动端或未来多平台适配

### 2. `UiThemeConfig`

把这些从代码里抽出来：

- 颜色
- 字体
- 字号
- 间距
- 圆角
- 阴影
- 默认按钮样式

不要把这些散在 `PrototypeUiToolkit` 的硬编码里。

### 3. `UiLocalizationBinder`

正式项目里不建议把文案长期写死在 View / Window 代码里。

建议：

- View / Window 负责绑定 Key
- Localize 服务负责给值
- `Template` 不持有业务字符串

### 4. 公共系统 UI

建议尽快沉淀成公共模块：

- `LoadingOverlay`
- `ConfirmDialog`
- `Toast`
- `Tooltip`
- `Blocker`
- `ErrorPopup`

### 5. `UiAudioFeedbackService`

统一处理：

- Hover
- Click
- Confirm
- Cancel
- Open / Close

不要每个按钮自己播音效。

## 3.3 P2：产品化后期建议补齐

- UI PlayMode 自动化测试
- 截图回归基线
- UI 内存 / 实例数监控
- UI 打开耗时采样
- Addressables 或更正式的 UI 资源分包策略

---

## 4. UI 类型选择规则

这是最重要的部分。

| 类型 | 继承基类 | 用途 | 典型例子 |
| --- | --- | --- | --- |
| 窗口类 UI | `WindowBase` | 弹窗、背包窗、日志窗、商店窗、确认框、对话框 | Inventory、QuestJournal、Merchant、ConfirmDialog |
| 界面类 UI | `ViewBase` | HUD、整页界面、常驻叠层、全屏主界面、BaseHub Overlay | RaidHud、MainMenuView、BaseHubOverlay |
| 通用控件 | `UiWidgetBase` | Tab、按钮、血条、拖拽项、可重用块 | TabGroup、AnimatedButton、DragGhost |
| 列表项渲染器 | `UiReusableRenderer<T>` | 长列表或复用列表项 | QuestItemRenderer、InventoryItemRenderer |
| Prefab 引用模板 | `MonoBehaviour` | 只负责字段引用，不负责业务逻辑 | `XxxTemplate` |
| 展示数据 / ViewModel | 普通 C# 类或 struct | 只负责展示数据，不负责场景对象引用 | `XxxContent`、`XxxViewModel` |

硬规则：

- 窗口类 UI 必须继承 `Assets/Res/Scripts/UI/WindowBase.cs`
- 界面类 UI 必须继承 `Assets/Res/Scripts/UI/ViewBase.cs`
- 小控件不要继承 `ViewBase` 或 `WindowBase`，应继承 `UiWidgetBase`
- 列表项不要直接写成普通 `MonoBehaviour`，应优先继承 `UiReusableRenderer<T>`
- `Template` 绝不写业务逻辑
- 窗口类 UI 默认进入 `UiWindowService` 的统一管理栈
- 界面类 UI 默认不进入统一栈，只有需要接管全局 Submit / Cancel 的界面才显式 opt-in

---

## 5. 什么时候用 `WindowBase`

满足以下任一条件，就优先用 `WindowBase`：

- 需要浮在其他 UI 之上
- 需要遮罩层
- 需要可以关闭 / 返回
- 需要作为 Modal 阻断下层输入
- 需要多个窗口并存或叠层
- 需要窗口标题、子标题、Footer 区域

典型例子：

- 商店
- 背包
- 任务日志
- 设施详情
- 对话弹窗
- 名字输入框
- 设置窗口

窗口类 UI 规则：

- 默认放在 `Window` 或 `Modal` Layer
- 外部只通过 `ShowWindow / HideWindow / SetWindowVisible` 控制，不直接 `SetActive`
- 默认由 `UiWindowService` 管理显示顺序与顶层输入处理
- 优先绑定作者制作好的 Prefab，不要在 `BuildWindow` 里临时拼完整业务层级
- 如果是正式项目，`PrototypeUiToolkit.CreateWindowChrome()` 应只作为兜底或调试用途
- 有输入框的窗口必须定义默认焦点与关闭后的焦点释放规则
- 有确认 / 取消逻辑的窗口必须定义关闭策略
  - Enter 是否确认
  - Escape 是否取消
  - 点击遮罩是否关闭

---

## 6. 什么时候用 `ViewBase`

满足以下条件时优先用 `ViewBase`：

- 是整页 UI
- 是 HUD 或常驻叠层
- 不强调“弹窗”
- 跟场景生命周期更接近
- 需要长期存在，只是显示 / 隐藏

典型例子：

- Raid HUD
- Main Menu 主页面
- BaseHub Overlay
- Quest Tracker
- Interaction Prompt

界面类 UI 规则：

- 默认放在 `Background` / `Hud` / `Overlay` Layer
- 外部只通过 `ShowView / HideView / SetViewVisible` 控制，不直接 `SetActive`
- 如果需要参与全局 Submit / Cancel 分发，必须显式接入 `UiWindowService`
- `BuildView` 负责绑定视图结构，不负责拉取大量业务数据
- 数据刷新应通过显式 `Refresh`、Presenter 回调或事件驱动，不要靠 `Update()` 轮询

---

## 7. `UiWidgetBase`、列表和模板的使用规则

## 7.1 `UiWidgetBase`

适用于：

- 可被多个页面复用的功能块
- 局部状态切换控件
- 动效按钮、Tab、分页器、血条、通用条目块

规则：

- 小控件统一继承 `Assets/Res/Scripts/UI/Common/UiWidgetBase.cs`
- 控件内部可以维护自己的局部状态
- 不直接处理大块业务流程
- `OnInitialize` 里做一次性初始化
- `OnRefresh` 里做数据刷新
- 控件自己的显示隐藏用 `Show / Hide / SetWidgetVisible`

## 7.2 `UiReusableRenderer<T>`

适用于：

- 滚动列表项
- 可复用卡片项
- 任务列表、背包列表、商店列表等

规则：

- 统一继承 `Assets/Res/Scripts/UI/Common/UiReusableRendererBase.cs` 或泛型子类
- 绑定逻辑只写在 `BindData`
- 清理逻辑写在 `UnbindData`
- 列表项不直接访问全局服务，不直接打开大型窗口

## 7.3 `UiVirtualList`

适用于：

- 数据量较大
- 会频繁刷新
- 项目后期容易变长的列表

建议规则：

- 列表项数量可能超过 20 时，优先评估使用 `Assets/Res/Scripts/UI/Common/UiVirtualList.cs`
- 不要在滚动列表里每次刷新都 `Instantiate / Destroy` 全部项
- 列表项高度不固定时，必须显式提供高度解析器或保证 `PreferredHeight` 稳定

## 7.4 `Template`

规则：

- `Template` 只做引用收口
- `Template` 不做业务判断
- `Template` 不读取服务
- `Template` 不写输入逻辑
- `Template` 不写资源加载
- `Template` 不做事件订阅

`Template` 唯一职责：

- 持有 Prefab 内部控件引用
- 在极少数情况下提供简单的 `ConfigureReferences`
- 对 `WindowBase` 场景可提供 `CreateWindowChrome` 这类纯结构适配

---

## 8. 命名规范

统一命名，不允许一个功能出现三套叫法。

### 8.1 脚本命名

- 界面类：`XxxView`
- 窗口类：`XxxWindowController`
- 模板类：`XxxTemplate`
- 通用控件：`XxxWidget`
- 列表项：`XxxItemRenderer`
- 内容数据：`XxxContent` 或 `XxxViewModel`
- 服务类：`UiXxxService`
- 路由类：`UiXxxRouter`

### 8.2 Prefab 命名

- 界面类 Prefab：`XxxView.prefab` 或项目既定页面名
- 窗口类 Prefab：`XxxWindow.prefab`
- 列表项 Prefab：`XxxItem.prefab`
- 通用控件 Prefab：`XxxWidget.prefab`

### 8.3 不允许的命名

- `TestWindow`
- `NewView`
- `TempPanel`
- `Panel1`
- `UIRootNew`
- `ButtonItem`

名字必须体现业务含义，而不是编辑过程痕迹。

---

## 9. 目录规范

建议从现在开始逐步收敛目录结构。

## 9.1 脚本目录

建议结构：

- `Assets/Res/Scripts/UI/Common`
- `Assets/Res/Scripts/UI/Views`
- `Assets/Res/Scripts/UI/Windows`
- `Assets/Res/Scripts/UI/Widgets`
- `Assets/Res/Scripts/UI/Renderers`
- `Assets/Res/Scripts/UI/Templates`
- `Assets/Res/Scripts/UI/Data`
- `Assets/Res/Scripts/UI/Services`
- `Assets/Res/Scripts/UI/Routing`

当前项目还没完全拆开，可以逐步迁移，不需要一次性大搬家。

## 9.2 Prefab 目录

正式运行时 UI 统一放在：

- `Assets/Resources/UI/<Feature>/`

例如：

- `Assets/Resources/UI/Raid/`
- `Assets/Resources/UI/Quest/`
- `Assets/Resources/UI/Loot/`
- `Assets/Resources/UI/Base/`
- `Assets/Resources/UI/Common/`

规则：

- 按功能域放，不按“按钮 / 图片 / 文本”这种美术类型放
- 共享控件单独进 `Common`
- 同一功能的 `View / Window / Item / Widget` 尽量放在同一功能目录下

---

## 10. 标准制作流程

以后每做一个正式 UI，都按下面流程走。

### 第 1 步：先判断 UI 类型

先回答三个问题：

- 这是窗口，还是整页界面，还是小控件？
- 它是否需要阻断下层输入？
- 它是否是高频复用列表项？

判断结果：

- 窗口 -> `WindowBase`
- 界面 -> `ViewBase`
- 控件 -> `UiWidgetBase`
- 列表项 -> `UiReusableRenderer<T>`

### 第 2 步：定义展示数据

先定义：

- 输入参数
- 刷新参数
- 关闭结果
- 列表项数据结构

不要一开始就把逻辑塞进按钮回调。

### 第 3 步：先做 Prefab，再写代码

先在 `Assets/Resources/UI/<Feature>/` 下制作 Prefab。

Prefab 要求：

- Root 上必须挂 `RectTransform`
- 窗口 / 界面 Prefab 必须挂自己的 `Template`
- 结构稳定后再写脚本绑定

### 第 4 步：制作 `Template`

规则：

- 把需要的 `TMP_Text`、`Button`、`Image`、`ScrollRect`、`RectTransform` 全部序列化出来
- 不写业务逻辑
- 不在 `Awake` 里做查找

### 第 5 步：制作 `View` 或 `Window`

规则：

- 窗口类继承 `WindowBase`
- 界面类继承 `ViewBase`
- `BuildView / BuildWindow` 只负责绑定模板和初始化 UI 结构
- 订阅按钮事件时要先 `RemoveListener` 再 `AddListener`

### 第 6 步：接业务逻辑

建议：

- View / Window 只处理 UI 行为和输入桥接
- 业务状态变化由外部 Service / Presenter / Controller 推进
- 不要让 UI 自己直接变成领域逻辑中心

### 第 7 步：做自测

至少检查：

- 打开
- 关闭
- 重复打开
- 焦点是否正确
- 返回键是否正确
- 文案长度拉满是否破版
- 分辨率变化是否破版
- 列表数据为空 / 很多 / 异常时是否正常

---

## 11. 必须遵守的硬规则

以下规则不允许绕开。

### 11.1 生命周期规则

- 外部代码不直接 `SetActive` 控制 `ViewBase / WindowBase`
- 必须走 `ShowView / HideView / SetViewVisible`
- 必须走 `ShowWindow / HideWindow / SetWindowVisible`
- 需要初始化时调用 `EnsureView / EnsureWindow / EnsureInitialized`

### 11.2 业务边界规则

- `Template` 不写业务逻辑
- `Template` 不查服务
- `Template` 不读存档
- `Template` 不做输入判断
- `Template` 不做资源加载

### 11.3 输入规则

- 正式 UI 不允许每个窗口各自轮询一套键盘逻辑作为长期方案
- Escape / Enter / Submit / Cancel 最终必须收敛到统一 UI 输入服务
- 输入框、Tab、按钮导航顺序必须可控

### 11.4 资源规则

- 正式 UI Prefab 一律放在 `Assets/Resources/UI/...`
- 不允许把场景里的临时对象直接当正式 UI 资源长期引用
- 同一业务 UI 只保留一个正式 Prefab 入口

### 11.5 单一入口规则

- 同一份正式业务 UI 不允许长期同时存在 `XxxView` 和 `XxxWindowController` 两套入口
- 选定后只能保留一个 canonical 入口
- 如果临时实验了第二种实现，合并后必须删掉实验版本

### 11.6 代码风格规则

- 不在 UI 逻辑里到处 `FindFirstObjectByType`
- 正式业务代码优先通过 `UiRouter` 获取 / 打开 / 关闭 UI
- 不在 `BuildView / BuildWindow` 里拼复杂业务层级
- 不把大量运行时样式写死在业务 View 里
- 不在列表刷新里反复 `Instantiate / Destroy`
- 不在深层 UI 结构里到处强制 `LayoutRebuilder.ForceRebuildLayoutImmediate`

---

## 12. 推荐补的统一能力

建议后续逐步沉淀出这些公共模块：

- `UiWindowService`
- `UiRouter`
- `UiInputService`
- `UiPrefabRegistry`
- `UiValidation`
- `UiLoadingOverlay`
- `UiConfirmDialog`
- `UiToastService`
- `UiTooltipService`
- `UiThemeConfig`
- `UiSafeAreaAdapter`
- `UiLocalizationBinder`

如果这些公共能力不补，项目 UI 越多，风格和行为越容易碎片化。

---

## 13. 自检清单

每做完一个 UI，都先过一遍这份清单：

- 这个 UI 选对基类了吗？
- 窗口类是否继承了 `WindowBase`？
- 界面类是否继承了 `ViewBase`？
- 小控件是否误用了 `ViewBase / WindowBase`？
- Prefab 是否放在 `Assets/Resources/UI/<Feature>/`？
- 是否有独立 `Template`？
- `Template` 是否只负责引用？
- 是否存在第二套重复入口？
- 是否统一通过 `PrototypeRuntimeUiManager` Layer 挂载？
- 是否定义了显示隐藏流程？
- 是否定义了按钮事件解绑与重绑？
- 是否检查了空数据、长文本、重复打开和关闭流程？
- 列表是否需要用 `UiVirtualList`？
- 是否会和其他 UI 在输入、焦点或遮罩上冲突？

---

## 14. 一句话原则

Project-XX 后续 UI 的标准做法应该是：

- `WindowBase` 管窗口
- `ViewBase` 管界面
- `UiWidgetBase` 管控件
- `UiReusableRenderer<T>` 管列表项
- `Template` 只管引用
- 业务逻辑通过 Service / Presenter / Controller 收口
- Runtime UI 全部统一挂到 `PrototypeRuntimeUiManager`

如果未来出现“做一个 UI 不知道该放哪、该继承谁、该谁管逻辑”的情况，先回来看这份文档，再动手写代码。
