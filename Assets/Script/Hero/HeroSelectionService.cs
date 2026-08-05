// 맵/로스터 어느 쪽에서 클릭하든 "현재 선택된 영웅"을 하나로 관리해 테두리 표시를 동기화한다.
public static class HeroSelectionService
{
    private static Hero current;

    public static Hero Current => current;

    public static void Select(Hero hero)
    {
        if (hero == null || current == hero) return;

        current?.SetSelected(false);
        current = hero;
        current.SetSelected(true);
    }

    public static void Clear()
    {
        current?.SetSelected(false);
        current = null;
    }

    // 제거된 유닛이 지금 선택된 영웅일 때만 해제한다(다른 영웅이 선택된 상태를 건드리지 않음).
    public static void ClearIfSelected(Hero hero)
    {
        if (hero == null || current != hero) return;
        Clear();
    }
}
