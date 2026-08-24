using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class DayNightButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RectTransform icon;
    [SerializeField] private TextMeshProUGUI dayText;
    private GameManager gameManager;
    private EnviromentManager enviromentManager;

    // Quaternion.Slerp은 180도 회전에서 어느 쪽으로 돌지가 애매해서(부동소수점에 따라 달라짐),
    // 방향을 확실히 통제하려고 각도를 직접 실수로 누적한다(래핑 없이 계속 더함).
    private float currentZ = 0f;

    [Inject]
    private void Construct(GameManager gameManager, EnviromentManager enviromentManager)
    {
        this.gameManager = gameManager;
        this.enviromentManager = enviromentManager;
    }

    private void Start()
    {
        button.onClick.AddListener(OnButton);
        // EnviromentManager.OnDay는 낮 전환이 "끝난 뒤" 불리는 이벤트라(빛이 이미 다 바뀐 다음),
        // 그걸 쓰면 아이콘 회전이 실제 빛 전환보다 한 박자 늦게 시작된다. GameManager.ChangeToDay는
        // 전환이 "시작되는" 시점에 불리고 EnviromentManager도 이걸로 빛 전환을 시작하므로,
        // 여기 구독하면 실제 빛 변화와 아이콘 회전이 동시에 시작된다.
        icon.transform.rotation = Quaternion.identity;
        gameManager.ChangeToDay += OnDayStart;
        dayText.text = $"Day {gameManager.DayCount}";
    }

    // 낮 -> 밤: 버튼만 바로 비활성화한다. 아이콘은 계속 떠있는 채로 -180도만큼 돈다(안 숨김).
    private void OnButton()
    {
        gameManager.OnNight();
        button.gameObject.SetActive(false);
        RotateIconBy(180f, null).Forget();
    }

    // 밤 -> 낮: 아이콘이 계속 같은 방향으로 -180도 더 돌고 나면 버튼을 다시 켠다(왕복 아니라 계속 한 방향).
    private void OnDayStart()
    {
        RotateIconBy(180f, () =>
        {
            button.gameObject.SetActive(true);
            button.interactable = true;
        }).Forget();
    }

    // EnviromentManager.TransitionRoutine과 같은 시간(TransitionDuration) 동안 아이콘을 deltaZ만큼 돌린다.
    private async UniTaskVoid RotateIconBy(float deltaZ, Action onComplete)
    {
        if (icon == null)
        {
            onComplete?.Invoke();
            return;
        }

        float duration = enviromentManager.TransitionDuration;
        float startZ = currentZ;
        float targetZ = currentZ + deltaZ;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentZ = Mathf.Lerp(startZ, targetZ, elapsed / duration);
            icon.localRotation = Quaternion.Euler(0f, 0f, currentZ);
            await UniTask.Yield();
        }
        currentZ = targetZ;
        // 한 바퀴(360도) 돌면 값을 0~-360 범위로 접어서 계속 불어나지 않게 한다 - 같은 방향으로 계속
        // 돌되, 시각적으로는 완전히 한 바퀴 돈 자리라 티가 안 난다.
        if (currentZ <= -360f || currentZ >= 360f)
        {
            currentZ %= 360f;
        }
        icon.localRotation = Quaternion.Euler(0f, 0f, currentZ);
        dayText.text = $"Day {gameManager.DayCount}";
        onComplete?.Invoke();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
        gameManager.ChangeToDay -= OnDayStart;
    }

    // 세이브 로드처럼 화면 연출 없이 조용히 일차가 바뀌었을 때 "Day N" 글자만 다시 찍는다 (로드 복원 전용)
    public void RefreshDayText()
    {
        dayText.text = $"Day {gameManager.DayCount}";
    }
}
