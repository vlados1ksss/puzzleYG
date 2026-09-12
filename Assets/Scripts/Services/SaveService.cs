using UnityEngine;
using CityPuzzle.Core;

namespace CityPuzzle.Services
{
    // Local persistence via PlayerPrefs. YandexSDKManager mirrors this to Yandex cloud saves.
    public static class SaveService
    {
        const string UnlockedKey = "cp_unlocked_levels";
        const string BestTimeKeyPrefix = "cp_best_time_";

        public static int GetUnlockedCount() => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedKey, 1));

        public static void SetUnlockedCount(int count)
        {
            if (count <= GetUnlockedCount()) return;
            PlayerPrefs.SetInt(UnlockedKey, count);
            PlayerPrefs.Save();
        }

        static string BestTimeKey(int levelIndex, Difficulty difficulty) => BestTimeKeyPrefix + levelIndex + "_" + (int)difficulty;

        public static float GetBestTime(int levelIndex, Difficulty difficulty) =>
            PlayerPrefs.GetFloat(BestTimeKey(levelIndex, difficulty), -1f);

        public static void SetBestTime(int levelIndex, Difficulty difficulty, float seconds)
        {
            float current = GetBestTime(levelIndex, difficulty);
            if (current >= 0f && seconds >= current) return;
            PlayerPrefs.SetFloat(BestTimeKey(levelIndex, difficulty), seconds);
            PlayerPrefs.Save();
        }
    }
}
