using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GuideClickBurst : MonoBehaviour
{
    [SerializeField] private Color burstColor = new Color(0.97f, 0.84f, 0.49f);
    [SerializeField] private float burstSize = 10f;
    [SerializeField] private float burstDuration = 0.34f;

    // 실제로 클릭한 자리에 마름모 파티클을 만들어 터뜨린다
    public void SpawnAt(RectTransform target)
    {
        var burstObject = new GameObject("ClickBurst", typeof(RectTransform), typeof(Image));
        var burstRect = (RectTransform)burstObject.transform;
        burstRect.SetParent(target, false);
        burstRect.anchoredPosition = GetClickLocalPosition(target);
        burstRect.sizeDelta = new Vector2(burstSize, burstSize);
        burstRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var image = burstObject.GetComponent<Image>();
        image.color = burstColor;
        image.raycastTarget = false;

        AnimateBurstAsync(burstRect, image).Forget();
    }

    // 클릭한 화면 좌표를 대상 버튼 기준 로컬 좌표로 바꾼다
    private Vector2 GetClickLocalPosition(RectTransform target)
    {
        var canvas = target.GetComponentInParent<Canvas>();
        var canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screenPoint = Mouse.current.position.ReadValue();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screenPoint, canvasCamera, out localPoint);
        return localPoint;
    }

    // 파티클을 점점 키우면서 투명하게 만들고 끝나면 지운다
    private async UniTaskVoid AnimateBurstAsync(RectTransform burstRect, Image image)
    {
        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / burstDuration;
            float scale = Mathf.Lerp(0.4f, 4f, progress);
            burstRect.localScale = new Vector3(scale, scale, 1f);

            var color = image.color;
            color.a = Mathf.Lerp(1f, 0f, progress);
            image.color = color;

            await UniTask.Yield();
        }
        Destroy(burstRect.gameObject);
    }
}
