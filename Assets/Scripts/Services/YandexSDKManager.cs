using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace CityPuzzle.Services
{
    // Thin wrapper around the Yandex Games SDK JS bridge (Assets/Plugins/WebGL/YandexSDK.jslib).
    // Outside WebGL builds it falls back to local stubs so the game is fully playable in the Editor.
    public class YandexSDKManager : MonoBehaviour
    {
        public static YandexSDKManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }
        public event Action OnSdkReady;

        Action<bool> pendingRewardedCallback;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void YG_Initialize(string gameObjectName);
        [DllImport("__Internal")] static extern void YG_ShowFullscreenAd();
        [DllImport("__Internal")] static extern void YG_ShowRewardedAd();
        [DllImport("__Internal")] static extern void YG_SaveData(string json);
        [DllImport("__Internal")] static extern void YG_LoadData();
        [DllImport("__Internal")] static extern void YG_SubmitScore(string leaderboardName, int score);
        [DllImport("__Internal")] static extern void YG_GameReady();
#endif

        [Serializable]
        class CloudSaveData
        {
            public int unlocked;
            public float[] bestTimes;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YG_Initialize(gameObject.name);
#else
            StartCoroutine(SimulateInit());
#endif
        }

        IEnumerator SimulateInit()
        {
            yield return new WaitForSeconds(0.2f);
            Debug.Log("[YandexSDK-Stub] SDK initialized (editor/standalone stub).");
            OnSdkInitialized(null);
        }

        // Called by the JS bridge via SendMessage(gameObject.name, "OnSdkInitialized", "") once ysdk.init() resolves.
        public void OnSdkInitialized(string _)
        {
            IsInitialized = true;
            NotifyGameReady();
            OnSdkReady?.Invoke();
        }

        public void NotifyGameReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YG_GameReady();
#endif
        }

        const int LevelCount = 10;

        public void SaveProgress()
        {
            var sb = new StringBuilder();
            sb.Append("{\"unlocked\":").Append(SaveService.GetUnlockedCount()).Append(",\"bestTimes\":[");
            bool first = true;
            for (int i = 0; i < LevelCount; i++)
            {
                for (int d = 0; d < Core.DifficultyInfo.Count; d++)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(SaveService.GetBestTime(i, (Core.Difficulty)d).ToString(CultureInfo.InvariantCulture));
                }
            }
            sb.Append("]}");
#if UNITY_WEBGL && !UNITY_EDITOR
            YG_SaveData(sb.ToString());
#else
            Debug.Log("[YandexSDK-Stub] SaveProgress: " + sb);
#endif
        }

        public void LoadProgress()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YG_LoadData();
#else
            Debug.Log("[YandexSDK-Stub] LoadProgress: no cloud in editor, using local PlayerPrefs.");
#endif
        }

        // Called by the JS bridge with the player's cloud save data (JSON) or "null" if none exists yet.
        public void OnDataLoaded(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null") return;
            try
            {
                var data = JsonUtility.FromJson<CloudSaveData>(json);
                if (data == null) return;
                SaveService.SetUnlockedCount(data.unlocked);
                if (data.bestTimes != null)
                {
                    int perLevel = Core.DifficultyInfo.Count;
                    for (int flat = 0; flat < data.bestTimes.Length; flat++)
                        SaveService.SetBestTime(flat / perLevel, (Core.Difficulty)(flat % perLevel), data.bestTimes[flat]);
                }
                Debug.Log("[YandexSDK] Cloud save merged.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[YandexSDK] Failed to parse cloud save: " + e.Message);
            }
        }

        public void ShowInterstitial()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PauseGame(true);
            YG_ShowFullscreenAd();
#else
            Debug.Log("[YandexSDK-Stub] Showing interstitial ad (stub, no-op).");
#endif
        }

        // Called by the JS bridge when the interstitial ad closes.
        public void OnInterstitialClosed(string _)
        {
            PauseGame(false);
        }

        public void ShowRewardedAd(Action<bool> onResult)
        {
            pendingRewardedCallback = onResult;
#if UNITY_WEBGL && !UNITY_EDITOR
            PauseGame(true);
            YG_ShowRewardedAd();
#else
            StartCoroutine(SimulateRewardedAd());
#endif
        }

        IEnumerator SimulateRewardedAd()
        {
            PauseGame(true);
            Debug.Log("[YandexSDK-Stub] Simulating rewarded ad playback...");
            yield return new WaitForSecondsRealtime(1.5f);
            OnRewardedAdClosed("success");
        }

        // Called by the JS bridge with "success" or "fail" once the rewarded ad flow ends.
        public void OnRewardedAdClosed(string result)
        {
            PauseGame(false);
            var cb = pendingRewardedCallback;
            pendingRewardedCallback = null;
            cb?.Invoke(result == "success");
        }

        public void SubmitLeaderboardScore(string leaderboardName, int score)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YG_SubmitScore(leaderboardName, score);
#else
            Debug.Log($"[YandexSDK-Stub] SubmitLeaderboardScore({leaderboardName}, {score})");
#endif
        }

        void PauseGame(bool pause)
        {
            Time.timeScale = pause ? 0f : 1f;
            AudioListener.pause = pause;
        }
    }
}
