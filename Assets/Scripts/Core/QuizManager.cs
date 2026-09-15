using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CityPuzzle.UI;
using CityPuzzle.Services;

namespace CityPuzzle.Core
{
    // Pure question/answer controller — reports the result and lets the caller (GameManager)
    // decide what happens next (unlock, ad, navigation). Knows nothing about ads or screens.
    public class QuizManager : MonoBehaviour
    {
        public QuizUI ui;

        static readonly Color CorrectColor = new Color(0.22f, 0.70f, 0.45f);
        static readonly Color WrongColor = new Color(0.90f, 0.32f, 0.36f);

        Action<bool> onResolved;
        string correctCityName;
        bool pendingCorrect;

        public void StartQuiz(LevelData level, Action<bool> resolved)
        {
            onResolved = resolved;
            correctCityName = Loc.City(level.cityName);
            ui.ResetView();
            ui.questionText.text = Loc.T("Какой город изображён на собранном пазле?", "Which city is shown in the puzzle?");

            var options = new List<string> { correctCityName };
            var pool = new List<string>(level.wrongAnswerPool);
            Shuffle(pool);
            for (int i = 0; i < pool.Count && options.Count < 4; i++)
                options.Add(Loc.City(pool[i]));
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
        }

        void OnAnswer(string chosen, int buttonIndex)
        {
            bool correct = chosen == correctCityName;
            pendingCorrect = correct;

            foreach (var b in ui.answerButtons)
            {
                // transition=None so the built-in disabled-state color tint can't stomp our feedback colors.
                b.transition = Selectable.Transition.None;
                b.interactable = false;
            }
            var chosenImage = ui.answerButtons[buttonIndex].GetComponent<Image>();
            if (chosenImage != null) chosenImage.color = correct ? CorrectColor : WrongColor;

            if (correct)
            {
                ui.feedbackText.text = Loc.T("Верно!", "Correct!");
            }
            else
            {
                HighlightCorrectAnswer();
                ui.feedbackText.text = Loc.T($"Неверно. Правильный ответ: {correctCityName}", $"Wrong. The correct answer is {correctCityName}");
            }

            Invoke(nameof(Resolve), 1.1f);
        }

        void Resolve()
        {
            onResolved?.Invoke(pendingCorrect);
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
