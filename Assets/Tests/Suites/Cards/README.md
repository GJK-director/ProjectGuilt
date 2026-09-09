# Cards Suite

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`1fab2f48f34395a5a4fb639a70da02c75b67c704`)

目标范围：CardState、UsePolicy、资源、Deck、Keyword 和卡牌效果相关测试。

Batch 5B 已建立 `CardDeckManifestTests` Formal Suite。Batch 5D 将其从 4 个 Case 扩展到 6 个，新增 `DeckManifestsResolveAllCardsWithoutMissingEntries` 与 `DeckManifestOrdersNormalCardsBeforeSpecialCards`，承接 Mode114 的 Cards-domain responsibilities。Mode109 仍作为 Legacy compatibility consumer；Mode89 仍保留 Legacy compatibility runner，其 FirstStrike Execution 测试逻辑由 `FirstStrikeExecutionTests` Formal Suite 提供。没有为 Manifest 创建 Bootstrap duplicate test。
