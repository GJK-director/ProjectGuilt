# Developer Feature Guide

Status: CURRENT
Role: HUMAN DEVELOPER FEATURE GUIDE
Last Verified: 2026-09-16

这是 Project Guilt 面向策划和开发者的任务操作手册。请先按“我想做什么”查找入口；只有需要理解内部代码、排查异常或确认职责边界时，再阅读 [CodeMap](CodeMap.md) 或找 Sol。普通策划不需要先理解 Runtime Owner。

## 最常用入口

| 我想 | 先看哪里 |
|---|---|
| 修改已有卡牌 | [卡牌制作](#1-卡牌制作) |
| 新增卡牌 | [新增卡牌](#4-新增卡牌) |
| 修改 Buff | [Buff 实用操作](#2-buff-实用操作) |
| 修改 Buff 三级面板 | [Buff 三级面板](#21-我要修改-buff-三级面板) |
| 调整角色脚底 Buff 图标 Hover 判定范围 | [Buff 图标 Hover 判定范围](#22-我要调整角色脚底-buff-图标-hover-判定范围) |
| 增加卡牌效果 | [增加卡牌效果](#17-我要给卡牌增加效果) |
| 修改二级词条 | [二级词条 / Keywords](#3-二级词条--keywords) |
| 调整卡牌黄色词条 Hover | [卡牌黄色词条 Hover 与二级面板](#31-我要调整卡牌黄色词条-hover-判定和二级面板弹出速度) |
| 修改牌组 | [修改牌组](#9-修改牌组) |
| 修改卡牌视觉 | [卡牌图片 / 卡面视觉](#5-卡牌图片--卡面视觉) |
| 修改镜头 | [Camera](#14-其他非卡牌功能) |
| 调整角色自身目标判定框 | [自身目标判定框](#141-我要调整角色自身目标判定框) |
| 修改敌人行动 | [Enemy Intent](#14-其他非卡牌功能) |
| 修改剧情 | [Story](#14-其他非卡牌功能) |
| 修改设置/UI | [UI / Settings](#14-其他非卡牌功能) |
| 开启/调整自动拼点 | [自动拼点](#142-我要开启或调整自动拼点) |
| 调整行动顺序数字视觉 | [行动顺序数字视觉](#143-我要调整行动顺序数字) |
| 验证行动槽卡牌详情 Hover | [行动槽卡牌详情 Hover](#144-我要验证行动槽卡牌详情-hover) |
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

更详细的两阶段伤害、Cumulative 累计分段、Impact interval、`resolvedDamage` / `actualDamage` 与 Damage Number 说明见 [Damage Contract](Battle/Damage.md)。

策划操作上的边界：

- 需要两个独立伤害段时，配置 `damageDistributionMode`、`damageImpactPercents` 和 `damageImpactDelaySeconds`；每段会分别提交 DamageModifier、HP 和 Damage Number。
- 需要累计卡牌时使用 `Cumulative`；例如 Base 7 与 `[100, 180, 230]` 形成真实段 `7、5、4`。
- 需要调整数字大小、颜色、寿命时，检查 `BattleScene` 上的 `BattleDamageNumberPresenter` Inspector 字段；当前正式值和生命周期限制见 Damage 文档。
- HP 变为 0 但尚未正式结束时，按 `HP Depleted` 与 `Defeated` 区分，不要用 `IsDead()` 代替终局判断。
- 特殊卡视觉 Damage Marker 目前是 `RESERVED / NOT IMPLEMENTED IN v0.1`，不是现有卡的配置入口。

### 1.4 我要修改冷却

修改 `cooldown`，保存后退出 Play Mode，再按[最快验证方法](#10-最快验证方法)进入 `BattleScene` 验证卡牌可用时间和实际 CD。

当前卡牌冷却表现会读取 Runtime 的 `BattleCardState.currentCooldown`。
剩余 CD 大于 0 时显示冷却遮罩和当前剩余数字，回到 0 后隐藏。
如果只是修改冷却显示样式，不要修改 `cooldown` Gameplay 数据，
请看“卡牌图片 / 卡面视觉”中的 CD 显示说明。

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

不要自行写新的 `effectType` 名字。最常用的是 `ApplyBuff`，需要关注 `trigger`、`target`、`buffID`、`stackDelta`、可选的 `intensityDelta`、`applyTiming`、`delayTurns`、`applyTimes`、`intervalTurns`、`conditions`、`filters`、`formula`。

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

| 我想获得 | 复用 ID | 当前读取语义 |
|---|---|---|
| 下一次拼点 +X | `NextClashPointUp` | `intensity` 作为一次 ClashPoint 加成，`stack` 是剩余触发次数 |
| 当前攻击卡点数 +X | `Strength` | 只要 `stack > 0`，读取 `intensity` 加到 AttackPoint |
| 下一张点数卡 +X | `NextCardPointUp` | `intensity` 作为一次 CardPoint 加成，`stack` 是剩余触发次数 |
| 造成伤害提高 | `DamageUp` | 读取 `intensity` 作为 DamageDealt Modifier |
| 造成伤害降低 | `DamageDown` | 读取 `intensity` 作为 DamageDealt Modifier |
| 受到伤害提高 | `Vulnerable` | 读取 `intensity` 作为 DamageTaken Modifier |
| 受到伤害降低 | `DamageReduction` | 读取 `intensity` 作为 DamageTaken Modifier |
| 子弹 | `Bullet` | 由射击资源规则读取 |
| 怒 | `Anger` | 由 Anger 规则读取 |
| 改装 | `Modification` | 由 Modification 规则读取 |
| 节约 | `Conservation` | 由 Conservation 规则读取 |

`Strength`、`NextClashPointUp`、`NextCardPointUp`、`DamageUp`、`DamageDown`、`Vulnerable`、`DamageReduction` 属于已有通用 Modifier 路径，可以通过已有 JSON Effect 复用。

`Bullet`、`Anger`、`Modification`、`Conservation` 虽然存在于 `BuffDefinitions`，但拥有额外正式代码规则，不能把它们理解成复制一个 Resource Buff 就会自动得到新资源功能。

完全新增资源不能纯 JSON 得到完整正式资源语义，找 Sol。

### Anger 零层状态与验证

Anger Definition 的配置入口是 `Assets/Resources/Data/Buffs/BuffDefinitions.json`。愤怒卡 `sin_anger_001` 使用 `EnableAngerMechanic`：成功使用后启用 Anger mechanic，并确保 Anger canonical state 存在；首次建立时是 0 层，不直接增加 1 层。运行时 owner 是 `CharacterData` 的 `Anger` state，Iai 的清空 owner 是 `BattleKnifeCardRules.FinalizeCompletedInteraction`，会把层数归零但保留 retained-zero state。

验证时运行现有 Anger / Buff regression，确认愤怒卡成功后机制已启用、`Anger` state 非空且 stack 为 0；再确认后续增加和消费回到 0 时一级 Buff 图标仍显示数字 0。不要通过 UI 或新增 bool 伪造 Anger state。

Buff 的正式 Runtime state 是 `CharacterData` 中按 `buffID` 唯一保存的 `BuffData`，字段为 `stack` 与 `intensity`。重复普通 `ApplyBuff` 只增加 `stack`；需要改变每次效果强度时，才在 Effect 中显式填写 `intensityDelta`。`stack` 与 `intensity` 是两个独立语义，不能把它们默认相乘。

定义中的 `maxStacks` 与 `maxIntensity`（大于 0 时）负责上限；`retainWhenZero` 决定消费到 0 后是否保留 Runtime state，`showWhenZero` 只决定一级 UI 是否显示 0 层。`consumeRule` 决定正式事件边界上的消费规则；它们不是卡牌描述字段，也不应通过 UI 伪造。

`NextClashPointUp` 本身是可复用通用 Buff；但当前 `dodge_001` 的 `GrantNextClashPointUpOnSuccessfulDodge` 属于特殊 Trait / Pending 行为。如果只是给予下一次拼点 `+X`，优先参考通用 Buff；如果要求“只有闪避成功后才给予”，参考现有 Trait，并在不确定时找 Sol。

### 2.1 我要修改 Buff 三级面板

Buff 的一级图标由 `BattleBuffGroupUIView` 根据角色当前 canonical Active Buff state 交给 `BattleBuffIconUIView`。Hover 后的二级面板内容和 Buff 三级明细由 `BattleSecondaryInfoPanelHost` 应用；不要在 UI 层伪造 Buff 层数或生命周期。

正式视觉入口是 `Assets/Prefabs/Battle/Units/UI/BattleSecondaryInfoPanel.prefab`。当前序列化接线字段包括 `buffDetailRoot`、`bubbleContainer`、`bubbleTemplate`、`stackLabelText`、`stackValueText`、`durationLabelText` 和 `durationValueText`。当前 canonical bubble 由每个 `buffID` state 生成一条 summary details，使用 `stack`，在定义需要时附带 `intensity`；不显示 per-instance source / duration。需要调整背景、字体、颜色、字号、间距或 Padding 时，先改这个 Prefab；需要改变 state、Pending mutation、消费或保留语义时，找 Sol 检查 `CharacterData` / `BattleBuffGroupUIView` / `BattleSecondaryInfoPanelHost`。

当前修改后的最短验收路径是：让目标角色获得对应 Buff，在 `BattleScene` Hover 角色 Buff 图标，确认正文、一级 canonical state 层数，以及 `BuffDetailRoot` 中对应 summary Bubble 的层数与必要的强度；再检查普通卡牌黄色词条仍走原有二级面板路径。

### 2.2 我要调整角色脚底 Buff 图标 Hover 判定范围

正式入口是 Ally 与 Enemy 状态 UI Prefab 中的：

`FootStatusGroup` → `BuffGroup` → `BuffTemplate` → `HoverHitbox`

`HoverHitbox` 是透明 Image 命中层。需要统一调整所有角色脚底 Buff 图标的 Hover 范围时，在两个正式 Prefab 的 `HoverHitbox` RectTransform 中修改 `Anchored Position X/Y`、`Width` 和 `Height`；运行时 Buff 图标由 `BattleBuffGroupUIView` 从该模板实例化，因此不要按 `buffID` 单独配置，也不要放大 `IconImage` 或 `StackText` 来代替判定框，也不要恢复已经退休的 `DecayText`。

Ally 与 Enemy 的模板参数应保持一致。视觉 Graphic 的 `Raycast Target` 保持关闭，只有 `HoverHitbox` 参与命中；`BattleBuffIconUIView` 继续作为 `BuffTemplate` 根节点上的 Pointer owner，`BattleSecondaryInfoPanelHost` 不负责计算 Hover 区域。修改后在 `BattleScene` 分别检查 Ally 与 Enemy 的 Buff Hover，以及普通卡牌黄色词条仍使用原有二级面板路径。

## 3. 二级词条 / Keywords

`CardTestData.keywords` 是本卡局部关键词说明。修改某一张卡自己的二级词条时，修改 `CardsTest.json` 对应卡牌的 `keywords`。

项目还存在全局关键词说明，部分全局词条描述由程序侧维护，不是全部都能从 `CardsTest.json` 改。本卡局部关键词可以直接改 JSON；需要修改多张卡共享的全局关键词定义时，不要只改某张卡，找 Sol 确认当前来源。

### 3.1 我要调整卡牌黄色词条 Hover 判定和二级面板弹出速度

卡牌黄色词条 Hover 的正式配置入口：

`Assets/Art/battle/kapai/BattleCardUI.prefab` → `BattleCardUIView` →

- `Keyword Hover Padding X`：扩大关键词左右方向的判定范围。
- `Keyword Hover Padding Y`：扩大关键词上下方向的判定范围。

这两个参数只扩展黄色关键词附近的命中区域，不会把整张卡变成关键词 Hover 区，也不会新增透明 UI 覆盖层。代码默认值为 `keywordHoverPaddingX = 0`、`keywordHoverPaddingY = 0`；当前正式 Prefab 保存值为 `Padding X = 35`、`Padding Y = 15`。

二级面板打开时间的正式配置入口：

`Assets/Prefabs/Battle/Units/UI/BattleSecondaryInfoPanel.prefab` → `BattleSecondaryInfoPanelHost` → `Hover Open Delay`。

`0` 表示不增加人为等待；`0.1` 表示 Hover 后约等待 `0.1` 秒；`1` 表示 Hover 后约等待 `1` 秒。代码默认值为 `hoverOpenDelay = 0.45`；当前正式 Prefab 保存值为 `Hover Open Delay = 0.1`。实际关闭宽限仍由现有 `DefaultCloseGrace = 0.12` 控制，不是本入口的 Inspector 参数。

卡牌黄色词条 Hover、二级面板 Delay 已由用户在 Unity 中人工验收：`USER UNITY MANUAL VERIFIED`。

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

### 5.1 CD 冷却显示

当前资产：`Assets/Art/battle/kapai/BattleCardUI.prefab`

当前运行 View：`Assets/Scripts/UI/BattleCardUIView.cs`

Prefab 层级：

```text
VisualRoot
└── CooldownOverlay
    └── CooldownValueText
```

规则：

- `currentCooldown <= 0`：遮罩和 CD 数字隐藏。
- `currentCooldown > 0`：遮罩显示，`CooldownValueText` 显示当前 `currentCooldown`。

视觉参数全部在 Prefab 中调整：

- `CooldownOverlay`：Image Color / Alpha；当前默认黑色、Alpha `0.5`。
- `CooldownValueText`：TextMeshProUGUI → Font Size；RectTransform → Pos X / Pos Y；TextMeshProUGUI → Color / Vertex Color。

两个 UI Graphic 都必须保持 `Raycast Target = false`。
这些只是视觉参数，不拥有卡牌是否可用的规则。卡牌能否使用仍由现有 CardState / CardManager / CanSelect 链路决定。
不要让普通开发者为了改字号或颜色去修改 C#。

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

当前行动槽的卡牌显示时，`CardModeSwitchButton` 同时出现；卡牌区域关闭时，按钮同时隐藏。按钮只作为当前手牌存在时的普通卡 / 罪卡切换入口，`ToggleCardGroup()` 仍保留 Runtime guard。

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

每次进入新的 `Prepare` 后，正式角色 UI 完成绑定时会自动把当前速度最快且仍可用的友方正式 Slot1 作为默认来源，并以战斗位置作为同速时的次级顺序。玩家可以直接改选其他角色/槽位或清空选择；普通刷新不会把选择抢回。默认来源使用现有 Planning 手牌路径，因此需要验证时观察对应角色手牌与 `CardModeSwitchButton` 是否按原规则显示。

分别观察：

- 改基础点数：看卡面范围和实际拼点。
- 改 Effect：触发对应条件，观察 Buff、资源和伤害。
- 改 Buff：观察层数、强度、点数/伤害修正，以及触发消费和归零保留行为。
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
- 已存在 Buff 的 `stackDelta` 与可选 `intensityDelta`；生命周期规则改动需同时核对 Buff definition 的 `retainWhenZero` / `showWhenZero` / `consumeRule`
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

### 14.2 我要开启或调整自动拼点

路径：`BattleScene` → `BattleSceneBootstrap` GameObject → `BattleAutoClashController`。在 Inspector 中查看或调整该组件；如果是在尚未接入它的场景中使用，才需要手动添加组件。

配置：

- `Enable Auto Clash`：关闭时保留手动拼点；开启时，执行阶段自动拼点。
- `Auto Clash Delay`：进入正式 Roll Gate 后的等待时间，最小为 `0`。常用值为 `0`、`0.1`、`1`。

新建组件时的代码默认值是 `Enable Auto Clash = false`、`Auto Clash Delay = 0.1`；当前正式 `BattleScene` 的序列化配置是 `Enable Auto Clash = true`、`Auto Clash Delay = 0`。以 Scene Inspector 中的序列化值为准。

Planning 阶段的 Space 永远保留，仍由玩家按下以开始正式执行。Auto ON 时，执行阶段原本用于手动 Roll 的 Space 不再触发拼点。

自动模式是切换 Runner 的 `RollMode`，由现有 Auto Roll Gate 自动推进，不是模拟自动按空格。Auto Clash 覆盖正式 Runner 原本负责的 Roll Gate 范围，包括 Clash、unilateral roll、Defense 和 Dodge。

### 14.3 我要调整行动顺序数字

正式位置：`Assets/Prefabs/Battle/Units/UI/AllyStatusUI.prefab` 与 `Assets/Prefabs/Battle/Units/UI/EnemyStatusUI.prefab`。在对应的 `Slot_01` 或 `Slot_02` 下找到 `OrderText` / `Order Text` TMP 子物体，直接调整 RectTransform、字体、字号、颜色和对齐方式。

四个 OrderText 的 `Raycast Target` 必须保持关闭，避免数字显示挡住卡牌或目标区域的点击。不要交换 `Slot_01` / `Slot_02` 的位置或引用，也不要断开 `BattleActionSlotUIView.orderText`。

如果以后需要给数字增加背景、图标、FirstStrike 边框或动画，只调整数字显示本身；不要为此改动卡牌安排、行动关系线或执行顺序。

### 14.4 我要验证行动槽卡牌详情 Hover

Hover 已安排的 Ally 或 Enemy 行动槽时，先确认当前卡牌详情正常显示；如果该行动槽存在正式最终 response relation，应同时看到另一侧原本的卡牌详情。单方面行动只显示当前卡牌。

如果同一 Enemy Intent 后来由另一个合法 responder 替换，Hover 应只显示最终 responder 的详情，不应根据 `requestedEnemyIntent` 显示历史 Slot。Enemy 侧已有 Locked 详情时，paired Hover 不得覆盖 Locked 内容；面板位置和现有 Ally / Enemy 布局不应改变。

最短人工验收路径是：在 `BattleScene` 安排一组响应行动，分别 Hover Ally responder、Enemy Intent、无响应行动槽和替换前的旧 Slot，观察双侧详情、最终 responder、Hover Exit 清理以及 Locked 内容是否保持。

## DOC IMPACT GATE

CodeMap: YES
Reason: Action Slot Card Info Host 新增 paired display state，并消费 BattleActionSlotManager 的最终 response read-only query。

FeatureGuide: YES
Reason: 本任务就是重写人类操作手册。

Domain Docs: NO
Reason: 没有修改 Domain 规则。

Testing Docs: YES
Reason: Mode105 retained runner 新增 Paired Action Slot Hover A-D coverage；本轮未修改测试治理结构。
