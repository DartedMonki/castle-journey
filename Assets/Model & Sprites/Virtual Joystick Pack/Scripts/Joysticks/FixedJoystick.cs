using UnityEngine;
using UnityEngine.EventSystems;

public class FixedJoystick : Joystick
{
    private int? activePointer;

    public override void OnDrag(PointerEventData eventData)
    {
        if (activePointer != eventData.pointerId || background == null || handle == null)
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out var local))
            return;
        var radius = background.rect.size * .5f;
        if (radius.x <= 0f || radius.y <= 0f)
            return;
        inputVector = Vector2.ClampMagnitude(new Vector2(local.x / radius.x, local.y / radius.y), 1f);
        ClampJoystick();
        handle.anchoredPosition = Vector2.Scale(inputVector, radius) * handleLimit;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (activePointer.HasValue)
            return;
        activePointer = eventData.pointerId;
        OnDrag(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (activePointer == eventData.pointerId)
            ResetInput();
    }

    private void OnDisable() => ResetInput();
    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
            ResetInput();
    }

    private void ResetInput()
    {
        activePointer = null;
        inputVector = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}
