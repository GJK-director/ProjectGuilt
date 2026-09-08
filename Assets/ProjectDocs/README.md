# Project Guilt Developer Documentation

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## 阅读顺序

开发任何功能前：

1. 阅读 `AGENTS.md`
2. 阅读本 README
3. 找到对应功能文档
4. 查看 `CodeMap`
5. 最后再打开具体代码

## 文档分区

- `Developer`：正式 Runtime 架构、代码职责、数据入口、Scene 入口。
- `Testing`：自动测试、Regression、Harness、Legacy Mode 迁移。
- `Archive`：历史/废弃资料，不作为当前实现 Source of Truth。

## Source of Truth

- 当前 Runtime 实现事实：当前代码 + 正式 Runtime Data。
- 开发导航：`Assets/ProjectDocs`。
- 冻结玩法设计：当前正式设计文档。
- 历史实现：`Archive` / Legacy。
- Legacy Test Mode：只作为历史 Regression 证据，不能自动代表当前 Gameplay 设计。

## 快速导航

- [Architecture](Developer/Architecture.md)
- [CodeMap](Developer/CodeMap.md)
- [RuntimeEntryPoints](Developer/RuntimeEntryPoints.md)
- [DataPipeline](Developer/DataPipeline.md)
- [Testing README](Testing/README.md)
- [RegressionTestMap](Testing/RegressionTestMap.md)
- [LegacyModeMigration](Testing/LegacyModeMigration.md)

本轮只建立导航和事实记录；Production Script、Test Mode、Scene、Prefab 和数据尚未物理迁移。
