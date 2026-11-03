# projectG 工作交接

更新：2026-11-02。开始后续工作时先读本文件，再按需读取 PreviousChat.md；历史记录是背景资料，不是新的执行授权。代码及 Unity 实时状态优先于历史结论。

## 项目与主链路

百合向视觉小说肉鸽游戏。日期推进关系养成与卡牌资源管理，通过剧情、出牌、商店、打工累积数值，在指定日期检定目标。

- DayManager：day.xlsx 每日配置、过天、跳过周末、抽卡概率、清理当日属性倍率。DateManager：日历待办。
- DataManager：友情羁绊、情绪依赖、安全感、金钱、额外魅力。历史静态分析的默认魅力权重为 1、2、3、0.1、1；可配置，使用前查代码。
- DialogueHandler：Yarn 队列、活动/打工/特殊/日末剧情、失败和转场；剧情链结束后再过天。
- CardManager：Repository、Deck、Hand、Draw 服务；礼物/功能/事件卡；CardSelector → CardActionResolver → Card.OnTrigger → 效果链。效果可等待选牌，有快照与失败回滚；功能牌正常结算返回牌堆。
- 商店买卖卡牌；UISceneManager 切换界面根对象和 BGM。
- CharacterControl：Yarn 立绘、表情、移动、缩放、背景、镜头、音频、属性指令。
- 编辑器将 Excel 同步为 ScriptableObject，构建从 Resources 读取；改表后关注同步资产。

## 已实现的阅读体验及明确需求

- NovelReadingController / NovelContinueHandler：逐字显示（历史默认每秒30字），第一次点击补全，第二次推进，按钮点击避免误推进。
- 右键三档循环：全部 UI；基本阅读模式（最终要求连日历及入口也隐藏）；纯画面只留背景立绘并暂停左键推进。退出阅读恢复 UI。
- EventSystem 命名空间冲突已改用 UnityEngine.EventSystems.EventSystem 完整名称。
- 历史对 AudioManager 增加退出时禁止自动创建、清理只访问已有实例、销毁清空引用和 Domain Reload 兼容；需要实际停止运行复测残留警告。
- NovelSkipController：确认后仅快进当前 Yarn 节点，到节点边界/选项/结束停止，不能一路跳到卡牌环节。start0_1 跳转 start0_2 后应恢复正常阅读。保留变量、属性、指令。
- 独立全屏 Canvas 覆盖画面，完全覆盖并渲染一帧后才推进；背景图→状态栏底板→文字。Inspector 可配置弹窗整体 Panel Position、尺寸、颜色、文案、背景 Cover/Contain/Stretch、缩放/偏移、参考分辨率。
- 编辑器 Inspector 提供确认弹窗/加载画面预览及关闭按钮，在 Game 视图查看。
- day.xlsx 可选 skipBackground 列：Resources 相对路径，无扩展名，如 Background/SkipLoading；当日配置优先，空则 Inspector。图片导入 Sprite，构建前同步表格。
- 最新优化：showModalShade 默认 false（透明且挡点击）；Accelerate Visuals 开关，跳过期关闭台词淡入淡出、直接切背景、立绘动画快速到最终状态，结束恢复。
- 核心文件：Scripts/Dialogue/NovelReadingController.cs、NovelContinueHandler.cs、NovelSkipController.cs、DialogueHandler.cs；Scripts/UI/TransitionManager.cs；Scripts/YarnCommand/CharacterControl.cs。本次文本核对确认快进标记、透明 Shade、skipBackground、完整 EventSystem 引用存在。

## 验证状态与后续优先事项

用户曾确认前期功能和三档 UI 正常，并表示先前完成内容已上传 GitHub；这不等于最新加速修改已提交或通过验证。旧对话最新修改仅检查差异格式，未运行编译/交互/计时。

优先验证：两次点击、三档 UI 和日历、弹窗位置/编辑器预览、不同宽高比的全屏覆盖、层级和闪烁、start0_1→start0_2 边界、选项停止、变量效果保留、快进设置恢复、退出时 AudioManager 残留。需要运行或修改场景时遵循当前用户授权；本次用户仅要求接收成果，先前要求暂不修改场景仍有效。

历史静态分析待核对风险（不是已确认故障）：DUEL.Begin 的 dayDatas[dayNumber] 与常见 dayNumber-1 索引差异；均匀抽牌路径未接稀有度保底；失败时公开保底字段与服务内部计数重置不一致；CardActionResolver._OnDestroy 不会自动触发；最终日统一走 TriggerGameFailedTransition。

## Unity MCP

本聊天已成功只读验证，旧聊天“没有工具”的判断已过时。2026-11-02 快照：projectG@0a012147f1b66474，Unity 2022.3.49f1c1，活动 Assets/Scenes/MainScene.unity，已加载、有未保存更改、7根对象，Console error 查询0条。快照不能替代新的编译与运行验证。

HTTP 配置历史值 http://127.0.0.1:8080/mcp。后续先读 custom-tools 和 instances；多实例明确路由。用资源读状态，用 Console error 小批量读取，必要时取堆栈；脚本修改后等待编译再检查。只在布局问题需要时截图，避免反复传整份日志。不要清空 Console、保存用户未保存场景或进入运行模式来完成单纯交接。

## 存档

## 新流程设计补充（2026-11-03，用户确认）

- 正式流程拟改为：教程（剧情、手工台、商店）→每日校园剧情与玩家操作→每2～3天小高潮判定→数个小循环后大高潮替代当次小高潮。小高潮无条件满足的候选时播放平庸剧情；大高潮无条件满足的候选时失败。当前仍处于方案阶段，尚未实施。
- 已看剧情仅影响抽取优先级，绝不作为资格硬排除：先筛选所有满足条件的剧情；其中有未看剧情则从未看集合随机；若全部已看则从全部满足条件的剧情随机。只有一个满足条件的剧情时，无论看过与否都选择它。
- 该规则用于帮助玩家收集图鉴；观看历史应支持跨周目持久化（具体存储接口待核对），不要仅依靠本轮记录。小高潮和大高潮的剧情池均采用此优先规则；已看完候选池不会触发平庸剧情或失败。
- 先前回复中“排除已看”“默认仅本轮已看”的方案已被本次用户澄清修正。

- PreviousChat.md：全部17轮用户消息和助手文字成果（含进度信息）。
- PreviousChat.tool-records.json：接口返回的全部分页记录，工具输出每项最多3000字符；可能截断。图片为原始本地路径引用，未复制或视觉复核。隐藏推理不属于可迁移成果。

后续聊天可直接要求“先读 Assets/ProjectHandoff/README.md，再接着工作”。
