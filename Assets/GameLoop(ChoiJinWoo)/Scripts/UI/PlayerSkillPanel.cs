using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private SkillSlide skillSlide;

    private GameManager gameManager;
    private EnviromentManager enviromentManager;
    private PlayerManaManager mana;
    private PlayerSkillCastController cast;
    private Keyboard keyboard;
    [SerializeField] private Key skill1Key = Key.Digit1;
    [SerializeField] private Key skill2Key = Key.Digit2;
    [SerializeField] private Key skill3Key = Key.Digit3;

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
        keyboard = Keyboard.current;

        foreach (Entry entry in entries)
        {
            PlayerSkillSlot slot = entry.skill;
            entry.button.onClick.AddListener(() =>
            {
                // PlayerSkillMention 튜토리얼 스텝에서는 언급만 하고 실제 발동은 막는다 - 자세한
                // 이유는 TutorialInputGate.BlockPlayerSkillCast 주석 참고.
                if (TutorialInputGate.BlockPlayerSkillCast) return;
                cast?.ArmSkill(slot);
            });
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
        skillSlide.HideNow(); // 시작은 낮이므로 연출 없이 꺼둔다
    }

    private void OnDestroy()
    {
        enviromentManager.OnNight -= Show;
        gameManager.ChangeToDay -= Hide;
    }

    // 밤 전환이 끝난 패널의 등장 연출을 시작한다.
    private void Show()
    {
        skillSlide.Open();
    }

    // 낮 전환이 시작된 패널의 퇴장 연출을 시작한다.
    private void Hide()
    {
        skillSlide.Close();
    }

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

        if (keyboard == null) return;
        // playerSkillGroup(CanvasGroup)의 blocksRaycasts는 버튼 클릭만 막고 여기서 직접 폴링하는
        // 키보드 입력은 못 막는다(GameSpeedUI와 동일한 이유) - PlayerSkillMention 스텝 등 튜토리얼
        // 진행 중엔 BlockHotkeys로 직접 막아야 한다.
        if (TutorialInputGate.BlockHotkeys) return;

        if (keyboard[skill1Key].wasPressedThisFrame)
        {
            if (mana.CurrentMana < entries[0].skill.manaCost) return;

            entries[0].button.onClick?.Invoke();
        }

        if (keyboard[skill2Key].wasPressedThisFrame)
        {
            if (mana.CurrentMana < entries[1].skill.manaCost) return;

            entries[1].button.onClick?.Invoke();
        }

        if (keyboard[skill3Key].wasPressedThisFrame)
        {
            if (mana.CurrentMana < entries[2].skill.manaCost) return;

            entries[2].button.onClick?.Invoke();
        }
    }
}
