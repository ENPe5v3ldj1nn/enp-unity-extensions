using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    public enum TimerDirection
    {
        CountDown,
        CountUp
    }

    public sealed class CountdownTimer
    {
        private static readonly Action<TMP_Text, TimeSpan> _defaultTextFormatter = DefaultTextFormatter;

        private TMP_Text _text;
        private DateTime _referenceUtc;
        private TimerDirection _direction;
        private TimeSpan? _limit;
        private bool _isRunning;
        private Action<TMP_Text, TimeSpan> _textFormatter = _defaultTextFormatter;
        private Action _onCompleted;
        private CancellationTokenSource _cts;

        public void Setup(TMP_Text text, Action onCompleted = null)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _onCompleted = onCompleted;
        }

        public void SetTextFormatter(Action<TMP_Text, TimeSpan> textFormatter)
        {
            _textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
        }

        public void ResetTextFormatter()
        {
            _textFormatter = _defaultTextFormatter;
        }

        public void ClearCompletedCallback()
        {
            _onCompleted = null;
        }

        // For CountDown, referenceUtc is the target time to count down to.
        // For CountUp, referenceUtc is the start time to count up from; limit is optional —
        // when set, the timer auto-completes once elapsed time reaches it, otherwise it runs
        // indefinitely until StopTimer() is called.
        public void StartTimer(DateTime referenceUtc, TimerDirection direction = TimerDirection.CountDown, TimeSpan? limit = null)
        {
            if (referenceUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Reference time must be UTC.", nameof(referenceUtc));
            }

            if (_text == null)
            {
                throw new InvalidOperationException("Timer text is not set. Call Setup first.");
            }

            _referenceUtc = referenceUtc;
            _direction = direction;
            _limit = limit;
            _isRunning = true;

            StopTimer();

            var elapsedOrRemaining = GetElapsedOrRemaining();
            if (_direction == TimerDirection.CountDown && elapsedOrRemaining <= TimeSpan.Zero)
            {
                Complete();
                return;
            }

            _cts = new CancellationTokenSource();

            ApplyText(elapsedOrRemaining);
            Run(_cts.Token).Forget();
        }

        public void StopTimer()
        {
            _isRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTask Run(CancellationToken token)
        {
            while (_isRunning && !token.IsCancellationRequested)
            {
                var elapsedOrRemaining = GetElapsedOrRemaining();

                if (_direction == TimerDirection.CountDown && elapsedOrRemaining <= TimeSpan.Zero)
                {
                    Complete();
                    return;
                }

                if (_direction == TimerDirection.CountUp && _limit.HasValue && elapsedOrRemaining >= _limit.Value)
                {
                    ApplyText(_limit.Value);
                    Complete();
                    return;
                }

                ApplyText(elapsedOrRemaining);
                await UniTask.Delay(1000,  cancellationToken: token);
            }
        }

        private TimeSpan GetElapsedOrRemaining()
        {
            return _direction == TimerDirection.CountUp
                ? DateTime.UtcNow - _referenceUtc
                : _referenceUtc - DateTime.UtcNow;
        }

        private void ApplyText(TimeSpan value)
        {
            if (_direction == TimerDirection.CountDown && value <= TimeSpan.Zero)
            {
                _textFormatter(_text, TimeSpan.Zero);
                return;
            }

            _textFormatter(_text, value);
        }

        private void Complete()
        {
            StopTimer();

            if (_direction == TimerDirection.CountDown)
            {
                ApplyText(TimeSpan.Zero);
            }

            var onCompleted = _onCompleted;
            onCompleted?.Invoke();
        }


        private static void DefaultTextFormatter(TMP_Text text, TimeSpan value)
        {
            var totalHours = value.Days * 24 + value.Hours;
            text.SetText("{0:00}:{1:00}:{2:00}", totalHours, value.Minutes, value.Seconds);
        }
    }
}