using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityPuzzle.Puzzle
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class PuzzlePiece : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Vector2 TargetPosition { get; private set; }
        public bool IsLocked { get; private set; }

        RectTransform rectTransform;
        PuzzleBoard board;
        float snapDistance;
        Vector2 dragOffset;

        public void Setup(RectTransform layer, PuzzleBoard owningBoard, Vector2 targetPos, float snapDist)
        {
            rectTransform = (RectTransform)transform;
            board = owningBoard;
            TargetPosition = targetPos;
            snapDistance = snapDist;
            IsLocked = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsLocked) return;
            rectTransform.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsLocked) return;
            if (TryGetLocalPoint(eventData, out var localPoint))
                dragOffset = localPoint - rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsLocked) return;
            if (TryGetLocalPoint(eventData, out var localPoint))
                rectTransform.anchoredPosition = localPoint - dragOffset;
        }

        bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            var parent = (RectTransform)rectTransform.parent;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out localPoint);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsLocked) return;
            if (Vector2.Distance(rectTransform.anchoredPosition, TargetPosition) <= snapDistance)
            {
                SnapToTarget();
            }
        }

        void SnapToTarget()
        {
            IsLocked = true;
            rectTransform.anchoredPosition = TargetPosition;
            StopAllCoroutines();
            StartCoroutine(BounceScale());
            board.NotifyPieceSnapped();
        }

        IEnumerator BounceScale()
        {
            const float duration = 0.18f;
            Vector3 start = Vector3.one * 1.15f;
            Vector3 end = Vector3.one;
            rectTransform.localScale = start;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                rectTransform.localScale = Vector3.Lerp(start, end, t / duration);
                yield return null;
            }
            rectTransform.localScale = end;
        }
    }
}
