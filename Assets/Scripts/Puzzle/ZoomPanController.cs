using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CityPuzzle.Puzzle
{
    // Lives on the (ancestor) viewport so mouse-wheel zoom keeps working even while the pointer
    // is over a piece; panning only engages when a drag starts on empty space, since a piece
    // intercepts its own drag before it ever bubbles up here.
    public class ZoomPanController : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform world;
        public float minScale = 0.5f;
        public float maxScale = 1.8f;
        public float scrollZoomSpeed = 0.08f;
        public float pinchZoomSpeed = 0.012f;

        RectTransform viewportRect;
        Canvas rootCanvas;
        bool isPinching;
        float lastPinchDistance;
        bool draggingPointer;

        void Awake()
        {
            viewportRect = (RectTransform)transform;
            rootCanvas = GetComponentInParent<Canvas>();
        }

        void Update()
        {
            HandlePinch();
        }

        void HandlePinch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) { isPinching = false; return; }

            TouchControl t0 = null, t1 = null;
            foreach (var t in touchscreen.touches)
            {
                if (!t.press.isPressed) continue;
                if (t0 == null) t0 = t;
                else if (t1 == null) { t1 = t; break; }
            }

            if (t0 == null || t1 == null)
            {
                isPinching = false;
                return;
            }

            Vector2 p0 = t0.position.ReadValue();
            Vector2 p1 = t1.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewportRect, p0, null) &&
                !RectTransformUtility.RectangleContainsScreenPoint(viewportRect, p1, null))
            {
                isPinching = false;
                return;
            }

            float distance = Vector2.Distance(p0, p1);
            Vector2 midpoint = (p0 + p1) * 0.5f;

            if (!isPinching)
            {
                isPinching = true;
                lastPinchDistance = distance;
                return;
            }

            float delta = distance - lastPinchDistance;
            lastPinchDistance = distance;
            ApplyZoom(delta * pinchZoomSpeed, midpoint);
        }

        public void OnScroll(PointerEventData eventData)
        {
            ApplyZoom(eventData.scrollDelta.y * scrollZoomSpeed, eventData.position);
        }

        void ApplyZoom(float scaleDelta, Vector2 screenPoint)
        {
            if (Mathf.Approximately(scaleDelta, 0f)) return;
            float oldScale = world.localScale.x;
            float newScale = Mathf.Clamp(oldScale + scaleDelta, minScale, maxScale);
            if (Mathf.Approximately(newScale, oldScale)) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, screenPoint, null, out var localPoint))
                return;

            Vector2 worldPointUnderCursor = (localPoint - world.anchoredPosition) / oldScale;
            world.localScale = new Vector3(newScale, newScale, 1f);
            world.anchoredPosition = localPoint - worldPointUnderCursor * newScale;
            ClampPan();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            draggingPointer = !isPinching;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!draggingPointer || isPinching) return;
            float scale = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            world.anchoredPosition += eventData.delta / scale;
            ClampPan();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            draggingPointer = false;
        }

        void ClampPan()
        {
            float scale = world.localScale.x;
            float halfWorldW = world.rect.width * scale * 0.5f;
            float halfWorldH = world.rect.height * scale * 0.5f;
            float halfViewW = viewportRect.rect.width * 0.5f;
            float halfViewH = viewportRect.rect.height * 0.5f;

            float maxX = Mathf.Max(0f, halfWorldW - halfViewW * 0.3f);
            float maxY = Mathf.Max(0f, halfWorldH - halfViewH * 0.3f);
            Vector2 pos = world.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
            world.anchoredPosition = pos;
        }

        public void ResetView(float scale, Vector2 anchoredPosition)
        {
            isPinching = false;
            draggingPointer = false;
            world.localScale = new Vector3(scale, scale, 1f);
            world.anchoredPosition = anchoredPosition;
        }
    }
}
