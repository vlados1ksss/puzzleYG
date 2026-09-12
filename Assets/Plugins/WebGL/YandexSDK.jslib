// JS bridge between CityPuzzle.Services.YandexSDKManager (C#) and the Yandex Games SDK (window.ysdk).
// Loaded automatically by Unity WebGL builds. The actual `YaGames` SDK script is included by
// Assets/WebGLTemplates/YandexGames/index.html, which must be selected under
// Player Settings > Resolution and Presentation > WebGL Template.
mergeInto(LibraryManager.library, {

  YG_Initialize: function (gameObjectNamePtr) {
    var goName = UTF8ToString(gameObjectNamePtr);
    window.__cpGameObject = goName;

    if (typeof YaGames === 'undefined') {
      console.warn('[YandexSDK] YaGames SDK script not found — check the WebGL template.');
      return;
    }

    YaGames.init().then(function (ysdk) {
      window.__cpYsdk = ysdk;
      SendMessage(goName, 'OnSdkInitialized', '');
    }).catch(function (err) {
      console.error('[YandexSDK] init() failed', err);
    });
  },

  // Tell the platform loading screen the game is visible and interactive (ysdk.features.LoadingAPI.ready()).
  YG_GameReady: function () {
    var ysdk = window.__cpYsdk;
    if (ysdk && ysdk.features && ysdk.features.LoadingAPI) {
      ysdk.features.LoadingAPI.ready();
    }
  },

  YG_ShowFullscreenAd: function () {
    var goName = window.__cpGameObject;
    var ysdk = window.__cpYsdk;
    if (!ysdk || !ysdk.adv) { SendMessage(goName, 'OnInterstitialClosed', ''); return; }

    ysdk.adv.showFullscreenAdv({
      callbacks: {
        onClose: function () { SendMessage(goName, 'OnInterstitialClosed', ''); },
        onError: function (err) {
          console.warn('[YandexSDK] Interstitial error', err);
          SendMessage(goName, 'OnInterstitialClosed', '');
        }
      }
    });
  },

  YG_ShowRewardedAd: function () {
    var goName = window.__cpGameObject;
    var ysdk = window.__cpYsdk;
    if (!ysdk || !ysdk.adv) { SendMessage(goName, 'OnRewardedAdClosed', 'fail'); return; }

    var rewarded = false;
    ysdk.adv.showRewardedVideo({
      callbacks: {
        onRewarded: function () { rewarded = true; },
        onClose: function () { SendMessage(goName, 'OnRewardedAdClosed', rewarded ? 'success' : 'fail'); },
        onError: function (err) {
          console.warn('[YandexSDK] Rewarded ad error', err);
          SendMessage(goName, 'OnRewardedAdClosed', 'fail');
        }
      }
    });
  },

  YG_SaveData: function (jsonPtr) {
    var ysdk = window.__cpYsdk;
    if (!ysdk) return;
    var json = UTF8ToString(jsonPtr);
    ysdk.getPlayer().then(function (player) {
      return player.setData(JSON.parse(json), true);
    }).catch(function (err) { console.error('[YandexSDK] SaveData failed', err); });
  },

  YG_LoadData: function () {
    var goName = window.__cpGameObject;
    var ysdk = window.__cpYsdk;
    if (!ysdk) { SendMessage(goName, 'OnDataLoaded', 'null'); return; }

    ysdk.getPlayer().then(function (player) {
      return player.getData();
    }).then(function (data) {
      SendMessage(goName, 'OnDataLoaded', JSON.stringify(data || null));
    }).catch(function (err) {
      console.error('[YandexSDK] LoadData failed', err);
      SendMessage(goName, 'OnDataLoaded', 'null');
    });
  },

  // leaderboardName must already exist in the Yandex Games developer console.
  YG_SubmitScore: function (leaderboardNamePtr, score) {
    var ysdk = window.__cpYsdk;
    if (!ysdk) return;
    var name = UTF8ToString(leaderboardNamePtr);
    ysdk.getLeaderboards().then(function (lb) {
      return lb.setLeaderboardScore(name, score);
    }).catch(function (err) {
      console.warn('[YandexSDK] SubmitScore failed (leaderboard "' + name + '" may not be configured)', err);
    });
  }

});
