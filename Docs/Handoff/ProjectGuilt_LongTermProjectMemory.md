# Project Guilt — 完整长期项目记忆

Status: CURRENT  
Role: CANONICAL PROJECT HANDOFF SNAPSHOT  
Document Version: 1.0  
Last Verified: 2026-09-11

---

# 0. 这份文档是什么

本文件用于恢复 Project Guilt 的长期项目上下文。

目标是：

> 一个没有参与过此前对话的新 Sol，在阅读本文件、当前 ProjectDocs 与 GitHub 后，能够理解 Project Guilt 是什么、目前做到哪里、哪些设计和工程结论已经冻结、哪些问题仍待处理，以及下一步应该做什么。

本文件不是：

- 源码的替代品；
- CodeMap 的替代品；
- FeatureGuide 的替代品；
- Git changelog；
- 每次 commit 的流水账；
- 所有卡牌当前数值的永久记录。

对于具体代码路径、字段、Runtime 行为、测试状态和当前实现，应重新检查当前 GitHub、ProjectDocs 与源码。

如果本文件与当前正式仓库产生冲突：

> 以当前正式仓库事实和 CURRENT ProjectDocs 为准。

---

# 1. 项目定位

项目名称：

**Project Guilt**

曾使用中文方向：

**罪人计划**

项目当前是长期独立游戏开发项目。

作品核心不是单纯“打怪”“七宗罪”或“猎奇世界观”，而是：

> **让玩家和主角一起逐渐堕落。**

核心主题从早期的“逃避负罪感”进一步扩展为：

> **人如何逃避自己不愿面对的负面情绪，以及这种逃避最终付出的代价。**

负面情绪包括但不限于：

- 恐惧；
- 恶心；
- 焦虑；
- 负罪；
- 暴怒；
- 苦痛；
- 欲望；
- 嫉妒；
- 贪婪等。

Project Guilt 的剧情、能力系统、怪物化、罪人契约和战斗机制都应最终服务于这个核心，而不是为了设定复杂而复杂。

项目文风偏好：

- 文字简单；
- 强表现力；
- 不追求华丽辞藻；
- 不用谜语式叙事掩盖信息；
- 世界观服务于角色与主题；
- 角色和事件推动剧情。

参考作品可以提供“味道”和方法，但目标不是制作某部作品的换皮。

---

# 2. 当前 Demo 的定位

当前 Demo 的主要目标不是完整展示整个世界观，而是证明几个核心能力：

1. 战斗系统是否有成立的玩法基础；
2. 刀流和射击流是否有明显不同的资源与轮转体验；
3. 战斗表现、拼点、卡牌和镜头是否具有可继续发展的视觉与交互基础；
4. 剧情序章是否能够建立主角、工作和世界氛围；
5. 整个项目是否已经具备可以继续扩展而不是不断推倒重来的工程基础。

当前 Demo 战斗主要围绕：

- 1v1；
- 双方各两个行动槽；
- 刀流；
- 射击流；
- 拼点；
- 防御；
- 闪避；
- 卡牌 CD；
- Buff / Resource；
- 罪卡；
- 负罪感；
- 战斗表现与镜头。

Demo 不是完整教程。

设计上可以允许新玩家第一次体验有一定失败概率，重点是让玩家逐渐理解轮转、资源和提前斩杀。

---

# 3. 剧情核心与世界观

## 3.1 罪人与负面情绪

罪人的能力和负面情绪互相绑定。

基本逻辑：

> 负面情绪越强，能力越强。

但代价是：

> 使用能力本身又会进一步放大负面情绪。

罪人本身也会主动向契约者施加、诱导或放大相应负面。

负面情绪超过个人承受阈值后，存在怪物化风险。

每个人的最大承受值不同。

社会针对负面情绪通常不是“彻底消灭”，而是：

- 对冲；
- 转移；
- 压制；
- 利用。

例如：

- 暴怒可能要求保持冷静；
- 苦痛可以用于医疗；
- 不同罪拥有不同社会用途和风险。

---

## 3.2 罪人契约

罪人契约不是罪人可以随意反悔的赏赐。

它是一套：

> **罪人自身也必须遵守的异常规则。**

第二角色属于极少见的异常适配者。

特点：

- 罪人无法从她身上有效获得负面情绪；
- 但契约规则仍要求罪人提供能力。

这是她异常适配性的核心之一。

---

## 3.3 “随时退出”制度

主角所在体系不是传统“被强迫到死”的组织结构。

工作采用：

- 正式合同；
- 任务接取；
- 不强制每次任务；
- 明示可以退出。

退出后：

- 现有伤势不会消失；
- 已经形成的负面情绪不会消失；
- 失去后续收入；
- 失去组织福利；
- 失去治疗、装备、补贴等资源。

继续工作则：

- 可以获得更多报酬；
- 接更多任务可以获得更多奖励；
- 有战后治疗；
- 有装备和工作资源。

因此：

> “可以退出”本身不是自由，而是一个带有黑色幽默的现实选择。

耳机前辈属于已经退出战斗岗位、转做情报工作的样本。

---

# 4. 主角与长期剧情方向

## 4.1 主角

主角为男性。

主要武器：

- 武士刀；
- 左轮。

外观方向：

- 西装；
- 大衣。

气质偏克制、可靠，但不是冷血。

序章不应让主角过早表现成张扬或过度嘴贫的人。

他会害怕、恶心、焦虑。

只是长期底层生活和社会压力，使他具备更强的忍耐与行动能力。

长期心理逻辑：

> “我这么做是为了妹妹。”

最初这句话是主角逃避自己行为责任的重要借口。

但剧情不能把主角写成“直到最后才知道自己杀的是人”。

正确方向是：

> 他会越来越清楚目标接近人类、同事也可能怪物化、自己的行为确实在伤害别人，但仍然继续。

因此堕落来自：

**明知仍做。**

---

## 4.2 妹妹

妹妹年龄方向约 15～18 岁。

性格：

- 早熟；
- 不刻意叛逆；
- 大致知道哥哥的钱从哪里来；
- 不会简单以“不要为了我这样做”阻止哥哥。

家庭改善是长期不可逆的。

阶段性经济目标可以包括：

1. 温饱；
2. 更好的住所；
3. 教育；
4. 妹妹生病后出现更紧迫的金钱需求。

妹妹是主角继续工作的核心现实动机，但不应成为主角所有行为的道德免责工具。

---

## 4.3 第二角色

第二角色为女性。

核心气质：

- 无口；
- 无感；
- 情绪变化极慢。

武器：

- 长匕首。

当前核心能力来自“怠惰”方向。

主要视觉 / 战斗概念：

- 黑色油状流体；
- 天眼领域；
- 杀意感知；
- 远程控制匕首；
- 黑油回收武器。

天眼领域核心概念：

> 领域中速度存在固定参照，高速目标被“盯住”后受到限制。

她属于异常适配者。

长期人物弧线不是突然变成人情味十足，而是：

> 到结尾时才刚刚有一点真正的人情味。

终局方向：

主角最终主动请求第二角色杀死自己。

第二角色在确认主角妹妹相关信息后执行。

之后：

> 第二角色替主角继续养活妹妹。

---

## 4.4 甜妹同事

甜妹属于：

- 社交能力强；
- 讨好型人格；
- 相对容易让玩家产生亲近感的同事。

她在中期发生怪物化。

主角自身的行为会成为事件的重要诱因之一。

她的作用不是单纯制造悲剧，而是让：

> “怪物”“同事”“人”之间的边界真正开始崩塌。

---

## 4.5 心理医生

心理医生贯穿故事。

心理体检会明确告诉契约者：

- 能力与负面情绪之间的关系；
- 风险；
- 怪物化可能性。

因此项目不应依赖：

> “组织一直骗主角，主角最后才知道真相”

作为主要反转。

---

# 5. 当前世界中的“罪”

当前罪已经不再局限于传统七宗罪，而是更广义的负面情绪集合。

现阶段曾明确包含：

- 暴食；
- 暴怒；
- 傲慢；
- 丑陋；
- 嫉妒；
- 贪婪；
- 苦痛；
- 欺诈；
- 死亡；
- 怠惰；
- 虚伪；
- 欲望；
- 焦虑；
- 色欲。

这些名称和具体社会映射仍可以随着剧情继续调整。

部分当前方向：

- 苦痛 → 医疗；
- 暴怒 → 治安 / 暴力执行；
- 欲望 → 高度保密；
- 欺诈 / 死亡 → 高保密或缺乏公开记录。

“罪”既是世界观概念，也是能力、社会生产力和角色心理的结合体。

---

# 6. 战斗系统总体结构

Project Guilt 当前战斗为回合制卡牌战斗。

视觉方向：

- 左右对战；
- 2.5D 舞台感；
- 透视 Camera。

长期队伍规模方向：

- 我方最多 4；
- 常态预计 2～3；
- 敌方最多 4。

Demo 当前主要验证 1v1。

双方拥有行动槽。

Demo 中：

- 玩家两个行动槽；
- 敌人两个行动槽。

先攻如果成为一次正式行动，同样占用行动槽。

---

## 6.1 敌人行动

敌方行动在敌方回合 / 规划阶段生成。

玩家可以看到敌方目标与行动意图。

UI 可以通过指向线等方式表现：

> 敌人准备攻击谁。

玩家随后通过自己的行动：

- 响应；
- 拦截；
- 防御；
- 闪避；
- 改变冲突关系。

---

## 6.2 卡牌类型

主要卡牌方向包括：

- Attack；
- Defense；
- Dodge；
- Ability；
- Sin Card。

卡牌拥有：

- 基础点数；
- CD；
- 效果；
- Buff；
- Resource Rule；
- Trait；
- 负罪感变化；
- 使用条件；
- 表现相关配置。

当前系统不是传统随机抽牌 Roguelike。

卡牌更接近：

> 已装备 / 已配置的战斗技能集合。

---

# 7. 刀流设计

刀流核心体验：

> **持续进攻 → 建立怒 → 承担风险 → 用高收益技能兑现。**

怒层主要通过主动造成伤害建立。

设计方向：

- 越持续攻击，怒越高；
- 高怒带来更高战斗收益；
- 同时提高风险；
- 玩家需要判断何时继续积累，何时兑现。

当前刀流的重要机制方向包括：

- 基础攻击；
- 刺；
- 双斩；
- 重劈；
- 防御；
- 调整呼吸；
- 怒；
- 一闪。

部分具体卡牌数值属于高频可变内容，不应由本文件长期固化。

当前正式数据应查：

`Assets/Resources/Data/CardsTest.json`

---

## 7.1 怒

怒属于 Resource / BuffStack 类机制，但拥有额外正式规则。

核心设计方向：

- 造成伤害可以叠怒；
- 被防御等未实际造成伤害的情况不一定叠加；
- 高怒同时提供收益和风险；
- 怒存在阶段性强化；
- 一闪等能力可以消耗怒进行兑现。

Anger 不是通过复制普通 Resource Buff 就能获得完整语义的新资源模板。

具体上限、触发和数值应以当前正式代码及 FeatureGuide 为准。

---

## 7.2 一闪

一闪是怒的主要终结 / 兑现方式之一。

核心原则：

- 高风险；
- 高收益；
- 更强调拼点胜利；
- 拼点失败具有明显代价；
- 使用后清空怒。

重要结论：

> 一闪进行本次拼点时，仍按照使用前的怒层享受对应阶段强化。

怒不是在拼点前提前清空，而是在该次正式结算后处理。

---

# 8. 射击流设计

射击流核心体验：

> **子弹是一种逐渐减少的资源，玩家围绕剩余弹量安排收益和轮转。**

与刀流形成对比：

- 刀流：越打怒越多；
- 射击：越打子弹越少。

主要卡牌方向：

- 抵近射击；
- 点射；
- ALL IN；
- 盲射；
- 抽身；
- 装弹；
- 改造；
- 节约。

---

## 8.1 Bullet

Bullet 是当前正式 BuffStack Resource。

基础弹仓容量当前存在正式上限规则。

Modification 会改变弹仓规则并进行 Clamp。

这些不是单纯 Buff JSON 就能复制的新资源逻辑。

涉及：

- 消耗子弹；
- 剩余子弹；
- 全弹消耗；
- 子弹不足时替代行为；
- 特定卡牌击杀后的补弹；

均应以正式 Resource Rule 与特殊代码逻辑为准。

---

## 8.2 ALL IN

ALL IN 的核心设计：

> 消耗当前全部子弹，进行高额多段输出。

子弹越多，总收益增加，但后续每颗子弹的边际收益递减。

ALL IN 当前属于带有专用 Trait / Runtime Rule 的特殊卡牌。

不要把它当成任意普通 Resource Rule 就能完全复制的通用模板。

---

## 8.3 节约

节约属于射击流的重要罪卡 / 资源博弈机制。

核心设计目标：

> 诱导玩家在低子弹状态下获得更高收益，同时承担保留资源的代价。

节约与 ALL IN 应形成决策关系：

- 保留少量子弹追求高收益；
- 或主动全部投入寻求击杀；
- 击杀后再进入新的资源循环。

具体当前数值与触发应查 FeatureGuide、CardsTest.json 与正式代码。

---

# 9. 卡牌、Buff、Timing 与配置原则

面向人类制作人员的正式操作入口：

`Assets/ProjectDocs/Developer/FeatureGuide.md`

面向 Sol / AI 的实现定位入口：

`Assets/ProjectDocs/Developer/CodeMap.md`

长期原则：

> FeatureGuide 回答“怎么改”。

> CodeMap 回答“内部在哪里、怎么连”。

本文件不复制完整配置 API。

---

## 9.1 卡牌基础值与最终值

`minPoint` / `maxPoint` 是卡牌基础点数。

最终拼点结果可能进一步受到：

- AttackPoint；
- ClashPoint；
- CardPoint；
- Buff；
- Resource；
- Trait；
- 特殊 Runtime Rule；

影响。

因此：

> 基础值正确但最终点数异常时，不应继续盲调 min/max。

---

## 9.2 Buff

BuffDefinitions 主数据位于：

`Assets/Resources/Data/Buffs/BuffDefinitions.json`

当前存在：

- 通用 Modifier Buff；
- Resource Buff；
- 特殊代码规则 Buff。

例如：

- Strength；
- NextClashPointUp；
- NextCardPointUp；
- DamageUp；
- DamageDown；
- Vulnerable；
- DamageReduction；

属于较通用的 Modifier 路径。

而：

- Bullet；
- Anger；
- Modification；
- Conservation；

虽然存在 Buff Definition，但还拥有额外正式代码语义。

---

## 9.3 Timing

Timing 是卡牌和 Buff 设计的重要接口。

当前系统存在：

- 回合级；
- 行动级；
- 卡牌使用级；
- 拼点级；
- Damage / Hit；
- Kill；
- Resolve；
- Finish；

等不同阶段。

`BeforeUse` 当前属于正式使用中的 Timing。

`OnPlay` 属于与 `BeforeUse` 兼容的旧写法。

具体当前正式 Timing 列表和使用建议：

> 以 FeatureGuide 和当前代码为准。

不要从旧聊天记忆自行补 Timing 名称。

---

# 10. 二级词条与卡牌说明

卡牌本地二级词条主要通过：

`CardsTest.json`

中的：

`keywords`

维护。

卡牌自身局部描述和全局共享关键词不是完全相同的来源。

部分全局关键词说明仍由程序侧维护。

因此：

- 单卡关键词 → 可以查 FeatureGuide；
- 全局共享关键词 → 必须确认当前正式来源。

不要假设所有 Tooltip 文案都来自同一个 JSON。

---

# 11. 卡牌视觉

当前 Project Guilt 没有正式的：

> `cardID → 单卡独立插画`

数据管线。

当前 BattleCardUI 主要使用共享卡框。

不同：

- rarity；
- Sin Card；

决定共享框体表现。

如果未来希望每一张卡拥有独立 Artwork：

> 需要正式扩展系统，而不是自行往 CardsTest.json 增加一个不存在的 artwork 字段。

---

# 12. 战斗 Camera 与舞台表现

Camera 是此前已经经历多轮灰盒验证的重要系统。

以下结论属于当前冻结方向。

---

## 12.1 默认构图

已经基本锁定：

- 默认角色大小；
- 默认角色站位；
- 默认 Camera 构图；
- 默认地平线。

后续不应通过不断改变角色比例或默认 Camera 来迁就场景素材。

---

## 12.2 Background / Floor

正确思路：

> 先确定最终舞台画布，再让 Camera 在这个画布上工作。

因此：

- Background；
- Floor；

最终比例 / 范围属于舞台设计。

Camera 不应该反过来不断迁就错误尺寸的背景。

---

## 12.3 横向

横向移动保持尽量纯粹的 Horizontal Camera Movement。

不要为了背景适配在横向混入不必要的缩放或复杂补偿。

---

## 12.4 纵向

纵向目标不是：

> Camera 后退，把更多背景“缩进来”。

这种方案已经被实际验证为不符合目标。

正确目标：

默认：

> 大约显示 Background 的部分区域。

朝 Background 方向移动时：

1. 先通过 Vertical Pan / framing 揭示更多背景；
2. Background 表观尺寸尽量保持稳定；
3. 到达临界点；
4. 继续输入后进入第二阶段；
5. Camera Down + Tilt Up；
6. Floor 发生透视压缩；
7. Background 继续被揭示；
8. 最终完整看到 Background。

相反方向则用于查看更多 Floor。

---

## 12.5 Background 独立补偿

Camera 不再同时承担：

- Floor 透视；
- Background 尺寸稳定；
- Background 揭示；

三种职责。

当前架构方向是：

> Camera 主要负责 Floor。

> Background Billboard Visual 负责自己的 projected-size / depth / vertical framing 补偿。

当前拟使用层级：

`Background_Guide_Root`
→ `Background_FramingRoot`
→ `black-1 (SpriteRenderer + Billboard)`

Background 在第二阶段可以通过 Camera-space depth 与 viewport framing 进行补偿。

目标是：

> Background 的变化主要表现为“显示范围变化”，而不是“视觉尺寸被放大 / 缩小”。

不要重新回到：

- arbitrary Camera Backward；
- 简单 World Y Drop；
- 为了收进 Background 直接改 Camera Radius；

等旧方案。

---

# 13. 当前工程文档体系

ProjectDocs 已经完成一次正式治理。

主要职责如下。

---

## 13.1 AGENTS.md

作用：

> AI / Executor 长期执行协议。

包含：

- 角色职责；
- Gate；
- PLAN CONFLICT；
- Git；
- Unity 安全；
- JIT Test Migration；
- DOC IMPACT；
- batchmode 禁止等。

---

## 13.2 Assets/ProjectDocs/README.md

作用：

> 当前正式 ProjectDocs 导航入口。

区分：

- CURRENT；
- HISTORICAL；
- DEPRECATED；
- DEFERRED_DEBT。

注意：

> DEPRECATED 不等于 SAFE TO DELETE。

---

## 13.3 Developer/FeatureGuide.md

作用：

> HUMAN DEVELOPER FEATURE GUIDE。

面向用户 / 制作人员。

回答：

> “我要做 X，具体应该怎么改？”

重点是：

- 修改位置；
- 字段；
- 参数；
- 示例；
- 参考卡；
- 验证；
- 什么情况找 Sol。

不应重新退化成“某脚本被谁调用”的程序员架构说明。

---

## 13.4 Developer/CodeMap.md

作用：

> CANONICAL AI REPO MAP。

面向 Sol / AI。

记录：

- Domain；
- 当前路径；
- Runtime Entry；
- Data；
- Consumer；
- Dependency；
- Scene / Prefab / Profile；
- Regression Test；
- Harness；
- Risk；
- Deferred Debt。

CodeMap 用于快速定位。

真正做技术判断时：

> 仍应读取当前源码。

---

## 13.5 其他正式文档

例如：

- Architecture；
- RuntimeEntryPoints；
- DataPipeline；
- Testing 文档；
- RegressionTestMap；
- LegacyModeMigration；
- ManualHarnesses；

用于各自专业职责。

不应把所有信息复制进一个巨型总文档。

---

# 14. 测试治理

Project Guilt 不再追求：

> “把所有 Legacy Test 一次性迁完。”

当前测试治理为：

**JUST_IN_TIME_TEST_MIGRATION**

原则：

> 修改某个 Production 子系统时，再处理与这个子系统真正相关且有价值的测试。

不要因为：

- 文件在 Legacy；
- Mode 编号旧；
- 名字不好看；

就擅自删除。

删除或退休测试需要：

- 当前覆盖调查；
- caller / consumer 调查；
- 重叠分析；
- unique coverage 判断；
- Sol 明确授权。

---

## 14.1 Formal Suite

当前正式 Suite 已存在多个 Domain Owner。

主要包括：

- EnemyIntent；
- Cards；
- Execution；
- Bootstrap。

部分其他 Suite 目录仍可能是 placeholder。

Formal Case 当前主要通过项目自身的 static bool / compatibility caller / runner 体系工作。

它不是单纯 Unity Test Runner `[Test]` 模式。

不要因为测试形式不同就假设它“不正式”。

---

## 14.2 Presentation / Camera / UI

以下内容不要求为了形式上的自动化测试覆盖而阻塞 Demo：

- UI；
- Camera；
- Animation；
- Presentation；
- 视觉表现。

这些系统可以主要依赖：

- Shared Harness；
- Manual Harness；
- Unity Editor 人工验收。

---

# 15. 已完成的工程治理

## 15.1 Phase 7

Phase 7 已完成并关闭。

主要完成：

- Production 物理结构整理；
- Static Legacy Test 隔离；
- Support boundary；
- 低风险目录治理；
- 保留高风险兼容边界。

目标不是清空所有历史代码，而是建立：

> 可以继续 Demo 开发的安全结构。

---

## 15.2 Phase 8

Phase 8 已完成并关闭。

主要完成：

- AI Repo Map；
- 人类 Developer Feature Guide；
- ProjectDocs 导航；
- Runtime Entry Point；
- Data Pipeline；
- CURRENT / HISTORICAL / DEPRECATED / DEFERRED_DEBT 状态体系；
- Test governance；
- DOC IMPACT GATE；
- AI / Human 文档职责分离；
- AGENTS 执行协议校准。

Phase 8 完成后，工程治理不再是当前 Demo 开发的主要阻塞项。

---

# 16. 当前 Deferred Debt

以下系统存在已知架构债务，但目前不应因为架构洁癖主动拆除：

- BattleSimpleUIController；
- BattleSceneExecutionPresenter；
- BattleResolver；
- BattleActionSlotManager；
- BattleExecutionPlanExecutor；
- BattleSceneBootstrap；
- BattleCameraDirector。

它们属于：

> 已知、记录、允许暂时存在的 Deferred Debt。

原则：

> 如果当前 Demo 功能不要求修改它，就不要为了“漂亮”主动重构。

如果未来某个功能真正进入这些系统：

> 再基于当前需求重新做 Source Gate 与风险判断。

---

# 17. 当前已知 Demo Backlog

截至本文件 Last Verified，已记录的 Demo 待处理项包括：

1. 敲门声音文件问题；
2. 角色 Buff 图标描述与词条描述不一致；
3. Settings 确认按钮无效；
4. 删除绿色方块视觉点；
5. 狠击 + 试探同回合点数异常：
   - 狠击预期约 9–12；
   - 实际可能异常变成 2–4；
6. 敌人双防御时，玩家无法正常 Pass / 空过。

这些属于：

> 当前交接快照中的已知问题。

它们的优先级不是永久冻结的。

用户可以随时重新排序或加入新的 Demo 任务。

---

# 18. 当前 GitHub 正式基线

截至：

**2026-09-11**

当前确认的 `main` HEAD：

`3c12a6cb903f8942819a1a299ed67cefa4e016b7`

Commit：

`docs: rewrite developer feature guide`

该提交完成：

> FeatureGuide v1 正式操作手册化。

它的父提交为：

`1278569315d8bdfbd143cf4bf93bde41dc349d45`

该信息属于：

> 易变 Snapshot。

任何后续新 Sol 接手时，都必须重新检查当前 `main` HEAD。

不要因为本文件写了这个 SHA，就把它当成永久 baseline。

---

# 19. 当前协作模式摘要

Project Guilt 使用固定三方协作。

---

## 19.1 用户

负责：

- 产品目标；
- 玩法目标；
- 设计决策；
- 优先级；
- 最终 Unity 人工验收；
- 是否允许 commit / push。

用户主要负责：

> 战斗系统、卡牌设计和相关制作。

剧情场景逻辑主要由用户朋友负责。

---

## 19.2 Sol

Sol / 高阶主模型作为 Technical Lead。

负责：

- 架构判断；
- 技术决策；
- GitHub 分析；
- Source Gate；
- 冻结 Plan；
- 编写 Executor Task；
- 审查执行结果；
- 用户 push 后执行 Remote Gate；
- 判断 Batch 是否关闭。

Sol 不应：

> 把模糊架构问题直接丢给 Executor 自己设计。

---

## 19.3 Executor

Executor 可以是 Luna、Codex 或其他模型。

身份以用户当前声明为准。

职责：

- 检查本地真实仓库；
- 执行冻结方案；
- 报告本地事实；
- 不扩大范围；
- 不自行重新设计。

遇到冻结方案与真实仓库冲突：

> 报告 PLAN CONFLICT。

---

# 20. 固定 Gate

正式流程：

`SOURCE GATE`
→ `PLAN GATE`
→ `EXECUTION`
→ `UNITY GATE`
→ `COMMIT / PUSH`
→ `REMOTE GATE`

只有：

> REMOTE GATE PASS

之后，该 Batch / Phase 才正式关闭。

---

# 21. Git / Unity 安全边界

长期规则：

- 未经用户明确允许，不 stage；
- 未经用户明确允许，不 commit；
- 未经用户明确允许，不 push；
- 禁止盲目 `git add .`；
- 必须保护任务开始前已经存在的 dirty 文件；
- 提交前检查目标文件范围；
- Unity `.meta` 与 GUID 必须保留；
- 已存在 `.meta` 不随意重建；
- Scene / Prefab / ScriptableObject / asmdef 属于高风险文件；
- serialized reference 优先检查 GUID；
- 不默认使用 Unity batchmode；
- Runtime / Visual 修改通常需要 Unity 人工 Gate。

---

# 22. DOC IMPACT GATE

Production 修改后应判断：

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

检查不意味着必须每次编辑所有文档。

目标是：

> 防止正式代码和正式文档长期漂移。

---

# 23. 当前交接阶段

当前工程治理已经结束。

当前正在完成最后两份长期交接资料：

1. `ProjectGuilt_LongTermProjectMemory.md`
2. `ProjectGuilt_SolHandoffProtocol.md`

本文件即第一份。

第二份：

> 《Project Guilt — Sol 协作系统接班协议》

将专门规定：

- 新 Sol 如何接手；
- 先查什么；
- 怎么做 Source Gate；
- 怎么冻结 Plan；
- 怎么写 Executor Task；
- 怎么处理 PLAN CONFLICT；
- 怎么做 Unity / Commit / Remote Gate；
- 怎么使用 CodeMap / FeatureGuide；
- 怎么处理 Testing / JIT Migration；
- 怎么维护长期项目记忆。

这两份完成并经过 Remote Gate 后：

> 项目交接 / 治理阶段可以正式结束。

然后回到 Demo 功能和 Bug 开发。

---

# 24. 当前下一阶段

当前推荐顺序：

第一步：

> 完成本文件并保存到正式仓库。

第二步：

> 完成《Project Guilt — Sol 协作系统接班协议》。

第三步：

> 回到 Demo 功能 / Bug backlog。

不建议再次开启大规模目录整理或架构清理。

后续原则：

> Demo 功能优先，必要重构随功能进入。

---

# 25. 哪些信息不能长期直接相信本文件

以下内容属于高频变化事实：

- 最新 commit SHA；
- 当前分支状态；
- working tree dirty；
- 当前具体卡牌点数；
- 当前 Buff 数值；
- 当前文件路径；
- 当前测试 Case 数量；
- 当前 Scene serialized reference；
- 当前具体 Trait / EffectType / Timing 列表；
- 当前 Bug 数量；
- 当前任务优先级；
- 某个 Deferred Debt 是否已经解决。

新 Sol 需要处理这些内容时：

1. 先看 CURRENT ProjectDocs；
2. 再看 CodeMap / FeatureGuide；
3. 再查当前 GitHub；
4. 必要时让 Executor 检查本地工作树；
5. 不凭本文件旧 Snapshot 猜。

---

# 26. 本文件维护规则

本文件是：

> Project Guilt 当前项目级长期上下文的 Canonical Handoff Snapshot。

它必须保持可信，但不需要每次 commit 都更新。

---

## 26.1 什么时候需要检查是否更新

每个重要 Batch / Phase 完成 Remote Gate 后，Sol 应执行：

```text
LONG-TERM MEMORY IMPACT

LongTermProjectMemory: YES / NO
Reason:
```

---

## 26.2 什么情况下通常需要更新

包括但不限于：

- 一个重要 Phase 正式关闭；
- 一个核心系统从未完成进入正式可用；
- Demo 范围发生改变；
- 当前主线开发目标明显改变；
- 核心架构发生变化；
- 已冻结设计结论被正式推翻；
- 新的长期设计结论被正式冻结；
- Deferred Debt 状态发生重要变化；
- Testing / Document / Collaboration 治理发生长期变化；
- 当前 backlog 出现明显重新排序；
- 当前“下一阶段”发生变化；
- 如果不更新，新 Sol 会对项目当前状态产生明显误解。

判断核心不是：

> “代码有没有变化？”

而是：

> “如果不更新，新接手者会不会对现在项目做到哪里产生实质性错误理解？”

---

## 26.3 什么情况下通常不需要更新

例如：

- 一张卡从 4–8 调成 5–9；
- 普通数值平衡；
- 一个小 UI Bug；
- 单独一个普通 commit；
- 小字段调整；
- CodeMap 已覆盖的普通路径移动；
- 不改变项目状态的内部重构；
- 临时实验；
- 文案细调。

这些信息由：

- Git；
- FeatureGuide；
- CodeMap；
- Domain Docs；
- 数据文件；

负责。

---

## 26.4 谁负责判断更新

由：

> **Sol**

负责判断 Long-Term Memory Impact。

原因：

需要判断：

- 什么已经成为正式事实；
- 什么仍是实验；
- 什么已经过时；
- 什么应该从“下一步”移动到“已完成”；
- 什么值得长期保留。

Executor 不负责自行总结长期项目状态。

---

## 26.5 谁负责写更新内容

原则：

> Sol 负责内容。

Executor 可以负责：

> 按 Sol 冻结内容机械落盘。

不允许给 Executor：

> “根据最近代码自行总结更新长期项目记忆。”

这属于架构 / 项目状态判断，不应下放。

---

## 26.6 正确更新方式

本文件维护：

> 当前真相。

不要变成：

> 时间流水账。

错误方式：

```text
9 月 11 日做了……
9 月 12 日做了……
9 月 13 日又做了……
```

正确方式：

如果某项完成：

- 从“待完成”删除；
- 写入“已完成”；
- 更新“下一阶段”；
- 删除已经不再成立的旧状态。

Git 历史已经负责保存过去版本。

本文件只需要保持：

> 当前版本可信。

---

## 26.7 文件名与版本

Canonical 文件名固定：

`Docs/Handoff/ProjectGuilt_LongTermProjectMemory.md`

不要创建：

- `_v1.1.md`
- `_v1.2.md`
- `_final.md`
- `_new.md`

等多个并存版本。

文件内部维护：

`Document Version`

普通项目状态更新：

`1.0 → 1.1 → 1.2`

只有项目阶段或整体工作方式发生重大变化时：

`1.x → 2.0`

例如：

- Demo 阶段正式结束；
- 进入完整游戏 Production；
- 核心战斗系统整体重构；
- 三方协作模式发生根本变化。

---

# 27. 最后原则

Project Guilt 当前已经完成足够的工程治理。

从现在开始，项目不应继续因为：

> “还能再整理一点”

而长期停留在工程治理阶段。

以后应遵循：

> 能直接安全制作 Demo，就制作 Demo。

> 真正遇到结构问题，再处理结构问题。

> 必要重构跟随真实需求进入。

> 不为了架构洁癖阻碍游戏完成。

同时：

> 任何长期项目记忆都不能替代真实仓库。

最终事实优先级始终是：

当前用户明确决策  
→ 当前正式仓库事实  
→ CURRENT ProjectDocs  
→ 当前本地 Source Gate 报告  
→ 本文件长期 Snapshot  
→ 旧对话 / 历史记忆
