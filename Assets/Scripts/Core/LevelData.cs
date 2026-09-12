using UnityEngine;

namespace CityPuzzle.Core
{
    [CreateAssetMenu(fileName = "Level", menuName = "CityPuzzle/Level Data")]
    public class LevelData : ScriptableObject
    {
        public string cityName;
        public Sprite cityImage;

        // Curated visually-similar cities used as quiz distractors (3 are picked at random each time).
        public string[] wrongAnswerPool = new string[0];
    }
}
