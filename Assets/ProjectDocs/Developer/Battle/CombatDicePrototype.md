# Combat Dice Split Prototype v0.1

Status: IMPLEMENTED — UNITY GATE NOT VERIFIED; SOL REMOTE GATE PENDING
Baseline: battle/targeting-contract @ 144334acaaf1602834fbe7fac25ab01a85db71ec
Initial prototype pre-existing dirty: NONE. Follow-up collision revision starts from the prototype's existing LOCAL_DIRTY files; all are preserved. User authorized both implementation and the collision revision after discussion.

## Agreed scope / implementation plan

- Player-only 刀枪测试 against one ordinary melee Attack, enabled through an opt-in BattleScene development harness. No formal deck changes.
- One clash session, existing tie rerolls (10-tie limit). Clash die 1–10; damage dice 1–5 melee then 1–5 close-range shooting. Shooting costs no Bullet.
- Point modifiers affect clash only. Each damage roll independently enters existing damage multipliers, DamageModifier and Hit; live changes from the first hit affect the second.
- One ResolutionPlan, two real Impacts, one card use/completion. Each damage roll is captured once by rules immediately before its presentation, with synchronous commit using the same preparation path.
- Independent ClashWin presentation cue: basic attack pose without slash VFX; existing yellow PerfectGuard FX at a fixed weapon-contact position, 0.05s actor Hit Stop, light shake, small visual-only enemy recoil and 0.15s pause. No damage, damage dice roll, Hit, blood or damage number. It does not duplicate the existing ClashWin gameplay event.
- First attack finishes its animation/reaction/camera before second starts. Camera and target position carry forward; second shot has shorter knockback, retained muzzle flash/hit feedback/camera shake. Final cleanup once after both dice.
- Fixed-screen execution HUD plus existing card detail / keyword UI. No new art, no scene/prefab GUID changes, no Story edits, no ALL IN migration, no unrelated cleanup.

## Expected files

Authorized collision follow-up: BattleCombatDicePrototypePresenter, BattleSceneExecutionPresenter, BattleAttackVsGuardPresentationPlayer (read-only profile access), BattlePerfectGuardFxPlayer (fixed-world-position overload/cancellation), BattleCameraDirector (per-playback shake amplitude); extend retained Mode83 invariants if needed. Relevant docs: this record, CodeMap, FeatureGuide, BattlePresentation, ManualHarnesses. No card/rule changes, shared Profile asset edits, art/Scene/Prefab/Story edits, new Mode, stage/commit/push or Unity batchmode. Unity acceptance: collision zero events/roll/damage; world-fixed FX; short recoil into stationary first attack; both real hits; cancel/reload; existing FullBlock unaffected. Standalone compile/static checks only before User Unity Gate.

Existing source: CardTestData, CardDataLoader, BattleCalculator (range selection), BattleResolutionPlan, BattleResolver, BattleExecutionRunner, BattlePresentationProtocol, BattleSceneExecutionPresenter, BattleAttackVsAttackPresentationPlayer, BattleCharacterPresentationController (per-playback knockback scale), BattleCameraDirector (feedback completion property / per-playback shake), BattleAttackVsGuardPresentationPlayer (profile access), BattlePerfectGuardFxPlayer (world contact / cancellation), BattleCardUIPreviewBuilder, BattleFormalPresentationTestHarness, BattleSceneBootstrap, CardsTest.json.

New source: CardCombatDiceData (data/validation), BattleCombatDicePrototypePresenter (sequence adapter), BattleCombatDiceHUD (runtime UGUI), BattleCombatDicePrototypeSetup (isolated encounter), CardCombatDiceTests (Cards Suite); paired new .meta files only.

Tests/callers: existing BattleResolutionPlanTests and BattlePresentationProtocolTests; preserve existing cases and callers. New rules cases use shared factories. Relevant retained regression: Modes 80/82/83/105/107/112/125; no retirement or new mode. Mode82 calls new Cards Suite cases; Mode83 protects presentation gates.

Docs: this progress/contract file, FeatureGuide, CodeMap, Cards, Damage, BattlePresentation, RegressionTestMap, ManualHarnesses; update Testing README if coverage inventory changes.

## Verification / authorization

- Read-only/static checks and standalone C# compile if available; no Unity batchmode.
- Unity Editor Refresh/compile, prototype win/loss/tie, two independent rolls/hits, first-hit lethal, sequential camera and shortened second knockback, cancel/exit cleanup, old melee/shoot/Double Slash/ALL IN remain USER MANUAL GATE.
- User authorized staging, committing and pushing this prototype on 2026-09-27. Only task files may be staged. Unity Gate is NOT RUN; Sol Remote Gate remains separate and this batch is not CLOSED.
- DOC IMPACT: CodeMap YES; FeatureGuide YES; Domain Docs YES; Testing Docs YES.

## Resume log

- 2026-09-27: resumed at the same branch / HEAD. All dirty files belong to this task; no unrelated dirty changes detected.
- Optional dice data, separate roll/impact rules, ClashWin and between-dice tail gates, existing animation/camera adapter, fixed HUD and opt-in encounter are implemented locally.
- Initial standalone Roslyn compile (Unity-bundled compiler and existing Bee references; output only under ignored Temp) succeeded. This is a static compilation, not Unity Editor Refresh or runtime acceptance.
- 8 Cards Suite cases are connected to Mode82; 3 presentation gate cases added to Mode83. Existing cases/callers retained. No new Mode or retired tests.
- Runtime/test sources and Editor sources passed standalone Roslyn compilation; no diagnostics. Output remains under ignored `Temp/CombatDiceCheck`, no Unity process launched.
- Static checks: old 23 JSON card objects unchanged; exactly one new card (24 total); 6 new .meta GUIDs unique; existing art/Scene/Prefab/Profile/.meta/Story untouched; `git diff --check` clean.
- Production/source review and owner documentation updated. Remaining: User Unity Gate and any fixes supported by actual findings. No stage/commit/push; Remote Gate NOT RUN.
- Unity compile/play/visual checks: NOT RUN.
- Collision revision: same branch / HEAD, prior prototype LOCAL_DIRTY preserved. Visual-only Clash Resolution now uses a slash contact callback without slash VFX, world-fixed yellow guard FX, 0.05s actor Hit Stop, light shake, 0.2-unit recoil without red tint, then 0.15s pause. Original damage dice / rule paths unchanged. Mode83 existing cases extended for zero rule events and cancellation before Damage Die 1. Unity verification remains NOT RUN.
- 2026-09-27 delivery review: current branch / HEAD matched the prototype baseline. `BattleScene` had been locally switched to `CombatDiceSplitPrototype`; its serialized `Scenario` is now `None` for the repository. Static review confirmed the independent ClashWin gate and two separately committed Impact paths. User authorized repository upload; Unity Editor compile / Play / visual verification remains NOT RUN.

## 本轮交付报告

以下描述本轮原型的实现范围，不代表 Unity 人工验收或 Sol Remote Gate 已通过。仓库中的测试场景开关保持 `None`，本次不交付 Scene 文件差异。

### 1. 修改文件列表

数据与规则：

- `Assets/Resources/Data/CardsTest.json`
- `Assets/Scripts/Battle/Cards/Data/CardTestData.cs`
- `Assets/Scripts/Data/Loaders/CardDataLoader.cs`
- `Assets/Scripts/Battle/Resolution/BattleCalculator.cs`
- `Assets/Scripts/Battle/Resolution/BattleResolutionPlan.cs`
- `Assets/Scripts/Battle/Resolution/BattleResolver.cs`
- `Assets/Scripts/Battle/Execution/BattleExecutionRunner.cs`

表现、UI 与开发入口：

- `Assets/Scripts/Presentation/Core/BattlePresentationProtocol.cs`
- `Assets/Scripts/Presentation/BattleSceneExecutionPresenter.cs`
- `Assets/Scripts/Presentation/BattleAttackVsAttackPresentationPlayer.cs`
- `Assets/Scripts/Presentation/BattleAttackVsGuardPresentationPlayer.cs`
- `Assets/Scripts/Presentation/BattlePerfectGuardFxPlayer.cs`
- `Assets/Scripts/Presentation/BattleCharacterPresentationController.cs`
- `Assets/Scripts/Camera/BattleCameraDirector.cs`
- `Assets/Scripts/UI/BattleCardUIPreviewBuilder.cs`
- `Assets/Scripts/Debug/BattleFormalPresentationTestHarness.cs`
- `Assets/Scripts/Battle/Bootstrap/BattleSceneBootstrap.cs`

测试与文档：

- `Assets/Tests/Legacy/Core/BattleResolutionPlanTests.cs`
- `Assets/Tests/Legacy/Core/BattlePresentationProtocolTests.cs`
- `Assets/Tests/Suites/Cards/README.md`
- `Assets/ProjectDocs/Developer/FeatureGuide.md`
- `Assets/ProjectDocs/Developer/CodeMap.md`
- `Assets/ProjectDocs/Developer/Battle/Cards.md`
- `Assets/ProjectDocs/Developer/Battle/Damage.md`
- `Assets/ProjectDocs/Developer/Presentation/BattlePresentation.md`
- `Assets/ProjectDocs/Testing/RegressionTestMap.md`
- `Assets/ProjectDocs/Testing/ManualHarnesses.md`
- `Assets/ProjectDocs/Testing/README.md`

### 2. 新增文件列表

下列 6 个文件均附同名 `.meta`（共新增 12 个文件）；没有新增任何美术资产：

- `Assets/Scripts/Battle/Cards/Data/CardCombatDiceData.cs`
- `Assets/Scripts/Debug/BattleCombatDicePrototypeSetup.cs`
- `Assets/Scripts/Presentation/BattleCombatDicePrototypePresenter.cs`
- `Assets/Scripts/UI/BattleCombatDiceHUD.cs`
- `Assets/Tests/Suites/Cards/CardCombatDiceTests.cs`
- `Assets/ProjectDocs/Developer/Battle/CombatDicePrototype.md`（本文）

### 3. 测试卡位置与 ID

`Assets/Resources/Data/CardsTest.json` 最后一个对象；ID `prototype_sword_gun_001`，名称“刀枪测试”。正式 Deck / Character / Enemy / Encounter JSON 均未改动。仅开发 Harness 的 `CombatDiceSplitPrototype` 创建单人测试对局，保留角色原 Buff，临时给玩家此卡、给敌人普通固定 5 点近战攻击。

### 4. ClashDie 存储

可选 `CardTestData.clashDie`，结构为 `ClashDieData { min, max }`；JSON 为 `{ "min": 1, "max": 10 }`。Runtime range capture 与卡面预览读取新字段。原模板 `minPoint/maxPoint = 1/10` 仍保留给兼容读取，实际拼点范围以新字段为准；Strength 等点数修正继续生效。一次是一个 ClashSession，保留平手重掷和 10 次上限。

### 5. DamageDice 存储

可选 `CardTestData.damageDice[]`，每项为 `DamageDieData { min, max, presentation }`。两项均为 1～5，演出分别为 `Melee`、`CloseRangeShoot`。Loader 拒绝半组字段、非法范围以及与旧多段 / 资源 / Trait 配置混用。旧卡无需填任何新字段。

### 6. 如何逐颗结算

胜利后生成一个计划、两个 Impact；建立计划时不投伤害骰、不扣 HP。`ClashWin` 表现结束后，规则层保存第 1 颗原始 Roll；动画命中节点放行它的 Commit。第一击尾段结束才投第 2 颗并播放射击。每段独立读取当时的伤害加减成与 DamageModifier。失败及平手结束不创建测试卡 DamageDice Impact。同步 Resolver 复用同一 preparation / commit 路径。

### 7. 如何触发两次 OnHit

两个 Impact 分别设置 `shouldTriggerHit = true`，各自调用现有 `BattleTiming.Hit`；已提交 Impact 不重复触发。第 1 击改变的状态可影响第 2 击。CardUsed / CardResolved 各一次。首击令 HP 归零时第二击仍命中（实际 HP 损失可能为 0），击杀在整次行动收尾后确认一次。这里 OnHit 对应项目当前 Hit 事件，没有新增另一套 Buff 触发系统。

### 8. Clash Win 与两击演出

独立 `BattlePresentationCue.ClashWin` → 适配器 `PlayClashWin` → 现有基础攻击姿势，关闭刀光。攻击原有接触节点只播放碰撞反馈：复用 `tx-3.png` 格挡火花，捕获世界坐标后固定淡出，不跟随敌人；双方停顿 0.05s，Camera 轻震屏，敌人以现有 Hit 姿势小幅弹开 0.2 世界单位（无染红、无血）。等受力 / 特效 / 镜头尾段结束，短停顿 0.15s，再完成此 cue。整个阶段不 Roll 伤害骰，不触发 HP、Hit、DamageModifier 或伤害数字；不会重复广播规则 ClashWin。

火花位置以双方 PresentationController 位置之间 65%（偏敌人侧）为基准，默认世界偏移 X=0、Y=2.458；这是首版调参起点，需 Unity 看实际武器位置。现有 Guard Profile 的相对位置值不改。轻震屏复用 MeleeGuardReactionProfile，幅度乘 0.5（当前横向 0.0225、纵向 0.0125，持续 0.1s）。普通 Guard 与正式两击仍使用原倍率。碰撞弹开后第一刀仍原地挥击，因此是三段表现、两次真正命中。

两次真正攻击复用现有 AttackVsAttack Player；第一次近战，第二次原地抵近射击并使用已有枪口火焰 / ShootHitFx。第一击完整结束后保留镜头终点和目标位置，第二击从此状态接续既有射击推进 / 冲击 / 震屏。只缩短第二次 burst 和 follow 击退距离（默认 20%）；时长、Hit 反馈和镜头强度保留。最后一次统一释放 Camera；没有 Bullet 资源规则，因此不扣子弹。

### 9. Prototype UI 位置

一级卡面及现有行动槽卡牌详情读取 `BattleCardUIPreviewBuilder` 的双骰说明；蓝色“拼点骰”、橙色“伤害骰”，显示 `1~10` 与 `1~5 → 1~5`。Hover 黄色“骰子说明”进入现有二级面板，包含卡名与上述范围。

执行时在 Scene Presenter 对象上动态添加 `BattleCombatDiceHUD`，子节点 `CombatDiceHUD/DicePanel/DiceText` 为 ScreenSpaceOverlay，固定在屏幕上方中央。显示 `Damage 1 / 2`、`Roll N`、累计 `Hit`；完成或取消隐藏。使用纯色 Image 和已有 TMP 字体，无贴图。Roll 是原始伤害骰，最终 HP 损失可受 Buff / HP Clamp 影响。

### 10. 旧卡兼容

两字段缺省时，数据验证、旧拼点 / 伤害 / 演出路径保留。现有播放器新参数默认保持旧值。23 张旧卡 JSON 对象逐项比较无变化。`BattleScene` 序列化的 Scenario 保持 `None`；Release 忽略开发注入，后续回合 provider 只在成功准备此原型时覆盖。未改 Story、牌组构筑、ALL IN、Buff 架构，未清理无关代码。原型误入单方面攻击或非普通近战对局会返回不支持，不自动套用旧伤害。

### 11. Unity 人工操作与验收

见下节。Unity Gate 归 User；没有运行 Unity batchmode。仓库中的测试场景开关已恢复为 `None`。

### 12. 尚未实际验证的风险

- Unity 导入 / Assets Refresh 后编译、所有回归 Case 执行结果：NOT RUN。独立编译使用现有 Bee 引用，只证明本地 C# 编译一致性。
- 连续两次镜头的力度、第一击终点保持、第二击 20% 距离和画面构图：NOT VERIFIED；需要实际观看调整。
- 现有资源绑定、枪口火焰 / HitFx、黄色碰撞火花实际位置与大小、无刀光动作可辨识度、弹开后第一刀接触感：NOT VERIFIED。
- 中文 TMP 字体、箭头字形、卡面 / 二级面板换行及不同分辨率 HUD 遮挡：NOT VERIFIED。
- 首击致命仍播第二击、取消 / 场景重载清理、后续回合重新安排及旧卡表现回归：NOT VERIFIED。
- v0.1 限定玩家测试卡响应普通近战，防御 / 闪避 / 无人响应 / ALL IN 未扩展。

## Unity 人工操作与验收

1. 退出 Play，执行 Assets → Refresh，等待 Unity 编译，确认 Console 无编译错误或 Missing Script。
2. 打开 `Assets/Scenes/BattleScene.unity`，选中 Hierarchy 的 `BattleSceneBootstrap`，找到已有 `BattleFormalPresentationTestHarness`，设 `Scenario = CombatDiceSplitPrototype`。保持正式初始化入口，不启用 Debug Test Initialization；不需要保存 Scene。
3. Play。首回合测试卡已安排为响应敌方攻击，按 Space 开始执行；若自动拼点关闭，按 Space 放行拼点。保留角色初始 Buff，因此实际拼点可能是 2～11。不要将测试卡安排为自由攻击。
4. 胜利时观察顺序：CLASH RESOLUTION（无刀光基础动作、接触处黄色火花、短 Hit Stop / 轻震屏、敌人小幅弹开、短停顿；HP 不变，不显示伤害数字或伤害骰结果）→ Damage 1 / 2，Roll 1～5，原地近战正常刀光 / 命中 → 完整尾段与保持镜头 → Damage 2 / 2，Roll 1～5，原地射击和短击退 → 整卡收尾。火花应留在碰撞处而非跟着敌人移动。正常累计 Hit 最终为 2；两颗 Roll 可以相等。Bullet 前后不减少。
5. 后续回合用现有安排方式将“刀枪测试”响应到唯一敌方攻击槽，再执行。观察失败不进入双骰、平手仍重掷；可重复进入 Play 观察更多样本。
6. Hover 测试卡 / 槽位详情及黄色“骰子说明”，检查两种骰子标题、颜色与范围；切换窗口尺寸检查 HUD 始终在固定屏幕位置。确认没有新的缺字或描述裁切。
7. 验证首击致命时第二击仍播放、整卡结束才正式击杀；验证中途退出 / 重载后 HUD、镜头及动作不残留。数值边界另由 Mode82 / Mode83 的固定输入用例覆盖。
8. 在 `Assets/Scenes/SampleScene.unity` 的 CardLoadTest 依次运行 Mode82、Mode83，保存新增及旧用例的结果区。按本轮受影响路径再回归 Modes80 / 105 / 107 / 112 / 125。它们未在本轮自动执行，也没有替换已有 Case。
9. 退出 Play，Scenario 改回 None；重新 Play 检查原牌组、普通近战、抵近射击、Double Slash、ALL IN。关闭原型以 None 为准，不以删除组件或修改 Deck 为关闭方法。
10. 切换 `AttackVsGuardFullBlock` 进行旧防御演出验收，确认黄色火花仍按原相对位置播放；共享 Profile / Sprite / Scene 没有被原型调参修改。另在原型碰撞阶段中途退出 / 重载，确认无残留定点火花、角色暂停或 Camera Shake。

可在 Play 的 `BattleSceneExecutionPresenter` 对象上找到动态 `BattleCombatDicePrototypePresenter`（首次胜利时创建），在 `Clash Resolution` 中调整：`Clash Contact Bias`（0 我方、1 敌方）、`Clash Fx Offset`（X 顺攻击方向、Y 高度）、`Clash Hit Stop Duration`、`Clash Recoil Distance`、`Clash Shake Scale`、`Clash Pause Duration`。这些参数不修改旧 Guard Profile；退出 Play 不保存，要保留数值需记录后再写入默认配置。

## DOC IMPACT GATE

```text
CodeMap: YES
Reason: 新增数据 / 规则分支、动态 Presenter / HUD 入口、Harness 与测试 owner；碰撞修订新增 Guard FX / Profile consumer 与单次震屏参数入口；已更新。

FeatureGuide: YES
Reason: 新增可关闭原型与可选双骰配置 / 验证入口；补充 Play 中碰撞位置 / 节奏参数操作；已更新。

Domain Docs: YES
Reason: Cards / Damage / BattlePresentation 的双骰语义、独立命中和等待契约新增；本次更新 BattlePresentation 的无伤害碰撞 / 弹开契约；已更新。

Testing Docs: YES
Reason: 新增 8 个 Cards Case、Mode83 三个等待 Case；本次扩展原 Case 的零事件和碰撞取消检查，补充定点火花及旧 FullBlock 人工验收；已更新。
```

下一步为用户 Unity 验收，发现异常时保留 Console 错误与具体复现步骤，再据此修复。用户已于 2026-09-27 授权本轮提交与推送；Sol Remote Gate 仍需独立核验，本阶段未 CLOSED。
