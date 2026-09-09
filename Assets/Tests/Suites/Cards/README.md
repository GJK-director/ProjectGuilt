# Cards Suite

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`610fba0ca460945658a3fa17cd1472d2f5fceb75`)

目标范围：CardState、UsePolicy、资源、Deck、Keyword 和卡牌效果相关测试。

Batch 5B 已建立 `CardDeckManifestTests` Formal Suite。Batch 5D 将其从 4 个 Case 扩展到 6 个，新增 `DeckManifestsResolveAllCardsWithoutMissingEntries` 与 `DeckManifestOrdersNormalCardsBeforeSpecialCards`，承接 Mode114 的 Cards-domain responsibilities。Mode109 standalone 已在 Phase6B retired，`BattleDeckManifestTests` wrapper 仍供 Mode114 使用；Mode89 standalone 已 retired，其 compatibility wrapper 的 FirstStrike Execution 测试逻辑由 `FirstStrikeExecutionTests` Formal Suite 提供。没有为 Manifest 创建 Bootstrap duplicate test。
