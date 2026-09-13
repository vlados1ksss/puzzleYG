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

        // A level counts as completed once any difficulty has a recorded best time.
        public static bool IsLevelCompleted(int levelIndex)
        {
            for (int d = 0; d < DifficultyInfo.Count; d++)
                if (GetBestTime(levelIndex, (Difficulty)d) >= 0f) return true;
            return false;
        }

        // Best time across whichever difficulties have been completed, or -1 if none.
        public static float GetOverallBestTime(int levelIndex)
        {
            float best = -1f;
            for (int d = 0; d < DifficultyInfo.Count; d++)
            {
                float t = GetBestTime(levelIndex, (Difficulty)d);
                if (t >= 0f && (best < 0f || t < best)) best = t;
            }
            return best;
        }

        // 1/2/3 base stars for the hardest difficulty (Easy/Medium/Hard) ever completed.
        public static int GetStarRating(int levelIndex)
        {
            int stars = 0;
            for (int d = 0; d < DifficultyInfo.Count; d++)
                if (GetBestTime(levelIndex, (Difficulty)d) >= 0f) stars = Mathf.Max(stars, d + 1);
            return stars;
        }

        const string QuizBonusKeyPrefix = "cp_quiz_bonus_";

        public static bool HasQuizBonus(int levelIndex) => PlayerPrefs.GetInt(QuizBonusKeyPrefix + levelIndex, 0) == 1;

        public static void SetQuizBonus(int levelIndex)
        {
            PlayerPrefs.SetInt(QuizBonusKeyPrefix + levelIndex, 1);
            PlayerPrefs.Save();
        }
    }
}
