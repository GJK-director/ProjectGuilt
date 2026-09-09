# Cards Suite

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`e9043550a20fe3f227b3c3da5c521a43e7991f13`)

目标范围：CardState、UsePolicy、资源、Deck、Keyword 和卡牌效果相关测试。

Batch 5B 已建立 `CardDeckManifestTests` Formal Suite，当前包含四个公开 Case：刀流卡牌定义值、两套 Deck Manifest 成员与隔离、可用卡牌解析，以及 Shooting FirstStrike Trait。Mode109 仍作为 Legacy compatibility consumer；Mode89 的 Execution FirstStrike policy regression 保持在 Legacy。
