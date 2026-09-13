# Battle UI

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

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

## Targeting Contract

规划期从已选 `SelectedSourceSlot` 出发：Attack 只能指向 `EnemyActionSlot`；Defense/Dodge 可以确认自身槽位或 `EnemyActionSlot`；Ability 可以确认自身角色命中区或 `EnemyActionSlot`。正式自身放置仍写入 `BattleActionPlacementType.Self`，不生成行动关系线。

角色命中区由 `BattleCharacterTargetHitbox` 接收点击和 Hover；只有当前来源角色的 Ability 会激活它。`BattleCharacterTargetOutline` 是独立的可选目标轮廓，Hover 不改变轮廓显隐。没有新命中区时，`BattleCharacterStatusUIView` 保留旧 `BattleSelfActionDropZone` 行为；接入命中区后旧区域仅关闭 Graphic 射线。

新增命中区需要在 Ally Status UI 上人工放置透明 Graphic，设置其 RectTransform 后绑定 `BattleCharacterStatusUIView.characterTargetHitbox`；新增轮廓需要在 Ally World Prefab 实例上人工添加并配置 `BattleCharacterTargetOutline`。缺少这些可选引用不会阻止 Runtime 生成。

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
