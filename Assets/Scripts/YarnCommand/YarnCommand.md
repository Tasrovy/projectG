# YarnCommand

Yarn Spinner 命令扩展模块。通过 `[YarnCommand]` 特性将 C# 方法注册为可在 `.yarn` 剧本文件中直接调用的命令，供对话设计师在对话脚本中控制角色立绘、镜头、背景、音频和玩家属性。

---

## CharacterControl.cs

挂载在对话 UI 根节点（或其子节点）上的 `MonoBehaviour`，实现了全部面向 Yarn 的命令接口。所有 `[YarnCommand]` 均为 `static` 包装方法，内部通过 `Object.FindAnyObjectByType<CharacterControl>()` 找到实例后委托给对应的实例方法执行。

---

### Inspector 暴露参数

| 字段 | 说明 |
|------|------|
| `shakeDuration` | 抖动持续时长（秒，默认 0.5） |
| `shakeMagnitude` | 抖动幅度（像素，默认 40） |
| `portraitSizeType2Scale` | 2 档立绘放大比例（最小值 1，默认 1.15） |
| `portraitSizeType3Scale` | 3 档立绘放大比例（最小值 1，默认 1.3） |

---

### 立绘槽位系统

场景中存在一个名为 `talk` 的 GameObject，其下有两个立绘槽位子物体：`Player`（左侧/玩家方）和 `Character`（右侧/角色方），以及背景 `talkBG`。`objectToCharacterMap` 字典记录「槽位名→当前所显示角色名」的映射关系，供差分切换等命令使用。

---

### Yarn 命令一览

#### `set_character_person`

```yarn
<<set_character_person objectName characterName emotion>>
```

**功能**：将指定角色以指定表情分配到某个立绘槽位，并以 0.4 秒交叉淡入淡出（Crossfade）切换图片。同时更新 `objectToCharacterMap` 映射。

| 参数 | 类型 | 说明 |
|------|------|------|
| `objectName` | string | 槽位名，只能是 `"Player"` 或 `"Character"` |
| `characterName` | string | 角色名，须与 `CharacterHighlightManager` 中的配置一致 |
| `emotion` | string | 差分表情名（大小写不敏感） |

**示例**：
```yarn
<<set_character_person Character Tasrovy happy>>
<<set_character_person Player Odara normal>>
```

**备注**：若指定 emotion 不存在，自动回退使用该角色第一张差分图；若槽位物体未激活（`talk` 未显示），命令静默失败。

---

#### `set_character_move`

```yarn
<<set_character_move objectName axis distance duration>>
```

**功能**：将指定立绘槽位沿单轴移动一段距离。`axis` 使用单字符方向：`x`=水平移动，`y`=竖直移动。`duration` 可省略，默认为 `0`（瞬时移动）。

| 参数 | 类型 | 说明 |
|------|------|------|
| `objectName` | string | 槽位名，只能是 `"Player"` 或 `"Character"` |
| `axis` | char/string | 方向字符，`x` 或 `y`（大小写不敏感，仅取首字符） |
| `distance` | float | 移动距离，正负值均可 |
| `duration` | float | 过渡时间（秒），可省略，默认 `0` |

**示例**：
```yarn
<<set_character_move Character x 120 0.25>>
<<set_character_move Player y -60>>
```

**备注**：每次对话结束时，`Player` 与 `Character` 会自动恢复到游戏启动时记录的初始位置。

---

#### `set_character_sprite`

```yarn
<<set_character_sprite characterName emotion>>
```

**功能**：在不更换槽位归属的前提下，直接切换已显示角色的差分表情（立即替换，无渐变）。

| 参数 | 类型 | 说明 |
|------|------|------|
| `characterName` | string | 角色名 |
| `emotion` | string | 目标表情名 |

**示例**：
```yarn
<<set_character_sprite Tasrovy surprised>>
```

**备注**：通过 `objectToCharacterMap` 反向查找角色当前所在槽位，再定位对应 `Image` 组件。

---

#### `clear_character_person`

```yarn
<<clear_character_person objectName>>
```

**功能**：清除指定槽位的立绘图片（隐藏 Image 并清除 sprite），同时从 `objectToCharacterMap` 中移除该槽位的映射记录。

| 参数 | 类型 | 说明 |
|------|------|------|
| `objectName` | string | 槽位名，只能是 `"Player"` 或 `"Character"` |

**示例**：
```yarn
<<clear_character_person Character>>
```

---

#### `set_character_shake`

```yarn
<<set_character_shake characterName shakeType>>
```

**功能**：让指定角色的立绘以正弦曲线（非随机）抖动一次，持续 `shakeDuration` 秒，幅度 `shakeMagnitude` 像素。

| 参数 | 类型 | 说明 |
|------|------|------|
| `characterName` | string | 角色名 |
| `shakeType` | string | `"up_down"`（上下）/ `"left_right"`（左右）/ 其他（全向） |

**示例**：
```yarn
<<set_character_shake Tasrovy up_down>>
<<set_character_shake Odara left_right>>
```

---

#### `set_character_size`

```yarn
<<set_character_size objectName sizeType yPoint duration>>
```

**功能**：改变指定立绘槽位的显示大小，支持带缓动（SmoothStep）的过渡动画。同一槽位的上一次大小动画会被打断并替换为新动画。

| 参数 | 类型 | 说明 |
|------|------|------|
| `objectName` | string | 槽位名，`"Player"` 或 `"Character"` |
| `sizeType` | int | `1`=原始大小，`2`=中档放大，`3`=大档放大（倍率由 Inspector 配置） |
| `yPoint` | float | 放大时的 Y 轴锚点（`0`=上边缘，`1`=下边缘）。锚点位置在放大前后保持屏幕坐标不变，即图片向该点"推近"。 |
| `duration` | float | 过渡时间（秒），`0` 为瞬间切换 |

**示例**：
```yarn
<<set_character_size Character 2 1.0 0.3>>   # 放大到2档，锚定下边缘，0.3秒过渡
<<set_character_size Character 1 0.5 0.5>>   # 还原1档，0.5秒过渡
```

---

#### `set_background`（可等待）

```yarn
<<set_background backgroundName>>
```

**功能**：通过 `TransitionManager` 播放全屏黑幕过渡，在完全变黑的瞬间替换背景图片，随后淡出。Yarn Spinner 会**等待**整个协程结束后才执行下一行。

| 参数 | 类型 | 说明 |
|------|------|------|
| `backgroundName` | string | 背景图资源名，从 `Resources/Background/` 下加载 |

**示例**：
```yarn
<<set_background classroom>>
# 此后的对话在新背景下进行
```

**备注**：这是所有命令中唯一返回 `IEnumerator` 的命令，Yarn Spinner 自动等待其完成。

---

#### `camera_zoom`

```yarn
<<camera_zoom anchorX anchorY targetScale duration>>
```

**功能**：仅缩放/平移背景（`talkBG`），立绘和对话框保持原位不受影响，制造「镜头推近」的视觉效果。

| 参数 | 类型 | 说明 |
|------|------|------|
| `anchorX` | float | 缩放锚点 X（`0`=左边缘，`1`=右边缘，`0.5`=水平居中） |
| `anchorY` | float | 缩放锚点 Y（`0`=上边缘，`1`=下边缘，`0.5`=垂直居中） |
| `targetScale` | float | 放大倍数（`1.0`=原始大小，`1.5`=放大约 1.5 倍） |
| `duration` | float | 过渡时间（秒），`0` 为瞬间 |

**示例**：
```yarn
<<camera_zoom 0.5 0.5 1.5 1.0>>   # 居中推近1.5倍，1秒过渡
<<camera_zoom 0.8 0.9 1.3 0.5>>   # 向右下角推近1.3倍
```

---

#### `reset_camera`

```yarn
<<reset_camera duration>>
```

**功能**：将 `camera_zoom` 造成的背景缩放/偏移恢复到原始状态。若当前未处于缩放状态则无操作。

| 参数 | 类型 | 说明 |
|------|------|------|
| `duration` | float | 过渡时间（秒），`0` 为瞬间 |

**示例**：
```yarn
<<reset_camera 0.8>>
```

---

#### `add_property`

```yarn
<<add_property type num>>
```

**功能**：直接修改玩家属性数值，调用 `DataManager.Instance.Add(type, num)`。

| 参数 | 类型 | 说明 |
|------|------|------|
| `type` | int | `1`=nature1，`2`=nature2，`3`=nature3，`4`=MoneyNum，`5`=ExtraCharm |
| `num` | int | 增减量（可为负数） |

**示例**：
```yarn
<<add_property 1 10>>    # nature1 +10
<<add_property 4 -50>>   # 金钱 -50
```

---

#### `play_bgm` / `stop_bgm`

```yarn
<<play_bgm audioParam>>
<<stop_bgm>>
```

**功能**：播放或停止背景音乐，委托 `AudioManager.PlayBGM` / `StopBGM`。

| 参数 | 类型 | 说明 |
|------|------|------|
| `audioParam` | string | 音频资源路径（下划线代替 `/`），内部自动转换；留空则等同于 `stop_bgm` |

**示例**：
```yarn
<<play_bgm level1_theme>>    # 播放 Sound/bgm/level1/theme
<<stop_bgm>>
```

---

#### `play_whitenoise` / `stop_whitenoise`

```yarn
<<play_whitenoise audioParam>>
<<stop_whitenoise>>
```

**功能**：播放或停止白噪音层音频，委托 `AudioManager.PlayWhiteNoise` / `StopWhiteNoise`。

**示例**：
```yarn
<<play_whitenoise rain_heavy>>   # 播放 Sound/Whitenoise/rain/heavy
<<stop_whitenoise>>
```

---

### 行标签音效（Line Tag SFX）

Yarn 每行对话可附加 `#sfx_...` 标签，由 `DialogueHandler`（Dialogue 文件夹）在逐字展示时调用 `PlayAudioFromTag()` 方法解析并播放音效。

**格式**：
```yarn
人物: 对话内容 #sfx_Characters_Player_laugh
```

解析规则：去除 `#` 前缀 → 确认以 `sfx_` 开头 → 截取后段 → 将下划线替换为 `/` → 拼接 `Sound/` 前缀 → 调用 `AudioManager.PlaySound`。

**示例**：
```
#sfx_Characters_Player_laugh
→ 播放 Sound/Characters/Player/laugh
```

---

## 与 Dialogue 文件夹的关联

| 关联点 | 说明 |
|--------|------|
| `DialogueHandler` 调用 `PlayAudioFromTag` | `DialogueHandler` 在 Yarn 逐行展示时，将行上的标签逐一传入 `CharacterControl.PlayAudioFromTag()`，实现台词同步音效 |
| `set_background` 依赖 `TransitionManager` | 此命令通过 `TransitionManager.Instance.PlayTransition()` 实现黑幕过渡，与 `DialogueHandler` 驱动的场景切换流程共用同一过渡动画系统 |
| `CharacterHighlightManager` 角色配置 | `set_character_person` / `set_character_sprite` 从 `CharacterHighlightManager`（挂载在 Dialogue 相关 UI 节点）读取角色名、差分列表等配置数据 |
| `CharacterHighlightManager` 对话结束回调 | 在 `OnDialogueCompleteAsync` 中调用 `CharacterControl.ResetPortraitPositionsAfterDialogue()`，将 `Player` 与 `Character` 立绘位置恢复到初始值 |
| `add_property` 写入 `DataManager` | 剧情中通过此命令修改属性后，`DialogueHandler` 在检定节点读取 `DataManager` 判定成败，两者构成「对话触发数值变化→检定读取结果」的完整链路 |
