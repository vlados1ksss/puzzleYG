using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityPuzzle.UI
{
    public class UIManager : MonoBehaviour
    {
        public CanvasGroup mainMenuPanel;
        public CanvasGroup levelSelectPanel;
        public CanvasGroup gameplayPanel;
        public CanvasGroup resultPanel;
        public CanvasGroup difficultyPanel;

        const float FadeDuration = 0.25f;

        CanvasGroup currentScreen;
        readonly Dictionary<CanvasGroup, Coroutine> fadeRoutines = new Dictionary<CanvasGroup, Coroutine>();

        void Awake()
        {
            SetImmediate(mainMenuPanel, false);
            SetImmediate(levelSelectPanel, false);
            SetImmediate(gameplayPanel, false);
            SetImmediate(resultPanel, false);
            SetImmediate(difficultyPanel, false);
        }

        public void ShowMainMenu() => SwitchScreen(mainMenuPanel);
        public void ShowLevelSelect() => SwitchScreen(levelSelectPanel);
        public void ShowGameplay() => SwitchScreen(gameplayPanel);

        public void ShowResult() => Fade(resultPanel, true);
        public void HideResult() => Fade(resultPanel, false);
        public void ShowDifficulty() => Fade(difficultyPanel, true);
        public void HideDifficulty() => Fade(difficultyPanel, false);

        void SwitchScreen(CanvasGroup next)
        {
            if (currentScreen == next) return;
            if (currentScreen != null) Fade(currentScreen, false);
            Fade(next, true);
            currentScreen = next;
        }

        void Fade(CanvasGroup panel, bool show)
        {
            if (panel == null) return;
            if (fadeRoutines.TryGetValue(panel, out var running) && running != null) StopCoroutine(running);
            panel.gameObject.SetActive(true);
            fadeRoutines[panel] = StartCoroutine(FadeRoutine(panel, show));
        }

        IEnumerator FadeRoutine(CanvasGroup panel, bool show)
        {
            float start = panel.alpha;
            float end = show ? 1f : 0f;
            panel.interactable = show;
            panel.blocksRaycasts = show;
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                panel.alpha = Mathf.Lerp(start, end, t / FadeDuration);
                yield return null;
            }
            panel.alpha = end;
            if (!show) panel.gameObject.SetActive(false);
        }

        void SetImmediate(CanvasGroup panel, bool show)
        {
            if (panel == null) return;
            panel.alpha = show ? 1f : 0f;
            panel.interactable = show;
            panel.blocksRaycasts = show;
            panel.gameObject.SetActive(show);
        }
    }
}
