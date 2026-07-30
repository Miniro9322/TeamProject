// 밤 전투 중 "시전자 선택 → 타겟 칸 클릭"으로 영웅의 장착 액티브 스킬 1개를 사용한다.
// PlaceAction.SelectTile을 통해 매 클릭마다 호출되며, MapAssemble이 조립한다.
public class HeroSkillCastController
{
    public BuildingUiLink buildingUi;
    private Hero selectedCaster;

    public void HandleClick(Tile tile)
    {
        if (tile == null || buildingUi.CanBuild()) return; // 낮에는 동작 안 함(CanBuild==true가 낮)

        Hero clickedHero = null;
        if (tile.OccupantObject != null)
            tile.OccupantObject.TryGetComponent(out clickedHero);
        bool clickedIsCastable = clickedHero != null && clickedHero.ActiveSkill != null && !clickedHero.IsDead;

        if (selectedCaster == null)
        {
            if (clickedIsCastable) selectedCaster = clickedHero;
            return;
        }

        if (clickedIsCastable && clickedHero != selectedCaster)
        {
            selectedCaster = clickedHero; // 다른 시전 가능 영웅 클릭 시 대상 전환
            return;
        }

        selectedCaster.TryUseActiveSkill(tile); // 성공/범위밖 실패 무관하게 선택 해제
        selectedCaster = null;
    }

    public void ClearSelection() => selectedCaster = null;
}
