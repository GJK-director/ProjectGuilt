# Regression Test Map

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Important Legacy Modes

| Mode | Current Coverage | Future Domains | Status |
|---|---|---|---|
| 103 | Full Battle Integration；Enemy Intent cycle、target slot、当前 Definition/ownership 引用、legacy fallback、250% damage 相关回归 | EnemyIntent、Bootstrap、Resolution | LEGACY_ACTIVE |
| 107 | Anger、Knife、Iai、Double Slash、Heavy、Breath、staged HP 等组合回归 | Cards/Knife、Resolution/MultiImpact | LEGACY_ACTIVE |
| 113 | Conservation、0 Bullet、Cooldown、能力回归链，并调用部分 Shooting/Ability 回归 | Cards/Shooting、Buffs | LEGACY_ACTIVE |
| 132 | Card Keyword Presentation、Timing Vocabulary、Tooltip/Description formatting | UI/Cards、Cards/Keywords | LEGACY_ACTIVE |
| 133 | Settings、Deck preference、Display mapping、Bootstrap preset | Settings、Bootstrap、Cards/Decks | LEGACY_ACTIVE |

以上状态只描述当前 Legacy Mode 事实，不表示未来迁移已经完成，也不输出 KEEP/DELETE/MERGE 决策。

其余 Mode 的当前索引见 `LegacyModeMigration.md`。
