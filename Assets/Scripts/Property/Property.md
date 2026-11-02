# Property

配置集成模块。提供一个统一的 Inspector 面板，将分散在多个脚本上的变量集中到一处管理，并在运行时自动推送值到各目标组件。

---

## ConfigHub.cs

挂载在任意 GameObject 上的运行时组件，持有所有托管变量的当前值，并通过反射写入目标组件。

### 数据结构

四种内嵌可序列化类，分别对应四种基础类型，结构相同：

| 字段 | 说明 |
|------|------|
| `target` | 目标组件引用 |
| `fieldName` | 目标字段名（字符串） |
| `value` | 在 ConfigHub 上集中保存的值 |

四个列表分别持有各类型的全部条目：`intEntries` / `floatEntries` / `boolEntries` / `stringEntries`。

### 公开方法

| 方法 | 功能 |
|------|------|
| `PushAll()` | 遍历四个列表，逐条调用 `Push()` 将值写入目标组件。`Awake` 和 `OnValidate` 时自动调用；Inspector 中"立即推送"按钮也直接调用此方法。 |

### 关键私有方法

#### `Push(Component, string, object)`（静态）

通过反射在目标组件上查找指定字段名，用 `Convert.ChangeType` 进行类型转换后调用 `FieldInfo.SetValue` 写入。支持 `public` 字段和带 `[SerializeField]` 的私有字段。目标为空或字段不存在时输出警告，类型转换失败时输出错误。

### 同步时机

| 时机 | 说明 |
|------|------|
| `Awake`（运行时） | 游戏启动后立即推送一次，保证运行时各组件使用 ConfigHub 中设定的值 |
| `OnValidate`（编辑器） | Inspector 中修改任意值后立即推送到目标组件，实时预览效果 |
| 手动点击"立即推送"按钮 | 由 `ConfigHubEditor` 提供的按钮，强制触发一次全量推送 |

---

## ConfigHubEditor.cs（Assets/Editor）

`ConfigHub` 的自定义编辑器，提供可视化的添加/删除/展示界面。

### 展示区（DrawSection）

按 `int / float / bool / string` 分区显示所有已添加条目。每条目一行，显示 `组件名.字段名`（作为标签）和值输入框，右侧有删除按钮（×）。

- 若原始字段带有 `[Header]`，在该条目上方渲染灰色小标题
- 若带有 `[Tooltip]`，鼠标悬停标签时显示提示文字

### 添加区（DrawAddArea）

分四步选择要托管的字段：

1. **目标物体**：拖入场景中的 GameObject
2. **目标组件**：从该物体所有组件中下拉选择（解决直接拖 Component 只能取到 Transform 的问题）
3. **变量类型**：`int / float / bool / string`
4. **选择字段**：仅列出该组件上符合所选类型、且可被 Unity 序列化（`public` 或 `[SerializeField]`，排除 `[HideInInspector]` 和 `[NonSerialized]`）的字段

点击"添加"后读取目标组件上该字段的当前值作为初始值，并通过 `InsertGrouped()` 将新条目插入到同一组件的最后一个条目之后（同组件的字段聚集排列）。操作支持 Undo。

### 关键私有方法

#### `InsertGrouped<T>(List<T>, T, Func<T, Component>)`（静态）

从列表末尾向前查找最后一个与新条目 `target` 相同的条目，将新条目插入其正后方。若列表中尚无该组件的条目，则追加到末尾。保证同一脚本的托管字段在列表中始终相邻。

---

## 与外部脚本的关联

ConfigHub 本身不依赖任何特定业务脚本。它通过反射与**任意**挂载在场景物体上的组件交互，字段名以字符串存储，因此对目标脚本无编译期依赖。

实际使用中，任何需要集中管理的 Inspector 变量（如各 Manager 的阈值、概率参数等）均可托管到 ConfigHub，由此脚本统一保存和分发。
