# ProjectGuilt 当前开发状态

## 1. 当前战斗架构

当前主流程：

```text
Definition
→ BattleRuntimeState
→ BattleLifecycleController
→ BattleExecutionPlan
→ BattleExecutionRunner
→ Battle Resolver / Impact Commit
→ IBattleExecutionPresenter
→ BattleSceneExecutionPresenter
→ BattleCharacterPresentationController
```

战斗逻辑与表现逻辑保持分离。Presentation 不计算伤害、胜负或资源消耗；Impact Presentation 完成后，Runner 才允许正式提交对应 Impact。

## 2. 已完成阶段

- Phase 0：动态角色与 Runtime 基础迁移完成。
- Phase 1：正式 Battle Runtime 初始化完成。
- Phase 2：Lifecycle 与 Planning Interaction 完成。
- Phase 3：Execution Runner、Clash Session、Pausable Roll、Resolution Commit 与 Presentation Waiting Protocol 完成。
- Phase 4：正在进行 Battle Presentation。

## 3. 当前 Phase 4 状态

当前已有：

- `BattleCharacterPresentationController`
- `BattlePresentationSandboxController`
- `BattleSceneExecutionPresenter`

Sandbox 已实现并验证的基础表现原型：

- Idle
- Sprint
- Slash
- Hit
- Afterimage
- Slash Effect
- Recoil
- Shake
- HitStop
- Attack vs Attack
- AttackTie 基础表现

正式 Scene Presenter 当前状态：

- 已完成 `CharacterData → BattleUnitViewHandle → PresentationController` 运行时映射。
- 已完成跨帧 Runner Driver。
- 已实现 ActionBegin AttackVsAttack Dual Sprint 结构。
- 正式空间表现参数暂缓验收。

背景、地板、摄像机、角色大小、最终站位与 UI 位置后续仍会调整，因此暂时不继续调节 ClashReady Gap、正式接敌距离、TurnEnd Return，以及 Tie 的世界位置回退与再次接敌。

RollResult、Impact、ActionComplete 当前仍主要立即 completion；正式 Attack 与 Hit 尚未接入。

## 4. 当前 Presentation 设计原则

- `WorldRoot` 表示角色真实世界位置。
- `BodyVisualRoot` 负责局部视觉偏移。
- `CharacterSprite` 负责 Pose 切换。
- UI Anchor 跟随 `WorldRoot`。
- Afterimage 固定在生成瞬间的世界位置。
- 默认 Motion 为 Fixed Duration + EaseOutQuad。
- Repeated Lerp 已淘汰。
- Presentation 必须支持安全 completion 与 cancellation。
- RequestId 必须阻止旧表现完成新请求。

## 5. 未来角色表现方向

以下是设计方向，尚未形成完整系统：

- `CharacterPresentationProfile`
- `BattlePresentationResolver`
- `BattlePresentationSequence`
- `BattlePresentationStep`

角色应拥有自己的 Pose、Effect 与动画参数。普通动作复用通用 Presentation 能力；特殊卡牌可定义独立 Sequence，并复用 Move、Dodge、Shake、Fade、Afterimage、HitStop 等底层能力。特殊演出不得直接修改战斗数值，Impact 时机继续由 Presentation → Runner 协议控制。

## 6. 当前明确 Deferred

以下内容当前不重构：

- 固定 2 Ally + 2 Enemy
- Dynamic 1~4 actor spawning
- `prefabKey` 驱动不同角色 Prefab
- BattleStage、背景与地板数据化
- Camera System
- 最终角色站位
- TurnEnd Return
- 全项目文件夹大重构
- CharacterPresentationProfile
- BattlePresentationSequence 编辑系统

这些方向没有被否定，只是优先级较低，或依赖正式场景空间最终确定。

## 7. 下一阶段原则

下一目标是继续完成角色基础 Presentation 能力，并准备正式 Default Attack / Hit 接入。

复杂动画继续实现前，不应把所有逻辑直接堆入 `BattleSceneExecutionPresenter`。Presenter 主要负责 Cue、Context、Mapping、Dispatch，以及 Completion / Cancel 边界；角色表现由角色 Presentation 层执行。特殊 Sequence 等到真实特殊卡需求出现后再实现，避免提前建立万能动画编辑器。

## 8. 当前重构策略

当前不进行全项目大重构。开发期历史结构允许保留，新代码从现在开始按清晰职责落位。待战斗、Presentation、Camera、Stage 与角色制作流程跑通后，再进行 Project Structure Refactor。
