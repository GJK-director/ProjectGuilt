# Test Suites

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`1fab2f48f34395a5a4fb639a70da02c75b67c704`)

未来按系统组织 Cards、Buffs、Resolution、Execution、Lifecycle、EnemyIntent、Bootstrap、Settings、UI、Presentation Suite。

Stage5A 已建立第一套 Formal Suite：`EnemyIntentTests`。没有 Legacy Mode 被删除或退役，Mode103 暂时作为 compatibility consumer。

Batch 5B 已建立第二套 Formal Suite：`Cards/CardDeckManifestTests`。该 Suite 按 Cards 域提供四个 Case，Mode109 继续作为 compatibility consumer；没有 Legacy Mode 被删除或退役。

Batch 5C 已建立第三套 Formal Suite：`Execution/FirstStrikeExecutionTests`。当前已有 `EnemyIntentTests`、`CardDeckManifestTests` 和 `FirstStrikeExecutionTests` 三套 Formal Suite；Mode89 与 Mode109 仍保留为 compatibility runner，没有 Legacy Mode 被退役。

Batch 5D 已建立第四套 Formal Suite：`Bootstrap/DeckPresetBootstrapTests`。当前已有 EnemyIntent、Cards、Execution、Bootstrap 四套 Formal Suite；本批首次验证已有 Cards Suite 可以增加 Case 服务另一个 Legacy compatibility runner，没有为 Mode114 创建重复的 Deck Manifest 测试环境。
