using System;
using CityPuzzle.Core;

namespace CityPuzzle.UI
{
    public class LevelSelectUI : UnityEngine.MonoBehaviour
    {
        public LevelButton[] buttons;
        public LevelManager levelManager;

        public Action<int> onLevelSelected;

        public void Refresh()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                bool unlocked = levelManager.IsUnlocked(index);
                string name = levelManager.GetLevel(index).cityName;
                buttons[i].Setup(index, name, unlocked, () => onLevelSelected?.Invoke(index));
            }
        }
    }
}
