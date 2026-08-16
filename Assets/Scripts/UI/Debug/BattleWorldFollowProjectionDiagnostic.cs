using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleWorldFollowProjectionDiagnostic : MonoBehaviour
{
    private const float MarkerSize = 14f;

    private Camera worldCamera;
    private Transform footAnchor;
    private SpriteRenderer bodyRenderer;
    private GUIStyle distanceLabelStyle;

    public void Bind(
        Camera camera,
        Transform anchor,
        SpriteRenderer renderer
    )
    {
        worldCamera = camera;
        footAnchor = anchor;
        bodyRenderer = renderer;
    }

    private void OnGUI()
    {
        if (!TryGetProjectedPoints(
                out Vector3 anchorScreen,
                out Vector3 visualFootScreen))
        {
            return;
        }

        Vector2 anchorGuiPoint = ToGuiPoint(anchorScreen);
        Vector2 visualFootGuiPoint = ToGuiPoint(visualFootScreen);

        Color previousColor = GUI.color;
        DrawMarker(anchorGuiPoint, Color.red);
        DrawMarker(visualFootGuiPoint, Color.green);
        GUI.color = previousColor;

        EnsureLabelStyle();
        float pixelDistance = Vector2.Distance(
            anchorScreen,
            visualFootScreen
        );
        Vector2 labelPosition =
            (anchorGuiPoint + visualFootGuiPoint) * 0.5f +
            new Vector2(10f, -24f);
        GUI.Label(
            new Rect(labelPosition.x, labelPosition.y, 280f, 24f),
            "Anchor <-> Visual Foot = " + pixelDistance.ToString("F1") +
            " px",
            distanceLabelStyle
        );
    }

    private bool TryGetProjectedPoints(
        out Vector3 anchorScreen,
        out Vector3 visualFootScreen
    )
    {
        anchorScreen = default;
        visualFootScreen = default;
        if (worldCamera == null || footAnchor == null ||
            bodyRenderer == null || bodyRenderer.sprite == null)
        {
            return false;
        }

        Sprite sprite = bodyRenderer.sprite;
        Vector3 spriteLocalFoot = new Vector3(
            sprite.bounds.center.x,
            sprite.bounds.min.y,
            sprite.bounds.center.z
        );

        // TransformPoint保留Sprite Pivot及完整视觉层级造成的实际偏移。
        Vector3 visualFootWorld =
            bodyRenderer.transform.TransformPoint(spriteLocalFoot);
        anchorScreen = worldCamera.WorldToScreenPoint(footAnchor.position);
        visualFootScreen = worldCamera.WorldToScreenPoint(visualFootWorld);
        return anchorScreen.z > 0f && visualFootScreen.z > 0f;
    }

    private static Vector2 ToGuiPoint(Vector3 screenPoint)
    {
        return new Vector2(screenPoint.x, Screen.height - screenPoint.y);
    }

    private static void DrawMarker(Vector2 center, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(
            new Rect(
                center.x - MarkerSize * 0.5f,
                center.y - MarkerSize * 0.5f,
                MarkerSize,
                MarkerSize
            ),
            Texture2D.whiteTexture
        );
    }

    private void EnsureLabelStyle()
    {
        if (distanceLabelStyle != null)
        {
            return;
        }

        distanceLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        distanceLabelStyle.normal.textColor = Color.white;
    }
}
