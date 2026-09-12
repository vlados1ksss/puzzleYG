using System.Collections.Generic;
using UnityEngine;
using CityPuzzle.Services;

namespace CityPuzzle.Core
{
    public class LevelManager : MonoBehaviour
    {
        public List<LevelData> levels = new List<LevelData>();

        public int LevelCount => levels.Count;
        public List<LevelData> AllLevels => levels;

        public LevelData GetLevel(int index) => levels[index];

        public bool IsUnlocked(int index) => index < SaveService.GetUnlockedCount();

        public void UnlockLevel(int completedIndex)
        {
            int newCount = Mathf.Clamp(completedIndex + 2, 1, levels.Count);
            SaveService.SetUnlockedCount(newCount);
        }
    }
}
