using UnityEngine;
using UnityEngine.UI;

namespace CityPuzzle.UI
{
    // "Cover" fit: scales the sprite uniformly so it fully covers its parent's rect, cropping the
    // overflow (the parent needs a RectMask2D) instead of stretching it non-uniformly like a plain
    // stretched Image does. Keeps city photos undistorted regardless of screen aspect ratio.
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class AspectFillImage : MonoBehaviour
    {
        RectTransform rt;
        RectTransform parentRt;
        Image image;
        Sprite lastSprite;
        Vector2 lastParentSize;

        void Awake()
        {
            rt = (RectTransform)transform;
            parentRt = (RectTransform)transform.parent;
            image = GetComponent<Image>();
        }

        void LateUpdate() => Apply();

        void Apply()
        {
            Sprite sprite = image.sprite;
            Vector2 parentSize = parentRt.rect.size;
            if (sprite == lastSprite && parentSize == lastParentSize) return;
            lastSprite = sprite;
            lastParentSize = parentSize;
            if (sprite == null || parentSize.x <= 0f || parentSize.y <= 0f) return;

            float spriteAspect = sprite.rect.width / sprite.rect.height;
            float containerAspect = parentSize.x / parentSize.y;

            Vector2 size = spriteAspect > containerAspect
                ? new Vector2(parentSize.y * spriteAspect, parentSize.y)
                : new Vector2(parentSize.x, parentSize.x / spriteAspect);

            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
