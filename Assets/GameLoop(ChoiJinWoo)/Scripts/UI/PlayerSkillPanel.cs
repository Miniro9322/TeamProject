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
    [SerializeField] private Key skill1Key = Key.Digit1;
    [SerializeField] private Key skill2Key = Key.Digit2;
    [SerializeField] private Key skill3Key = Key.Digit3;
    private InputAction skill1Action;
    private InputAction skill2Action;
    private InputAction skill3Action;
    // 씬 로딩 중 게임을 끄면 Start가 끝나기 전에 OnDestroy가 불릴 수 있어, 이때 아래 필드들이
    // 아직 null이라 OnDestroy가 터진다. 이 플래그로 Start 완료 여부를 확인하고 조기 종료한다.
    private bool started;
    // gameManager.CanBuild는 밤 전환이 "시작"되자마자 false가 돼서, 그걸로 단축키를 막으면 낮->밤
    // 전환 연출이 다 끝나기도 전에 스킬을 쓸 수 있었다. 패널이 실제로 뜨는 시점(OnNight)과 맞춰
    // 이 플래그로 따로 추적한다.
    private bool isFullyNight;

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
        skill1Action = new InputAction("PlayerSkill1", binding: Keyboard.current[skill1Key].path);
        skill1Action.performed += _ => TryCastHotkey(0);
        skill1Action.Enable();

        skill2Action = new InputAction("PlayerSkill2", binding: Keyboard.current[skill2Key].path);
        skill2Action.performed += _ => TryCastHotkey(1);
        skill2Action.Enable();

        skill3Action = new InputAction("PlayerSkill3", binding: Keyboard.current[skill3Key].path);
        skill3Action.performed += _ => TryCastHotkey(2);
        skill3Action.Enable();

        mana.ManaChanged += RefreshManaDisplay;
        RefreshManaDisplay();

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

        started = true;
    }

    private void OnDestroy()
    {
        if (!started)
        {
            return;
        }

        enviromentManager.OnNight -= Show;
        gameManager.ChangeToDay -= Hide;
        mana.ManaChanged -= RefreshManaDisplay;

        skill1Action.Disable();
        skill1Action.Dispose();
        skill2Action.Disable();
        skill2Action.Dispose();
        skill3Action.Disable();
        skill3Action.Dispose();
    }

    // 밤 전환이 끝난 패널의 등장 연출을 시작한다.
    private void Show()
    {
        isFullyNight = true;
        skillSlide.Open();
    }

    // 낮 전환이 시작된 패널의 퇴장 연출을 시작한다.
    private void Hide()
    {
        isFullyNight = false;
        skillSlide.Close();
    }

    // mana.ManaChanged(밤 회복 틱마다 발화)를 구독해 갱신한다 - 매 프레임 폴링하지 않는다.
    private void RefreshManaDisplay()
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

    // playerSkillGroup(CanvasGroup)의 blocksRaycasts는 버튼 클릭만 막고 InputAction으로 받는 키보드
    // 입력은 못 막는다(GameSpeedUI와 동일한 이유) - PlayerSkillMention 스텝 등 튜토리얼 진행 중엔
    // BlockHotkeys로 직접 막아야 한다.
    private void TryCastHotkey(int index)
    {
        if (TutorialInputGate.BlockHotkeys) return;
        if (!isFullyNight) return; // 완전히 밤이 된 뒤(패널이 뜬 뒤)에만 동작 - 패널이 숨겨져 있어도 키 입력은 막혀 있지 않으므로 별도 체크 필요
        if (mana.CurrentMana < entries[index].skill.manaCost) return;

        entries[index].button.onClick?.Invoke();
    }
}
