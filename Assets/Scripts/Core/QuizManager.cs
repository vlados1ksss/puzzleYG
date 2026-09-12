using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CityPuzzle.UI;
using CityPuzzle.Services;

namespace CityPuzzle.Core
{
    public class QuizManager : MonoBehaviour
    {
        public QuizUI ui;

        static readonly Color CorrectColor = new Color(0.22f, 0.70f, 0.45f);
        static readonly Color WrongColor = new Color(0.90f, 0.32f, 0.36f);

        Action<bool> onResolved;
        string correctCityName;

        public void StartQuiz(LevelData level, Action<bool> resolved)
        {
            onResolved = resolved;
            correctCityName = level.cityName;
            ui.ResetView();
            ui.questionText.text = "Какой город изображён на собранном пазле?";

            var options = new List<string> { level.cityName };
            var pool = new List<string>(level.wrongAnswerPool);
            Shuffle(pool);
            for (int i = 0; i < pool.Count && options.Count < 4; i++)
                options.Add(pool[i]);
            Shuffle(options);

            for (int i = 0; i < ui.answerButtons.Length; i++)
            {
                bool active = i < options.Count;
                ui.answerButtons[i].gameObject.SetActive(active);
                if (!active) continue;
                string option = options[i];
                ui.answerTexts[i].text = option;
                ui.answerButtons[i].onClick.RemoveAllListeners();
                int idx = i;
                ui.answerButtons[i].onClick.AddListener(() => OnAnswer(option, idx));
            }

            ui.watchAdButton.onClick.RemoveAllListeners();
            ui.watchAdButton.onClick.AddListener(OnWatchAdClicked);
        }

        void OnAnswer(string chosen, int buttonIndex)
        {
            bool correct = chosen == correctCityName;
            foreach (var b in ui.answerButtons)
            {
                // transition=None so the built-in disabled-state tint can't stomp our feedback colors.
                b.transition = Selectable.Transition.None;
                b.interactable = false;
            }
            var chosenImage = ui.answerButtons[buttonIndex].GetComponent<Image>();
            if (chosenImage != null) chosenImage.color = correct ? CorrectColor : WrongColor;

            if (correct)
            {
                ui.feedbackText.text = "Верно! Следующий уровень открыт.";
                Invoke(nameof(ResolveCorrect), 1.0f);
            }
            else
            {
                HighlightCorrectAnswer();
                ui.feedbackText.text = $"Неверно. Правильный ответ: {correctCityName}";
                ui.watchAdContainer.SetActive(true);
            }
        }

        void HighlightCorrectAnswer()
        {
            for (int i = 0; i < ui.answerButtons.Length; i++)
            {
                if (!ui.answerButtons[i].gameObject.activeSelf) continue;
                if (ui.answerTexts[i].text == correctCityName)
                {
                    var img = ui.answerButtons[i].GetComponent<Image>();
                    if (img != null) img.color = CorrectColor;
                }
            }
        }

        void ResolveCorrect()
        {
            onResolved?.Invoke(true);
        }

        void OnWatchAdClicked()
        {
            ui.watchAdContainer.SetActive(false);
            var sdk = YandexSDKManager.Instance;
            if (sdk == null) { onResolved?.Invoke(true); return; }
            sdk.ShowRewardedAd(success =>
            {
                if (success)
                {
                    ui.feedbackText.text = "Реклама просмотрена. Уровень открыт!";
                    onResolved?.Invoke(true);
                }
                else
                {
                    ui.feedbackText.text = "Реклама не была досмотрена. Попробуйте ещё раз.";
                    ui.watchAdContainer.SetActive(true);
                }
            });
        }

        void Shuffle(List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
