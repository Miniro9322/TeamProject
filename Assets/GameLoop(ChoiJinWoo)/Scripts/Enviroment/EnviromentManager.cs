using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using VContainer;

public class EnviromentManager : MonoBehaviour
{
    [SerializeField] private Light sunLight;
    [SerializeField] private float transitionDuration = 2f;
    public float TransitionDuration => transitionDuration;

    [Header("Day Settings")]
    [SerializeField] private Color dayColor = Color.white;
    [SerializeField] private float dayIntensity = 1f;
    [SerializeField] private Color dayAmbient = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Material daySkybox;

    [Header("Night Settings")]
    [SerializeField] private Color nightColor = new Color(0.1f, 0.1f, 0.3f);
    [SerializeField] private float nightIntensity = 0.05f;
    [SerializeField] private Color nightAmbient = new Color(0.05f, 0.05f, 0.1f);
    [SerializeField] private Material nightSkybox;

    private bool isNight = false;
    private GameManager gameManager;
    private ResourcesManager resourcesManager;

    public event Action OnDay;
    public event Action OnNight;

    [Inject]
    private void Construct(GameManager gameManager, ResourcesManager resourcesManager)
    {
        this.gameManager = gameManager;
        this.resourcesManager = resourcesManager;
    }

    private void Awake()
    {
        gameManager.ChangeToDay += ToggleDayNight;
        gameManager.ChangeToNight += ToggleDayNight;
    }

    private void Start()
    {
        SetDay();
    }

    public void ToggleDayNight()
    {
        isNight = !isNight;

        TransitionRoutine(isNight).Forget();
    }

    public void SetSunLight(Light light)
    {
        sunLight = light;
    }

    private void SetNight()
    {
        sunLight.color = nightColor;
        sunLight.intensity = nightIntensity;
        RenderSettings.ambientLight = nightAmbient;
        RenderSettings.skybox = nightSkybox;
        DynamicGI.UpdateEnvironment();
    }

    private void SetDay()
    {
        sunLight.color = dayColor;
        sunLight.intensity = dayIntensity;
        RenderSettings.ambientLight = dayAmbient;
        RenderSettings.skybox = daySkybox;
        DynamicGI.UpdateEnvironment();
    }

    private async UniTaskVoid TransitionRoutine(bool toNight)
    {
        if (sunLight == null) return;

        Color startColor = sunLight.color;
        Color targetColor = toNight ? nightColor : dayColor;

        float startIntensity = sunLight.intensity;
        float targetIntensity = toNight ? nightIntensity : dayIntensity;

        Color startAmbient = RenderSettings.ambientLight;
        Color targetAmbient = toNight ? nightAmbient : dayAmbient;

        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            if (sunLight == null) return;

            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / transitionDuration;

            sunLight.color = Color.Lerp(startColor, targetColor, t);
            sunLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, t);

            await UniTask.Yield(cancellationToken);
        }

        if (sunLight == null) return;

        // 최종값 정확히 세팅
        sunLight.color = targetColor;
        sunLight.intensity = targetIntensity;
        RenderSettings.ambientLight = targetAmbient;
        if (isNight)
        {
            SetNight();
            gameManager.ChangeCanSpawnEnemy(true);
            OnNight?.Invoke();
        }
        else
        {
            SetDay();
            gameManager.ChangeCanBuild(true);
            OnDay?.Invoke();
            if (gameManager.perfactDefence)
                resourcesManager.GetSpecial();
        }
    }
}