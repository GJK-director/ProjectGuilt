// 脚本中文说明：用Recording Adapter验证多槽位Relation的Visual Identity、可见性与Anchor映射。
using System.Collections.Generic;
using UnityEngine;

public static class BattleMultiSlotRelationRenderingTests
{
    private sealed class RecordedVisual
    {
        public BattleActionRelationDescriptor Descriptor;
        public bool Visible;
        public bool SourceAnchorAvailable;
        public bool TargetAnchorAvailable;
    }

    private sealed class RecordingVisualAdapter
    {
        private readonly Dictionary<string, RecordedVisual> states =
            new Dictionary<string, RecordedVisual>();
        private readonly Dictionary<string, RectTransform> anchors;

        public int StateCount => states.Count;

        public RecordingVisualAdapter(
            Dictionary<string, RectTransform> registeredAnchors
        )
        {
            anchors = registeredAnchors;
        }

        public void Refresh(
            IReadOnlyList<BattleActionRelationDescriptor> descriptors,
            string hoveredSlotID,
            string selectedSlotID,
            bool revealAll
        )
        {
            states.Clear();
            if (descriptors == null)
            {
                return;
            }

            for (int index = 0; index < descriptors.Count; index++)
            {
                BattleActionRelationDescriptor descriptor =
                    descriptors[index];
                if (descriptor == null ||
                    string.IsNullOrEmpty(descriptor.RelationID))
                {
                    continue;
                }

                states[descriptor.RelationID] = new RecordedVisual
                {
                    Descriptor = descriptor,
                    Visible = BattleActionRelationVisibilityPolicy.IsVisible(
                        descriptor,
                        hoveredSlotID,
                        selectedSlotID,
                        revealAll
                    ),
                    SourceAnchorAvailable = HasAnchor(
                        descriptor.SourceSlotID
                    ),
                    TargetAnchorAvailable = HasAnchor(
                        descriptor.TargetSlotID
                    )
                };
            }
        }

        public bool TryGet(
            string relationID,
            out RecordedVisual visual
        )
        {
            return states.TryGetValue(relationID, out visual);
        }

        private bool HasAnchor(string slotID)
        {
            return !string.IsNullOrEmpty(slotID) &&
                anchors != null && anchors.TryGetValue(
                    slotID,
                    out RectTransform anchor
                ) && anchor != null;
        }
    }

    private sealed class Fixture
    {
        public BattleRuntimeState runtime;
        public BattleActionRelationQueryService slotIdentity;
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleEnemyIntent attackIntent;
        public BattleEnemyIntent defenseIntent;
        public BattleEnemyIntent dodgeIntent;
        public BattleActionRelationDescriptor attackRelation;
        public BattleActionRelationDescriptor defenseRelation;
        public BattleActionRelationDescriptor dodgeRelation;
        public Dictionary<string, RectTransform> anchors =
            new Dictionary<string, RectTransform>();
        public List<GameObject> createdObjects = new List<GameObject>();
    }

    public static bool Run()
    {
        Fixture fixture = CreateFixture();
        try
        {
            RecordingVisualAdapter adapter =
                new RecordingVisualAdapter(fixture.anchors);
            BattleActionRelationDescriptor differentTarget =
                CreateEnemyRelation(
                    fixture,
                    CreateIntent(
                        fixture.enemy,
                        CreateCardState(
                            fixture.enemy,
                            CardType.Attack,
                            "mode100_enemy_attack_different"
                        ),
                        fixture.allyB,
                        2,
                        4
                    )
                );
            IReadOnlyList<BattleActionRelationDescriptor> firstTwo =
                new List<BattleActionRelationDescriptor>
                {
                    fixture.attackRelation,
                    fixture.defenseRelation
                };

            adapter.Refresh(firstTwo, string.Empty, string.Empty, false);
            bool test1 = HasState(adapter, fixture.attackRelation, false);
            bool test2 = adapter.StateCount == 2 &&
                HasState(adapter, fixture.attackRelation, false) &&
                HasState(adapter, fixture.defenseRelation, false);
            bool test3 = VerifySourceSlotViewMapping(
                fixture,
                fixture.attackIntent,
                0,
                1
            );
            bool test4 = VerifySourceSlotViewMapping(
                fixture,
                fixture.defenseIntent,
                1,
                2
            );

            adapter.Refresh(firstTwo, string.Empty, string.Empty, true);
            bool test5 = IsVisible(adapter, fixture.attackRelation) &&
                IsVisible(adapter, fixture.defenseRelation);
            adapter.Refresh(firstTwo, "Enemy:1", string.Empty, false);
            bool test6 = IsVisible(adapter, fixture.attackRelation) &&
                !IsVisible(adapter, fixture.defenseRelation);
            adapter.Refresh(firstTwo, "Enemy:2", string.Empty, false);
            bool test7 = !IsVisible(adapter, fixture.attackRelation) &&
                IsVisible(adapter, fixture.defenseRelation);
            bool test8 = test6 && test7 && adapter.StateCount == 2;
            adapter.Refresh(firstTwo, string.Empty, "Enemy:1", false);
            bool test9 = IsVisible(adapter, fixture.attackRelation) &&
                !IsVisible(adapter, fixture.defenseRelation);
            adapter.Refresh(firstTwo, string.Empty, "Enemy:2", false);
            bool test10 = !IsVisible(adapter, fixture.attackRelation) &&
                IsVisible(adapter, fixture.defenseRelation);

            adapter.Refresh(firstTwo, string.Empty, string.Empty, false);
            bool test11 = HasState(adapter, fixture.defenseRelation, false);
            adapter.Refresh(firstTwo, "Enemy:2", string.Empty, false);
            bool test12 = IsVisible(adapter, fixture.defenseRelation);
            adapter.Refresh(
                new List<BattleActionRelationDescriptor>
                {
                    fixture.dodgeRelation
                },
                string.Empty,
                string.Empty,
                true
            );
            bool test13 = HasState(adapter, fixture.dodgeRelation, true);

            adapter.Refresh(firstTwo, string.Empty, string.Empty, true);
            bool test14 = adapter.StateCount == 2 &&
                fixture.attackRelation.TargetSlotID ==
                    fixture.defenseRelation.TargetSlotID;
            adapter.Refresh(
                new List<BattleActionRelationDescriptor>
                {
                    fixture.attackRelation,
                    differentTarget
                },
                string.Empty,
                string.Empty,
                true
            );
            bool test15 = fixture.attackRelation.TargetSlotID !=
                    differentTarget.TargetSlotID &&
                HasResolvedTargetAnchor(adapter, fixture.attackRelation) &&
                HasResolvedTargetAnchor(adapter, differentTarget);

            BattleActionRelationDescriptor response1 =
                CreateResponseRelation(fixture, fixture.attackIntent, 1);
            BattleActionRelationDescriptor response2 =
                CreateResponseRelation(fixture, fixture.defenseIntent, 2);
            adapter.Refresh(
                new List<BattleActionRelationDescriptor>
                {
                    response1
                },
                string.Empty,
                string.Empty,
                true
            );
            bool test16 = HasResponseIdentity(response1, 1) &&
                HasState(adapter, response1, true);
            adapter.Refresh(
                new List<BattleActionRelationDescriptor>
                {
                    response2
                },
                string.Empty,
                string.Empty,
                true
            );
            bool test17 = HasResponseIdentity(response2, 2) &&
                HasState(adapter, response2, true);
            adapter.Refresh(
                new List<BattleActionRelationDescriptor>
                {
                    response1,
                    response2
                },
                string.Empty,
                string.Empty,
                true
            );
            bool test18 = adapter.StateCount == 2 &&
                response1.RelationID != response2.RelationID &&
                IsVisible(adapter, response1) &&
                IsVisible(adapter, response2);

            adapter.Refresh(
                new List<BattleActionRelationDescriptor>
                {
                    fixture.dodgeRelation
                },
                string.Empty,
                string.Empty,
                true
            );
            bool test19 = TryGetVisual(
                    adapter,
                    fixture.dodgeRelation,
                    out RecordedVisual slot3Visual
                ) && slot3Visual.Visible &&
                !slot3Visual.SourceAnchorAvailable;

            adapter.Refresh(
                new List<BattleActionRelationDescriptor>
                {
                    fixture.defenseRelation,
                    fixture.attackRelation
                },
                "Enemy:2",
                string.Empty,
                false
            );
            bool test20 = adapter.StateCount == 2 &&
                !IsVisible(adapter, fixture.attackRelation) &&
                IsVisible(adapter, fixture.defenseRelation);

            bool[] results =
            {
                test1, test2, test3, test4, test5,
                test6, test7, test8, test9, test10,
                test11, test12, test13, test14, test15,
                test16, test17, test18, test19, test20
            };
            string[] names =
            {
                "单Enemy Slot1 Attack建立Visual State",
                "同Enemy Slot1/Slot2拥有独立Visual Identity",
                "formal Slot1映射UI Slot0 Source Anchor",
                "formal Slot2映射UI Slot1 Source Anchor",
                "RevealAll同时显示Slot1/Slot2",
                "Hover Slot1只命中Slot1 Relation",
                "Hover Slot2只命中Slot2 Relation",
                "Hover切换不残留错误Slot Visual",
                "Selection Slot1精确命中Relation",
                "Selection Slot2精确命中Relation",
                "Defense Descriptor建立Visual State",
                "Defense满足Policy时Visible",
                "Dodge不因CardType被Visual过滤",
                "同actualTarget保留两个Source Visual",
                "不同actualTarget解析不同End Anchor",
                "Player Response精确绑定Enemy Slot1",
                "Player Response精确绑定Enemy Slot2",
                "两个Response Visual互不覆盖",
                "formal Slot3保留identity并安全缺少Anchor",
                "Descriptor换序不改变Visibility Identity"
            };

            bool allPassed = true;
            for (int index = 0; index < results.Length; index++)
            {
                Debug.Log(
                    "模式100 测试" + (index + 1) + " " + names[index] +
                    "：" + results[index]
                );
                allPassed &= results[index];
            }
            Debug.Log(
                "模式100 Multi-slot Relation Rendering聚合结果：" +
                allPassed
            );
            return allPassed;
        }
        finally
        {
            DestroyFixture(fixture);
        }
    }

    private static Fixture CreateFixture()
    {
        Fixture fixture = new Fixture
        {
            allyA = new CharacterData("mode100_ally_a", 30, 10, 10),
            allyB = new CharacterData("mode100_ally_b", 30, 9, 9),
            enemy = new CharacterData("mode100_enemy", 50, 5, 5)
        };
        fixture.attackIntent = CreateIntent(
            fixture.enemy,
            CreateCardState(
                fixture.enemy,
                CardType.Attack,
                "mode100_enemy_attack"
            ),
            fixture.allyA,
            1,
            1
        );
        fixture.defenseIntent = CreateIntent(
            fixture.enemy,
            CreateCardState(
                fixture.enemy,
                CardType.Defense,
                "mode100_enemy_defense"
            ),
            fixture.allyA,
            1,
            2
        );
        fixture.dodgeIntent = CreateIntent(
            fixture.enemy,
            CreateCardState(
                fixture.enemy,
                CardType.Dodge,
                "mode100_enemy_dodge"
            ),
            fixture.allyB,
            2,
            3
        );
        fixture.runtime = new BattleRuntimeState();
        fixture.runtime.SetCharacters(
            fixture.allyA,
            fixture.allyB,
            fixture.enemy
        );
        fixture.slotIdentity = new BattleActionRelationQueryService(
            fixture.runtime
        );
        fixture.attackRelation = CreateEnemyRelation(
            fixture,
            fixture.attackIntent
        );
        fixture.defenseRelation = CreateEnemyRelation(
            fixture,
            fixture.defenseIntent
        );
        fixture.dodgeRelation = CreateEnemyRelation(
            fixture,
            fixture.dodgeIntent
        );

        RegisterSlotView(fixture, fixture.enemy, 0, fixture.attackIntent);
        RegisterSlotView(fixture, fixture.enemy, 1, fixture.defenseIntent);
        RegisterSlotView(fixture, fixture.allyA, 0, null);
        RegisterSlotView(fixture, fixture.allyB, 1, null);
        return fixture;
    }

    private static BattleActionSlotUIView RegisterSlotView(
        Fixture fixture,
        CharacterData character,
        int uiSlotIndex,
        BattleEnemyIntent intent
    )
    {
        GameObject value = new GameObject(
            "Mode100Slot_" + character.characterName + "_" + uiSlotIndex,
            typeof(RectTransform),
            typeof(BattleActionSlotUIView)
        );
        fixture.createdObjects.Add(value);
        BattleActionSlotUIView view = value.GetComponent<
            BattleActionSlotUIView
        >();
        view.BindInteraction(character, uiSlotIndex, intent != null, null);
        view.SetBoundEnemyIntent(intent);
        string slotID = fixture.slotIdentity.GetSlotID(
            character,
            view.FormalSlotIndex
        );
        fixture.anchors[slotID] = view.RelationLineAnchor;
        return view;
    }

    private static bool VerifySourceSlotViewMapping(
        Fixture fixture,
        BattleEnemyIntent intent,
        int uiSlotIndex,
        int formalSlotIndex
    )
    {
        BattleActionSlotUIView view = null;
        for (int index = 0; index < fixture.createdObjects.Count; index++)
        {
            BattleActionSlotUIView candidate = fixture.createdObjects[index]
                .GetComponent<BattleActionSlotUIView>();
            if (candidate != null && candidate.IsEnemySlot &&
                candidate.UISlotIndex == uiSlotIndex)
            {
                view = candidate;
                break;
            }
        }
        string expectedID = fixture.slotIdentity.GetSlotID(
            fixture.enemy,
            formalSlotIndex
        );
        return view != null && view.FormalSlotIndex == formalSlotIndex &&
            object.ReferenceEquals(view.BoundEnemyIntent, intent) &&
            fixture.anchors.ContainsKey(expectedID);
    }

    private static BattleActionRelationDescriptor CreateEnemyRelation(
        Fixture fixture,
        BattleEnemyIntent intent
    )
    {
        string source = fixture.slotIdentity.GetSlotID(
            intent.enemy,
            intent.enemySlotIndex
        );
        string target = fixture.slotIdentity.GetSlotID(
            intent.actualTargetCharacter,
            intent.actualTargetSlotIndex
        );
        return new BattleActionRelationDescriptor(
            source + "->" + target,
            BattleActionRelationKind.EnemyUnilateralTarget,
            source,
            target,
            target,
            source,
            BattleActionRelationSide.Enemy,
            intent.enemySlotIndex,
            intent.actualTargetSlotIndex,
            string.Empty,
            intent.enemyCardState.cardData.cardType,
            false,
            intent
        );
    }

    private static BattleActionRelationDescriptor CreateResponseRelation(
        Fixture fixture,
        BattleEnemyIntent intent,
        int playerSlotIndex
    )
    {
        BattleActionSlot response = new BattleActionSlot(
            fixture.allyA,
            playerSlotIndex
        );
        BattleCardState attack = CreateCardState(
            fixture.allyA,
            CardType.Attack,
            "mode100_response_" + intent.enemySlotIndex
        );
        response.AssignResponse(fixture.allyA, attack, intent, false);
        string playerSlot = fixture.slotIdentity.GetSlotID(
            fixture.allyA,
            playerSlotIndex
        );
        string enemySlot = fixture.slotIdentity.GetSlotID(
            intent.enemy,
            intent.enemySlotIndex
        );
        return new BattleActionRelationDescriptor(
            playerSlot + "<->" + enemySlot,
            intent.enemyCardState.cardData.cardType == CardType.Defense
                ? BattleActionRelationKind.DefenseResponse
                : BattleActionRelationKind.AttackClash,
            playerSlot,
            enemySlot,
            playerSlot,
            enemySlot,
            BattleActionRelationSide.Player,
            playerSlotIndex,
            intent.enemySlotIndex,
            CardType.Attack,
            intent.enemyCardState.cardData.cardType,
            true,
            intent,
            response
        );
    }

    private static bool HasResponseIdentity(
        BattleActionRelationDescriptor relation,
        int expectedEnemySlotIndex
    )
    {
        return relation != null && relation.SourceIntent != null &&
            relation.IntentSourceSlotIndex == expectedEnemySlotIndex &&
            relation.EnemySlotID == "Enemy:" + expectedEnemySlotIndex &&
            relation.ResponseSlot != null;
    }

    private static bool HasState(
        RecordingVisualAdapter adapter,
        BattleActionRelationDescriptor relation,
        bool expectedVisible
    )
    {
        return TryGetVisual(adapter, relation, out RecordedVisual visual) &&
            object.ReferenceEquals(visual.Descriptor, relation) &&
            visual.Visible == expectedVisible;
    }

    private static bool IsVisible(
        RecordingVisualAdapter adapter,
        BattleActionRelationDescriptor relation
    )
    {
        return TryGetVisual(adapter, relation, out RecordedVisual visual) &&
            visual.Visible;
    }

    private static bool HasResolvedTargetAnchor(
        RecordingVisualAdapter adapter,
        BattleActionRelationDescriptor relation
    )
    {
        return TryGetVisual(adapter, relation, out RecordedVisual visual) &&
            visual.TargetAnchorAvailable;
    }

    private static bool TryGetVisual(
        RecordingVisualAdapter adapter,
        BattleActionRelationDescriptor relation,
        out RecordedVisual visual
    )
    {
        visual = null;
        return adapter != null && relation != null &&
            adapter.TryGet(relation.RelationID, out visual);
    }

    private static BattleEnemyIntent CreateIntent(
        CharacterData enemy,
        BattleCardState cardState,
        CharacterData target,
        int targetSlotIndex,
        int enemySlotIndex
    )
    {
        return new BattleEnemyIntent(
            "mode100_intent_" + enemySlotIndex + "_" +
                cardState.cardData.cardType,
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
        return new BattleCardState(
            owner,
            new CardTestData
            {
                cardID = id,
                cardName = id,
                cardType = cardType,
                minPoint = 1,
                maxPoint = 1,
                cooldown = 0,
                damageFormula = "PointAsDamage"
            },
            id + "_state"
        );
    }

    private static void DestroyFixture(Fixture fixture)
    {
        if (fixture == null)
        {
            return;
        }
        for (int index = 0; index < fixture.createdObjects.Count; index++)
        {
            if (fixture.createdObjects[index] != null)
            {
                Object.DestroyImmediate(fixture.createdObjects[index]);
            }
        }
    }
}
