# Legacy Contract Triage

Status: TRANSITIONAL  
Last Verified: 2026-09-10  
Repository Basis: `5db805ea452288e86502df0b3075becb7f8f4024`

## Purpose

Phase6D-B 将 Legacy Mode 从“一个 Mode 一个迁移目标”改为 Contract Cluster 治理。Cluster 是长期 coverage 边界，不是当前必须建立的 Suite 数量。未来只在相关 Production system 修改或 regression value 足够高时，按 `JUST_IN_TIME_TEST_MIGRATION` 选择 carrier。

## Baseline

- Active Legacy Modes：110
- Conceptual Contract Clusters：30
- Automated-oriented clusters：24
- Manual/design-oriented clusters：6
- Existing Formal Suite carriers：EnemyIntent、Cards、Execution、Bootstrap
- Mode86：`ACTIVE_ENUM` + `PARTIAL_FORMAL_COVERAGE` + `JIT_MIGRATION_PENDING`

## Contract Cluster Map

| ID | Domain | Contract Name | Source Legacy Modes | Recommended Carrier |
|---|---|---|---|---|
| C01 | Lifecycle | Turn start/end and terminal state | 2, 3, 44, 46, 61, 76, 77, 78 | INTEGRATION_SUITE |
| C02 | Resolution | Responded Attack-v-Attack outcomes | 7, 8, 9, 10 | EXISTING_FORMAL_SUITE |
| C03 | Resolution | Defense full/reduced block and known point | 11, 12, 13, 25, 26, 32, 33 | EXISTING_FORMAL_SUITE |
| C04 | Execution | Free/unresponded action planning and completion | 19, 20, 22, 45, 51, 79 | EXISTING_FORMAL_SUITE |
| C05 | Execution | Speed, tie, and item ordering | 39, 40, 58, 80, 81 | EXISTING_FORMAL_SUITE |
| C06 | Guard | Passive guard selection and fallback | 36, 37 | EXISTING_FORMAL_SUITE |
| C07 | Dodge | Dodge selection, failure, and continuation | 41, 42, 43, 59 | EXISTING_FORMAL_SUITE |
| C08 | Buffs | Buff definition, trigger, timing, and expiry | 47, 48, 49, 50, 72 | EXISTING_FORMAL_SUITE |
| C09 | Cards | Card resource, cooldown, and use-count semantics | 53, 55, 63, 85, 105 | EXISTING_FORMAL_SUITE |
| C10 | Cards | Card assignment eligibility and target ownership | 54, 57, 60, 66, 67 | EXISTING_FORMAL_SUITE |
| C11 | Cards | Card data, defaults, and production references | 56, 101, 103 | INTEGRATION_SUITE |
| C12 | Cards | FirstStrike priority and participant source | 86, 89, 109 | EXISTING_FORMAL_SUITE |
| C13 | Interactions | Interaction classification and effective context | 87, 90, 91 | EXISTING_FORMAL_SUITE |
| C14 | Presentation | Presentation protocol and engagement lifecycle | 83, 84, 95, 97, 98 | SHARED_MANUAL_HARNESS |
| C15 | Execution | Pausable execution and roll gating | 96, 104 | SHARED_MANUAL_HARNESS |
| C16 | Decks | Manifest membership, isolation, and ordering | 109, 114, 115 | EXISTING_FORMAL_SUITE |
| C17 | Ability | Ability phase ordering and one-shot lifecycle | 110, 111, 117, 118 | EXISTING_FORMAL_SUITE |
| C18 | Resources | CardUsed resource payment and special state | 119, 120, 121, 122, 127, 128 | EXISTING_FORMAL_SUITE |
| C19 | Events | CardResolved, ActionFinished, and Impact facts | 123, 124, 125 | EXISTING_FORMAL_SUITE |
| C20 | Events | Scoped DamageModifier and effect hookup | 126, 130 | EXISTING_FORMAL_SUITE |
| C21 | Cards | Frozen Knife/Shooting semantics migration | 106, 107, 108, 131 | INTEGRATION_SUITE |
| C22 | Shooting | ALL IN, reload, and conservation resource rules | 112, 113, 122 | EXISTING_FORMAL_SUITE |
| C23 | Presentation | Character binding and result-state ownership | 102, 129 | SHARED_MANUAL_HARNESS |
| C24 | Settings | Game settings, deck preference, and display mapping | 133 | INTEGRATION_SUITE |
| H01 | UI | Card interaction, drag, click, and spread motion | 60, 65, 66, 67, 68 | SHARED_MANUAL_HARNESS |
| H02 | UI/Relations | Action relation line query and rendering | 73, 75, 99, 100 | SHARED_MANUAL_HARNESS |
| H03 | UI/Status | Buff grid, inspector preview, and world-follow status | 70, 71, 74 | SHARED_MANUAL_HARNESS |
| H04 | Presentation | Character animation, pose handoff, and hit presentation | 83, 84, 95, 97, 102, 129 | SHARED_MANUAL_HARNESS |
| H05 | Camera | Camera framing, movement, and visual transition | 83, 84, 96, 98 | SHARED_MANUAL_HARNESS |
| H06 | Design Review | Full BattleScene visual composition and UX acceptance | 61, 62, 64, 69, 133 | SHARED_MANUAL_HARNESS |

The six H clusters are manual/design-oriented because they depend on GameObject, Canvas, Camera, animation, frame timing, or human visual review. The map intentionally allows a Mode to contribute to more than one Contract boundary; it is not a second 110-row inventory.

## Disposition Summary

| Disposition | Count |
|---|---:|
| MIGRATE_UNIQUE_COVERAGE | 44 |
| MERGE_THEN_RETIRE | 41 |
| RETIRE_AFTER_EXISTING_COVERAGE | 0 |
| MANUAL_SCENARIO_MERGE | 16 |
| DESIGN_REVIEW_REQUIRED | 9 |
| RETIRE_NO_LONG_TERM_VALUE | 0 |
| **Total** | **110** |

Mode86 is included in `MIGRATE_UNIQUE_COVERAGE` for governance accounting, but its migration is JIT-pending and it remains `ACTIVE_ENUM` until the two unique contracts are carried elsewhere. The former `RETIRE_AFTER_EXISTING_COVERAGE = 1` conclusion is corrected to 0.

## Policy

1. Existing Formal Suites are the preferred regression source.
2. A Legacy Mode may remain temporarily; existence alone does not require Formalization.
3. Before changing a Production system, consult `RegressionTestMap.md` and this Contract Map.
4. Migrate only the relevant Contract when it still has regression value.
5. Do not create one Suite per historical Mode.
6. UI / Camera / Animation / Presentation use on-demand shared harnesses; this document does not create one.
7. `active Legacy Mode = 0` is not a current Demo blocker.

**Phase6 is CLOSED FOR CURRENT DEMO GOVERNANCE.** This means the architecture, inventory, triage, wrapper lifecycle rules, and current-demo boundary are recorded. It does not mean all Legacy Modes are deleted, all Legacy tests are Formalized, or all Manual Harnesses exist. Future work is JIT migration, not a Phase6 bulk cleanup.

## Mode115 Correction

Mode115 is not automatically a Manual UI Harness. Its current observable rules are deck grouping, manifest order, reference identity, legacy fallback, and runtime deck stability. Future carrier selection remains `CardDeckManifestTests` or Cards-domain automated cases when the related Production system changes.

## Result Separator Policy

Future newly added or materially rewritten runner, compatibility runner, or manual harness result output begins after:

```text
========== 以下是测试结果 ==========
```

No existing runner was changed by Phase6E Revised.
