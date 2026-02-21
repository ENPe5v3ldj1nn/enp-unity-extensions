using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace enp_unity_extensions.Runtime.Scripts.UI.Scroll
{
    [DisallowMultipleComponent]
    public class Scroll : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public enum Axis { Horizontal, Vertical }
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public enum SnapAlignment { Start, Center, End }

        [SerializeField] RectTransform content;
        [SerializeField] Axis axis = Axis.Vertical;
        [SerializeField] MovementType movementType = MovementType.Elastic;

        [SerializeField] bool inertia = true;
        [SerializeField] float decelerationRate = 0.135f;
        [SerializeField] float elasticity = 0.10f;
        [SerializeField] float scrollSensitivity = 20f;
        [SerializeField] bool useUnscaledTime = true;

        [Header("Nested routing")]
        [SerializeField] Scroll parentScroll;
        [SerializeField] bool routeToParentWhenOppositeAxis = true;
        [SerializeField] float directionDecideThreshold = 10f;

        [Header("Snap")]
        [SerializeField] bool snapEnabled = false;
        [SerializeField] SnapAlignment snapAlignment = SnapAlignment.Center;
        [SerializeField] bool snapUseVelocityToAdvance = true;
        [SerializeField] float snapAdvanceVelocityThreshold = 1200f;
        [SerializeField] float snapStartVelocityThreshold = 800f;
        [SerializeField] float snapSmoothTime = 0.12f;
        [SerializeField] float snapMaxSpeed = 20000f;
        [SerializeField] float snapStopDistance = 0.5f;
        [SerializeField] bool snapAfterWheel = true;
        [SerializeField] float snapWheelDelay = 0.08f;

        RectTransform viewRect;

        Bounds viewBounds;
        Bounds contentBounds;

        Vector2 pointerStartLocalCursor;
        Vector2 contentStartPosition;

        Vector2 velocity;
        Vector2 springVelocity;

        bool dragging;
        bool decidingDirection;
        bool routeToParent;
        Vector2 beginDragScreenPos;

        Vector2 prevContentPos;
        float prevTime;
        bool hasPrev;

        bool pendingSnap;
        bool snapping;
        float snapTargetAxis;
        float snapDampVel;
        float wheelSnapTimer;

        float TimeNow => useUnscaledTime ? Time.unscaledTime : Time.time;
        float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        void Awake()
        {
            viewRect = transform as RectTransform;
            if (content == null && transform.childCount > 0)
                content = transform.GetChild(0) as RectTransform;
        }

        void OnEnable()
        {
            dragging = false;
            decidingDirection = false;
            routeToParent = false;

            hasPrev = false;
            velocity = Vector2.zero;
            springVelocity = Vector2.zero;

            pendingSnap = false;
            snapping = false;
            snapDampVel = 0f;
            wheelSnapTimer = 0f;
        }

        void OnDisable()
        {
            dragging = false;
            decidingDirection = false;
            routeToParent = false;
            pendingSnap = false;
            snapping = false;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsActiveAndValid()) return;

            beginDragScreenPos = eventData.position;
            decidingDirection = routeToParentWhenOppositeAxis && parentScroll != null;
            routeToParent = false;
            dragging = false;
            pendingSnap = false;
            snapping = false;

            if (!decidingDirection)
                BeginDragSelf(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsActiveAndValid()) return;

            if (decidingDirection && !dragging && !routeToParent)
            {
                Vector2 d = eventData.position - beginDragScreenPos;
                if (d.sqrMagnitude < directionDecideThreshold * directionDecideThreshold) return;

                bool dominantForSelf = axis == Axis.Horizontal ? Mathf.Abs(d.x) >= Mathf.Abs(d.y) : Mathf.Abs(d.y) >= Mathf.Abs(d.x);

                if (dominantForSelf)
                {
                    BeginDragSelf(eventData);
                }
                else
                {
                    routeToParent = true;
                    parentScroll.ForwardBeginDrag(eventData);
                }
            }

            if (routeToParent)
            {
                parentScroll.ForwardDrag(eventData);
                return;
            }

            if (!dragging) return;

            UpdateBounds();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out var localCursor);

            Vector2 pointerDelta = localCursor - pointerStartLocalCursor;
            Vector2 newPos = contentStartPosition + pointerDelta;

            var cur = content.anchoredPosition;
            if (axis == Axis.Horizontal) newPos.y = cur.y;
            else newPos.x = cur.x;

            Vector2 deltaMove = newPos - cur;

            if (movementType != MovementType.Unrestricted)
            {
                Vector2 offset = CalculateOffset(deltaMove);

                if (movementType == MovementType.Clamped)
                {
                    newPos += offset;
                }
                else if (movementType == MovementType.Elastic)
                {
                    if (offset.x != 0f) newPos.x -= RubberDelta(offset.x, viewBounds.size.x);
                    if (offset.y != 0f) newPos.y -= RubberDelta(offset.y, viewBounds.size.y);
                }
            }

            SetContentAnchoredPosition(newPos);
            UpdateVelocity();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsActiveAndValid()) return;

            if (routeToParent)
            {
                parentScroll.ForwardEndDrag(eventData);
                routeToParent = false;
                decidingDirection = false;
                return;
            }

            dragging = false;
            decidingDirection = false;

            if (snapEnabled)
            {
                pendingSnap = true;
                snapping = false;
                snapDampVel = 0f;
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!IsActiveAndValid()) return;

            if (routeToParent && parentScroll != null)
            {
                parentScroll.OnScroll(eventData);
                return;
            }

            UpdateBounds();

            Vector2 delta = eventData.scrollDelta;
            delta.y *= -1f;

            if (axis == Axis.Horizontal) delta.y = 0f;
            else delta.x = 0f;

            Vector2 pos = content.anchoredPosition + delta * scrollSensitivity;

            if (movementType == MovementType.Clamped)
            {
                Vector2 offset = CalculateOffset(pos - content.anchoredPosition);
                pos += offset;
            }

            SetContentAnchoredPosition(pos);
            UpdateBounds();

            if (snapEnabled && snapAfterWheel)
            {
                wheelSnapTimer = Mathf.Max(wheelSnapTimer, snapWheelDelay);
                pendingSnap = false;
                snapping = false;
                snapDampVel = 0f;
            }
        }

        void LateUpdate()
        {
            if (!IsActiveAndValid()) return;

            float dt = DeltaTime;
            if (dt <= 0f) return;

            if (wheelSnapTimer > 0f)
            {
                wheelSnapTimer -= dt;
                if (wheelSnapTimer <= 0f && snapEnabled) pendingSnap = true;
            }

            if (!dragging)
            {
                UpdateBounds();
                Vector2 offset = CalculateOffset(Vector2.zero);

                if (movementType == MovementType.Elastic && (offset.x != 0f || offset.y != 0f))
                {
                    snapping = false;
                    if (snapEnabled) pendingSnap = true;

                    Vector2 target = content.anchoredPosition + offset;

                    if (axis == Axis.Horizontal) target.y = content.anchoredPosition.y;
                    else target.x = content.anchoredPosition.x;

                    Vector2 pos = Vector2.SmoothDamp(content.anchoredPosition, target, ref springVelocity, Mathf.Max(0.001f, elasticity), Mathf.Infinity, dt);
                    SetContentAnchoredPosition(pos);
                    velocity = Vector2.zero;
                    hasPrev = false;
                    return;
                }

                if (snapping)
                {
                    float curAxis = GetAxisValue(content.anchoredPosition);
                    float newAxis = Mathf.SmoothDamp(curAxis, snapTargetAxis, ref snapDampVel, Mathf.Max(0.001f, snapSmoothTime), snapMaxSpeed, dt);

                    Vector2 pos = content.anchoredPosition;
                    pos = SetAxisValue(pos, newAxis);

                    if (movementType == MovementType.Clamped)
                    {
                        Vector2 off = CalculateOffset(pos - content.anchoredPosition);
                        pos += off;
                    }

                    SetContentAnchoredPosition(pos);

                    if (Mathf.Abs(snapTargetAxis - newAxis) <= snapStopDistance)
                    {
                        Vector2 finalPos = SetAxisValue(content.anchoredPosition, snapTargetAxis);

                        if (movementType == MovementType.Clamped)
                        {
                            Vector2 off = CalculateOffset(finalPos - content.anchoredPosition);
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

                if (inertia)
                {
                    if (axis == Axis.Horizontal) velocity.y = 0f;
                    else velocity.x = 0f;

                    velocity *= Mathf.Pow(decelerationRate, dt);

                    if (Mathf.Abs(velocity.x) < 1f) velocity.x = 0f;
                    if (Mathf.Abs(velocity.y) < 1f) velocity.y = 0f;

                    if (velocity.x != 0f || velocity.y != 0f)
                    {
                        Vector2 pos = content.anchoredPosition + velocity * dt;

                        if (movementType == MovementType.Clamped)
                        {
                            Vector2 off = CalculateOffset(pos - content.anchoredPosition);
                            pos += off;
                            velocity = Vector2.zero;
                        }

                        SetContentAnchoredPosition(pos);
                        UpdateBounds();
                    }
                }

                if (snapEnabled && pendingSnap)
                {
                    float v = GetAxisValue(velocity);
                    if (!inertia || Mathf.Abs(v) <= snapStartVelocityThreshold)
                    {
                        StartSnap(v);
                        pendingSnap = false;
                    }
                }
            }
        }

        void BeginDragSelf(PointerEventData eventData)
        {
            dragging = true;
            decidingDirection = false;
            routeToParent = false;

            snapping = false;
            pendingSnap = false;
            wheelSnapTimer = 0f;

            velocity = Vector2.zero;
            springVelocity = Vector2.zero;
            snapDampVel = 0f;

            UpdateBounds();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out pointerStartLocalCursor);
            contentStartPosition = content.anchoredPosition;

            prevContentPos = content.anchoredPosition;
            prevTime = TimeNow;
            hasPrev = true;
        }

        void UpdateVelocity()
        {
            float t = TimeNow;
            float dt = t - prevTime;
            if (!hasPrev || dt <= 0f)
            {
                prevContentPos = content.anchoredPosition;
                prevTime = t;
                hasPrev = true;
                return;
            }

            Vector2 cur = content.anchoredPosition;
            Vector2 v = (cur - prevContentPos) / dt;

            if (axis == Axis.Horizontal) v.y = 0f;
            else v.x = 0f;

            velocity = v;

            prevContentPos = cur;
            prevTime = t;
        }

        void StartSnap(float axisVelocity)
        {
            if (content == null) return;
            int childCount = content.childCount;
            if (childCount <= 0) return;

            UpdateBounds();

            float curAxis = GetAxisValue(content.anchoredPosition);

            float bestAxis = curAxis;
            float bestDist = float.PositiveInfinity;

            float[] candidates = null;
            int cCount = 0;

            candidates = new float[childCount];

            for (int i = 0; i < childCount; i++)
            {
                var child = content.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy) continue;

                Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(viewRect, child);

                float childAlign = GetAlignedValue(b, axis, snapAlignment);
                float viewAlign = GetAlignedValue(viewBounds, axis, snapAlignment);

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

            if (snapUseVelocityToAdvance && Mathf.Abs(axisVelocity) >= snapAdvanceVelocityThreshold)
            {
                int dir = axisVelocity > 0f ? 1 : -1;
                chosenIndex = Mathf.Clamp(nearestIndex + dir, 0, cCount - 1);
            }

            bestAxis = candidates[chosenIndex];
            bestDist = Mathf.Abs(bestAxis - curAxis);

            if (bestDist <= snapStopDistance)
            {
                snapTargetAxis = bestAxis;
                snapping = false;
                return;
            }

            Vector2 targetPos = SetAxisValue(content.anchoredPosition, bestAxis);

            if (movementType == MovementType.Clamped)
            {
                Vector2 off = CalculateOffset(targetPos - content.anchoredPosition);
                targetPos += off;
                bestAxis = GetAxisValue(targetPos);
            }

            snapTargetAxis = bestAxis;
            velocity = Vector2.zero;
            snapDampVel = 0f;
            snapping = true;
            hasPrev = false;
        }

        float GetAlignedValue(Bounds b, Axis ax, SnapAlignment align)
        {
            if (ax == Axis.Horizontal)
            {
                if (align == SnapAlignment.Start) return b.min.x;
                if (align == SnapAlignment.End) return b.max.x;
                return b.center.x;
            }
            else
            {
                if (align == SnapAlignment.Start) return b.max.y;
                if (align == SnapAlignment.End) return b.min.y;
                return b.center.y;
            }
        }

        void SetContentAnchoredPosition(Vector2 position)
        {
            if (axis == Axis.Horizontal) position.y = content.anchoredPosition.y;
            else position.x = content.anchoredPosition.x;

            content.anchoredPosition = position;
        }

        void UpdateBounds()
        {
            viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewRect, content);
        }

        Vector2 CalculateOffset(Vector2 delta)
        {
            if (movementType == MovementType.Unrestricted) return Vector2.zero;

            Vector2 offset = Vector2.zero;

            Vector3 min = contentBounds.min + (Vector3)delta;
            Vector3 max = contentBounds.max + (Vector3)delta;

            if (axis == Axis.Horizontal)
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

        float RubberDelta(float overStretching, float viewSize)
        {
            float abs = Mathf.Abs(overStretching);
            if (abs <= 0f) return 0f;
            float x = (abs * 0.55f) / Mathf.Max(0.0001f, viewSize);
            return (1f - 1f / (x + 1f)) * viewSize * Mathf.Sign(overStretching);
        }

        bool IsActiveAndValid()
        {
            return isActiveAndEnabled && content != null && viewRect != null;
        }

        float GetAxisValue(Vector2 v) => axis == Axis.Horizontal ? v.x : v.y;

        Vector2 SetAxisValue(Vector2 v, float a)
        {
            if (axis == Axis.Horizontal) v.x = a;
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
        }

        public void ForwardBeginDrag(PointerEventData eventData)
        {
            decidingDirection = false;
            routeToParent = false;
            dragging = false;
            BeginDragSelf(eventData);
        }

        public void ForwardDrag(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void ForwardEndDrag(PointerEventData eventData)
        {
            OnEndDrag(eventData);
        }
    }
}