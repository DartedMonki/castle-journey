using UnityEngine;
using UnityEngine.EventSystems;

public class Joybutton : MonoBehaviour,IPointerUpHandler,IPointerDownHandler
{
    [HideInInspector]
    public bool Pressed;


    public void OnPointerDown(PointerEventData eventData)
    {
         Pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
         Pressed = false;
    }
}
