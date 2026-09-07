using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class DayNightButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RectTransform icon;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Animator slideAnim;
    [SerializeField] private Key nightKey = Key.N;
    private static readonly int UpHash = Animator.StringToHash("DayUp");
    private static readonly int DownHash = Animator.StringToHash("DayDown");
    private GameManager gameManager;
    private EnviromentManager enviromentManager;
    private InputAction nightKeyAction;
    private bool canToggle = true;

    private bool started;

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
        icon.transform.rotation = Quaternion.identity;
        gameManager.ChangeToDay += OnDayStart;
        enviromentManager.OnDay += FinishDay;
        dayText.text = $"Day {gameManager.DayCount}";

        nightKeyAction = new InputAction("NightToggle", binding: Keyboard.current[nightKey].path);
        nightKeyAction.performed += OnNightKeyPerformed;
        nightKeyAction.Enable();

        icon.localRotation = gameManager.CanBuild ? Quaternion.identity : Quaternion.Euler(0f, 0f, 180f);

        started = true;
    }

    private void OnNightKeyPerformed(InputAction.CallbackContext context)
    {
        if (!TutorialInputGate.BlockHotkeys) OnButton();
    }

    private void OnButton()
    {
        if (!CanToggle())
        {
            return;
        }

        StartNight();
    }

    private bool CanToggle()
    {
        return canToggle;
    }

    private void StartNight()
    {
        canToggle = false;
        slideAnim.Play(DownHash, 0, 0f);
        gameManager.OnNight();
        RotateIconBy(180f, null).Forget();
    }

    private void OnDayStart()
    {
        RotateIconBy(180f, null).Forget();
    }

    private void FinishDay()
    {
        slideAnim.Play(UpHash, 0, 0f);
        button.interactable = true;
        canToggle = true;
    }

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
        if (!started)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        gameManager.ChangeToDay -= OnDayStart;
        enviromentManager.OnDay -= FinishDay;

        nightKeyAction.performed -= OnNightKeyPerformed;
        nightKeyAction.Disable();
        nightKeyAction.Dispose();
    }

    public void RestoreNightLock()
    {
        canToggle = false;
        slideAnim.Play(DownHash, 0, 0f);

        RotateIconBy(180f, null).Forget();
    }

    public void RefreshDayText()
    {
        dayText.text = $"Day {gameManager.DayCount}";
    }
}
