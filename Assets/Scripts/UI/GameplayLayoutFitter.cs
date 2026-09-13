using UnityEngine;

namespace CityPuzzle.UI
{
    // The gameplay layout is designed portrait-first. On a wide desktop/16:9 window,
    // CanvasAspectFitter pillarboxes the whole canvas but leaves a lot of unused horizontal
    // room around the fixed-width play area. This widens the viewport/world (more room to
    // scatter pieces) and grows the assembly board itself on wide screens.
    public class GameplayLayoutFitter : MonoBehaviour
    {
        public RectTransform viewport;
        public RectTransform world;
        public RectTransform boardArea;

        const float ReferenceAspect = 1080f / 1920f;
        const float RefCanvasHeight = 1920f;

        static readonly Vector2 PortraitViewport = new Vector2(1050f, 1560f);
        static readonly Vector2 PortraitWorld = new Vector2(1700f, 1900f);
        static readonly Vector2 PortraitBoard = new Vector2(1000f, 1000f);

        const float MaxWidenFactor = 1.9f;
        const float BoardGrowShare = 0.55f;

        float lastAspect = -1f;

        void OnEnable() => Apply();

        void Update()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (!Mathf.Approximately(aspect, lastAspect)) Apply();
        }

        void Apply()
        {
            lastAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

            if (lastAspect <= ReferenceAspect)
            {
                viewport.sizeDelta = PortraitViewport;
                world.sizeDelta = PortraitWorld;
                boardArea.sizeDelta = PortraitBoard;
                return;
            }

            float canvasWidthUnits = lastAspect * RefCanvasHeight;
            float targetViewportWidth = Mathf.Min(canvasWidthUnits * 0.92f, PortraitViewport.x * MaxWidenFactor);
            float widenFactor = Mathf.Max(1f, targetViewportWidth / PortraitViewport.x);

            viewport.sizeDelta = new Vector2(targetViewportWidth, PortraitViewport.y);
            world.sizeDelta = new Vector2(PortraitWorld.x * widenFactor, PortraitWorld.y);
            boardArea.sizeDelta = PortraitBoard * (1f + (widenFactor - 1f) * BoardGrowShare);
        }
    }
}
