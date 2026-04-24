using enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash;
using UnityEngine;

namespace RaijtonExtentions.Debugging
{
    [DisallowMultipleComponent]
    public sealed class LivingInnerWashStreakDebugController : MonoBehaviour
    {
        private const int MaxStreak = 50;
        private static readonly Color FailBottomCollapseColor = Hex(0x7F, 0x96, 0xC7);
        private static readonly LivingInnerWashFiveSegments FailBottomBreakFootprintMin = new LivingInnerWashFiveSegments(0.18f, 0.50f, 1f, 0.50f, 0.18f);
        private static readonly LivingInnerWashFiveSegments FailBottomBreakFootprintMax = new LivingInnerWashFiveSegments(0.34f, 0.82f, 1.12f, 0.82f, 0.34f);
        private static readonly LivingInnerWashFiveSegments FailBottomCollapseFootprintMin = new LivingInnerWashFiveSegments(0.12f, 0.26f, 0.44f, 0.26f, 0.12f);
        private static readonly LivingInnerWashFiveSegments FailBottomCollapseFootprintMax = new LivingInnerWashFiveSegments(0.22f, 0.48f, 0.66f, 0.48f, 0.22f);
        private const float MinFailFlashDuration = 0.04f;
        private const float MaxFailFlashDuration = 0.10f;
        private const float MinFailHoldDuration = 0f;
        private const float MaxFailHoldDuration = 0.20f;
        private const float MinFailCollapseDuration = 0.10f;
        private const float MaxFailCollapseDuration = 0.22f;
        private const float MinFailRecoverDuration = 0.18f;
        private const float MaxFailRecoverDuration = 0.35f;

        [SerializeField] private LivingInnerWashMotionController _controller;
        [SerializeField] [Range(0, MaxStreak)] private int _currentStreak;
        [SerializeField] [Min(0.01f)] private float _streakTransitionDuration = 0.14f;
        [SerializeField] private Color _failFlashColor = Hex(0xE8, 0x5A, 0x70);
        [SerializeField] [Range(0f, 2f)] private float _failFlashIntensity = 1.15f;
        [SerializeField] [Range(MinFailFlashDuration, MaxFailFlashDuration)] private float _failFlashDuration = 0.06f;
        [SerializeField] [Range(MinFailHoldDuration, MaxFailHoldDuration)] private float _failHoldDuration = 0.08f;
        [SerializeField] [Range(MinFailCollapseDuration, MaxFailCollapseDuration)] private float _failCollapseDuration = 0.14f;
        [SerializeField] [Range(0f, 1.5f)] private float _failMotionSuppression = 0.95f;
        [SerializeField] [Range(0f, 1.5f)] private float _failBottomAccentBoost = 0.78f;
        [SerializeField] private bool _captureBaselineOnEnable = true;
        [SerializeField] private bool _showRuntimeButtons = true;

        private LivingInnerWashState _highState;
        private LivingInnerWashState _appliedState;
        private LivingInnerWashState _targetState;
        private LivingInnerWashMotionController.MotionTuning _highTuning;
        private LivingInnerWashMotionController.MotionTuning _appliedTuning;
        private LivingInnerWashMotionController.MotionTuning _targetTuning;
        private LivingInnerWashState _settleState;
        private LivingInnerWashMotionController.MotionTuning _settleTuning;
        private bool _baselineCaptured;
        private FailReaction _failReaction;

        private void Awake()
        {
            CacheController();
        }

        private void OnEnable()
        {
            CacheController();

            if (_captureBaselineOnEnable)
            {
                CaptureBaseline();
            }
        }

        private void OnDisable()
        {
            if (_controller != null && _baselineCaptured)
            {
                _controller.ClearDebugConfiguration();
            }
        }

        private void Update()
        {
            CacheController();

            if (_controller == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (!_baselineCaptured)
            {
                CaptureBaseline();
            }

            float deltaTime = Application.isPlaying ? Time.unscaledDeltaTime : 0.016f;
            if (deltaTime <= 0f)
            {
                deltaTime = 0.016f;
            }

            UpdateFailReaction(deltaTime);

            RebuildTargets();
            ApplySmoothing(deltaTime);

            _controller.ApplyDebugConfiguration(_appliedState, _appliedTuning);
        }

        [ContextMenu("+1 Streak")]
        public void IncrementStreak()
        {
            SetStreak(_currentStreak + 1);
        }

        [ContextMenu("-1 Streak")]
        public void DecrementStreak()
        {
            SetStreak(_currentStreak - 1);
        }

        [ContextMenu("Set Full Streak")]
        public void SetFullStreak()
        {
            SetStreak(MaxStreak);
        }

        [ContextMenu("Reset Streak")]
        public void ResetStreak()
        {
            SetStreak(0);
        }

        [ContextMenu("Trigger Fail")]
        public void TriggerFail()
        {
            int lostStreak = _currentStreak;

            if (lostStreak < 4)
            {
                _currentStreak = 0;
                _failReaction = default;
                return;
            }

            float failStrength = EvaluateFailStrength(lostStreak);
            _failReaction = new FailReaction
            {
                Active = true,
                StartStreak = lostStreak,
                EndStreak = 0,
                Strength = failStrength,
                Elapsed = 0f,
                FlashDuration = _failFlashDuration,
                HoldDuration = _failHoldDuration,
                CollapseDuration = _failCollapseDuration,
                RecoverDuration = Mathf.Lerp(MinFailRecoverDuration, MaxFailRecoverDuration, failStrength),
                Committed = false
            };
        }

        [ContextMenu("Capture Current As High Baseline")]
        public void CaptureBaseline()
        {
            CacheController();

            if (_controller == null)
            {
                return;
            }

            _highState = _controller.State;
            _highTuning = _controller.CurrentTuning;
            _targetState = _highState;
            _targetTuning = _highTuning;
            _appliedState = _highState;
            _appliedTuning = _highTuning;
            _baselineCaptured = true;
            RebuildTargets();
        }

        public void SetStreak(int streak)
        {
            _currentStreak = Mathf.Clamp(streak, 0, MaxStreak);
        }

        private void RebuildTargets()
        {
            int drivingStreak = _currentStreak;
            int settleStreak = _currentStreak;

            if (_failReaction.Active)
            {
                drivingStreak = _failReaction.Committed ? _failReaction.EndStreak : _failReaction.StartStreak;
                settleStreak = _failReaction.EndStreak;
            }

            float normalized = Mathf.Clamp01(drivingStreak / (float)MaxStreak);
            float motionT = 1f - Mathf.Pow(1f - normalized, 2.1f);
            float intensityT = SmoothStep(4f / MaxStreak, 20f / MaxStreak, normalized);
            float accentT = SmoothStep(10f / MaxStreak, 1f, normalized);
            float colorT = SmoothStep(0.30f, 1f, normalized);

            _targetState = BuildTargetState(_highState, intensityT, accentT, colorT);
            _targetTuning = BuildTargetTuning(_highTuning, motionT, accentT);

            float settleNormalized = Mathf.Clamp01(settleStreak / (float)MaxStreak);
            float settleMotionT = 1f - Mathf.Pow(1f - settleNormalized, 2.1f);
            float settleIntensityT = SmoothStep(4f / MaxStreak, 20f / MaxStreak, settleNormalized);
            float settleAccentT = SmoothStep(10f / MaxStreak, 1f, settleNormalized);
            float settleColorT = SmoothStep(0.30f, 1f, settleNormalized);
            _settleState = BuildTargetState(_highState, settleIntensityT, settleAccentT, settleColorT);
            _settleTuning = BuildTargetTuning(_highTuning, settleMotionT, settleAccentT);

            if (_failReaction.Active)
            {
                ApplyFailOverlay(ref _targetState, ref _targetTuning, _settleState, _settleTuning);
            }
        }

        private void ApplySmoothing(float deltaTime)
        {
            float response = 1f / Mathf.Max(0.01f, _streakTransitionDuration);
            float t = 1f - Mathf.Exp(-response * deltaTime);
            _appliedState = LerpState(_appliedState, _targetState, t);
            _appliedTuning = LerpTuning(_appliedTuning, _targetTuning, t);
        }

        private LivingInnerWashState BuildTargetState(LivingInnerWashState highState, float intensityT, float accentT, float colorT)
        {
            LivingInnerWashState state = highState;
            state.Intensity = Mathf.LerpUnclamped(0.03f, highState.Intensity, intensityT);
            Color lowBottom = BuildLowBottomColor(highState);
            state.BottomColor = Color.LerpUnclamped(lowBottom, highState.BottomColor, colorT);
            state.BottomAccents = Scale(highState.BottomAccents, Mathf.LerpUnclamped(0.03f, 1f, accentT));
            state.LeftAccents = Scale(highState.LeftAccents, Mathf.LerpUnclamped(0.02f, 0.92f, accentT));
            state.RightAccents = Scale(highState.RightAccents, Mathf.LerpUnclamped(0.02f, 0.92f, accentT));
            return LivingInnerWashState.Sanitize(state);
        }

        private LivingInnerWashMotionController.MotionTuning BuildTargetTuning(
            LivingInnerWashMotionController.MotionTuning highTuning,
            float motionT,
            float accentT)
        {
            LivingInnerWashMotionController.MotionTuning tuning = highTuning;
            tuning.GlobalMotionSpeed = Mathf.LerpUnclamped(highTuning.GlobalMotionSpeed * 0.45f, highTuning.GlobalMotionSpeed, motionT);
            tuning.IntensityBreathAmplitude = Mathf.LerpUnclamped(highTuning.IntensityBreathAmplitude * 0.30f, highTuning.IntensityBreathAmplitude, motionT);
            tuning.ThicknessBreathAmplitude = Mathf.LerpUnclamped(highTuning.ThicknessBreathAmplitude * 0.35f, highTuning.ThicknessBreathAmplitude, motionT);
            tuning.BottomSegmentMotionAmplitude = Mathf.LerpUnclamped(highTuning.BottomSegmentMotionAmplitude * 0.35f, highTuning.BottomSegmentMotionAmplitude, motionT);
            tuning.SideSegmentMotionAmplitude = Mathf.LerpUnclamped(highTuning.SideSegmentMotionAmplitude * 0.40f, highTuning.SideSegmentMotionAmplitude, motionT);
            tuning.AccentMotionAmplitude = Mathf.LerpUnclamped(0f, highTuning.AccentMotionAmplitude, accentT);
            tuning.AccentMotionSpeed = Mathf.LerpUnclamped(highTuning.AccentMotionSpeed * 0.65f, highTuning.AccentMotionSpeed, accentT);
            return LivingInnerWashMotionController.MotionTuning.Sanitize(tuning);
        }

        private void ApplyFailOverlay(
            ref LivingInnerWashState state,
            ref LivingInnerWashMotionController.MotionTuning tuning,
            LivingInnerWashState settleState,
            LivingInnerWashMotionController.MotionTuning settleTuning)
        {
            float flashEnvelope = _failReaction.FlashEnvelope;
            float holdEnvelope = _failReaction.HoldEnvelope;
            float collapseEnvelope = _failReaction.CollapseEnvelope;
            float recoverEnvelope = _failReaction.RecoverEnvelope;
            float strength = _failReaction.Strength;
            float strengthCurve = Mathf.LerpUnclamped(strength, Mathf.Sqrt(strength), 0.35f);
            float strongWeight = strengthCurve * strengthCurve;
            float strengthBoost = 0.82f + strongWeight * 0.74f;
            float flashPhase = Mathf.Max(flashEnvelope, holdEnvelope);
            float flashIntensity = flashPhase * _failFlashIntensity * (0.24f + strengthCurve * 0.76f);
            float collapseIntensity = collapseEnvelope * (0.44f + strongWeight * 0.76f);
            float recoverEase = 1f - recoverEnvelope * 0.18f;

            Color flashColor = BuildFailFlashColor(_failFlashColor);
            state.TintColor = Color.LerpUnclamped(state.TintColor, Color.white, flashIntensity);
            state.TopColor = Color.LerpUnclamped(state.TopColor, flashColor, flashIntensity);
            state.BottomColor = Color.LerpUnclamped(state.BottomColor, flashColor, flashIntensity);
            state.AccentColor = Color.LerpUnclamped(state.AccentColor, flashColor, flashIntensity);

            tuning.GlobalMotionSpeed += _highTuning.GlobalMotionSpeed * (0.06f + strengthCurve * 0.14f) * flashEnvelope;
            tuning.AccentMotionAmplitude += Mathf.Max(0.04f, _highTuning.AccentMotionAmplitude * (0.18f + strongWeight * 0.42f)) * flashEnvelope;
            tuning.AccentMotionSpeed += _highTuning.AccentMotionSpeed * (0.08f + strengthCurve * 0.16f) * flashEnvelope;
            tuning.BottomSegmentMotionAmplitude += _highTuning.BottomSegmentMotionAmplitude * (0.03f + strengthCurve * 0.08f) * flashEnvelope;
            tuning.SideSegmentMotionAmplitude += _highTuning.SideSegmentMotionAmplitude * (0.02f + strengthCurve * 0.05f) * flashEnvelope;

            float suppression = _failMotionSuppression * (0.55f + strongWeight * 0.65f);
            state.Intensity = Mathf.LerpUnclamped(state.Intensity, Mathf.LerpUnclamped(0.020f, 0.004f, strongWeight), collapseIntensity);
            state.Thickness = Mathf.LerpUnclamped(state.Thickness, Mathf.Max(0.015f, _highState.Thickness * Mathf.LerpUnclamped(0.22f, 0.10f, strongWeight)), collapseIntensity);
            tuning.AccentMotionAmplitude = Mathf.LerpUnclamped(tuning.AccentMotionAmplitude, 0f, collapseIntensity * suppression * 1.2f);
            tuning.BottomSegmentMotionAmplitude = Mathf.LerpUnclamped(tuning.BottomSegmentMotionAmplitude, _highTuning.BottomSegmentMotionAmplitude * Mathf.LerpUnclamped(0.26f, 0.05f, strongWeight), collapseIntensity * strengthBoost * suppression);
            tuning.SideSegmentMotionAmplitude = Mathf.LerpUnclamped(tuning.SideSegmentMotionAmplitude, _highTuning.SideSegmentMotionAmplitude * Mathf.LerpUnclamped(0.32f, 0.08f, strongWeight), collapseIntensity * strengthBoost * suppression);
            tuning.IntensityBreathAmplitude = Mathf.LerpUnclamped(tuning.IntensityBreathAmplitude, _highTuning.IntensityBreathAmplitude * Mathf.LerpUnclamped(0.28f, 0.08f, strongWeight), collapseIntensity * strengthBoost * suppression);
            tuning.ThicknessBreathAmplitude = Mathf.LerpUnclamped(tuning.ThicknessBreathAmplitude, _highTuning.ThicknessBreathAmplitude * Mathf.LerpUnclamped(0.30f, 0.09f, strongWeight), collapseIntensity * strengthBoost * suppression);
            tuning.MultiZoneBlend = Mathf.LerpUnclamped(tuning.MultiZoneBlend, Mathf.Clamp01(_highTuning.MultiZoneBlend - (0.18f + strongWeight * 0.24f)), collapseIntensity);

            Color failCollapseColor = BuildFailBottomCollapseColor(state.BottomColor);
            state.BottomColor = Color.LerpUnclamped(state.BottomColor, failCollapseColor, collapseIntensity * (0.82f + strengthCurve * 0.20f));

            LivingInnerWashFiveSegments breakFootprint = Lerp(FailBottomBreakFootprintMin, FailBottomBreakFootprintMax, strengthCurve);
            LivingInnerWashFiveSegments collapseFootprint = Lerp(FailBottomCollapseFootprintMin, FailBottomCollapseFootprintMax, strongWeight);
            LivingInnerWashFiveSegments widenedBreak = MultiplyPerSegment(state.BottomAccents, breakFootprint, 1f + flashIntensity * _failBottomAccentBoost * (0.30f + strengthCurve * 0.60f));
            LivingInnerWashFiveSegments collapsedBottom = MultiplyPerSegment(state.BottomAccents, collapseFootprint, Mathf.LerpUnclamped(0.28f, 0.04f, strongWeight));
            state.BottomAccents = Lerp(state.BottomAccents, widenedBreak, flashIntensity * (0.54f + strengthCurve * 0.24f));
            state.BottomAccents = Lerp(state.BottomAccents, collapsedBottom, collapseIntensity * strengthBoost);

            LivingInnerWashThreeSegments widenedLeft = new LivingInnerWashThreeSegments(
                state.LeftAccents.Top * Mathf.LerpUnclamped(1.02f, 1.07f, strengthCurve),
                state.LeftAccents.Mid * (1f + flashIntensity * (0.10f + strengthCurve * 0.12f)),
                state.LeftAccents.Bottom * Mathf.LerpUnclamped(1.08f, 1.22f, strengthCurve));
            LivingInnerWashThreeSegments widenedRight = new LivingInnerWashThreeSegments(
                state.RightAccents.Top * Mathf.LerpUnclamped(1.02f, 1.07f, strengthCurve),
                state.RightAccents.Mid * (1f + flashIntensity * (0.10f + strengthCurve * 0.12f)),
                state.RightAccents.Bottom * Mathf.LerpUnclamped(1.08f, 1.22f, strengthCurve));
            LivingInnerWashThreeSegments collapsedSide = new LivingInnerWashThreeSegments(
                state.LeftAccents.Top * Mathf.LerpUnclamped(0.30f, 0.14f, strongWeight),
                state.LeftAccents.Mid * Mathf.LerpUnclamped(0.24f, 0.08f, strongWeight),
                state.LeftAccents.Bottom * Mathf.LerpUnclamped(0.18f, 0.04f, strongWeight));
            state.LeftAccents = Lerp(state.LeftAccents, widenedLeft, flashIntensity * (0.12f + strengthCurve * 0.08f));
            state.RightAccents = Lerp(state.RightAccents, widenedRight, flashIntensity * (0.12f + strengthCurve * 0.08f));
            state.LeftAccents = Lerp(state.LeftAccents, collapsedSide, collapseIntensity * strengthBoost);
            state.RightAccents = Lerp(state.RightAccents, new LivingInnerWashThreeSegments(
                state.RightAccents.Top * Mathf.LerpUnclamped(0.30f, 0.14f, strongWeight),
                state.RightAccents.Mid * Mathf.LerpUnclamped(0.24f, 0.08f, strongWeight),
                state.RightAccents.Bottom * Mathf.LerpUnclamped(0.18f, 0.04f, strongWeight)), collapseIntensity * strengthBoost);

            state.Intensity = Mathf.LerpUnclamped(state.Intensity, settleState.Intensity, recoverEnvelope);
            state.Thickness = Mathf.LerpUnclamped(state.Thickness, settleState.Thickness, recoverEnvelope);
            state.TintColor = Color.LerpUnclamped(state.TintColor, settleState.TintColor, recoverEnvelope * recoverEase);
            state.TopColor = Color.LerpUnclamped(state.TopColor, settleState.TopColor, recoverEnvelope * recoverEase);
            state.BottomColor = Color.LerpUnclamped(state.BottomColor, settleState.BottomColor, recoverEnvelope * recoverEase);
            state.AccentColor = Color.LerpUnclamped(state.AccentColor, settleState.AccentColor, recoverEnvelope * recoverEase);
            state.BottomAccents = Lerp(state.BottomAccents, settleState.BottomAccents, recoverEnvelope);
            state.LeftAccents = Lerp(state.LeftAccents, settleState.LeftAccents, recoverEnvelope);
            state.RightAccents = Lerp(state.RightAccents, settleState.RightAccents, recoverEnvelope);
            tuning.GlobalMotionSpeed = Mathf.LerpUnclamped(tuning.GlobalMotionSpeed, settleTuning.GlobalMotionSpeed, recoverEnvelope * recoverEase);
            tuning.AccentMotionAmplitude = Mathf.LerpUnclamped(tuning.AccentMotionAmplitude, settleTuning.AccentMotionAmplitude, recoverEnvelope);
            tuning.AccentMotionSpeed = Mathf.LerpUnclamped(tuning.AccentMotionSpeed, settleTuning.AccentMotionSpeed, recoverEnvelope);
            tuning.BottomSegmentMotionAmplitude = Mathf.LerpUnclamped(tuning.BottomSegmentMotionAmplitude, settleTuning.BottomSegmentMotionAmplitude, recoverEnvelope);
            tuning.SideSegmentMotionAmplitude = Mathf.LerpUnclamped(tuning.SideSegmentMotionAmplitude, settleTuning.SideSegmentMotionAmplitude, recoverEnvelope);
            tuning.IntensityBreathAmplitude = Mathf.LerpUnclamped(tuning.IntensityBreathAmplitude, settleTuning.IntensityBreathAmplitude, recoverEnvelope);
            tuning.ThicknessBreathAmplitude = Mathf.LerpUnclamped(tuning.ThicknessBreathAmplitude, settleTuning.ThicknessBreathAmplitude, recoverEnvelope);
            tuning.MultiZoneBlend = Mathf.LerpUnclamped(tuning.MultiZoneBlend, settleTuning.MultiZoneBlend, recoverEnvelope);
            tuning = LivingInnerWashMotionController.MotionTuning.Sanitize(tuning);
            state = LivingInnerWashState.Sanitize(state);
        }

        private static LivingInnerWashState LerpState(LivingInnerWashState from, LivingInnerWashState to, float t)
        {
            t = Mathf.Clamp01(t);
            LivingInnerWashState state = to;
            state.TintColor = Color.LerpUnclamped(from.TintColor, to.TintColor, t);
            state.TopColor = Color.LerpUnclamped(from.TopColor, to.TopColor, t);
            state.BottomColor = Color.LerpUnclamped(from.BottomColor, to.BottomColor, t);
            state.AccentColor = Color.LerpUnclamped(from.AccentColor, to.AccentColor, t);
            state.Intensity = Mathf.LerpUnclamped(from.Intensity, to.Intensity, t);
            state.Thickness = Mathf.LerpUnclamped(from.Thickness, to.Thickness, t);
            state.Softness = Mathf.LerpUnclamped(from.Softness, to.Softness, t);
            state.BandTightness = Mathf.LerpUnclamped(from.BandTightness, to.BandTightness, t);
            state.CenterClear = Mathf.LerpUnclamped(from.CenterClear, to.CenterClear, t);
            state.CornerRoundness = Mathf.LerpUnclamped(from.CornerRoundness, to.CornerRoundness, t);
            state.TopStrength = Mathf.LerpUnclamped(from.TopStrength, to.TopStrength, t);
            state.BottomStrength = Mathf.LerpUnclamped(from.BottomStrength, to.BottomStrength, t);
            state.LeftStrength = Mathf.LerpUnclamped(from.LeftStrength, to.LeftStrength, t);
            state.RightStrength = Mathf.LerpUnclamped(from.RightStrength, to.RightStrength, t);
            state.BottomSegments = Lerp(from.BottomSegments, to.BottomSegments, t);
            state.LeftSegments = Lerp(from.LeftSegments, to.LeftSegments, t);
            state.RightSegments = Lerp(from.RightSegments, to.RightSegments, t);
            state.BottomAccents = Lerp(from.BottomAccents, to.BottomAccents, t);
            state.LeftAccents = Lerp(from.LeftAccents, to.LeftAccents, t);
            state.RightAccents = Lerp(from.RightAccents, to.RightAccents, t);
            return LivingInnerWashState.Sanitize(state);
        }

        private static LivingInnerWashMotionController.MotionTuning LerpTuning(
            LivingInnerWashMotionController.MotionTuning from,
            LivingInnerWashMotionController.MotionTuning to,
            float t)
        {
            t = Mathf.Clamp01(t);
            LivingInnerWashMotionController.MotionTuning tuning = to;
            tuning.MotionEnabled = to.MotionEnabled;
            tuning.UseUnscaledTime = to.UseUnscaledTime;
            tuning.GlobalMotionSpeed = Mathf.LerpUnclamped(from.GlobalMotionSpeed, to.GlobalMotionSpeed, t);
            tuning.IntensityBreathAmplitude = Mathf.LerpUnclamped(from.IntensityBreathAmplitude, to.IntensityBreathAmplitude, t);
            tuning.ThicknessBreathAmplitude = Mathf.LerpUnclamped(from.ThicknessBreathAmplitude, to.ThicknessBreathAmplitude, t);
            tuning.BottomSegmentMotionAmplitude = Mathf.LerpUnclamped(from.BottomSegmentMotionAmplitude, to.BottomSegmentMotionAmplitude, t);
            tuning.SideSegmentMotionAmplitude = Mathf.LerpUnclamped(from.SideSegmentMotionAmplitude, to.SideSegmentMotionAmplitude, t);
            tuning.AccentMotionAmplitude = Mathf.LerpUnclamped(from.AccentMotionAmplitude, to.AccentMotionAmplitude, t);
            tuning.AccentMotionSpeed = Mathf.LerpUnclamped(from.AccentMotionSpeed, to.AccentMotionSpeed, t);
            tuning.SegmentFrequencyMin = Mathf.LerpUnclamped(from.SegmentFrequencyMin, to.SegmentFrequencyMin, t);
            tuning.SegmentFrequencyMax = Mathf.LerpUnclamped(from.SegmentFrequencyMax, to.SegmentFrequencyMax, t);
            tuning.SegmentSmoothing = Mathf.LerpUnclamped(from.SegmentSmoothing, to.SegmentSmoothing, t);
            tuning.MultiZoneBlend = Mathf.LerpUnclamped(from.MultiZoneBlend, to.MultiZoneBlend, t);
            tuning.Seed = to.Seed;
            return LivingInnerWashMotionController.MotionTuning.Sanitize(tuning);
        }

        private static LivingInnerWashFiveSegments Scale(LivingInnerWashFiveSegments segments, float scale)
        {
            return new LivingInnerWashFiveSegments(
                segments.FarLeft * scale,
                segments.Left * scale,
                segments.Center * scale,
                segments.Right * scale,
                segments.FarRight * scale);
        }

        private static LivingInnerWashThreeSegments Scale(LivingInnerWashThreeSegments segments, float scale)
        {
            return new LivingInnerWashThreeSegments(
                segments.Top * scale,
                segments.Mid * scale,
                segments.Bottom * scale);
        }

        private static LivingInnerWashFiveSegments MultiplyPerSegment(
            LivingInnerWashFiveSegments values,
            LivingInnerWashFiveSegments multipliers,
            float scale)
        {
            return new LivingInnerWashFiveSegments(
                values.FarLeft * multipliers.FarLeft * scale,
                values.Left * multipliers.Left * scale,
                values.Center * multipliers.Center * scale,
                values.Right * multipliers.Right * scale,
                values.FarRight * multipliers.FarRight * scale);
        }

        private static LivingInnerWashFiveSegments Lerp(LivingInnerWashFiveSegments from, LivingInnerWashFiveSegments to, float t)
        {
            return new LivingInnerWashFiveSegments(
                Mathf.LerpUnclamped(from.FarLeft, to.FarLeft, t),
                Mathf.LerpUnclamped(from.Left, to.Left, t),
                Mathf.LerpUnclamped(from.Center, to.Center, t),
                Mathf.LerpUnclamped(from.Right, to.Right, t),
                Mathf.LerpUnclamped(from.FarRight, to.FarRight, t));
        }

        private static LivingInnerWashThreeSegments Lerp(LivingInnerWashThreeSegments from, LivingInnerWashThreeSegments to, float t)
        {
            return new LivingInnerWashThreeSegments(
                Mathf.LerpUnclamped(from.Top, to.Top, t),
                Mathf.LerpUnclamped(from.Mid, to.Mid, t),
                Mathf.LerpUnclamped(from.Bottom, to.Bottom, t));
        }

        private static float SmoothStep(float min, float max, float value)
        {
            if (Mathf.Approximately(min, max))
            {
                return value >= max ? 1f : 0f;
            }

            float t = Mathf.Clamp01((value - min) / (max - min));
            return t * t * (3f - 2f * t);
        }

        private static float EvaluateFailStrength(int lostStreak)
        {
            if (lostStreak <= 3)
            {
                return 0f;
            }

            float normalized = Mathf.Clamp01(Mathf.InverseLerp(4f, 20f, lostStreak));
            return Mathf.LerpUnclamped(normalized, SmoothStep(0f, 1f, normalized), 0.5f);
        }

        private static Color BuildLowBottomColor(LivingInnerWashState highState)
        {
            Color coolTarget = Color.LerpUnclamped(highState.BottomColor, highState.TopColor, 0.42f);
            Color muted = Color.LerpUnclamped(coolTarget, new Color(0.72f, 0.82f, 0.98f, highState.BottomColor.a), 0.26f);
            muted.a = highState.BottomColor.a;
            return muted;
        }

        private static Color BuildFailFlashColor(Color flashColor)
        {
            flashColor.a = 1f;
            return flashColor;
        }

        private static Color BuildFailBottomCollapseColor(Color baseColor)
        {
            Color cold = Color.LerpUnclamped(baseColor, FailBottomCollapseColor, 0.78f);
            cold = Color.LerpUnclamped(cold, new Color(0.87f, 0.91f, 0.98f, baseColor.a), 0.12f);
            cold.a = baseColor.a;
            return cold;
        }

        private static Color Hex(byte r, byte g, byte b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private void CacheController()
        {
            if (_controller == null)
            {
                _controller = GetComponent<LivingInnerWashMotionController>();
            }
        }

        private void UpdateFailReaction(float deltaTime)
        {
            if (!_failReaction.Active)
            {
                return;
            }

            _failReaction.Elapsed += deltaTime;

            if (!_failReaction.Committed && _failReaction.Elapsed >= _failReaction.FlashDuration + _failReaction.HoldDuration + _failReaction.CollapseDuration)
            {
                _currentStreak = _failReaction.EndStreak;
                _failReaction.Committed = true;
            }

            if (_failReaction.Elapsed >= _failReaction.TotalDuration)
            {
                _failReaction = default;
            }
        }

        private void OnGUI()
        {
            if (!_showRuntimeButtons || !Application.isPlaying)
            {
                return;
            }

            const float width = 150f;
            const float height = 28f;
            Rect area = new Rect(16f, 16f, width, 220f);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label($"Streak: {_currentStreak}/{MaxStreak}");

            if (GUILayout.Button("+1 streak", GUILayout.Width(width), GUILayout.Height(height)))
            {
                IncrementStreak();
            }

            if (GUILayout.Button("-1 streak", GUILayout.Width(width), GUILayout.Height(height)))
            {
                DecrementStreak();
            }

            if (GUILayout.Button("full 50", GUILayout.Width(width), GUILayout.Height(height)))
            {
                SetFullStreak();
            }

            if (GUILayout.Button("reset 0", GUILayout.Width(width), GUILayout.Height(height)))
            {
                ResetStreak();
            }

            if (GUILayout.Button("fail", GUILayout.Width(width), GUILayout.Height(height)))
            {
                TriggerFail();
            }

            GUILayout.EndArea();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _currentStreak = Mathf.Clamp(_currentStreak, 0, MaxStreak);
            _streakTransitionDuration = Mathf.Max(0.01f, _streakTransitionDuration);
            _failFlashDuration = Mathf.Clamp(_failFlashDuration, MinFailFlashDuration, MaxFailFlashDuration);
            _failHoldDuration = Mathf.Clamp(_failHoldDuration, MinFailHoldDuration, MaxFailHoldDuration);
            _failCollapseDuration = Mathf.Clamp(_failCollapseDuration, MinFailCollapseDuration, MaxFailCollapseDuration);
        }
#endif

        private struct FailReaction
        {
            public bool Active;
            public int StartStreak;
            public int EndStreak;
            public float Strength;
            public float Elapsed;
            public float FlashDuration;
            public float HoldDuration;
            public float CollapseDuration;
            public float RecoverDuration;
            public bool Committed;

            public float TotalDuration => FlashDuration + HoldDuration + CollapseDuration + RecoverDuration;

            public float FlashEnvelope
            {
                get
                {
                    if (!Active || FlashDuration <= 0f || Elapsed >= FlashDuration)
                    {
                        return 0f;
                    }

                    float t = Mathf.Clamp01(Elapsed / FlashDuration);
                    return 1f - Mathf.Pow(1f - t, 2.4f);
                }
            }

            public float CollapseEnvelope
            {
                get
                {
                    float start = FlashDuration + HoldDuration;

                    if (!Active || CollapseDuration <= 0f || Elapsed < start || Elapsed >= start + CollapseDuration)
                    {
                        return 0f;
                    }

                    float t = Mathf.Clamp01((Elapsed - start) / CollapseDuration);
                    return t * t * (3f - 2f * t);
                }
            }

            public float RecoverEnvelope
            {
                get
                {
                    float start = FlashDuration + HoldDuration + CollapseDuration;

                    if (!Active || RecoverDuration <= 0f || Elapsed < start)
                    {
                        return 0f;
                    }

                    float t = Mathf.Clamp01((Elapsed - start) / RecoverDuration);
                    return t * t * (3f - 2f * t);
                }
            }

            public float HoldEnvelope
            {
                get
                {
                    float start = FlashDuration;

                    if (!Active || HoldDuration <= 0f || Elapsed < start || Elapsed >= start + HoldDuration)
                    {
                        return 0f;
                    }

                    return 1f;
                }
            }

        }
    }
}
