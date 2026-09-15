using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class BattleActionSlotOrderViewTests
{
    public static bool Run()
    {
        bool[] results =
        {
            VerifySetOrderDisplaysValue(),
            VerifyZeroOrderIsDisplayed(),
            VerifyClearOrderHidesValue(),
            VerifyMissingOrderTextIsSafe(),
            VerifyBindingChangeClearsOrder()
        };
        string[] names =
        {
            "A SetOrder displays 3",
            "B SetOrder displays 0",
            "C ClearOrder clears and hides",
            "D missing orderText is safe",
            "E changed binding clears order"
        };

        bool passed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "Mode105 BattleActionSlotOrderView " + names[index] + ": " +
                results[index]
            );
            passed &= results[index];
        }

        return passed;
    }

    static bool VerifySetOrderDisplaysValue()
    {
        TestContext context = CreateContext("mode105_order_a", true);
        context.view.SetOrder(3);
        bool result = context.orderText.text == "3" &&
            context.orderText.gameObject.activeSelf;
        DestroyContext(context);
        return result;
    }

    static bool VerifyZeroOrderIsDisplayed()
    {
        TestContext context = CreateContext("mode105_order_b", true);
        context.view.SetOrder(0);
        bool result = context.orderText.text == "0" &&
            context.orderText.gameObject.activeSelf;
        DestroyContext(context);
        return result;
    }

    static bool VerifyClearOrderHidesValue()
    {
        TestContext context = CreateContext("mode105_order_c", true);
        context.view.SetOrder(5);
        context.view.ClearOrder();
        bool result = context.orderText.text == string.Empty &&
            !context.orderText.gameObject.activeSelf;
        DestroyContext(context);
        return result;
    }

    static bool VerifyMissingOrderTextIsSafe()
    {
        TestContext context = CreateContext("mode105_order_d", false);
        context.view.SetOrder(2);
        context.view.ClearOrder();
        DestroyContext(context);
        return true;
    }

    static bool VerifyBindingChangeClearsOrder()
    {
        TestContext context = CreateContext("mode105_order_e", true);
        CharacterData characterA = new CharacterData(
            "mode105_order_character_a",
            30,
            5,
            5
        );
        CharacterData characterB = new CharacterData(
            "mode105_order_character_b",
            30,
            5,
            5
        );
        context.view.BindInteraction(characterA, 0, false, null);
        context.view.SetOrder(4);
        context.view.BindInteraction(characterB, 0, false, null);
        bool result = context.orderText.text == string.Empty &&
            !context.orderText.gameObject.activeSelf;
        DestroyContext(context);
        return result;
    }

    static TestContext CreateContext(string name, bool includeOrderText)
    {
        TestContext context = new TestContext();
        context.rootObject = new GameObject(
            name + "Root",
            typeof(RectTransform),
            typeof(Image)
        );
        context.rootObject.SetActive(false);

        GameObject effectObject = new GameObject(
            name + "SelectionEffect",
            typeof(RectTransform),
            typeof(Image),
            typeof(BattleActionSlotSelectionEffectUIView)
        );
        effectObject.transform.SetParent(
            context.rootObject.transform,
            false
        );

        if (includeOrderText)
        {
            GameObject orderObject = new GameObject(
                name + "OrderText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );
            orderObject.transform.SetParent(
                context.rootObject.transform,
                false
            );
            context.orderText = orderObject.GetComponent<TMP_Text>();
        }

        context.texture = new Texture2D(1, 1);
        context.texture.SetPixel(0, 0, Color.white);
        context.texture.Apply();
        context.sprite = Sprite.Create(
            context.texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f)
        );
        context.view = context.rootObject.AddComponent<BattleActionSlotUIView>();
        context.view.ConfigureTestVisuals(
            context.rootObject.GetComponent<Image>(),
            context.sprite
        );
        if (context.orderText != null)
        {
            context.view.ConfigureOrderTextForTesting(context.orderText);
        }
        context.rootObject.SetActive(true);
        return context;
    }

    static void DestroyContext(TestContext context)
    {
        if (context == null)
        {
            return;
        }

        Object.Destroy(context.rootObject);
        Object.Destroy(context.sprite);
        Object.Destroy(context.texture);
    }

    sealed class TestContext
    {
        public GameObject rootObject;
        public BattleActionSlotUIView view;
        public TMP_Text orderText;
        public Sprite sprite;
        public Texture2D texture;
    }
}
