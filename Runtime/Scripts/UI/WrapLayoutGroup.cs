using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Layout/Wrap Layout Group")]
public class WrapLayoutGroup : LayoutGroup
{
    [SerializeField]
    private Vector2 _spacing = Vector2.zero;
    private float AvailableWidth => rectTransform.rect.width - padding.horizontal;

    private readonly List<Vector2> _childPositions = new();
    private readonly List<float> _rowHeights = new();
    private readonly List<int> _rowStartIndexes = new();

    private float _totalContentHeight;

    private bool _pendingRebuild;
    private bool _rebuildInProgress;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_pendingRebuild)
        {
            _pendingRebuild = false;
            StartRebuild();
        }
        else
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _rebuildInProgress = false;
    }

    public void RequestRebuild()
    {
        if (!isActiveAndEnabled)
        {
            _pendingRebuild = true;
            return;
        }

        StartRebuild();
    }

    private void StartRebuild()
    {
        if (_rebuildInProgress)
            return;

        StartCoroutine(RebuildCoroutine());
    }

    private IEnumerator RebuildCoroutine()
    {
        _rebuildInProgress = true;
        yield return null;

        if (this != null && isActiveAndEnabled)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        _rebuildInProgress = false;
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
    }

    public override void CalculateLayoutInputVertical()
    {
        CalculatePositionsAndSize();
        SetLayoutInputForAxis(_totalContentHeight, _totalContentHeight, -1, 1);
    }

    public override void SetLayoutHorizontal() { }

    public override void SetLayoutVertical()
    {
        SetChildrenPositions();
    }

    private void CalculatePositionsAndSize()
    {
        _childPositions.Clear();
        _rowHeights.Clear();
        _rowStartIndexes.Clear();

        float x = padding.left;
        float y = 0f;
        float maxHeight = 0f;
        int startIndex = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float width = LayoutUtility.GetPreferredWidth(child);
            float height = LayoutUtility.GetPreferredHeight(child);

            if (x + width > AvailableWidth + padding.left && x > padding.left)
            {
                _rowHeights.Add(maxHeight);
                _rowStartIndexes.Add(startIndex);
                y += maxHeight + _spacing.y;

                x = padding.left;
                maxHeight = 0f;
                startIndex = i;
            }

            _childPositions.Add(new Vector2(x, y));
            x += width + _spacing.x;
            maxHeight = Mathf.Max(maxHeight, height);
        }

        _rowHeights.Add(maxHeight);
        _rowStartIndexes.Add(startIndex);
        _totalContentHeight = y + maxHeight + padding.vertical;
    }

    private void SetChildrenPositions()
    {
        float containerHeight = rectTransform.rect.height;
        float verticalOffset = 0f;

        switch (childAlignment)
        {
            case TextAnchor.MiddleLeft:
            case TextAnchor.MiddleCenter:
            case TextAnchor.MiddleRight:
                verticalOffset = (containerHeight - _totalContentHeight) / 2f;
                break;
            case TextAnchor.LowerLeft:
            case TextAnchor.LowerCenter:
            case TextAnchor.LowerRight:
                verticalOffset = containerHeight - _totalContentHeight - padding.bottom;
                break;
        }

        for (int row = 0; row < _rowHeights.Count; row++)
        {
            int start = _rowStartIndexes[row];
            int end = (row + 1 < _rowStartIndexes.Count) ? _rowStartIndexes[row + 1] : rectChildren.Count;

            float totalRowWidth = 0f;

            for (int i = start; i < end; i++)
            {
                totalRowWidth += LayoutUtility.GetPreferredWidth(rectChildren[i]) + _spacing.x;
            }

            if (totalRowWidth > 0)
                totalRowWidth -= _spacing.x;

            float offsetX = padding.left;

            switch (childAlignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    offsetX += (AvailableWidth - totalRowWidth) / 2f;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    offsetX += AvailableWidth - totalRowWidth;
                    break;
            }

            for (int i = start; i < end; i++)
            {
                RectTransform child = rectChildren[i];
                float width = LayoutUtility.GetPreferredWidth(child);
                float height = LayoutUtility.GetPreferredHeight(child);

                float y = _childPositions[i].y;
                float finalY = y;

                switch (childAlignment)
                {
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.MiddleRight:
                        finalY += (_rowHeights[row] - height) / 2f;
                        break;
                    case TextAnchor.LowerLeft:
                    case TextAnchor.LowerCenter:
                    case TextAnchor.LowerRight:
                        finalY += (_rowHeights[row] - height);
                        break;
                }

                finalY += verticalOffset + padding.top;

                SetChildAlongAxis(child, 0, offsetX, width);
                SetChildAlongAxis(child, 1, finalY, height);
                offsetX += width + _spacing.x;
            }
        }
    }
}
