# Regression Test Map

Status: CURRENT
Role: CURRENT REGRESSION MAP
Last Verified: 2026-09-11

路径事实见 [CodeMap](../Developer/CodeMap.md#test-and-support-locations)。Legacy 字样描述载体，不自动判定行为过时；下表未列出的回归从 [Legacy inventory](LegacyModeMigration.md) 查询，不能推断没有保护。

## Domain Coverage and Callers

| Domain | Formal Coverage | Legacy Unique Coverage | Current Caller | JIT Migration Note |
|---|---|---|---|---|
| EnemyIntent/Bootstrap/Resolution | EnemyIntentTests：5 Case | Mode103 其余 ownership、provider/自动回合、伤害/表现集成 | CardLoadTest Mode103 → FullBattleIntegrationRegressionTests；Test4 部分、Test5 全部委托 | 保留集成边界，不把部分委托写成全迁移 |
| Cards/Execution FirstStrike | FirstStrikeExecutionTests：13 Case | Mode86 JSON traits missing/null/empty compatibility；LongRangeShoot non-implication | CardLoadTest Mode86；Formal execution 经 retained wrapper 链 | priority/order/pairing 有重叠，unique coverage 仍在 Legacy |
| Cards/Decks | CardDeckManifestTests：6 Case | Mode115 grouping、reference identity、fallback、runtime deck stability | Mode115 → BattleDeckHandGroupingTests → BattleDeckBootstrapPresetTests | Grouping 不是自动归类为手工 UI；按规则价值迁 |
| Bootstrap preset | DeckPresetBootstrapTests：11 Case | 其他正式初始化/provider 与 Settings 集成 | retained Mode114 wrapper；Mode103/133 | 不为每个旧 Mode 建新 Suite |
| Knife/Resolution | 当前无该域完整 Formal owner | Mode107 Anger/Knife/Iai/Double Slash/Heavy/Breath/staged HP 组合 | CardLoadTest Mode107 → BattleAngerAndKnifeCardsBasicTests | 修改对应规则再拆相关契约 |
| Shooting/Ability/Buffs | 当前无该域完整 Formal owner | Mode113 Conservation、0 Bullet、CD、Ability 及依赖链 | CardLoadTest Mode113 → BattleConservationAbilityTests | 不误把已有 Cards manifest Case 当资源规则覆盖 |
| UI/Keywords | 无 UI Formal C# owner | Mode132 timing/tooltip/description 格式 | CardLoadTest Mode132 → BattleCardKeywordPresentationTests | 文字契约与视觉验收分开 |
| Settings | 无 Settings Formal C# owner | Mode133 preference/display/Bootstrap 集成 | CardLoadTest Mode133 → BattleGameSettingsIntegrationTests | 结合 Menu 人工流程 |
| Lifecycle/Turn/Events | 无对应 Formal C# owner | lifecycle/terminal/CardUsed/Resolved/ActionFinished/Impact | Legacy/Core 及 CardLoadTest 内嵌测试；按 inventory 找 caller | 先查事件顺序与资源提交合约 |
| Resolution/Interaction | 无完整 Formal owner；FirstStrike 只覆盖其专属契约 | Generic Attack/Guard/Dodge、Clash/Context/Plan | Legacy/Core；CardLoadTest dispatch | 按实际 interaction 选择回归 |
| Presentation/UI/Camera | 无 Formal C# owner | Protocol/Engagement/Binding/Pausable、关系 UI | Legacy/Core、现有人工入口 | 自动合约不能代替视觉验收 |
| Story | 无 Formal C# owner | 无已登记 Formal regression；宿主/Editor 校验为当前入口 | StoryTestHost、NewGameText、IntroStorySceneSetup | 先区分 Story 核心与宿主 |

## Retained Wrapper Chain

Mode115 ACTIVE → BattleDeckHandGroupingTests → BattleDeckBootstrapPresetTests（retained Mode114 wrapper）
→ BattleDeckManifestTests（retained Mode109 wrapper）
→ BattleExecutionPlanFirstStrikePolicyTests（retained Mode89 wrapper）
→ FirstStrikeExecutionTests。

Mode114 wrapper 同时直接调用 Cards/Bootstrap Formal Cases；Mode109 wrapper 调用 Cards Cases。
89/109/114 均无 active standalone enum/dispatch，但 wrapper 仍被消费。Formal ownership != standalone retirement != wrapper deletion。

## Running and Provenance

SampleScene 运行选中的 Legacy caller；Inspector 选择可能为 LOCAL_DIRTY。Formal Case 不自动出现在 Unity Test Runner。
人工入口见 [ManualHarnesses](ManualHarnesses.md)。历史 clustering 仅在必要时查看 [LegacyContractTriage](LegacyContractTriage.md)，其中建议不代表现有 Suite。
