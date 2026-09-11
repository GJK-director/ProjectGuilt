# Data Pipeline

Status: CURRENT
Role: RUNTIME DATA SOURCE TO CONSUMER
Last Verified: 2026-09-11

路径由 [CodeMap](CodeMap.md#data-and-asset-locations) 维护；本页拥有数据来源与消费语义。

| Runtime Data Source | Loader | Runtime Consumer |
|---|---|---|
| CardsTest.json | CardDataLoader | Bootstrap、UnitFactory、CardState/Manager、Effects |
| CharacterDefinitions.json | CharacterDefinitionLoader | Bootstrap、UnitFactory |
| EnemyDefinitions.json | EnemyDefinitionLoader | Bootstrap、UnitFactory |
| EncounterDefinitions.json | EncounterDefinitionLoader | Bootstrap.CreateIntentQueueForTurn |
| BuffDefinitions.json | BuffDefinitionLoader（独立文件） | UnitFactory、CardEffectExecutor、CharacterData |
| prologue_501.json | ResourcesStoryContentProvider | StorySceneFacade / Story Flow |

## Deck Source Precedence

GameSettingsState 有已选 deck preference 时优先使用它，否则使用 Scene Inspector preset。
显式 preset 通过 BattleDeckManifest / Bootstrap 提供卡 ID 与初始资源；UnitFactory 显式 cardIDs 路径不等于 CharacterDefinition.startingCardIDs 默认路径，不应修改原 Definition。
修改角色 startingCardIDs 不保证改变选中 preset 的正式牌组；先核实实际初始化 overload。

## Character Visual Selection

prefabKey / portraitKey 当前是 Definition 字段并参与 loader 校验，不是 BattleUnitViewSpawner 的完整自动资源解析系统。
Spawner 当前通过 Scene 序列化的 allyWorldPrefab、enemyWorldPrefab、allyStatusUIPrefab、enemyStatusUIPrefab 选择资源；改 prefabKey 不会自动切换 Prefab。
Character Data、World Prefab、Status UI、表现 Profile 分别承担不同职责。

## Binding and Compatibility

Resources 路径、card/buff/character ID、timing、Scene 名称与 GUID/fileID 都需核验 consumer。
CardsTest/CardTestData 的 Test 命名不表示非正式；兼容字段不按名称直接删除。正式 guilt 通过 RuntimeState 累计，旧角色级值只服务未绑定场景。
数据改动先查 [RegressionTestMap](../Testing/RegressionTestMap.md)，不把历史 Mode 的 fixture 当作设计事实。
