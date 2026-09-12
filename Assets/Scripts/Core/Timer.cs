using UnityEngine;

namespace CityPuzzle.Core
{
    public class Timer : MonoBehaviour
    {
        public float Elapsed { get; private set; }
        public bool IsRunning { get; private set; }

        void Update()
        {
            if (IsRunning) Elapsed += Time.deltaTime;
        }

        public void StartTimer()
        {
            Elapsed = 0f;
            IsRunning = true;
        }

        public void StopTimer()
        {
            IsRunning = false;
        }

        public static string Format(float seconds)
        {
            if (seconds < 0f) return "--:--";
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:00}:{s:00}";
        }
    }
}
