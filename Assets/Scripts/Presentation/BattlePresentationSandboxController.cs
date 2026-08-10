using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 仅用于 BattlePresentationSandbox 的手动姿态切换测试。
public sealed class BattlePresentationSandboxController : MonoBehaviour
{
    [SerializeField] private BattleCharacterPresentationController character;
    [SerializeField] private BattleCharacterPresentationController defender;
    [SerializeField] private float engagementGap = 1.0f;
    [SerializeField] private float dualClashReadyGap = 2.8f;
    [SerializeField] private float sprintDistance = 2f;
    [SerializeField] private float sprintDuration = 0.35f;
    [SerializeField, Range(0f, 1f)]
    private float repeatedLerpFactor = 0.2f;
    [SerializeField] private float repeatedLerpSnapRatio = 0.01f;
    [SerializeField] private float slashHoldDuration = 0.2f;
    [SerializeField] private float afterimageSpawnInterval = 0.08f;
    [SerializeField] private float hitStopDuration = 0.08f;
    [SerializeField] private float clashRecoilDistance = 0.40f;
    [SerializeField] private float clashRecoilDuration = 0.08f;

    private Coroutine dynamicTestCoroutine;
    private Coroutine hitStopCoroutine;
    private Coroutine dualHitStopCoroutine;
    private Coroutine loserHitCoroutine;
    private Coroutine characterTieSlashCoroutine;
    private Coroutine defenderTieSlashCoroutine;
    private Coroutine tieClashRecoilCoroutine;
    private bool waitingForManualRoll;
    private bool attackImpactHandled;
    private bool characterTieImpactReached;
    private bool defenderTieImpactReached;
    private bool tieCollisionHandled;
    private bool characterTieSlashCompleted;
    private bool defenderTieSlashCompleted;
    private bool tieClashRecoilCompleted;
    private float currentTieDirectionSign = 1f;
    private bool hasWarnedMissingDefender;
    private bool hasWarnedMissingAttackPair;
    private bool hasWarnedMissingTiePair;
    private Vector3 characterResetPosition;
    private bool hasCharacterResetPosition;

    private const int RepeatedLerpMaxFrames = 600;

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
        else if (keyboard.qKey.wasPressedThisFrame)
        {
            StartRepeatedLerpSprintForward();
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

        if (characterTieSlashCoroutine != null)
        {
            StopCoroutine(characterTieSlashCoroutine);
            characterTieSlashCoroutine = null;
        }

        if (defenderTieSlashCoroutine != null)
        {
            StopCoroutine(defenderTieSlashCoroutine);
            defenderTieSlashCoroutine = null;
        }

        if (tieClashRecoilCoroutine != null)
        {
            StopCoroutine(tieClashRecoilCoroutine);
            tieClashRecoilCoroutine = null;
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

        waitingForManualRoll = false;
        attackImpactHandled = false;
        ResetTieRoundState();

        if (character != null)
        {
            character.SetPresentationPaused(false);
            character.ClearBodyVisualOffsets();
            character.ClearAfterimages();
            character.ClearSlashEffect();
            character.SetIdle();
        }

        if (defender != null)
        {
            defender.SetPresentationPaused(false);
            defender.ClearAfterimages();
            defender.ClearSlashEffect();
            defender.FinishHitReaction();
        }
    }

    private void StartSprintForward()
    {
        StartDynamicTest(false);
    }

    private void StartRepeatedLerpSprintForward()
    {
        if (character == null || dynamicTestCoroutine != null)
        {
            return;
        }

        Vector3 startPosition = character.transform.position;
        Vector3 targetPosition = startPosition + Vector3.right * sprintDistance;
        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        float safeLerpFactor = Mathf.Clamp01(repeatedLerpFactor);

        character.SetSprint();
        if (totalDistance <= Mathf.Epsilon || safeLerpFactor <= 0f)
        {
            character.transform.position = targetPosition;
            character.SetIdle();
            return;
        }

        dynamicTestCoroutine = StartCoroutine(
            RunRepeatedLerpSprintTest(
                startPosition,
                targetPosition,
                totalDistance,
                safeLerpFactor
            )
        );
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

    private IEnumerator RunHitReactionTest()
    {
        yield return character.PlayHitReaction(-1f);
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
        if (dynamicTestCoroutine != null)
        {
            return;
        }

        ResetTieRoundState();
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

    private IEnumerator RunRepeatedLerpSprintTest(
        Vector3 startPosition,
        Vector3 targetPosition,
        float totalDistance,
        float safeLerpFactor
    )
    {
        yield return RunRepeatedLerpSprintMovement(
            startPosition,
            targetPosition,
            totalDistance,
            safeLerpFactor
        );

        if (character != null)
        {
            character.transform.position = targetPosition;
            character.SetIdle();
        }

        dynamicTestCoroutine = null;
    }

    private IEnumerator RunRepeatedLerpSprintMovement(
        Vector3 startPosition,
        Vector3 targetPosition,
        float totalDistance,
        float safeLerpFactor
    )
    {
        if (character == null)
        {
            yield break;
        }

        float safeSnapRatio = Mathf.Max(0f, repeatedLerpSnapRatio);
        float snapDistance = totalDistance * safeSnapRatio;
        float afterimageElapsed = 0f;
        bool spawnRepeatedAfterimages = afterimageSpawnInterval > 0f;
        int frameCount = 0;

        character.SpawnAfterimage();
        while (frameCount < RepeatedLerpMaxFrames)
        {
            if (character == null)
            {
                yield break;
            }

            Vector3 currentPosition = character.transform.position;
            character.transform.position = currentPosition +
                (targetPosition - currentPosition) * safeLerpFactor;
            frameCount++;

            float remainingDistance = Vector3.Distance(
                character.transform.position,
                targetPosition
            );
            bool shouldContinue = remainingDistance > snapDistance &&
                frameCount < RepeatedLerpMaxFrames;

            if (spawnRepeatedAfterimages && shouldContinue)
            {
                afterimageElapsed += Time.deltaTime;
                if (afterimageElapsed >= afterimageSpawnInterval)
                {
                    character.SpawnAfterimage();
                    afterimageElapsed = 0f;
                }
            }

            if (!shouldContinue)
            {
                break;
            }

            yield return null;
        }

        if (character != null)
        {
            character.transform.position = targetPosition;
        }
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

        character.transform.position = characterResetPosition;
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
            float easedT = EaseOutQuad(linearT);
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

    private IEnumerator RunDualSprintMovement(
        Vector3 characterStartPosition,
        Vector3 characterTargetPosition,
        Vector3 defenderStartPosition,
        Vector3 defenderTargetPosition
    )
    {
        if (character == null || defender == null)
        {
            yield break;
        }

        if (sprintDuration <= 0f)
        {
            character.transform.position = characterTargetPosition;
            defender.transform.position = defenderTargetPosition;
            yield break;
        }

        float elapsed = 0f;
        float afterimageElapsed = 0f;
        bool spawnRepeatedAfterimages = afterimageSpawnInterval > 0f;
        bool characterMoves =
            (characterTargetPosition - characterStartPosition).sqrMagnitude >
            Mathf.Epsilon;
        bool defenderMoves =
            (defenderTargetPosition - defenderStartPosition).sqrMagnitude >
            Mathf.Epsilon;

        if (characterMoves)
        {
            character.SpawnAfterimage();
        }
        if (defenderMoves)
        {
            defender.SpawnAfterimage();
        }

        while (elapsed < sprintDuration)
        {
            if (character == null || defender == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(elapsed / sprintDuration);
            float easedT = EaseOutQuad(linearT);
            character.transform.position = characterStartPosition +
                (characterTargetPosition - characterStartPosition) * easedT;
            defender.transform.position = defenderStartPosition +
                (defenderTargetPosition - defenderStartPosition) * easedT;

            if (spawnRepeatedAfterimages &&
                (characterMoves || defenderMoves))
            {
                afterimageElapsed += Time.deltaTime;
                if (afterimageElapsed >= afterimageSpawnInterval &&
                    elapsed < sprintDuration)
                {
                    if (characterMoves)
                    {
                        character.SpawnAfterimage();
                    }
                    if (defenderMoves)
                    {
                        defender.SpawnAfterimage();
                    }
                    afterimageElapsed = 0f;
                }
            }

            yield return null;
        }

        if (character == null || defender == null)
        {
            yield break;
        }

        character.transform.position = characterTargetPosition;
        defender.transform.position = defenderTargetPosition;
    }

    private IEnumerator RunDualApproachToClashReady()
    {
        if (character == null || defender == null)
        {
            yield break;
        }

        Vector3 characterStartPosition = character.transform.position;
        Vector3 defenderStartPosition = defender.transform.position;
        float horizontalDelta =
            defenderStartPosition.x - characterStartPosition.x;
        float directionSign = GetHorizontalDirectionSign(horizontalDelta);
        float safeDualClashReadyGap = Mathf.Max(0f, dualClashReadyGap);
        float horizontalDistance = Mathf.Abs(horizontalDelta);

        character.SetSprint();
        defender.SetSprint();

        if (horizontalDistance <= safeDualClashReadyGap)
        {
            yield break;
        }

        float midX =
            (characterStartPosition.x + defenderStartPosition.x) * 0.5f;
        Vector3 characterTargetPosition = characterStartPosition;
        Vector3 defenderTargetPosition = defenderStartPosition;
        characterTargetPosition.x = midX -
            directionSign * safeDualClashReadyGap * 0.5f;
        defenderTargetPosition.x = midX +
            directionSign * safeDualClashReadyGap * 0.5f;

        yield return RunDualSprintMovement(
            characterStartPosition,
            characterTargetPosition,
            defenderStartPosition,
            defenderTargetPosition
        );
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
        if (character == null || defender == null)
        {
            FinishAttackTest(character, defender);
            yield break;
        }

        float horizontalDelta =
            defender.transform.position.x - character.transform.position.x;
        float directionSign = GetHorizontalDirectionSign(horizontalDelta);

        yield return RunDualApproachToClashReady();

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

        BattleCharacterPresentationController winner = defenderWins
            ? defender
            : character;
        BattleCharacterPresentationController loser = defenderWins
            ? character
            : defender;
        float attackDirectionSign = defenderWins
            ? -directionSign
            : directionSign;

        yield return RunWinnerAttackPresentation(
            winner,
            loser,
            attackDirectionSign
        );
    }

    private IEnumerator RunAttackTieLoopTest()
    {
        if (character == null || defender == null)
        {
            FinishTieLoopAfterAbort();
            yield break;
        }

        yield return RunDualApproachToClashReady();

        while (character != null && defender != null)
        {
            yield return WaitForManualRollForPair();
            if (character == null || defender == null)
            {
                break;
            }

            float horizontalDelta =
                defender.transform.position.x - character.transform.position.x;
            float directionSign = GetHorizontalDirectionSign(horizontalDelta);
            yield return RunTieSlashRound(directionSign);

            if (character == null || defender == null)
            {
                break;
            }

            yield return RunDualApproachToClashReady();
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

    private IEnumerator RunTieSlashRound(float directionSign)
    {
        ResetTieRoundState();
        currentTieDirectionSign = directionSign;
        character.SetSlash();
        defender.SetSlash();

        float slashStartedAt = Time.time;
        characterTieSlashCoroutine = StartCoroutine(
            RunTieSlashPresentation(character, directionSign, true)
        );
        defenderTieSlashCoroutine = StartCoroutine(
            RunTieSlashPresentation(defender, -directionSign, false)
        );

        while (!characterTieSlashCompleted ||
            !defenderTieSlashCompleted ||
            !tieClashRecoilCompleted)
        {
            if (character == null || defender == null)
            {
                yield break;
            }

            yield return null;
        }

        characterTieSlashCoroutine = null;
        defenderTieSlashCoroutine = null;
        tieClashRecoilCoroutine = null;

        float remainingSlashHold = slashHoldDuration -
            (Time.time - slashStartedAt);
        while (remainingSlashHold > 0f)
        {
            if (character == null || defender == null)
            {
                yield break;
            }

            remainingSlashHold -= Time.deltaTime;
            yield return null;
        }

        // 同一调用栈内完成双方Slash收尾并切回Sprint，避免出现单方中间帧。
        character.ClearSlashEffect();
        defender.ClearSlashEffect();
        character.FinishSlashPresentation();
        defender.FinishSlashPresentation();
        character.SetSprint();
        defender.SetSprint();
    }

    private IEnumerator RunTieSlashPresentation(
        BattleCharacterPresentationController actor,
        float attackDirectionSign,
        bool isCharacter
    )
    {
        if (actor != null)
        {
            yield return actor.PlaySlashPresentation(
                attackDirectionSign,
                () => MarkTieImpactReached(isCharacter)
            );
        }

        if (isCharacter)
        {
            characterTieSlashCompleted = true;
        }
        else
        {
            defenderTieSlashCompleted = true;
        }
    }

    private void MarkTieImpactReached(bool isCharacter)
    {
        if (isCharacter)
        {
            characterTieImpactReached = true;
        }
        else
        {
            defenderTieImpactReached = true;
        }

        if (tieCollisionHandled ||
            !characterTieImpactReached ||
            !defenderTieImpactReached)
        {
            return;
        }

        tieCollisionHandled = true;
        if (character == null || defender == null)
        {
            return;
        }

        // Tie不暂停Slash表现；双方到达Impact后立即并行弹开根节点。
        tieClashRecoilCoroutine = StartCoroutine(
            RunTieClashRecoil(currentTieDirectionSign)
        );
    }

    private IEnumerator RunTieClashRecoil(float directionSign)
    {
        yield return RunDualClashRecoil(directionSign);
        tieClashRecoilCompleted = true;
    }

    private IEnumerator RunDualClashRecoil(float directionSign)
    {
        if (character == null || defender == null)
        {
            yield break;
        }

        Vector3 characterStartPosition = character.transform.position;
        Vector3 defenderStartPosition = defender.transform.position;
        float safeRecoilDistance = Mathf.Max(0f, clashRecoilDistance);
        Vector3 characterTargetPosition = characterStartPosition -
            Vector3.right * directionSign * safeRecoilDistance;
        Vector3 defenderTargetPosition = defenderStartPosition +
            Vector3.right * directionSign * safeRecoilDistance;

        if (clashRecoilDuration <= 0f)
        {
            character.transform.position = characterTargetPosition;
            defender.transform.position = defenderTargetPosition;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < clashRecoilDuration)
        {
            if (character == null || defender == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(elapsed / clashRecoilDuration);
            float easedT = EaseOutQuad(linearT);
            character.transform.position = characterStartPosition +
                (characterTargetPosition - characterStartPosition) * easedT;
            defender.transform.position = defenderStartPosition +
                (defenderTargetPosition - defenderStartPosition) * easedT;
            yield return null;
        }

        if (character == null || defender == null)
        {
            yield break;
        }

        character.transform.position = characterTargetPosition;
        defender.transform.position = defenderTargetPosition;
    }

    private void ResetTieRoundState()
    {
        characterTieImpactReached = false;
        defenderTieImpactReached = false;
        tieCollisionHandled = false;
        characterTieSlashCompleted = false;
        defenderTieSlashCompleted = false;
        tieClashRecoilCompleted = false;
    }

    private void FinishTieLoopAfterAbort()
    {
        if (dualHitStopCoroutine != null)
        {
            StopCoroutine(dualHitStopCoroutine);
            dualHitStopCoroutine = null;
        }

        if (characterTieSlashCoroutine != null)
        {
            StopCoroutine(characterTieSlashCoroutine);
            characterTieSlashCoroutine = null;
        }

        if (defenderTieSlashCoroutine != null)
        {
            StopCoroutine(defenderTieSlashCoroutine);
            defenderTieSlashCoroutine = null;
        }

        if (tieClashRecoilCoroutine != null)
        {
            StopCoroutine(tieClashRecoilCoroutine);
            tieClashRecoilCoroutine = null;
        }

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
        ResetTieRoundState();
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

    private static float EaseOutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }

    private static float GetHorizontalDirectionSign(float horizontalDelta)
    {
        return Mathf.Abs(horizontalDelta) <= 0.0001f
            ? 1f
            : Mathf.Sign(horizontalDelta);
    }
}
