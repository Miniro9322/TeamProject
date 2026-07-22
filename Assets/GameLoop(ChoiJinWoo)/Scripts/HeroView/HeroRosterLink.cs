using UnityEngine;

// 로스터에서 배치된 유닛에 붙여 원본 엔트리를 기억해둔다.
// 이 유닛이 제거될 때 UnitRemover가 이 컴포넌트를 보고 엔트리를 Available로 되돌린다.
public class HeroRosterLink : MonoBehaviour
{
    public HeroRosterEntry Entry;
}
