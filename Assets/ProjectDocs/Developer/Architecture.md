# Architecture

Status: CURRENT
Role: HIGH-LEVEL ARCHITECTURE
Last Verified: 2026-09-11

## System Boundaries

Battle 拥有状态、卡牌、Buff、行动安排、意图、交互、执行、结算、事件与回合。Data 读取定义；Bootstrap 组装 Runtime。UI 负责输入/显示，Presentation 负责动作及完成协议，Camera 参与视觉编排。Story 核心独立，StoryDemo 宿主决定剧情后的场景流；Settings 提供用户偏好。

正式关系：Menu → StoryDemo/Story → Battle Bootstrap → RuntimeState → UI/Lifecycle → Execution → Resolver/Events + Presentation → terminal UI → Menu。
表现完成回调不能代替或重复提交战斗规则。具体入口见 [RuntimeEntryPoints](RuntimeEntryPoints.md)，唯一路径地图见 [CodeMap](CodeMap.md)。

## Assemblies

Story 核心为 ProjectGuilt.Story，显式引用 Newtonsoft.Json.dll；Story UGUI 为 ProjectGuilt.Story.UGUI，引用 Story 与 UnityEngine.UI，两者 autoReferenced。
Battle、UI、Presentation、StoryDemo、Settings 与现有 Tests 在默认 Assembly-CSharp；Editor 工具受 Editor 目录边界约束。目前没有独立 Battle/Test asmdef。物理归类不等于程序集隔离。

## Source of Truth

遵守 [文档根](../README.md) 的 LOCAL WORKING TREE / GITHUB MAIN / UNITY EDITOR / PROJECT DOCS 区分。代码、Runtime Data 和 Unity 事实优先于旧导航；旧测试不自动代表当前 Gameplay Design。

## Physical / Logical Boundaries

物理目录不要求与每个逻辑职责一一对应：Core 总控制器、Buff 域 UI、Camera、Bootstrap 意图生成均有跨域关系。由 CodeMap 解释，不为目录整齐迁移代码。

## Deferred Debt

CURRENT + DEFERRED_DEBT：BattleSimpleUIController、BattleSceneExecutionPresenter、BattleResolver、BattleActionSlotManager、BattleExecutionPlanExecutor、BattleSceneBootstrap、BattleCameraDirector。
这些实现仍服务 Demo；不能因职责多或文件大标为 Deprecated。只有具体 feature / regression / maintainability 需要才讨论拆分，不为了架构洁癖阻碍 Demo。

兼容调用、旧 timing、ForTesting seams 按 JIT 核验，不以清零为目标。Scene/Prefab-bound support 保留真实引用；高风险变化先按 AGENTS 检查边界。
