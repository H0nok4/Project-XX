# Project-XX Runtime UI 制作规范

## 1. 目的

本文档用于统一 `Project-XX` 的运行时 UI 制作方式，避免继续出现以下问题：

- 同一种 UI 同时存在 IMGUI、运行时代码拼 UGUI、UGUI 预制体三套实现方式。
- 布局结构散落在 `BuildView` / `BuildWindow` 代码里，后续改版必须改 C#，协作成本高。
- UI 层级、命名、资源路径不统一，后续难以维护和复用。

从本文档生效后，项目中新增的运行时 UI 一律使用 `UGUI 预制体` 制作，不再新增 `IMGUI`，也不再新增“通过代码在运行时搭完整 UGUI 层级”的实现。

## 2. 当前框架基线

基于现有代码，当前 UI 框架的职责边界如下：

- `PrototypeRuntimeUiManager`
  - 负责运行时唯一 Canvas、`CanvasScaler`、`GraphicRaycaster`、`EventSystem` 与分层根节点。
  - 负责提供 `Background / World / Hud / Window / Modal / Overlay` 六个 UI 层。
- `ViewBase`
  - 负责常驻视图/HUD 的生命周期、显隐、根节点管理。
  - 适合 HUD、提示条、浮层、页面壳等非窗口型 UI。
- `WindowBase`
  - 负责窗口/弹窗的生命周期、显隐与窗口 chrome 接口。
  - 适合 Inventory、Dialog、Journal、输入框等窗口型 UI。
- `*Template`
  - 负责把预制体中的 `RectTransform`、`Text`、`Button`、`InputField`、`ScrollRect` 等引用收口到一个脚本里，供 Controller/View 绑定。
- `PrototypeUiToolkit`
  - 目前仍承担基础 UI 工具和部分旧实现的兜底逻辑。
  - 后续不再作为“新运行时界面的主要搭建方式”，只保留给底层基础设施、历史兼容或编辑器辅助用途。
- `Assets/Res/Scripts/UI/Common/*`
  - 负责沉淀运行时 UGUI 的通用组件能力，例如生命周期小组件、虚拟列表、按钮动效、通用过渡、Tab 组等。
  - 新界面制作时应先评估是否可以直接复用这些组件，而不是每个界面各自重复实现一套交互逻辑。

通用组件库的设计说明与典型用法见：

- `Docs/RuntimeUiComponentLibraryGuide.md`

## 3. 强制规则

### 3.1 新增运行时 UI 一律使用 UGUI 预制体

- 新增运行时 UI 必须先在 Unity Editor 中制作成 `UGUI prefab`。
- 运行时脚本只负责 `实例化预制体`、`绑定引用`、`注册事件`、`刷新数据`、`控制显隐`。
- 不允许在运行时通过代码手动创建完整界面结构。

### 3.2 禁止在运行时 UI 中继续使用 IMGUI

以下 API 不允许再出现在运行时 UI 脚本中：

- `OnGUI`
- `GUI`
- `GUILayout`

说明：

- `Assets/**/Editor` 下的编辑器工具窗口可以继续使用 IMGUI，这是 Unity Editor 工具范畴，不属于运行时 UI。
- 提交到仓库的运行时 UI 不允许以 IMGUI 作为正式实现。

### 3.3 禁止通过代码拼完整 UGUI 层级

新增运行时 UI 不允许继续复制以下做法：

- `new GameObject(...)` 创建整套界面节点
- `AddComponent<Text/Image/Button/InputField/ScrollRect/...>()` 拼完整界面
- 在 `BuildView` / `BuildWindow` 中大量使用 `PrototypeUiToolkit.CreateText/CreateButton/CreatePanel/CreateScrollView/...` 搭建整页 UI

允许的行为只有两类：

- 运行时实例化 `已经做好的 UI 预制体`
- 运行时实例化 `已经做好的条目/列表项/按钮项预制体`

也就是说，动态内容可以生成，但生成出来的单元也必须来自预制体，而不是代码手搓层级。

### 3.4 `PrototypeRuntimeUiManager` 仍是唯一层级入口

- 所有运行时 UI 都必须挂到 `PrototypeRuntimeUiManager` 提供的 layer root 下。
- 不允许每个系统自己额外创建新的屏幕级 Canvas，除非是明确评审过的特殊场景（例如独立 world-space UI）。
- 普通界面必须通过以下入口挂载：
  - `UiManager.GetLayerRoot(Layer)`
  - `UiManager.GetLayerRoot(WindowLayer)`

## 4. 推荐目录与命名

### 4.1 预制体目录

- 统一放在 `Assets/Resources/UI/...`
- 资源路径常量统一写成 `UI/<Module>/<PrefabName>`

示例：

- `Assets/Resources/UI/Quest/DialogueWindow.prefab`
- `Assets/Resources/UI/Quest/QuestJournal.prefab`
- `Assets/Resources/UI/StudentID/StudentIdCardNameEntryWindow.prefab`

### 4.2 脚本命名

推荐命名如下：

- 窗口控制器：`XxxWindowController`
- 常驻视图/HUD：`XxxView` / `XxxHud`
- 预制体引用脚本：`XxxTemplate` / `XxxViewTemplate` / `XxxWindowTemplate`

示例：

- `StudentIdCardNameEntryWindowController`
- `StudentIdCardNameEntryWindowTemplate`
- `DialogueSystem`
- `DialogueWindowViewTemplate`

## 5. 推荐实现方式

### 5.1 Window/Modal 类型

标准结构：

- `XxxWindowController : WindowBase`
- `XxxWindowTemplate : MonoBehaviour`
- `Assets/Resources/UI/<Module>/XxxWindow.prefab`

推荐职责：

- `CreateWindowChrome()`
  - 从 `Resources` 加载窗口预制体。
  - 实例化到 `UiManager.GetLayerRoot(WindowLayer)`。
  - 从 `Template` 中组装 `WindowChrome`。
- `BuildWindow()`
  - 只做引用校验、事件绑定、初始状态设置、数据刷新。
  - 不负责创建窗口布局。

如果窗口需要标题、子标题、Body、Footer，请在预制体里直接准备好，再由 `Template.CreateWindowChrome()` 把这些引用返回给 `WindowBase`。

### 5.2 View/HUD 类型

标准结构：

- `XxxView : ViewBase`
- `XxxViewTemplate : MonoBehaviour`
- `Assets/Resources/UI/<Module>/XxxView.prefab`

推荐职责：

- `CreateViewRoot()`
  - 优先直接实例化完整预制体，并返回其根节点。
- `BuildView()`
  - 只做引用绑定、事件注册、状态初始化、数据刷新。
  - 不负责搭建整套 HUD 结构。

如果由于基类限制保留一个空 root 作为挂点，那么该 root 下面的第一个有效子节点也必须来自预制体，不允许继续在代码里把 Text/Button/ScrollView 一层层拼出来。

### 5.3 动态列表与重复内容

以下内容允许运行时动态生成：

- 列表项
- Tab 项
- 商店商品项
- 任务项
- 背包格子项

但要求：

- 这些动态单元本身也必须是单独的 UGUI 预制体。
- 控制器只负责 `Instantiate(itemPrefab)`、绑定数据、回收/刷新。
- 不允许在循环里反复 `CreateText/CreateButton/AddComponent` 拼条目。

### 5.4 新界面应优先评估通用组件复用

新增运行时 UI 时，不要只考虑“这个界面能不能做出来”，还要优先考虑“这个界面哪些部分已经有通用组件可复用”。

推荐先检查以下几类：

- 是否存在重复条目、长列表、滚动列表
  - 优先评估 `UiVirtualList` + `UiReusableRenderer<TData>`
- 是否存在通用按钮、导航按钮、主操作按钮、Tab 按钮
  - 优先评估 `UiAnimatedButton`
- 是否存在窗口、页面、浮层、HUD 的进出场动画
  - 优先评估 `UiTransitionPlayer`
- 是否是基于 `ViewBase` / `WindowBase` 的界面，且根节点需要显隐动画
  - 优先评估 `UiAnimatedViewBase` / `UiAnimatedWindowBase`
- 是否存在页面切换、Tab 切换、二级导航切换
  - 优先评估 `UiTabGroup`
- 是否存在会被反复绑定不同数据的卡片、格子、列表单元
  - 优先评估 `UiReusableRendererBase` / `UiReusableRenderer<TData>`

要求如下：

- 可以复用通用组件时，优先复用，不要在业务脚本里重新手写一套相同能力。
- 如果现有通用组件只能覆盖 70% 到 80% 的场景，优先在通用组件上做小幅扩展，而不是直接在具体界面里复制一份“特化版本”。
- 只有当交互模型明显不同，或者抽象后会让通用层变得更混乱时，才考虑单独实现。

这条规则的目的是：

- 保持按钮、切页、进出场、列表复用的体验一致。
- 降低后续界面改版和维护成本。
- 避免项目里再次出现多个系统各写一套列表、按钮、Tab、显隐动画逻辑的情况。

## 6. 预制体制作要求

每个正式运行时 UI 预制体至少应满足以下要求：

- 根节点命名清晰，和功能一致。
- 挂好对应 `Template` 脚本。
- 所有运行时要访问的节点都通过序列化字段暴露在 `Template` 上。
- `Button`、`InputField`、`ScrollRect`、列表内容根节点、关闭按钮等关键引用必须可直接拖拽绑定。
- 锚点、布局组、`LayoutElement`、`ContentSizeFitter` 等在编辑器里配置完成，不把布局参数硬编码到运行时代码里。
- 如果界面要跟随项目运行时字体策略，实例化后统一调用 `PrototypeUiToolkit.ApplyFontRecursively(...)`。
- 需要阻挡点击的遮罩层必须在预制体里明确配置 `Image` / `CanvasGroup` / `raycastTarget`，不要依赖隐式行为。
- 如果按钮、列表、Tab、页面切换、显隐动画已经可以用通用组件解决，应直接把对应通用组件挂到 prefab 上，而不是把同类逻辑散落到控制器脚本里。

## 7. 层级使用规范

`PrototypeUiLayer` 的使用约定如下：

- `Background`
  - 全屏背景、静态底板、纯展示层。
- `World`
  - 特殊 world-space 或世界信息层；慎用。
- `Hud`
  - 战斗 HUD、状态栏、常驻提示。
- `Window`
  - 非强阻塞窗口，例如库存、角色页、一般信息面板。
- `Modal`
  - 阻塞操作、强焦点弹窗、命名输入、对话框、确认框。
- `Overlay`
  - Toast、转场遮罩、强提示、全局浮层。

窗口应放到“最小但足够”的层级，不要为了省事全部塞进 `Overlay`。

## 8. 现有代码中的参考与非参考案例

### 8.1 可以作为正向参考的写法

以下实现符合或接近后续标准：

- `Assets/Res/Scripts/UI/StudentIdCardNameEntryWindowController.cs`
  - 使用 `Resources` 预制体 + `Template` + `WindowBase`
- `Assets/Res/Scripts/UI/StudentIdCardNameEntryWindowTemplate.cs`
  - 通过模板脚本暴露窗口引用
- `Assets/Res/Scripts/Quest/DialogueSystem.cs`
  - 优先实例化 `DialogueWindow.prefab`
- `Assets/Res/Scripts/Quest/DialogueWindowViewTemplate.cs`
  - 使用模板对象组装 `WindowChrome`
- `Assets/Res/Scripts/Profile/PrototypeMainMenuController.cs`
  - 入口层面使用 UGUI 主预制体

### 8.2 属于历史实现，不再新增复制的写法

以下代码可以暂时保留，但不应再作为后续新 UI 的模板：

- `Assets/Res/Scripts/UI/StudentIdCardNameEntryView.cs`
  - 在 `BuildView` 中代码创建完整输入界面
- `Assets/Res/Scripts/Loot/PlayerInventoryWindowController.cs`
  - 在 `BuildWindow` 中代码创建完整窗口内容
- `Assets/Res/Scripts/Quest/QuestTrackerHUD.cs`
  - `BuildView` 与 `EnsureJournalUi` 中仍保留代码拼 UGUI 的 fallback 分支
- 各类运行时 `OnGUI` / `GUI` / `GUILayout` 实现

结论：

- 这些属于已有技术债或过渡实现。
- 可以继续运行，但后续不要照着扩展。
- 如果对这些界面进行较大改版，应优先在同一任务内改造成预制体方案。

## 9. 标准制作流程

新增一个运行时 UI 时，按以下顺序执行：

1. 在 Unity Editor 中先做出 UGUI 预制体。
2. 先检查界面中的按钮、列表、页签、进出场、重复条目，评估是否可以直接复用 `Assets/Res/Scripts/UI/Common/` 中的通用组件。
3. 给预制体添加 `Template` 脚本，并把所有关键引用拖好。
4. 将预制体放入 `Assets/Resources/UI/<Module>/`。
5. 编写 `Controller/View` 脚本，继承 `WindowBase` 或 `ViewBase`；如果需要根节点显隐动画，优先使用 `UiAnimatedWindowBase` 或 `UiAnimatedViewBase`。
6. 在 `CreateWindowChrome()` 或 `CreateViewRoot()` 中实例化预制体。
7. 在 `BuildWindow()` 或 `BuildView()` 中只做绑定和初始化，不做布局搭建。
8. 如果有动态列表，再单独制作列表项预制体，并优先评估是否接入 `UiVirtualList` 与 `UiReusableRenderer<TData>`。
9. 联调以下内容：
   - 打开/关闭
   - 层级是否正确
   - 输入焦点与鼠标锁定
   - 分辨率缩放
   - 按钮事件、滚动区域、遮罩拦截
   - 是否已经正确接入可复用通用组件，而不是在控制器里重复实现相同交互

## 10. 落地约定

- 从本规范落地后，新增运行时 UI 必须遵守本文件。
- 新需求不再接受 IMGUI 运行时实现。
- 新需求不再接受“代码直接拼完整 UGUI”的实现。
- `PrototypeUiToolkit.Create*` 在后续新 UI 中只允许用于底层基础设施、历史兼容、编辑器辅助脚本，不再作为正式界面搭建主路径。
- 新需求在制作界面时，必须先评估是否可以复用现有通用组件；可以复用时，不应重新手写同类按钮、列表、Tab、显隐动画逻辑。
- 需要对旧 UI 做小修时，可以先继续沿用旧代码；但如果任务已经涉及改布局、改交互、改结构，则应优先顺手改成预制体方案，而不是继续扩大技术债。

