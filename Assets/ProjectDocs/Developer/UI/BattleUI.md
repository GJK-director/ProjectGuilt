# Battle UI

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-15

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

## Keyword Tooltip Contract

卡牌黄色词条 Hover 的 owner 是 `BattleCardUIView`。命中顺序为 Exact TMP Link Hit → Padding Fallback；Fallback 按 TMP 字符的真实行分段计算，并由 `keywordHoverPaddingX` / `keywordHoverPaddingY` 控制，不通过透明 UI 覆盖层实现，因此不会新增 Raycast interception layer。

二级面板的打开、关闭和 Source/Panel Hover ownership 由 `BattleSecondaryInfoPanelHost` 负责。`hoverOpenDelay` 是 Inspector 可调字段，使用 `Time.unscaledTime`；代码默认值为 `0.45f`，当前正式 `Assets/Prefabs/Battle/Units/UI/BattleSecondaryInfoPanel.prefab` 保存值为 `0.1`。

`DefaultCloseGrace` 仍为 `0.12f`，本次未 Inspector 化。`BattleCardTooltipResolver`、卡牌点击、关键词数据来源以及现有 Clear/Exit/Disable 生命周期不属于该配置入口。

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
