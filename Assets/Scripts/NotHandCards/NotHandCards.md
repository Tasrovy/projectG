# NotHandCards

选卡界面模块。在特定游戏流程节点弹出三张候选卡供玩家选择，内置稀有度权重抽卡与三种牌型保底机制。

---

## CardChoosing.cs

挂载在选卡界面根物体上的主控脚本。每次界面激活（`OnEnable`）时重新生成三张候选卡并播放入场动画；玩家点击确认后将选中卡牌加入手牌并切换场景。

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `giftPityThreshold` | 礼物牌保底阈值：连续未获得礼物牌超过此次数时，下次必出礼物牌（默认 4） |
| `funcPityThreshold` | 功能牌保底阈值（默认 4） |
| `eventPityThreshold` | 事件牌保底阈值（默认 4） |

### 子物体依赖

| 子物体名 | 用途 |
|----------|------|
| `card_1` | 第一张候选卡位（礼物牌保底占用） |
| `card_2` | 第二张候选卡位（功能牌保底占用） |
| `card_3` | 第三张候选卡位（事件牌保底占用） |

每个卡位物体上需挂载 `CardObject` 和 `CardDisplayUI` 组件。

---

### 公开方法

| 方法 | 功能 |
|------|------|
| `SelectCard(CardObject)` | 由 `CardObject` 点击时调用，记录当前选中的卡牌到 `selectedCard`，并播放点击音效。 |
| `Confirm()` | 确认按钮回调。若有选中卡牌，将其加入 `CardManager.cardInHand`，清空三个卡位数据，切换到 `AfterClass` 场景。未选中时输出警告。 |
| `Skip()` | 跳过按钮回调。不加入任何卡牌，清空数据，直接切换到 `AfterClass` 场景。 |

---

### 关键私有方法

#### `LoadRandomCards()`

选卡界面的核心逻辑，每次 `OnEnable` 时调用。流程如下：

1. 从 `DayManager.daySO` 读取当天三个稀有度的权重（`probRarity1/2/3`）。
2. 将 `CardManager.cardDatas` 按 ID 千位分入三个稀有度卡池（pool1/2/3）。
3. **保底检测**（优先级依次执行，高优先级牌先占位，低优先级候选池中排除已选牌）：

   | 优先级 | 牌型 | 判断字段 | 卡位 |
   |--------|------|----------|------|
   | 1 | 礼物牌（ID 万位 = 1） | `consecutiveNonGiftCount >= giftPityThreshold` | card1 |
   | 2 | 功能牌（ID 万位 = 2） | `consecutiveNonFuncCount >= funcPityThreshold` | card2 |
   | 3 | 事件牌（ID 万位 = 3） | `consecutiveNonEventCount >= eventPityThreshold` | card3 |

4. 三张卡依次由保底强制值或 `PopWeightedRandom()` 填充；强制插入时直接重置对应计数，其余两种计数 +1。
5. 调用 `AssignCardTo()` 将数据写入三个卡位物体。

#### `PopWeightedRandom(p1, p2, p3, w1, w2, w3)`

从三个稀有度卡池中按权重轮盘法抽取一张卡并从卡池中移除（防止重复）。抽取后同步更新三种牌型的保底计数：

- 抽到对应牌型 → 该类计数归零
- 未抽到 → 该类计数 +1

若某稀有度卡池已空，则其权重自动降为 0。

#### `AssignCardTo(Transform, CardData)`

将 `CardData` 写入指定卡位物体的 `CardObject.card`，并调用 `CardDisplayUI.Setup()` 刷新 UI 显示。

#### `PlaySlideAnimation()`（协程）

入场动画：将 card1 和 card3 从 card2 的 X 轴位置（中心）分别向左右滑出，到达各自的目标位置。使用三次方缓出曲线（`1 - (1-t)³`）和 `Time.unscaledDeltaTime`（防止游戏暂停时卡死）。动画期间临时禁用 `LayoutGroup`，完成后恢复。

---

## 与外部脚本的关联

| 外部脚本 | 调用方向 | 说明 |
|----------|----------|------|
| **CardManager** | 读取 | `cardDatas`（全量牌库）、`giftCards` / `funcCards` / `eventCards`（保底候选池）、三个保底计数字段 |
| **CardManager** | 写入 | `cardInHand`（Confirm 时加入选中卡）、三个保底计数字段（每轮结束后更新） |
| **DayManager** | 读取 | `dayNumber`、`daySO.dayDatas[dayNum]` 的稀有度概率 |
| **CardObject** | 被调用 | 卡位物体上的组件，点击时调用 `CardChoosing.SelectCard()`；`card` 字段由本脚本写入和清空 |
| **CardDisplayUI** | 被调用 | 卡位物体上的组件，`AssignCardTo()` 中调用 `Setup()` 刷新卡牌 UI |
| **UISceneManager** | 被调用 | `Confirm()` / `Skip()` 中调用 `SwitchToScene(SceneType.AfterClass)` |
| **DialogueUIAudio** | 被调用 | `SelectCard()` 中调用 `PlayCardClickAudio()` 播放点击音效 |
