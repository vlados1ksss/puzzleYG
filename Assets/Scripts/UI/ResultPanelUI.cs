using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CityPuzzle.Core;
using CityPuzzle.Services;

namespace CityPuzzle.UI
{
    // The single "liquid glass" card shown after a puzzle is solved. Content depends on whether
    // this is the level's first-ever completion (intro text + quiz) or a replay (stars only).
    public class ResultPanelUI : MonoBehaviour
    {
        public Text timeText;
        public Text difficultyText;

        public GameObject firstTimeBody;
        public Text introText;

        public GameObject repeatBody;
        public Text repeatTitleText;
        public StarsDisplay repeatStars;

        public GameObject buttonsRow;
        public Button menuButton;
        public Button nextButton;

        public void WireButtons(Action onMenu, Action onNext)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(() => onMenu?.Invoke());
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => onNext?.Invoke());
        }

        public void ShowFirstTime(float time, string difficultyLabel)
        {
            SetHeader(time, difficultyLabel);
            firstTimeBody.SetActive(true);
            repeatBody.SetActive(false);
            buttonsRow.SetActive(false);
            introText.text = Loc.T("Уровень пройден! Сможете отгадать, какой город изображён на фото?",
                "Level complete! Can you guess which city is in the photo?");
        }

        public void RevealButtons() => buttonsRow.SetActive(true);

        public void ShowRepeat(float time, string difficultyLabel, int starCount, bool bonus)
        {
            SetHeader(time, difficultyLabel);
            firstTimeBody.SetActive(false);
            repeatBody.SetActive(true);
            buttonsRow.SetActive(false);
            repeatTitleText.text = Loc.T("Уровень пройден!", "Level complete!");
            StopAllCoroutines();
            StartCoroutine(RevealStarsThenButtons(starCount, bonus));
        }

        IEnumerator RevealStarsThenButtons(int starCount, bool bonus)
        {
            yield return repeatStars.AnimateReveal(starCount, bonus);
            yield return new WaitForSecondsRealtime(0.2f);
            buttonsRow.SetActive(true);
        }

        void SetHeader(float time, string difficultyLabel)
        {
            timeText.text = Loc.T($"Время: {Timer.Format(time)}", $"Time: {Timer.Format(time)}");
            difficultyText.text = difficultyLabel;
        }
    }
}
