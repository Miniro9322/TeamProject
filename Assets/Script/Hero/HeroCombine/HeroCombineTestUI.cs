using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 테스트 전용: 맵 클릭으로 선택된 영웅(HeroSelectionService.Current)을 "추가" 버튼으로 최대 3개까지 모으고,
// "합성" 버튼으로 HeroCombineManager.TryCombine(List<Hero>)을 호출해본다.
public class HeroCombineTestUI : MonoBehaviour
{
    [SerializeField] private HeroCombineManager combineManager;
    [SerializeField] private Button addButton;
    [SerializeField] private Button combineButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<Hero> selected = new();

    private void OnEnable()
    {
        addButton.onClick.AddListener(OnAddClicked);
        combineButton.onClick.AddListener(OnCombineClicked);
        clearButton.onClick.AddListener(OnClearClicked);
        Refresh();
    }

    private void OnDisable()
    {
        addButton.onClick.RemoveListener(OnAddClicked);
        combineButton.onClick.RemoveListener(OnCombineClicked);
        clearButton.onClick.RemoveListener(OnClearClicked);
    }

    // 지금 맵에서 선택돼 테두리가 켜진 영웅을 후보 목록에 담는다.
    private void OnAddClicked()
    {
        Hero hero = HeroSelectionService.Current;
        if (hero == null || selected.Count >= 3 || selected.Contains(hero)) return;

        selected.Add(hero);
        Refresh();
    }

    private void OnCombineClicked()
    {
        bool success = combineManager.TryCombine(selected);
        statusText.text = success ? "합성 성공" : "합성 실패 (같은 영웅 3개인지, 다음 티어 데이터가 있는지 확인)";
        selected.Clear();
        Refresh();
    }

    private void OnClearClicked()
    {
        selected.Clear();
        Refresh();
    }

    private void Refresh()
    {
        statusText.text = $"선택됨: {selected.Count}/3";
        combineButton.interactable = selected.Count == 3;
    }
}
