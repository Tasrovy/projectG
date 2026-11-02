# Date

日期数据模块，负责从 Excel 加载日期配置，并在日历 UI 点击或游戏初始化时将对应日期的待办文本填充到 UI。

---

## DateDataSO.cs

纯数据容器，无逻辑。

### DateData（可序列化类）

单条日期记录，对应 `date.xlsx` 的一行。

| 字段 | 类型 | 说明 |
|------|------|------|
| `day` | `int` | 游戏内天数编号，与 `DayManager.dayNumber` 对应 |
| `date` | `string` | 现实日期键，格式 `MM_dd`（如 `05_16`），供日历点击时查找 |
| `morning` | `string` | 上午待办文本 |
| `afternoon` | `string` | 下午待办文本 |
| `afterclass` | `string` | 课后待办文本 |
| `text` | `string` | 当天描述文字 |

### DateDataSO（ScriptableObject）

持有 `List<DateData> dateDatas`，由 `ExcelLoader.ReadDateExcel()` 写入，运行时由 `DateManager` 持有引用。

---

## DateManager.cs

单例（`Singleton<DateManager>`，`IsPersistent = false`），场景级生命周期。负责加载日期数据并将文本填充到 UI。

### 初始化流程

1. `Awake`：调用 `ExcelLoader.Instance.ReadDateExcel()` 加载 `date.xlsx` 到 `dateSO`，随后调用 `ResolveReferences()` 自动绑定四个 TMP 引用。
2. `Start`：调用 `ShowCurrentDayData()` 显示当前游戏天对应的待办数据。

### 公开方法

| 方法 | 签名 | 功能 |
|------|------|------|
| `OnDateClicked` | `OnDateClicked(DateTime date)` | 日历格子点击的外部入口。将 `DateTime` 转换为 `MM_dd` 格式的键，在 `dateSO` 中查找匹配的 `DateData`，找到则填充四个文本，否则清空。由 `CalendarPopup.OnDateClicked()` 调用。 |

### 关键私有方法

| 方法 | 功能 |
|------|------|
| `ResolveReferences()` | 在场景根级 Canvas 下按路径 `properties/calendar/window/todoList` 查找四个 TMP 子物体（`morning_todo` / `afternoon_todo` / `afterclass_todo` / `dayDesc`）并缓存引用。支持父物体处于 inactive 状态时仍能找到。Inspector 已手动拖入的字段不会被覆盖。 |
| `ShowCurrentDayData()` | 用 `DayManager.GetDayNumber()` 获取当前游戏天数，在 `dateSO.dateDatas` 中按 `day` 字段查找并填充文本。用于游戏启动时的初始显示。 |
| `ProcessRichText(string)` | 将 Excel 单元格中字面量 `\n` 替换为真正的换行符，其余 TMP 富文本标签（`<b>`、`<size>` 等）直接透传。 |

### UI 文本绑定目标

Canvas 下路径 `properties/calendar/window/todoList` 的四个子物体：

| 子物体名 | 对应字段 |
|----------|----------|
| `morning_todo` | `DateData.morning` |
| `afternoon_todo` | `DateData.afternoon` |
| `afterclass_todo` | `DateData.afterclass` |
| `dayDesc` | `DateData.text` |

---

## 脚本间关联

```
DateManager
  ├─ dateSO (DateDataSO)  ←  ExcelLoader.ReadDateExcel() 写入
  ├─ ShowCurrentDayData()  →  读取 DateData.day 字段匹配
  └─ OnDateClicked()       →  读取 DateData.date 字段匹配
```

---

## 与外部脚本的关联

| 外部脚本 | 调用方向 | 说明 |
|----------|----------|------|
| **ExcelLoader** | `DateManager` → `ExcelLoader` | `Awake` 中调用 `ReadDateExcel("date.xlsx")` 生成 `DateDataSO` |
| **DayManager** | `DateManager` → `DayManager` | `ShowCurrentDayData()` 中调用 `GetDayNumber()` 获取当前游戏天数 |
| **CalendarPopup** | `CalendarPopup` → `DateManager` | 日历格子点击后调用 `DateManager.Instance.OnDateClicked(DateTime)` |
