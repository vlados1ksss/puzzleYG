using System;
using UnityEngine;
using UnityEngine.UI;

namespace CityPuzzle.UI
{
    public class LevelButton : MonoBehaviour
    {
        public Button button;
        public Text levelNumberText;
        public Text cityNameText;
        public GameObject lockOverlay;

        public void Setup(int levelIndex, string cityName, bool unlocked, Action onClick)
        {
            levelNumberText.text = (levelIndex + 1).ToString();
            cityNameText.text = unlocked ? cityName : "???";
            lockOverlay.SetActive(!unlocked);
            button.interactable = unlocked;
            button.onClick.RemoveAllListeners();
            if (unlocked && onClick != null) button.onClick.AddListener(() => onClick());
        }
    }
}
