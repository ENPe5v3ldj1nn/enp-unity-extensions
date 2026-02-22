using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace enp_unity_extensions.Runtime.Scripts.UI.Scroll
{
    [DisallowMultipleComponent]
    public sealed class Scroll : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public enum Axis { Horizontal, Vertical }
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public enum SnapAlignment { Start, Center, End }
        public enum NestedMode { DominantAxis, Simultaneous }

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
        [SerializeField] private bool _snapUseVelocityToAdvance = true;
        [SerializeField] private float _snapAdvanceVelocityThreshold = 1200f;
        [SerializeField] private float _snapStartVelocityThreshold = 800f;
        [SerializeField] private float _snapSmoothTime = 0.12f;
        [SerializeField] private float _snapMaxSpeed = 20000f;
        [SerializeField] private float _snapStopDistance = 0.5f;
        [SerializeField] private bool _snapAfterWheel = true;
        [SerializeField] private float _snapWheelDelay = 0.08f;

        private RectTransform viewRect;

        private Bounds viewBounds;
        private Bounds contentBounds;

        private Vector2 pointerStartLocalCursor;
        private Vector2 contentStartPosition;

        private Vector2 velocity;
        private Vector2 springVelocity;

        private bool dragging;

        private Vector2 prevContentPos;
        private float prevTime;
        private bool hasPrev;

        private bool pendingSnap;
        private bool snapping;
        private float snapTargetAxis;
        private float snapDampVel;
        private float wheelSnapTimer;

        private bool decidingDirection;
        private bool routeToParent;
        private Vector2 pressScreenPos;
        private Vector2 lastScreenPosForSimultaneous;

        private float TimeNow => _useUnscaledTime ? Time.unscaledTime : Time.time;
        private float DeltaTime => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private void Awake()
        {
            viewRect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            dragging = false;

            velocity = Vector2.zero;
            springVelocity = Vector2.zero;

            hasPrev = false;

            pendingSnap = false;
            snapping = false;
            snapDampVel = 0f;
            wheelSnapTimer = 0f;

            decidingDirection = false;
            routeToParent = false;

            pressScreenPos = Vector2.zero;
            lastScreenPosForSimultaneous = Vector2.zero;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            pendingSnap = false;
            snapping = false;
            snapDampVel = 0f;
            wheelSnapTimer = 0f;

            pressScreenPos = eventData.position;
            lastScreenPosForSimultaneous = eventData.position;

            decidingDirection = false;
            routeToParent = false;
            dragging = false;

            if (_parentScroll != null && _nestedMode == NestedMode.Simultaneous)
            {
                BeginDragSelfAt(eventData, pressScreenPos);
                _parentScroll.ExternalBeginDragAt(eventData, pressScreenPos);
                return;
            }

            if (_parentScroll != null && _nestedMode == NestedMode.DominantAxis)
            {
                decidingDirection = true;
                return;
            }

            BeginDragSelfAt(eventData, pressScreenPos);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_parentScroll != null && _nestedMode == NestedMode.Simultaneous)
            {
                DragSelf(eventData);

                Vector2 d = eventData.position - lastScreenPosForSimultaneous;
                lastScreenPosForSimultaneous = eventData.position;

                float parentAxis = _parentScroll._axis == Axis.Horizontal ? d.x : d.y;
                if (Mathf.Abs(parentAxis) >= _parentAxisDeadZone)
                    _parentScroll.ExternalDrag(eventData);

                return;
            }

            if (decidingDirection)
            {
                Vector2 d = eventData.position - pressScreenPos;
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

                decidingDirection = false;

                if (chooseParent && _parentScroll != null)
                {
                    routeToParent = true;
                    _parentScroll.ExternalBeginDragAt(eventData, pressScreenPos);
                    _parentScroll.ExternalDrag(eventData);
                    return;
                }

                routeToParent = false;
                BeginDragSelfAt(eventData, pressScreenPos);
                DragSelf(eventData);
                return;
            }

            if (routeToParent)
            {
                _parentScroll.ExternalDrag(eventData);
                return;
            }

            DragSelf(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_parentScroll != null && _nestedMode == NestedMode.Simultaneous)
            {
                EndDragSelf();
                _parentScroll.ExternalEndDrag(eventData);
                return;
            }

            if (routeToParent)
            {
                _parentScroll.ExternalEndDrag(eventData);
                routeToParent = false;
                decidingDirection = false;
                return;
            }

            decidingDirection = false;
            EndDragSelf();
        }

        public void OnScroll(PointerEventData eventData)
        {
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
                wheelSnapTimer = Mathf.Max(wheelSnapTimer, _snapWheelDelay);
                pendingSnap = false;
                snapping = false;
                snapDampVel = 0f;
            }
        }

        private void LateUpdate()
        {
            float dt = DeltaTime;
            if (dt <= 0f) return;
            if (dragging) return;
            if (decidingDirection) return;
            if (routeToParent) return;

            if (wheelSnapTimer > 0f)
            {
                wheelSnapTimer -= dt;
                if (wheelSnapTimer <= 0f && _snapEnabled) pendingSnap = true;
            }

            UpdateBounds();

            Vector2 offset = CalculateOffset(Vector2.zero);

            if (_movementType == MovementType.Elastic && (offset.x != 0f || offset.y != 0f))
            {
                snapping = false;
                if (_snapEnabled) pendingSnap = true;

                Vector2 target = _content.anchoredPosition + offset;

                if (_axis == Axis.Horizontal) target.y = _content.anchoredPosition.y;
                else target.x = _content.anchoredPosition.x;

                Vector2 pos = Vector2.SmoothDamp(_content.anchoredPosition, target, ref springVelocity, Mathf.Max(0.001f, _elasticity), Mathf.Infinity, dt);
                SetContentAnchoredPosition(pos);

                velocity = Vector2.zero;
                hasPrev = false;
                return;
            }

            if (snapping)
            {
                float curAxis = GetAxisValue(_content.anchoredPosition);
                float newAxis = Mathf.SmoothDamp(curAxis, snapTargetAxis, ref snapDampVel, Mathf.Max(0.001f, _snapSmoothTime), _snapMaxSpeed, dt);

                Vector2 pos = SetAxisValue(_content.anchoredPosition, newAxis);

                if (_movementType == MovementType.Clamped)
                {
                    Vector2 off = CalculateOffset(pos - _content.anchoredPosition);
                    pos += off;
                }

                SetContentAnchoredPosition(pos);

                if (Mathf.Abs(snapTargetAxis - newAxis) <= _snapStopDistance)
                {
                    Vector2 finalPos = SetAxisValue(_content.anchoredPosition, snapTargetAxis);

                    if (_movementType == MovementType.Clamped)
                    {
                        Vector2 off = CalculateOffset(finalPos - _content.anchoredPosition);
                        finalPos += off;
                        snapTargetAxis = GetAxisValue(finalPos);
                        finalPos = SetAxisValue(finalPos, snapTargetAxis);
                    }

                    SetContentAnchoredPosition(finalPos);
                    velocity = Vector2.zero;
                    snapDampVel = 0f;
                    snapping = false;
                    hasPrev = false;
                }

                return;
            }

            if (_inertia)
            {
                if (_axis == Axis.Horizontal) velocity.y = 0f;
                else velocity.x = 0f;

                velocity *= Mathf.Pow(_decelerationRate, dt);

                if (Mathf.Abs(velocity.x) < 1f) velocity.x = 0f;
                if (Mathf.Abs(velocity.y) < 1f) velocity.y = 0f;

                if (velocity.x != 0f || velocity.y != 0f)
                {
                    Vector2 pos = _content.anchoredPosition + velocity * dt;

                    if (_movementType == MovementType.Clamped)
                    {
                        Vector2 off = CalculateOffset(pos - _content.anchoredPosition);
                        pos += off;
                        velocity = Vector2.zero;
                    }

                    SetContentAnchoredPosition(pos);
                    UpdateBounds();
                }
            }

            if (_snapEnabled && pendingSnap)
            {
                float v = GetAxisValue(velocity);
                if (!_inertia || Mathf.Abs(v) <= _snapStartVelocityThreshold)
                {
                    StartSnap(v);
                    pendingSnap = false;
                }
            }
        }

        private void BeginDragSelfAt(PointerEventData eventData, Vector2 screenPos)
        {
            dragging = true;
            decidingDirection = false;
            routeToParent = false;

            velocity = Vector2.zero;
            springVelocity = Vector2.zero;

            pendingSnap = false;
            snapping = false;
            snapDampVel = 0f;
            wheelSnapTimer = 0f;

            UpdateBounds();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, screenPos, eventData.pressEventCamera, out pointerStartLocalCursor);
            contentStartPosition = _content.anchoredPosition;

            prevContentPos = _content.anchoredPosition;
            prevTime = TimeNow;
            hasPrev = true;
        }

        private void DragSelf(PointerEventData eventData)
        {
            if (!dragging)
                BeginDragSelfAt(eventData, eventData.position);

            UpdateBounds();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out Vector2 localCursor);

            Vector2 pointerDelta = localCursor - pointerStartLocalCursor;
            Vector2 newPos = contentStartPosition + pointerDelta;

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
                    if (offset.x != 0f) newPos.x -= RubberDelta(offset.x, viewBounds.size.x);
                    if (offset.y != 0f) newPos.y -= RubberDelta(offset.y, viewBounds.size.y);
                }
            }

            SetContentAnchoredPosition(newPos);
            UpdateVelocity();
        }

        private void EndDragSelf()
        {
            dragging = false;
            decidingDirection = false;
            routeToParent = false;

            if (_snapEnabled)
            {
                pendingSnap = true;
                snapping = false;
                snapDampVel = 0f;
            }
        }

        public void ExternalBeginDragAt(PointerEventData eventData, Vector2 screenPos)
        {
            BeginDragSelfAt(eventData, screenPos);
        }

        public void ExternalDrag(PointerEventData eventData)
        {
            DragSelf(eventData);
        }

        public void ExternalEndDrag(PointerEventData eventData)
        {
            EndDragSelf();
        }

        private void UpdateVelocity()
        {
            float t = TimeNow;
            float dt = t - prevTime;

            if (!hasPrev || dt <= 0f)
            {
                prevContentPos = _content.anchoredPosition;
                prevTime = t;
                hasPrev = true;
                return;
            }

            Vector2 cur = _content.anchoredPosition;
            Vector2 v = (cur - prevContentPos) / dt;

            if (_axis == Axis.Horizontal) v.y = 0f;
            else v.x = 0f;

            velocity = v;

            prevContentPos = cur;
            prevTime = t;
        }

        private void StartSnap(float axisVelocity)
        {
            int childCount = _content.childCount;
            if (childCount <= 0) return;

            UpdateBounds();

            float curAxis = GetAxisValue(_content.anchoredPosition);

            float[] candidates = new float[childCount];
            int cCount = 0;

            for (int i = 0; i < childCount; i++)
            {
                RectTransform child = (RectTransform)_content.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;

                Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(viewRect, child);

                float childAlign = GetAlignedValue(b, _axis, _snapAlignment);
                float viewAlign = GetAlignedValue(viewBounds, _axis, _snapAlignment);

                float delta = viewAlign - childAlign;
                float targetAxis = curAxis + delta;

                candidates[cCount++] = targetAxis;
            }

            if (cCount <= 0) return;

            Array.Sort(candidates, 0, cCount);

            int nearestIndex = 0;
            float nearestDist = Mathf.Abs(candidates[0] - curAxis);

            for (int i = 1; i < cCount; i++)
            {
                float d = Mathf.Abs(candidates[i] - curAxis);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearestIndex = i;
                }
            }

            int chosenIndex = nearestIndex;

            if (_snapUseVelocityToAdvance && Mathf.Abs(axisVelocity) >= _snapAdvanceVelocityThreshold)
            {
                int dir = axisVelocity > 0f ? 1 : -1;
                chosenIndex = Mathf.Clamp(nearestIndex + dir, 0, cCount - 1);
            }

            float bestAxis = candidates[chosenIndex];
            float bestDist = Mathf.Abs(bestAxis - curAxis);

            if (bestDist <= _snapStopDistance)
            {
                snapTargetAxis = bestAxis;
                snapping = false;
                return;
            }

            Vector2 targetPos = SetAxisValue(_content.anchoredPosition, bestAxis);

            if (_movementType == MovementType.Clamped)
            {
                Vector2 off = CalculateOffset(targetPos - _content.anchoredPosition);
                targetPos += off;
                bestAxis = GetAxisValue(targetPos);
            }

            snapTargetAxis = bestAxis;
            velocity = Vector2.zero;
            snapDampVel = 0f;
            snapping = true;
            hasPrev = false;
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
        }

        private void UpdateBounds()
        {
            viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewRect, _content);
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            if (_movementType == MovementType.Unrestricted) return Vector2.zero;

            Vector2 offset = Vector2.zero;

            Vector3 min = contentBounds.min + (Vector3)delta;
            Vector3 max = contentBounds.max + (Vector3)delta;

            if (_axis == Axis.Horizontal)
            {
                if (min.x > viewBounds.min.x) offset.x = viewBounds.min.x - min.x;
                else if (max.x < viewBounds.max.x) offset.x = viewBounds.max.x - max.x;
            }
            else
            {
                if (min.y > viewBounds.min.y) offset.y = viewBounds.min.y - min.y;
                else if (max.y < viewBounds.max.y) offset.y = viewBounds.max.y - max.y;
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
            velocity = Vector2.zero;
            springVelocity = Vector2.zero;
            hasPrev = false;

            pendingSnap = false;
            snapping = false;
            snapDampVel = 0f;
            wheelSnapTimer = 0f;

            decidingDirection = false;
            routeToParent = false;
        }
    }
}