using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityPuzzle.Services
{
    // Two-language (RU/EN) text switch. The language comes from the Yandex SDK (environment.i18n.lang):
    // Russian-speaking locales get Russian, everything else gets English. Russian is the default until detected.
    public static class Loc
    {
        static readonly HashSet<string> RussianLocales = new HashSet<string> { "ru", "be", "kk", "uk", "uz" };

        public static bool IsRussian { get; private set; } = true;

        public static void SetLanguage(string langCode)
        {
            string code = string.IsNullOrEmpty(langCode) ? "ru" : langCode.ToLowerInvariant();
            IsRussian = RussianLocales.Contains(code);
            Debug.Log($"[Loc] SDK language '{langCode}' -> {(IsRussian ? "ru" : "en")}");
            if (!IsRussian) TranslateSceneTexts();
        }

        public static string T(string ru, string en) => IsRussian ? ru : en;

        public static string Level(int number) => T($"Уровень {number}", $"Level {number}");

        public static string Pieces(int count) =>
            IsRussian ? $"{count} {RussianPlural(count, "деталь", "детали", "деталей")}" : $"{count} pieces";

        // Level assets store city names in Russian; they double as the lookup key here.
        public static string City(string russianName) =>
            IsRussian || !CityNamesEn.TryGetValue(russianName, out var en) ? russianName : en;

        static string RussianPlural(int n, string one, string few, string many)
        {
            int mod100 = n % 100, mod10 = n % 10;
            if (mod100 >= 11 && mod100 <= 14) return many;
            if (mod10 == 1) return one;
            if (mod10 >= 2 && mod10 <= 4) return few;
            return many;
        }

        // Labels authored in Russian directly in the scene. Labels filled from code use T() instead.
        static void TranslateSceneTexts()
        {
            foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (!text.gameObject.scene.IsValid()) continue; // skip prefab/asset instances
                if (SceneTextsEn.TryGetValue(text.text, out var en)) text.text = en;
            }
        }

        static readonly Dictionary<string, string> CityNamesEn = new Dictionary<string, string>
        {
            { "Москва", "Moscow" },
            { "Париж", "Paris" },
            { "Санкт-Петербург", "Saint Petersburg" },
            { "Нью-Йорк", "New York" },
            { "Рио-де-Жанейро", "Rio de Janeiro" },
            { "Белград", "Belgrade" },
            { "Прага", "Prague" },
            { "Пекин", "Beijing" },
            { "Сеул", "Seoul" },
            { "Загреб", "Zagreb" },
        };

        static readonly Dictionary<string, string> SceneTextsEn = new Dictionary<string, string>
        {
            { "Пазлы\nс городами", "City\nPuzzles" },
            { "10 городов · 3 уровня сложности", "10 cities · 3 difficulty levels" },
            { "Играть", "Play" },
            { "Назад", "Back" },
            { "Меню", "Menu" },
            { "Следующий ->", "Next ->" },
            { "⟲ Вид", "⟲ View" },
            { "Город", "City" },
            { "Уровень 1", "Level 1" },
            { "Лёгкий", "Easy" },
            { "12 деталей", "12 pieces" },
            { "Лёгкий · 12 деталей", "Easy · 12 pieces" },
            { "Рекорд: —", "Best: —" },
            { "Время: 00:00", "Time: 00:00" },
            { "Пройдите предыдущий уровень", "Complete the previous level" },
            { "Больше уровней -\nскоро", "More levels\ncoming soon" },
            { "Уровень пройден!", "Level complete!" },
            { "Уровень пройден! Сможете отгадать, какой город изображён на фото?", "Level complete! Can you guess which city is in the photo?" },
            { "Какой город изображён на собранном пазле?", "Which city is shown in the puzzle?" },
            { "Вариант 1", "Option 1" },
            { "Вариант 2", "Option 2" },
            { "Вариант 3", "Option 3" },
            { "Вариант 4", "Option 4" },
        };
    }
}
