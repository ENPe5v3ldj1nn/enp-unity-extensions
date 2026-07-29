using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    public sealed class CountdownTimer
    {
        private static readonly Action<TMP_Text, TimeSpan> _defaultTextFormatter = DefaultTextFormatter;

        private TMP_Text _text;
        private DateTime _targetUtc;
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

        public void StartTimer(DateTime targetUtc)
        {
            if (targetUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Target time must be UTC.", nameof(targetUtc));
            }

            if (_text == null)
            {
                throw new InvalidOperationException("Timer text is not set. Call Setup first.");
            }

            _targetUtc = targetUtc;
            _isRunning = true;

            StopTimer();

            var remaining = _targetUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                Complete();
                return;
            }
            
            _cts = new CancellationTokenSource();

            ApplyText(remaining);
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
                var remaining = _targetUtc - DateTime.UtcNow;

                if (remaining <= TimeSpan.Zero)
                {
                    Complete();
                    return;
                }

                ApplyText(remaining);
                await UniTask.Delay(1000,  cancellationToken: token);
            }
        }

        private void ApplyText(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero)
            {
                _textFormatter(_text, TimeSpan.Zero);
                return;
            }

            _textFormatter(_text, remaining);
        }

        private void Complete()
        {
            StopTimer();
            ApplyText(TimeSpan.Zero);

            var onCompleted = _onCompleted;
            onCompleted?.Invoke();
        }
        

        private static void DefaultTextFormatter(TMP_Text text, TimeSpan remaining)
        {
            var totalHours = remaining.Days * 24 + remaining.Hours;
            text.SetText("{0:00}:{1:00}:{2:00}", totalHours, remaining.Minutes, remaining.Seconds);
        }
    }
}