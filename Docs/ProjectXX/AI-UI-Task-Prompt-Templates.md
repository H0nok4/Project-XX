# AI UI Task Prompt Templates

## 1. 用途

这份文档提供给 Project-XX 后续使用 AI 制作 UI 时的标准提示模板。

目标：

- 让 AI 按当前 UI 框架统一方式落地
- 减少每次重新解释 `WindowBase / ViewBase / Template / UiRouter / UiPrefabRegistry`
- 让 AI 输出更稳定，少走偏路

配套文档：

- `Docs/ProjectXX/ProjectXX-UI-Framework-Guidelines.md`
- `Docs/ProjectXX/AI-UI-Creation-Checklist.md`

---

## 2. 通用主模板

以后给 AI 提 UI 任务时，优先用这个模板。

```md
请基于 Project-XX 当前 UI 框架制作一个【UI名称】。

必须遵守：

1. 先判断这个 UI 属于 `Window`、`View`、`Widget` 还是 `UiReusableRenderer<T>`。
2. 如果是窗口类，优先继承 `PrefabWindowBase<TTemplate>`。
3. 如果是界面类，优先继承 `PrefabViewBase<TTemplate>`。
4. prefab 必须放在 `Assets/Resources/UI/<Feature>/`。
5. 脚本必须放在 `Assets/Res/Scripts/UI/...`。
6. 必须使用 `*Template` 收口 prefab 引用。
7. 不要把业务逻辑写进 `Template`。
8. 打开关闭优先走 `UiRouter`。
9. prefab 加载优先走 `UiPrefabRegistry`。
10. 不要在业务 UI 中滥用 `FindFirstObjectByType`、裸 `Resources.Load`、直接 `SetActive` 管理顶层 UI。

这次任务目标：

- UI 类型：
- 所属 Feature 目录：
- 需要的 prefab：
- 需要的脚本：
- 是否需要接入 `UiWindowService`：
- 是否需要处理 `Submit / Cancel`：
- 是否需要列表：
- 是否需要使用 `UiVirtualList`：

交付要求：

- 创建或修改 prefab
- 创建或修改 `Template`
- 创建或修改 UI 脚本
- 如有必要，接入 `UiPrefabRegistry`
- 说明这个 UI 为什么选用该基类
- 说明打开关闭入口应该如何使用

额外要求：

- 文案先用占位内容即可，但结构要正确
- 按当前项目命名规范命名
- 做完后给出简短自检结果
```

---

## 3. 窗口类模板

适用于：

- 背包
- 商店
- 任务日志
- 设施详情
- 对话框
- 设置窗口

```md
请基于 Project-XX 当前 UI 框架制作一个窗口类 UI：【窗口名称】。

必须：

- 优先继承 `PrefabWindowBase<TTemplate>`
- prefab 放在 `Assets/Resources/UI/<Feature>/`
- 模板类命名为 `XxxTemplate`
- 控制类命名为 `XxxWindowController`
- 如果需要顶层输入，使用 `TryHandleUiSubmit / TryHandleUiCancel`
- 打开关闭方式要兼容 `UiRouter.OpenWindow<T>() / CloseWindow<T>()`

这次窗口需求：

- 窗口标题：
- 窗口副标题：
- 是否允许取消：
- 是否需要确认按钮：
- 是否需要滚动列表：
- 是否需要遮罩：
- 是否需要 footer 区域：

交付：

- `XxxWindow.prefab`
- `XxxTemplate.cs`
- `XxxWindowController.cs`

不要做：

- 不要把业务逻辑写进 template
- 不要直接写成普通 `MonoBehaviour`
- 不要自己重新造一套打开关闭机制
```

---

## 4. 界面类模板

适用于：

- HUD
- BaseHub Overlay
- Main Menu 主界面
- 常驻追踪 UI

```md
请基于 Project-XX 当前 UI 框架制作一个界面类 UI：【界面名称】。

必须：

- 优先继承 `PrefabViewBase<TTemplate>`
- prefab 放在 `Assets/Resources/UI/<Feature>/`
- 模板类命名为 `XxxTemplate`
- 界面类脚本命名为 `XxxView`
- 如果需要接顶层 Submit / Cancel，必须显式说明是否接入 `UiWindowService`
- 显示隐藏走 `ShowView / HideView / SetViewVisible`

这次界面需求：

- 所属 Layer：
- 是否常驻：
- 是否默认显示：
- 是否需要接任务/状态刷新：
- 是否需要按钮区：
- 是否需要小地图/状态条/文本块：

交付：

- `XxxView.prefab`
- `XxxTemplate.cs`
- `XxxView.cs`

不要做：

- 不要误用 `WindowBase`
- 不要把整个页面逻辑塞进 `Update`
- 不要直接用旧包自带 HUD 当正式方案
```

---

## 5. 列表类模板

适用于：

- 背包列表
- 商店列表
- 任务列表
- 对话选项列表

```md
请基于 Project-XX 当前 UI 框架制作一个列表 UI：【列表名称】。

必须：

- 如果条目数量可能较多，优先使用 `UiVirtualList`
- 列表项优先继承 `UiReusableRenderer<T>`
- 列表项 prefab 命名为 `XxxItem.prefab`
- 列表项脚本命名为 `XxxItemRenderer`
- 列表项只负责显示，不负责重业务逻辑

这次列表需求：

- 列表宿主是 `Window` 还是 `View`：
- 数据类型：
- 是否需要虚拟化：
- 每项大致结构：
- 是否支持点击：
- 是否支持选中状态：
- 是否需要动态高度：

交付：

- 列表宿主 UI
- 列表项 prefab
- 列表项 renderer
- 绑定方式说明

不要做：

- 不要每次刷新都 `Instantiate / Destroy`
- 不要让列表项直接依赖全局单例服务
```

---

## 6. 通用控件模板

适用于：

- 标签页
- 条形血量
- 通用按钮组
- 交互提示条

```md
请基于 Project-XX 当前 UI 框架制作一个通用控件：【控件名称】。

必须：

- 继承 `UiWidgetBase`
- 如果是列表项则改用 `UiReusableRenderer<T>`
- 这个控件必须可被多个页面复用
- 控件脚本只处理局部 UI 状态

这次控件需求：

- 控件功能：
- 输入参数：
- 刷新方式：
- 是否需要动画：
- 是否需要 CanvasGroup：

交付：

- `XxxWidget.prefab` 或对应结构
- `XxxWidget.cs`
- 必要时补充示例接入方式
```

---

## 7. 给 AI 的固定附加语句

以后你发 UI 任务时，建议把下面这段固定附在最后：

```md
补充要求：

- 严格遵守 `Docs/ProjectXX/ProjectXX-UI-Framework-Guidelines.md`
- 严格遵守 `Docs/ProjectXX/AI-UI-Creation-Checklist.md`
- 新 UI 必须符合 Project-XX 当前 UI 框架，不要自行发明另一套体系
- 做完后说明：
  1. 这个 UI 属于哪一类
  2. 为什么选这个基类
  3. prefab 放在哪
  4. 如何打开关闭
  5. 是否接入了 `UiPrefabRegistry`
  6. 是否接入了 `UiWindowService / UiInputService`
```

---

## 8. 实战示例

## 8.1 示例：让 AI 做商店窗口

```md
请基于 Project-XX 当前 UI 框架制作一个窗口类 UI：MerchantShopWindow。

必须遵守：

1. 优先继承 `PrefabWindowBase<TTemplate>`。
2. prefab 放在 `Assets/Resources/UI/Merchant/`。
3. 使用 `MerchantShopWindowTemplate` 收口所有引用。
4. 控制类命名为 `MerchantShopWindowController`。
5. 打开关闭兼容 `UiRouter.OpenWindow<MerchantShopWindowController>()`。
6. 商品列表如果条目较多，优先使用 `UiVirtualList`。
7. 不要把业务逻辑写进 template。

这次窗口需求：

- 标题：商店
- 副标题：浏览并购买商品
- 左侧是商人信息
- 中间是商品列表
- 右侧是商品详情
- 底部有关闭按钮
- 支持 Cancel 关闭

交付要求：

- prefab
- template
- window controller
- 商品列表 renderer
- 简短说明如何打开关闭
```

## 8.2 示例：让 AI 做 Raid HUD

```md
请基于 Project-XX 当前 UI 框架制作一个界面类 UI：RaidHudView。

必须遵守：

1. 优先继承 `PrefabViewBase<TTemplate>`。
2. prefab 放在 `Assets/Resources/UI/Raid/`。
3. 使用 `RaidHudTemplate` 收口引用。
4. `RaidHudView` 只负责显示和刷新，不承载重领域逻辑。
5. 不要使用旧包自带 HUD 当正式方案。

这次界面需求：

- 左上显示任务目标
- 右上显示状态文本
- 左下显示生命、体力、弹药
- 中间可显示交互提示
- 默认显示

交付要求：

- prefab
- template
- view 脚本
- 刷新接口说明
```

---

## 9. 一句话用法

以后你让 AI 做 UI，最省事的方式是：

1. 先选本文件里的一个模板
2. 把 UI 名称和需求填进去
3. 再追加项目业务要求
4. 最后附上“固定附加语句”

这样 AI 大概率会按当前 Project-XX UI 框架稳定落地，而不是重新发明一套 UI 体系。

---

## 10. 如果希望 AI 在做功能时自动按 UI 规范工作

如果你的目标不是“单独让 AI 做一个 UI”，而是：

- 让 AI 在开发某个功能时
- 只要它判断这个功能需要新 UI 或修改现有 UI
- 就自动按 Project-XX UI 规范落地

那最有效的做法不是临时提醒一句“顺便注意 UI”，而是给 AI 一个固定的“功能开发总约束”。

### 10.1 推荐做法

以后你给 AI 提任何功能任务时，在任务末尾固定追加下面这段：

```md
额外硬约束：

如果这个功能在实现过程中需要新增、修改或接入运行时 UI，AI 必须自动按 Project-XX 当前 UI 规范执行，不需要再额外等我提醒。

具体要求：

1. 先判断新增内容属于 `Window`、`View`、`Widget` 还是 `UiReusableRenderer<T>`。
2. 窗口类 UI 优先使用 `PrefabWindowBase<TTemplate>`。
3. 界面类 UI 优先使用 `PrefabViewBase<TTemplate>`。
4. prefab 必须放在 `Assets/Resources/UI/<Feature>/`。
5. 需要独立 `*Template` 收口引用。
6. 打开关闭优先走 `UiRouter`。
7. 顶层输入优先接 `UiInputService / UiWindowService`。
8. prefab 加载优先接 `UiPrefabRegistry`。
9. 不要在业务 UI 中滥用 `FindFirstObjectByType`、裸 `Resources.Load`、直接 `SetActive` 控制顶层 UI。
10. 做完后必须在结果里说明：
   - 为什么需要 UI
   - UI 属于哪一类
   - 选了哪个基类
   - prefab 放在哪
   - 如何打开关闭

如果当前功能不需要 UI，就不要为了符合规范强行做 UI。

严格遵守：

- `Docs/ProjectXX/ProjectXX-UI-Framework-Guidelines.md`
- `Docs/ProjectXX/AI-UI-Creation-Checklist.md`
- `Docs/ProjectXX/AI-UI-Task-Prompt-Templates.md`
```

### 10.2 这段话的作用

它会把 AI 的行为从：

- “只有你明确说做 UI 时才切 UI 规范”

变成：

- “只要功能实现里需要 UI，就自动进入 UI 规范模式”

这才是最适合“功能开发过程中顺带做 UI”的用法。

---

## 11. 功能开发总提示模板

如果你想一步到位，可以直接把下面这段作为“功能开发类任务”的默认提示模板。

```md
请帮我完成这个功能需求，并直接落地实现。

通用要求：

1. 先基于现有代码和框架实现，不要重新发明体系。
2. 如果功能实现过程中需要新增、修改或接入运行时 UI，必须自动按 Project-XX 当前 UI 规范执行。
3. 不需要为了有 UI 而做 UI；只有功能确实需要 UI 时才落地。
4. 如果涉及 UI，必须自动完成：
   - UI 类型判断：`Window` / `View` / `Widget` / `UiReusableRenderer<T>`
   - 正确基类选择
   - prefab 与脚本目录落位
   - `Template` 收口引用
   - `UiRouter` 打开关闭接入
   - 必要时接 `UiWindowService / UiInputService`
   - 必要时接 `UiPrefabRegistry`
5. 最终说明里如果涉及 UI，必须补充：
   - 做了哪些 UI
   - 为什么这些 UI 需要存在
   - 用了哪个基类
   - prefab 放在哪
   - 如何打开关闭

严格遵守：

- `Docs/ProjectXX/ProjectXX-UI-Framework-Guidelines.md`
- `Docs/ProjectXX/AI-UI-Creation-Checklist.md`
- `Docs/ProjectXX/AI-UI-Task-Prompt-Templates.md`

下面是具体功能需求：

【在这里写功能需求】
```

---

## 12. 最推荐的实际使用方式

以后你可以这样分三层使用：

### 层 1：项目长期规则

始终让 AI 知道这三份文档存在：

- `ProjectXX-UI-Framework-Guidelines.md`
- `AI-UI-Creation-Checklist.md`
- `AI-UI-Task-Prompt-Templates.md`

### 层 2：功能任务附加约束

每次功能需求后面固定追加“额外硬约束”那段。

### 层 3：需要精确 UI 时再加专门模板

如果这次任务本来就是以 UI 为主，再额外从本文件第 2 到 8 节里挑对应模板贴进去。

这样就能形成：

- 普通功能任务：AI 自动遵守 UI 规范
- 纯 UI 任务：AI 按更细的 UI 模板执行

---

## 13. 一句话建议

如果你想让 AI 在“做功能的过程中”自动按规范做 UI，最关键的不是再写更多 UI 规则，而是：

- 把“只要功能需要 UI，就自动切到 UI 规范模式”写成每次功能任务都固定附加的硬约束

这样 AI 才会把 UI 规范当成默认工作流，而不是可选提醒。
