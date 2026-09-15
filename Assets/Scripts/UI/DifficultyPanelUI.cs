using System;
using UnityEngine;
using UnityEngine.UI;
using CityPuzzle.Core;
using CityPuzzle.Services;

namespace CityPuzzle.UI
{
    public class DifficultyPanelUI : MonoBehaviour
    {
        public Text levelTitleText;
        public DifficultyOptionRow[] rows; // Easy, Medium, Hard, in that order
        public Button closeButton;

        public void Show(int levelIndex, Action<Difficulty> onPlay, Action onClose)
        {
            levelTitleText.text = Loc.Level(levelIndex + 1);
            for (int i = 0; i < rows.Length; i++)
            {
                var difficulty = (Difficulty)i;
                rows[i].nameText.text = DifficultyInfo.DisplayName(difficulty);
                rows[i].pieceCountText.text = Loc.Pieces(DifficultyInfo.PieceCount(difficulty));
                float best = SaveService.GetBestTime(levelIndex, difficulty);
                rows[i].bestTimeText.text = best >= 0f ? Timer.Format(best) : "—";
                rows[i].playButton.onClick.RemoveAllListeners();
                rows[i].playButton.onClick.AddListener(() => onPlay?.Invoke(difficulty));
            }
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => onClose?.Invoke());
        }
    }
}
