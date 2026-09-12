using System;
using UnityEngine.UI;
using CityPuzzle.Core;

namespace CityPuzzle.UI
{
    public class WinPanelUI : UnityEngine.MonoBehaviour
    {
        public Text timeText;
        public Text bestTimeText;
        public Button continueButton;

        public void Show(float time, float bestTime, Action onContinue)
        {
            timeText.text = $"Время: {Timer.Format(time)}";
            bestTimeText.text = $"Лучшее время: {Timer.Format(bestTime)}";
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => onContinue?.Invoke());
        }
    }
}
