# Dialogue

对话系统核心模块。负责驱动全局日程流程、管理 Yarn Spinner 对话的排队与播放、控制立绘高亮、文字特效、音频触发和玩家名字处理。本文件夹是游戏中联系最广的模块之一，与 `DayManager`、`DataManager`、`CardManager`、`UISceneManager`、`TransitionManager`、`AudioManager` 以及 `YarnCommand/CharacterControl` 均有直接关联。

---

## 脚本概览

| 脚本 | 职责 |
|------|------|
| `DialogueHandler` | 日程驱动核心；对话队列管理；失败检定；过天与场景切换协调 |
| `CharacterHighlightManager` | Yarn Presenter 扩展；立绘高亮/隐藏；属性结算；视觉清场 |
| `DialogueUIAudio` | 对话相关 UI 音效单例；统一封装各场景下的音效播放 |
| `PlayerNameHandler` | 玩家自定义名字输入；写入 Yarn 变量与 PlayerPrefs |
| `TextSpecialEffect` | TMP 文字特效；解析 `<wave>` / `<shake>` 自定义标签并驱动顶点动画 |
| `OptionSoundHelper` | 对话选项点击音效辅助；IPointerClickHandler + UnityEvent |

---

## DialogueHandler.cs

手动单例（`static Instance`），挂载在对话系统根节点上。是整个日程流程的**总调度器**，协调 Yarn Spinner、`DayManager`、`CharacterHighlightManager` 和 `TransitionManager` 完成"早晨对话→打工/商店/约会→睡前结算→过天"的完整日循环。

### 关键字段

| 字段 | 说明 |
|------|------|
| `dialogueRunner` | 引用的 Yarn `DialogueRunner` |
| `skipDialogueButton` | 跳过对话按钮，对话播放时显示，结束时隐藏 |
| `pendingDialogues` | 对话队列（`Queue<string>`），保证多段对话顺序无缝播放 |
| `isHandlingDialogueSequence` | 全局"对话序列进行中"锁，防止重复入队或并发启动 |
| `_dealProgress` | 每个 `deal` 系列的已播进度（`i → j`），持久保存在内存中 |
| `_specialProgress` | 每个 `special` 系列的已播进度 |
| `_doWorkI` / `_doWorkJ` | 打工对话当前系列和集数 |
| `_deferredFailureNode` | 延迟失败检定：前置对话（deal/doWork/事件）结束后再触发 |
| `_isPlayingFailedDialogue` | 当前正在播放失败对话的标志，用于结束时走失败分支 |

---

### Update 驱动的天数监测

`Update` 每帧比较 `DayManager.GetDayNumber()` 与 `lastCheckedDay`：
- 天数发生变化时，若场景内名为 `talk` 的物体处于激活状态，读取当天 `dailyDialog` 节点并通过 `StartDialogue` 入队。
- 同时调用 `SetNextSceneType("Select")` 准备对话结束后的场景切换。
- `talk` 未激活时等待（不更新 `lastCheckedDay`），避免在错误时机触发。

`Update` 还通过 `wasDialogueRunning` / `IsDialogueRunning` 检测 Yarn 的真实启停状态，状态变为"停止"时自动启动 `EndDialogueRoutine`。

---

### 对话入口方法

#### `StartDialogue(string yarnScript)`

通用对话入口，所有外部调用均经由此方法。

- 若当前有对话正在运行或队列非空，新节点加入 `pendingDialogues` 排队。
- 否则设置锁并启动 `StartDialogueRoutine` 协程。

#### `StartDialogueRoutine(string yarnScript)`（私有协程）

实际启动流程：

1. 检测场景内是否已有激活的 `talk` 物体。若无，触发 `TransitionManager.PlayTransition`，在黑屏中点调用 `UISceneManager.SwitchToScene(SceneType.Talk)` 切换到对话场景。
2. 逐帧等待 `talk` 物体被激活（防止 Yarn 在物体未就绪时执行立绘命令）。
3. 从 `PlayerPrefs` 取出玩家自定名并写入 `DialogueRunner.VariableStorage`。
4. 调用 `dialogueRunner.StartDialogue(yarnScript)` 正式启动 Yarn 节点。

---

### 日程触发方法

#### `TriggerEndDayWithDeal(string sceneTypeName = "Talk")`

供商店/收摊按钮的 UnityEvent 直接调用。

1. 立刻调用 `TryGetFailedDialogue` 记录延迟失败节点（存入 `_deferredFailureNode`，此时不触发）。
2. 标记过天（`SetAdvanceDayAfterDialogue(true)`）并记录目标场景。
3. 从 `deal1_1`、`deal1_2`、`deal2_1`... 按进度顺序找到第一个存在的 deal 节点并播放，进度 +1。
4. 若没有任何 deal 节点，直接调用 `EndDialogueRoutine` 跳过 deal 进行过天检测。

#### `TriggerDoWorkDialogue()`

打工环节专用。

1. 记录延迟失败节点。
2. 按 `doWork{i}_{j}` 顺序查找并播放，`j` 递增；当前 `i` 系列耗尽后切到 `i+1` 系列；全部耗尽后回绕到 `doWork1_1`。

#### `TriggerEventDialogue(string eventNodeName, string sceneTypeName = "Talk")`

事件牌触发对话入口。

1. 记录延迟失败节点。
2. 标记过天并记录目标场景。
3. 直接播放事件对话节点（节点名由事件牌携带）。

#### `TriggerEndDayDirectly(string sceneTypeName = "Talk")`

直接回家场景（无 deal/打工）触发过天。

1. 立刻执行失败检定：若失败，标记 `_isPlayingFailedDialogue` 并播放失败对话，不过天。
2. 若通过，标记过天 + 目标场景，手动触发 `EndDialogueRoutine`。

---

### EndDialogueRoutine（核心结算协程）

每段对话播放结束后由 `Update` 调用，是日程流程的**漏斗**，所有收尾逻辑均在此汇聚。

```
对话结束
  └→ [转场开始] PlayTransition
       └→ [黑屏中点] ClearVisualsOnTransitionMidpoint（清立绘/背景/射线）
  └→ 是否 _isPlayingFailedDialogue？
       ├─ 是 → OnGameFailed() 并终止
       └─ 否 ↓
  └→ 是否有 _deferredFailureNode？
       ├─ 有 → 清空 pendingDialogues，将失败节点入队，标记 _isPlayingFailedDialogue，取消过天
       └─ 无 ↓
  └→ 是否 shouldAdvanceDayAfterDialogue？
       ├─ 有 special → 将 special 节点入队，保持过天标志（等 special 播完再过天）
       └─ 无 special → DayManager.NextDay()，复位 shouldAdvanceDayAfterDialogue
  └→ pendingDialogues 有内容？
       ├─ 有 → 取出下一段，直接启动 StartDialogueRoutine（不解锁）
       └─ 无 ↓
  └→ willSwitchScene？
       ├─ 是 → UISceneManager.SwitchToScene(nextSceneType)
       └─ 否 → 解锁 isHandlingDialogueSequence
```

关键设计：Special 对话处理采用"先入队，保持过天标志"策略——special 播完后再次进入 `EndDialogueRoutine`，才真正执行 `NextDay()`，确保 special 在"跨天瞬间"之前播完。

---

### 失败检定

#### `TryGetFailedDialogue(out string failedNode)`（私有）

读取 `DayManager.daySO.dayDatas[当前天-1]`，若该天有 `failedDialog` 字段，则按以下逻辑判断是否触发失败：

1. 根据 `DayManager.TargetType`（1~4）将玩家对应属性（`nature1/2/3` / `MoneyNum`）与 `target1~4` 比对，**属性达标则直接通过**。
2. 属性未达标时，进一步检测魅力：若 `DayData.targetCharm > 0` 且 `DataManager.GetCharm() >= targetCharm`，**魅力达标也视为通过**（两者满足其一即可）。
3. 两项均未达标时，返回 `true` 并输出 `failedDialog` 节点名，触发失败流程。

#### `OnGameFailed()`

失败对话播放结束后调用，执行完整重置：

- `DataManager` 全属性清零（`nature1/2/3`、`MoneyNum`、`extraCharm`）
- `CardManager.ClearAllCards()`，三种保底计数清零
- `DayManager.ResetToStart()`（天数重置为 0，因其为 `DontDestroyOnLoad` 必须手动重置）
- `UISceneManager.SwitchToScene(SceneType.Begin)` 跳回起始场景

---

### Special 对话检索

#### `GetAvailableSpecialDialogue()`（私有）

遍历 `special1_1`、`special1_2`... 直到 `special20_x`，找到最小 `i` 系列中当前进度 `j` 对应的节点，判断 `currentDay >= j * 3`（每三天可推进一段），满足条件则返回节点名并将该系列进度 +1。

---

### 辅助公开方法

| 方法 | 用途 |
|------|------|
| `SetNextSceneType(string)` | 供 UnityEvent 调用，字符串转 `SceneType` 枚举后保存 |
| `SetNextSceneByEnum(SceneType)` | 代码直接传枚举 |
| `SetAdvanceDayAfterDialogue(bool)` | 转发给 `CharacterHighlightManager.shouldAdvanceDayAfterDialogue` |
| `SetDialogueProperties(int, int, int)` | 转发给 `CharacterHighlightManager.SetDialogueCompleteProperties` |
| `SetDialogueMoney(int)` | 转发给 `CharacterHighlightManager.SetDialogueCompleteMoney` |

---

## CharacterHighlightManager.cs

继承 Yarn Spinner 的 `DialoguePresenterBase`，挂载在与 `DialogueHandler` 相同的节点上，负责对话过程中的**所有视觉与属性结算**。

### 数据结构

```
Character
  ├── characterName     角色名（玩家角色运行时由 SyncPlayerName 动态更新）
  ├── normalColor       正在说话时的立绘颜色（默认白色）
  ├── dimColor          未说话时的立绘颜色（默认灰色 0.5）
  └── emotionSprites    List<EmotionSprite>
           ├── emotion  差分名（如 "happy", "sad"）
           └── sprite   对应 Sprite 资源
```

`characters[0]` 约定为玩家角色，`characters[1+]` 为 NPC。

### Yarn Presenter 生命周期重写

#### `RunLineAsync`（每行对话执行）

1. 调用 `SyncPlayerName()` 确保角色名最新。
2. 截获说话者名字，若匹配玩家默认名/占位符（`"林奈"` / `"{$MY_NAME}"` / `"Player"` 等），强制替换为 Yarn 变量中的真实名字，并直接写入 `LinePresenter.characterNameText`（绕过 Yarn 原生可能的未替换 bug）。
3. 解析行标签（`line.Metadata`），逐个交给同节点的 `CharacterControl.PlayAudioFromTag()` 处理（触发 sfx 音效）。
4. 调用 `HightlightSpeaker(speakerName)` 更新立绘亮暗状态。

#### `OnDialogueStartedAsync`

- 激活 `dialogueBackground`
- 重新开启 LinePresenter 和 OptionsPresenter 的 `CanvasGroup.blocksRaycasts`（防止对话时点穿）
- 激活 `Player` 和 `Character` 立绘节点
- 调用 `HightlightSpeaker("")` 将所有人初始变暗

#### `OnDialogueCompleteAsync`

- 调用 `ApplyDialogueCompleteProperties()` 将积累的属性增减写入 `DataManager`
- 调用 `CharacterControl.ResetPortraitPositionsAfterDialogue()`，将 `Player` / `Character` 立绘恢复到游戏启动时记录的初始位置
- 停止 BGM 和白噪音
- 注意：视觉清场（隐藏背景/立绘）已移交给 `ClearVisualsOnTransitionMidpoint`，此处不做视觉操作

### 关键私有方法

#### `HightlightSpeaker(string speaker)`

在 `talk` 下查找 `Player` 和 `Character` 物体的 `Image`，依据说话者：
- 说话者对应立绘设为 `normalColor`（白色，全亮）
- 另一人设为 `dimColor`（灰色，变暗）
- 旁白（speaker 为空）时两人均变暗

#### `ClearVisualsOnTransitionMidpoint()`

由 `DialogueHandler.EndDialogueRoutine` 在黑屏中点回调，神不知鬼不觉地：
- 隐藏 `dialogueBackground`
- 关闭所有对话 UI 的射线拦截
- 清空 `CharacterControl.objectToCharacterMap`
- 隐藏并禁用 `Player` / `Character` 立绘图片

#### `SyncPlayerName()`

每帧（`Update`）调用。从 `InMemoryVariableStorage` 读取 `$MY_NAME`，实时同步到 `characters[0].characterName`。Yarn 未完全初始化时捕捉 `InvalidOperationException` 静默跳过。

#### `ApplyDialogueCompleteProperties()`（私有）

将 `dialogueCompleteProperties[0~3]` 对应写入 `DataManager.Add(1~4, ...)`，随后全部清零。

### 属性结算接口

| 方法 | 用途 |
|------|------|
| `SetDialogueCompleteProperties(p1, p2, p3)` | 设置对话结束后三项 nature 增减量 |
| `SetDialogueCompleteMoney(money)` | 设置金钱增减量 |
| `SetAdvanceDayAfterDialogue(bool)` | 设置是否在本次对话结束后过天（被 DialogueHandler 转发调用） |

---

## DialogueUIAudio.cs

继承 `Singleton<DialogueUIAudio>`，集中管理全游戏对话相关 UI 音效的播放，统一调用 `SfxTrigger.PlaySingle(path, pitch)`。路径常量均以 `Sound/` 开头，`PlayDialogueAudio` 内部会自动裁去前缀以兼容 `SfxTrigger` 的路径拼接规则。

### 公开方法速查

| 方法 | 音效用途 |
|------|---------|
| `PlayDialogueClick()` | 推进对话行（点击/按键） |
| `PlayDialogueOptionClick()` | 点击对话选项 |
| `PlaySkipDialogue()` | 点击跳过按钮 |
| `PlayChangeSceneAudio()` | 场景切换（`TransitionManager` 调用） |
| `PlayStartGameAudio()` | 游戏开始 |
| `PlayCardRewardAudio()` | 三选一卡牌奖励界面 |
| `PlayShopBuyCardAudio()` | 商店购买卡牌 |
| `PlayShopBattleEndAudio()` | 商店/战斗结束按钮 |
| `PlayCardClickAudio()` | 卡牌点击（开启随机音高） |
| `PlayDefaultAudio()` | 通用默认音效 |

---

## PlayerNameHandler.cs

挂载在玩家名字输入界面（Begin 场景）的普通 `MonoBehaviour`。

### 字段

| 字段 | 说明 |
|------|------|
| `nameInputField` | 名字输入框（TMP_InputField） |
| `defaultName` | 默认名（输入为空时使用，默认 `"Player"`） |
| `variableStorage` | 关联的 Yarn `VariableStorageBehaviour` |

### `ConfirmName()`

供确认按钮 UnityEvent 调用：
1. 取 `nameInputField.text`，为空则使用 `defaultName`
2. 写入 `variableStorage` 的 `$MY_NAME` 变量
3. 持久化到 `PlayerPrefs.SetString("PLAYER_CUSTOM_NAME", ...)`

`DialogueHandler.Start` 和 `StartDialogueRoutine` 在进入新对话前均会从 `PlayerPrefs` 读取并重新写入 `VariableStorage`，保证跨场景后名字不丢失。

---

## TextSpecialEffect.cs

挂载在 `LinePresenter` 的父物体（`DialogueSystem` 节点）上，自动在子层级中找到名为 `"Text"` 的 `TMP_Text`，通过每帧修改顶点坐标实现文字动画效果。

### 支持的 Yarn 标签

在 `.yarn` 文件中直接在对话文本内嵌入自定义标签：

```yarn
伊兰莉特: <wave>花开得真好。</wave>
（<shake>树枝折断了</shake>）
伊兰莉特: 把它们<shake>做成花环</shake>戴在你头上。
```

| 标签 | 效果 | 可配置参数 |
|------|------|-----------|
| `<wave>...</wave>` | 字符依次上下正弦波动 | `waveAmplitude`（振幅）、`waveSpeed`（速率）、`waveSpacing`（相邻字符相位差） |
| `<shake>...</shake>` | 字符每帧随机 XY 偏移（抖动） | `shakeMagnitude`（最大偏移像素） |

### 工作流程

1. **`CheckTextChanged()`**（每帧）：通过 `maxVisibleCharacters` 下降检测 Yarn 开始新行（比对文本内容更可靠），避免重复解析同一行。检测到新内容后：
   - 调用 `ParseEffectRanges(raw)` 解析自定义标签，将需要动画的**可见字符索引**分别存入 `_waveChars` / `_shakeChars`
   - 调用 `StripCustomTags` 剥离自定义标签后写回 `_tmp.text`（TMP 不识别自定义标签，需清除）
2. **`ApplyVertexAnimation()`**（有 effect 时每帧）：`ForceMeshUpdate` 后遍历所有可见字符，对标记字符计算偏移量并写入顶点数组，最后提交 mesh 更新。

---

## OptionSoundHelper.cs

轻量辅助脚本，挂载在对话选项按钮物体上。实现 `IPointerClickHandler`，点击时触发 `onClickEvent`（UnityEvent，可在 Inspector 中配置任意回调）。`SelectAudioPlay()` 可直接加入该 UnityEvent，调用 `DialogueUIAudio.PlayDialogueOptionClick()` 播放选项点击音效。

---

## 模块间关联总览

| 调用方 | 被调用方 | 关联内容 |
|--------|---------|---------|
| `DialogueHandler` | `DayManager` | 读取天数、`daySO`、调用 `NextDay()` / `ResetToStart()` / `GetDayNumber()` |
| `DialogueHandler` | `DataManager` | `OnGameFailed` 时清零所有属性 |
| `DialogueHandler` | `CardManager` | `OnGameFailed` 时清空手牌、重置保底计数 |
| `DialogueHandler` | `UISceneManager` | 切换场景（`SwitchToScene`） |
| `DialogueHandler` | `TransitionManager` | `StartDialogueRoutine` 和 `EndDialogueRoutine` 均依赖黑幕过渡 |
| `DialogueHandler` | `PropertiesShow` | `EndDialogueRoutine` 结算后刷新属性条显示 |
| `DialogueHandler` | `CharacterHighlightManager` | 转发属性设置、过天标志；在黑屏中点调用 `ClearVisualsOnTransitionMidpoint` |
| `CharacterHighlightManager` | `DataManager` | `ApplyDialogueCompleteProperties` 写入属性增减 |
| `CharacterHighlightManager` | `AudioManager` | `OnDialogueCompleteAsync` 停止 BGM 和白噪音 |
| `CharacterHighlightManager` | `CharacterControl`（YarnCommand） | `RunLineAsync` 中将行标签转发给 `PlayAudioFromTag`；`OnDialogueCompleteAsync` 触发立绘位置复位；`ClearVisualsOnTransitionMidpoint` 清空 `objectToCharacterMap` |
| `CharacterHighlightManager` | `InMemoryVariableStorage` | `SyncPlayerName` 读取 `$MY_NAME` |
| `DialogueUIAudio` | `SfxTrigger` | 所有音效最终通过 `SfxTrigger.PlaySingle` 播放 |
| `DialogueUIAudio` | `TransitionManager`（被调用） | `PlayChangeSceneAudio` 在 `TransitionManager.PlayTransition` 开头被调用 |
| `PlayerNameHandler` | `VariableStorageBehaviour` | 写入 `$MY_NAME` |
| `PlayerNameHandler` | `PlayerPrefs` | 持久化玩家名，供 `DialogueHandler` 跨场景恢复 |
| `OptionSoundHelper` | `DialogueUIAudio` | `SelectAudioPlay` → `PlayDialogueOptionClick` |
