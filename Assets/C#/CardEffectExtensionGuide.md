# 卡牌效果执行系统 — 拓展指南

## 系统架构总览

```
Excel (string)
  ↓ ParseStringToCommands / ParseFieldWithIntSupport
EffectCommand { methodName, parameters }
  ↓ CardRuntime.OnTrigger / OnMade / OnBroken / OnAdded ...
CardEffect.ExecuteEffectList(card, plan)
  ↓
CardEffectExecutor  ← 异步链式执行（队列 + 暂停/恢复）
  ↓ ExecuteSingleEffect → 先检查前置条件（手牌数、礼品卡数）
  ↓ _owner.Execute(methodName, parameters)
CardEffectInvoker  ← 反射分发：GetMethod(IgnoreCase) → Invoke
  ↓
CardEffectLibrary.方法名(...)  ← 实际效果实现
```

**关键规则**：方法名字符串（如 `"beMade(2)"`）通过反射匹配到 `CardEffectLibrary` 上的方法名（大小写不敏感）。无需注册，加方法即可。

---

## 一、如何添加新函数

### 步骤（3 步）

#### 1. 在 `CardEffectLibrary` 中添加方法

```csharp
// Assets/C#/Card/Effects/CardEffectLibrary.cs
public void 你的方法名(int 参数1, int 参数2)
{
    // 通过 _owner.CallerCard 获取触发卡
    // 调用 DataManager / CardManager / 其他系统
}
```

参数类型支持 `int` / `float` / `string`。Excel 中字符串参数会自动通过 `Convert.ChangeType` 转换。可选参数通过 `HasDefaultValue` 支持。

#### 2. （可选）在 `CardEffect` 中添加转发方法

Library 中的方法默认通过 `CardEffectInvoker` 反射调用。如果想直接 C# 调用而非通过反射，可以在 `CardEffect.cs` 添加转发：

```csharp
public void 你的方法名(int 参数1, int 参数2)
{
    EnsureComponents();
    _library.你的方法名(参数1, 参数2);
}
```

#### 3. 在 Excel 的 trigger / made / broken / added / buff / nextTurn 字段中调用

字段格式：`方法名(参数1,参数2,...);方法名2(参数1)`
用分号分隔多个命令。

---

## 二、函数可以实现的功能类别

### A. 直接操作数据（无需选牌）

这些方法立即执行，不需要暂停链等待玩家交互。

| 功能 | 实现方式 | 示例 |
|------|----------|------|
| 修改卡牌属性 | `_owner.CallerCard.Add(id, num)` | `addNature(3,1)` — 增加 nature1 |
| 设置属性下限 | `_owner.CallerCard.AddTo(id, num)` | `addNatureTo(5,2)` — nature2 至少 5 |
| 修改角色数据 | `DataManager.Instance.Add(id, num)` | `addNatureAtSum(10,1)` — 角色 nature1+10 |
| 属性转移 | `DataManager.Instance.AddNatureFromTo(id1, id2)` | `addNatureFromTo(1,2)` |
| 添加金钱 | `DataManager.Instance.Add(4, money)` | `addMoney(50)` |
| 添加魅力 | `DataManager.Instance.Add(5, num)` | `addCharm(5)` |
| 添加卡牌到牌库 | `CardManager.Instance.AddCard(cardID, num)` | `addCard(1001,1)` |
| 添加随机卡牌 | `CardManager.Instance.AddRandomCard(type, num, level)` | `addRandomCard(1,2,2)` |
| 添加未拥有随机卡 | `CardManager.Instance.AddRandomCardIfNot(type, num, level)` | `addRandomCardIfNot(1,2,3)` |
| 抽牌 | `CardManager.Instance.DrawCard(num)` | `drawCard(2)` |
| 不消耗 | 复制自己到手牌 | `noConsumed()` |
| 屏幕震动 | `BattleDialogController.Instance.Shake(type, extent)` | `shake(1,0.5f)` |
| 屏幕放大 | `BattleDialogController.Instance.Magnify(multiple)` | `magnify(1.5f)` |
| 切换手牌 | 与手中另一张随机卡牌互换 | `changeHandGift()` |
| 添加属性效果 | 注册次日监听器，倍率生效 | `changeProperty(1.5f)` |

**示例 Excel 配置**：

```
trigger = "addNature(3,1);addMoney(50);drawCard(1);addRandomCard(2,1,2)"
```

### B. 选牌后执行（需要暂停链）

这类方法通过 `CardActionResolver.StartEffectSelection` 暂停效果链，等待玩家选牌后恢复。

#### 模式一：让玩家选一张牌，对该牌执行操作

`CardSubmitHelper` 中已有的模板（直接复用或参考）：

```csharp
public void 你的选牌效果(int 参数)
{
    CardActionResolver.Instance.StartEffectSelection(
        onConfirm: (selectedCard) =>
        {
            // 对 selectedCard 执行操作
            selectedCard.Add(1, 参数);
            selectedCard.OnAdded();
            
            CardManager.Instance.NotifyDeckOrHandChanged();
            CardActionResolver.Instance.CompletePendingPlayedCard(true);
            CardEffect.Instance.OnSelectCardEnd(true);
        },
        onCancel: () =>
        {
            // 取消 → 标记条件失败 → 回滚快照
            RestoreCallerCard();
            CardEffect.Instance.OnSelectCardEnd(false);
        },
        buttonText: "按钮文字"
    );
}
```

然后在 `CardEffectLibrary` 中调用：

```csharp
public void 你的方法(int 参数)
{
    if (_owner.CallerCard == null || 参数 == 0) return;
    CardSubmitHelper.Instance.你的选牌效果(参数);
}
```

#### 模式二：多次选牌（递归）

参考 `ShengZhang` / `JianZhi` 的递归模式：

```csharp
private void 递归选牌(int timesLeft)
{
    if (timesLeft <= 0)
    {
        CardActionResolver.Instance.CompletePendingPlayedCard(true);
        CardEffect.Instance.OnSelectCardEnd(true);
        return;
    }
    
    CardActionResolver.Instance.StartEffectSelection(
        onConfirm: (selectedCard) =>
        {
            DoSomething(selectedCard);
            递归选牌(timesLeft - 1);  // 递归调起下一次选牌
        },
        onCancel: () => { RestoreCallerCard(); ... },
        buttonText: "..."
    );
}
```

**关键机制**：`HandleSubmit` 中先暂存回调 → 立即 `ResetToNormalMode()` → 再 invoke。这样递归调用 `StartEffectSelection` 不会互相覆盖。

#### 模式三：自动批量操作（不弹 UI）

参考 `addWithSameTogether`：

```csharp
public void 批量操作(int num)
{
    // 直接遍历手牌，不弹选牌UI
    foreach (Card card in CardManager.Instance.cardInHand)
    {
        // 对每张符合条件的卡牌执行操作
    }
}
```

#### 模式四：选牌 + 条件判断

参考 `addWithSame`：

```csharp
public void 选牌并按条件执行(int threshold, int trueVal, int falseVal)
{
    CardActionResolver.Instance.StartEffectSelection(
        onConfirm: (selectedCard) =>
        {
            if (MeetCondition(selectedCard, threshold))
            {
                DoTrueBranch(selectedCard, trueVal);
            }
            else
            {
                DoFalseBranch(selectedCard, falseVal);
            }
            ...
        },
        ...
    );
}
```

---

## 三、已有效果分类（可参考的模板）

### 已有选牌效果一览

| Library 方法 | 对应 Helper | 选牌次数 | 行为 |
|---|---|---|---|
| `beAdded(num, times)` | `ShengZhang(num, times)` | times 次（递归） | 给选中牌的所有属性 +num |
| `beAddedTo(num)` | `ShengZhangTo(num)` | 1 次 | 将选中牌各属性提升至至少 num |
| `beMade(num)` | `ShengZhi()` | 1 次 | 次日生成 num 张该牌的复制 |
| `beBroken(num)` | `JianZhi()` | num 次（递归） | 拆解 num 张牌 |
| `addWithSame(sameNum, t, f)` | 内联 lambda | 1 次 | 选一张，数相同牌数 → 条件分支 |
| `addNatureAtSumIf(t1, t2, num)` | `AddNatureAtSumIf` | 1 次 | 若选中牌 t1>0，角色 t2+num |
| `addCardSale(num)` | `AddCardSale` | 1 次 | 选中牌售价 +num |

### 生命周期触发时机

| 事件 | 触发时机 | 效果字段 |
|------|----------|----------|
| `OnTrigger` | 主动打出卡牌时 | `trigger` + `made`/`broken`/`added` 的数字前置 |
| `OnMade` | 被其他牌的 `beMade` 选中时 | `made`（字符串） |
| `OnBroken` | 被其他牌的 `beBroken` 选中时 | `broken`（字符串） |
| `OnAdded` | 被其他牌的 `beAdded` 选中时 | `added`（字符串） |
| `OnBuffUpdate` | buff 更新时 | `buff` |
| `OnNextTurn` | 下一回合开始时 | `nextTurn` |

**数字字段的特殊行为**：如果 `made`/`broken`/`added` 只填纯数字（如 `made = "2"`），会自动转为前置条件，在 `OnTrigger` 时**提前**执行——先触发「生枝/剪枝/生长」再执行 `trigger` 里的其他效果。

---

## 四、可拓展的新功能方向

以下是可以利用现有架构拓展的新效果类型。前三个是最常用的选牌模式：

### 1. 选牌 → 销毁/消耗（类似 beBroken 但操作不同）

```csharp
public void discardForGain(int gainType, int gainNum)
{
    CardActionResolver.Instance.StartEffectSelection(
        onConfirm: (selectedCard) =>
        {
            // 记录选中牌的信息
            int cardType = CardIdUtility.GetCardType(selectedCard.id);
            // 拆解这张牌
            selectedCard.OnBroken();
            CardManager.Instance.BreakCard(selectedCard);
            // 根据类型给予不同增益
            if (cardType == 1) DataManager.Instance.Add(4, gainNum); // 钱
            else DataManager.Instance.Add(5, gainNum);               // 魅力
            ...
        },
        ...
    );
}
```

### 2. 选牌 → 复制/衍生物

```csharp
public void copyCard()
{
    CardActionResolver.Instance.StartEffectSelection(
        onConfirm: (selectedCard) =>
        {
            Card copy = new Card();
            copy.InitCard(selectedCard);
            CardManager.Instance.AddCardInHand(copy);
            ...
        },
        ...
    );
}
```

### 3. 选牌 → 交换属性

```csharp
public void swapNature(int natureId)
{
    CardActionResolver.Instance.StartEffectSelection(
        onConfirm: (selectedCard) =>
        {
            // 把自己和目标牌的某个属性互换
            int callerVal = _owner.CallerCard.GetNatureById(natureId);
            int targetVal = selectedCard.GetNatureById(natureId);
            _owner.CallerCard.AddTo(natureId, targetVal);
            selectedCard.AddTo(natureId, callerVal);
            ...
        },
        ...
    );
}
```

### 4. 条件分支（无需选牌）

```csharp
public void conditionalBranch(int threshold, int effectId)
{
    if (DataManager.Instance.GetNatureById(1) >= threshold)
    {
        // 分支A：向 CardEffectInvoker 投递另一个效果
        // 或直接调用 Library 中已有的方法
    }
    else
    {
        // 分支B
    }
}
```

### 5. 复合效果（组合多个已有方法）

```csharp
public void compositeEffect(int natureNum, int money, int cardId)
{
    // 一次调用执行多个效果
    _owner.CallerCard.Add(1, natureNum);
    DataManager.Instance.Add(4, money);
    CardManager.Instance.AddCard(cardId, 1);
}
```

### 6. 对全手牌生效

```csharp
public void buffAllHand(int addNum)
{
    foreach (Card card in CardManager.Instance.cardInHand)
    {
        if (card == _owner.CallerCard) continue; // 排除自己
        card.Add(1, addNum);
        card.Add(2, addNum);
        card.Add(3, addNum);
        card.OnAdded();
    }
    CardManager.Instance.NotifyDeckOrHandChanged();
}
```

### 7. 按条件过滤手牌后批量操作

```csharp
public void filterAndBuff(int type, int minVal, int addNum)
{
    foreach (Card card in CardManager.Instance.cardInHand)
    {
        if (card.GetNatureById(1) >= minVal)
        {
            card.Add(type, addNum);
        }
    }
    CardManager.Instance.NotifyDeckOrHandChanged();
}
```

---

## 五、在 Executor 中添加前置条件检查

如果新效果需要检查手牌数或特定条件，需要在 `CardEffectExecutor.ExecuteSingleEffect` 中添加判断分支（参考现有 `addWithSame` / `beMade` 的模式）：

```csharp
// CardEffectExecutor.cs ExecuteSingleEffect 中
if (effect.methodName == "你的方法")
{
    if (!SomeConditionCheck())
    {
        CardSubmitHelper.Instance.RestoreCallerCardOnInvalidTarget();
        StartExecutingNextChain();
        return;
    }
    _waitingForAsync = true;  // 标记需要等待异步
}
```

如果不需要等待异步（纯数据操作），不要设置 `_waitingForAsync`，链会继续执行下一条。

---

## 六、快照与回滚机制

- 快照在 `CardActionResolver.HandleSubmit`（NormalPlay 模式）中、`BreakCard` 之前拍摄
- 包含：手牌列表、牌堆列表、DataManager 的关键数值
- 当效果执行中玩家取消选牌，或前置条件不满足时：
  1. `RestoreCallerCard()` → `MarkConditionFailed(caller)` 
  2. Executor 检测到 `IsConditionFailed` → `RestoreSnapshot()` → 清空队列
- 回滚后整副手牌和牌堆恢复到打牌前的状态

**注意**：快照只覆盖 `CardManager.Instance.cardInHand`、`cardSet` 和 `DataManager` 的部分字段。如果自定义效果修改了`DayManager`、`DialogueHandler` 等外部状态，需要自己管理回滚或确保不影响快照一致性。

---

## 七、最佳实践

1. **不需要反射的方法**：如果方法只在 C# 代码中调用（不经 Excel 配置），直接在 Library 中写，不需要添加参数转换支持
2. **方法命名**：使用小驼峰（`addNature`、`beMade`），反射使用 `IgnoreCase` 匹配，但保持 Excel 配置的可读性
3. **参数数量匹配**：Excel 中的参数个数必须与方法签名一致（可选参数除外，`ConvertParameters` 会填充默认值）
4. **选牌后回调**：选牌完成后一定调用 `CardEffect.Instance.OnSelectCardEnd(true)`，取消时传 `false`
5. **CompletePendingPlayedCard**：type 2（事件卡）打完后要放回牌库，在回调中调用 `CompletePendingPlayedCard(true)`
6. **CardEffect 转发层**：新方法如果只通过 Excel 配置调用（不走 C# 直接调用），可以跳过在 `CardEffect.cs` 添加转发——Invoker 通过反射直接调用 Library
7. **不要破坏快照**：如果效果修改了后续效果依赖的全局状态，确保快照能完整恢复，或确保效果链不会在修改后被取消
