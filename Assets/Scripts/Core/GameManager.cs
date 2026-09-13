using System;
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
            if (sdk != null) sdk.OnSdkReady += HandleSdkReady;
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
            bool wasCompletedBefore = SaveService.IsLevelCompleted(currentLevelIndex);
            SaveService.SetBestTime(currentLevelIndex, currentDifficulty, finalTime);

            var sdk = YandexSDKManager.Instance;
            if (sdk != null) sdk.SubmitLeaderboardScore($"level_{currentLevelIndex + 1}_{currentDifficulty}_time", Mathf.RoundToInt(finalTime));

            puzzleGenerator.Clear();
            uiManager.ShowResult();

            string difficultyLabel = $"{DifficultyInfo.DisplayName(currentDifficulty)} · {DifficultyInfo.PieceCount(currentDifficulty)} деталей";

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
            if (correct)
            {
                SaveService.SetQuizBonus(currentLevelIndex);
                levelManager.UnlockLevel(currentLevelIndex);
                if (sdk != null) sdk.SaveProgress();
                resultPanelUI.RevealButtons();
                return;
            }

            Action<bool> afterAd = _ =>
            {
                levelManager.UnlockLevel(currentLevelIndex);
                if (sdk != null) sdk.SaveProgress();
                int target = currentLevelIndex + 1;
                uiManager.HideResult();
                lastViewedLevelIndex = target;
                uiManager.ShowLevelSelect();
                levelCarouselUI.GoTo(target, animateUnlock: true);
            };

            if (sdk != null) sdk.ShowRewardedAd(afterAd);
            else afterAd(true);
        }

        void OnResultMenuClicked()
        {
            int target = currentLevelIndex;
            RunInterstitialThen(() =>
            {
                uiManager.HideResult();
                lastViewedLevelIndex = target;
                OpenLevelSelect();
            });
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
            if (sdk != null) sdk.ShowInterstitial(() => then());
            else then();
        }
    }
}
