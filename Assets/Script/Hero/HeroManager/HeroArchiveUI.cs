using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.UI;

public class HeroArchiveUI : MonoBehaviour
{
    [SerializeField] private HeroArchiveItem itemPrefab;
    [SerializeField] private HeroRegistry registry;
    [SerializeField] private Transform listContent;

    [SerializeField] private Image mainImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button exitButton;
    [SerializeField] private Animator bookAnimator;
    [SerializeField] private CanvasGroup leftPanel;
    [SerializeField] private CanvasGroup rightPanel;
    private float fadeDuration = 0.35f;
    private CancellationTokenSource revealCts;
    private bool isBookOpening = false;

    private void Awake()
    {
        BuildList();
    }

    //private void OnDisable()
    //{
    //    ClearList();
    //}
    private void BuildList()
    {
        //ClearList();
        foreach (HeroData data in registry.AllHeroDatas)
        {
            HeroArchiveItem item = Instantiate(itemPrefab, listContent);
            item.Setup(data, OnHeroArchiveClicked);
        }
        HeroData heroData = registry.AllHeroDatas[0];
        mainImage.sprite = heroData.Icon;
        nameText.text = DataTableManager.StringTable.Get(heroData.HeroNameKey);
        descText.text = DataTableManager.StringTable.Get(heroData.HeroDescriptionKey);
    }

    private void OnHeroArchiveClicked(HeroData picked)
    {
        PlayOpenAndReveal();
        mainImage.sprite = picked.Icon;
        nameText.text = DataTableManager.StringTable.Get(picked.HeroNameKey);
        descText.text = DataTableManager.StringTable.Get(picked.HeroDescriptionKey);
    }

    private void PlayOpenAndReveal()
    {
        CancelReveal();
        if (bookAnimator != null)
        {
            bookAnimator.ResetTrigger("Open");
            bookAnimator.SetTrigger("Open");
        }
        SetRevealAlpha(0f);
        isBookOpening = true;
        revealCts = new CancellationTokenSource();
        RevealAfterOpen(revealCts).Forget();
    }

    private void CancelReveal()
    {
        revealCts?.Cancel();
        revealCts?.Dispose();
        revealCts = null;
        isBookOpening = false;
    }

    private void SetRevealAlpha(float a)
    {
        leftPanel.alpha = a;
        rightPanel.alpha = a;
    }

    private async UniTask RevealAfterOpen(CancellationTokenSource own)
    {
        CancellationToken token = own.Token;
        try
        {
            float t = 0f;
            float revealDelay = 0.5f;

            while (t < revealDelay)
            {
                t += Time.unscaledDeltaTime;
                await UniTask.Yield(token);
            }
            isBookOpening = false;

            t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                SetRevealAlpha(Mathf.Clamp01(t / fadeDuration));
                await UniTask.Yield(token);
            }
            SetRevealAlpha(1f);
        }
        finally
        {
            if (revealCts == own)
            {
                isBookOpening = false;
            }
        }
    }

    private void ClearList()
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);
    }
    public void OnExit()
    {
        gameObject.SetActive(false);
    }
}
