// 脚本中文说明：验证多槽位 Enemy Intent 在 Relation Query 中保持独立 Slot Identity。
using System.Collections.Generic;
using UnityEngine;

public static class BattleMultiSlotIntentRelationQueryTests
{
    private sealed class Fixture
    {
        public BattleRuntimeState runtime;
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleCardState allyAttackA;
        public BattleCardState allyAttackB;
        public BattleEnemyIntent intent1;
        public BattleEnemyIntent intent2;
        public BattleEnemyIntent intent3;
    }

    public static bool Run()
    {
        Fixture fixture = CreateThreeAttackFixture();
        BattleActionRelationQueryService query =
            new BattleActionRelationQueryService(fixture.runtime);
        BattleActionRelationDescriptor descriptor1 =
            GetSingleDescriptor(query, fixture.intent1);
        BattleActionRelationDescriptor descriptor2 =
            GetSingleDescriptor(query, fixture.intent2);
        BattleActionRelationDescriptor descriptor3 =
            GetSingleDescriptor(query, fixture.intent3);

        bool[] results =
        {
            query.TryGetIntentBySlot(fixture.enemy, 1, out var slot1) &&
                object.ReferenceEquals(slot1, fixture.intent1),
            HasIntentSource(descriptor1, fixture.intent1, 1),
            HasIntentSource(descriptor2, fixture.intent2, 2),
            descriptor1 != null && descriptor2 != null &&
                descriptor1.RelationID != descriptor2.RelationID,
            HasIntentSource(descriptor3, fixture.intent3, 3),
            descriptor1 != null && descriptor2 != null &&
                object.ReferenceEquals(
                    descriptor1.ActualTargetCharacter,
                    descriptor2.ActualTargetCharacter
                ) && descriptor1.ActualTargetSlotIndex ==
                    descriptor2.ActualTargetSlotIndex,
            descriptor3 != null &&
                object.ReferenceEquals(
                    descriptor3.ActualTargetCharacter,
                    fixture.allyB
                ) && descriptor3.ActualTargetSlotIndex == 2,
            HasDistinctRelationIDs(query.GetAllCurrentRelations()),
            query.TryGetIntentBySlot(fixture.enemy, 2, out var slot2) &&
                object.ReferenceEquals(slot2, fixture.intent2),
            !query.TryGetIntentBySlot(fixture.enemy, 4, out var missing) &&
                missing == null,
            VerifyNonAttackIntentExists(CardType.Defense),
            VerifyNonAttackDescriptor(CardType.Defense),
            VerifyNonAttackDescriptor(CardType.Dodge),
            VerifyResponseIdentity(1),
            VerifyResponseIdentity(2),
            VerifyTwoResponsesRemainIndependent(),
            descriptor3 != null && object.ReferenceEquals(
                descriptor3.ActualTargetCharacter,
                fixture.intent3.actualTargetCharacter
            ),
            VerifyQueueOrderDoesNotDefineSlotIdentity(fixture, query)
        };

        string[] names =
        {
            "Enemy单Slot Attack可按具体Slot查询",
            "UI Slot0对应formal sourceSlot 1",
            "UI Slot1对应formal sourceSlot 2",
            "同Enemy Slot0/Slot1 Descriptor不覆盖",
            "formal Slot3 Query Contract成立",
            "同目标多Slot仍保留独立来源",
            "不同目标分别保留actualTarget",
            "Relation Key区分同角色不同Slot",
            "查询Slot1不会返回Slot0 Intent",
            "不存在Slot安全失败且不回退Slot0",
            "Defense Intent仍存在于Query Contract",
            "Defense Intent建立中性Target Descriptor",
            "Dodge Intent建立中性Target Descriptor",
            "Player Response精确绑定Enemy Slot0",
            "Player Response精确绑定Enemy Slot1",
            "两个Response分别绑定且不覆盖",
            "Descriptor直接保留Intent actualTarget",
            "Intent列表换序后仍按Slot Identity查询"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式99 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式99 Multi-slot Intent/Relation Query聚合结果：" + allPassed);
        return allPassed;
    }

    private static bool VerifyNonAttackIntentExists(string cardType)
    {
        Fixture fixture = CreateSingleIntentFixture(cardType, 2);
        BattleActionRelationQueryService query =
            new BattleActionRelationQueryService(fixture.runtime);
        return query.TryGetIntentBySlot(
            fixture.enemy,
            2,
            out BattleEnemyIntent intent
        ) && object.ReferenceEquals(intent, fixture.intent1);
    }

    private static bool VerifyNonAttackDescriptor(string cardType)
    {
        Fixture fixture = CreateSingleIntentFixture(cardType, 2);
        BattleActionRelationDescriptor descriptor = GetSingleDescriptor(
            new BattleActionRelationQueryService(fixture.runtime),
            fixture.intent1
        );
        return HasIntentSource(descriptor, fixture.intent1, 2) &&
            descriptor.EnemyActionType == cardType &&
            descriptor.Kind == BattleActionRelationKind.EnemyUnilateralTarget;
    }

    private static bool VerifyResponseIdentity(int enemySlotIndex)
    {
        Fixture fixture = CreateSingleIntentFixture(
            CardType.Attack,
            enemySlotIndex
        );
        BattleActionSlot responseSlot = new BattleActionSlot(
            fixture.allyA,
            enemySlotIndex
        );
        responseSlot.AssignResponse(
            fixture.allyA,
            fixture.allyAttackA,
            fixture.intent1,
            false
        );
        fixture.intent1.SetActualTarget(
            fixture.allyA,
            responseSlot.slotIndex
        );
        fixture.intent1.MarkResponded();
        fixture.runtime.SetActionSlots(new List<BattleActionSlot>
        {
            responseSlot
        });

        BattleActionRelationDescriptor descriptor = GetSingleDescriptor(
            new BattleActionRelationQueryService(fixture.runtime),
            fixture.intent1
        );
        return HasIntentSource(
                descriptor,
                fixture.intent1,
                enemySlotIndex
            ) && object.ReferenceEquals(descriptor.ResponseSlot, responseSlot);
    }

    private static bool VerifyTwoResponsesRemainIndependent()
    {
        Fixture fixture = CreateThreeAttackFixture();
        BattleActionSlot responseA = new BattleActionSlot(fixture.allyA, 1);
        BattleActionSlot responseB = new BattleActionSlot(fixture.allyB, 1);
        BindResponse(
            responseA,
            fixture.allyA,
            fixture.allyAttackA,
            fixture.intent1
        );
        BindResponse(
            responseB,
            fixture.allyB,
            fixture.allyAttackB,
            fixture.intent2
        );
        fixture.runtime.SetActionSlots(new List<BattleActionSlot>
        {
            responseA,
            responseB
        });

        BattleActionRelationQueryService query =
            new BattleActionRelationQueryService(fixture.runtime);
        BattleActionRelationDescriptor first =
            GetSingleDescriptor(query, fixture.intent1);
        BattleActionRelationDescriptor second =
            GetSingleDescriptor(query, fixture.intent2);
        return first != null && second != null &&
            object.ReferenceEquals(first.ResponseSlot, responseA) &&
            object.ReferenceEquals(second.ResponseSlot, responseB) &&
            first.RelationID != second.RelationID;
    }

    private static bool VerifyQueueOrderDoesNotDefineSlotIdentity(
        Fixture fixture,
        BattleActionRelationQueryService query
    )
    {
        fixture.runtime.SetIntentQueue(new List<BattleEnemyIntent>
        {
            fixture.intent3,
            fixture.intent1,
            fixture.intent2
        });
        return query.TryGetIntentBySlot(
                fixture.enemy,
                2,
                out BattleEnemyIntent intent
            ) && object.ReferenceEquals(intent, fixture.intent2) &&
            HasIntentSource(
                GetSingleDescriptor(query, intent),
                fixture.intent2,
                2
            );
    }

    private static void BindResponse(
        BattleActionSlot slot,
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent intent
    )
    {
        slot.AssignResponse(actor, cardState, intent, false);
        intent.SetActualTarget(actor, slot.slotIndex);
        intent.MarkResponded();
    }

    private static BattleActionRelationDescriptor GetSingleDescriptor(
        BattleActionRelationQueryService query,
        BattleEnemyIntent intent
    )
    {
        IReadOnlyList<BattleActionRelationDescriptor> descriptors =
            query.GetRelationsForIntent(intent);
        return descriptors.Count == 1 ? descriptors[0] : null;
    }

    private static bool HasIntentSource(
        BattleActionRelationDescriptor descriptor,
        BattleEnemyIntent intent,
        int expectedFormalSlotIndex
    )
    {
        return descriptor != null &&
            object.ReferenceEquals(descriptor.SourceIntent, intent) &&
            object.ReferenceEquals(
                descriptor.IntentSourceCharacter,
                intent.enemy
            ) && descriptor.IntentSourceSlotIndex == expectedFormalSlotIndex;
    }

    private static bool HasDistinctRelationIDs(
        IReadOnlyList<BattleActionRelationDescriptor> descriptors
    )
    {
        HashSet<string> ids = new HashSet<string>();
        for (int index = 0; index < descriptors.Count; index++)
        {
            if (!ids.Add(descriptors[index].RelationID))
            {
                return false;
            }
        }
        return descriptors.Count == 3;
    }

    private static Fixture CreateThreeAttackFixture()
    {
        Fixture fixture = CreateBaseFixture();
        BattleCardState enemyAttack1 = CreateCardState(
            fixture.enemy,
            CardType.Attack,
            "mode99_enemy_attack_1"
        );
        BattleCardState enemyAttack2 = CreateCardState(
            fixture.enemy,
            CardType.Attack,
            "mode99_enemy_attack_2"
        );
        BattleCardState enemyAttack3 = CreateCardState(
            fixture.enemy,
            CardType.Attack,
            "mode99_enemy_attack_3"
        );
        fixture.intent1 = CreateIntent(
            "mode99_intent_1",
            fixture.enemy,
            enemyAttack1,
            fixture.allyA,
            1,
            1
        );
        fixture.intent2 = CreateIntent(
            "mode99_intent_2",
            fixture.enemy,
            enemyAttack2,
            fixture.allyA,
            1,
            2
        );
        fixture.intent3 = CreateIntent(
            "mode99_intent_3",
            fixture.enemy,
            enemyAttack3,
            fixture.allyB,
            2,
            3
        );
        fixture.runtime.SetIntentQueue(new List<BattleEnemyIntent>
        {
            fixture.intent1,
            fixture.intent2,
            fixture.intent3
        });
        return fixture;
    }

    private static Fixture CreateSingleIntentFixture(
        string cardType,
        int enemySlotIndex
    )
    {
        Fixture fixture = CreateBaseFixture();
        fixture.intent1 = CreateIntent(
            "mode99_single_" + cardType,
            fixture.enemy,
            CreateCardState(
                fixture.enemy,
                cardType,
                "mode99_enemy_" + cardType
            ),
            fixture.allyA,
            1,
            enemySlotIndex
        );
        fixture.runtime.SetIntentQueue(new List<BattleEnemyIntent>
        {
            fixture.intent1
        });
        return fixture;
    }

    private static Fixture CreateBaseFixture()
    {
        Fixture fixture = new Fixture
        {
            allyA = new CharacterData("mode99_ally_a", 30, 10, 10),
            allyB = new CharacterData("mode99_ally_b", 30, 9, 9),
            enemy = new CharacterData("mode99_enemy", 50, 5, 5)
        };
        fixture.allyAttackA = CreateCardState(
            fixture.allyA,
            CardType.Attack,
            "mode99_ally_attack_a"
        );
        fixture.allyAttackB = CreateCardState(
            fixture.allyB,
            CardType.Attack,
            "mode99_ally_attack_b"
        );
        fixture.runtime = new BattleRuntimeState();
        fixture.runtime.SetCharacters(
            fixture.allyA,
            fixture.allyB,
            fixture.enemy
        );
        fixture.runtime.SetActionSlots(new List<BattleActionSlot>());
        BattleLifecyclePhaseContractTests.TryReachPhaseForTest(
            fixture.runtime,
            BattleLifecyclePhase.Prepare
        );
        return fixture;
    }

    private static BattleEnemyIntent CreateIntent(
        string id,
        CharacterData enemy,
        BattleCardState cardState,
        CharacterData target,
        int targetSlotIndex,
        int enemySlotIndex
    )
    {
        return new BattleEnemyIntent(
            id,
            enemy,
            cardState,
            target,
            targetSlotIndex,
            enemySlotIndex,
            enemySlotIndex
        );
    }

    private static BattleCardState CreateCardState(
        CharacterData owner,
        string cardType,
        string id
    )
    {
        CardTestData card = new CardTestData
        {
            cardID = id,
            cardName = id,
            cardType = cardType,
            minPoint = 1,
            maxPoint = 1,
            cooldown = 0,
            damageFormula = "PointAsDamage"
        };
        return new BattleCardState(owner, card, id + "_state");
    }
}
