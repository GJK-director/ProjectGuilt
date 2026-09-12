# Menu And Settings

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

主菜单流、deck/display preference。

## NOT Responsible

manifest 成员定义、结算、Story 内部执行。

## Main Entry

MainMenuController.StartNewGame / GameSettingsState。

## Data

PlayerPrefs、Menu 序列化字段。

## Runtime Flow

Menu → NewGameText（执行 Story）→ BattleScene；`StartNewGame` 将当前 `SelectedDeck` snapshot 为一次性 pending deck，Bootstrap 优先消费 pending；无 pending 时使用 Inspector fallback。单独存在的 persistent preference 不覆盖直接 BattleScene Play。

## Invariants

默认值和 Scene 字段一起核对；explicit preset 不等于 startingCardIDs。Pending 是 session-only、consume-once，不得持久化；PlayerPrefs `SelectedDeck` 只记录菜单偏好。

## Dependencies

SceneManager、Screen、Bootstrap/DeckManifest。

## Regression Tests

Mode133 Settings integration；Bootstrap Suite。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

菜单设置与正式跳转链；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

不要把历史直达 BattleScene 记录当当前入口。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
