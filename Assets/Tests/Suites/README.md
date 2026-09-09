# Test Suites

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`bcdd67f63e9579851edfcee37d7fa6fb2a41b9dd`)

未来按系统组织 Cards、Buffs、Resolution、Execution、Lifecycle、EnemyIntent、Bootstrap、Settings、UI、Presentation Suite。

Stage5A 已建立第一套 Formal Suite：`EnemyIntentTests`。没有 Legacy Mode 被删除或退役，Mode103 暂时作为 compatibility consumer。

Batch 5B 已建立第二套 Formal Suite：`Cards/CardDeckManifestTests`。该 Suite 按 Cards 域提供四个 Case，Mode109 继续作为 compatibility consumer；没有 Legacy Mode 被删除或退役。

Batch 5C 已建立第三套 Formal Suite：`Execution/FirstStrikeExecutionTests`。当前已有 `EnemyIntentTests`、`CardDeckManifestTests` 和 `FirstStrikeExecutionTests` 三套 Formal Suite；Mode89 与 Mode109 仍保留为 compatibility runner，没有 Legacy Mode 被退役。
