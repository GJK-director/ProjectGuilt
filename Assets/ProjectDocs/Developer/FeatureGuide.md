# Developer Feature Guide

Status: CURRENT
Role: HUMAN DEVELOPER FEATURE GUIDE
Last Verified: 2026-09-11

这是 Project Guilt 面向策划和开发者的任务操作手册。请先按“我想做什么”查找入口；只有需要理解内部代码、排查异常或确认职责边界时，再阅读 [CodeMap](CodeMap.md) 或找 Sol。普通策划不需要先理解 Runtime Owner。

## 最常用入口

| 我想 | 先看哪里 |
|---|---|
| 修改已有卡牌 | [卡牌制作](#1-卡牌制作) |
| 新增卡牌 | [新增卡牌](#4-新增卡牌) |
| 修改 Buff | [Buff 实用操作](#2-buff-实用操作) |
| 增加卡牌效果 | [增加卡牌效果](#17-我要给卡牌增加效果) |
| 修改二级词条 | [二级词条 / Keywords](#3-二级词条--keywords) |
| 修改牌组 | [修改牌组](#9-修改牌组) |
| 修改卡牌视觉 | [卡牌图片 / 卡面视觉](#5-卡牌图片--卡面视觉) |
| 修改镜头 | [Camera](#14-其他非卡牌功能) |
| 调整角色自身目标判定框 | [自身目标判定框](#141-我要调整角色自身目标判定框) |
| 修改敌人行动 | [Enemy Intent](#14-其他非卡牌功能) |
| 修改剧情 | [Story](#14-其他非卡牌功能) |
| 修改设置/UI | [UI / Settings](#14-其他非卡牌功能) |
| 修改后进行验证 | [最快验证方法](#10-最快验证方法) |

## 1. 卡牌制作

### 1.1 我要修改卡牌名称/描述

主文件：`Assets/Resources/Data/CardsTest.json`

修改对应卡牌的 `cardName` 和 `description`。这两项属于一级卡面内容；二级词条请单独修改 `keywords`，不要混在 `description` 中。

### 1.2 我要修改卡牌基础点数

修改 `minPoint` 和 `maxPoint`。它们只是基础范围，不保证等于最终拼点结果。最终结果还可能受到 `AttackPoint`、`ClashPoint`、`CardPoint`、资源规则、卡牌 Trait 和特殊机制影响。

只是修改基础值：可以直接改 JSON。基础值正确但游戏最终点数异常：不要继续乱调 `minPoint/maxPoint`，找 Sol 检查 Modifier 或 Runtime Rule。

### 1.3 我要修改卡牌伤害

修改卡牌的 `damageFormula`。当前正式可用值为：

- `PointAsDamage`
- `DoublePointDamage`
- `PointAsDamage150Percent`
- `PointAsDamage160Percent`
- `PointAsDamage250Percent`

不能自行发明新的公式字符串。现有公式满足需求时可以直接配置；需要新公式时找 Sol。

多段数据还包括 `damageImpactPercents`。它表达 Gameplay 伤害段的倍率；`hpDisplayStageCount` 只负责一个 `BattleImpact` 内的 HP 表现分段，不能当成多段伤害机制。

### 1.4 我要修改冷却

修改 `cooldown`，保存后退出 Play Mode，再按[最快验证方法](#10-最快验证方法)进入 `BattleScene` 验证卡牌可用时间和实际 CD。

### 1.5 我要修改卡牌类型

修改 `cardType`。当前主要类型为：

- `Attack`
- `Defense`
- `Dodge`
- `Ability`

罪卡继续使用 `isSinCard`、`sinCardCategory`、`sinCardUseRule` 等字段定义。不要自行创造新的类型字符串。

### 1.6 我要修改攻击方式 / 演出方式

相关字段为 `attackDeliveryMode`、`presentationVariant`、`usePolicy`。这些字段已有 Loader Validation，只允许使用当前代码支持的值。

如果需求需要一个不存在的新类型或新演出方式，找 Sol。

### 1.7 我要给卡牌增加效果

修改卡牌的 `effects`。当前 EffectType 为：

- `ApplyBuff`
- `ReduceCooldown`
- `EnableAngerMechanic`
- `ActivateModification`
- `ActivateConservation`

不要自行写新的 `effectType` 名字。最常用的是 `ApplyBuff`，需要关注 `trigger`、`target`、`buffType`、`stack`、`duration`、`applyTiming`、`conditions`、`filters`、`formula`。

### 1.8 我要控制效果什么时候触发

当前正式 Timing 包括：

`TurnStart`、`ExecutionStart`、`ActionStart`、`BeforeUse`、
`CardUsed`、`ClashStart`、`Clash`、`ClashWin`、`ClashLose`、
`DamageModifier`、`Hit`、`AfterDamage`、`AfterKill`、
`CardResolved`、`ActionFinished`、`TurnEnd`。

其中 `BeforeUse` 当前仍被正式机制使用，例如
`NextCardPointUp` 的检查时点就是 `BeforeUse`。

`OnPlay` 是与 `BeforeUse` 兼容的旧写法。
现有 JSON 如果已经使用 `OnPlay`，不要为了统一命名擅自批量替换。

遇到 `Resolved` 等历史 / 兼容词汇时，以当前调用点和现有卡牌为准，
不要把它作为新效果的默认推荐 Timing。

如果不知道新效果应放在哪个 Timing，
先查一张现有相似卡；仍不确定就找 Sol。

注意：
不要自行补充更多 Timing。
不要解释 Runtime 内部实现。

### 1.9 我要增加条件

`useConditions` 表示这张卡现在能不能用；`effect.conditions` / `filters` 表示效果到达对应 Timing 后是否执行。

当前 `useCondition`：`HpBelowPercent`、`HpAbovePercent`、`HasBuff`、`BuffStackAtLeast`、`GuiltAtLeast`、`GuiltBelow`。

当前效果 condition：`OpponentCardTypeIs`、`ResourceStackAtLeast`、`ClashResultIs`。

当前 filter：`CardTypeIs`、`CardConsumesResource`、`EligibleShootingAttack`。

多个 condition/filter 当前按 AND 理解，不要设计 OR 语法。

### 1.10 我要用公式决定效果数值

当前 `formula` 不是万能表达式。支持输入 `CurrentAnger`、`ResourceSnapshot`，以及 `multiplier`、`additive`、`lookup`。需求超出这些能力时找 Sol。

## 2. Buff 实用操作

主文件：`Assets/Resources/Data/Buffs/BuffDefinitions.json`

`BuffDefinitions` 定义 Buff 本身。卡牌要实际给予 Buff，通常仍需要在 `CardsTest.json` 的 `effects` 中配置 `ApplyBuff`。

| 我想获得 | 复用 ID | 每层作用 |
|---|---|---|
| 下一次拼点 +X | `NextClashPointUp` | `ClashPoint +1` |
| 当前攻击卡点数 +X | `Strength` | `AttackPoint +1` |
| 下一张点数卡 +X | `NextCardPointUp` | `CardPoint +1` |
| 造成伤害提高 | `DamageUp` | `DamageDealt +10%` |
| 造成伤害降低 | `DamageDown` | `DamageDealt -10%` |
| 受到伤害提高 | `Vulnerable` | `DamageTaken +10%` |
| 受到伤害降低 | `DamageReduction` | `DamageTaken -10%` |
| 子弹 | `Bullet` | 由射击资源规则读取 |
| 怒 | `Anger` | 由 Anger 规则读取 |
| 改装 | `Modification` | 由 Modification 规则读取 |
| 节约 | `Conservation` | 由 Conservation 规则读取 |

`Strength`、`NextClashPointUp`、`NextCardPointUp`、`DamageUp`、`DamageDown`、`Vulnerable`、`DamageReduction` 属于已有通用 Modifier 路径，可以通过已有 JSON Effect 复用。

`Bullet`、`Anger`、`Modification`、`Conservation` 虽然存在于 `BuffDefinitions`，但拥有额外正式代码规则，不能把它们理解成复制一个 Resource Buff 就会自动得到新资源功能。

完全新增资源不能纯 JSON 得到完整正式资源语义，找 Sol。

`NextClashPointUp` 本身是可复用通用 Buff；但当前 `dodge_001` 的 `GrantNextClashPointUpOnSuccessfulDodge` 属于特殊 Trait / Pending 行为。如果只是给予下一次拼点 `+X`，优先参考通用 Buff；如果要求“只有闪避成功后才给予”，参考现有 Trait，并在不确定时找 Sol。

## 3. 二级词条 / Keywords

`CardTestData.keywords` 是本卡局部关键词说明。修改某一张卡自己的二级词条时，修改 `CardsTest.json` 对应卡牌的 `keywords`。

项目还存在全局关键词说明，部分全局词条描述由程序侧维护，不是全部都能从 `CardsTest.json` 改。本卡局部关键词可以直接改 JSON；需要修改多张卡共享的全局关键词定义时，不要只改某张卡，找 Sol 确认当前来源。

## 4. 新增卡牌

1. 在 `Assets/Resources/Data/CardsTest.json` 增加完整 `CardTestData`。
2. 使用唯一 `cardID`。
3. 配置名称、描述、类型、基础点数、CD 等。
4. 按需要配置 `effects`、`traits`、`resourceRule`。
5. 如果要进入正式玩家牌组，把 `cardID` 加到对应 `BattleDeckManifest`。
6. 退出 Play Mode。
7. 重新进入 `BattleScene`。
8. 确认卡牌出现、显示正确、可用性正确、效果正确。

只写 `CardsTest.json` 不会自动让卡进入正式当前 Deck。

## 5. 卡牌图片 / 卡面视觉

当前项目没有 `cardID → 独立卡牌插画` 的配置机制。`CardsTest.json` 没有 `image`、`icon`、`sprite` 或 `artwork` 字段。

当前 `BattleCardUI` 使用共享卡框，由 `rarity` / `isSinCard` 选择：`White`、`Blue`、`Purple`、`Gold`、`Sin`。新增普通卡当前不需要放一张独立卡图。

想修改某个品质的共享卡框，属于 Prefab / Visual Style 资产修改，不是单张卡 JSON 配置。想让某张卡拥有独立插画，当前系统不支持策划纯配置，找 Sol。不要自行新增 `artwork` 字段。

## 6. Trait

当前 `BattleCardTrait`：

`FirstStrike`、`DoubleClashAgainstDefense`、`HeavyAnger`、`IaiAnger`、`GrantNextClashPointUpOnSuccessfulDodge`、`GrantBulletOnSuccessfulDodge`、`ReloadBulletOnDodgeResolution`、`AllInBulletDump`。

traits 只能使用代码已有枚举值。想复用完全相同的正式机制，可以参考现有卡；效果类似但触发条件不同，不要随便套 Trait。需要新 Trait 时找 Sol。

## 7. 子弹 / 射击卡

`Bullet` 是 BuffStack Resource。`resourceRule` / `resourceRules` 负责卡牌资源规则，常见字段包括：

`resourceType`、`resourceID`、`requiredStackForNormalVersion`、`fallbackMinPoint`、`fallbackMaxPoint`、`pointPerStack`、`exactStackForBonus`、`exactStackPointBonus`、`consumeAmountOnSuccess`、`consumeAllCapturedOnSuccess`、`insufficientBehavior`、`consumeTiming`。

修改已有射击卡的 Bullet 规则可以参考现有射击卡。不要自行创造新的 `resourceType/resourceID` 并假设系统会获得完整新资源功能。`ALL IN` 使用 `AllInBulletDump` Trait，并有代码级专用规则。

## 8. 罪卡

罪卡相关字段为 `isSinCard`、`consumeOnUse`、`maxUseCount`、`sinCardUseRule`、`sinCardCategory`、`guiltGain`。当前 category 为 `Clash`、`Ability`；当前 use rule 为 `UseCount`、`Permanent`。

`guiltGain` 当前有正式使用链路，表示增加负罪感。虽然 `guiltCost` 字段存在，但当前没有足够证据把它写成已确认的正式支付机制；不要依赖 `guiltCost` 设计正式消耗，需要该机制时找 Sol。

## 9. 修改牌组

正式玩家 Deck 当前来自：`Assets/Scripts/Battle/Cards/Decks/BattleDeckManifest.cs`。

当前牌组为 `Knife` 和 `Shooting`。新增卡牌不等于自动进入 Deck；把已经存在的卡加入现有牌组时修改 manifest。新牌组、动态组牌或新的 Deck 结构找 Sol。

Manifest 引用不存在的 `cardID` 会使正式 Bootstrap 初始化失败。

## 10. 最快验证方法

普通策划验证的主入口是 `Assets/Scenes/BattleScene.unity`，不要把 `SampleScene` 当成默认卡牌策划验证入口。

正常工作流：

`退出 Play Mode → 修改 JSON → 保存 → 重新进入 BattleScene → 选择需要的 Knife / Shooting Deck → 验证`

Card runtime 会在 Bootstrap 时重新创建；`BuffDefinitionLoader` 有静态缓存。因此不要依赖正在运行的战斗自动热更新。

分别观察：

- 改基础点数：看卡面范围和实际拼点。
- 改 Effect：触发对应条件，观察 Buff、资源和伤害。
- 改 Buff：观察层数、点数/伤害修正和持续时间。
- 改 keywords：查看卡牌 Tooltip / 二级说明。
- 新增卡：确认进入 manifest 后出现在正式手牌。

## 11. 测试规则

如果只是正常策划修改，先完成 `BattleScene` 人工验证。如果修改后既有 Regression Test 失败，不要为了让测试变绿而自行修改测试。

特别是设计数值、牌组成员或正式机制被有意修改时，测试预期可能也需要更新。由 Sol 判断是代码回归、旧测试预期，还是应该更新测试。

不要把 `FirstStrikeExecutionTests`、`EnemyIntentTests` 等全部称为 Cards Suite。

## 12. 什么时候我可以直接改

可以直接改：

- 已有卡名称/描述
- 基础 `min/max`
- `cooldown`
- 已支持的 `damageFormula`
- 已存在的 `EffectType`
- 已存在 Buff 的 `stack/duration`
- 本卡 `keywords`
- 已有资源规则的数值
- 设计已经明确要调整现有 `Knife` / `Shooting` 牌组成员时，可以把已经存在的 `cardID` 加入对应现有 Deck Manifest；如果因此出现既有 Regression Test 失败，不自行修改测试，交给 Sol 判断
- 只有当新需求的触发条件、结算时机和正式语义与现有 Trait 完全一致时，才直接复用已有 Trait；只要有语义差异，就找 Sol

建议先参考现有卡再改：

- 复杂 timing
- `conditions`
- `filters`
- `formula`
- Bullet 规则
- Sin Card
- multi-hit

必须找 Sol：

- 新 EffectType
- 新 Trait
- 新 CardType
- 新 Timing
- 新 Formula 类型
- 全新资源
- 新 Deck 架构
- 单卡独立 Artwork 系统
- 需要修改 Runtime C# 才能实现的效果
- 基础 JSON 正确但 Runtime 结果异常
- 想删除或修改 Formal Regression Test

## 13. 现有卡作为模板

| 想做什么 | 参考卡 |
|---|---|
| 普通刀攻击 | `atk_001 / 顺斩` |
| 防御 | `def_001` |
| 闪避 | `dodge_001` |
| 高点刀卡 / 怒联动 | `knife_heavy_001 / 重劈` |
| 怒终结 | `sin_iai_001 / 一闪` |
| 普通射击 | `atk_bullet_001 / 盲射` |
| 抵近射击 | `shoot_close_001` |
| ALL IN | `shoot_all_in_001` |
| 装填 / 闪避类 | `shoot_reload_001 / shoot_disengage_001` |
| 改装 | `ability_modification_001` |
| 节约 | `sin_conservation_001` |

实际字段和机制以当前 `CardsTest.json` 为准，不要根据记忆捏造。

## 14. 其他非卡牌功能

以下内容只提供人类入口：

| 我要改什么 | 先去哪里 | 常规怎么验 | 什么情况找 Sol |
|---|---|---|---|
| Camera | Camera 参数、对应 Profile 或 `BattleScene` | `BattleScene` / Sandbox 观察构图、跟随、震动 | 需要改 Camera 归属或运动规则 |
| Enemy Intent | `Assets/Resources/Data/Encounters/EncounterDefinitions.json`、`Assets/Resources/Data/Enemies/EnemyDefinitions.json` | `BattleScene` 观察槽位、目标和逐回合意图 | 规则与数据不一致、需要新目标/Intent 类型 |
| Story | Story 数据、`NewGameText`、配置后的 StoryTestHost | 从菜单进入剧情并推进 | 新剧情系统或缺少正式入口 |
| UI | 对应 UI View / Host / Prefab | `BattleScene`、Preview 或对应辅助 Scene | 新交互、跨战斗流程或缺少绑定 |
| Settings | `Menu`、`GameSettingsState`、对应序列化字段 | Menu → NewGameText → BattleScene → Menu | 新设置持久化或影响 Bootstrap |

### 14.1 我要调整角色自身目标判定框

位置：`BattleCharacterStatusWorldFollower` 的 `Center Offset`。它负责调整 `SelfActionDropZone` 相对于角色的默认位置；不要通过运行时 `SelfActionDropZone` RectTransform 的 Pos X / Pos Y 配置正式默认位置。

大小：调整 `SelfActionDropZone` 的 RectTransform `Width / Height`，用于改变判定范围。保持基础 Scale 为 `1,1,1`，不要用 Scale 代替正式范围调节。

可视调试：临时提高 `SelfActionDropZone → Image` 的 Alpha，在 `BattleScene` 中观察判定范围；调整完成后将 Alpha 调回透明。`Raycast Target` 必须保持开启。

运行时位置由 `Center World Anchor → Camera → Canvas` 投影持续刷新；Prefab 编辑状态下 World Camera、Target Canvas 和 World Anchors 显示为 None 可以是正常的，不要手动绑定运行时 Scene 对象。

内部路径、调用者和依赖统一查 [CodeMap](CodeMap.md)，不要在本手册重复架构说明。

## DOC IMPACT GATE

CodeMap: NO
Reason: 当前 Runtime 路径和架构没有变化。

FeatureGuide: YES
Reason: 本任务就是重写人类操作手册。

Domain Docs: NO
Reason: 没有修改 Domain 规则。

Testing Docs: NO
Reason: 没有改变测试架构或治理规则。
