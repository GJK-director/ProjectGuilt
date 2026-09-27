# Suites — Cards — README

Status: CURRENT
Role: TEST LOCATION GUIDE
Last Verified: 2026-09-11

CardDeckManifestTests 拥有 manifest 成员/隔离/顺序、解析与特定数据/traits Cases。它不等于完整 Cards 资源规则覆盖。由 retained wrappers 消费，见 [RegressionTestMap](../../../ProjectDocs/Testing/RegressionTestMap.md)。

CardCombatDiceTests 拥有分离骰子原型的 8 个规则 Case，由现有 Mode82 的 BattleResolutionPlanTests 调用；协议等待由 Mode83 追加 Case 保护，实际动画 / Camera / UI 仍需 BattleScene 人工验收。只增加原型覆盖，没有退休旧测试或迁移全部卡牌。
