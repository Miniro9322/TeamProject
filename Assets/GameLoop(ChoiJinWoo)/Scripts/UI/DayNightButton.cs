using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static System.Net.Mime.MediaTypeNames;

public class DayNightButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RectTransform icon;
    [SerializeField] private TextMeshProUGUI text;
    private GameManager gameManager;
    private EnviromentManager enviromentManager;

    [Inject]
    private void Construct(GameManager gameManager, EnviromentManager enviromentManager)
    {
        this.gameManager = gameManager;
        this.enviromentManager = enviromentManager;
    }

    private void Start()
    {
        button.onClick.AddListener(OnButton);
        enviromentManager.OnDay += EnableButton;
    }

    private void OnButton()
    {
        gameManager.OnNight();
        text.gameObject.SetActive(false);
        RotateThenHide().Forget();
    }

    // EnviromentManager.TransitionRoutine과 같은 시간(TransitionDuration) 동안 아이콘을 180도 돌리고,
    // 끝나면 숨긴다 - 지금까지는 클릭하자마자 바로 숨겨서 회전이 보일 틈이 없었다.
    private async UniTaskVoid RotateThenHide()
    {
        button.interactable = false; // 회전 끝나기 전 중복 클릭 방지

        if (icon != null)
        {
            float duration = enviromentManager.TransitionDuration;
            Quaternion start = icon.localRotation;
            Quaternion target = start * Quaternion.Euler(0f, 0f, 180f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                icon.localRotation = Quaternion.Slerp(start, target, elapsed / duration);
                await UniTask.Yield();
            }
            icon.localRotation = target;
        }

        gameObject.SetActive(false);
    }

    private void EnableButton()
    {
        gameObject.SetActive(true);
        button.interactable = true;
        if (icon != null) icon.localRotation = Quaternion.identity; // 다음 낮에 다시 누를 수 있게 원위치
        text.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
        enviromentManager.OnDay -= EnableButton;
    }
}
