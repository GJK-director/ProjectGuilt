using System;
using System.Collections;
using TMPro;
using UnityEngine;

// Opt-in adapter around the existing slash/shoot player and camera. Rules stay in Resolver.
public sealed class BattleCombatDicePrototypePresenter : MonoBehaviour
{
    [SerializeField, Min(0f)] float rollReadDuration = 0.4f;
    [SerializeField, Range(0f, 1f)] float secondKnockbackScale = 0.2f;
    [Header("Clash Resolution (visual only)")]
    [SerializeField, Min(0f)] float clashHitStopDuration = 0.05f;
    [SerializeField, Min(0f)] float clashPauseDuration = 0.15f;
    [SerializeField, Min(0f)] float clashRecoilDistance = 0.2f;
    [SerializeField, Range(0f, 1f)] float clashShakeScale = 0.5f;
    [SerializeField, Range(0f, 1f), Tooltip("0 = attacker, 1 = opponent; capture once at weapon contact.")]
    float clashContactBias = 0.65f;
    [SerializeField, Tooltip("World units from the contact point; X follows attack direction. Does not move the normal guard FX.")]
    Vector2 clashFxOffset = new Vector2(0f, 2.458f);
    BattleAttackVsAttackPresentationPlayer player;
    BattleAttackVsGuardPresentationProfile guardProfile;
    BattlePerfectGuardFxPlayer clashFx;
    bool clashFxPlaying;
    bool clashFeedbackActive;
    BattleCameraDirector cameraDirector;
    BattleCharacterPresentationController attacker;
    BattleCharacterPresentationController target;
    Transform targetRoot;
    BattleCombatDiceHUD hud;
    TMP_FontAsset font;
    Coroutine sequence;
    BattlePresentationCompletion pending;
    BattlePresentationRequest request;
    int version;
    bool attackFinished = true;
    float direction;
    int hitCount;

    public void Present(BattlePresentationRequest next, BattlePresentationCompletion completion,
        BattleAttackVsAttackPresentationPlayer attackPlayer, BattleAttackVsGuardPresentationProfile guard,
        BattleCameraDirector director,
        BattleUnitViewHandle source, BattleUnitViewHandle victim, Action actionComplete)
    {
        if (sequence != null) StopCoroutine(sequence);
        sequence = null;
        request = next;
        pending = completion;
        player = attackPlayer;
        guardProfile = guard;
        cameraDirector = director;
        attacker = source?.PresentationController;
        target = victim?.PresentationController;
        targetRoot = victim?.WorldRoot != null ? victim.WorldRoot.transform : null;
        if (player == null || player.NormalHitProfile == null || director == null ||
            attacker == null || target == null || targetRoot == null || source.WorldRoot == null)
        {
            Fail("缺少现有攻击 Player、角色绑定或 Camera。", completion);
            return;
        }
        direction = targetRoot.position.x >= source.WorldRoot.transform.position.x ? 1f : -1f;
        var sourceText = source.StatusUIRoot != null ? source.StatusUIRoot.GetComponentInChildren<TMP_Text>(true) : null;
        font = sourceText != null ? sourceText.font : TMP_Settings.defaultFontAsset;
        if (hud == null) hud = gameObject.AddComponent<BattleCombatDiceHUD>();
        // Tail requests must not invalidate the previous player's onFinished callback.
        int current = (next.Cue == BattlePresentationCue.ClashWin || next.Cue == BattlePresentationCue.Impact)
            ? ++version : version;
        if (next.Cue == BattlePresentationCue.ClashWin)
        {
            hitCount = 0;
            sequence = StartCoroutine(PlayClashWin(current, completion));
        }
        else if (next.Cue == BattlePresentationCue.Impact)
            sequence = StartCoroutine(PlayDie(current, completion));
        else
            sequence = StartCoroutine(FinishDie(current, completion, actionComplete));
    }

    IEnumerator PlayClashWin(int current, BattlePresentationCompletion completion)
    {
        if (guardProfile == null || guardProfile.PerfectGuardFxSprite == null ||
            guardProfile.MeleeGuardReactionProfile == null)
        {
            Fail("拼点碰撞缺少现有防御特效或受力 Profile。", completion);
            yield break;
        }
        hud.Show("CLASH RESOLUTION\n拼点胜利碰撞\n不造成伤害", new Color(0.4f, 0.8f, 1f), font);
        attacker.ClearSlashEffect();
        attacker.SetSlash();
        // The existing slash contact marker starts visual feedback only.
        // No combat Impact completion is wired to that marker.
        bool collisionStarted = false;
        yield return attacker.PlaySlashPresentation(direction,
            () => collisionStarted = StartClashCollision(current), playAttackEffect: false);
        if (current != version) yield break;
        if (!collisionStarted)
        {
            Fail("无法播放现有防御碰撞特效。", completion);
            yield break;
        }
        yield return new WaitForSecondsRealtime(clashHitStopDuration);
        if (current != version) yield break;
        attacker.SetPresentationPaused(false);
        target.SetPresentationPaused(false);

        BattleHitPresentationProfile reaction = guardProfile.MeleeGuardReactionProfile;
        float profileDistance = reaction.ImpactBurstDistance + reaction.FollowKnockbackDistance;
        float recoilScale = profileDistance > Mathf.Epsilon ? clashRecoilDistance / profileDistance : 0f;
        // Reuse pose/motion only: no red tint, blood FX, event, HP or damage number.
        yield return target.PlaySustainedHitReaction(targetRoot, direction, reaction,
            reaction.FollowKnockbackDistance, false, recoilScale);
        if (current != version) yield break;
        while (clashFxPlaying || cameraDirector.IsHitFeedbackPlaying)
        {
            if (current != version) yield break;
            yield return null;
        }
        attacker.FinishSlashPresentation();
        yield return new WaitForSecondsRealtime(clashPauseDuration);
        clashFeedbackActive = false;
        // Only now may Runner roll Damage Die 1. Target keeps its small recoil offset.
        if (current == version) Complete(completion);
    }

    bool StartClashCollision(int current)
    {
        if (current != version || attacker == null || target == null) return false;
        Vector3 contact = Vector3.Lerp(attacker.transform.position, target.transform.position, clashContactBias);
        contact += new Vector3(direction * clashFxOffset.x, clashFxOffset.y, 0f);
        clashFeedbackActive = true;
        target.SetHit();
        attacker.SetPresentationPaused(true);
        target.SetPresentationPaused(true);
        clashFxPlaying = true;
        if (!BattlePerfectGuardFxPlayer.TrySpawnAtWorldPosition(guardProfile, target, direction,
                contact, out clashFx,
                () => { if (current == version) { clashFxPlaying = false; clashFx = null; } },
                () => this != null && isActiveAndEnabled && current == version)) return false;
        cameraDirector.TryPlayImpactShake(guardProfile.MeleeGuardReactionProfile, clashShakeScale);
        return true;
    }

    IEnumerator PlayDie(int current, BattlePresentationCompletion completion)
    {
        BattleImpact impact = request.Impact;
        if (impact == null || !impact.damageDieRolled)
        {
            Fail("表现请求缺少规则层已保存的伤害骰结果。", completion);
            yield break;
        }
        ShowDie(impact);
        yield return new WaitForSecondsRealtime(rollReadDuration);
        if (current != version) yield break;
        bool shoot = impact.damageDie.presentation == CardCombatDiceRules.CloseRangeShoot;
        attackFinished = false;
        bool started = player.TryPlayDamageDie(attacker, target, targetRoot, direction,
            shoot, shoot ? secondKnockbackScale : 1f,
            () =>
            {
                if (current != version) return;
                // Both start at the camera frame retained by the previous stage.
                // Shooting keeps the existing close-range push-in/kick instead of scaling it with knockback.
                if (shoot) cameraDirector.TryPlayGenericHitImpact(direction);
                else cameraDirector.TryPlayNormalHitImpact(targetRoot, direction, player.NormalHitActiveDuration);
                cameraDirector.TryPlayImpactShake(player.NormalHitProfile);
            },
            () => { if (current == version) Complete(completion); },
            () => { if (current == version) attackFinished = true; });
        if (!started) Fail("上一段尚未结束或现有攻击 Player 无法播放。", completion);
    }

    IEnumerator FinishDie(int current, BattlePresentationCompletion completion, Action actionComplete)
    {
        while (!attackFinished || cameraDirector.IsHitFeedbackPlaying)
        {
            if (current != version) yield break;
            if (player == null || !player.isActiveAndEnabled)
            {
                Fail("攻击 Player 在表现收尾期间失效。", completion);
                yield break;
            }
            yield return null;
        }
        if (current != version) yield break;
        if (actionComplete != null)
        {
            hud.Hide();
            pending = null;
            actionComplete();
        }
        else Complete(completion);
    }

    public void ObserveImpact(BattleImpact impact)
    {
        if (impact?.damageDie == null || impact.state != BattleImpactState.Committed ||
            request?.ResolutionPlan == null || !request.ResolutionPlan.impacts.Contains(impact)) return;
        if (impact.didHit) hitCount++;
        ShowDie(impact);
    }

    void ShowDie(BattleImpact impact)
    {
        hud.Show("Damage " + (impact.impactIndex + 1) + " / " + request.ResolutionPlan.impacts.Count +
            "\nRoll " + impact.damageDieRoll +
            "\n" + (impact.damageDie.presentation == CardCombatDiceRules.Melee ? "近战" : "射击") +
            "  |  Hit " + hitCount, new Color(1f, 0.73f, 0.4f), font);
    }

    void Complete(BattlePresentationCompletion completion)
    {
        completion.TryComplete(completion.RequestId);
        if (ReferenceEquals(pending, completion)) pending = null;
    }

    void Fail(string message, BattlePresentationCompletion completion)
    {
        Debug.LogError("[CombatDice Prototype] " + message, this);
        completion.TryCancel(completion.RequestId);
        Cancel();
    }

    public void Cancel()
    {
        version++;
        if (sequence != null) StopCoroutine(sequence);
        sequence = null;
        clashFx?.Cancel();
        clashFx = null;
        clashFxPlaying = false;
        if (clashFeedbackActive) cameraDirector?.CancelImpactShake();
        clashFeedbackActive = false;
        pending?.TryCancel(pending.RequestId);
        pending = null;
        player?.CancelAndReset();
        attacker?.ResetToStableIdlePresentation();
        target?.ResetToStableIdlePresentation();
        hud?.Hide();
        attackFinished = true;
        request = null;
    }

    void OnDisable() => Cancel();
}
