namespace CityPuzzle.Core
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public static class DifficultyInfo
    {
        public const int Count = 3;

        public static int Columns(Difficulty d) => d switch
        {
            Difficulty.Easy => 4,
            Difficulty.Medium => 9,
            _ => 14
        };

        public static int Rows(Difficulty d) => d switch
        {
            Difficulty.Easy => 3,
            Difficulty.Medium => 6,
            _ => 9
        };

        public static int PieceCount(Difficulty d) => Columns(d) * Rows(d);

        public static string DisplayName(Difficulty d) => d switch
        {
            Difficulty.Easy => "Лёгкий",
            Difficulty.Medium => "Средний",
            _ => "Сложный"
        };
    }
}
