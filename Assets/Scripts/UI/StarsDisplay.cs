using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CityPuzzle.UI
{
    // Shared by the level carousel card (static display) and the result card (animated reveal).
    public class StarsDisplay : MonoBehaviour
    {
        public Image[] baseStars; // 3: Easy / Medium / Hard
        public Image bonusStar;   // extra star for a correct quiz answer

        static readonly Color Earned = new Color(1f, 0.82f, 0.25f);
        static readonly Color BonusEarned = new Color(0.45f, 0.85f, 1f);
        // Mid-gray (not translucent white) so it stays visible on both the dark carousel card and
        // the light result dialog.
        static readonly Color Unearned = new Color(0.6f, 0.6f, 0.64f, 0.55f);

        public void SetImmediate(int starCount, bool bonus)
        {
            StopAllCoroutines();
            for (int i = 0; i < baseStars.Length; i++)
            {
                baseStars[i].gameObject.SetActive(true);
                baseStars[i].color = i < starCount ? Earned : Unearned;
                baseStars[i].transform.localScale = Vector3.one;
            }
            bonusStar.gameObject.SetActive(bonus);
            bonusStar.color = BonusEarned;
            bonusStar.transform.localScale = Vector3.one;
        }

        public IEnumerator AnimateReveal(int starCount, bool bonus)
        {
            for (int i = 0; i < baseStars.Length; i++)
            {
                baseStars[i].gameObject.SetActive(true);
                baseStars[i].color = Unearned;
            }
            bonusStar.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.15f);

            for (int i = 0; i < starCount && i < baseStars.Length; i++)
            {
                yield return Pop(baseStars[i], Earned);
                yield return new WaitForSecondsRealtime(0.15f);
            }
            if (bonus)
            {
                bonusStar.gameObject.SetActive(true);
                yield return Pop(bonusStar, BonusEarned);
            }
        }

        IEnumerator Pop(Image star, Color color)
        {
            star.color = color;
            const float duration = 0.28f;
            Vector3 from = Vector3.one * 0.2f;
            Vector3 mid = Vector3.one * 1.25f;
            Vector3 end = Vector3.one;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duration;
                star.transform.localScale = p < 0.6f ? Vector3.Lerp(from, mid, p / 0.6f) : Vector3.Lerp(mid, end, (p - 0.6f) / 0.4f);
                yield return null;
            }
            star.transform.localScale = end;
        }
    }
}
