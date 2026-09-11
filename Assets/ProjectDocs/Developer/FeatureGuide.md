# Developer Feature Guide

Status: CURRENT
Role: HUMAN DEVELOPER FEATURE GUIDE
Last Verified: 2026-09-11

先用本表定位任务，再读 [CodeMap](CodeMap.md) 对应 Domain 的真实路径、consumer、测试与风险；系统规则由对应 Domain 文档说明。路径事实统一由 CodeMap 维护。

| Feature | Change Here First | Data / Config | Runtime Owner | Regression Tests | Manual Verification | Important Notes |
|---|---|---|---|---|---|---|
| 卡牌值/效果 | Cards / Card Effects | CardsTest.json、effects | CardDataLoader → BattleCardState/Manager、CardEffectExecutor | Cards Suite；资源/效果 Legacy | SampleScene、BattleScene | Test 命名不表示非正式数据 |
| 牌组 | Cards 的 BattleDeckManifest | explicit preset、菜单 preference | Bootstrap → UnitFactory | Cards/Bootstrap/Execution Suite；Mode115 caller 链 | 选择牌组后进入战斗 | explicit preset 与 startingCardIDs 不是完全同一来源 |
| 角色数据 | Units / Data | CharacterDefinitions、EnemyDefinitions | Loader → UnitFactory → CharacterData | DefaultCard Legacy、Bootstrap Suite | HP、速度、牌组与状态 | 数据和角色外观分开修改 |
| 角色外观 | UI / Units 的 Spawner、对应 Prefab | Scene 的 World/Status Prefab 引用 | BattleUnitViewSpawner | Binding Legacy | 角色、锚点、状态跟随 | prefabKey/portraitKey 不是完整自动资源解析系统 |
| Buff | Buffs / Card Effects | BuffDefinitions、timing、stack/duration | CharacterData、EventProcessor、TurnProcessor | Buff/资源 Legacy | Buff Preview、BattleScene | Buffs Suite 目录目前是 placeholder |
| 敌人行动 | Bootstrap 的意图生成入口 | EncounterDefinitions、EnemyDefinitions | BattleDefinitionBootstrap.CreateIntentQueueForTurn | EnemyIntent Suite、Mode103 | 逐回合目标/槽位 | 不只查看 EnemyIntent 文件夹 |
| 行动安排/目标 | Actions / Planning、对应 UI Router | 槽位、eligibility、目标 | BattleActionSlotManager | assignment/interaction Legacy | 点击、拖拽、取消/替换 | 不直接在 View 写结算规则 |
| 拼点/伤害 | Resolution / Clash | 卡牌公式、Buff、快照 | BattleResolver、BattleCalculator、BattleClashSession | Clash/Resolution/Generic Legacy | 正式 Harness | 不改表现来补偿规则错误 |
| 回合/终局 | Lifecycle / Turn | RuntimeState | LifecycleController、TurnProcessor | Lifecycle/Timing/EndLock Legacy | 回合切换、终局返回 | 表现完成与规则阶段需分别验证 |
| 负罪感 | Guilt / Battle State | guiltGain | GuiltManager、RuntimeState | 罪卡/资源 Legacy | Guilt UI | 正式共享 guilt；未绑定 runtime 才用角色兼容值 |
| 镜头 | Camera | Camera 参数、表现 Profile | BattleCameraDirector、GrayboxCameraController | 相关 Legacy＋人工 | 热键、Sandbox、BattleScene | Director 有动态入口 |
| 战斗表现 | Presentation 的对应 Player/Router | Profile、角色表现 Prefab | BattleSceneExecutionPresenter | Protocol/Engagement/Binding Legacy | 正式 Harness、Sandbox | completion 不可提前替代规则完成 |
| 剧情 | Story / StoryDemo | prologue_501、StoryPanel、宿主参数 | IntroStoryHost → StorySceneFacade | Editor validate；无 Story Formal Suite | NewGameText、配置后的 StoryTestHost | future design 不等于已实现 |
| UI | 对应 View / Host | Prefab、Scene、生成器 | 局部 View/Host | UI/关键词/关系线 Legacy | BattleScene、Preview | 只有跨 Battle flow 才进入 BattleSimpleUIController |
| 菜单/设置 | Settings / MainMenu | PlayerPrefs、Menu 字段 | MainMenuController、GameSettingsState | Mode133 | Menu → NewGameText → BattleScene → Menu | 同时核对默认值与序列化值 |

## 修改前的三个确认

1. [DataPipeline](DataPipeline.md)：CardsTest.json → CardDataLoader → Card Runtime/Effects；显式 deck preset 可提供独立于角色 startingCardIDs 的卡 ID，修改角色默认牌不保证改变选中的 preset。
2. Character Data 不等于 Character Visual Prefab。当前 Spawner 使用序列化的 ally/enemy World/Status Prefab；改 prefabKey 不会自动切换它。
3. [RegressionTestMap](../Testing/RegressionTestMap.md) → [Legacy inventory](../Testing/LegacyModeMigration.md) → 必要时历史 triage。Formal Cases 是 static bool ownership，运行还需实际 caller；人工步骤见 [ManualHarnesses](../Testing/ManualHarnesses.md)。

修改后执行 AGENTS 的 DOC IMPACT GATE。不要因大文件或未迁完 Legacy 自行扩大任务。
