# Story System

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

Facade、Flow、节点、变量/历史、View 与内容获取。

## NOT Responsible

Battle 规则、宿主场景跳转决策。

## Main Entry

StorySceneFacade；IntroStoryHost 是 Demo 宿主。

## Data

prologue_501、StoryPanel、宿主参数。

## Runtime Flow

IntroStoryHost → Facade → ContentProvider/Flow → View；结束回调由宿主加载 Battle。

## Invariants

Story 核心不依赖 Battle/卡牌/角色；宿主节点与程序集边界保持；future design 不写成已实现。

## Dependencies

Story asmdef、UGUI、Resources；宿主用 SceneManager。

## Regression Tests

无 Story Formal Suite；Editor validate。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

NewGameText、配置后 StoryTestHost；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

StoryTestHost 无 tracked Scene 绑定；生成音效占位。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
