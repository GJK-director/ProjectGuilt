# Project Guilt Documentation

Status: CURRENT
Role: DOCUMENTATION ROOT NAVIGATION
Last Verified: 2026-09-12

## 阅读链

[AGENTS](../../AGENTS.md) → [FeatureGuide](Developer/FeatureGuide.md) → [CodeMap](Developer/CodeMap.md) → 相关领域契约 → [Testing](Testing/README.md)（修改 Production 时）→ 真实 Source / GitHub。

已知 Domain 可直接进入 CodeMap；不要求机械通读全部文档。

## 文档所有权

- FeatureGuide = “What do I edit?”：人类开发者的功能修改入口。
- CodeMap = “How is it connected?”：CANONICAL AI REPO MAP，路径事实唯一 owner。
- [GitCollaboration](Developer/GitCollaboration.md) = 人类开发者的 Git / GitHub Desktop 协作、分支、提交、合并与 Unity 仓库安全指南。
- [Architecture](Developer/Architecture.md)：高层边界、程序集与 deferred debt，不维护第二份路径表。
- [RuntimeEntryPoints](Developer/RuntimeEntryPoints.md)：正式 Scene 流与动态入口。
- [DataPipeline](Developer/DataPipeline.md)：数据来源、加载与消费语义。
- Domain Docs：系统职责、流程和不变量；从 CodeMap 进入。
- Testing docs：覆盖、Case owner、caller、人工入口与写测试规则。

“接入两份开发文档”指每次检查 FeatureGuide/CodeMap 是否受影响，不是每次强制修改。DOC IMPACT GATE 由 AGENTS 定义。

## Source of Truth

- LOCAL WORKING TREE：尚未 push 的本地事实；dirty 结论标记 LOCAL_DIRTY。
- GITHUB MAIN：已 push 的远端事实；Sol Remote Gate 以此为准，不能据此否定明确尚未 push 的本地修改。
- UNITY EDITOR / SERIALIZED ASSET：Scene、Prefab、Inspector、运行行为与人工视觉验收事实；文本扫描不能替代 Unity 验收。
- PROJECT DOCS：导航/契约索引，不能覆盖真实代码、数据或序列化事实。

冲突时 report conflict → inspect source → update stale doc（按授权）；不要让 Runtime 迎合旧文档。

## 状态与历史

- CURRENT：当前开发可依赖的入口、契约或导航。
- DEPRECATED：仍可能有 compatibility consumer，不作为新功能默认入口；DEPRECATED != SAFE TO DELETE。
- HISTORICAL：历史、迁移来源或旧设计，不覆盖当前事实。
- DEFERRED_DEBT 是 CURRENT implementation 的技术债标签，不是 Status，也不等于 Deprecated。

[根 Docs](../../Docs/README.md) 保存历史及明确标记的设计参考，不是默认 Production 导航。[Archive](Archive/README.md) 说明历史资料边界。

## Testing 导航

- [TestArchitecture](Testing/TestArchitecture.md)
- [TestWritingGuide](Testing/TestWritingGuide.md)
- [RegressionTestMap](Testing/RegressionTestMap.md)
- [ManualHarnesses](Testing/ManualHarnesses.md)
- [LegacyModeMigration](Testing/LegacyModeMigration.md)：CURRENT inventory
- [LegacyContractTriage](Testing/LegacyContractTriage.md)：HISTORICAL provenance
