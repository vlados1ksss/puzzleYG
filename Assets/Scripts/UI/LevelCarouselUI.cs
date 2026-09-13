using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CityPuzzle.Core;
using CityPuzzle.Services;

namespace CityPuzzle.UI
{
    // One-level-per-page carousel: swipe left/right to browse, tap Play to open the difficulty
    // picker. The background is a blurred copy of that level's own photo.
    public class LevelCarouselUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public LevelManager levelManager;
        public Image backgroundImage;
        public Image backgroundDim;
        public CanvasGroup cardGroup;
        public Text levelLabelText;
        public Text cityNameText;
        public Text bestTimeText;
        public StarsDisplay stars;
        public GameObject lockIcon;
        public CanvasGroup lockIconGroup;
        public Button playButton;
        public Image[] pageDots;
        public Button arrowLeftButton;
        public Button arrowRightButton;
        public CanvasGroup arrowLeftGroup;
        public CanvasGroup arrowRightGroup;
        public Text comingSoonText;

        const float ArrowDisabledAlpha = 0.35f;

        static readonly Color DotActive = new Color(1f, 1f, 1f, 0.95f);
        static readonly Color DotInactive = new Color(1f, 1f, 1f, 0.28f);

        public event Action<int> onPlayRequested;

        int currentIndex;
        bool dragging;
        Vector2 dragStart;
        const float SwipeThreshold = 90f;

        readonly Dictionary<int, Sprite> blurredCache = new Dictionary<int, Sprite>();
        const int BlurredSize = 220;

        public int CurrentIndex => currentIndex;
        public int TotalPages => levelManager.LevelCount + 1; // +1 for the trailing "coming soon" page

        void Awake()
        {
            playButton.onClick.AddListener(() =>
            {
                if (levelManager.IsUnlocked(currentIndex)) onPlayRequested?.Invoke(currentIndex);
            });
            if (arrowLeftButton != null) arrowLeftButton.onClick.AddListener(() => GoTo(currentIndex - 1));
            if (arrowRightButton != null) arrowRightButton.onClick.AddListener(() => GoTo(currentIndex + 1));
        }

        public void Open(int index)
        {
            StopAllCoroutines();
            currentIndex = Mathf.Clamp(index, 0, TotalPages - 1);
            ApplyContent(currentIndex);
            cardGroup.alpha = 1f;
        }

        public void RefreshCurrent() => ApplyContent(currentIndex);

        Sprite GetBlurred(int index)
        {
            if (blurredCache.TryGetValue(index, out var sprite) && sprite != null) return sprite;
            var level = levelManager.GetLevel(index);
            sprite = ImageBlur.CreateBlurredSprite(level.cityImage.texture, BlurredSize);
            blurredCache[index] = sprite;
            return sprite;
        }

        void ApplyContent(int index)
        {
            if (index >= levelManager.LevelCount) ApplyComingSoonContent();
            else ApplyLevelContent(index);

            if (pageDots != null)
                for (int i = 0; i < pageDots.Length; i++)
                    pageDots[i].color = i == index ? DotActive : DotInactive;

            if (arrowLeftButton != null) SetArrowState(arrowLeftButton, arrowLeftGroup, index > 0);
            if (arrowRightButton != null) SetArrowState(arrowRightButton, arrowRightGroup, index < TotalPages - 1);
        }

        void ApplyLevelContent(int index)
        {
            var level = levelManager.GetLevel(index);
            bool unlocked = levelManager.IsUnlocked(index);
            bool completed = SaveService.IsLevelCompleted(index);

            backgroundImage.color = Color.white;
            backgroundImage.sprite = GetBlurred(index);
            backgroundDim.color = new Color(0f, 0f, 0f, unlocked ? (completed ? 0.25f : 0.4f) : 0.72f);

            lockIcon.SetActive(!unlocked);
            if (lockIconGroup != null) lockIconGroup.alpha = 1f;
            lockIcon.transform.localScale = Vector3.one;
            playButton.gameObject.SetActive(unlocked);

            levelLabelText.gameObject.SetActive(true);
            levelLabelText.text = $"Уровень {index + 1}";
            cityNameText.gameObject.SetActive(completed);
            if (completed) cityNameText.text = level.cityName;

            float best = SaveService.GetOverallBestTime(index);
            bestTimeText.gameObject.SetActive(unlocked);
            bestTimeText.text = best >= 0f ? Timer.Format(best) : "—";

            stars.gameObject.SetActive(unlocked);
            if (unlocked) stars.SetImmediate(SaveService.GetStarRating(index), SaveService.HasQuizBonus(index));

            if (comingSoonText != null) comingSoonText.gameObject.SetActive(false);
        }

        void ApplyComingSoonContent()
        {
            backgroundImage.sprite = null;
            backgroundImage.color = new Color(0.06f, 0.07f, 0.12f, 1f);
            backgroundDim.color = new Color(0f, 0f, 0f, 0.55f);

            lockIcon.SetActive(false);
            playButton.gameObject.SetActive(false);
            levelLabelText.gameObject.SetActive(false);
            cityNameText.gameObject.SetActive(false);
            bestTimeText.gameObject.SetActive(false);
            stars.gameObject.SetActive(false);

            if (comingSoonText != null)
            {
                comingSoonText.gameObject.SetActive(true);
                comingSoonText.text = "Больше уровней -\nскоро";
            }
        }

        static void SetArrowState(Button button, CanvasGroup group, bool enabled)
        {
            button.interactable = enabled;
            if (group == null) return;
            group.alpha = enabled ? 1f : ArrowDisabledAlpha;
            group.blocksRaycasts = enabled;
        }

        public void GoTo(int index, bool animateUnlock = false)
        {
            index = Mathf.Clamp(index, 0, TotalPages - 1);
            StopAllCoroutines();
            if (index == currentIndex)
            {
                ApplyContent(currentIndex);
                cardGroup.alpha = 1f;
                if (animateUnlock) StartCoroutine(AnimateUnlock());
                return;
            }
            StartCoroutine(TransitionTo(index, animateUnlock));
        }

        IEnumerator TransitionTo(int index, bool animateUnlock)
        {
            yield return Fade(0f, 0.15f);
            currentIndex = index;
            ApplyContent(currentIndex);
            yield return Fade(1f, 0.25f);
            if (animateUnlock && levelManager.IsUnlocked(currentIndex))
                yield return AnimateUnlock();
        }

        IEnumerator Fade(float target, float duration)
        {
            float start = cardGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cardGroup.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            cardGroup.alpha = target;
        }

        IEnumerator AnimateUnlock()
        {
            if (!lockIcon.activeSelf) yield break;
            const float duration = 0.35f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duration;
                lockIcon.transform.localScale = Vector3.one * (1f + p * 0.5f);
                if (lockIconGroup != null) lockIconGroup.alpha = 1f - p;
                yield return null;
            }
            lockIcon.SetActive(false);
            lockIcon.transform.localScale = Vector3.one;
            if (lockIconGroup != null) lockIconGroup.alpha = 1f;
            playButton.gameObject.SetActive(true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            dragStart = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;
            float deltaX = eventData.position.x - dragStart.x;
            if (deltaX > SwipeThreshold) GoTo(currentIndex + 1);
            else if (deltaX < -SwipeThreshold) GoTo(currentIndex - 1);
        }
    }
}
