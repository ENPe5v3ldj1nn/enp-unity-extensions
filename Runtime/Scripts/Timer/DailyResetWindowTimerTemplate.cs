using System;
using TMPro;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.Timer
{
    public abstract class DailyResetWindowTimerTemplate : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timerText;

        private CountdownTimer _countdownTimer;

        protected virtual void Awake()
        {
            if (_timerText == null)
            {
                throw new InvalidOperationException($"{nameof(_timerText)} is not assigned.");
            }

            _countdownTimer = new CountdownTimer();
            _countdownTimer.Setup(_timerText, OnCountdownCompleted);
        }

        protected virtual void OnEnable()
        {
            var nowUtc = DateTime.UtcNow;

            RefreshDataIfNeeded(nowUtc);
            StartCountdown(nowUtc);
        }

        protected virtual void OnDisable()
        {
            _countdownTimer.StopTimer();
        }

        protected void RestartCountdown()
        {
            StartCountdown(DateTime.UtcNow);
        }

        protected virtual DateTime GetNextResetUtc(DateTime nowUtc)
        {
            return nowUtc.Date.AddDays(1);
        }

        protected virtual void OnCountdownCompleted()
        {
        }

        private void StartCountdown(DateTime nowUtc)
        {
            var nextResetUtc = GetNextResetUtc(nowUtc);
            _countdownTimer.StartTimer(nextResetUtc);
        }

        protected abstract void RefreshDataIfNeeded(DateTime nowUtc);
    }
}