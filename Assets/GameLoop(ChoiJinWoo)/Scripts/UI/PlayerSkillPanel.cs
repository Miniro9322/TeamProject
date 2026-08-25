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
    private EnviromentManager enviromentManager;
    private PlayerManaManager mana;
    private PlayerSkillCastController cast;

    [Inject]
    private void Construct(GameManager gameManager, EnviromentManager enviromentManager, PlayerManaManager mana)
    {
        this.gameManager = gameManager;
        this.enviromentManager = enviromentManager;
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
            if (entry.button.GetComponent<TooltipTrigger>() is TooltipTrigger tooltip)
            {
                tooltip.SetMessaege(string.Format(DataTableManager.StringTable.Get(slot.skillDescKey), slot.manaCost));
            }
        }

        // ChangeToNight이 아니라 EnviromentManager.OnNight을 쓴다 - ChangeToNight은 밤 전환이
        // "시작"되자마자 발동하는데, 그 순간엔 화면이 아직 밤으로 다 안 바뀌어 있다(빛/스카이박스
        // 전환 애니메이션 진행 중). OnNight은 그 전환이 실제로 다 끝난 시점에 발동한다.
        enviromentManager.OnNight += Show;
        gameManager.ChangeToDay += Hide;
        Hide(); // 시작은 낮이므로 꺼둔다
    }

    private void OnDestroy()
    {
        enviromentManager.OnNight -= Show;
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
