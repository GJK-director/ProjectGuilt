# Manual Harnesses

Status: CURRENT
Role: MANUAL AND INTEGRATION ENTRY GUIDE
Last Verified: 2026-09-11

具体源码/资产路径由 [CodeMap](../Developer/CodeMap.md#test-and-support-locations) 维护。以下为操作和验收责任，未声明本次测试已运行。

| Entry | 如何进入 / Scene or Host | 自动还是人工 | 人实际看什么 | 正式 Runtime 主链 |
|---|---|---|---|---|
| CardLoadTest + SampleScene | Inspector 选 Mode，Play 后 Start dispatch | 自动执行选中 caller，人工看日志/部分视觉 | bool/aggregate、异常与对应 UI | 否；Legacy runner host；selected mode may differ in local working tree |
| BattlePresentationSandboxController | Sandbox Scene，Inspector Run Selected Test/Reset；空格启动或放行 Roll | 人工触发、协程执行 | 位移、Tie、Attack/Guard/Dodge、动作衔接 | 否；共用部分正式 Players/Profiles |
| BattleFormalPresentationTestHarness | BattleScene 配置 scenario；Bootstrap 首次 UI 读取前 TryPrepareScenario | 准备输入自动；正式流程与验收人工 | 正式 Runner/Presenter 的可控结果 | 开发输入挂在主链；默认 None，不替代执行器 |
| StoryTestHost | 显式配置宿主并绑定 StorySceneFacade；Start 或触发/关闭剧情 ContextMenu | 自动 Start 可选；人工交互 | 面板打开、推进、关闭 | 否；无 tracked Scene/Prefab 绑定 |
| BattleBuffGroupDebugPreview | 状态 Prefab/Inspector 的应用、清除 Buff 数量预览 | 人工配置/刷新 | 网格、数量、刷新及恢复 | 开发支持；绑定 AllyStatusUI |
| BattleSceneDevelopmentHotkeys | Editor Play：R 重载，F8 入场，F9 回合恢复，F10 双单位焦点 | 人工触发 | Camera/Scene 重建和视觉恢复 | Editor 开发支持；非 Player 输入 |
| WorldFollowProjectionDiagnostic | 需要在目标角色上人工挂载，并通过现有 Bind 绑定 Camera/anchor/renderer | 手工绘制、人工观察 | anchor 与视觉脚底投影偏差 | Runtime 诊断支持，不是 gameplay owner |
| IntroStorySceneSetup | Editor 菜单 Validate Prologue 501；另有 Rebuild | 校验/重建工具，人工确认 | Story Host/Facade/Overlay 接线 | Editor 工具；Rebuild 会修改资源，需任务授权 |

## Combat Dice Split Prototype v0.1

仓库中的 BattleScene 测试 `Scenario` 为 `None`。其 `BattleSceneBootstrap` 对象已有 `BattleFormalPresentationTestHarness`：退出 Play，临时选择 `Scenario = CombatDiceSplitPrototype`，再 Play；首回合已安排测试卡，按 Space 开始（自动拼点关闭时再次 Space 投拼点骰）。后续回合将测试卡响应到唯一敌方普通攻击；退出后改回 `None` 即关闭，不要保存测试开关或给正式 Deck 加卡。

检查胜利阶段零伤害，近战 / 原地射击两次命中，固定 HUD 分别显示 Roll，第一击镜头与受击位置保留，第二击短击退且射击镜头 / 火焰 / 震屏完整；验收失败 / 平手、首击致命、取消 / 重载、二级“骰子说明”和旧卡。完整步骤及风险见 [CombatDicePrototype](../Developer/Battle/CombatDicePrototype.md#unity-人工操作与验收)。本任务尚未执行 Unity Gate；`Mode82` 和 `Mode83` 新 Case 需在 SampleScene 运行，随后按影响回归旧卡 / Buff / 资源流程。

碰撞修订额外检查：基础动作无刀光，黄色格挡火花位于武器接触处且不随敌人后退；0.05s Hit Stop / 轻震屏、0.2 世界单位弹开、0.15s 短停顿后才显示 Damage 1。该阶段不显示血 / 伤害数字，不扣血，不掷伤害骰，不触发 Hit。第一刀原地衔接；取消或退出时定点火花、暂停状态、震屏必须清理。再切换现有 `AttackVsGuardFullBlock` 人工入口，确认原防御特效位置、Hit Stop 与普通击退保持旧行为。Mode83 同时覆盖碰撞阶段零规则事件及碰撞中取消；视觉仍由人工验收。

## Safety and Acceptance

文本/YAML 核验不代替 Editor compile、Missing Script、Play/视觉验证。运行哪种流程由修改影响决定；纯文档不需要启动 Unity。
不要为验证顺手运行资源重建菜单。DO NOT USE UNITY BATCHMODE。新增/改写结果输出遵守 [TestWritingGuide](TestWritingGuide.md)。
