using UnityEngine;
using UnityEngine.UI;

namespace CityPuzzle.UI
{
    // The game is designed portrait-first (1080x1920). On a wide desktop/16:9 window we don't
    // stretch that layout sideways — instead we match by HEIGHT so the portrait UI keeps its
    // proportions and simply gets pillarboxed, matching how Yandex Games itself frames
    // portrait-only titles on desktop. Tall/narrow phone screens keep matching by width.
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasAspectFitter : MonoBehaviour
    {
        CanvasScaler scaler;
        float lastAspect = -1f;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
        }

        void Update()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (Mathf.Approximately(aspect, lastAspect)) return;
            lastAspect = aspect;

            float referenceAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
            // Wider than the portrait reference (landscape/desktop) -> match height (pillarbox).
            // Taller/narrower (typical phone) -> match width (fills width, may add a hair of
            // top/bottom safe area rather than stretching horizontally).
            scaler.matchWidthOrHeight = aspect > referenceAspect ? 1f : 0f;
        }
    }
}
