using UnityEngine;
using CityPuzzle.UI;
using CityPuzzle.Puzzle;
using CityPuzzle.Services;

namespace CityPuzzle.Core
{
    public class GameManager : MonoBehaviour
    {
        public UIManager uiManager;
        public LevelManager levelManager;
        public PuzzleGenerator puzzleGenerator;
        public QuizManager quizManager;
        public ZoomPanController zoomPanController;

        public LevelSelectUI levelSelectUI;
        public DifficultyPanelUI difficultyPanelUI;
        public GameplayUI gameplayUI;
        public WinPanelUI winPanelUI;
        public Timer gameTimer;

        int currentLevelIndex;
        Difficulty currentDifficulty;
        PuzzleBoard currentBoard;

        void Start()
        {
            var sdk = YandexSDKManager.Instance;
            if (sdk != null) sdk.OnSdkReady += HandleSdkReady;
            ShowMainMenu();
        }

        void HandleSdkReady()
        {
            YandexSDKManager.Instance.LoadProgress();
        }

        public void ShowMainMenu()
        {
            uiManager.ShowMainMenu();
        }

        public void OpenLevelSelect()
        {
            levelSelectUI.onLevelSelected = OpenDifficultyPicker;
            levelSelectUI.Refresh();
            uiManager.ShowLevelSelect();
        }

        void OpenDifficultyPicker(int levelIndex)
        {
            currentLevelIndex = levelIndex;
            LevelData level = levelManager.GetLevel(levelIndex);
            difficultyPanelUI.Show(levelIndex, level.cityName, OnDifficultyChosen, () => uiManager.HideDifficulty());
            uiManager.ShowDifficulty();
        }

        void OnDifficultyChosen(Difficulty difficulty)
        {
            uiManager.HideDifficulty();
            StartLevel(currentLevelIndex, difficulty);
        }

        void StartLevel(int index, Difficulty difficulty)
        {
            currentLevelIndex = index;
            currentDifficulty = difficulty;
            LevelData level = levelManager.GetLevel(index);

            gameplayUI.SetLevelTitle(index, difficulty);
            gameplayUI.Bind(gameTimer, OnBackFromGameplay);
            uiManager.ShowGameplay();

            if (currentBoard != null) currentBoard.OnCompleted -= HandlePuzzleCompleted;
            currentBoard = puzzleGenerator.Build(level, difficulty, index);
            currentBoard.OnCompleted += HandlePuzzleCompleted;

            if (zoomPanController != null) zoomPanController.ResetView(0.85f, Vector2.zero);
            gameTimer.StartTimer();
        }

        public void ResetGameplayView()
        {
            if (zoomPanController != null) zoomPanController.ResetView(0.85f, Vector2.zero);
        }

        void OnBackFromGameplay()
        {
            gameTimer.StopTimer();
            puzzleGenerator.Clear();
            OpenLevelSelect();
        }

        void HandlePuzzleCompleted()
        {
            gameTimer.StopTimer();
            float finalTime = gameTimer.Elapsed;
            SaveService.SetBestTime(currentLevelIndex, currentDifficulty, finalTime);
            float best = SaveService.GetBestTime(currentLevelIndex, currentDifficulty);

            var sdk = YandexSDKManager.Instance;
            if (sdk != null) sdk.SubmitLeaderboardScore($"level_{currentLevelIndex + 1}_{currentDifficulty}_time", Mathf.RoundToInt(finalTime));

            winPanelUI.Show(finalTime, best, OnWinContinue);
            uiManager.ShowWin();
        }

        void OnWinContinue()
        {
            uiManager.HideWin();
            LevelData level = levelManager.GetLevel(currentLevelIndex);
            quizManager.StartQuiz(level, OnQuizResolved);
            uiManager.ShowQuiz();
        }

        void OnQuizResolved(bool unlocked)
        {
            uiManager.HideQuiz();
            puzzleGenerator.Clear();
            if (unlocked) levelManager.UnlockLevel(currentLevelIndex);

            var sdk = YandexSDKManager.Instance;
            if (sdk != null) sdk.SaveProgress();

            OpenLevelSelect();
        }
    }
}
