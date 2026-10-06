using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public sealed class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect previousSafeArea;
    private Vector2Int previousSize;

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        Refresh();
    }

    private void Update()
    {
        if (previousSafeArea != Screen.safeArea || previousSize != new Vector2Int(Screen.width, Screen.height))
            Refresh();
    }

    private void Refresh()
    {
        previousSafeArea = Screen.safeArea;
        previousSize = new Vector2Int(Screen.width, Screen.height);
        if (previousSize.x <= 0 || previousSize.y <= 0)
            return;
        rectTransform.anchorMin = new Vector2(previousSafeArea.xMin / previousSize.x, previousSafeArea.yMin / previousSize.y);
        rectTransform.anchorMax = new Vector2(previousSafeArea.xMax / previousSize.x, previousSafeArea.yMax / previousSize.y);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
