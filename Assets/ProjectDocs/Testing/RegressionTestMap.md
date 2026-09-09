# Regression Test Map

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`fea7fabf2bdf75f6c6eceb8ababf88c6565a3eb0`)

## Important Legacy Modes

| Mode | Current Coverage | Future Domains | Status |
|---|---|---|---|
| 103 | Legacy Mode103 → Shared Production Fixture Consumer → Shared synthetic factories → 28 checks retained；Test4 部分委托 EnemyIntent Formal Suite，Test5 完全委托；剩余 Definition/ownership、250% damage、Response/Presentation 回归仍在 Legacy | EnemyIntent、Bootstrap、Resolution | LEGACY_ACTIVE |
| 89 | 无 active enum/standalone dispatch；保留 `BattleExecutionPlanFirstStrikePolicyTests` compatibility wrapper，13 个 Case 由 `FirstStrikeExecutionTests` 提供 | Execution | STANDALONE_RETIRED |
| 109 | Legacy Mode109 → 保留的历史 Mode89 compatibility wrapper → `FirstStrikeExecutionTests` Formal coverage；Cards-domain frozen values、Manifest membership/isolation、ResolveAvailableCardIDs、Shooting FirstStrike trait | Cards/Decks、Execution | LEGACY_ACTIVE |
| 114 | Legacy Mode114 → Cards/CardDeckManifestTests + Bootstrap/DeckPresetBootstrapTests Formal coverage；Cross-domain compatibility 仍通过 BattleDeckManifestTests / Mode109 → Cards + Mode89/Execution Formal | Cards/Decks、Bootstrap、Execution | LEGACY_ACTIVE |
| 107 | Anger、Knife、Iai、Double Slash、Heavy、Breath、staged HP 等组合回归 | Cards/Knife、Resolution/MultiImpact | LEGACY_ACTIVE |
| 113 | Conservation、0 Bullet、Cooldown、能力回归链，并调用部分 Shooting/Ability 回归 | Cards/Shooting、Buffs | LEGACY_ACTIVE |
| 132 | Card Keyword Presentation、Timing Vocabulary、Tooltip/Description formatting | UI/Cards、Cards/Keywords | LEGACY_ACTIVE |
| 133 | Settings、Deck preference、Display mapping、Bootstrap preset | Settings、Bootstrap、Cards/Decks | LEGACY_ACTIVE |

以上状态只描述当前 Legacy Mode 事实，不表示未来迁移已经完成，也不输出 KEEP/DELETE/MERGE 决策。

Batch 5A Formal Coverage：

- Test4：PARTIAL。Formal coverage includes cycle fixed target definitions, repeat flag, runtime rounds 1..21 and duplicate runtime card-state identity；Automatic Turn Cycle / provider integration remains Legacy。
- Test5：FORMAL COVERAGE COMPLETE，通过 `EnemyIntentTests.LegacyPatternFallbackCreatesExpectedTwoSlotQueue`。

Batch 5B Formal Coverage：

- Cards domain：`CardDeckManifestTests` 提供四个 Formal Case，覆盖 Knife 数值、两套 Manifest 成员与隔离、可用卡牌解析和 Shooting FirstStrike Trait；Mode109 继续作为 compatibility consumer。
- Execution domain：Mode89 standalone 已退休；`BattleExecutionPlanFirstStrikePolicyTests` wrapper 仍作为 Mode109 的跨域回归依赖。

Batch 5C Formal Coverage：

- Mode89：FORMAL COVERAGE COMPLETE；standalone enum/dispatch 已退休，13 个 FirstStrike Execution Priority Case 由 `FirstStrikeExecutionTests` 提供，wrapper 仅保留历史兼容 aggregation / logging。
- Mode109：Cards domain Formal Coverage COMPLETE；Execution dependency 为 `保留的 Mode89 compatibility wrapper → FirstStrikeExecutionTests Formal coverage`，不再是 Legacy Execution logic。

Batch 5D Formal Coverage：

- Mode114：FORMAL COVERAGE COMPLETE。Cards domain 由 `CardDeckManifestTests` 提供 6 个 Case；Bootstrap domain 由 `DeckPresetBootstrapTests` 提供 11 个 Case。Legacy Mode114 仅保留 aggregation / logging。
- Mode114 的 `DeckManifest` regression 仍通过 `BattleDeckManifestTests` / Mode109 wrapper，继续消费 Cards 与 Mode89/Execution Formal coverage。

Batch 6A Retirement：

- Mode89 的 standalone `BattleTestMode` enum member 与 `CardLoadTest.Start()` dispatch 已移除；Mode89 不再是可从 Inspector 选择的 standalone runner。
- `BattleExecutionPlanFirstStrikePolicyTests` wrapper、13 个 FirstStrike Case 和 Mode109/Mode114 的兼容调用保留。

其余 Mode 的当前索引见 `LegacyModeMigration.md`。
