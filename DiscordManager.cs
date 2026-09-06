using System;
using Il2Cpp;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedrunToolkitMod
{
    public class DiscordManager : IDisposable
    {
        private DiscordRpc _rpc;
        private float _lastUpdate;

        public DiscordManager()
        {
            _rpc = new DiscordRpc();
            _rpc.TryConnect();
        }

        public void Update()
        {
            if (_rpc == null) return;

            _rpc.TryConnect();

            if (Time.time - _lastUpdate >= 3.0f)
            {
                _lastUpdate = Time.time;
                UpdatePresence();
            }

            _rpc.Pump();
        }

        private string _lastMapName = "";
        private long _cachedStartTimestamp = 0;

        private void UpdatePresence()
        {
            string unityScene = SceneManager.GetActiveScene().name;

            if (string.IsNullOrEmpty(unityScene) || unityScene.ToLower().Contains("menu") || unityScene.ToLower().Contains("init"))
            {
                _lastMapName = "";
                _cachedStartTimestamp = 0;

                _rpc.SetActivity(new DiscordActivity
                {
                    Details = "In Main Menu",
                    State = "Browsing maps",
                    LargeImage = DiscordRpc.IMG_LARGE,
                    LargeText = "Run Pro",
                    SmallImage = DiscordRpc.IMG_SMALL_MENU,
                    SmallText = "Main Menu"
                });
                return;
            }

            string mapName = GetMapName(unityScene);
            string pbTime = GetPBTime();
            long currentRunStart = GetRunStartTime();

            // Запоминаем timestamp и обновляем его ТОЛЬКО если сменилась карта 
            // или если игрок вручную перезапустил забег (разница более 2 секунд)
            if (_lastMapName != mapName || _cachedStartTimestamp == 0 || Math.Abs(currentRunStart - _cachedStartTimestamp) > 2)
            {
                _lastMapName = mapName;
                _cachedStartTimestamp = currentRunStart > 0 ? currentRunStart : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            var activity = new DiscordActivity
            {
                Details = $"Map: {mapName}",
                State = $"PB: {pbTime}",
                StartTimestamp = _cachedStartTimestamp, // Передаем жестко закэшированный timestamp
                LargeImage = DiscordRpc.IMG_LARGE,
                LargeText = "Run Pro",
                SmallImage = DiscordRpc.IMG_SMALL_PLAYING,
                SmallText = "Speedrunning"
            };

            _rpc.SetActivity(activity);
        }

        private string GetMapName(string fallback)
        {
            try
            {
                // 1. Пробуем прочитать InviteUI.RoomName (игра всегда хранит там имя текущей комнаты/карты)
                if (!string.IsNullOrEmpty(InviteUI.RoomName))
                {
                    return InviteUI.RoomName;
                }

                // 2. Ищем ScoreBoardCanvas, включая скрытые на сцене объекты
                var scoreBoards = Resources.FindObjectsOfTypeAll<ScoreBoardCanvas>();
                if (scoreBoards != null && scoreBoards.Length > 0)
                {
                    var sb = scoreBoards[0];
                    if (sb.m_map != null && !string.IsNullOrEmpty(sb.m_map.text))
                    {
                        return sb.m_map.text;
                    }
                }
            }
            catch { }

            return fallback;
        }

        private string GetPBTime()
        {
            try
            {
                // Ищем ScoreBoardCanvas среди всех (включая неактивные) объектов
                var scoreBoards = Resources.FindObjectsOfTypeAll<ScoreBoardCanvas>();
                if (scoreBoards != null && scoreBoards.Length > 0)
                {
                    var sb = scoreBoards[0];
                    if (sb.playerLine != null && sb.playerLine.bestTime != null)
                    {
                        string best = sb.playerLine.bestTime.text;
                        if (!string.IsNullOrWhiteSpace(best))
                        {
                            return best;
                        }
                    }
                }
            }
            catch { }

            return "--:--.---";
        }

        private long GetRunStartTime()
        {
            try
            {
                var timeManagers = Resources.FindObjectsOfTypeAll<TimeManager>();
                if (timeManagers != null)
                {
                    foreach (var tm in timeManagers)
                    {
                        // Ищем именно активный таймер с валидной базовой датой
                        if (tm.isActive && tm.gameObject.activeInHierarchy)
                        {
                            long ticks = tm.dt_start.Ticks;
                            if (ticks > 0)
                            {
                                DateTime startTime = new DateTime(ticks, DateTimeKind.Local);
                                return new DateTimeOffset(startTime).ToUnixTimeSeconds();
                            }
                        }
                    }
                }
            }
            catch { }

            return 0;
        }

        public void Dispose()
        {
            _rpc?.Dispose();
        }
    }
}