using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.Timer
{
    public sealed class CountdownTimer
    {
        private static readonly WaitForSecondsRealtime _tickDelay = new(1f);
        private static readonly Action<TMP_Text, TimeSpan> _defaultTextFormatter = DefaultTextFormatter;

        private TMP_Text _text;
        private Coroutine _coroutine;
        private DateTime _targetUtc;
        private bool _isRunning;
        private Action<TMP_Text, TimeSpan> _textFormatter = _defaultTextFormatter;
        private Action _onCompleted;

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

            StopCoroutineInternal();

            var remaining = _targetUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                Complete();
                return;
            }

            ApplyText(remaining);
            Run().Start(out _coroutine);
        }

        public void StopTimer()
        {
            _isRunning = false;
            StopCoroutineInternal();
        }

        private IEnumerator Run()
        {
            while (_isRunning)
            {
                var remaining = _targetUtc - DateTime.UtcNow;

                if (remaining <= TimeSpan.Zero)
                {
                    Complete();
                    yield break;
                }

                ApplyText(remaining);
                yield return _tickDelay;
            }

            _coroutine = null;
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
            _isRunning = false;
            _coroutine = null;
            ApplyText(TimeSpan.Zero);

            var onCompleted = _onCompleted;
            onCompleted?.Invoke();
        }

        private void StopCoroutineInternal()
        {
            if (_coroutine != null)
            {
                _coroutine.Stop();
                _coroutine = null;
            }
        }

        private static void DefaultTextFormatter(TMP_Text text, TimeSpan remaining)
        {
            var totalHours = remaining.Days * 24 + remaining.Hours;
            text.SetText("{0:00}:{1:00}:{2:00}", totalHours, remaining.Minutes, remaining.Seconds);
        }
    }
}