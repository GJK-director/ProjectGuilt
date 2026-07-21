using UnityEngine;

public class CharacterStatusUIFollower : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Transform footAnchor;
    [SerializeField] private RectTransform headSlotGroup;
    [SerializeField] private RectTransform footStatusGroup;
    [SerializeField] private Vector2 headOffset;
    [SerializeField] private Vector2 footOffset;

    void LateUpdate()
    {
        if (worldCamera == null || canvasRect == null)
        {
            return;
        }

        UpdateFollowGroup(headAnchor, headSlotGroup, headOffset);
        UpdateFollowGroup(footAnchor, footStatusGroup, footOffset);
    }

    void UpdateFollowGroup(Transform anchor, RectTransform uiGroup, Vector2 screenOffset)
    {
        if (anchor == null || uiGroup == null)
        {
            return;
        }

        // 先把角色世界空间锚点转换到屏幕坐标。
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchor.position);

        // z <= 0 表示锚点在摄像机背后，此时隐藏对应 UI 组。
        if (screenPoint.z <= 0f)
        {
            if (uiGroup.gameObject.activeSelf)
            {
                uiGroup.gameObject.SetActive(false);
            }

            return;
        }

        if (!uiGroup.gameObject.activeSelf)
        {
            uiGroup.gameObject.SetActive(true);
        }

        Vector2 localPoint;

        // Canvas 是 Screen Space - Overlay，所以 camera 参数传 null。
        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            new Vector2(screenPoint.x, screenPoint.y) + screenOffset,
            null,
            out localPoint
        );

        if (!converted)
        {
            return;
        }

        // UI 组使用 Canvas 本地坐标跟随角色锚点。
        uiGroup.anchoredPosition = localPoint;
    }
}
