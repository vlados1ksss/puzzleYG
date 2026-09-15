using System;
using UnityEngine;
using CityPuzzle.UI;
using CityPuzzle.Puzzle;
using CityPuzzle.Services;
using YG;


namespace CityPuzzle.Core
{
    public class GameManager : MonoBehaviour
    {
        public UIManager uiManager;
        public LevelManager levelManager;
        public PuzzleGenerator puzzleGenerator;
        public QuizManager quizManager;
        public ZoomPanController zoomPanController;

        public LevelCarouselUI levelCarouselUI;
        public DifficultyPanelUI difficultyPanelUI;
        public GameplayUI gameplayUI;
        public ResultPanelUI resultPanelUI;
        public Timer gameTimer;

        int currentLevelIndex;
        Difficulty currentDifficulty;
        PuzzleBoard currentBoard;
        int lastViewedLevelIndex;

        void Start()
        {
            var sdk = YandexSDKManager.Instance;
            if (sdk != null) sdk.WhenReady(HandleSdkReady);
            levelCarouselUI.onPlayRequested += OpenDifficultyPicker;
            resultPanelUI.WireButtons(OnResultMenuClicked, OnResultNextClicked);
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
            uiManager.ShowLevelSelect();
            levelCarouselUI.Open(lastViewedLevelIndex);
        }

        void OpenDifficultyPicker(int levelIndex)
        {
            lastViewedLevelIndex = levelIndex;
            currentLevelIndex = levelIndex;
            difficultyPanelUI.Show(levelIndex, OnDifficultyChosen, () => uiManager.HideDifficulty());
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
            lastViewedLevelIndex = index;
            LevelData level = levelManager.GetLevel(index);

            gameplayUI.SetLevelTitle(index, difficulty);
            gameplayUI.Bind(gameTimer, OnBackFromGameplay);
            uiManager.ShowGameplay();

            if (currentBoard != null) currentBoard.OnCompleted -= HandlePuzzleCompleted;
            currentBoard = puzzleGenerator.Build(level, difficulty, index);
            currentBoard.OnCompleted += HandlePuzzleCompleted;

            if (zoomPanController != null) zoomPanController.ResetView(0.85f, Vector2.zero);
            gameTimer.StartTimer();
            YG2.GameplayStart();
        }

        public void ResetGameplayView()
        {
            if (zoomPanController != null) zoomPanController.ResetView(0.85f, Vector2.zero);
        }

        void OnBackFromGameplay()
        {
            YG2.GameplayStop();
            gameTimer.StopTimer();
            puzzleGenerator.Clear();
            OpenLevelSelect();
        }

        void HandlePuzzleCompleted()
        {
            YG2.GameplayStop();
            gameTimer.StopTimer();
            float finalTime = gameTimer.Elapsed;
            bool wasCompletedBefore = SaveService.IsLevelCompleted(currentLevelIndex);
            SaveService.SetBestTime(currentLevelIndex, currentDifficulty, finalTime);

            var sdk = YandexSDKManager.Instance;
            if (sdk != null) sdk.SubmitLeaderboardScore($"level_{currentLevelIndex + 1}_{currentDifficulty}_time", Mathf.RoundToInt(finalTime));

            puzzleGenerator.Clear();
            uiManager.ShowResult();

            string difficultyLabel = $"{DifficultyInfo.DisplayName(currentDifficulty)} · {Loc.Pieces(DifficultyInfo.PieceCount(currentDifficulty))}";

            if (!wasCompletedBefore)
            {
                LevelData level = levelManager.GetLevel(currentLevelIndex);
                resultPanelUI.ShowFirstTime(finalTime, difficultyLabel);
                quizManager.StartQuiz(level, OnQuizAnswered);
            }
            else
            {
                int stars = SaveService.GetStarRating(currentLevelIndex);
                bool bonus = SaveService.HasQuizBonus(currentLevelIndex);
                resultPanelUI.ShowRepeat(finalTime, difficultyLabel, stars, bonus);
            }
        }

        void OnQuizAnswered(bool correct)
        {
            var sdk = YandexSDKManager.Instance;
            levelManager.UnlockLevel(currentLevelIndex);
            if (correct) SaveService.SetQuizBonus(currentLevelIndex);
            if (sdk != null) sdk.SaveProgress();

            if (correct)
            {
                resultPanelUI.RevealButtons();
                return;
            }

            // Wrong answer: fullscreen ad first, then straight on to the next level in the carousel.
            int target = currentLevelIndex + 1;
            RunInterstitialThen(() =>
            {
                uiManager.HideResult();
                lastViewedLevelIndex = target;
                uiManager.ShowLevelSelect();
                levelCarouselUI.GoTo(target, animateUnlock: true);
            });
        }

        void OnResultMenuClicked()
        {
            uiManager.HideResult();
            lastViewedLevelIndex = currentLevelIndex;
            OpenLevelSelect();
        }

        void OnResultNextClicked()
        {
            int target = currentLevelIndex + 1;
            RunInterstitialThen(() =>
            {
                uiManager.HideResult();
                lastViewedLevelIndex = target;
                uiManager.ShowLevelSelect();
                levelCarouselUI.Open(target);
            });
        }

        void RunInterstitialThen(Action then)
        {
            var sdk = YandexSDKManager.Instance;
            if (sdk != null) sdk.ShowInterstitial(then);
            else then();
        }
    }
}
