using UnityEngine;
using UnityEngine.EventSystems;

namespace DitzeGames.MobileJoystick
{
    public class TouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [HideInInspector] public Vector2 TouchDist;
        [HideInInspector] protected Vector2 PointerOld;
        [HideInInspector] protected int PointerId;
        [HideInInspector] public bool Pressed;
        private Vector2 pendingDelta;

        private void Update()
        {
            TouchDist = pendingDelta;
            pendingDelta = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Pressed)
                return;
            Pressed = true;
            PointerId = eventData.pointerId;
            PointerOld = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Pressed || eventData.pointerId != PointerId)
                return;
            pendingDelta += eventData.position - PointerOld;
            PointerOld = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == PointerId)
                OnDisable();
        }

        private void OnDisable()
        {
            Pressed = false;
            TouchDist = Vector2.zero;
            pendingDelta = Vector2.zero;
        }
    }
}
