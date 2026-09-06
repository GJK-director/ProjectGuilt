using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHpUIView : MonoBehaviour
{
    [SerializeField] private Image hpFillCurrentImage;
    [SerializeField] private Image hpBackLostImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private bool showMaxHp = false;
    [SerializeField, Min(0f)] private float stagedHpStepDelay = 0.12f;

    private Coroutine stagedHpCoroutine;
    private bool isStaging;
    private int stagedStartHp;
    private int stagedStageCount = 1;
    private int latestLogicalHp;
    private int latestMaxHp;
    private int displayedHp;

    public bool IsStaging => isStaging;
    public int DisplayedHp => displayedHp;

    public void SetCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            Clear();
            return;
        }

        SetHp(characterData.currentHP, characterData.maxHP);
    }

    public void SetHp(int currentHp, int maxHp)
    {
        latestLogicalHp = Mathf.Max(0, currentHp);
        latestMaxHp = Mathf.Max(0, maxHp);
        if (isStaging)
        {
            return;
        }

        ApplyHp(currentHp, maxHp);
    }

    private void ApplyHp(int currentHp, int maxHp)
    {
        if (maxHp <= 0)
        {
            displayedHp = 0;
            SetFillAmount(0f);
            SetHpText("-");
            return;
        }

        int clampedCurrentHp = Mathf.Clamp(currentHp, 0, maxHp);
        displayedHp = clampedCurrentHp;
        float fillAmount = Mathf.Clamp01((float)clampedCurrentHp / maxHp);

        SetFillAmount(fillAmount);

        string text = showMaxHp
            ? clampedCurrentHp + " / " + maxHp
            : clampedCurrentHp.ToString();

        SetHpText(text);
    }

    public void Clear()
    {
        CancelStagedHpTransition();
        displayedHp = 0;
        SetFillAmount(0f);
        SetHpText("-");
    }

    public void BeginStagedHpTransition(int startHp, int maxHp, int stageCount)
    {
        CancelStagedHpTransition();
        stagedStartHp = Mathf.Clamp(startHp, 0, Mathf.Max(0, maxHp));
        stagedStageCount = Mathf.Max(1, stageCount);
        latestLogicalHp = stagedStartHp;
        latestMaxHp = Mathf.Max(0, maxHp);
        isStaging = stagedStageCount > 1;
        ApplyHp(stagedStartHp, latestMaxHp);
    }

    public void CompleteStagedHpTransition(
        int finalHp,
        int maxHp,
        float stepDelayOverride = -1f
    )
    {
        latestLogicalHp = Mathf.Max(0, finalHp);
        latestMaxHp = Mathf.Max(0, maxHp);
        if (!isStaging)
        {
            ApplyHp(latestLogicalHp, latestMaxHp);
            return;
        }

        if (stagedHpCoroutine != null)
        {
            StopCoroutine(stagedHpCoroutine);
        }
        float delay = stepDelayOverride >= 0f
            ? stepDelayOverride
            : stagedHpStepDelay;
        stagedHpCoroutine = StartCoroutine(RunStagedHpTransition(delay));
    }

    public void CancelStagedHpTransition()
    {
        if (stagedHpCoroutine != null)
        {
            StopCoroutine(stagedHpCoroutine);
            stagedHpCoroutine = null;
        }
        isStaging = false;
    }

    private void OnDisable()
    {
        if (isStaging)
        {
            CancelStagedHpTransition();
            ApplyHp(latestLogicalHp, latestMaxHp);
        }
    }

    private IEnumerator RunStagedHpTransition(float stepDelay)
    {
        List<int> stages = BuildStagedHpValues(
            stagedStartHp,
            latestLogicalHp,
            stagedStageCount
        );
        for (int index = 0; index < stages.Count; index++)
        {
            ApplyHp(stages[index], latestMaxHp);
            if (index < stages.Count - 1)
            {
                if (stepDelay > 0f)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
                else
                {
                    yield return null;
                }
            }
        }

        stagedHpCoroutine = null;
        isStaging = false;
        ApplyHp(latestLogicalHp, latestMaxHp);
    }

    public static List<int> BuildStagedHpValues(
        int startHp,
        int finalHp,
        int stageCount
    )
    {
        int safeStageCount = Mathf.Max(1, stageCount);
        int totalDamage = Mathf.Max(0, startHp - finalHp);
        List<int> values = new List<int>(safeStageCount);
        for (int stage = 1; stage <= safeStageCount; stage++)
        {
            int appliedDamage = Mathf.CeilToInt(
                (float)totalDamage * stage / safeStageCount
            );
            values.Add(stage == safeStageCount
                ? finalHp
                : startHp - appliedDamage);
        }
        return values;
    }

    private void SetFillAmount(float fillAmount)
    {
        if (hpFillCurrentImage != null)
        {
            hpFillCurrentImage.fillAmount = fillAmount;
        }

        if (hpBackLostImage != null)
        {
            hpBackLostImage.fillAmount = fillAmount;
        }
    }

    private void SetHpText(string text)
    {
        if (hpText != null)
        {
            hpText.text = text;
        }
    }
}
