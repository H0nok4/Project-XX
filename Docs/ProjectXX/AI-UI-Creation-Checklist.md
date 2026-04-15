# AI UI Creation Checklist

## 1. 用途

这份文档是给后续 AI 在 Project-XX 中制作 UI 时使用的硬约束清单。

目标只有一个：

- 让 AI 做出来的 UI 预制体、脚本、打开关闭方式、资源路径和模板结构保持统一

如果和长版规范冲突，以这份清单和当前框架代码为准。

相关基础框架：

- `Assets/Res/Scripts/UI/ViewBase.cs`
- `Assets/Res/Scripts/UI/WindowBase.cs`
- `Assets/Res/Scripts/UI/PrefabViewBase.cs`
- `Assets/Res/Scripts/UI/PrefabWindowBase.cs`
- `Assets/Res/Scripts/UI/Common/UiWidgetBase.cs`
- `Assets/Res/Scripts/UI/Common/UiReusableRendererBase.cs`
- `Assets/Res/Scripts/UI/UiRouter.cs`
- `Assets/Res/Scripts/UI/UiWindowService.cs`
- `Assets/Res/Scripts/UI/UiInputService.cs`
- `Assets/Res/Scripts/UI/UiPrefabRegistry.cs`

---

## 2. AI 必须先判断的事

做任何 UI 之前，先判断它属于哪一类。

### 2.1 窗口类 UI

特征：

- 会弹出
- 会关闭
- 会遮罩下层
- 属于日志、背包、商店、确认框、弹窗、对话框

AI 必须：

- 优先继承 `PrefabWindowBase<TTemplate>`
- 只有非常特殊时才直接继承 `WindowBase`

### 2.2 界面类 UI

特征：

- 是整页或 HUD
- 常驻
- 强调显示 / 隐藏而不是弹窗

AI 必须：

- 优先继承 `PrefabViewBase<TTemplate>`
- 只有非常特殊时才直接继承 `ViewBase`

### 2.3 通用控件

特征：

- 只是页面的一部分
- 不单独承担顶层 UI 生命周期

AI 必须：

- 继承 `UiWidgetBase`

### 2.4 列表项

特征：

- 会被滚动列表反复复用
- 是任务项、背包项、商店项、条目卡片

AI 必须：

- 继承 `UiReusableRenderer<T>`

---

## 3. AI 制作 UI 的硬规则

### 3.1 基类规则

- 窗口类 UI 不要直接写成普通 `MonoBehaviour`
- 界面类 UI 不要直接写成普通 `MonoBehaviour`
- 小控件不要误继承 `ViewBase` 或 `WindowBase`
- 列表项不要误写成普通 `MonoBehaviour`

### 3.2 Prefab 规则

- 正式运行时 UI prefab 必须放在 `Assets/Resources/UI/<Feature>/`
- prefab 根节点必须是 `RectTransform`
- 正式 UI prefab 根节点必须挂 `*Template`
- 同一业务 UI 只能保留一个正式 prefab 入口

### 3.3 Template 规则

- `Template` 只负责引用绑定
- `Template` 不写业务逻辑
- `Template` 不访问服务
- `Template` 不做输入判断
- `Template` 不做资源加载
- `Template` 不写 `Update`

### 3.4 打开关闭规则

- 不要在业务代码里直接 `SetActive(true/false)` 控制 `View` / `Window`
- 窗口类统一走 `ShowWindow / HideWindow / SetWindowVisible`
- 界面类统一走 `ShowView / HideView / SetViewVisible`
- 外部业务调用优先走 `UiRouter`

### 3.5 输入规则

- 不要给每个 UI 自己写一套长期存在的 `Keyboard.current` 轮询方案
- Submit / Cancel 优先接到 `UiInputService`
- 顶层输入处理优先接到 `TryHandleUiSubmit / TryHandleUiCancel`
- 需要进统一栈的界面必须明确 `RegistersWithUiWindowService`

### 3.6 资源加载规则

- 不要在业务 UI 脚本里到处写裸 `Resources.Load("路径")`
- 优先通过 `UiPrefabRegistry` 加载
- `PrefabViewBase / PrefabWindowBase` 已经内置 registry 优先加载

### 3.7 查找规则

- 不要在业务 UI 代码里到处 `FindFirstObjectByType`
- 获取 / 创建 UI 优先走 `UiRouter.GetOrCreate<T>()`
- 打开 UI 优先走 `UiRouter.OpenWindow<T>()` 或 `UiRouter.OpenView<T>()`

### 3.8 代码边界规则

- `View` / `Window` 负责 UI 行为，不负责重领域逻辑
- 领域状态变化应由 Service / Controller / Presenter 推进
- 不要把复杂业务直接塞进按钮回调

---

## 4. AI 标准工作流

AI 以后做 UI，必须按这个顺序执行。

### 第 1 步：确定 UI 类型

先判断是：

- `Window`
- `View`
- `Widget`
- `ReusableRenderer`

### 第 2 步：确定 Feature 目录

Prefab 放到：

- `Assets/Resources/UI/<Feature>/`

脚本优先放到：

- `Assets/Res/Scripts/UI/...`

### 第 3 步：先做 prefab 和 template

必须先有：

- prefab
- root `RectTransform`
- `*Template`
- 所有需要的 `TMP_Text / Button / Image / ScrollRect / RectTransform` 引用

### 第 4 步：再写脚本

优先模式：

- `PrefabWindowBase<TTemplate>`
- `PrefabViewBase<TTemplate>`

脚本中应只做：

- 绑定 template
- 初始化静态文案
- 按钮事件绑定
- 输入桥接
- 显示隐藏
- 数据刷新

### 第 5 步：接入路由和注册表

必须检查：

- 是否能通过 `UiRouter` 获取
- 是否能通过 `UiRouter` 打开 / 关闭
- prefab 是否已进入 `UiPrefabRegistry`

### 第 6 步：做自检

至少检查：

- 能打开
- 能关闭
- 重复打开不报错
- 重复关闭不报错
- 焦点正常
- Submit / Cancel 正常
- 文案超长不破版
- 空数据不报错
- prefab 引用没有漏

---

## 5. AI 不允许做的事

以下做法默认禁止：

- 同一份业务 UI 同时做 `XxxView` 和 `XxxWindowController` 两套正式实现
- 把 `Template` 写成业务逻辑类
- 在 `BuildView / BuildWindow` 里动态拼完整复杂业务 UI 层级
- 在 UI 里到处直接 `FindFirstObjectByType`
- 在 UI 里到处直接 `Resources.Load`
- 在正式 UI 中长期保留各自 `Update()` 键盘轮询
- 在列表刷新中反复 `Instantiate / Destroy`
- 绕开 `UiRouter` 直接在业务层创建一堆顶层 UI 实例
- 直接扩展第三方包自带旧 UI 当正式产品方案

---

## 6. AI 制作脚本命名规范

- 界面类：`XxxView`
- 窗口类：`XxxWindowController`
- 模板类：`XxxTemplate`
- 控件类：`XxxWidget`
- 列表项：`XxxItemRenderer`
- 数据类：`XxxContent` 或 `XxxViewModel`

不允许：

- `TestWindow`
- `TempView`
- `NewPanel`
- `Panel1`

---

## 7. AI 制作 prefab 命名规范

- 窗口 prefab：`XxxWindow.prefab`
- 界面 prefab：`XxxView.prefab` 或已有业务名
- 控件 prefab：`XxxWidget.prefab`
- 列表项 prefab：`XxxItem.prefab`

如果项目已有固定业务名称，也可以沿用业务名，但必须保持稳定，不要一会儿一个名字。

---

## 8. AI 提交前自检清单

每次 AI 完成 UI 后，必须自检并在说明里覆盖这些点：

- 这是 `Window`、`View`、`Widget` 还是 `ReusableRenderer`？
- 继承基类是否正确？
- prefab 是否放在 `Assets/Resources/UI/<Feature>/`？
- 是否有 `Template`？
- `Template` 是否只负责引用？
- 是否用了 `PrefabViewBase<TTemplate>` 或 `PrefabWindowBase<TTemplate>`？
- 是否通过 `UiRouter` 打开 / 关闭？
- 是否接入 `UiPrefabRegistry`？
- Submit / Cancel 是否走统一入口？
- 是否避免了 `FindFirstObjectByType` 滥用？
- 是否避免了裸 `Resources.Load` 滥用？
- 是否检查过空状态、长文本、重复打开关闭？

---

## 9. AI 的默认实现偏好

如果没有特别说明，AI 应默认这样实现：

- 顶层窗口：`PrefabWindowBase<TTemplate>`
- 顶层界面：`PrefabViewBase<TTemplate>`
- 通用控件：`UiWidgetBase`
- 列表：`UiVirtualList`
- 列表项：`UiReusableRenderer<T>`
- 打开关闭：`UiRouter`
- 顶层输入：`UiInputService + UiWindowService`
- prefab 加载：`UiPrefabRegistry`

---

## 10. 一句话执行标准

以后 AI 在 Project-XX 做 UI 时，默认标准就是：

- 先分类型
- 再做 prefab 和 template
- 用正确基类
- 用 `UiRouter` 打开关闭
- 用 `UiPrefabRegistry` 管 prefab
- 用统一输入处理 Submit / Cancel
- 不把逻辑塞进 template
- 不制造第二套重复 UI 入口

如果 AI 做出来的 UI 违反上面任一条，就视为不符合项目规范。
