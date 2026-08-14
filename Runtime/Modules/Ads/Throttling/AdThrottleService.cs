using System;
using System.Collections.Generic;
using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    public sealed class AdThrottleService
    {
        private const string LastInterstitialKey = "ENP.AdThrottle.LastInterstitialUtcTicks";
        private const string InterstitialHistoryKey = "ENP.AdThrottle.InterstitialHistoryUtcTicks";
        private const string LastAppOpenKey = "ENP.AdThrottle.LastAppOpenUtcTicks";
        private const string LastRewardedKey = "ENP.AdThrottle.LastRewardedUtcTicks";
        private const string AppOpenHistoryKey = "ENP.AdThrottle.AppOpenHistoryUtcTicks";
        private const char HistorySeparator = ';';

        private readonly AdThrottleConfig _config;
        private readonly TimeSpan _cooldown;
        private readonly TimeSpan _antiChainCooldown;
        private readonly TimeSpan _hourWindow;
        private readonly TimeSpan _appOpenWindow;

        private bool _isDataLoaded;
        private DateTime? _lastInterstitialUtc;
        private DateTime? _lastAppOpenUtc;
        private DateTime? _lastRewardedUtc;
        private readonly List<long> _interstitialHistoryTicks = new();
        private readonly List<long> _appOpenHistoryTicks = new();
        private int _completedGamesThisSession;
        private int _interstitialsShownThisSession;

        public AdThrottleService(AdThrottleConfig config)
        {
            _config = config;
            _cooldown = TimeSpan.FromSeconds(config.InterstitialCooldownSeconds);
            _antiChainCooldown = TimeSpan.FromSeconds(config.AntiChainCooldownSeconds);
            _hourWindow = TimeSpan.FromMinutes(config.HourlyWindowMinutes);
            _appOpenWindow = TimeSpan.FromMinutes(config.AppOpenWindowMinutes);
        }

        public void RegisterCompletedGame()
        {
            _completedGamesThisSession++;
        }

        public bool ShouldShowInterstitialNow(string reason)
        {
            EnsureDataLoaded();

            var now = DateTime.UtcNow;

            if (_interstitialsShownThisSession >= _config.MaxInterstitialsPerSession)
            {
                Debug.Log($"[Ads][Throttle] interstitial_allowed=false reason=session_cap shown_this_session={_interstitialsShownThisSession} session_cap={_config.MaxInterstitialsPerSession} completed_games={_completedGamesThisSession} end_reason={reason}");
                return false;
            }

            if (_interstitialsShownThisSession == 0 &&
                _completedGamesThisSession < _config.MinimumCompletedGamesBeforeFirstInterstitial)
            {
                Debug.Log($"[Ads][Throttle] interstitial_allowed=false reason=completed_games completed_games={_completedGamesThisSession} shown_this_session={_interstitialsShownThisSession} history_count={_interstitialHistoryTicks.Count} end_reason={reason}");
                return false;
            }

            if (_lastInterstitialUtc.HasValue && now - _lastInterstitialUtc.Value < _cooldown)
            {
                Debug.Log($"[Ads][Throttle] interstitial_allowed=false reason=cooldown last_interstitial_utc={_lastInterstitialUtc.Value:O} now_utc={now:O} shown_this_session={_interstitialsShownThisSession} completed_games={_completedGamesThisSession} end_reason={reason}");
                return false;
            }

            if (_lastAppOpenUtc.HasValue && now - _lastAppOpenUtc.Value < _antiChainCooldown)
            {
                Debug.Log($"[Ads][Throttle] interstitial_allowed=false reason=app_open_chain last_app_open_utc={_lastAppOpenUtc.Value:O} now_utc={now:O}");
                return false;
            }

            if (_lastRewardedUtc.HasValue && now - _lastRewardedUtc.Value < _antiChainCooldown)
            {
                Debug.Log($"[Ads][Throttle] interstitial_allowed=false reason=rewarded_chain last_rewarded_utc={_lastRewardedUtc.Value:O} now_utc={now:O}");
                return false;
            }

            PruneInterstitialHistory(now);
            var allowed = _interstitialHistoryTicks.Count < _config.HourlyCap;
            Debug.Log($"[Ads][Throttle] interstitial_allowed={allowed} reason=hourly_cap history_count={_interstitialHistoryTicks.Count} hourly_cap={_config.HourlyCap} completed_games={_completedGamesThisSession} shown_this_session={_interstitialsShownThisSession} end_reason={reason}");
            return allowed;
        }

        public bool IsInterstitialLoadWorthwhile()
        {
            EnsureDataLoaded();

            if (_interstitialsShownThisSession >= _config.MaxInterstitialsPerSession)
            {
                return false;
            }

            PruneInterstitialHistory(DateTime.UtcNow);
            return _interstitialHistoryTicks.Count < _config.HourlyCap;
        }

        public bool CanShowAppOpenNow()
        {
            EnsureDataLoaded();

            var now = DateTime.UtcNow;

            if (_lastInterstitialUtc.HasValue && now - _lastInterstitialUtc.Value < _antiChainCooldown)
            {
                return false;
            }

            if (_lastRewardedUtc.HasValue && now - _lastRewardedUtc.Value < _antiChainCooldown)
            {
                return false;
            }

            PruneAppOpenHistory(now);

            if (_appOpenHistoryTicks.Count >= _config.AppOpenMaxShowsPerWindow)
            {
                return false;
            }

            if (_appOpenHistoryTicks.Count == 0)
            {
                return true;
            }

            var lastShowUtc = new DateTime(_appOpenHistoryTicks[^1], DateTimeKind.Utc);
            var minutesSinceLast = (now - lastShowUtc).TotalMinutes;
            return minutesSinceLast >= _config.AppOpenMinIntervalBetweenShowsMinutes;
        }

        public void NotifyAdShown(AdType adType)
        {
            EnsureDataLoaded();
            var now = DateTime.UtcNow;

            switch (adType)
            {
                case AdType.Interstitial:
                    _lastInterstitialUtc = now;
                    _interstitialsShownThisSession++;
                    _interstitialHistoryTicks.Add(now.Ticks);
                    PruneInterstitialHistory(now);
                    SaveInterstitialState();
                    break;
                case AdType.AppOpen:
                    _lastAppOpenUtc = now;
                    _appOpenHistoryTicks.Add(now.Ticks);
                    PruneAppOpenHistory(now);
                    SaveAppOpenState();
                    break;
                case AdType.Rewarded:
                    _lastRewardedUtc = now;
                    PlayerPrefs.SetString(LastRewardedKey, now.Ticks.ToString());
                    PlayerPrefs.Save();
                    break;
            }
        }

        private void EnsureDataLoaded()
        {
            if (_isDataLoaded)
            {
                return;
            }

            _isDataLoaded = true;

            LoadHistory(InterstitialHistoryKey, _interstitialHistoryTicks);
            LoadHistory(AppOpenHistoryKey, _appOpenHistoryTicks);

            _lastInterstitialUtc = ReadDateTimeFromPrefs(LastInterstitialKey);
            _lastAppOpenUtc = ReadDateTimeFromPrefs(LastAppOpenKey);
            _lastRewardedUtc = ReadDateTimeFromPrefs(LastRewardedKey);

            PruneInterstitialHistory(DateTime.UtcNow);
            PruneAppOpenHistory(DateTime.UtcNow);
        }

        private static void LoadHistory(string key, List<long> destination)
        {
            destination.Clear();

            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            var parts = raw.Split(new[] { HistorySeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (long.TryParse(part, out var ticks))
                {
                    destination.Add(ticks);
                }
            }
        }

        private void SaveInterstitialState()
        {
            SaveHistory(InterstitialHistoryKey, _interstitialHistoryTicks);

            if (_lastInterstitialUtc.HasValue)
            {
                PlayerPrefs.SetString(LastInterstitialKey, _lastInterstitialUtc.Value.Ticks.ToString());
            }
            else
            {
                PlayerPrefs.DeleteKey(LastInterstitialKey);
            }

            PlayerPrefs.Save();
        }

        private void SaveAppOpenState()
        {
            SaveHistory(AppOpenHistoryKey, _appOpenHistoryTicks);

            if (_lastAppOpenUtc.HasValue)
            {
                PlayerPrefs.SetString(LastAppOpenKey, _lastAppOpenUtc.Value.Ticks.ToString());
            }
            else
            {
                PlayerPrefs.DeleteKey(LastAppOpenKey);
            }

            PlayerPrefs.Save();
        }

        private static void SaveHistory(string key, List<long> history)
        {
            var raw = string.Join(HistorySeparator.ToString(), history);
            if (string.IsNullOrEmpty(raw))
            {
                PlayerPrefs.DeleteKey(key);
            }
            else
            {
                PlayerPrefs.SetString(key, raw);
            }
        }

        private void PruneInterstitialHistory(DateTime now)
        {
            var cutoffTicks = now.Add(-_hourWindow).Ticks;
            _interstitialHistoryTicks.RemoveAll(ticks => ticks < cutoffTicks);
        }

        private void PruneAppOpenHistory(DateTime now)
        {
            var cutoffTicks = now.Add(-_appOpenWindow).Ticks;
            _appOpenHistoryTicks.RemoveAll(ticks => ticks < cutoffTicks);
        }

        private static DateTime? ReadDateTimeFromPrefs(string key)
        {
            var raw = PlayerPrefs.GetString(key, string.Empty);
            return long.TryParse(raw, out var ticks) ? new DateTime(ticks, DateTimeKind.Utc) : null;
        }
    }
}
