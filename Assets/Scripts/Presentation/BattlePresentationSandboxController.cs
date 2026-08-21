using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 仅用于 BattlePresentationSandbox 的手动姿态切换测试。
public sealed class BattlePresentationSandboxController : MonoBehaviour
{
    [SerializeField] private BattleCharacterPresentationController character;
    [SerializeField] private BattleCharacterPresentationController defender;
    [SerializeField]
    private BattleAttackVsAttackPresentationPlayer attackVsAttackPresentationPlayer;
    [SerializeField]
    private BattleAttackVsGuardPresentationPlayer attackVsGuardPresentationPlayer;
    [SerializeField] private BattleClashEngagementProfile clashEngagementProfile;
    [SerializeField, Min(0f)] private float characterTestSpeed = 5f;
    [SerializeField, Min(0f)] private float defenderTestSpeed = 5f;
    [SerializeField] private float engagementGap = 1.0f;
    [SerializeField] private float sprintDistance = 2f;
    [SerializeField] private float sprintDuration = 0.35f;
    [SerializeField] private float slashHoldDuration = 0.2f;
    [SerializeField] private float afterimageSpawnInterval = 0.08f;
    [SerializeField] private float hitStopDuration = 0.08f;
    [SerializeField, Min(0f)] private float shootAimHoldDuration = 0.2f;

    private Coroutine dynamicTestCoroutine;
    private Coroutine hitStopCoroutine;
    private Coroutine dualHitStopCoroutine;
    private Coroutine loserHitCoroutine;
    private bool waitingForManualRoll;
    private bool attackImpactHandled;
    private bool hasWarnedMissingDefender;
    private bool hasWarnedMissingAttackPair;
    private bool hasWarnedMissingSharedAttackPlayer;
    private bool hasWarnedMissingTiePair;
    private bool hasWarnedMissingGuardPair;
    private bool hasWarnedMissingSharedGuardPlayer;
    private Vector3 characterResetPosition;
    private bool hasCharacterResetPosition;

    void Awake()
    {
        CacheCharacterResetPosition();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (waitingForManualRoll && keyboard.spaceKey.wasPressedThisFrame)
        {
            // Space只解除当前ClashReady等待，不直接执行Slash逻辑。
            waitingForManualRoll = false;
        }

        if (character != null && keyboard.digit1Key.wasPressedThisFrame)
        {
            character.SetIdle();
        }
        else if (character != null && keyboard.digit2Key.wasPressedThisFrame)
        {
            character.SetSprint();
        }
        else if (character != null && keyboard.digit3Key.wasPressedThisFrame)
        {
            character.SetSlash();
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            StartSprintForward();
        }
        else if (keyboard.digit5Key.wasPressedThisFrame)
        {
            StartSprintSlash();
        }
        else if (keyboard.digit6Key.wasPressedThisFrame)
        {
            StartHitReaction();
        }
        else if (keyboard.digit7Key.wasPressedThisFrame)
        {
            StartCharacterAttackTest();
        }
        else if (keyboard.digit8Key.wasPressedThisFrame)
        {
            StartAttackVsAttackTest(false);
        }
        else if (keyboard.digit9Key.wasPressedThisFrame)
        {
            StartAttackVsAttackTest(true);
        }
        else if (keyboard.digit0Key.wasPressedThisFrame)
        {
            StartAttackTieLoopTest();
        }
        else if (keyboard.gKey.wasPressedThisFrame)
        {
            SetGuardPoseForInspection();
        }
        else if (keyboard.hKey.wasPressedThisFrame)
        {
            StartPerfectGuardTest();
        }
        else if (keyboard.jKey.wasPressedThisFrame)
        {
            StartPartialGuardTest();
        }
        else if (keyboard.kKey.wasPressedThisFrame)
        {
            StartDodgeMotionTest();
        }
        else if (keyboard.qKey.wasPressedThisFrame)
        {
            StartBasicShootSequenceTest();
        }
        else if (keyboard.rKey.wasPressedThisFrame)
        {
            ResetCharacterToTestStart();
        }
    }

    void OnDisable()
    {
        if (dualHitStopCoroutine != null)
        {
            StopCoroutine(dualHitStopCoroutine);
            dualHitStopCoroutine = null;
        }

        if (loserHitCoroutine != null)
        {
            StopCoroutine(loserHitCoroutine);
            loserHitCoroutine = null;
        }

        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = null;
        }

        if (dynamicTestCoroutine != null)
        {
            StopCoroutine(dynamicTestCoroutine);
            dynamicTestCoroutine = null;
        }

        // Sandbox停用时同步取消共享Player，避免留下暂停、特效或回调等待。
        attackVsAttackPresentationPlayer?.CancelAndReset();
        attackVsGuardPresentationPlayer?.CancelAndReset();

        waitingForManualRoll = false;
        attackImpactHandled = false;
        if (character != null)
        {
            character.SetPresentationPaused(false);
            character.FinishDodgePresentation();
            character.ClearBodyVisualOffsets();
            character.ClearAfterimages();
            character.ClearSlashEffect();
            character.ClearPerfectGuardEffect();
            character.SetIdle();
        }

        if (defender != null)
        {
            defender.ResetToStableIdlePresentation();
        }
    }

    private void StartSprintForward()
    {
        StartDynamicTest(false);
    }

    private void StartSprintSlash()
    {
        StartDynamicTest(true);
    }

    private void StartHitReaction()
    {
        if (character == null || dynamicTestCoroutine != null)
        {
            return;
        }

        dynamicTestCoroutine = StartCoroutine(RunHitReactionTest());
    }

    private void StartDodgeMotionTest()
    {
        if (character == null || dynamicTestCoroutine != null)
        {
            return;
        }

        dynamicTestCoroutine = StartCoroutine(RunDodgeMotionTest());
    }

    private void StartBasicShootSequenceTest()
    {
        if (character == null || dynamicTestCoroutine != null)
        {
            return;
        }

        dynamicTestCoroutine = StartCoroutine(RunBasicShootSequenceTest());
    }

    private IEnumerator RunBasicShootSequenceTest()
    {
        character.SetAim();
        float remainingAimHold = Mathf.Max(0f, shootAimHoldDuration);
        while (remainingAimHold > 0f)
        {
            if (character == null || !character.isActiveAndEnabled)
            {
                dynamicTestCoroutine = null;
                yield break;
            }

            remainingAimHold -= Time.deltaTime;
            yield return null;
        }

        if (character == null || !character.isActiveAndEnabled)
        {
            dynamicTestCoroutine = null;
            yield break;
        }

        character.SetShoot();
        bool flashFinished = false;
        character.PlayMuzzleFlash(() => flashFinished = true);

        // Flash自身拥有时序和关闭逻辑；Sandbox只等待正式完成回调。
        while (!flashFinished)
        {
            if (character == null || !character.isActiveAndEnabled)
            {
                dynamicTestCoroutine = null;
                yield break;
            }

            yield return null;
        }

        Debug.Log("[ShootSandbox] Aim -> Shoot + MuzzleFlash Complete");
        dynamicTestCoroutine = null;
    }

    private IEnumerator RunDodgeMotionTest()
    {
        character.SetDodge();
        bool finished = false;
        bool started = character.PlayDodgeMotion(1f, () => finished = true);
        if (!started)
        {
            character.SetIdle();
            dynamicTestCoroutine = null;
            yield break;
        }

        // Sandbox只组合Pose与可复用Local Motion，不直接操作VisualRoot。
        while (!finished && character != null && character.isActiveAndEnabled)
        {
            yield return null;
        }

        // 正常完成只清除Local Motion，保留最后明确设置的Dodge Pose。
        dynamicTestCoroutine = null;
    }

    private void SetGuardPoseForInspection()
    {
        BattleCharacterPresentationController guardCharacter =
            defender != null ? defender : character;
        if (guardCharacter != null)
        {
            guardCharacter.SetGuard();
        }
    }

    private void StartPerfectGuardTest()
    {
        StartSharedGuardTest(BattleGuardPresentationResult.FullBlock);
    }

    private void StartPartialGuardTest()
    {
        StartSharedGuardTest(BattleGuardPresentationResult.ReducedDamage);
    }

    private void StartSharedGuardTest(BattleGuardPresentationResult result)
    {
        if (character == null || defender == null)
        {
            if (!hasWarnedMissingGuardPair)
            {
                Debug.LogWarning(
                    "BattlePresentationSandboxController：按键H/J需要同时绑定Character和Defender。"
                );
                hasWarnedMissingGuardPair = true;
            }
            return;
        }

        hasWarnedMissingGuardPair = false;
        if (attackVsGuardPresentationPlayer == null)
        {
            if (!hasWarnedMissingSharedGuardPlayer)
            {
                Debug.LogWarning(
                    "BattlePresentationSandboxController：按键H/J缺少共享AttackVsGuard Player。"
                );
                hasWarnedMissingSharedGuardPlayer = true;
            }
            return;
        }

        hasWarnedMissingSharedGuardPlayer = false;
        if (dynamicTestCoroutine != null ||
            attackVsGuardPresentationPlayer.IsRunning)
        {
            return;
        }

        dynamicTestCoroutine = StartCoroutine(RunSharedGuardTest(result));
    }

    private IEnumerator RunSharedGuardTest(BattleGuardPresentationResult result)
    {
        float attackDirectionSign = GetHorizontalDirectionSign(
            defender.transform.position.x - character.transform.position.x
        );
        bool finished = false;
        bool started = attackVsGuardPresentationPlayer.TryPlayGuardImpact(
            character,
            defender,
            attackDirectionSign,
            result,
            null,
            () => finished = true
        );
        if (!started)
        {
            dynamicTestCoroutine = null;
            yield break;
        }

        // Sandbox只维持测试互斥锁，具体Guard时序完全由共享Player拥有。
        while (!finished && attackVsGuardPresentationPlayer != null &&
            attackVsGuardPresentationPlayer.IsRunning)
        {
            yield return null;
        }

        dynamicTestCoroutine = null;
    }

    private IEnumerator RunHitReactionTest()
    {
        yield return character.PlayHitReaction(character.transform, -1f);
        dynamicTestCoroutine = null;
    }

    private void StartCharacterAttackTest()
    {
        if (defender == null)
        {
            if (!hasWarnedMissingDefender)
            {
                Debug.LogWarning(
                    "BattlePresentationSandboxController：按键7需要绑定Defender。"
                );
                hasWarnedMissingDefender = true;
            }
            return;
        }

        hasWarnedMissingDefender = false;
        if (character == null || dynamicTestCoroutine != null)
        {
            return;
        }

        attackImpactHandled = false;
        dynamicTestCoroutine = StartCoroutine(RunCharacterAttackTest());
    }

    private void StartAttackVsAttackTest(bool defenderWins)
    {
        if (character == null || defender == null)
        {
            if (!hasWarnedMissingAttackPair)
            {
                Debug.LogWarning(
                    "BattlePresentationSandboxController：按键8/9需要同时绑定Character和Defender。"
                );
                hasWarnedMissingAttackPair = true;
            }
            return;
        }

        hasWarnedMissingAttackPair = false;
        if (attackVsAttackPresentationPlayer == null ||
            !attackVsAttackPresentationPlayer.isActiveAndEnabled)
        {
            if (!hasWarnedMissingSharedAttackPlayer)
            {
                Debug.LogWarning(
                    "BattlePresentationSandboxController：按键8/9需要绑定并启用共享AttackVsAttack Player。"
                );
                hasWarnedMissingSharedAttackPlayer = true;
            }
            return;
        }

        hasWarnedMissingSharedAttackPlayer = false;
        if (dynamicTestCoroutine != null)
        {
            return;
        }

        attackImpactHandled = false;
        dynamicTestCoroutine = StartCoroutine(
            RunAttackVsAttackTest(defenderWins)
        );
    }

    private void StartAttackTieLoopTest()
    {
        if (character == null || defender == null)
        {
            if (!hasWarnedMissingTiePair)
            {
                Debug.LogWarning(
                    "BattlePresentationSandboxController：按键0需要同时绑定Character和Defender。"
                );
                hasWarnedMissingTiePair = true;
            }
            return;
        }

        hasWarnedMissingTiePair = false;
        if (attackVsAttackPresentationPlayer == null ||
            !attackVsAttackPresentationPlayer.isActiveAndEnabled)
        {
            if (!hasWarnedMissingSharedAttackPlayer)
            {
                Debug.LogWarning(
                    "BattlePresentationSandboxController：按键0需要绑定并启用共享AttackVsAttack Player。"
                );
                hasWarnedMissingSharedAttackPlayer = true;
            }
            return;
        }

        hasWarnedMissingSharedAttackPlayer = false;
        if (dynamicTestCoroutine != null)
        {
            return;
        }

        dynamicTestCoroutine = StartCoroutine(RunAttackTieLoopTest());
    }

    private void StartDynamicTest(bool playSlashAfterSprint)
    {
        if (character == null || dynamicTestCoroutine != null)
        {
            return;
        }

        Vector3 startPosition = character.transform.position;
        Vector3 targetPosition = startPosition + Vector3.right * sprintDistance;
        character.SetSprint();

        if (sprintDuration <= 0f)
        {
            character.transform.position = targetPosition;
            if (!playSlashAfterSprint)
            {
                character.SetIdle();
                return;
            }

            dynamicTestCoroutine = StartCoroutine(
                WaitForManualRollAndSlash()
            );
            return;
        }

        dynamicTestCoroutine = StartCoroutine(
            RunSprintTest(
                startPosition,
                targetPosition,
                playSlashAfterSprint
            )
        );
    }

    private IEnumerator RunSprintTest(
        Vector3 startPosition,
        Vector3 targetPosition,
        bool playSlashAfterSprint
    )
    {
        yield return RunSprintMovement(startPosition, targetPosition);

        if (character == null)
        {
            waitingForManualRoll = false;
            dynamicTestCoroutine = null;
            yield break;
        }

        if (!playSlashAfterSprint)
        {
            character.SetIdle();
            dynamicTestCoroutine = null;
            yield break;
        }

        yield return WaitForManualRollAndSlash();
    }

    private void CacheCharacterResetPosition()
    {
        if (character == null || hasCharacterResetPosition)
        {
            return;
        }

        characterResetPosition = character.transform.position;
        hasCharacterResetPosition = true;
    }

    private void ResetCharacterToTestStart()
    {
        if (character == null || dynamicTestCoroutine != null)
        {
            return;
        }

        CacheCharacterResetPosition();
        if (!hasCharacterResetPosition)
        {
            return;
        }

        attackVsAttackPresentationPlayer?.CancelAndReset();
        character.transform.position = characterResetPosition;
        character.FinishDodgePresentation();
        character.ClearAfterimages();
        character.SetIdle();
    }

    private IEnumerator RunSprintMovement(
        Vector3 startPosition,
        Vector3 targetPosition
    )
    {
        if (character == null)
        {
            yield break;
        }

        if (sprintDuration <= 0f)
        {
            character.transform.position = targetPosition;
            yield break;
        }

        float elapsed = 0f;
        float afterimageElapsed = 0f;
        bool spawnRepeatedAfterimages = afterimageSpawnInterval > 0f;
        bool hasWorldMovement =
            (targetPosition - startPosition).sqrMagnitude > Mathf.Epsilon;
        if (hasWorldMovement)
        {
            character.SpawnAfterimage();
        }

        while (elapsed < sprintDuration)
        {
            if (character == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(elapsed / sprintDuration);
            float easedT = BattlePresentationEasing.EaseOutQuad(linearT);
            character.transform.position = startPosition +
                (targetPosition - startPosition) * easedT;

            if (spawnRepeatedAfterimages && hasWorldMovement)
            {
                afterimageElapsed += Time.deltaTime;
                if (afterimageElapsed >= afterimageSpawnInterval &&
                    elapsed < sprintDuration)
                {
                    character.SpawnAfterimage();
                    afterimageElapsed = 0f;
                }
            }
            yield return null;
        }

        if (character == null)
        {
            yield break;
        }

        character.transform.position = targetPosition;
    }

    private IEnumerator WaitForManualRollAndSlash()
    {
        yield return WaitForManualRoll();
        if (character == null)
        {
            dynamicTestCoroutine = null;
            yield break;
        }

        character.SetSlash();
        float slashStartedAt = Time.time;
        yield return character.PlaySlashPresentation(1f, BeginHitStop);

        float remainingSlashHold = slashHoldDuration -
            (Time.time - slashStartedAt);
        while (remainingSlashHold > 0f)
        {
            if (character == null)
            {
                waitingForManualRoll = false;
                dynamicTestCoroutine = null;
                yield break;
            }

            remainingSlashHold -= Time.deltaTime;
            yield return null;
        }

        if (character != null)
        {
            character.ClearSlashEffect();
            character.FinishSlashPresentation();
        }

        waitingForManualRoll = false;
        // 清除统一运行锁，允许再次触发其他动态测试。
        dynamicTestCoroutine = null;
    }

    private IEnumerator WaitForManualRoll()
    {
        // ClashReady阶段暂时保持Sprint Pose，只暂停当前测试协程。
        waitingForManualRoll = true;
        while (waitingForManualRoll)
        {
            if (character == null)
            {
                waitingForManualRoll = false;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator RunCharacterAttackTest()
    {
        if (character == null || defender == null)
        {
            FinishAttackTest(character, defender);
            yield break;
        }

        Vector3 startPosition = character.transform.position;
        Vector3 defenderPosition = defender.transform.position;
        float horizontalDelta = defenderPosition.x - startPosition.x;
        float attackDirectionSign = Mathf.Abs(horizontalDelta) <= 0.0001f
            ? 1f
            : Mathf.Sign(horizontalDelta);
        float safeEngagementGap = Mathf.Max(0f, engagementGap);
        float horizontalDistance = Mathf.Abs(horizontalDelta);

        character.SetSprint();
        if (horizontalDistance > safeEngagementGap)
        {
            Vector3 engagementPosition = startPosition;
            engagementPosition.x = defenderPosition.x -
                attackDirectionSign * safeEngagementGap;
            yield return RunSprintMovement(
                startPosition,
                engagementPosition
            );
        }

        if (character == null || defender == null)
        {
            FinishAttackTest(character, defender);
            yield break;
        }

        yield return WaitForManualRoll();
        if (character == null || defender == null)
        {
            FinishAttackTest(character, defender);
            yield break;
        }

        yield return RunWinnerAttackPresentation(
            character,
            defender,
            attackDirectionSign
        );
    }

    private IEnumerator RunAttackVsAttackTest(bool defenderWins)
    {
        if (character == null || defender == null ||
            attackVsAttackPresentationPlayer == null)
        {
            FinishSharedAttackVsAttackTest();
            yield break;
        }

        float horizontalDelta =
            defender.transform.position.x - character.transform.position.x;
        float directionSign = GetHorizontalDirectionSign(horizontalDelta);
        BattleClashEngagementResult engagement =
            BattleClashEngagementResolver.Resolve(
                clashEngagementProfile,
                character.PresentationKey,
                defender.PresentationKey,
                characterTestSpeed,
                defenderTestSpeed
            );
        if (engagement == null)
        {
            Debug.LogWarning(
                "BattlePresentationSandboxController：按键8/9无法解析Clash Engagement。"
            );
            FinishSharedAttackVsAttackTest();
            yield break;
        }

        bool approachFinished = false;
        bool approachStarted = attackVsAttackPresentationPlayer
            .TryPlayClashReadyApproach(
                character,
                character.transform,
                defender,
                defender.transform,
                engagement,
                () => approachFinished = true
            );
        if (!approachStarted)
        {
            Debug.LogWarning(
                "BattlePresentationSandboxController：共享AttackVsAttack Approach启动失败。"
            );
            FinishSharedAttackVsAttackTest();
            yield break;
        }

        while (!approachFinished)
        {
            if (attackVsAttackPresentationPlayer == null)
            {
                FinishSharedAttackVsAttackTest();
                yield break;
            }

            yield return null;
        }

        if (character == null || defender == null)
        {
            FinishSharedAttackVsAttackTest();
            yield break;
        }

        yield return WaitForManualRoll();
        if (character == null || defender == null)
        {
            FinishSharedAttackVsAttackTest();
            yield break;
        }

        BattleCharacterPresentationController winner = defenderWins
            ? defender
            : character;
        BattleCharacterPresentationController loser = defenderWins
            ? character
            : defender;
        float attackDirectionSign = defenderWins
            ? -directionSign
            : directionSign;

        bool resolvedAttackFinished = false;
        bool resolvedAttackStarted = attackVsAttackPresentationPlayer
            .TryPlayResolvedWinnerAttack(
                winner,
                loser,
                loser.transform,
                attackDirectionSign,
                () => attackImpactHandled = true,
                () => resolvedAttackFinished = true
            );
        if (!resolvedAttackStarted)
        {
            Debug.LogWarning(
                "BattlePresentationSandboxController：共享AttackVsAttack胜者攻击启动失败。"
            );
            FinishSharedAttackVsAttackTest();
            yield break;
        }

        while (!resolvedAttackFinished)
        {
            if (attackVsAttackPresentationPlayer == null)
            {
                FinishSharedAttackVsAttackTest();
                yield break;
            }

            yield return null;
        }

        FinishSharedAttackVsAttackTest();
    }

    private void FinishSharedAttackVsAttackTest()
    {
        // Player负责视觉复位；Sandbox只释放测试输入状态与互斥锁。
        waitingForManualRoll = false;
        attackImpactHandled = false;
        dynamicTestCoroutine = null;
    }

    private IEnumerator RunAttackTieLoopTest()
    {
        if (character == null || defender == null ||
            attackVsAttackPresentationPlayer == null)
        {
            FinishTieLoopAfterAbort();
            yield break;
        }

        BattleClashEngagementResult engagement =
            BattleClashEngagementResolver.Resolve(
                clashEngagementProfile,
                character.PresentationKey,
                defender.PresentationKey,
                characterTestSpeed,
                defenderTestSpeed
            );
        if (engagement == null)
        {
            FinishTieLoopAfterAbort();
            yield break;
        }

        bool approachFinished = false;
        bool approachStarted = attackVsAttackPresentationPlayer
            .TryPlayClashReadyApproach(
                character,
                character.transform,
                defender,
                defender.transform,
                engagement,
                () => approachFinished = true
            );
        if (!approachStarted)
        {
            FinishTieLoopAfterAbort();
            yield break;
        }

        while (!approachFinished)
        {
            if (character == null || defender == null ||
                attackVsAttackPresentationPlayer == null)
            {
                FinishTieLoopAfterAbort();
                yield break;
            }

            yield return null;
        }

        while (character != null && defender != null)
        {
            yield return WaitForManualRollForPair();
            if (character == null || defender == null ||
                attackVsAttackPresentationPlayer == null)
            {
                break;
            }

            bool tieFinished = false;
            bool tieStarted = attackVsAttackPresentationPlayer
                .TryPlayTieResult(
                    character,
                    character.transform,
                    defender,
                    defender.transform,
                    engagement,
                    () => tieFinished = true
                );
            if (!tieStarted)
            {
                break;
            }

            while (!tieFinished)
            {
                if (character == null || defender == null ||
                    attackVsAttackPresentationPlayer == null)
                {
                    FinishTieLoopAfterAbort();
                    yield break;
                }

                yield return null;
            }
        }

        FinishTieLoopAfterAbort();
    }

    private IEnumerator WaitForManualRollForPair()
    {
        waitingForManualRoll = true;
        while (waitingForManualRoll)
        {
            if (character == null || defender == null)
            {
                waitingForManualRoll = false;
                yield break;
            }

            yield return null;
        }
    }

    private void FinishTieLoopAfterAbort()
    {
        if (dualHitStopCoroutine != null)
        {
            StopCoroutine(dualHitStopCoroutine);
            dualHitStopCoroutine = null;
        }

        attackVsAttackPresentationPlayer?.CancelAndReset();

        if (character != null)
        {
            character.SetPresentationPaused(false);
            character.ClearSlashEffect();
            character.ClearBodyVisualOffsets();
            character.ClearAfterimages();
            character.SetIdle();
        }

        if (defender != null)
        {
            defender.SetPresentationPaused(false);
            defender.ClearSlashEffect();
            defender.ClearBodyVisualOffsets();
            defender.ClearAfterimages();
            defender.SetIdle();
        }

        waitingForManualRoll = false;
        dynamicTestCoroutine = null;
    }

    private IEnumerator RunWinnerAttackPresentation(
        BattleCharacterPresentationController winner,
        BattleCharacterPresentationController loser,
        float attackDirectionSign
    )
    {
        if (winner == null || loser == null)
        {
            FinishAttackTest(winner, loser);
            yield break;
        }

        winner.SetSlash();
        float slashStartedAt = Time.time;
        yield return winner.PlaySlashPresentation(
            attackDirectionSign,
            () => BeginAttackImpact(
                winner,
                loser,
                attackDirectionSign
            )
        );

        float remainingSlashHold = slashHoldDuration -
            (Time.time - slashStartedAt);
        while (remainingSlashHold > 0f)
        {
            if (winner == null || loser == null)
            {
                FinishAttackTest(winner, loser);
                yield break;
            }

            remainingSlashHold -= Time.deltaTime;
            yield return null;
        }

        FinishAttackTest(winner, loser);
    }

    private void BeginAttackImpact(
        BattleCharacterPresentationController winner,
        BattleCharacterPresentationController loser,
        float recoilDirectionSign
    )
    {
        if (attackImpactHandled)
        {
            return;
        }

        attackImpactHandled = true;
        if (winner == null || loser == null)
        {
            return;
        }

        // 先切换受击姿态，再冻结双方表现。
        loser.SetHit();
        loserHitCoroutine = StartCoroutine(
            RunLoserSustainedHit(loser, recoilDirectionSign)
        );

        if (hitStopDuration <= 0f || dualHitStopCoroutine != null)
        {
            return;
        }

        winner.SetPresentationPaused(true);
        loser.SetPresentationPaused(true);
        dualHitStopCoroutine = StartCoroutine(
            RunDualHitStop(winner, loser)
        );
    }

    private IEnumerator RunLoserSustainedHit(
        BattleCharacterPresentationController loser,
        float recoilDirectionSign
    )
    {
        if (loser != null)
        {
            yield return loser.PlaySustainedHitReaction(
                loser.transform,
                recoilDirectionSign
            );
        }

        loserHitCoroutine = null;
    }

    private IEnumerator RunDualHitStop(
        BattleCharacterPresentationController first,
        BattleCharacterPresentationController second
    )
    {
        float elapsed = 0f;
        while (elapsed < hitStopDuration)
        {
            if (first == null || second == null)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (first != null)
        {
            first.SetPresentationPaused(false);
        }

        if (second != null)
        {
            second.SetPresentationPaused(false);
        }

        dualHitStopCoroutine = null;
    }

    private void FinishAttackTest(
        BattleCharacterPresentationController winner,
        BattleCharacterPresentationController loser
    )
    {
        if (dualHitStopCoroutine != null)
        {
            StopCoroutine(dualHitStopCoroutine);
            dualHitStopCoroutine = null;
        }

        if (loserHitCoroutine != null)
        {
            StopCoroutine(loserHitCoroutine);
            loserHitCoroutine = null;
        }

        if (winner != null)
        {
            winner.SetPresentationPaused(false);
            winner.ClearSlashEffect();
        }

        if (loser != null)
        {
            loser.SetPresentationPaused(false);
        }

        // 两名角色在同一同步收尾中恢复，渲染前不会出现单方先复位。
        if (winner != null)
        {
            winner.FinishSlashPresentation();
        }

        if (loser != null)
        {
            loser.FinishHitReaction();
        }

        waitingForManualRoll = false;
        attackImpactHandled = false;
        dynamicTestCoroutine = null;
    }

    private void BeginHitStop()
    {
        if (character == null ||
            hitStopDuration <= 0f ||
            hitStopCoroutine != null)
        {
            return;
        }

        character.SetPresentationPaused(true);
        hitStopCoroutine = StartCoroutine(RunHitStop());
    }

    private IEnumerator RunHitStop()
    {
        float elapsed = 0f;
        while (elapsed < hitStopDuration)
        {
            if (character == null)
            {
                hitStopCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (character != null)
        {
            character.SetPresentationPaused(false);
        }

        hitStopCoroutine = null;
    }

    private static float GetHorizontalDirectionSign(float horizontalDelta)
    {
        return Mathf.Abs(horizontalDelta) <= 0.0001f
            ? 1f
            : Mathf.Sign(horizontalDelta);
    }
}
