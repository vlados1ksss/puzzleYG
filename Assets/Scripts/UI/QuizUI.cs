using UnityEngine;
using UnityEngine.UI;

namespace CityPuzzle.UI
{
    public class QuizUI : MonoBehaviour
    {
        public Text questionText;
        public Button[] answerButtons;
        public Text[] answerTexts;
        public Text feedbackText;
        public Button watchAdButton;
        public GameObject watchAdContainer;

        // Captured once at build time so ResetView can restore it instead of guessing a color.
        public Color answerBaseColor = Color.white;

        public void ResetView()
        {
            feedbackText.text = "";
            watchAdContainer.SetActive(false);
            foreach (var b in answerButtons)
            {
                b.gameObject.SetActive(true);
                b.transition = Selectable.Transition.ColorTint;
                b.interactable = true;
                var img = b.GetComponent<Image>();
                if (img != null) img.color = answerBaseColor;
            }
        }
    }
}
