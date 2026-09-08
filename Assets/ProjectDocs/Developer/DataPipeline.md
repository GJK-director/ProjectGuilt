# Data Pipeline

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

| Data | Loader | Runtime Consumer | Current Observation |
|---|---|---|---|
| `Resources/Data/CardsTest.json` | `CardDataLoader` | `BattleDefinitionBootstrap`、`BattleUnitFactory`、CardLoadTest | 文件名带 Test，但实际是正式 Demo Runtime 数据 |
| `Resources/Data/Characters/CharacterDefinitions.json` | `CharacterDefinitionLoader` | Bootstrap、Factory、Mode103/114/133 | 正式角色定义 |
| `Resources/Data/Enemies/EnemyDefinitions.json` | `EnemyDefinitionLoader` | Bootstrap | 正式敌人定义 |
| `Resources/Data/Encounters/EncounterDefinitions.json` | `EncounterDefinitionLoader` | Bootstrap | Encounter、Intent Pattern、Intent Cycle |
| `Resources/Data/Buffs/BuffDefinitions.json` | `BuffDefinitionLoader`（位于 `CardEffectExecutor.cs`） | Factory、Effect Executor、测试 | 正式 Buff 定义 |
| `Resources/Story/prologue_501.json` | `ResourcesStoryContentProvider` | Story Flow/Facade | 正式剧情入口 |

当前数据数量：Cards 23、Characters 2、Enemies 1、Encounters 1、Buffs 16。

当前主要间接绑定：

- `Resources.Load` 路径字符串。
- card ID、buff ID、timing、prefabKey、portraitKey。
- SceneManager 场景名字符串。
- Unity Scene/Prefab 序列化引用。

`CardTestData` 和 `CardEffectData` 仍包含部分历史/兼容字段。字段是否全部仍被读取，在本批没有逐字段证明，记为 `UNKNOWN`。

本轮不改名、不移动、不修改 JSON 或 Resources 路径。
