using System.Collections.Generic;

// Development encounter setup only. It does not roll dice, advance turns or drive animation.
public sealed class BattleCombatDicePrototypeSetup
{
    BattleRuntimeState state;
    BattleCardState enemyCard;

    public bool Prepare(BattleRuntimeState runtimeState, out string failure)
    {
        failure = string.Empty;
        var cards = CardDataLoader.LoadCardData();
        CardTestData template = CardDataLoader.FindCardByID(cards, CardCombatDiceRules.PrototypeCardID);
        if (runtimeState?.allyA == null || runtimeState.enemy == null || template == null ||
            runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare || runtimeState.currentExecutionPlan != null)
        {
            failure = "骰子测试需要 Prepare 阶段、玩家/敌人和有效测试卡数据。";
            return false;
        }
        state = runtimeState;
        CharacterData ally = state.allyA;
        CharacterData enemy = state.enemy;
        state.SetCharacters(ally, null, enemy);
        ally.battleCards.Clear();
        enemy.battleCards.Clear();
        BattleCardState testCard = BattleCardManager.CreateBattleCard(ally, template, template.cardID + "_instance");
        enemyCard = BattleCardManager.CreateBattleCard(enemy, new CardTestData
        {
            cardID = "[TEST]_DICE_OPPONENT", cardName = "原型对手攻击",
            cardType = CardType.Attack, attackDeliveryMode = AttackDeliveryMode.Melee,
            rarity = CardRarity.White, isClashable = true, minPoint = 5, maxPoint = 5,
            damageFormula = "PointAsDamage", cooldown = 0, effects = new List<CardEffectData>()
        }, "[TEST]_DICE_OPPONENT_INSTANCE");
        state.SetActionSlots(BattleActionSlotManager.CreateCharacterActionSlots(ally, 2));
        if (!CreateIntents(state.currentTurn, out List<BattleEnemyIntent> intents, out failure)) return false;
        state.SetIntentQueue(intents);
        if (!BattleActionSlotManager.TryAssignToEnemyIntent(state, ally, 1, testCard, intents[0],
                out BattleActionAssignmentResult result))
        {
            failure = result != null ? result.message : "无法安排测试卡。";
            return false;
        }
        return true;
    }

    public bool CreateIntents(int turn, out List<BattleEnemyIntent> intents, out string failure)
    {
        intents = null;
        failure = string.Empty;
        if (state == null || enemyCard == null)
        {
            failure = "骰子原型尚未初始化。";
            return false;
        }
        intents = new List<BattleEnemyIntent>();
        if (!state.enemy.IsDefeated() && !state.allyA.IsDefeated())
            intents.Add(new BattleEnemyIntent("[TEST]_DICE_" + turn, state.enemy, enemyCard,
                state.allyA, 1, 1, 1));
        return true;
    }
}
