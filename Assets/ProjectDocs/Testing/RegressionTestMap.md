# Regression Test Map

Status: TRANSITIONAL
Last Verified: 2026-09-10
Repository Basis: `5db805ea452288e86502df0b3075becb7f8f4024`

## Important Legacy Modes

| Mode | Current Coverage | Future Domains | Status |
|---|---|---|---|
| 103 | Legacy Mode103 → Shared Production Fixture Consumer → Shared synthetic factories → 28 checks retained；Test4 部分委托 EnemyIntent Formal Suite，Test5 完全委托；剩余 Definition/ownership、250% damage、Response/Presentation 回归仍在 Legacy | EnemyIntent、Bootstrap、Resolution | LEGACY_ACTIVE |
| 86 | `BattleFirstStrikeExecutionPlanBasic` 仍为 active standalone；`FirstStrikeExecutionTests` 覆盖 execution priority/order/pairing，但 JSON traits missing/null/empty compatibility 与 LongRangeShoot non-implication 仍为 Legacy unique coverage | Execution/Cards | PARTIAL_FORMAL_COVERAGE / LEGACY_ACTIVE / JIT_MIGRATION_PENDING |
| 89 | 无 active enum/standalone dispatch；保留 `BattleExecutionPlanFirstStrikePolicyTests` compatibility wrapper，13 个 Case 由 `FirstStrikeExecutionTests` 提供 | Execution | STANDALONE_RETIRED |
| 109 | 无 active enum/standalone dispatch；保留 `BattleDeckManifestTests` historical compatibility wrapper，Cards Cases 由 `CardDeckManifestTests` 提供，Execution 经 retained Mode89 wrapper → `FirstStrikeExecutionTests`；Mode114 仍消费 `BattleDeckManifestTests.Run(cards)` | Cards/Decks、Execution | STANDALONE_RETIRED |
| 114 | 无 active enum/standalone dispatch；保留 `BattleDeckBootstrapPresetTests` historical compatibility wrapper，Cards 由 `CardDeckManifestTests`、Bootstrap 由 `DeckPresetBootstrapTests` 提供；Mode115 仍消费 `BattleDeckBootstrapPresetTests.Run(cards)`，Execution 经 retained Mode109 wrapper → retained Mode89 wrapper → `FirstStrikeExecutionTests` | Cards/Decks、Bootstrap、Execution | STANDALONE_RETIRED |
| 107 | Anger、Knife、Iai、Double Slash、Heavy、Breath、staged HP 等组合回归 | Cards/Knife、Resolution/MultiImpact | LEGACY_ACTIVE |
| 113 | Conservation、0 Bullet、Cooldown、能力回归链，并调用部分 Shooting/Ability 回归 | Cards/Shooting、Buffs | LEGACY_ACTIVE |
| 132 | Card Keyword Presentation、Timing Vocabulary、Tooltip/Description formatting | UI/Cards、Cards/Keywords | LEGACY_ACTIVE |
| 133 | Settings、Deck preference、Display mapping、Bootstrap preset | Settings、Bootstrap、Cards/Decks | LEGACY_ACTIVE |

以上状态只描述当前 Legacy Mode 事实，不表示未来迁移已经完成，也不输出 KEEP/DELETE/MERGE 决策。

Batch 5A Formal Coverage：

- Test4：PARTIAL。Formal coverage includes cycle fixed target definitions, repeat flag, runtime rounds 1..21 and duplicate runtime card-state identity；Automatic Turn Cycle / provider integration remains Legacy。
- Test5：FORMAL COVERAGE COMPLETE，通过 `EnemyIntentTests.LegacyPatternFallbackCreatesExpectedTwoSlotQueue`。

Batch 5B Formal Coverage：

- Cards domain：`CardDeckManifestTests` 提供四个 Formal Case，覆盖 Knife 数值、两套 Manifest 成员与隔离、可用卡牌解析和 Shooting FirstStrike Trait；Mode109 standalone 已退休，`BattleDeckManifestTests` wrapper 继续作为 Mode114 的 compatibility consumer。
- Execution domain：Mode89 standalone 已退休；`BattleExecutionPlanFirstStrikePolicyTests` wrapper 仍作为 Mode109 的跨域回归依赖。

Batch 5C Formal Coverage：

- Mode89：FORMAL COVERAGE COMPLETE；standalone enum/dispatch 已退休，13 个 FirstStrike Execution Priority Case 由 `FirstStrikeExecutionTests` 提供，wrapper 仅保留历史兼容 aggregation / logging。
- Mode109：Cards domain Formal Coverage COMPLETE；standalone entry 已退休，`BattleDeckManifestTests` wrapper 仅为 Mode114 保留；Execution dependency 为 `保留的 Mode89 compatibility wrapper → FirstStrikeExecutionTests Formal coverage`，不再是 Legacy Execution logic。

Batch 5D Formal Coverage：

- Mode114：FORMAL COVERAGE COMPLETE。Cards domain 由 `CardDeckManifestTests` 提供 6 个 Case；Bootstrap domain 由 `DeckPresetBootstrapTests` 提供 11 个 Case。Legacy Mode114 仅保留 aggregation / logging。
- Mode114 的 `DeckManifest` regression 仍通过 `BattleDeckManifestTests` / retained Mode109 wrapper，继续消费 Cards 与 Mode89/Execution Formal coverage。

Batch 6A Retirement：

- Mode89 的 standalone `BattleTestMode` enum member 与 `CardLoadTest.Start()` dispatch 已移除；Mode89 不再是可从 Inspector 选择的 standalone runner。
- `BattleExecutionPlanFirstStrikePolicyTests` wrapper、13 个 FirstStrike Case 和 Mode109/Mode114 的兼容调用保留。

Batch 6B Retirement：

- Mode109 的 standalone `BattleTestMode` enum member 与 `CardLoadTest.Start()` dispatch 已移除；Mode109 不再是可从 Inspector 选择的 standalone runner。
- `BattleDeckManifestTests` wrapper、Cards Formal Suite、retained Mode89 wrapper 和 Mode114 consumer 全部保留。

Batch 6C Retirement：

- Mode114 的 standalone `BattleTestMode` enum member 与 `CardLoadTest.Start()` dispatch 已移除；Mode114 不再是可从 Inspector 选择的 standalone runner。
- `BattleDeckBootstrapPresetTests` wrapper、Cards Formal Suite、Bootstrap Formal Suite、retained Mode109 wrapper 和 Mode115 consumer 全部保留。

其余 Mode 的当前索引见 `LegacyModeMigration.md`。

## Phase 6 Governance Closure

- Phase6D-A：110 个 active Legacy Mode 已完成 inventory/value audit。
- Phase6D-B：110 个 Mode 概念性归并为 30 个 Contract Cluster，其中 24 个 automated-oriented、6 个 manual/design-oriented；Map 与 carrier 建议见 `LegacyContractTriage.md`。
- Phase6E Revised：Mode86 保持 active。其 Formal overlap 仅覆盖 FirstStrike execution priority/order/pairing；JSON trait compatibility 与 LongRangeShoot 不自动产生 FirstStrike 仍未迁移。

采用 `JUST_IN_TIME_TEST_MIGRATION`：未来修改 Production system 前先查本表与 Contract Map，只迁移仍有价值的相关 Contract；不为历史 Mode 一对一创建 Suite，也不以 active Legacy Mode 清零作为当前 Demo 阻塞条件。

**Phase6 is CLOSED FOR CURRENT DEMO GOVERNANCE.** 剩余 Legacy coverage 按需迁移；这不表示所有 Legacy Mode 已退休、所有测试已 Formal 化或所有 Manual Harness 已建立。UI / Camera / Animation / Presentation 继续使用按需 shared harness，不在本批创建。
