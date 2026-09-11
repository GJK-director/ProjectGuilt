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

Menu → NewGameText → BattleScene；Bootstrap 读取已选 deck，否则 Inspector fallback。

## Invariants

默认值和 Scene 字段一起核对；explicit preset 不等于 startingCardIDs。

## Dependencies

SceneManager、Screen、Bootstrap/DeckManifest。

## Regression Tests

Mode133 Settings integration；Bootstrap Suite。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

菜单设置与正式跳转链；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

不要把历史直达 BattleScene 记录当当前入口。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
