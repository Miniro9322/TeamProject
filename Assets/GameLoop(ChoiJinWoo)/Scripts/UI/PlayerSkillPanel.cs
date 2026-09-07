using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class PlayerSkillPanel : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public Button button;
        public PlayerSkillSlot skill;
        [NonSerialized] public CanvasGroup canvasGroup;
    }

    [SerializeField] private List<Entry> entries;
    [SerializeField] private Image manaRing;
    [SerializeField] private SkillSlide skillSlide;
    [SerializeField] private SkillRadialToggle radialToggle;

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
    private bool started;
    private bool isFullyNight;

    [Inject]
    private void Construct(GameManager gameManager, EnviromentManager enviromentManager, PlayerManaManager mana)
    {
        this.gameManager = gameManager;
        this.enviromentManager = enviromentManager;
        this.mana = mana;
    }

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

        foreach (Entry entry in entries)
        {
            PlayerSkillSlot slot = entry.skill;
            entry.button.onClick.AddListener(() =>
            {
                if (TutorialInputGate.BlockPlayerSkillCast) return;
                cast?.ArmSkill(slot);
            });
            if (entry.button.GetComponent<TooltipTrigger>() is TooltipTrigger tooltip)
            {
                tooltip.SetMessaege(string.Format(DataTableManager.StringTable.Get(slot.skillDescKey), slot.manaCost));
            }

            entry.canvasGroup = entry.button.GetComponent<CanvasGroup>();
            if (entry.canvasGroup == null)
            {
                entry.canvasGroup = entry.button.gameObject.AddComponent<CanvasGroup>();
            }
        }

        mana.ManaChanged += RefreshManaDisplay;
        RefreshManaDisplay();

        enviromentManager.OnNight += Show;
        gameManager.ChangeToDay += Hide;
        radialToggle.Collapsed += SlideDownAfterCollapse;
        skillSlide.HideNow();

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
        radialToggle.Collapsed -= SlideDownAfterCollapse;
        mana.ManaChanged -= RefreshManaDisplay;

        skill1Action.Disable();
        skill1Action.Dispose();
        skill2Action.Disable();
        skill2Action.Dispose();
        skill3Action.Disable();
        skill3Action.Dispose();
    }

    private void Show()
    {
        isFullyNight = true;
        skillSlide.Open();
        radialToggle.ResetCollapsedNow();
    }

    private void Hide()
    {
        isFullyNight = false;
        if (radialToggle.IsSpread)
        {
            radialToggle.Collapse();
        }
        skillSlide.Close();
    }

    public void SpreadSkillButtons()
    {
        radialToggle.Spread();
    }

    private void SlideDownAfterCollapse()
    {
        skillSlide.Close();
    }

    private void RefreshManaDisplay()
    {
        manaRing.fillAmount = mana.CurrentMana / mana.MaxMana;

        foreach (Entry entry in entries)
        {
            bool canCast = mana.CurrentMana >= entry.skill.manaCost;
            entry.button.interactable = canCast;
            entry.canvasGroup.alpha = canCast ? 1f : 0.07f;
        }
    }

    private void TryCastHotkey(int index)
    {
        if (TutorialInputGate.BlockHotkeys) return;
        if (!isFullyNight) return;
        if (mana.CurrentMana < entries[index].skill.manaCost) return;

        entries[index].button.onClick?.Invoke();
    }
}
