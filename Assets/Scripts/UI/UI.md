# UI

通用 UI 工具模块。负责场景切换过渡动画和玩家属性面板的显示与刷新。

---

## TransitionManager.cs

继承 `Singleton<TransitionManager>`，负责全屏黑幕过渡动画。在 `Awake` 时以代码动态创建一个 `DontDestroyOnLoad` 的顶层 Canvas（`sortingOrder = 9999`），其中放置全屏黑色 `Image`，通过 `CanvasGroup.alpha` 控制淡入淡出。

### 关键字段

| 字段 | 说明 |
|------|------|
| `transitionDuration` | 淡入 / 淡出各自的持续时间（默认 0.5 秒） |
| `midWaitDuration` | 全黑时在回调执行后额外停留的时间（默认 0.2 秒） |
| `isTransitioning` | 防重入标志，避免同时触发两次转场 |

### 公开方法

#### `PlayTransition(Action onMidPoint = null)`（协程）

完整过渡流程分三阶段：

1. **淡入变黑**：在 `transitionDuration` 内将 `CanvasGroup.alpha` 从 0 升至 1，同时开启 `blocksRaycasts` 阻挡玩家输入
2. **执行回调**：画面全黑时调用 `onMidPoint`（场景切换、UI 替换等"暗箱操作"在此进行）；随后等待 `midWaitDuration`
3. **淡出还原**：将 `alpha` 从 1 降至 0，关闭 `blocksRaycasts`

若过渡正在进行中（`isTransitioning == true`），则跳过动画直接执行回调，防止逻辑阻塞。

每次过渡开始时调用 `DialogueUIAudio.Instance.PlayChangeSceneAudio()` 播放切场音效。

### 私有方法

#### `Initialize()`

幂等初始化。检测 `transitionGroup` 是否已存在，若无则动态创建 Canvas 层级结构。通常只在首次 `Awake` 时执行一次，但 `PlayTransition` 内也有防御性调用。

---

## PropertiesShow.cs

手动单例（`static Instance`），挂载在属性面板 UI GameObject 上，负责将 `DataManager` 中的玩家属性数值显示到四个属性图标（`propIcon_1~4`），并在顶部显示本局攻略目标及下次检定信息。

### 属性图标结构

每个 `propIcon_N` 子物体下包含一个 `Slider`（进度条）和一个名为 `num` 的文字节点。文字节点同时查找 `TMP_Text` 和旧版 `Text`，兼容两种组件。对应属性：

| 图标 | 数据来源 |
|------|---------|
| `propIcon_1` | `DataManager.nature1` |
| `propIcon_2` | `DataManager.nature2` |
| `propIcon_3` | `DataManager.nature3` |
| `propIcon_4` | `DataManager.GetCharm()`（魅力，浮点数） |

### 公开方法

| 方法 | 说明 |
|------|------|
| `UpdatePropertiesShow()` | 从 `DataManager` 读取四个属性值，刷新所有 Slider 和数字文字 |
| `InitializeRandomTargetByEvent()` | 由 UI 的 UnityEvent 调用，随机生成攻略目标类型（1~4），写入 `DayManager.SetTargetType`，并刷新目标文字 |

### 事件订阅

`OnEnable` 时订阅 `DayManager.OnDayAdvanced`，绑定 `RefreshTargetText` 和 `UpdatePropertiesShow`；`OnDisable` 时取消订阅。每次过天后自动同步显示。

### 关键私有方法

#### `RefreshTargetText()`

从 `DayManager.TargetType` 读取攻略目标类型，将目标名称（友情羁绊 / 情绪依赖 / 安全感 / 金钱）写入 `targetText`，并附加 `BuildNextCheckDetailLine()` 返回的检定信息。

#### `BuildNextCheckDetailLine(int targetType)`

遍历 `DayManager.daySO.dayDatas`，找到当前天数之后（含当天）最近的一条有 `failedDialog` 的 `DayData`，计算距该天的剩余天数、对应目标值（通过 `targetType` 映射 `target1~4`）以及魅力目标值（`targetCharm`），组成格式字符串返回，例如 `距离下次检定：3天，目标值：50，魅力目标：30`。

#### `UpdateSliderVisual(Slider, Image, Color, float)`

将属性值归一化后写入 `Slider.normalizedValue`。属性值 ≤ 100 时最大量程为 100，超过 100 时量程改为 1000 并将填充色改为红色以作视觉警示。

---

## UpdateMoney.cs

挂载在显示金钱数量的 `TextMeshProUGUI` 节点上，与文字组件共存于同一 GameObject，通过 `GetComponent<TextMeshProUGUI>()` 在 `Awake` 时自动关联，无需 Inspector 拖拽。

### 数值更新时机

| 触发方 | 方式 |
|--------|------|
| `OnEnable`（自身） | GameObject 激活时自动刷新，适合商店面板重新打开时同步 |
| `ShopController.RefreshMoneyUI()`（静态方法） | 购买/出售完成后，用 `FindObjectsOfType<UpdateMoney>(true)` 找到场景中所有实例并逐一调用 `UpdateText()` |

### 脚本关联方式

脚本直接挂在文字节点上，`GetComponent` 取同节点的 `TextMeshProUGUI`，无需任何 Inspector 引用。因此只要将 `UpdateMoney` 组件添加到任意金钱显示文字物体，即可自动接入更新流程。

---

## 外部关联

| 脚本 | 关联方式 |
|------|---------|
| `DialogueUIAudio` | `TransitionManager` 在过渡开始时调用其 `PlayChangeSceneAudio()` |
| `DayManager` | `PropertiesShow` 订阅 `OnDayAdvanced` 事件；`InitializeRandomTargetByEvent` 写入 `TargetType`；`BuildNextCheckDetailLine` 读取 `daySO` 和 `GetDayNumber()` |
| `DataManager` | `UpdatePropertiesShow` 读取 `nature1/2/3` 和 `GetCharm()`；`UpdateMoney` 读取 `MoneyNum` |
| `ShopController` | 调用 `UpdateMoney.UpdateText()` 刷新金钱显示 |
