using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 一级卡面的静态视觉样式，只处理类型文字、类型颜色、描边和品质底图。
public sealed class BattleCardVisualStyle : MonoBehaviour
{
    const string AbilityCardType = "Ability";

    [Header("类型文字")]
    [SerializeField] private string attackLabel = "攻";
    [SerializeField] private string defenseLabel = "防";
    [SerializeField] private string dodgeLabel = "闪";
    [SerializeField] private string abilityLabel = "能";
    [SerializeField] private string fallbackLabel = "？";

    [Header("类型颜色")]
    [SerializeField] private Color attackColor = new Color(0.85f, 0.2f, 0.2f);
    [SerializeField] private Color defenseColor = new Color(0.2f, 0.45f, 0.9f);
    [SerializeField] private Color dodgeColor = new Color(0.2f, 0.7f, 0.4f);
    [SerializeField] private Color abilityColor = new Color(0.65f, 0.3f, 0.85f);
    [SerializeField] private Color fallbackColor = Color.white;

    [Header("类型描边")]
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.2f;

    [Header("品质底图")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Sprite whiteFrameSprite;
    [SerializeField] private Sprite blueFrameSprite;
    [SerializeField] private Sprite purpleFrameSprite;
    [SerializeField] private Sprite goldFrameSprite;
    [SerializeField] private Sprite sinFrameSprite;
    [SerializeField] private Sprite fallbackFrameSprite;

    private TMP_Text outlinedText;
    private Material outlinedMaterial;
    private bool warnedMissingTypeText;
    private bool warnedMissingFrameImage;

    public Material AppliedOutlineMaterial => outlinedMaterial;

    public void Apply(BattleCardUIPreviewData data, TMP_Text targetTypeText)
    {
        ApplyTypeStyle(data != null ? data.cardType : "", targetTypeText);
        ApplyFrameStyle(
            data != null ? data.rarity : CardRarity.White,
            data != null && data.isSinCard
        );
    }

    public string GetTypeLabel(string cardType)
    {
        if (cardType == CardType.Attack)
        {
            return attackLabel;
        }

        if (cardType == CardType.Defense)
        {
            return defenseLabel;
        }

        if (cardType == CardType.Dodge)
        {
            return dodgeLabel;
        }

        if (cardType == AbilityCardType)
        {
            return abilityLabel;
        }

        return fallbackLabel;
    }

    public Color GetTypeColor(string cardType)
    {
        if (cardType == CardType.Attack)
        {
            return attackColor;
        }

        if (cardType == CardType.Defense)
        {
            return defenseColor;
        }

        if (cardType == CardType.Dodge)
        {
            return dodgeColor;
        }

        if (cardType == AbilityCardType)
        {
            return abilityColor;
        }

        return fallbackColor;
    }

    public Sprite GetFrameSprite(string rarity, bool isSinCard)
    {
        if (isSinCard)
        {
            return sinFrameSprite != null
                ? sinFrameSprite
                : fallbackFrameSprite;
        }

        Sprite frameSprite = null;
        string normalizedRarity = string.IsNullOrEmpty(rarity)
            ? CardRarity.White
            : rarity;

        if (normalizedRarity == CardRarity.White)
        {
            frameSprite = whiteFrameSprite;
        }
        else if (normalizedRarity == CardRarity.Blue)
        {
            frameSprite = blueFrameSprite;
        }
        else if (normalizedRarity == CardRarity.Purple)
        {
            frameSprite = purpleFrameSprite;
        }
        else if (normalizedRarity == CardRarity.Gold)
        {
            frameSprite = goldFrameSprite;
        }

        return frameSprite != null ? frameSprite : fallbackFrameSprite;
    }

    void ApplyTypeStyle(string cardType, TMP_Text targetTypeText)
    {
        if (targetTypeText == null)
        {
            if (!warnedMissingTypeText)
            {
                Debug.LogWarning(
                    "BattleCardVisualStyle 缺少类型文字引用，无法应用卡牌类型视觉。",
                    this
                );
                warnedMissingTypeText = true;
            }

            return;
        }

        targetTypeText.text = GetTypeLabel(cardType);
        targetTypeText.color = GetTypeColor(cardType);

        if (outlinedText != targetTypeText || outlinedMaterial == null)
        {
            // TMP 的 fontMaterial 会为当前文字组件取得并缓存独立材质。
            outlinedText = targetTypeText;
            outlinedMaterial = targetTypeText.fontMaterial;
        }

        if (outlinedMaterial == null)
        {
            return;
        }

        outlinedMaterial.SetColor(
            ShaderUtilities.ID_OutlineColor,
            outlineColor
        );
        outlinedMaterial.SetFloat(
            ShaderUtilities.ID_OutlineWidth,
            Mathf.Clamp01(outlineWidth)
        );
        targetTypeText.UpdateMeshPadding();
    }

    void ApplyFrameStyle(string rarity, bool isSinCard)
    {
        if (frameImage == null)
        {
            if (!warnedMissingFrameImage)
            {
                Debug.LogWarning(
                    "BattleCardVisualStyle 缺少品质底图 Image 引用。",
                    this
                );
                warnedMissingFrameImage = true;
            }

            return;
        }

        frameImage.raycastTarget = false;
        Sprite frameSprite = GetFrameSprite(rarity, isSinCard);

        // 所有 Sprite 都未配置时保留当前底图，避免整张卡牌消失。
        if (frameSprite != null)
        {
            frameImage.sprite = frameSprite;
        }
    }
}
