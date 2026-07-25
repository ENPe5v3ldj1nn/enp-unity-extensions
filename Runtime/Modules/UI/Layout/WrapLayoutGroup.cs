using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Layout/Wrap Layout Group")]
public class WrapLayoutGroup : LayoutGroup
{
    private enum WrapDirection
    {
        Horizontal,
        Vertical
    }

    [SerializeField]
    private Vector2 _spacing = Vector2.zero;
    [SerializeField]
    private bool _controlChildWidth = true;
    [SerializeField]
    private bool _controlChildHeight = true;
    [SerializeField]
    private WrapDirection _sortDirection = WrapDirection.Horizontal;

    private float AvailableWidth => rectTransform.rect.width - padding.horizontal;
    private float AvailableHeight => rectTransform.rect.height - padding.vertical;

    private readonly List<Vector2> _childPositions = new();
    private readonly List<float> _rowHeights = new();
    private readonly List<int> _rowStartIndexes = new();
    private readonly List<float> _columnWidths = new();
    private readonly List<float> _columnHeights = new();
    private readonly List<int> _columnStartIndexes = new();

    private float _totalContentWidth;
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
        CalculatePositionsAndSize();
        SetLayoutInputForAxis(_totalContentWidth, _totalContentWidth, -1, 0);
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

    private float GetChildWidth(RectTransform child)
    {
        return _controlChildWidth ? LayoutUtility.GetPreferredWidth(child) : child.rect.width;
    }

    private float GetChildHeight(RectTransform child)
    {
        return _controlChildHeight ? LayoutUtility.GetPreferredHeight(child) : child.rect.height;
    }

    private void CalculatePositionsAndSize()
    {
        _childPositions.Clear();
        _rowHeights.Clear();
        _rowStartIndexes.Clear();
        _columnWidths.Clear();
        _columnHeights.Clear();
        _columnStartIndexes.Clear();

        if (_sortDirection == WrapDirection.Horizontal)
        {
            CalculateHorizontalPositionsAndSize();
        }
        else
        {
            CalculateVerticalPositionsAndSize();
        }
    }

    private void CalculateHorizontalPositionsAndSize()
    {
        float x = padding.left;
        float y = 0f;
        float maxHeight = 0f;
        float currentRowWidth = 0f;
        float maxRowWidth = 0f;
        int startIndex = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float width = GetChildWidth(child);
            float height = GetChildHeight(child);

            if (x + width > AvailableWidth + padding.left && x > padding.left)
            {
                _rowHeights.Add(maxHeight);
                _rowStartIndexes.Add(startIndex);
                maxRowWidth = Mathf.Max(maxRowWidth, currentRowWidth - _spacing.x);
                y += maxHeight + _spacing.y;

                x = padding.left;
                maxHeight = 0f;
                currentRowWidth = 0f;
                startIndex = i;
            }

            _childPositions.Add(new Vector2(x, y));
            x += width + _spacing.x;
            currentRowWidth += width + _spacing.x;
            maxHeight = Mathf.Max(maxHeight, height);
        }

        _rowHeights.Add(maxHeight);
        _rowStartIndexes.Add(startIndex);
        maxRowWidth = Mathf.Max(maxRowWidth, rectChildren.Count > 0 ? currentRowWidth - _spacing.x : 0f);
        _totalContentWidth = maxRowWidth + padding.horizontal;
        _totalContentHeight = y + maxHeight + padding.vertical;
    }

    private void CalculateVerticalPositionsAndSize()
    {
        float x = 0f;
        float y = 0f;
        float maxWidth = 0f;
        float currentColumnHeight = 0f;
        float maxColumnHeight = 0f;
        int startIndex = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float width = GetChildWidth(child);
            float height = GetChildHeight(child);

            if (y + height > AvailableHeight && y > 0f)
            {
                float columnHeight = currentColumnHeight - _spacing.y;
                _columnWidths.Add(maxWidth);
                _columnHeights.Add(columnHeight);
                _columnStartIndexes.Add(startIndex);
                maxColumnHeight = Mathf.Max(maxColumnHeight, columnHeight);

                x += maxWidth + _spacing.x;
                y = 0f;
                maxWidth = 0f;
                currentColumnHeight = 0f;
                startIndex = i;
            }

            _childPositions.Add(new Vector2(x, y));
            y += height + _spacing.y;
            currentColumnHeight += height + _spacing.y;
            maxWidth = Mathf.Max(maxWidth, width);
        }

        float lastColumnHeight = rectChildren.Count > 0 ? currentColumnHeight - _spacing.y : 0f;
        _columnWidths.Add(maxWidth);
        _columnHeights.Add(lastColumnHeight);
        _columnStartIndexes.Add(startIndex);
        maxColumnHeight = Mathf.Max(maxColumnHeight, lastColumnHeight);

        _totalContentWidth = x + maxWidth + padding.horizontal;
        _totalContentHeight = maxColumnHeight + padding.vertical;
    }

    private void SetChildrenPositions()
    {
        if (_sortDirection == WrapDirection.Horizontal)
        {
            SetChildrenPositionsHorizontal();
        }
        else
        {
            SetChildrenPositionsVertical();
        }
    }

    private void SetChildrenPositionsHorizontal()
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
                totalRowWidth += GetChildWidth(rectChildren[i]) + _spacing.x;
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
                float width = GetChildWidth(child);
                float height = GetChildHeight(child);

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

                if (_controlChildWidth)
                {
                    SetChildAlongAxis(child, 0, offsetX, width);
                }
                else
                {
                    SetChildAlongAxis(child, 0, offsetX);
                }

                if (_controlChildHeight)
                {
                    SetChildAlongAxis(child, 1, finalY, height);
                }
                else
                {
                    SetChildAlongAxis(child, 1, finalY);
                }
                offsetX += width + _spacing.x;
            }
        }
    }

    private void SetChildrenPositionsVertical()
    {
        float horizontalOffset = 0f;
        float innerContentWidth = Mathf.Max(0f, _totalContentWidth - padding.horizontal);

        switch (childAlignment)
        {
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                horizontalOffset = (AvailableWidth - innerContentWidth) / 2f;
                break;
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                horizontalOffset = AvailableWidth - innerContentWidth;
                break;
        }

        for (int column = 0; column < _columnHeights.Count; column++)
        {
            int start = _columnStartIndexes[column];
            int end = (column + 1 < _columnStartIndexes.Count) ? _columnStartIndexes[column + 1] : rectChildren.Count;

            float totalColumnWidth = _columnWidths[column];
            float totalColumnHeight = _columnHeights[column];
            float offsetY = 0f;

            switch (childAlignment)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    offsetY = (AvailableHeight - totalColumnHeight) / 2f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    offsetY = AvailableHeight - totalColumnHeight;
                    break;
            }

            for (int i = start; i < end; i++)
            {
                RectTransform child = rectChildren[i];
                float width = GetChildWidth(child);
                float height = GetChildHeight(child);

                float x = _childPositions[i].x;
                float finalX = x + horizontalOffset + padding.left;

                switch (childAlignment)
                {
                    case TextAnchor.UpperCenter:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.LowerCenter:
                        finalX += (totalColumnWidth - width) / 2f;
                        break;
                    case TextAnchor.UpperRight:
                    case TextAnchor.MiddleRight:
                    case TextAnchor.LowerRight:
                        finalX += (totalColumnWidth - width);
                        break;
                }

                float y = _childPositions[i].y;
                float finalY = y + offsetY + padding.top;

                switch (childAlignment)
                {
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.MiddleRight:
                        finalY += (totalColumnHeight - height) / 2f;
                        break;
                    case TextAnchor.LowerLeft:
                    case TextAnchor.LowerCenter:
                    case TextAnchor.LowerRight:
                        finalY += (totalColumnHeight - height);
                        break;
                }

                if (_controlChildWidth)
                {
                    SetChildAlongAxis(child, 0, finalX, width);
                }
                else
                {
                    SetChildAlongAxis(child, 0, finalX);
                }

                if (_controlChildHeight)
                {
                    SetChildAlongAxis(child, 1, finalY, height);
                }
                else
                {
                    SetChildAlongAxis(child, 1, finalY);
                }
            }
        }
    }
}
