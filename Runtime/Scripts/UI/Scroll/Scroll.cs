using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace enp_unity_extensions.Runtime.Scripts.UI.Scroll
{
    [DisallowMultipleComponent]
    public sealed class Scroll : MonoBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public enum Axis { Horizontal, Vertical }
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public enum SnapAlignment { Start, Center, End }
        public enum NestedMode { DominantAxis, Simultaneous }
        public enum SnapPageOrder { Hierarchy, ReverseHierarchy }

        [SerializeField] private RectTransform _content;
        [SerializeField] private Axis _axis = Axis.Vertical;
        [SerializeField] private MovementType _movementType = MovementType.Elastic;

        [Header("Inertia")]
        [SerializeField] private bool _inertia = true;
        [SerializeField] private float _decelerationRate = 0.135f;
        [SerializeField] private float _elasticity = 0.10f;
        [SerializeField] private float _scrollSensitivity = 20f;
        [SerializeField] private bool _useUnscaledTime = true;

        [Header("Nested")]
        [SerializeField] private Scroll _parentScroll;
        [SerializeField] private NestedMode _nestedMode = NestedMode.DominantAxis;

        [Header("Nested DominantAxis")]
        [SerializeField] private float _directionDecideThreshold = 10f;
        [SerializeField] private float _directionDominanceRatio = 1.15f;
        [SerializeField] private float _directionForceDecideDistance = 24f;

        [Header("Nested Simultaneous")]
        [SerializeField] private float _parentAxisDeadZone = 2f;

        [Header("Snap")]
        [SerializeField] private bool _snapEnabled = false;
        [SerializeField] private SnapAlignment _snapAlignment = SnapAlignment.Center;
        [SerializeField] private SnapPageOrder _snapPageOrder = SnapPageOrder.Hierarchy;

        [SerializeField] private bool _snapUseVelocityToAdvance = true;
        [SerializeField] private float _snapAdvanceVelocityThreshold = 1200f;
        [SerializeField] private float _snapStartVelocityThreshold = 800f;
        [SerializeField] private float _snapSmoothTime = 0.12f;
        [SerializeField] private float _snapMaxSpeed = 20000f;
        [SerializeField] private float _snapStopDistance = 0.5f;
        [SerializeField] private bool _snapAfterWheel = true;
        [SerializeField] private float _snapWheelDelay = 0.08f;

        private RectTransform _viewRect;

        private Bounds _viewBounds;
        private Bounds _contentBounds;

        private Vector2 _pointerStartLocalCursor;
        private Vector2 _contentStartPosition;

        private Vector2 _velocity;
        private Vector2 _springVelocity;

        private bool _dragging;

        private Vector2 _prevContentPos;
        private float _prevTime;
        private bool _hasPrev;

        private bool _pendingSnap;
        private bool _snapping;
        private float _snapTargetAxis;
        private float _snapDampVel;
        private float _wheelSnapTimer;

        private bool _decidingDirection;
        private bool _routeToParent;
        private Vector2 _pressScreenPos;
        private Vector2 _lastScreenPosForSimultaneous;

        private float TimeNow => _useUnscaledTime ? Time.unscaledTime : Time.time;
        private float DeltaTime => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private readonly Subject<Unit> _startedSubject = new Subject<Unit>();
        private readonly Subject<Unit> _userDragBeganSubject = new Subject<Unit>();
        private readonly Subject<Unit> _userDragEndedSubject = new Subject<Unit>();

        private readonly Subject<Vector2> _contentPosSubject = new Subject<Vector2>();
        private readonly Subject<float> _axisPosSubject = new Subject<float>();

        private readonly Subject<int> _snapIndexSubject = new Subject<int>();
        private readonly Subject<float> _snapPagePosSubject = new Subject<float>();
        private readonly Subject<float> _snapPage01Subject = new Subject<float>();

        public IObservable<Unit> Started => _startedSubject;
        public IObservable<Unit> UserDragBegan => _userDragBeganSubject;
        public IObservable<Unit> UserDragEnded => _userDragEndedSubject;

        public IObservable<Vector2> ContentAnchoredPositionChanged => _contentPosSubject;
        public IObservable<float> AxisPositionChanged => _axisPosSubject;

        public IObservable<int> SnapIndexChanged => _snapIndexSubject;
        public IObservable<float> SnapPagePositionChanged => _snapPagePosSubject;
        public IObservable<float> SnapPage01Changed => _snapPage01Subject;

        private float[] _snapTargetsAxisByPage = Array.Empty<float>();

        private float[] _snapTargetsAxisSorted = Array.Empty<float>();
        private int[] _snapPageBySorted = Array.Empty<int>();

        private int _snapTargetsCount;
        private int _snapTargetsChildCount = -1;
        private Vector2 _snapTargetsViewSize;
        private bool _snapTargetsDirty = true;

        private bool _isInitialized;

        public int SnapPageCount => _snapTargetsCount;
        public void MarkSnapTargetsDirty() => _snapTargetsDirty = true;

        private Vector2 _lastNotifiedPos;
        private bool _hasLastNotifiedPos;

        private int _lastNotifiedSnapIndex = -1;
        private float _lastNotifiedSnapPagePos = float.NaN;
        private float _lastNotifiedSnap01 = float.NaN;

        private const float SnapNotifyEpsilon = 0.0001f;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            _viewRect = (RectTransform)transform;
            _snapTargetsDirty = true;
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized)
                Initialize();

            if (!_isInitialized)
                return;

            _dragging = false;

            _velocity = Vector2.zero;
            _springVelocity = Vector2.zero;

            _hasPrev = false;

            _pendingSnap = false;
            _snapping = false;
            _snapDampVel = 0f;
            _wheelSnapTimer = 0f;

            _decidingDirection = false;
            _routeToParent = false;

            _pressScreenPos = Vector2.zero;
            _lastScreenPosForSimultaneous = Vector2.zero;

            _snapTargetsDirty = true;
            if (_snapEnabled) EnsureSnapTargets();

            NotifyPositionChanged(_content.anchoredPosition);
            _startedSubject.OnNext(Unit.Default);
        }

        private void OnRectTransformDimensionsChange()
        {
            _snapTargetsDirty = true;
        }

        private void OnDestroy()
        {
            CompleteAndDispose(_startedSubject);
            CompleteAndDispose(_userDragBeganSubject);
            CompleteAndDispose(_userDragEndedSubject);

            CompleteAndDispose(_contentPosSubject);
            CompleteAndDispose(_axisPosSubject);
            CompleteAndDispose(_snapIndexSubject);
            CompleteAndDispose(_snapPagePosSubject);
            CompleteAndDispose(_snapPage01Subject);
        }

        private static void CompleteAndDispose<T>(Subject<T> s)
        {
            if (s == null) return;
            try { s.OnCompleted(); } catch {  }
            s.Dispose();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (!_isInitialized) return;
            eventData.useDragThreshold = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInitialized) return;

            _pendingSnap = false;
            _snapping = false;
            _snapDampVel = 0f;
            _wheelSnapTimer = 0f;

            _pressScreenPos = eventData.position;
            _lastScreenPosForSimultaneous = eventData.position;

            _decidingDirection = false;
            _routeToParent = false;
            _dragging = false;

            if (_parentScroll != null && _nestedMode == NestedMode.Simultaneous)
            {
                BeginDragSelfAt(eventData, _pressScreenPos);
                _parentScroll.ExternalBeginDragAt(eventData, _pressScreenPos);
                return;
            }

            if (_parentScroll != null && _nestedMode == NestedMode.DominantAxis)
            {
                _decidingDirection = true;
                return;
            }

            BeginDragSelfAt(eventData, _pressScreenPos);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isInitialized) return;

            if (_parentScroll != null && _nestedMode == NestedMode.Simultaneous)
            {
                DragSelf(eventData);

                Vector2 d = eventData.position - _lastScreenPosForSimultaneous;
                _lastScreenPosForSimultaneous = eventData.position;

                float parentAxis = _parentScroll._axis == Axis.Horizontal ? d.x : d.y;
                if (Mathf.Abs(parentAxis) >= _parentAxisDeadZone)
                    _parentScroll.ExternalDrag(eventData);

                return;
            }

            if (_decidingDirection)
            {
                Vector2 d = eventData.position - _pressScreenPos;
                float ax = Mathf.Abs(d.x);
                float ay = Mathf.Abs(d.y);

                if (ax * ax + ay * ay < _directionDecideThreshold * _directionDecideThreshold)
                    return;

                float selfMag = _axis == Axis.Horizontal ? ax : ay;
                float otherMag = _axis == Axis.Horizontal ? ay : ax;

                bool chooseSelf = false;
                bool chooseParent = false;

                if (selfMag >= otherMag * _directionDominanceRatio) chooseSelf = true;
                else if (otherMag >= selfMag * _directionDominanceRatio) chooseParent = true;
                else if (Mathf.Max(selfMag, otherMag) >= _directionForceDecideDistance)
                {
                    if (selfMag >= otherMag) chooseSelf = true;
                    else chooseParent = true;
                }
                else
                {
                    return;
                }

                _decidingDirection = false;

                if (chooseParent && _parentScroll != null)
                {
                    _routeToParent = true;
                    _parentScroll.ExternalBeginDragAt(eventData, _pressScreenPos);
                    _parentScroll.ExternalDrag(eventData);
                    return;
                }

                _routeToParent = false;
                BeginDragSelfAt(eventData, _pressScreenPos);
                DragSelf(eventData);
                return;
            }

            if (_routeToParent)
            {
                _parentScroll.ExternalDrag(eventData);
                return;
            }

            DragSelf(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isInitialized) return;

            if (_parentScroll != null && _nestedMode == NestedMode.Simultaneous)
            {
                EndDragSelf();
                _parentScroll.ExternalEndDrag(eventData);
                return;
            }

            if (_routeToParent)
            {
                _parentScroll.ExternalEndDrag(eventData);
                _routeToParent = false;
                _decidingDirection = false;
                return;
            }

            _decidingDirection = false;
            EndDragSelf();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!_isInitialized) return;

            UpdateBounds();

            Vector2 delta = eventData.scrollDelta;
            delta.y *= -1f;

            if (_axis == Axis.Horizontal) delta.y = 0f;
            else delta.x = 0f;

            Vector2 pos = _content.anchoredPosition + delta * _scrollSensitivity;

            if (_movementType == MovementType.Clamped)
            {
                Vector2 offset = CalculateOffset(pos - _content.anchoredPosition);
                pos += offset;
            }

            SetContentAnchoredPosition(pos);
            UpdateBounds();

            if (_snapEnabled && _snapAfterWheel)
            {
                _wheelSnapTimer = Mathf.Max(_wheelSnapTimer, _snapWheelDelay);
                _pendingSnap = false;
                _snapping = false;
                _snapDampVel = 0f;
            }
        }

        private void LateUpdate()
        {
            if (!_isInitialized) return;

            float dt = DeltaTime;
            if (dt <= 0f) return;
            if (_dragging) return;
            if (_decidingDirection) return;
            if (_routeToParent) return;

            if (_wheelSnapTimer > 0f)
            {
                _wheelSnapTimer -= dt;
                if (_wheelSnapTimer <= 0f && _snapEnabled) _pendingSnap = true;
            }

            UpdateBounds();

            Vector2 offset = CalculateOffset(Vector2.zero);

            if (_movementType == MovementType.Elastic && (offset.x != 0f || offset.y != 0f))
            {
                _snapping = false;
                if (_snapEnabled) _pendingSnap = true;

                Vector2 target = _content.anchoredPosition + offset;

                if (_axis == Axis.Horizontal) target.y = _content.anchoredPosition.y;
                else target.x = _content.anchoredPosition.x;

                Vector2 pos = Vector2.SmoothDamp(
                    _content.anchoredPosition,
                    target,
                    ref _springVelocity,
                    Mathf.Max(0.001f, _elasticity),
                    Mathf.Infinity,
                    dt);

                SetContentAnchoredPosition(pos);

                _velocity = Vector2.zero;
                _hasPrev = false;
                return;
            }

            if (_snapping)
            {
                float curAxis = GetAxisValue(_content.anchoredPosition);
                float newAxis = Mathf.SmoothDamp(
                    curAxis,
                    _snapTargetAxis,
                    ref _snapDampVel,
                    Mathf.Max(0.001f, _snapSmoothTime),
                    _snapMaxSpeed,
                    dt);

                Vector2 pos = SetAxisValue(_content.anchoredPosition, newAxis);

                if (_movementType == MovementType.Clamped)
                {
                    Vector2 off = CalculateOffset(pos - _content.anchoredPosition);
                    pos += off;
                }

                SetContentAnchoredPosition(pos);

                if (Mathf.Abs(_snapTargetAxis - newAxis) <= _snapStopDistance)
                {
                    Vector2 finalPos = SetAxisValue(_content.anchoredPosition, _snapTargetAxis);

                    if (_movementType == MovementType.Clamped)
                    {
                        Vector2 off = CalculateOffset(finalPos - _content.anchoredPosition);
                        finalPos += off;
                        _snapTargetAxis = GetAxisValue(finalPos);
                        finalPos = SetAxisValue(finalPos, _snapTargetAxis);
                    }

                    SetContentAnchoredPosition(finalPos);
                    _velocity = Vector2.zero;
                    _snapDampVel = 0f;
                    _snapping = false;
                    _hasPrev = false;
                }

                return;
            }

            if (_inertia)
            {
                if (_axis == Axis.Horizontal) _velocity.y = 0f;
                else _velocity.x = 0f;

                _velocity *= Mathf.Pow(_decelerationRate, dt);

                if (Mathf.Abs(_velocity.x) < 1f) _velocity.x = 0f;
                if (Mathf.Abs(_velocity.y) < 1f) _velocity.y = 0f;

                if (_velocity.x != 0f || _velocity.y != 0f)
                {
                    Vector2 pos = _content.anchoredPosition + _velocity * dt;

                    if (_movementType == MovementType.Clamped)
                    {
                        Vector2 off = CalculateOffset(pos - _content.anchoredPosition);
                        pos += off;
                        _velocity = Vector2.zero;
                    }

                    SetContentAnchoredPosition(pos);
                    UpdateBounds();
                }
            }

            if (_snapEnabled && _pendingSnap)
            {
                float v = GetAxisValue(_velocity);
                if (!_inertia || Mathf.Abs(v) <= _snapStartVelocityThreshold)
                {
                    StartSnap(v);
                    _pendingSnap = false;
                }
            }
        }

        private void BeginDragSelfAt(PointerEventData eventData, Vector2 screenPos)
        {
            if (!_dragging)
                _userDragBeganSubject.OnNext(Unit.Default);

            _dragging = true;
            _decidingDirection = false;
            _routeToParent = false;

            _velocity = Vector2.zero;
            _springVelocity = Vector2.zero;

            _pendingSnap = false;
            _snapping = false;
            _snapDampVel = 0f;
            _wheelSnapTimer = 0f;

            UpdateBounds();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewRect,
                screenPos,
                eventData.pressEventCamera,
                out _pointerStartLocalCursor);

            _contentStartPosition = _content.anchoredPosition;

            _prevContentPos = _content.anchoredPosition;
            _prevTime = TimeNow;
            _hasPrev = true;
        }

        private void DragSelf(PointerEventData eventData)
        {
            if (!_dragging)
                BeginDragSelfAt(eventData, eventData.position);

            UpdateBounds();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localCursor);

            Vector2 pointerDelta = localCursor - _pointerStartLocalCursor;
            Vector2 newPos = _contentStartPosition + pointerDelta;

            Vector2 cur = _content.anchoredPosition;

            if (_axis == Axis.Horizontal) newPos.y = cur.y;
            else newPos.x = cur.x;

            Vector2 deltaMove = newPos - cur;

            if (_movementType != MovementType.Unrestricted)
            {
                Vector2 offset = CalculateOffset(deltaMove);

                if (_movementType == MovementType.Clamped)
                {
                    newPos += offset;
                }
                else
                {
                    if (offset.x != 0f) newPos.x -= RubberDelta(offset.x, _viewBounds.size.x);
                    if (offset.y != 0f) newPos.y -= RubberDelta(offset.y, _viewBounds.size.y);
                }
            }

            SetContentAnchoredPosition(newPos);
            UpdateVelocity();
        }

        private void EndDragSelf()
        {
            if (_dragging)
                _userDragEndedSubject.OnNext(Unit.Default);

            _dragging = false;
            _decidingDirection = false;
            _routeToParent = false;

            if (_snapEnabled)
            {
                _pendingSnap = true;
                _snapping = false;
                _snapDampVel = 0f;
            }
        }

        public void ExternalBeginDragAt(PointerEventData eventData, Vector2 screenPos)
        {
            if (!_isInitialized) return;
            BeginDragSelfAt(eventData, screenPos);
        }

        public void ExternalDrag(PointerEventData eventData)
        {
            if (!_isInitialized) return;
            DragSelf(eventData);
        }

        public void ExternalEndDrag(PointerEventData eventData)
        {
            if (!_isInitialized) return;
            EndDragSelf();
        }

        private void UpdateVelocity()
        {
            float t = TimeNow;
            float dt = t - _prevTime;

            if (!_hasPrev || dt <= 0f)
            {
                _prevContentPos = _content.anchoredPosition;
                _prevTime = t;
                _hasPrev = true;
                return;
            }

            Vector2 cur = _content.anchoredPosition;
            Vector2 v = (cur - _prevContentPos) / dt;

            if (_axis == Axis.Horizontal) v.y = 0f;
            else v.x = 0f;

            _velocity = v;

            _prevContentPos = cur;
            _prevTime = t;
        }

        private void StartSnap(float axisVelocity)
        {
            EnsureSnapTargets();
            int count = _snapTargetsCount;
            if (count <= 0) return;

            UpdateBounds();

            float curAxis = GetAxisValue(_content.anchoredPosition);

            int nearestSorted = FindNearestSortedIndex(curAxis);
            if (nearestSorted < 0) return;

            int chosenSorted = nearestSorted;

            if (_snapUseVelocityToAdvance && Mathf.Abs(axisVelocity) >= _snapAdvanceVelocityThreshold)
            {
                int dir = axisVelocity > 0f ? 1 : -1;
                chosenSorted = Mathf.Clamp(nearestSorted + dir, 0, count - 1);
            }

            float bestAxis = _snapTargetsAxisSorted[chosenSorted];
            float bestDist = Mathf.Abs(bestAxis - curAxis);

            if (bestDist <= _snapStopDistance)
            {
                _snapTargetAxis = bestAxis;
                _snapping = false;
                return;
            }

            Vector2 targetPos = SetAxisValue(_content.anchoredPosition, bestAxis);

            if (_movementType == MovementType.Clamped)
            {
                Vector2 off = CalculateOffset(targetPos - _content.anchoredPosition);
                targetPos += off;
                bestAxis = GetAxisValue(targetPos);
            }

            _snapTargetAxis = bestAxis;
            _velocity = Vector2.zero;
            _snapDampVel = 0f;
            _snapping = true;
            _hasPrev = false;
        }

        private float GetAlignedValue(Bounds b, Axis ax, SnapAlignment align)
        {
            if (ax == Axis.Horizontal)
            {
                if (align == SnapAlignment.Start) return b.min.x;
                if (align == SnapAlignment.End) return b.max.x;
                return b.center.x;
            }

            if (align == SnapAlignment.Start) return b.max.y;
            if (align == SnapAlignment.End) return b.min.y;
            return b.center.y;
        }

        private void SetContentAnchoredPosition(Vector2 position)
        {
            if (_axis == Axis.Horizontal) position.y = _content.anchoredPosition.y;
            else position.x = _content.anchoredPosition.x;

            _content.anchoredPosition = position;
            NotifyPositionChanged(position);
        }

        private void UpdateBounds()
        {
            _viewBounds = new Bounds(_viewRect.rect.center, _viewRect.rect.size);
            _contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_viewRect, _content);
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            if (_movementType == MovementType.Unrestricted) return Vector2.zero;

            Vector2 offset = Vector2.zero;

            Vector3 min = _contentBounds.min + (Vector3)delta;
            Vector3 max = _contentBounds.max + (Vector3)delta;

            if (_axis == Axis.Horizontal)
            {
                if (min.x > _viewBounds.min.x) offset.x = _viewBounds.min.x - min.x;
                else if (max.x < _viewBounds.max.x) offset.x = _viewBounds.max.x - max.x;
            }
            else
            {
                if (min.y > _viewBounds.min.y) offset.y = _viewBounds.min.y - min.y;
                else if (max.y < _viewBounds.max.y) offset.y = _viewBounds.max.y - max.y;
            }

            return offset;
        }

        private float RubberDelta(float overStretching, float viewSize)
        {
            float abs = Mathf.Abs(overStretching);
            float x = (abs * 0.55f) / Mathf.Max(0.0001f, viewSize);
            return (1f - 1f / (x + 1f)) * viewSize * Mathf.Sign(overStretching);
        }

        private float GetAxisValue(Vector2 v)
        {
            return _axis == Axis.Horizontal ? v.x : v.y;
        }

        private Vector2 SetAxisValue(Vector2 v, float a)
        {
            if (_axis == Axis.Horizontal) v.x = a;
            else v.y = a;
            return v;
        }

        public void StopMovement()
        {
            if (!_isInitialized) return;

            _velocity = Vector2.zero;
            _springVelocity = Vector2.zero;
            _hasPrev = false;

            _pendingSnap = false;
            _snapping = false;
            _snapDampVel = 0f;
            _wheelSnapTimer = 0f;

            _decidingDirection = false;
            _routeToParent = false;
        }


        private void EnsureSnapTargetsCapacity(int required)
        {
            if (_snapTargetsAxisByPage.Length >= required) return;

            int newSize = Mathf.NextPowerOfTwo(Mathf.Max(required, 4));
            Array.Resize(ref _snapTargetsAxisByPage, newSize);
            Array.Resize(ref _snapTargetsAxisSorted, newSize);
            Array.Resize(ref _snapPageBySorted, newSize);
        }

        private void EnsureSnapTargets()
        {
            if (!_snapEnabled)
            {
                _snapTargetsCount = 0;
                return;
            }

            Vector2 viewSize = _viewRect.rect.size;
            int childCount = _content.childCount;

            if (!_snapTargetsDirty &&
                _snapTargetsChildCount == childCount &&
                _snapTargetsViewSize == viewSize)
                return;

            _snapTargetsDirty = false;
            _snapTargetsViewSize = viewSize;
            _snapTargetsChildCount = childCount;

            if (childCount <= 0)
            {
                _snapTargetsCount = 0;
                return;
            }

            UpdateBounds();

            float curAxis = GetAxisValue(_content.anchoredPosition);
            float viewAlign = GetAlignedValue(_viewBounds, _axis, _snapAlignment);

            EnsureSnapTargetsCapacity(childCount);

            int page = 0;

            if (_snapPageOrder == SnapPageOrder.Hierarchy)
            {
                for (int i = 0; i < childCount; i++)
                {
                    RectTransform child = (RectTransform)_content.GetChild(i);
                    if (!child.gameObject.activeInHierarchy) continue;

                    Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(_viewRect, child);
                    float childAlign = GetAlignedValue(b, _axis, _snapAlignment);

                    float targetAxis = curAxis + (viewAlign - childAlign);
                    _snapTargetsAxisByPage[page++] = targetAxis;
                }
            }
            else
            {
                for (int i = childCount - 1; i >= 0; i--)
                {
                    RectTransform child = (RectTransform)_content.GetChild(i);
                    if (!child.gameObject.activeInHierarchy) continue;

                    Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(_viewRect, child);
                    float childAlign = GetAlignedValue(b, _axis, _snapAlignment);

                    float targetAxis = curAxis + (viewAlign - childAlign);
                    _snapTargetsAxisByPage[page++] = targetAxis;
                }
            }

            _snapTargetsCount = page;

            if (_snapTargetsCount <= 0)
                return;

            for (int i = 0; i < _snapTargetsCount; i++)
            {
                _snapTargetsAxisSorted[i] = _snapTargetsAxisByPage[i];
                _snapPageBySorted[i] = i;
            }

            Array.Sort(_snapTargetsAxisSorted, _snapPageBySorted, 0, _snapTargetsCount);
        }

        private int FindNearestSortedIndex(float axis)
        {
            int count = _snapTargetsCount;
            if (count <= 0) return -1;

            float[] arr = _snapTargetsAxisSorted;
            int hi = count - 1;

            if (axis <= arr[0]) return 0;
            if (axis >= arr[hi]) return hi;

            int lo = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                float v = arr[mid];

                if (v < axis) lo = mid + 1;
                else if (v > axis) hi = mid - 1;
                else return mid;
            }

            int next = lo;
            int prev = lo - 1;
            return (axis - arr[prev] <= arr[next] - axis) ? prev : next;
        }

        private float CalculateSnapPagePosition(float axis)
        {
            int count = _snapTargetsCount;
            if (count <= 1) return 0f;

            float first = _snapTargetsAxisByPage[0];
            float last = _snapTargetsAxisByPage[count - 1];
            bool increasing = last > first;

            if (increasing)
            {
                if (axis <= first) return 0f;
                if (axis >= last) return count - 1;

                for (int i = 0; i < count - 1; i++)
                {
                    float a0 = _snapTargetsAxisByPage[i];
                    float a1 = _snapTargetsAxisByPage[i + 1];
                    if (axis <= a1)
                    {
                        float t = Mathf.InverseLerp(a0, a1, axis);
                        return i + t;
                    }
                }
            }
            else
            {
                if (axis >= first) return 0f;
                if (axis <= last) return count - 1;

                for (int i = 0; i < count - 1; i++)
                {
                    float a0 = _snapTargetsAxisByPage[i];
                    float a1 = _snapTargetsAxisByPage[i + 1];
                    if (axis >= a1)
                    {
                        float t = Mathf.InverseLerp(a0, a1, axis);
                        return i + t;
                    }
                }
            }

            return count - 1;
        }


        private void NotifyPositionChanged(Vector2 position)
        {
            if (_hasLastNotifiedPos && position == _lastNotifiedPos)
                return;

            _hasLastNotifiedPos = true;
            _lastNotifiedPos = position;

            _contentPosSubject.OnNext(position);

            float axis = GetAxisValue(position);
            _axisPosSubject.OnNext(axis);

            if (!_snapEnabled)
                return;

            EnsureSnapTargets();
            if (_snapTargetsCount <= 0)
                return;

            int nearestSorted = FindNearestSortedIndex(axis);
            if (nearestSorted >= 0)
            {
                int pageIndex = _snapPageBySorted[nearestSorted];
                if (pageIndex != _lastNotifiedSnapIndex)
                {
                    _lastNotifiedSnapIndex = pageIndex;
                    _snapIndexSubject.OnNext(pageIndex);
                }
            }

            float pagePos = CalculateSnapPagePosition(axis);
            if (float.IsNaN(_lastNotifiedSnapPagePos) || Mathf.Abs(pagePos - _lastNotifiedSnapPagePos) > SnapNotifyEpsilon)
            {
                _lastNotifiedSnapPagePos = pagePos;
                _snapPagePosSubject.OnNext(pagePos);
            }

            float page01 = (_snapTargetsCount <= 1) ? 0f : pagePos / (_snapTargetsCount - 1);
            if (float.IsNaN(_lastNotifiedSnap01) || Mathf.Abs(page01 - _lastNotifiedSnap01) > SnapNotifyEpsilon)
            {
                _lastNotifiedSnap01 = page01;
                _snapPage01Subject.OnNext(page01);
            }
        }


        public float GetNormalizedPosition()
        {
            if (!_isInitialized) return 0f;

            UpdateBounds();

            float contentSize = _axis == Axis.Horizontal ? _contentBounds.size.x : _contentBounds.size.y;
            float viewSize = _axis == Axis.Horizontal ? _viewBounds.size.x : _viewBounds.size.y;

            float hidden = contentSize - viewSize;
            if (hidden <= 0.0001f) return 0f;

            float viewMin = _axis == Axis.Horizontal ? _viewBounds.min.x : _viewBounds.min.y;
            float contentMin = _axis == Axis.Horizontal ? _contentBounds.min.x : _contentBounds.min.y;

            float t = (viewMin - contentMin) / hidden;
            return Mathf.Clamp01(t);
        }

        public void SetNormalizedPosition(float normalized, bool immediate = true)
        {
            if (!_isInitialized) return;

            normalized = Mathf.Clamp01(normalized);

            UpdateBounds();

            float contentSize = _axis == Axis.Horizontal ? _contentBounds.size.x : _contentBounds.size.y;
            float viewSize = _axis == Axis.Horizontal ? _viewBounds.size.x : _viewBounds.size.y;

            float hidden = contentSize - viewSize;
            if (hidden <= 0.0001f)
                return;

            float viewMin = _axis == Axis.Horizontal ? _viewBounds.min.x : _viewBounds.min.y;
            float contentMin = _axis == Axis.Horizontal ? _contentBounds.min.x : _contentBounds.min.y;

            float targetContentMin = viewMin - normalized * hidden;
            float delta = targetContentMin - contentMin;

            Vector2 pos = _content.anchoredPosition;
            float axis = GetAxisValue(pos);
            axis += delta;
            pos = SetAxisValue(pos, axis);

            if (_movementType == MovementType.Clamped)
            {
                Vector2 off = CalculateOffset(pos - _content.anchoredPosition);
                pos += off;
                axis = GetAxisValue(pos);
            }

            if (!immediate && _snapEnabled)
            {
                _snapTargetAxis = axis;
                _velocity = Vector2.zero;
                _snapDampVel = 0f;
                _snapping = true;
                _pendingSnap = false;
                _hasPrev = false;
                return;
            }

            _snapping = false;
            _pendingSnap = false;
            _velocity = Vector2.zero;
            _snapDampVel = 0f;
            _hasPrev = false;

            SetContentAnchoredPosition(pos);
        }

        public void SetPage(int pageIndex, bool immediate = true)
        {
            if (!_isInitialized) return;
            if (!_snapEnabled) return;

            EnsureSnapTargets();
            int count = _snapTargetsCount;
            if (count <= 0) return;

            if (pageIndex < 0) pageIndex = 0;
            else if (pageIndex >= count) pageIndex = count - 1;

            float axis = _snapTargetsAxisByPage[pageIndex];
            Vector2 pos = SetAxisValue(_content.anchoredPosition, axis);

            if (_movementType == MovementType.Clamped)
            {
                Vector2 off = CalculateOffset(pos - _content.anchoredPosition);
                pos += off;
                axis = GetAxisValue(pos);
            }

            if (!immediate)
            {
                _snapTargetAxis = axis;
                _velocity = Vector2.zero;
                _snapDampVel = 0f;
                _snapping = true;
                _pendingSnap = false;
                _hasPrev = false;
                return;
            }

            _snapping = false;
            _pendingSnap = false;
            _velocity = Vector2.zero;
            _snapDampVel = 0f;
            _hasPrev = false;

            SetContentAnchoredPosition(pos);
        }

        public void ResetToStart(bool immediate = true)
        {
            if (_snapEnabled) SetPage(0, immediate);
            else SetNormalizedPosition(0f, immediate);
        }

        public void ResetToEnd(bool immediate = true)
        {
            if (_snapEnabled)
            {
                EnsureSnapTargets();
                SetPage(Mathf.Max(0, _snapTargetsCount - 1), immediate);
            }
            else
            {
                SetNormalizedPosition(1f, immediate);
            }
        }
    }
}
