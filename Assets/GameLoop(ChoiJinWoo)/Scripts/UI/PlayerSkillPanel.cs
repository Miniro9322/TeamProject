using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

// 밤에만 보이는 플레이어 스킬 버튼 패널. BuildModePanel(낮에 뜨고 밤에 숨음)과 반대 방향으로 켜고 끈다.
public class PlayerSkillPanel : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public Button button;
        public PlayerSkillSlot skill;
    }

    [SerializeField] private List<Entry> entries;
    [SerializeField] private Slider manaBar;

    private GameManager gameManager;
    private PlayerManaManager mana;
    private PlayerSkillCastController cast;

    [Inject]
    private void Construct(GameManager gameManager, PlayerManaManager mana)
    {
        this.gameManager = gameManager;
        this.mana = mana;
    }

    // MapAssemble이 조립 직후 호출한다.
    public void Bind(PlayerSkillCastController cast) => this.cast = cast;

    private void Start()
    {
        foreach (Entry entry in entries)
        {
            PlayerSkillSlot slot = entry.skill;
            entry.button.onClick.AddListener(() => cast?.ArmSkill(slot));
        }

        gameManager.ChangeToNight += Show;
        gameManager.ChangeToDay += Hide;
        Hide(); // 시작은 낮이므로 꺼둔다
    }

    private void OnDestroy()
    {
        gameManager.ChangeToNight -= Show;
        gameManager.ChangeToDay -= Hide;
    }

    private void Show() => gameObject.SetActive(true);
    private void Hide() => gameObject.SetActive(false);

    private void Update()
    {
        if (manaBar != null)
        {
            manaBar.maxValue = mana.MaxMana;
            manaBar.value = mana.CurrentMana;
        }

        foreach (Entry entry in entries)
        {
            entry.button.interactable = mana.CurrentMana >= entry.skill.manaCost;
            entry.button.transition = entry.button.interactable ? Selectable.Transition.ColorTint : Selectable.Transition.None;
        }
    }
}
