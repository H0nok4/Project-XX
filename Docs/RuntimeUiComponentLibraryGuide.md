# Project-XX Runtime UGUI 组件库设计与用法

## 1. 目的

本文档用于说明 `Project-XX` 当前运行时 UGUI 通用组件库的设计目标、职责边界、推荐组合方式与典型用法。

它是对 [UiProductionStandard.md](./UiProductionStandard.md) 的补充：

- `UiProductionStandard.md`
  - 负责说明运行时 UI 的强制规则、目录规范、资源组织方式与禁止事项。
- 本文档
  - 负责说明已经封装好的通用组件如何在 `UGUI prefab` 中组合使用。

结论先行：

- 新运行时 UI 仍必须优先使用 `UGUI 预制体`。
- 通用组件库的目标是减少重复代码，而不是回到“运行时代码搭整页 UI”的旧做法。
- 新界面应优先通过“预制体 + Template + View/Window 控制器 + 通用组件”的组合完成。

## 2. 设计目标

当前组件库的设计目标如下：

### 2.1 Prefab First

- 交互结构、布局层级、节点关系应在 Unity Editor 中完成。
- 通用组件只负责：
  - 生命周期
  - 显隐
  - 动效
  - 列表复用
  - 页签切换
  - 常规交互状态

### 2.2 小组件可组合

- 尽量避免做“一个超级基类解决所有问题”。
- 更推荐把职责拆成可组合的独立组件，例如：
  - `UiAnimatedButton`
  - `UiTransitionPlayer`
  - `UiTabGroup`
  - `UiVirtualList`

### 2.3 与现有 ViewBase / WindowBase 体系兼容

- 常驻页面或 HUD 继续基于 `ViewBase`。
- 窗口或弹窗继续基于 `WindowBase`。
- 如果需要根节点进出场动画，可直接改为：
  - `UiAnimatedViewBase`
  - `UiAnimatedWindowBase`

### 2.4 列表项本身也必须 prefab 化

- 虚拟列表允许运行时生成条目。
- 但条目单元必须是独立 prefab。
- 列表控制器只做：
  - `Instantiate`
  - 绑定数据
  - 回收复用
  - 刷新显示

## 3. 组件清单

当前通用组件脚本位于：

- `Assets/Res/Scripts/UI/Common/`

### 3.1 `UiWidgetBase`

文件：

- `Assets/Res/Scripts/UI/Common/UiWidgetBase.cs`

职责：

- 作为通用 UI 小组件的生命周期基类。
- 提供初始化、刷新、显隐、交互状态控制、布局重建入口。

适用场景：

- 需要被挂在 prefab 上的通用显示组件
- 卡片、条目、通用面板、状态块、带行为的 UI 子模块

核心能力：

- `EnsureInitialized()`
- `RefreshWidget()`
- `Show() / Hide() / SetWidgetVisible()`
- `SetWidgetInteractable()`
- `RebuildLayout()`

推荐做法：

- 如果一个组件需要在 `OnEnable` 时自动刷新，可打开 `refreshOnEnable`。
- 如果一个组件需要依赖 `CanvasGroup` 控制交互，可打开 `autoCreateCanvasGroup`。

### 3.2 `UiReusableRendererBase` / `UiReusableRenderer<TData>`

文件：

- `Assets/Res/Scripts/UI/Common/UiReusableRendererBase.cs`

职责：

- 作为“可重复绑定数据的条目渲染器”基类。
- 适合列表项、卡片项、背包格子、商店商品项等重复单元。

适用场景：

- 一个 prefab 会被反复绑定不同数据
- 列表项需要支持回收和复用

核心能力：

- `Bind(object data, int index)`
- `Unbind()`
- 泛型版本 `UiReusableRenderer<TData>` 可直接做强类型绑定

推荐做法：

- 新的条目脚本优先继承 `UiReusableRenderer<TData>`。
- 在 `BindData(TData data, int index)` 中只做显示刷新，不做全局状态管理。

### 3.3 `UiVirtualList`

文件：

- `Assets/Res/Scripts/UI/Common/UiVirtualList.cs`

职责：

- 提供基于 `ScrollRect` 的虚拟化列表。
- 只创建并保持可视区附近的条目实例，超出区域的条目回收到池中。

适用场景：

- 背包列表
- 商店长列表
- 日志列表
- 任务列表
- 排行榜

关键引用：

- `scrollRect`
- `viewport`
- `contentRoot`
- `itemPrefab`

核心能力：

- `Configure(...)`
- `SetItemPrefab(...)`
- `SetItems<TData>(...)`
- `ClearItems()`
- `RefreshVisibleItems()`
- `ScrollToIndex(...)`

设计说明：

- `itemPrefab` 必须是带 `UiReusableRendererBase` 的 prefab。
- 列表默认按纵向从上到下布局。
- 每个条目的高度可以：
  - 走 `itemPrefab` 的 `PreferredHeight`
  - 走 `SetItems(..., heightResolver: ...)` 的外部高度回调

### 3.4 `UiAnimatedButton`

文件：

- `Assets/Res/Scripts/UI/Common/UiAnimatedButton.cs`

职责：

- 给标准 `Button` 增加 hover / press / selected / disabled 的视觉动效。

适用场景：

- 主操作按钮
- 二级操作按钮
- 导航按钮
- tab 按钮

可控制的视觉属性：

- 位移
- 缩放
- 颜色
- 透明度
- 点击 punch 动效

设计说明：

- 默认可关闭 `Selectable` 自带的颜色过渡，避免与自定义动画打架。
- `useSelectedStyle` 打开后，可让按钮拥有“选中态”表现。
- `UiTabGroup` 会自动尝试驱动同节点上的 `UiAnimatedButton` 选中状态。

### 3.5 `UiTransitionPlayer`

文件：

- `Assets/Res/Scripts/UI/Common/UiTransitionPlayer.cs`

职责：

- 为任意 `RectTransform` 提供统一的显示 / 隐藏过渡。

适用场景：

- 窗口根节点淡入淡出
- 页面切入切出
- HUD 浮层显隐
- tab 页面切换过渡

可控制的过渡属性：

- `anchoredPosition`
- `scale`
- `alpha`

核心能力：

- `PlayShow()`
- `PlayHide()`
- `ShowAndActivate()`
- `HideAndDeactivate()`
- `SnapToShown()`
- `SnapToHidden()`
- `CaptureShownPose()`

设计说明：

- 推荐直接挂在页面根或窗口根。
- 如果 `autoCreateTargetCanvasGroup` 打开，会自动补 `CanvasGroup`。
- `HideAndDeactivate()` 适合完全隐藏并禁用页面。
- `PlayHide()` 适合只做视觉收起，但不一定要马上 `SetActive(false)` 的场景。

### 3.6 `UiAnimatedViewBase`

文件：

- `Assets/Res/Scripts/UI/Common/UiAnimatedViewBase.cs`

职责：

- 在 `ViewBase` 的基础上增加“如果根节点存在 `UiTransitionPlayer`，则自动走过渡动画”的能力。

适用场景：

- HUD
- 常驻浮层
- 非窗口型页面壳

设计说明：

- 只接管 `SetViewVisible(bool visible)` 的显示逻辑。
- 不要求强制动画；如果根上没有 `UiTransitionPlayer`，则退回 `ViewBase` 原始行为。

### 3.7 `UiAnimatedWindowBase`

文件：

- `Assets/Res/Scripts/UI/Common/UiAnimatedWindowBase.cs`

职责：

- 在 `WindowBase` 的基础上增加根节点过渡能力。

适用场景：

- 背包窗口
- 对话窗口
- 日志窗口
- 命名输入弹窗

设计说明：

- 行为与 `UiAnimatedViewBase` 一致，只是面向 `WindowBase`。

### 3.8 `UiTabGroup`

文件：

- `Assets/Res/Scripts/UI/Common/UiTabGroup.cs`

职责：

- 提供标准页签切换逻辑。
- 管理按钮点击、默认页、当前页、页面显隐与可选的页面过渡。

适用场景：

- 主菜单 page 切换
- 仓库 / 商店 / 角色页签
- 日志 / 面板 / 二级导航页

每个 `TabEntry` 可配置：

- `id`
- `selectedByDefault`
- `button`
- `pageRoot`
- `pageTransition`

核心能力：

- `Select(int index)`
- `Select(string id)`
- `RefreshSelection()`
- `onTabIndexChanged`
- `onTabIdChanged`

设计说明：

- 若 `pageRoot` 上有 `UiTransitionPlayer`，则优先走页面过渡。
- 若 tab 按钮上有 `UiAnimatedButton`，则会同步切换按钮选中态。

## 4. 推荐 prefab 组合方式

### 4.1 标准按钮

推荐挂载：

- `Button`
- `UiAnimatedButton`

可选补充：

- `CanvasGroup`

适用：

- 普通按钮
- 主操作按钮
- 导航按钮

### 4.2 标准页签

按钮节点推荐挂载：

- `Button`
- `UiAnimatedButton`

页面根节点推荐挂载：

- `RectTransform`
- `CanvasGroup`
- `UiTransitionPlayer`

页签容器推荐挂载：

- `UiTabGroup`

### 4.3 标准列表

列表根推荐挂载：

- `ScrollRect`
- `UiVirtualList`

条目 prefab 推荐挂载：

- `RectTransform`
- `LayoutElement`
- `XxxItemRenderer : UiReusableRenderer<TData>`

### 4.4 标准动画窗口

窗口 prefab 根推荐挂载：

- `RectTransform`
- `CanvasGroup`
- `UiTransitionPlayer`
- `XxxWindowTemplate`

窗口控制器推荐继承：

- `UiAnimatedWindowBase`

### 4.5 标准动画 View / HUD

View prefab 根推荐挂载：

- `RectTransform`
- `CanvasGroup`
- `UiTransitionPlayer`
- `XxxViewTemplate`

控制器推荐继承：

- `UiAnimatedViewBase`

## 5. 典型用法

### 5.1 列表项 renderer

```csharp
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestListItemRenderer : UiReusableRenderer<QuestViewData>
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text summaryText;

    protected override void BindData(QuestViewData data, int index)
    {
        titleText.text = data.Title;
        summaryText.text = data.Summary;
    }

    protected override void UnbindData(QuestViewData data, int index)
    {
        titleText.text = string.Empty;
        summaryText.text = string.Empty;
    }
}
```

### 5.2 配置虚拟列表

```csharp
[SerializeField] private UiVirtualList questList;
[SerializeField] private QuestListItemRenderer questItemPrefab;

private readonly List<QuestViewData> items = new List<QuestViewData>();

private void RefreshQuestList()
{
    questList.SetItems(
        items,
        overrideItemPrefab: questItemPrefab);
}
```

如果条目高度不固定：

```csharp
questList.SetItems(
    items,
    heightResolver: item => item.IsExpanded ? 140f : 88f,
    overrideItemPrefab: questItemPrefab);
```

### 5.3 动画按钮

按钮 prefab 上：

- 挂 `Button`
- 挂 `UiAnimatedButton`
- 设置：
  - `motionTarget`
  - `tintGraphic`
  - `fadeTarget`

如果按钮要作为 tab 使用：

- 打开 `useSelectedStyle`

### 5.4 tab 组

在页签容器上挂 `UiTabGroup`，然后给每个 `TabEntry` 填：

- `button`
- `pageRoot`
- 可选 `pageTransition`

如果按钮上已经挂了 `UiAnimatedButton`：

- `UiTabGroup` 会自动给当前选中 tab 设置选中态

如果页面根上已经挂了 `UiTransitionPlayer`：

- `UiTabGroup` 会自动调用它做切页动画

### 5.5 动画窗口基类

```csharp
public sealed class InventoryWindowController : UiAnimatedWindowBase
{
    protected override PrototypeUiToolkit.WindowChrome CreateWindowChrome()
    {
        // 这里仍然按项目标准去实例化 prefab 和 Template
        return base.CreateWindowChrome();
    }

    protected override void BuildWindow(PrototypeUiToolkit.WindowChrome chrome)
    {
        // 这里只做绑定、初始化、刷新
    }
}
```

要求：

- 窗口 prefab 根节点上应挂 `UiTransitionPlayer`

### 5.6 动画 View 基类

```csharp
public sealed class QuestHudView : UiAnimatedViewBase
{
    protected override void BuildView(RectTransform root)
    {
        // 这里只做 prefab 引用绑定和数据刷新
    }
}
```

要求：

- View prefab 根节点上应挂 `UiTransitionPlayer`

## 6. 推荐开发流程

新增一个正式运行时 UI 时，推荐顺序如下：

1. 先在 Unity Editor 中做好 UGUI prefab。
2. 给 prefab 挂对应 `Template`。
3. 判断这个界面是：
   - `ViewBase` / `UiAnimatedViewBase`
   - `WindowBase` / `UiAnimatedWindowBase`
4. 如果有按钮，优先挂 `UiAnimatedButton`。
5. 如果有页签，优先挂 `UiTabGroup`。
6. 如果有长列表，优先使用 `UiVirtualList + UiReusableRenderer<TData>`。
7. 如果界面需要显隐动画，在根节点挂 `UiTransitionPlayer`。
8. 控制器里只做：
   - 预制体实例化
   - Template 引用绑定
   - 数据刷新
   - 事件注册

## 7. 不推荐的用法

以下做法不推荐继续扩展：

- 在控制器里继续大量 `new GameObject` 搭界面
- 在循环里动态 `AddComponent<Text/Image/Button/...>` 生成条目
- 把复杂页面状态写死在单个巨型 `MonoBehaviour` 中
- 让列表项自己维护全局数据真相
- 让 UI 动效逻辑和业务逻辑完全耦合在一起

## 8. 推荐职责边界

建议维持以下边界：

- `ViewBase / WindowBase`
  - 负责界面生命周期与挂载层级
- `Template`
  - 负责 prefab 引用收口
- `UiWidgetBase`
  - 负责通用组件生命周期
- `UiAnimatedButton / UiTransitionPlayer / UiTabGroup / UiVirtualList`
  - 负责可复用 UI 行为
- 业务控制器
  - 负责数据绑定、事件连接、外部系统协调

## 9. 后续可扩展方向

后续若继续扩展组件库，建议优先补这些方向：

- 通用确认框 / 弹窗控制器
- Toggle / Radio / Filter Group
- 分页器与懒加载列表
- 通用提示条 / Toast 控件
- 标准空状态 / 加载中 / 错误态面板
- 表单输入校验组件

## 10. 小结

这套组件库的目标不是替代 `ViewBase / WindowBase / Template` 的 prefab 工作流，而是让这套工作流更稳定、更少重复代码、更容易统一交互表现。

使用时优先记住三条：

1. 先做 prefab，再挂通用组件。
2. 控制器负责数据和事件，不负责手搓整页布局。
3. 动画、页签、列表复用都优先走通用组件，不要每个系统各写一套。
