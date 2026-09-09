# Regression Test Map

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`4abc9db9fb782b96255288f7504b5f1d848f0852`)

## Important Legacy Modes

| Mode | Current Coverage | Future Domains | Status |
|---|---|---|---|
| 103 | Legacy Mode103 → Shared Production Fixture Consumer → Shared synthetic factories → 28 checks retained；Test4 部分委托 EnemyIntent Formal Suite，Test5 完全委托；剩余 Definition/ownership、250% damage、Response/Presentation 回归仍在 Legacy | EnemyIntent、Bootstrap、Resolution | LEGACY_ACTIVE |
| 107 | Anger、Knife、Iai、Double Slash、Heavy、Breath、staged HP 等组合回归 | Cards/Knife、Resolution/MultiImpact | LEGACY_ACTIVE |
| 113 | Conservation、0 Bullet、Cooldown、能力回归链，并调用部分 Shooting/Ability 回归 | Cards/Shooting、Buffs | LEGACY_ACTIVE |
| 132 | Card Keyword Presentation、Timing Vocabulary、Tooltip/Description formatting | UI/Cards、Cards/Keywords | LEGACY_ACTIVE |
| 133 | Settings、Deck preference、Display mapping、Bootstrap preset | Settings、Bootstrap、Cards/Decks | LEGACY_ACTIVE |

以上状态只描述当前 Legacy Mode 事实，不表示未来迁移已经完成，也不输出 KEEP/DELETE/MERGE 决策。

Batch 5A Formal Coverage：

- Test4：PARTIAL。Formal coverage includes cycle fixed target definitions, repeat flag, runtime rounds 1..21 and duplicate runtime card-state identity；Automatic Turn Cycle / provider integration remains Legacy。
- Test5：FORMAL COVERAGE COMPLETE，通过 `EnemyIntentTests.LegacyPatternFallbackCreatesExpectedTwoSlotQueue`。

其余 Mode 的当前索引见 `LegacyModeMigration.md`。
