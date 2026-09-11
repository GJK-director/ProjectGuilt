# Project Guilt — AI Execution Protocol

## Roles

- User：提出目标、设计裁决、Unity 人工验收。
- Sol：架构判断、方案冻结、GitHub 远端核验、提供精确任务单并审查结果。
- Luna/Codex：核验本地仓库，按冻结方案执行；不自行扩大 scope 或重构架构。

## Reading Order

[ProjectDocs](Assets/ProjectDocs/README.md) → [FeatureGuide](Assets/ProjectDocs/Developer/FeatureGuide.md) → [CodeMap](Assets/ProjectDocs/Developer/CodeMap.md) → 相关 Domain Document → Testing docs（修改 Production 时）→ 真实 Source / GitHub。

FeatureGuide 定位“改什么”，CodeMap 定位入口、依赖、测试和风险，Domain 文档解释契约。已知 Domain 可直接读 CodeMap 对应行；不机械通读全部资料。根 Docs 不再是默认 Production 阅读入口。

## Source Gate and Source of Truth

开始执行前运行 `git branch --show-current`、`git rev-parse HEAD`、`git status --short`；与任务基线比对，记录 pre-existing dirty。HEAD 不符先报告 SOURCE GATE NOTICE，不自行 pull / checkout / reset。

- LOCAL WORKING TREE：本地尚未 push 的事实；dirty 文件结论标记 LOCAL_DIRTY。
- GITHUB MAIN：已 push 的远端事实，Sol Remote Gate 以此为准；不能否定明确声明尚未 push 的本地修改。
- UNITY EDITOR / SERIALIZED ASSET：Scene、Prefab、Inspector、运行行为及人工视觉验收事实；文本扫描不代替 Unity 验收。
- PROJECT DOCS：导航与契约索引，不覆盖真实代码、数据或 Unity 序列化事实。

冲突处理：report conflict → inspect source → update stale doc（在授权范围内）；不得为了符合旧文档修改 Runtime。

## PLAN CONFLICT

```text
PLAN CONFLICT

Expected:
...

Actual:
...

Evidence:
...

Impact:
...

Action:
STOP CONFLICTING STEP AND REPORT
```

停止冲突步骤；可以继续完全独立的步骤，不自行设计替代架构。

## Git Discipline

默认 NO STAGE / NO COMMIT / NO PUSH，只有用户明确授权才执行。禁止 `git add .`；提交前只 stage expected files。保护 pre-existing dirty，不清理、恢复或混入任务输出。小步修改，不随意重命名，不删除已有测试。

## Unity Asset Safety

已有 asset 移动必须带原 .meta，保留 GUID；不得重新生成已有 .meta。新增 asset 可生成新 .meta，但 GUID 必须唯一。
Scene / Prefab / ScriptableObject 是序列化资产；rename、type、namespace、asmdef 改动须显式风险核验，同时检查 GUID/fileID、consumer、Resources、Editor 与程序集边界。

DO NOT USE UNITY BATCHMODE。除非用户明确撤销此冻结规则，默认验收为 Unity Editor → Assets Refresh → compile → user manual verification。分别报告静态检查、编译和人工验收；未运行不得写 PASS。

## JUST_IN_TIME_TEST_MIGRATION

修改 Production 前：identify Domain → check existing Formal Coverage → check RegressionTestMap → check Legacy inventory if needed → migrate only valuable related contract → modify Production。
必要时才读 Historical LegacyContractTriage 追溯来源。不得为清零 Legacy 迁测试、为一个 Case 创建新 Mode，或无关扩张 Suite。UI / Camera / Animation / Presentation 默认结合 manual/shared harness 与人工 Unity 验收。

## DOC IMPACT GATE

每次完成 Production 修改后必须报告：

```text
DOC IMPACT GATE

CodeMap: YES / NO
Reason:

FeatureGuide: YES / NO
Reason:

Domain Docs: YES / NO
Reason:

Testing Docs: YES / NO
Reason:
```

- CodeMap YES：主要文件/entry、consumer/dependency、动态入口、Scene/Prefab/Profile binding、测试/Harness owner 或影响导航的 deferred debt 改变。
- FeatureGuide YES：“改哪里”、数据/配置入口、验证方式改变，新增可配置 feature，或原开发流程失效。
- Domain Docs YES：职责、数据语义、流程、不变量或边界契约改变。
- Testing Docs YES：Case 所有权、独有覆盖、caller、运行方式或人工验收要求改变。
- YES 时更新对应 owner 文档；若不在授权范围内，报告待更新项，不静默遗漏。
- 普通内部 Bug fix 若入口、数据位置、依赖、开发流程均未变，CodeMap/FeatureGuide 可为 NO；仍检查契约和测试影响。

每次必须检查两份开发导航是否受影响，不强制每次修改两份 Markdown。
