# Battle UI

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-16

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

局部卡牌/槽位/关系/状态/详情/Roll UI 与输入绑定。

## NOT Responsible

结算、Camera、Bootstrap Context 所有权。

## Main Entry

对应 View/Host；跨 Battle flow 才用 BattleSimpleUIController。

## Data

RuntimeState/CardState/Slot/Intent；Prefab/Scene。

## Runtime Flow

Scene 绑定 → Runtime View → 输入 Router → Planning；执行结果刷新 UI。

## Default Planning Source Selection

每次正式进入 `Prepare` 并完成当前回合的速度投掷与正式角色 Slot View 绑定后，`BattleSimpleUIController` 只自动选择一次默认来源：在 `runtimeState.allyUnits` 中按 `CharacterData.GetCurrentSpeed()` 降序、`BattleRuntimeState.GetBattlePositionIndex(...)` 升序选择第一个仍存活且可用的友方，并严格选择该角色的正式 Slot1。这个选择只属于 Planning UI，不改变 `BattleTurnProcessor` 的速度、行动槽或卡牌规则。

玩家可以随后切换到其他角色/槽位，或清空当前选择；普通 `RefreshView()` 不会重复夺回选择。下一回合进入新的 `Prepare` 后才重新执行一次。默认选择通过现有 `TrySelectActionSlotForPlanning(...)` 显示对应手牌，因此 `CardModeSwitchButton` 按既有手牌显隐规则自然出现。

## Sin Card Switch Button Contract

`CardModeSwitchButton` 位于 `BattleScene` 的 `Canvas_FixedBattleUI/CardModeSwitchButton`，由 `BattleSimpleUIController.qiehuanButton` 持有并绑定 `ToggleCardGroup()`。按钮直接跟随正式手牌 UI 的显隐生命周期，不单独维护 visibility state。

`BattleCardHandUIView.SetCards(...)` 对应手牌显示阶段，`ClearPlanningSelectionAndHideCards()` / `BattleCardHandUIView.ClearCards()` 对应手牌隐藏阶段；手牌显示时 `CardModeSwitchButton` 同步 active，手牌清理时同步 inactive。`Button.interactable` 不承担 Source Slot selection eligibility，Scene 默认值为 inactive；`ToggleCardGroup()` 内部 Runtime guard 仍然保留。

## Card Cooldown Visual

- Runtime source：`BattleCardState.currentCooldown`
- View：`BattleCardUIView`
- Prefab：`BattleCardUI.prefab`
- `currentCooldown > 0` → 显示 `CooldownOverlay` 和当前剩余 CD 文本。
- `currentCooldown <= 0` → 两者隐藏。
- UI 不保存第二份 CD 状态，不负责 CD Tick、卡牌使用资格或 `Update` polling。
- 当前通过 Bind / SetCard / 手牌重新建立刷新表现。

视觉配置归 Prefab：Overlay 色彩 / Alpha、TMP Font Size、TMP RectTransform Position、TMP Color。

交互要求：`CooldownOverlay` 和 `CooldownValueText` 的 `Raycast Target` 都必须为 `false`，视觉层不得阻挡 Hover / Click。

Legacy：现有旧 `cooldownText` / `HideLegacyCooldown()` 仍保持兼容状态，不要把新显示重新接回 legacy `cooldownText`。

Manual Verification：CD 0 无遮罩无数字；CD 1/3/10 显示对应数字；CD 下降后重新刷新手牌并在回到 0 后隐藏；Hover / Selection / Tooltip / Click 无回归。

## Auto Clash Input Boundary

Planning Space 与 Execution Roll Space 由 `BattleSimpleUIController.HandleBattleSpaceInput()` 读取，但按当前是否处于 `isScenePresentedTurnCycleRunning` 分流。

Planning 阶段仍由 Space 调用 `TryStartCompleteTurnCycle()`。Auto ON 不会自动开始 Planning。

执行阶段只有 Runner 的 `IsWaitingForInput` 为 true 时才会调用 `BattleLifecycleController.TryRequestManualRoll()`。Auto Runner 不处于 `WaitingForRoll` 输入状态，因此 Execution Manual Roll Space 不会进入该方法；计时和自动继续由 `BattleExecutionRunner` 负责。

## Targeting Contract

规划期从已选 `SelectedSourceSlot` 出发：Attack 只能指向 `EnemyActionSlot`；Defense/Dodge 可以确认自身槽位或 `EnemyActionSlot`；Ability 可以确认自身角色命中区或 `EnemyActionSlot`。正式自身放置仍写入 `BattleActionPlacementType.Self`，不生成行动关系线。

角色命中区由 `BattleCharacterTargetHitbox` 接收点击和 Hover；只有当前来源角色的 Ability 会激活它。`BattleCharacterTargetOutline` 是独立的可选目标轮廓，Hover 不改变轮廓显隐。没有新命中区时，`BattleCharacterStatusUIView` 保留旧 `BattleSelfActionDropZone` 行为；接入命中区后旧区域仅关闭 Graphic 射线。

新增命中区需要在 Ally Status UI 上人工放置透明 Graphic，设置其 RectTransform 后绑定 `BattleCharacterStatusUIView.characterTargetHitbox`；新增轮廓需要在 Ally World Prefab 实例上人工添加并配置 `BattleCharacterTargetOutline`。缺少这些可选引用不会阻止 Runtime 生成。

## Action Order UI Contract

正式数据显示链为：`BattlePlanningOrderSnapshot` → `BattleSimpleUIController.RefreshActionSlotOrderViews()` → `BattleCharacterStatusUIView.SetSlotOrder(...)` → `BattleActionSlotUIView.SetOrder()` / `ClearOrder()` → TMP `OrderText`。

Controller 只在 `Prepare` / `PlanReady` 阶段应用 Planning display order；其他阶段先清除四个角色状态 View 的槽位 order。View 只负责显示，不读取 Resolver、Snapshot 或运行时行动关系，也不自行计算排序。`SetOrder(0)` 仍显示 `0`；负数直接清除；绑定到不同角色或槽位时清除旧数字；缺少 `orderText` 时安全跳过，并在真正调用 `SetOrder` 时最多输出一次明确警告。

正式 Prefab 为 `Assets/Prefabs/Battle/Units/UI/AllyStatusUI.prefab` 与 `Assets/Prefabs/Battle/Units/UI/EnemyStatusUI.prefab`。每个 `Slot_01` / `Slot_02` 都有 `OrderText` / `Order Text` TMP 子物体；四个 OrderText 的 `Raycast Target` 保持关闭。正式 Slot identity 不由 Prefab 中的视觉 X 坐标或镜像外观决定，映射由 `BattleCharacterStatusUIView` 的 `slot01View` / `slot02View` 以及 UI slot index 决定。不要因为 Enemy Prefab 看起来左右镜像而交换正式 Slot1 / Slot2 引用，也不要断开 `BattleActionSlotUIView.orderText` 的序列化引用。

## Action Order Visual Iteration

行动顺序数字的视觉迭代只调整上述 Prefab 中 OrderText 的 RectTransform、字体、字号、颜色、对齐方式、TMP Material，以及未来明确属于数字显示的背景、图标、FirstStrike 边框或动画。不得通过修改 Controller、Snapshot、Resolver、slot mapping 或 sorting 来实现视觉调整。

未来如抽取独立 View，应保持 `BattleActionSlotUIView` → `ActionOrderIndicatorUIView` → Image/TMP/Animator 的显示边界，并继续保留 `SetOrder` / `ClearOrder` 作为接线契约；本阶段不提前接入该抽取。

## Action Slot Card Detail Hover Contract

已安排行动槽的详情 Hover 仍由 `BattleActionSlotUIView` 生成正常 `BattleActionSlotCardInfoHoverRequest`，交给 `BattleActionSlotCardInfoPanelHost`。Host 继续复用现有 Ally / Enemy 两块详情面板，不新增第三块面板或比较布局。

正常 Hover 与 paired request 共用 `BattleActionSlotUIView.BuildCardInfoPanelRequest(...)` 的 payload 构造；Controller 只负责提供当前最终 partner。

当存在正式最终 response relation 时，Hover 当前 Slot 会同时显示另一端原本的卡牌详情。Ally 侧读取当前 `BattleActionSlot.enemyIntent`；Enemy 侧通过 `BattleActionSlotManager.TryFindCurrentResponseSlot(...)` 查找最终 responder。`requestedEnemyIntent` 只是原始放置信息，不是 UI partner truth。

paired detail 使用 Host 内独立的 `pairedOwnerSource` / `pairedRequest` 状态，优先级低于真实 Hover 和 Locked 内容。它不会写入 `hoveredSource` 或 `lockedSource`，也不会伪造 partner 的 PointerEnter / PointerExit。真实来源 Hover 生命周期结束或被 SourceInvalidated 清除时，对应 paired detail 一起清除；另一侧真实 Hover / Locked 内容不受影响。

当前 BattleScene 使用 `Assets/Prefabs/BattleActionSlotCardInfoPanel.prefab` 的正式 Ally / Enemy panel 接线；B3 不修改 Prefab、Scene、面板位置或布局。没有 response relation 时只显示当前 Slot 详情。

## Keyword Tooltip Contract

卡牌黄色词条 Hover 的 owner 是 `BattleCardUIView`。命中顺序为 Exact TMP Link Hit → Padding Fallback；Fallback 按 TMP 字符的真实行分段计算，并由 `keywordHoverPaddingX` / `keywordHoverPaddingY` 控制，不通过透明 UI 覆盖层实现，因此不会新增 Raycast interception layer。

二级面板的打开、关闭和 Source/Panel Hover ownership 由 `BattleSecondaryInfoPanelHost` 负责。`hoverOpenDelay` 是 Inspector 可调字段，使用 `Time.unscaledTime`；代码默认值为 `0.45f`，当前正式 `Assets/Prefabs/Battle/Units/UI/BattleSecondaryInfoPanel.prefab` 保存值为 `0.1`。

`DefaultCloseGrace` 仍为 `0.12f`，本次未 Inspector 化。`BattleCardTooltipResolver`、卡牌点击、关键词数据来源以及现有 Clear/Exit/Disable 生命周期不属于该配置入口。

## Buff Detail Panel Contract

Runtime owner 是 `BattleBuffGroupUIView` 与 `BattleSecondaryInfoPanelHost`：前者读取角色当前每个 `buffID` 唯一的 canonical `BuffData`，生成一级 `BattleBuffIconUIView` 与 `BattleSecondaryInfoContent` / `BattleSecondaryInfoBubbleData`；后者在 `ApplyContent(...)` 中选择并刷新二级面板。每个 canonical state 生成一条 summary Bubble，Details 至少显示 `stack`，定义需要时再显示 `intensity`；不按来源、持续时间或旧 batch 拆分。没有 Buff bubble data 时，`BuffDetailRoot` 不显示，普通 Card Keyword 仍使用原有 Title / Body / Footer 路径。

当前正式 Prefab 是 `Assets/Prefabs/Battle/Units/UI/BattleSecondaryInfoPanel.prefab`。`BattleSecondaryInfoPanelHost` 的相关序列化字段是 `buffSummaryRoot`、`stackLabelText`、`stackValueText`、`durationLabelText`、`durationValueText`、`buffDetailRoot`、`bubbleContainer` 和 `bubbleTemplate`。本地层级为：

```text
SecondaryInfoPanel
├─ BuffSummaryRoot
│  ├─ StackBubble
│  └─ DurationBubble
├─ Body
├─ BuffDetailRoot
│  └─ BubbleContainer
│     └─ BubbleTemplate
│        ├─ Source
│        └─ Details
├─ Title
└─ Footer
```

`BuffDetailRoot` 和 `BubbleContainer` 是 `RectTransform + VerticalLayoutGroup + ContentSizeFitter` 的布局容器。`BubbleTemplate` 是 `RectTransform + CanvasRenderer + VerticalLayoutGroup + Image + ContentSizeFitter`，其子对象 `Source` / `Details` 是 TMP 文本；当前 canonical Host 只向 `Details` 写入 `BattleSecondaryInfoBubbleData.detailsText`，`Source` 与 `BuffSummaryRoot` 下的旧 `StackBubble` / `DurationBubble` 保留为兼容结构但不承载 canonical Buff Detail 数据。当前 BubbleTemplate Image 是 Prefab 保存的暖灰色背景（约 `0.18 / 0.17 / 0.14 / 0.96`），所有 Bubble Graphic 的 `Raycast Target` 关闭。布局的 padding、spacing、TMP Font Size、TMP Color、背景 Image / Outline 和 RectTransform 属于 Prefab 视觉入口，不是 Gameplay 规则。

Bubble 内容按 canonical state 字段显示：`Details` 写入 `stack`，当 `BuffDefinitionData.defaultIntensity != 0` 或定义提供正的 `maxIntensity` 时追加 `intensity`。`retainWhenZero` / `showWhenZero` / `consumeRule` 属于 Runtime definition contract，不在 Bubble 中伪造；因此 Strength 的 Bubble 显示层数与强度，Bullet 的 Bubble 显示资源层数。

Anger 的 0 层显示也不由 UI 伪造：只有真实 Anger canonical state 存在且定义的 `showWhenZero=true` 时，一级图标和 StackText 才显示 `0`。UI 不增加 Anger 专属 whitelist 或生命周期判断。

中文字体入口是 `Assets/Fonts/TMP_Font_CN_Runtime.asset`。当前主字体保持 Static，并通过 `fallbackFontAssetTable` 接入 `Assets/Fonts/TMP_Font_CN_DynamicFallback.asset`；DynamicFallback 的 source 是 `Assets/Fonts/SIMHEI.TTF`，自身 fallback list 为空。`BattleSecondaryInfoPanelHost.ApplySourceFont(...)` 会把解析到的字体传给面板及 Bubble 内 TMP。调整字体资产或 fallback 链属于 Fonts 维护范围，不在 Bubble Gameplay/View 代码中硬编码。

只改背景、边界、字体、颜色、字号、RectTransform、VerticalLayoutGroup 间距/Padding 或 Raycast Target：改 Prefab 即可。要改变 Bubble 是否按实例拆分、特殊资源是否聚合、来源/持续时间条件或新增显示字段：需要同时检查 `BattleBuffGroupUIView` 与 `BattleSecondaryInfoPanelHost` 的 Runtime/View 映射；要改变层数、消费或保留规则：回到 Buff Runtime owner，不在 UI 中实现。

## Buff Icon Hover Contract

角色脚底 Buff 图标的 Hover 事件仍由 `BattleBuffIconUIView` 处理；该组件挂在 `BuffTemplate` 根节点，运行时由 `BattleBuffGroupUIView` 复用同一个模板生成图标。正式模板位于 `Assets/Prefabs/Battle/Units/UI/AllyStatusUI.prefab` 与 `Assets/Prefabs/Battle/Units/UI/EnemyStatusUI.prefab` 的 `FootStatusGroup/BuffGroup/BuffTemplate`。

`BuffTemplate/HoverHitbox` 是唯一用于 Hover 命中的 Graphic：它使用透明 `Image`，`Raycast Target` 开启；`IconImage` 与 `StackText` 只负责视觉显示，`Raycast Target` 关闭。`HoverHitbox` 的 RectTransform `Anchored Position X/Y`、`Width` 与 `Height` 是 Inspector 中统一调整角色 Buff Hover 范围的入口，`BattleSecondaryInfoPanelHost` 不计算该范围，也不按 `buffID` 单独配置。

Ally 与 Enemy 两个正式 Prefab 的 `BuffTemplate/HoverHitbox` 参数应保持一致。需要扩大或缩小判定框时只调整两个模板的 HoverHitbox RectTransform，不要为了改变命中范围而放大视觉 Icon 或文本。

正式 Buff 图标已移除 legacy `DecayText`；当前没有通用的下回合减层量或通用 TurnEnd decay 指示器。Buff 图标只显示当前 canonical state 的 stacks，HoverHitbox 独立负责命中范围。未来如需显示明确的剩余值，必须读取真实 Runtime state，不得恢复通用 `DecayText` 或用 Prefab 静态文本预测生命周期。

## SelfActionDropZone Contract

`SelfActionDropZone` 是 Ally Status UI 的自身目标判定区域。它由 `BattleCharacterStatusWorldFollower` 负责世界跟随，使用角色的 `Center World Anchor` 投影到 Canvas；`Center Offset` 是人工布局偏移，`SelfActionDropZone` RectTransform 的 Width/Height 是人工判定范围。运行时投影持续写入位置，不能把运行时 Pos X/Pos Y 当作正式默认布局入口。

`Ability`、`Defense`、`Dodge` 可以进行 Self placement；`Attack` 禁止 Self。正式 Self placement 写入 `BattleActionPlacementType.Self`，不生成正式 Action Relation Line。

## Damage Number UI Contract

`BattleDamageNumberPresenter` 是 Damage Number owner；`BattleSceneExecutionPresenter.OnImpactCommitted` 在真实 `BattleImpact` Commit 后调用它，并传入 `impact.resolvedDamage`。目标坐标使用 `BattleUnitViewHandle.CenterAnchor`（缺少时 fallback `WorldRoot`）经 `WorldFollower.ResolvedTargetCanvas` 和 `WorldCamera` 投影到 Canvas。当前 `BattleScene` 绑定、`Font Size` / `Text Color` / `Lifetime` 以及未实现的 Fade、Move、避让、Crit、Heal、Shield、pooling 等限制见 [Damage](../Battle/Damage.md)。

## Invariants

View 不自行提交伤害；选择状态与正式安排分开；Data 与 Prefab 配置分别核验。

## Dependencies

Planning、Lifecycle、Spawner、Presentation。

## Regression Tests

UI/关键词/关系线/世界跟随 Legacy。Mode67 覆盖目标点击安排，Mode73 覆盖命中区顶部中心预览，Mode75 覆盖目标表面与轮廓生命周期。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

BattleScene、Buff Preview、对应操作验收；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT：总 Controller；ForTesting seams/Prefab preview 保留。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
