using UnityEngine;
using UnityEngine.EventSystems;

public class FixedJoystick : Joystick
{
    Vector2 joystickPosition = Vector2.zero;
    PointerEventData TempPED; // TempPointerEventData: This keeps track of the position where the touch event occured.
    private Camera cam = new Camera();
    private bool _IsPressed = false;

    void Start()
    {
        joystickPosition = RectTransformUtility.WorldToScreenPoint(cam, handle.position);
    }

    public override void OnDrag(PointerEventData eventData) // OnDrag does not get called recursively automatically on mobile.  
    {
        Vector2 direction = eventData.position - joystickPosition;
        inputVector = (direction.magnitude > background.sizeDelta.x / 2f) ? direction.normalized : direction / (background.sizeDelta.x / 2f);
        ClampJoystick();
        handle.anchoredPosition = (inputVector * background.sizeDelta.x / 2f) * handleLimit;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
        TempPED = eventData; // Assign event data to a variable we can access later.
        _IsPressed = true; // When you touch the joystick _IsPressed is set to true.
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        _IsPressed = false; // When you release the joystick _IsPressed is set to false.
    }

    public void Update()
    {
        if (_IsPressed)
        {
            OnDrag(TempPED); // The OnDrag method repeatedly gets called until you let go of the joystick; this makes the movement of the joystick work as intended.
            // This is by no means perfect but it works reasonably well.
        }
    }
}