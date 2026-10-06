using UnityEngine;
using UnityEngine.EventSystems;

namespace DitzeGames.MobileJoystick
{
    public class Joystick : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
    {
        protected RectTransform Background;
        protected bool Pressed;
        protected int PointerId;
        public RectTransform Handle;
        [Range(0f, 2f)] public float HandleRange = 1f;
        [HideInInspector] public Vector2 InputVector;
        public Vector2 AxisNormalized => InputVector.magnitude > .25f ? InputVector.normalized
            : (InputVector.magnitude < .01f ? Vector2.zero : InputVector * 4f);

        private void Awake()
        {
            Background = GetComponent<RectTransform>();
            if (Handle == null && transform.childCount > 0)
                Handle = transform.GetChild(0).GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Pressed)
                return;
            Pressed = true;
            PointerId = eventData.pointerId;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Pressed || eventData.pointerId != PointerId || Background == null || Handle == null)
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Background, eventData.position, eventData.pressEventCamera, out var local))
                return;
            var radius = Background.rect.size * .5f;
            if (radius.x <= 0f || radius.y <= 0f)
                return;
            InputVector = Vector2.ClampMagnitude(new Vector2(local.x / radius.x, local.y / radius.y), 1f);
            Handle.anchoredPosition = Vector2.Scale(InputVector, radius) * HandleRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == PointerId)
                ResetInput();
        }

        private void OnDisable() => ResetInput();

        private void ResetInput()
        {
            Pressed = false;
            InputVector = Vector2.zero;
            if (Handle != null)
                Handle.anchoredPosition = Vector2.zero;
        }
    }
}
