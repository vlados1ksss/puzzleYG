using System;
using UnityEngine;
using UnityEngine.UI;
using CityPuzzle.Core;
using CityPuzzle.Services;

namespace CityPuzzle.UI
{
    public class GameplayUI : MonoBehaviour
    {
        public Text levelTitleText;
        public Text timerText;
        public Button backButton;

        Timer boundTimer;

        public void Bind(Timer gameTimer, Action onBack)
        {
            boundTimer = gameTimer;
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => onBack?.Invoke());
        }

        public void SetLevelTitle(int levelIndex, Difficulty difficulty)
        {
            levelTitleText.text = $"{Loc.Level(levelIndex + 1)} · {DifficultyInfo.DisplayName(difficulty)}";
        }

        void Update()
        {
            if (boundTimer != null) timerText.text = Timer.Format(boundTimer.Elapsed);
        }
    }
}
