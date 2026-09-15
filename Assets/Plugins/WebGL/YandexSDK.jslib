// JS bridge between CityPuzzle.Services.YandexSDKManager (C#) and the Yandex Games SDK.
// SDK init, LoadingAPI.ready() and ads are owned by Plugin Your Games 2: its WebGL template
// (Assets/WebGLTemplates/YandexGames/index.html) calls YaGames.init() once and stores the result in the
// global `ysdk`. These functions reuse that instance — never call YaGames.init() here a second time.
mergeInto(LibraryManager.library, {

  // Interface language of the Yandex Games portal (environment.i18n.lang), e.g. "ru", "en", "tr". Empty if unknown.
  YG_GetLang: function () {
    var lang = '';
    try {
      if (typeof ysdk !== 'undefined' && ysdk !== null && ysdk.environment && ysdk.environment.i18n)
        lang = ysdk.environment.i18n.lang || '';
    } catch (e) {
      console.warn('[YandexSDK] Failed to read language', e);
    }
    var size = lengthBytesUTF8(lang) + 1;
    var buffer = _malloc(size);
    stringToUTF8(lang, buffer, size);
    return buffer;
  },

  YG_SaveData: function (jsonPtr) {
    if (typeof ysdk === 'undefined' || ysdk === null) return;
    var json = UTF8ToString(jsonPtr);
    ysdk.getPlayer().then(function (player) {
      return player.setData(JSON.parse(json), true);
    }).catch(function (err) { console.error('[YandexSDK] SaveData failed', err); });
  },

  YG_LoadData: function (gameObjectNamePtr) {
    var goName = UTF8ToString(gameObjectNamePtr);
    if (typeof ysdk === 'undefined' || ysdk === null) { SendMessage(goName, 'OnDataLoaded', 'null'); return; }

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
    if (typeof ysdk === 'undefined' || ysdk === null) return;
    var name = UTF8ToString(leaderboardNamePtr);
    ysdk.getLeaderboards().then(function (lb) {
      return lb.setLeaderboardScore(name, score);
    }).catch(function (err) {
      console.warn('[YandexSDK] SubmitScore failed (leaderboard "' + name + '" may not be configured)', err);
    });
  }

});
