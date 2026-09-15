using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using YG;

namespace CityPuzzle.Services
{
    // Game-side facade over Plugin Your Games 2 (YG2). The plugin owns SDK init, LoadingAPI.ready (autoGRA),
    // pause on focus loss and interstitial ads. Assets/Plugins/WebGL/YandexSDK.jslib only covers what the
    // installed YG2 modules don't: language, cloud saves and leaderboards — reusing the plugin's `ysdk`.
    public class YandexSDKManager : MonoBehaviour
    {
        public static YandexSDKManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }
        event Action onSdkReady;

        // How long to wait for the platform to actually open a requested interstitial before moving on.
        const float InterstitialOpenTimeout = 5f;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern string YG_GetLang();
        [DllImport("__Internal")] static extern void YG_SaveData(string json);
        [DllImport("__Internal")] static extern void YG_LoadData(string gameObjectName);
        [DllImport("__Internal")] static extern void YG_SubmitScore(string leaderboardName, int score);
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
            if (YG2.isSDKEnabled) HandleSdkData();
            else YG2.onGetSDKData += HandleSdkData;
        }

        void OnDestroy()
        {
            YG2.onGetSDKData -= HandleSdkData;
        }

        void HandleSdkData()
        {
            YG2.onGetSDKData -= HandleSdkData;
            if (IsInitialized) return;
            IsInitialized = true;
            Loc.SetLanguage(DetectLanguage());
            var ready = onSdkReady;
            onSdkReady = null;
            ready?.Invoke();
        }

        // Runs the action once the SDK is ready — immediately if it already is.
        public void WhenReady(Action action)
        {
            if (IsInitialized) action?.Invoke();
            else onSdkReady += action;
        }

        static string DetectLanguage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return YG_GetLang();
#elif UNITY_EDITOR
            return YG2.infoYG.Simulation.language;
#else
            return Application.systemLanguage == SystemLanguage.Russian ? "ru" : "en";
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
            YG_LoadData(gameObject.name);
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

        // Shows a YG2 interstitial and calls onDone exactly once when it's over. When no ad can be requested
        // (interval from the YG2 settings not elapsed, another ad open, SDK not ready) onDone runs immediately,
        // because YG2.InterstitialAdvShow() fires no callbacks in those cases. The plugin pauses the game itself.
        public void ShowInterstitial(Action onDone)
        {
            string skipReason = InterstitialSkipReason();
            if (skipReason != null)
            {
                Debug.Log($"[YandexSDK] Interstitial skipped: {skipReason} (isSDKEnabled={YG2.isSDKEnabled}, nowAdsShow={YG2.nowAdsShow}, timerInterAdv={YG2.timerInterAdv:0.0}s)");
                onDone?.Invoke();
                return;
            }

            bool opened = false;
            bool finished = false;

            void Opened() => opened = true;
            void Finish()
            {
                if (finished) return;
                finished = true;
                YG2.onOpenInterAdv -= Opened;
                YG2.onCloseInterAdv -= Finish;
                YG2.onErrorInterAdv -= Finish;
                Debug.Log($"[YandexSDK] Interstitial finished (opened={opened}).");
                onDone?.Invoke();
            }

            Debug.Log("[YandexSDK] Requesting interstitial...");
            YG2.onOpenInterAdv += Opened;
            YG2.onCloseInterAdv += Finish;
            YG2.onErrorInterAdv += Finish;
            YG2.InterstitialAdvShow();
            StartCoroutine(FinishIfNotOpened(() => opened, Finish));
        }

        // null = an ad can be requested; otherwise the reason it can't (logged, never shown to the player).
        static string InterstitialSkipReason()
        {
#if UNITY_EDITOR
            if (!YG2.infoYG.Simulation.enableInterAdv) return "simulation disabled in Simulation settings";
#endif
            if (!YG2.isSDKEnabled) return "YG2 SDK not enabled yet";
            if (YG2.nowAdsShow) return "another ad is already showing";
            if (!YG2.isTimerAdvCompleted) return $"cooldown active, {YG2.timerInterAdv:0.0}s left (InterstitialAdv.interAdvInterval in YG2 settings)";
            return null;
        }

        IEnumerator FinishIfNotOpened(Func<bool> opened, Action finish)
        {
            yield return new WaitForSecondsRealtime(InterstitialOpenTimeout);
            if (!opened())
            {
                Debug.LogWarning($"[YandexSDK] Interstitial did not open within {InterstitialOpenTimeout}s — treating as closed. Check the browser console for '[YandexSDK] Cancel InterAdvShow' or ysdk.adv errors.");
                finish();
            }
        }

        public void SubmitLeaderboardScore(string leaderboardName, int score)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YG_SubmitScore(leaderboardName, score);
#else
            Debug.Log($"[YandexSDK-Stub] SubmitLeaderboardScore({leaderboardName}, {score})");
#endif
        }
    }
}
