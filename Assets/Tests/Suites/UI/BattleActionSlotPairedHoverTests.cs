using System.Collections.Generic;
using UnityEngine;

public static class BattleActionSlotPairedHoverTests
{
    public static bool Run()
    {
        bool[] results =
        {
            VerifyFinalResponseLookup(),
            VerifyUnopposedLookup(),
            VerifyAllyPartnerPayload(),
            VerifyEnemyPartnerPayload()
        };
        string[] names =
        {
            "A final response lookup returns the replacement responder",
            "B unopposed intent has no paired detail",
            "C Ally response exposes Enemy partner payload",
            "D Enemy response exposes final Ally partner payload"
        };

        bool passed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "Mode105 Paired Action Slot Hover " + names[index] + ": " +
                results[index]
            );
            passed &= results[index];
        }

        return passed;
    }

    static bool VerifyFinalResponseLookup()
    {
        TestContext context = CreateRespondedContext();
        BattleActionSlot resolved;
        bool result =
            BattleActionSlotManager.TryFindCurrentResponseSlot(
                context.runtimeState,
                context.intent,
                out resolved
            ) &&
            object.ReferenceEquals(resolved, context.laterSlot) &&
            !object.ReferenceEquals(resolved, context.earlierSlot);
        DestroyContext(context);
        return result;
    }

    static bool VerifyUnopposedLookup()
    {
        TestContext context = CreateRespondedContext();
        context.intent.ResetResponseState();
        BattleActionSlot resolved;
        bool result =
            !BattleActionSlotManager.TryFindCurrentResponseSlot(
                context.runtimeState,
                context.intent,
                out resolved
            ) &&
            resolved == null;
        DestroyContext(context);
        return result;
    }

    static bool VerifyAllyPartnerPayload()
    {
        TestContext context = CreateRespondedContext();
        GameObject allyObject = new GameObject("mode105_paired_ally");
        GameObject enemyObject = new GameObject("mode105_paired_enemy");
        BattleActionSlotUIView allyView =
            allyObject.AddComponent<BattleActionSlotUIView>();
        BattleActionSlotUIView enemyView =
            enemyObject.AddComponent<BattleActionSlotUIView>();
        allyView.BindInteraction(context.ally, 0, false, null, null);
        enemyView.BindInteraction(context.enemy, 0, true, null, null);
        allyView.SetBoundActionSlot(context.laterSlot);
        enemyView.SetBoundEnemyIntent(context.intent);

        BattleActionSlotCardInfoHoverRequest request =
            enemyView.BuildCardInfoPanelRequest(
                BattleActionSlotCardInfoPointerEvent.HoverEnter
            );
        bool result = request != null &&
            request.isEnemySide &&
            object.ReferenceEquals(request.cardState, context.enemyCard) &&
            object.ReferenceEquals(request.owner, context.enemy) &&
            object.ReferenceEquals(
                request.target,
                context.intent.originalTargetCharacter
            );
        Object.Destroy(allyObject);
        Object.Destroy(enemyObject);
        DestroyContext(context);
        return result;
    }

    static bool VerifyEnemyPartnerPayload()
    {
        TestContext context = CreateRespondedContext();
        GameObject allyObject = new GameObject("mode105_paired_ally");
        BattleActionSlotUIView allyView =
            allyObject.AddComponent<BattleActionSlotUIView>();
        allyView.BindInteraction(context.ally, 0, false, null, null);
        allyView.SetBoundActionSlot(context.laterSlot);

        BattleActionSlotCardInfoHoverRequest request =
            allyView.BuildCardInfoPanelRequest(
                BattleActionSlotCardInfoPointerEvent.HoverEnter
            );
        bool result = request != null &&
            !request.isEnemySide &&
            object.ReferenceEquals(request.cardState, context.allyCardLater) &&
            object.ReferenceEquals(request.owner, context.ally) &&
            object.ReferenceEquals(request.target, context.enemy);
        Object.Destroy(allyObject);
        DestroyContext(context);
        return result;
    }

    static TestContext CreateRespondedContext()
    {
        TestContext context = new TestContext
        {
            ally = new CharacterData("mode105_paired_ally", 30, 6, 6),
            enemy = new CharacterData("mode105_paired_enemy", 30, 5, 5),
            runtimeState = new BattleRuntimeState()
        };
        context.runtimeState.SetCharacters(
            context.ally,
            null,
            context.enemy
        );
        context.enemyCard = CreateCard(
            context.enemy,
            "mode105_paired_enemy_card"
        );
        context.allyCardEarlier = CreateCard(
            context.ally,
            "mode105_paired_ally_earlier"
        );
        context.allyCardLater = CreateCard(
            context.ally,
            "mode105_paired_ally_later"
        );
        context.intent = new BattleEnemyIntent(
            "mode105_paired_intent",
            context.enemy,
            context.enemyCard,
            context.ally,
            1,
            1,
            1
        );
        context.earlierSlot = new BattleActionSlot(context.ally, 1);
        context.earlierSlot.AssignResponse(
            context.ally,
            context.allyCardEarlier,
            context.intent,
            false
        );
        context.earlierSlot.assignmentSequence = 1;
        context.laterSlot = new BattleActionSlot(context.ally, 2);
        context.laterSlot.AssignResponse(
            context.ally,
            context.allyCardLater,
            context.intent,
            false
        );
        context.laterSlot.assignmentSequence = 2;
        context.runtimeState.actionSlots = new List<BattleActionSlot>
        {
            context.earlierSlot,
            context.laterSlot
        };
        context.runtimeState.intentQueue.Add(context.intent);
        BattleActionSlotManager.RebuildPreparedActionRoles(
            context.runtimeState
        );
        return context;
    }

    static BattleCardState CreateCard(
        CharacterData owner,
        string instanceID
    )
    {
        return new BattleCardState(
            owner,
            new CardTestData
            {
                cardID = instanceID,
                cardName = instanceID,
                cardType = "Attack"
            },
            instanceID
        );
    }

    static void DestroyContext(TestContext context)
    {
        if (context == null)
        {
            return;
        }

        context.runtimeState.SetActionSlots(
            new List<BattleActionSlot>()
        );
    }

    sealed class TestContext
    {
        public CharacterData ally;
        public CharacterData enemy;
        public BattleRuntimeState runtimeState;
        public BattleCardState enemyCard;
        public BattleCardState allyCardEarlier;
        public BattleCardState allyCardLater;
        public BattleEnemyIntent intent;
        public BattleActionSlot earlierSlot;
        public BattleActionSlot laterSlot;
    }
}
