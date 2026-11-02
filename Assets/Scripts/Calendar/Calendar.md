# Calendar

日历弹窗功能模块，负责展示月历格子、翻月导航，以及点击日期后展示当天待办数据。

---

## CalendarPopup.cs

月历弹窗的主控脚本，挂载在日历弹窗根物体上。

### 功能概述

- `OnEnable` 时从 `DayManager` 获取游戏当前日期，以该月为初始视图渲染月历。
- 管理 `viewYear` / `viewMonth` 两个视图状态变量，驱动格子的整体重建。

### 公开方法

| 方法 | 功能 |
|------|------|
| `ShowPreviousMonth()` | 将视图月份回退一个月，重建日历。由 Inspector 中"上一月"按钮的 OnClick 调用。 |
| `ShowNextMonth()` | 将视图月份前进一个月，重建日历。由 Inspector 中"下一月"按钮的 OnClick 调用。 |

### 关键私有方法

| 方法 | 功能 |
|------|------|
| `RebuildCalendar()` | 清空 `btnGrid` 下所有旧格子，按当月实际日历布局（固定 42 格，周日起始）实例化 `dayCell` 预制体，逐一调用 `CalendarDayCell.Bind()` 完成绑定。同步更新标题文字（`viewYear.viewMonth`）。 |
| `OnDateClicked(DateTime)` | 格子点击回调。激活 `todoList` 子物体并将点击日期转发给 `DateManager.Instance.OnDateClicked()`。 |

### 子物体依赖（运行时查找）

| 路径 | 用途 |
|------|------|
| `btnGrid` | 格子容器，所有 `dayCell` 的父物体 |
| `texts/curMonth` | 显示当前视图年月的 TMP 标题 |
| `todoList` | 点击日期后激活的待办列表面板 |
| `Resources/Prefabs/Calender/dayCell` | 日期格子预制体（含 `CalendarDayCell` 组件） |

---

## CalendarDayCell.cs

单个日期格子的脚本，挂载在 `dayCell` 预制体上。

### 功能概述

- 通过 `Bind()` 接收日期数据与点击回调后自我渲染。
- 维护一个 `static currentSelected` 全局引用，保证同一时刻只有一个格子处于选中状态。
- 根据"今日 / 选中 / 普通"三种状态应用不同颜色。

### 公开方法

| 方法 | 签名 | 功能 |
|------|------|------|
| `Bind` | `Bind(DateTime date, bool active, Action<DateTime> clickAction)` | 初始化格子。`active=false` 时隐藏格子（用于填充月首/月末的空白位）；`active=true` 时设置日期文字、判断是否为今日、注册点击事件。点击时恢复上一个选中格子的视觉状态，将自身置为选中，并调用传入的 `clickAction`（即 `CalendarPopup.OnDateClicked`）。 |

---

## 脚本间关联

```
CalendarPopup
  ├─ RebuildCalendar()  →  实例化 dayCell 预制体
  │                         并调用 CalendarDayCell.Bind()
  └─ OnDateClicked()    →  CalendarDayCell 点击后的回调入口

CalendarDayCell
  └─ Bind() 内 clickAction  →  回调到 CalendarPopup.OnDateClicked()
```

---

## 与外部脚本的关联

| 外部脚本 | 调用点 | 说明 |
|----------|--------|------|
| **DayManager** | `CalendarPopup.OnEnable()` | 调用 `GetCurrentDate()` 获取游戏当前日期作为初始视图 |
| **DayManager** | `CalendarDayCell.Bind()` | 调用 `GetCurrentDate()` 判断格子是否为"今日"以应用红色高亮 |
| **DateManager** | `CalendarPopup.OnDateClicked()` | 调用 `OnDateClicked(DateTime)` 将选中日期传入，由 `DateManager` 负责查找并填充 `todoList` 四个文本字段（`morning_todo`、`afternoon_todo`、`afterclass_todo`、`dayDesc`） |
