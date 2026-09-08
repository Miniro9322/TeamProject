using UnityEngine;

public class TutorialLineFocus : MonoBehaviour
{
    [SerializeField] private RectTransform topLine;
    [SerializeField] private RectTransform bottomLine;
    [SerializeField] private RectTransform leftLine;
    [SerializeField] private RectTransform rightLine;

    [SerializeField, Min(0.01f)] private float moveTime = 0.6f;
    [SerializeField, Min(0f)] private float holdTime = 0.2f;
    [SerializeField, Min(1f)] private float startScale = 1.5f;

    private float elapsed;
    private bool isMoving;

    private void LateUpdate()
    {
        if (!CanMove())
        {
            StopMove();
            return;
        }

        if (!isMoving)
        {
            BeginMove();
        }

        Rect target = ReadRect();
        AdvanceTime();
        Rect current = GetRect(target);
        ApplyRect(current);
    }

    private void OnDisable()
    {
        StopMove();
    }

    private bool CanMove()
    {
        if (topLine == null || bottomLine == null)
        {
            return false;
        }

        if (leftLine == null || rightLine == null)
        {
            return false;
        }

        if (!topLine.gameObject.activeInHierarchy)
        {
            return false;
        }

        return bottomLine.gameObject.activeInHierarchy
            && leftLine.gameObject.activeInHierarchy
            && rightLine.gameObject.activeInHierarchy;
    }

    private Rect ReadRect()
    {
        Vector2 topSize = topLine.sizeDelta;
        Vector2 leftSize = leftLine.sizeDelta;
        Vector2 topPos = topLine.anchoredPosition;
        Vector2 leftPos = leftLine.anchoredPosition;

        float xMin = topPos.x - topSize.x * 0.5f;
        float xMax = topPos.x + topSize.x * 0.5f;
        float yMin = leftPos.y - leftSize.y * 0.5f;
        float yMax = leftPos.y + leftSize.y * 0.5f;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private void BeginMove()
    {
        elapsed = 0f;
        isMoving = true;
    }

    private void AdvanceTime()
    {
        float totalTime = moveTime + holdTime;
        elapsed += Time.unscaledDeltaTime;
        elapsed = Mathf.Repeat(elapsed, totalTime);
    }

    private float GetProgress()
    {
        if (elapsed >= moveTime)
        {
            return 1f;
        }

        float ratio = elapsed / moveTime;
        float remain = 1f - ratio;
        return 1f - remain * remain * remain;
    }

    private Rect GetRect(Rect target)
    {
        float progress = GetProgress();
        float scale = Mathf.Lerp(startScale, 1f, progress);
        Vector2 size = target.size * scale;
        Vector2 center = target.center;
        Vector2 half = size * 0.5f;

        return Rect.MinMaxRect(
            center.x - half.x,
            center.y - half.y,
            center.x + half.x,
            center.y + half.y);
    }

    private void ApplyRect(Rect current)
    {
        float thickness = topLine.sizeDelta.y;

        SetRect(topLine, current.xMin, current.yMax - thickness, current.xMax, current.yMax);
        SetRect(bottomLine, current.xMin, current.yMin, current.xMax, current.yMin + thickness);
        SetRect(leftLine, current.xMin, current.yMin, current.xMin + thickness, current.yMax);
        SetRect(rightLine, current.xMax - thickness, current.yMin, current.xMax, current.yMax);
    }

    private void SetRect(RectTransform line, float xMin, float yMin, float xMax, float yMax)
    {
        float width = Mathf.Max(0f, xMax - xMin);
        float height = Mathf.Max(0f, yMax - yMin);

        line.sizeDelta = new Vector2(width, height);
        line.anchoredPosition = new Vector2(
            (xMin + xMax) * 0.5f,
            (yMin + yMax) * 0.5f);
    }

    private void StopMove()
    {
        elapsed = 0f;
        isMoving = false;
    }
}
