using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PointerHoverVisualSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject defaultVisual;
    [SerializeField] private GameObject hoverVisual;
    [SerializeField] private Selectable selectable;

    void Awake()
    {
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }

        ShowDefaultVisual();
    }

    void OnEnable()
    {
        ShowDefaultVisual();
    }

    void OnDisable()
    {
        ShowDefaultVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanShowHoverVisual())
        {
            ShowDefaultVisual();
            return;
        }

        if (!HasVisualReferences("OnPointerEnter"))
        {
            return;
        }

        defaultVisual.SetActive(false);
        hoverVisual.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ShowDefaultVisual();
    }

    bool CanShowHoverVisual()
    {
        return selectable == null || selectable.interactable;
    }

    void ShowDefaultVisual()
    {
        if (!HasVisualReferences("ShowDefaultVisual"))
        {
            return;
        }

        defaultVisual.SetActive(true);
        hoverVisual.SetActive(false);
    }

    bool HasVisualReferences(string callerName)
    {
        bool hasReferences = true;

        if (defaultVisual == null)
        {
            Debug.LogError(callerName + " 失败：defaultVisual 未绑定。");
            hasReferences = false;
        }

        if (hoverVisual == null)
        {
            Debug.LogError(callerName + " 失败：hoverVisual 未绑定。");
            hasReferences = false;
        }

        return hasReferences;
    }
}
