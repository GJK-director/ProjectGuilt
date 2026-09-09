# Regression Test Map

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`bcdd67f63e9579851edfcee37d7fa6fb2a41b9dd`)

## Important Legacy Modes

| Mode | Current Coverage | Future Domains | Status |
|---|---|---|---|
| 103 | Legacy Mode103 → Shared Production Fixture Consumer → Shared synthetic factories → 28 checks retained；Test4 部分委托 EnemyIntent Formal Suite，Test5 完全委托；剩余 Definition/ownership、250% damage、Response/Presentation 回归仍在 Legacy | EnemyIntent、Bootstrap、Resolution | LEGACY_ACTIVE |
| 89 | Legacy Mode89 → FirstStrikeExecutionTests Formal Suite compatibility aggregation；13 FirstStrike Execution Priority Cases 完整由 Formal Suite 提供，Legacy 仅保留 aggregation / logging | Execution | LEGACY_ACTIVE |
| 109 | Legacy Mode109 → Mode89 compatibility runner → FirstStrikeExecutionTests Formal coverage；Cards-domain frozen values、Manifest membership/isolation、ResolveAvailableCardIDs、Shooting FirstStrike trait | Cards/Decks、Execution | LEGACY_ACTIVE |
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
- Execution domain：`BattleExecutionPlanFirstStrikePolicyTests` 及 Mode89 保持 Legacy，作为 Mode109 的跨域回归依赖。

Batch 5C Formal Coverage：

- Mode89：FORMAL COVERAGE COMPLETE。13 个 FirstStrike Execution Priority Case 由 `FirstStrikeExecutionTests` 提供，Legacy 仅保留 compatibility aggregation / logging。
- Mode109：Cards domain Formal Coverage COMPLETE；Execution dependency 为 `Mode89 compatibility runner → FirstStrikeExecutionTests Formal coverage`，不再是 Legacy Execution logic。

其余 Mode 的当前索引见 `LegacyModeMigration.md`。
